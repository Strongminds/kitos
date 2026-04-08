using System;
using System.Linq;
using System.Threading;
using Core.Abstractions.Types;
using Core.BackgroundJobs.Model;
using Core.DomainModel;
using Core.DomainServices.Context;
using Core.DomainServices.Time;
using Hangfire;
using Hangfire.Common;
using Infrastructure.DataAccess.Interceptors;
using Infrastructure.Services.BackgroundJobs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Presentation.Web;
using Presentation.Web.Helpers;
using Presentation.Web.Infrastructure.DI;
using Presentation.Web.Infrastructure.OData;
using Presentation.Web.Swagger;
using Serilog;
using System.Data.Entity.Infrastructure.Interception;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var configuration = builder.Configuration;
var services = builder.Services;

// Controllers with Newtonsoft.Json + OData
services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
    })
    .AddOData(options =>
    {
        options.AddRouteComponents("odata", ODataModelConfig.GetEdmModel());
        options.EnableQueryFeatures();
    });

// Routing
services.AddRouting();

// Authentication
var securityKeyString = configuration["AppSettings:SecurityKeyString"] ?? "";
var baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:44300/";

var signingKey = new SymmetricSecurityKey(
    System.Text.Encoding.UTF8.GetBytes(securityKeyString));

const string multiScheme = "CookieOrJwt";
services.AddAuthentication(options =>
    {
        options.DefaultScheme = multiScheme;
        options.DefaultChallengeScheme = multiScheme;
    })
    .AddPolicyScheme(multiScheme, "Cookie or JWT", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var auth = context.Request.Headers[Microsoft.Net.Http.Headers.HeaderNames.Authorization].FirstOrDefault();
            if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return JwtBearerDefaults.AuthenticationScheme;
            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidIssuer = baseUrl,
            ValidateIssuer = true,
            IssuerSigningKey = signingKey,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/account/login";
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = 401;
                return System.Threading.Tasks.Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = 403;
                return System.Threading.Tasks.Task.CompletedTask;
            }
        };
    });

services.AddAuthorization();

// Swagger
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KITOS API V1", Version = "1" });
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "KITOS API V2", Version = "2" });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        // Skip actions without an explicit HTTP method binding (OData, naming-convention V1, etc.)
        if (string.IsNullOrEmpty(apiDesc.HttpMethod)) return false;

        var isV2Path = (apiDesc.RelativePath ?? "").IsExternalApiPath();
        return docName == "v2" ? isV2Path : !isV2Path;
    });

    c.CustomSchemaIds(type =>
    {
        if (!type.IsConstructedGenericType) return type.Name;
        string Recurse(Type t) => !t.IsConstructedGenericType
            ? t.Name
            : t.GetGenericArguments().Select(Recurse).Aggregate((a, b) => a + b) + t.Name.Split('`')[0];
        return Recurse(type).Replace("[]", "_array_");
    });

    c.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "The KITOS TOKEN"
    });

    c.DocumentFilter<FilterByApiVersionFilter>(
        (Func<OpenApiDocument, int>)(doc => int.TryParse(doc.Info?.Version, out var v) ? v : 1),
        (Func<string, int>)(path => path.IsExternalApiPath() ? 2 : 1));
    c.DocumentFilter<RemoveUnneededMutatingCallsFilter>(
        (Predicate<OpenApiDocument>)(doc => int.TryParse(doc.Info?.Version, out var v) && v < 2));
    c.DocumentFilter<OnlyIncludeReadModelSchemasInSwaggerDocumentFilter>();
    c.DocumentFilter<PurgeUnusedTypesDocumentFilter>();

    c.OperationFilter<CreateOperationIdOperationFilter>();
    c.OperationFilter<FixNamingOfComplexQueryParametersFilter>();
    c.OperationFilter<FixContentParameterTypesOnSwaggerSpec>();

    c.SchemaFilter<SupplierFieldSchemaFilter>();
});

// Hangfire
var hangfireConnectionString = configuration.GetConnectionString("kitos_HangfireDB")
    ?? throw new InvalidOperationException("kitos_HangfireDB connection string is required");

if (builder.Environment.IsDevelopment())
{
    EnsureHangfireDatabaseCreated(hangfireConnectionString);
}

services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireConnectionString));

services.AddHangfireServer();

// AutoMapper - using explicit assembly scanning
var mapperConfig = new AutoMapper.MapperConfiguration(cfg => {
    cfg.AddMaps(typeof(MappingConfig).Assembly);
});
services.AddSingleton(mapperConfig.CreateMapper());

// HttpContext accessor
services.AddHttpContextAccessor();

// KITOS services (DI registrations)
KitosServiceRegistration.Register(services, configuration);

var app = builder.Build();

// Register the EF6 interceptor that auto-assigns ObjectOwnerId, LastChangedByUserId and LastChanged
// on every INSERT/UPDATE. Uses IHttpContextAccessor to resolve per-request scoped services; for
// background jobs (no HTTP context) it falls back to a dedicated scope or safe defaults.
RegisterEfEntityInterceptor(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enable buffering early so the body stream can be rewound and re-read by WriteModelMapperBase
// (CurrentRequestStream.GetInputStreamCopy sets Position=0; only works if stream is buffered)
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    await next();
});

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v2/swagger.json", "KITOS API V2");
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "KITOS API V1");
});

