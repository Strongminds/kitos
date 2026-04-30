using Core.BackgroundJobs.Model;
using Core.DomainModel;
using Core.DomainServices.Context;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Infrastructure.Services.BackgroundJobs;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Query.Expressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Presentation.Web;
using Presentation.Web.Controllers.API.V1.Auth;
using Presentation.Web.Hangfire;
using Presentation.Web.Helpers;
using Presentation.Web.Infrastructure.DI;
using Presentation.Web.Infrastructure.OData;
using Presentation.Web.Swagger;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hangfire.PostgreSql;
using Hangfire.SqlServer;
using Npgsql;

// Digst.OioIdws.* assemblies are IL-patched local DLLs (not NuGet packages) referenced
// as "type:reference" in deps.json. The runtime's assembly loader skips those entries
// and the AppContext.BaseDirectory fallback can fail under some hosting models (IIS,
// dotnet watch). Registering an explicit resolver here ensures they are always found.
System.Runtime.Loader.AssemblyLoadContext.Default.Resolving += static (context, name) =>
{
    if (name.Name?.StartsWith("Digst.OioIdws.", StringComparison.Ordinal) != true)
        return null;
    var path = System.IO.Path.Combine(AppContext.BaseDirectory, name.Name + ".dll");
    return System.IO.File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
};

// Npgsql 6+ requires DateTime.Kind=Utc for 'timestamp with time zone' columns by default.
// Enable legacy behaviour so that Unspecified/Local datetimes are accepted, matching
// prior SQL Server behaviour while we progressively normalise datetime kinds.
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

var configuration = builder.Configuration;
var services = builder.Services;

// Controllers with Newtonsoft.Json + OData
services.AddControllersWithViews(options =>
    {
        // Block rights-holder-only users from all endpoints by default.
        // Endpoints that should be accessible to rights holders are decorated with [AllowRightsHoldersAccess].
        options.Filters.Add(new Presentation.Web.Infrastructure.Attributes.DenyRightsHoldersAccessAttribute());
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        options.SerializerSettings.TypeNameHandling = TypeNameHandling.Auto;
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
    })
    .AddOData(options =>
    {
        options.AddRouteComponents("odata", ODataModelConfig.GetEdmModel(), services =>
            services.AddSingleton<IFilterBinder, CaseInsensitiveContainsFilterBinder>());
        options.EnableQueryFeatures();
    });

// Routing
services.AddRouting();

// Authentication
var securityKeyString = configuration["AppSettings:SecurityKeyString"] ?? "";
var baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://localhost:44300/";

var signingKey = new SymmetricSecurityKey(
    System.Text.Encoding.UTF8.GetBytes(securityKeyString))
{
    // KeyId is required so the JWT includes a 'kid' header.
    // JsonWebTokenHandler (used by AddJwtBearer) throws IDX10517
    // when the token has no 'kid' and the validation key has no KeyId.
    KeyId = "kitos-jwt"
};

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
            // JsonWebTokenHandler (.NET 8+) defaults ClaimsIdentity.AuthenticationType to
            // "AuthenticationTypes.Federation" which OwinAuthenticationContextFactory does not
            // recognise. Setting it to "Bearer" keeps the existing switch mapping working.
            AuthenticationType = JwtBearerDefaults.AuthenticationScheme,
            // JsonWebTokenHandler does not apply inbound claim type mapping, so the JWT "name"
            // claim stays as "name". Setting NameClaimType ensures Identity.Name resolves it.
            NameClaimType = "name",
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
var isDevelopment = builder.Environment.IsDevelopment();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "KITOS API V1", Version = "1" });
    c.SwaggerDoc("v2", new OpenApiInfo { Title = "KITOS API V2", Version = "2" });

    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        // Skip actions without an explicit HTTP method binding (OData, naming-convention V1, etc.)
        if (string.IsNullOrEmpty(apiDesc.HttpMethod)) return false;

        var isV2Path = (apiDesc.RelativePath ?? "").IsExternalApiPath();

        if (docName == "v2") return isV2Path;

        // V1 doc: in development show all V1 endpoints; in release only token authentication
        if (isV2Path) return false;
        if (isDevelopment) return true;
        return apiDesc.ActionDescriptor is ControllerActionDescriptor cad &&
               cad.ControllerTypeInfo == typeof(TokenAuthenticationController);
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
    c.DocumentFilter<PurgeUnusedTypesDocumentFilter>(
        (Predicate<OpenApiDocument>)(doc => int.TryParse(doc.Info?.Version, out var v) && v >= 2));

    // Register all DTO model types so they appear in the Swagger "Models" section.
    // Applied after PurgeUnusedTypesDocumentFilter to ensure the manually-added schemas survive the purge.
    c.DocumentFilter<RegisterDtoSchemasDocumentFilter>(
        (Predicate<OpenApiDocument>)(doc => int.TryParse(doc.Info?.Version, out var v) && v < 2),
        "Presentation.Web.Models.API.V1");
    c.DocumentFilter<RegisterDtoSchemasDocumentFilter>(
        (Predicate<OpenApiDocument>)(doc => int.TryParse(doc.Info?.Version, out var v) && v >= 2),
        "Presentation.Web.Models.API.V2");

    c.OperationFilter<CreateOperationIdOperationFilter>();
    c.OperationFilter<FixNamingOfComplexQueryParametersFilter>();
    c.OperationFilter<FixContentParameterTypesOnSwaggerSpec>();

    c.SchemaFilter<SupplierFieldSchemaFilter>();
});

