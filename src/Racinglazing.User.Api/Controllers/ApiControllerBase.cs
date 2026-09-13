using Microsoft.AspNetCore.Mvc;
using Racinglazing.User.Api.Common;

namespace Racinglazing.User.Api.Controllers;

/// <summary>
/// Base for API controllers. Centralises the { data } response envelope so
/// actions stay focused on delegating to the application services.
/// </summary>
[ApiController]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected IActionResult Data<T>(T data) => Ok(new DataEnvelope<T>(data));
}