app.UseSerilogRequestLogging();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Custom middleware (after auth so IAuthenticationContext is resolved with populated HttpContext.User)
app.UseMiddleware<Presentation.Web.Infrastructure.Middleware.CorrelationIdMiddleware>();
app.UseMiddleware<Presentation.Web.Infrastructure.Middleware.ApiRequestsLoggingMiddleware>();
app.UseMiddleware<Presentation.Web.Infrastructure.Middleware.DenyUsersWithoutApiAccessMiddleware>();
app.UseMiddleware<Presentation.Web.Infrastructure.Middleware.DenyModificationsThroughApiMiddleware>();
app.UseMiddleware<Presentation.Web.Infrastructure.Middleware.DenyTooLargeQueriesMiddleware>();

app.MapControllers();
// Conventional route for MVC controllers (e.g. HomeController → "/" redirects to "/ui",
// OldHomeController renders the legacy AngularJS shell at "/old")
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Initialize Hangfire recurring jobs
using (var scope = app.Services.CreateScope())
{
    InitializeHangfire(scope.ServiceProvider.GetRequiredService<IRecurringJobManager>());
}

app.Run();

void InitializeHangfire(IRecurringJobManager recurringJobManager)
{
    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.CheckExternalLinks,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchLinkCheckAsync(CancellationToken.None)),
        cronExpression: Cron.Weekly(DayOfWeek.Sunday, 0),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.ScheduleUpdatesForItSystemUsageReadModelsWhichChangesActiveState,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchUpdateStaleSystemUsageRmAsync(CancellationToken.None)),
        cronExpression: Cron.Daily(2),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.ScheduleUpdatesForItContractOverviewReadModelsWhichChangesActiveState,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchUpdateStaleContractRmAsync(CancellationToken.None)),
        cronExpression: Cron.Daily(2),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.ScheduleUpdatesForDataProcessingReadModelsWhichChangesActiveState,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchUpdateStaleDataProcessingRegistrationReadModels(CancellationToken.None)),
        cronExpression: Cron.Daily(2),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.ScheduleFkOrgUpdates,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchUpdateFkOrgSync(CancellationToken.None)),
        cronExpression: Cron.Weekly(DayOfWeek.Monday, 3),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.RebuildDataProcessingReadModels,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchFullReadModelRebuild(ReadModelRebuildScope.DataProcessingRegistration, CancellationToken.None)),
        cronExpression: Cron.Never(),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.RebuildItSystemUsageReadModels,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchFullReadModelRebuild(ReadModelRebuildScope.ItSystemUsage, CancellationToken.None)),
        cronExpression: Cron.Never(),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.RebuildItContractReadModels,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchFullReadModelRebuild(ReadModelRebuildScope.ItContract, CancellationToken.None)),
        cronExpression: Cron.Never(),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.PurgeOrphanedHangfireJobs,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchPurgeOrphanedHangfireJobs(CancellationToken.None)),
        cronExpression: Cron.Never(),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.CreateInitialPublicMessages,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchCreatePublicMessagesTask(CancellationToken.None)),
        cronExpression: Cron.Never(),
        timeZone: TimeZoneInfo.Local);

    recurringJobManager.AddOrUpdate(
        recurringJobId: StandardJobIds.CreateMainPublicMessage,
        job: Job.FromExpression((IBackgroundJobLauncher launcher) => launcher.LaunchCreateMainPublicMessageTask(CancellationToken.None)),
        cronExpression: Cron.Never(),
        timeZone: TimeZoneInfo.Local);
}

static void EnsureHangfireDatabaseCreated(string hangfireConnectionString)
{
    var csb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(hangfireConnectionString);
    var databaseName = csb.InitialCatalog;
    csb.InitialCatalog = "master";

    using var connection = new Microsoft.Data.SqlClient.SqlConnection(csb.ConnectionString);
    connection.Open();
    using var cmd = connection.CreateCommand();
    cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}') CREATE DATABASE [{databaseName}]";
    cmd.ExecuteNonQuery();
}

static void RegisterEfEntityInterceptor(IServiceProvider rootProvider)
{
    var httpContextAccessor = rootProvider.GetRequiredService<IHttpContextAccessor>();

    DbInterception.Add(new EFEntityInterceptor(
        operationClock: () =>
            httpContextAccessor.HttpContext?.RequestServices.GetService<IOperationClock>()
            ?? new OperationClock(),
        userContext: () =>
            httpContextAccessor.HttpContext?.RequestServices.GetService<Maybe<ActiveUserIdContext>>()
            ?? Maybe<ActiveUserIdContext>.None,
        fallbackUserResolver: () =>
            httpContextAccessor.HttpContext?.RequestServices.GetService<IFallbackUserResolver>()
            ?? new BackgroundJobFallbackUserResolver(rootProvider)));
}

/// <summary>
/// Used when there is no active HTTP request (e.g. Hangfire background jobs).
/// Creates a short-lived DI scope each time Resolve() is called so that
/// the underlying KitosContext is properly disposed after the query.
/// </summary>
sealed class BackgroundJobFallbackUserResolver : IFallbackUserResolver
{
    private readonly IServiceProvider _rootProvider;

    public BackgroundJobFallbackUserResolver(IServiceProvider rootProvider)
    {
        _rootProvider = rootProvider;
    }

    public User Resolve()
    {
        using var scope = _rootProvider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IFallbackUserResolver>().Resolve();
    }
}
