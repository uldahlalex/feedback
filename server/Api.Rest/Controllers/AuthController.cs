using Application.Interfaces;
using Application.Interfaces.Infrastructure.Websocket;
using Application.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Api.Rest.Controllers;

[ApiController]
public class AuthController(ISecurityService securityService, IConnectionManager connectionManager) : ControllerBase
{

    public const string LoginRoute =  nameof(Login);
    [HttpPost]
    [Route(LoginRoute)]
    public ActionResult<AuthResponseDto> Login([FromBody] AuthRequestDto dto)
    {
        return Ok(securityService.Login(dto));
    }

    public const string AuthWithJwtRoute = nameof(AuthWithJwt);

    [HttpGet]
    [Route(AuthWithJwtRoute)]
    public async Task<ActionResult> AuthWithJwt([FromHeader]string authorization, string clientId)
    {
        securityService.VerifyJwtOrThrow(authorization);
        await connectionManager.AddToTopic("alex", clientId);
        return Ok();
    }
}