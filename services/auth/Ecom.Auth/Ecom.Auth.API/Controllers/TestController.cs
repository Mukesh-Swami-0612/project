using Ecom.Auth.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecom.Auth.API.Controllers;

#if DEBUG
/// <summary>
/// Development-only test controller to verify authentication roles and policies.
/// </summary>
[ApiController]
[Route("api/v1/test")]
public class TestController : ControllerBase
{
    /// <summary>
    /// Test endpoint for Admin role authorization
    /// </summary>
    [Authorize(Roles = Roles.Admin)]
    [HttpGet("admin-only")]
    public IActionResult AdminOnly() => Ok("Welcome Admin");

    /// <summary>
    /// Test endpoint for User role authorization
    /// </summary>
    [Authorize(Roles = Roles.User)]
    [HttpGet("user-only")]
    public IActionResult UserOnly() => Ok("Welcome User");

    /// <summary>
    /// Test endpoint for any authenticated user
    /// </summary>
    [Authorize(Roles = Roles.Admin + "," + Roles.User)]
    [HttpGet("all-users")]
    public IActionResult AllUsers() => Ok("All authenticated users");
}
#endif