// Hangfire
var hangfireConnectionString = configuration.GetConnectionString("kitos_HangfireDB")
    ?? throw new InvalidOperationException("kitos_HangfireDB connection string is required");
var hangfireProvider = configuration["Hangfire:Provider"] ?? configuration["Database:Provider"];

EnsureHangfireDatabaseCreated(hangfireConnectionString, hangfireProvider);

services.AddHangfire(config =>
{
    config
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings();

    if (IsPostgreSqlProvider(hangfireProvider))
    {
        config.UsePostgreSqlStorage(hangfireConnectionString);
    }
    else
    {
        config.UseSqlServerStorage(hangfireConnectionString);
    }
});

services.AddSingleton<IBackgroundProcess>(provider => new KeepReadModelsInSyncProcess(provider));
services.AddHangfireServer();

// AutoMapper - using explicit assembly scanning
var mapperConfig = new AutoMapper.MapperConfiguration(cfg => {
    cfg.AddMaps(typeof(MappingConfig).Assembly);
}, NullLoggerFactory.Instance);
services.AddSingleton(mapperConfig.CreateMapper());

// HttpContext accessor
services.AddHttpContextAccessor();

// KITOS services (DI registrations)
KitosServiceRegistration.Register(services, configuration, signingKey);

var app = builder.Build();

// Initialize the SAML library's static HTTP context accessor so it can access HttpContext.Current
// during SAML flows without requiring DI injection into the (statically-instantiated) handler classes.
dk.nita.saml20.Utils.SamlHttpContextAccessor.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>());

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
// Forward X-Forwarded-For and X-Forwarded-Proto headers from reverse proxies (IIS Express,
// IIS, Nginx, etc.) so that Request.IsHttps correctly reflects the original connection.
// This must run before UseHttpsRedirection and the SAML handlers.
if (!app.Environment.IsDevelopment())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseHttpsRedirection();
}

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

// Hangfire dashboard — localhost only (enforced by LocalRequestsOnlyAuthorizationFilter)
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter() }
});

// Conventional route for MVC controllers (e.g. HomeController → "/" redirects to "/ui",
// OldHomeController renders the legacy AngularJS shell at "/old")
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Legacy .ashx handler routes: LoginHandler.ashx initiates external (SAML SSO) login
// by forwarding to the SAML sign-on endpoint with forceAuthn=true.
app.MapGet("/LoginHandler.ashx", (HttpContext ctx) =>
{
    ctx.Response.Redirect("/Login.ashx?forceAuthn=true");
    return Task.CompletedTask;
}).AllowAnonymous();

