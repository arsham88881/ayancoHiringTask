using Application.Exceptions;
using Domain.Interfaces.Contexts;
using Domain.Models.Shared;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Serilog.Context;

namespace MainTask.webApi.Middlewares;


public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var eventManager = httpContext.RequestServices.GetRequiredService<IEventManagerContext>();
        var uow = httpContext.RequestServices.GetRequiredService<IUnitOfWork>();

        using (LogContext.PushProperty("EventId", eventManager.EventGuid.ToString()))
        {
            _logger.LogInformation("Request came with eventId: {Id}", eventManager.EventGuid);

            // ========== ۱. لغو توسط کاربر (Client Closed Request) ==========
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                httpContext.Response.StatusCode = 499;
                eventManager.WithDescription($"درخواست توسط کاربر کنسل شد. EventId: {eventManager.EventGuid}");
                await RollbackTransactionAsync(uow);

                await WriteApiResponseAsync(httpContext, new ApiResponse
                {
                    StatusCode = 499,
                    TrackingId = eventManager.EventGuid.ToString(),
                    Messages =
                    [
                        new MessageItem(MessageItemContexts.Error, "درخواست توسط کاربر لغو شد", "ClientClosedRequest")
                    ]
                }, cancellationToken);

                _logger.LogWarning("Request ended with 499. EventId: {Id}", eventManager.EventGuid);
                return true;
            }

            // ========== ۲. Timeout (اتمام زمان) ==========
            if (exception is OperationCanceledException && !httpContext.RequestAborted.IsCancellationRequested)
            {
                eventManager.WithDescription("درخواست به علت اتمام زمان پاسخ کنسل شد.");
                await RollbackTransactionAsync(uow);

                await WriteApiResponseAsync(httpContext, new ApiResponse
                {
                    StatusCode = StatusCodes.Status408RequestTimeout,
                    TrackingId = eventManager.EventGuid.ToString(),
                    Messages =
                    [
                        new MessageItem(MessageItemContexts.Error, "اتمام زمان درخواست (Timeout)", "RequestTimeout")
                    ]
                }, cancellationToken);

                return true;
            }

            // ذخیره InnerException
            eventManager.InnerException = exception.InnerException ?? exception;

            // ========== ۳. SQL Timeout ==========
            if (eventManager.InnerException is SqlException sqlEx && sqlEx.Number == -2)
            {
                eventManager.WithDescription("درخواست به علت اتمام زمان پاسخ (SQL Timeout) کنسل شد.");
                await RollbackTransactionAsync(uow);

                await WriteApiResponseAsync(httpContext, new ApiResponse
                {
                    StatusCode = StatusCodes.Status408RequestTimeout,
                    TrackingId = eventManager.EventGuid.ToString(),
                    Messages =
                    [
                        new MessageItem(MessageItemContexts.Error, "اتمام زمان درخواست دیتابیس (SQL Timeout)", "SqlTimeout")
                    ]
                }, cancellationToken);

                return true;
            }

            // ========== ۴. تعیین StatusCode و پیام ==========
            var (statusCode, messages) = exception switch
            {
                BusinessException business => (
                    business.Result.StatusCode,
                    business.Result.Messages ??
                    [
                        new MessageItem(MessageItemContexts.Error, "خطای کسب و کار", "BusinessError")
                    ]
                ),

                InfrastructureException => (
                    StatusCodes.Status500InternalServerError,
                    new[]
                    {
                        new MessageItem(MessageItemContexts.Error, "خطای زیرساخت", "InfrastructureError")
                    }
                ),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    new[]
                    {
                        new MessageItem(MessageItemContexts.Error, "خطای ناشناخته رخ داده است", "UnhandledError")
                    }
                )
            };

            // ثبت توضیحات
            eventManager.WithDescription($"خطا رخ داد: {messages.FirstOrDefault()?.Message}");

            // Rollback
            await RollbackTransactionAsync(uow);

            // نوشتن پاسخ با مدل خودت
            await WriteApiResponseAsync(httpContext, new ApiResponse
            {
                StatusCode = statusCode,
                TrackingId = eventManager.EventGuid.ToString(),
                Messages = messages
            }, cancellationToken);

            _logger.LogError(exception,
                "Request ended with exception. EventId: {Id}, StatusCode: {StatusCode}",
                eventManager.EventGuid, statusCode);

            return true;
        }
    }

    private static async Task WriteApiResponseAsync(
        HttpContext context,
        ApiResponse response,
        CancellationToken cancellationToken)
    {
        context.Response.StatusCode = response.StatusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(response, cancellationToken);
    }

    private async Task RollbackTransactionAsync(IUnitOfWork uow)
    {
        if (uow.HasActiveTransaction)
        {
            _logger.LogWarning(
                "Unhandled exception with active transaction {TransactionId}. Rolling back as safety net.",
                uow.TransactionId);

            try
            {
                await uow.RollbackAsync();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx,
                    "Rollback in safety net also failed for transaction {TransactionId}.",
                    uow.TransactionId);
            }
        }
    }
}