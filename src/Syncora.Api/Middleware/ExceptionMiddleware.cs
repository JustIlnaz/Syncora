using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Syncora.Middleware
{
    /// <summary>
    /// Глобальный обработчик ошибок (ТЗ §22): стандартизированный JSON,
    /// без stack trace и чувствительных данных в ответе клиенту.
    /// </summary>
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        public ExceptionMiddleware(
            RequestDelegate next,
            ILogger<ExceptionMiddleware> logger,
            IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException)
            {
                await WriteError(context, StatusCodes.Status401Unauthorized,
                    "UNAUTHORIZED", "Требуется авторизация.");
            }
            catch (BadHttpRequestException ex)
            {
                await WriteError(context, StatusCodes.Status400BadRequest,
                    "BAD_REQUEST", ex.Message);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // Клиент отключился — ничего не отвечаем
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                var message = _environment.IsDevelopment()
                    ? ex.Message
                    : "Внутренняя ошибка сервера. Попробуйте позже.";

                await WriteError(context, StatusCodes.Status500InternalServerError,
                    "INTERNAL_ERROR", message);
            }
        }

        private static async Task WriteError(HttpContext context, int statusCode, string code, string message)
        {
            if (context.Response.HasStarted)
                return;

            context.Response.Clear();
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var body = JsonSerializer.Serialize(new
            {
                error = new { code, message }
            }, JsonOptions);

            await context.Response.WriteAsync(body);
        }
    }
}