// SAML .ashx handlers — migrated to net10.0 in the local OIOSAML.Net project.
// Login.ashx handles both the SP-initiated AuthnRequest (GET) and the IdP response (POST).
app.MapMethods("/Login.ashx", new[] { "GET", "POST" }, (HttpContext ctx) =>
{
    try
    {
        Log.Information("SAML /Login.ashx invoked. Method: {Method}, QueryString: {QueryString}, ContentType: {ContentType}",
            ctx.Request.Method, ctx.Request.QueryString.Value, ctx.Request.ContentType);
        
        new dk.nita.saml20.protocol.Saml20SignonHandler().ProcessRequest(ctx);
        
        Log.Information("SAML /Login.ashx ProcessRequest completed. StatusCode: {StatusCode}", ctx.Response.StatusCode);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception in SAML endpoint {Endpoint}. Method: {Method}, QueryString: {QueryString}",
            "/Login.ashx", ctx.Request.Method, ctx.Request.QueryString.Value);
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    }

    return Task.CompletedTask;
}).AllowAnonymous();

app.MapMethods("/Logout.ashx", new[] { "GET", "POST" }, (HttpContext ctx) =>
{
    try
    {
        Log.Information("SAML /Logout.ashx invoked. Method: {Method}, QueryString: {QueryString}, ContentType: {ContentType}",
            ctx.Request.Method, ctx.Request.QueryString.Value, ctx.Request.ContentType);
        
        new dk.nita.saml20.protocol.Saml20LogoutHandler().ProcessRequest(ctx);
        
        Log.Information("SAML /Logout.ashx ProcessRequest completed. StatusCode: {StatusCode}", ctx.Response.StatusCode);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception in SAML endpoint {Endpoint}. Method: {Method}, QueryString: {QueryString}",
            "/Logout.ashx", ctx.Request.Method, ctx.Request.QueryString.Value);
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    }

    return Task.CompletedTask;
}).AllowAnonymous();

app.MapGet("/Metadata.ashx", (HttpContext ctx) =>
{
    try
    {
        Log.Information("SAML /Metadata.ashx invoked. QueryString: {QueryString}",
            ctx.Request.QueryString.Value);
        
        new dk.nita.saml20.protocol.Saml20MetadataHandler().ProcessRequest(ctx);
        
        Log.Information("SAML /Metadata.ashx ProcessRequest completed. StatusCode: {StatusCode}", ctx.Response.StatusCode);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Unhandled exception in SAML endpoint {Endpoint}. QueryString: {QueryString}",
            "/Metadata.ashx", ctx.Request.QueryString.Value);
        ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
    }

    return Task.CompletedTask;
}).AllowAnonymous();

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

static void EnsureHangfireDatabaseCreated(string hangfireConnectionString, string? provider)
{
    if (IsPostgreSqlProvider(provider))
    {
        var csb = new NpgsqlConnectionStringBuilder(hangfireConnectionString);
        var databaseName = csb.Database;
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("Hangfire PostgreSQL connection string must include a database name.");

        csb.Database = "postgres";

        using var connection = new NpgsqlConnection(csb.ConnectionString);
        connection.Open();
        using var existsCmd = connection.CreateCommand();
        existsCmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @dbName";
        existsCmd.Parameters.AddWithValue("dbName", databaseName);

        var exists = existsCmd.ExecuteScalar() != null;
        if (!exists)
        {
            using var createCmd = connection.CreateCommand();
            createCmd.CommandText = $"CREATE DATABASE \"{databaseName.Replace("\"", "\"\"")}\"";
            createCmd.ExecuteNonQuery();
        }

        return;
    }

    var sqlCsb = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(hangfireConnectionString);
    var sqlDatabaseName = sqlCsb.InitialCatalog;
    sqlCsb.InitialCatalog = "master";

    using var sqlConnection = new Microsoft.Data.SqlClient.SqlConnection(sqlCsb.ConnectionString);
    sqlConnection.Open();
    using var cmd = sqlConnection.CreateCommand();
    cmd.CommandText = $"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{sqlDatabaseName}') CREATE DATABASE [{sqlDatabaseName}]";
    cmd.ExecuteNonQuery();
}

static bool IsPostgreSqlProvider(string? provider)
{
    return string.Equals(provider, "PostgreSql", StringComparison.OrdinalIgnoreCase)
        || string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase)
        || string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase);
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
