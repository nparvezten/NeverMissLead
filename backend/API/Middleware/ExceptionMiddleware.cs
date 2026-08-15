using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace NeverMissLead.API.Middleware;

/// <summary>
/// Global exception handler that converts unhandled exceptions into
/// RFC 7807 ProblemDetails responses. No stack traces leak to the client.
/// Sensitive details are logged server-side only.
/// </summary>
public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException vex)
        {
            Log.Warning(vex, "Validation failed for {Path}", context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status422UnprocessableEntity,
                "Validation Failed",
                "One or more validation errors occurred.",
                vex.Errors.Select(e => e.ErrorMessage).ToArray());
        }
        catch (KeyNotFoundException)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound,
                "Not Found", "The requested resource was not found.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unhandled exception on {Method} {Path}",
                context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, StatusCodes.Status500InternalServerError,
                "Server Error", "An unexpected error occurred. Please try again later.");
        }
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string[]? errors = null)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        if (errors?.Length > 0)
            problem.Extensions["errors"] = errors;

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
