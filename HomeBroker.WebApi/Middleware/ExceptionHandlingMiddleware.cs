using HomeBroker.Application;
using HomeBroker.Application.Exceptions;
using System.Net;
using System.Text.Json;

namespace HomeBroker.WebApi.Middleware
{
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
            catch (UnauthorizedException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access attempt");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (ForbiddenAccessException ex)
            {
                _logger.LogWarning(ex, "Forbidden access attempt");
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            object response = exception switch
            {
                NotFoundException notFoundException => new APIResponse(errors: ["The requested resource was not found."], statusCode: HttpStatusCode.NotFound),
                ValidationException validationException => new APIResponse(errors: validationException.Errors?.SelectMany(e => e.Value).ToList() ?? [validationException.Message], statusCode: HttpStatusCode.BadRequest),
                BadRequestException badRequestException => new APIResponse(errors: [badRequestException.Message], statusCode: HttpStatusCode.BadRequest),
                UnauthorizedException unauthorizedAccessException => new APIResponse(errors: [unauthorizedAccessException.Message], statusCode: HttpStatusCode.Unauthorized),
                ForbiddenAccessException forbiddenAccessException => new APIResponse(errors: [forbiddenAccessException.Message], statusCode: HttpStatusCode.Forbidden),
                _ => new APIResponse(errors: ["An internal server error occurred.", exception.Message], statusCode: HttpStatusCode.InternalServerError)
            };

            var statusCode = exception switch
            {
                NotFoundException => (int)HttpStatusCode.NotFound,
                ValidationException => (int)HttpStatusCode.BadRequest,
                BadRequestException => (int)HttpStatusCode.BadRequest,
                UnauthorizedException => (int)HttpStatusCode.Unauthorized,
                ForbiddenAccessException forbiddenAccessException => (int)HttpStatusCode.Forbidden,

                _ => (int)HttpStatusCode.InternalServerError
            };

            _logger.LogError(exception, "An error occurred:\nSTATUS_CODE: {StatusCode}\nERROR_MESSAGE: {Message}", statusCode, exception.Message);
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}
