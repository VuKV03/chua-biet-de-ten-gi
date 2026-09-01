using Admin.Application.Auth.Commands;
using Admin.Application.Auth.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Api.Base;
using SharedKernel.Application.Exceptions;
using SharedKernel.Application.Interfaces;

namespace Admin.Api.Controllers;

[Route("api/auth")]
[ApiController]
public class AuthController : BaseApiController
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginCommand request)
    {
        try
        {
            request.diaChiIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            request.thongTinThietBi = HttpContext.Request.Headers.UserAgent.ToString();

            var result = await Mediator.Send(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> RefreshToken([FromBody] RefreshTokenCommand request)
    {
        try
        {
            var result = await Mediator.Send(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] LogoutCommand request)
    {
        try
        {
            var identityService = HttpContext.RequestServices.GetRequiredService<IIdentityService>();
            request.userId = identityService.currentUser?.id;

            await Mediator.Send(request);
            return Ok(new { message = "Đăng xuất thành công." });
        }
        catch (Exception ex)
        {
            var err = ErrorCtr.ExtractErrorInfo(ex);
            return StatusCode((int)err.errorCode, err.errors);
        }
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult GetCurrentUser()
    {
        var identityService = HttpContext.RequestServices.GetRequiredService<IIdentityService>();
        return Ok(identityService.currentUser);
    }
}
