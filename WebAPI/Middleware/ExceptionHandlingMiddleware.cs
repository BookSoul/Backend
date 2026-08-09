using FluentValidation;
using System.Text.Json;

namespace WebAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            // Bật buffering để có thể đọc response
            context.Request.EnableBuffering();
            
            await _next(context);
            
            if (context.Response.StatusCode == 400)
            {
                Console.WriteLine($"[400 Bad Request] Path: {context.Request.Path}");
            }
        }
        catch (ValidationException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[400 Validation Error] {string.Join(", ", ex.Errors.Select(e => e.ErrorMessage))}");
            Console.ResetColor();

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var payload = new
            {
                message = "Validation failed.",
                errors = ex.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
            };
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (KeyNotFoundException ex)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
        catch (UnauthorizedAccessException ex)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
        catch (InvalidOperationException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[400 Business Rule Error] {ex.Message}");
            Console.ResetColor();

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = ex.Message }));
        }
        catch (Exception)
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "Internal server error." }));
        }
    }
}
