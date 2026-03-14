using System.Net;
using Auth.Application.Exceptions;
using Auth.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Auth.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Path}", context.Request.Path);
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        return exception switch
        {
            ApplicationValidationException validationEx => WriteAsync(context, HttpStatusCode.BadRequest, new { errors = validationEx.Errors }),
            UserAlreadyExistsException alreadyExistsEx => WriteAsync(context, HttpStatusCode.Conflict, new { error = alreadyExistsEx.Message }),
            UserNotFoundException notFoundEx => WriteAsync(context, HttpStatusCode.NotFound, new { error = notFoundEx.Message }),
            _ => WriteAsync(context, HttpStatusCode.InternalServerError, new { error = "An unexpected error occurred." })
        };
    }

    private static Task WriteAsync(HttpContext context, HttpStatusCode statusCode, object body)
    {
        context.Response.StatusCode = (int)statusCode;
        return context.Response.WriteAsJsonAsync(body);
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
