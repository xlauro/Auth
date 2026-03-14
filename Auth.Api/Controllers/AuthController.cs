using Auth.Api.DTOs;
using Auth.Application.Exceptions;
using Auth.Application.UseCases;
using Auth.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly RegisterUseCase _registerUseCase;
    private readonly LoginUseCase _loginUseCase;

    public AuthController(RegisterUseCase registerUseCase, LoginUseCase loginUseCase)
    {
        _registerUseCase = registerUseCase;
        _loginUseCase = loginUseCase;
    }

    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _registerUseCase.ExecuteAsync(request.Email, request.Password, cancellationToken);
            var response = new RegisterResponse(user.Id, user.Email);
            return Ok(response);
        }
        catch (UserAlreadyExistsException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (ApplicationValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var token = await _loginUseCase.ExecuteAsync(request.Email, request.Password, cancellationToken);
            return Ok(new LoginResponse(token));
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ApplicationValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors });
        }
    }
}