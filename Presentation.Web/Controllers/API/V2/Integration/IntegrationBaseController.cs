using Microsoft.AspNetCore.Mvc;
using Presentation.Web.Infrastructure.Attributes;

namespace Presentation.Web.Controllers.API.V2.Integration;

[IntegrationApi]
[ApiExplorerSettings(IgnoreApi = false)] // Integration contracts use mTLS, not the public API's bearer scheme.
public abstract class IntegrationBaseController : ControllerBase;
