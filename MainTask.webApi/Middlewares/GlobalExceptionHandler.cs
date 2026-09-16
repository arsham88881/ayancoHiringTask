using Application.Exceptions;
using Domain.Interfaces.Contexts;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Serilog.Context;

namespace MainTask.webApi.Middlewares;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // دریافت سرویس‌ها از DI
        var eventManager = httpContext.RequestServices.GetRequiredService<IEventManagerContext>();
        var uow = httpContext.RequestServices.GetRequiredService<IUnitOfWork>();

        // ثبت EventId در لاگ
        using (LogContext.PushProperty("FarnoorEventId", eventManager.EventGuid.ToString()))
        {
            _logger.LogInformation("Request came with eventId: {Id}", eventManager.EventGuid);

            // مدیریت لغو درخواست توسط کاربر (Client Closed Request)
            if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
            {
                httpContext.Response.StatusCode = 499;
                eventManager.WithDescription($"درخواست توسط کاربر کنسل شد. EventId: {eventManager.EventGuid}");
                await RollbackTransactionAsync(uow);
                _logger.LogWarning("Request ended with 499. EventId: {Id}", eventManager.EventGuid);
                return true; // خطا مدیریت شد
            }

            // مدیریت Timeout (اتمام زمان)
            if (exception is OperationCanceledException && !httpContext.RequestAborted.IsCancellationRequested)
            {
                eventManager.WithDescription("درخواست به علت اتمام زمان پاسخ کنسل شد.");
                await Prepare408TimeoutAsync(httpContext, eventManager, cancellationToken);
                await RollbackTransactionAsync(uow);
                return true;
            }

            // ذخیره InnerException
            eventManager.InnerException = exception.InnerException ?? exception;

            // بررسی Timeout دیتابیس
            if (eventManager.InnerException is SqlException sqlEx && sqlEx.Number == -2)
            {
                eventManager.WithDescription("درخواست به علت اتمام زمان پاسخ (SQL Timeout) کنسل شد.");
                await Prepare408TimeoutAsync(httpContext, eventManager, cancellationToken);
                return true;
            }

            // تعیین StatusCode بر اساس نوع Exception
            var statusCode = exception switch
            {
                InfrastructureException => StatusCodes.Status500InternalServerError,
                BusinessException business => business.Result.StatusCode,
                //BusinessException rolling => rolling.Result.StatusCode,
                //ApiExceptionWithOut rolling => rolling.Result.StatusCode,
                _ => StatusCodes.Status500InternalServerError
            };

            // ساخت پاسخ ProblemDetails
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = "خطا در پردازش درخواست",
                Detail = GetErrorMessage(exception),
                Instance = httpContext.Request.Path
            };

            // افزودن TrackingId
            problemDetails.Extensions["trackingId"] = eventManager.EventGuid.ToString();

            // ثبت توضیحات در eventManager
            eventManager.WithDescription($"خطا رخ داد: {problemDetails.Detail}");

            // Rollback تراکنش
            await RollbackTransactionAsync(uow);

            // نوشتن پاسخ با IProblemDetailsService
            httpContext.Response.StatusCode = statusCode;
            await _problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            });

            _logger.LogError(exception, "Request ended with exception. EventId: {Id}, StatusCode: {StatusCode}",
                eventManager.EventGuid, statusCode);

            return true; // خطا مدیریت شد
        }
    }

    private static string GetErrorMessage(Exception exception)
    {
        return exception switch
        {
            InfrastructureException => "خطای زیر ساخت",
            BusinessException => "خطای کسب و کار",
            _ => "خطای ناشناخته"
        };
    }

    private async Task Prepare408TimeoutAsync(
        HttpContext context,
        IEventManagerContext eventManager,
        CancellationToken cancellationToken)
    {
        context.Response.StatusCode = StatusCodes.Status408RequestTimeout;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status408RequestTimeout,
            Title = "Request Timeout",
            Detail = "اتمام درخواست",
            Instance = context.Request.Path
        };
        problemDetails.Extensions["trackingId"] = eventManager.EventGuid.ToString();

        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problemDetails
        });

        _logger.LogWarning("Request ended with timeout. EventId: {Id}", eventManager.EventGuid);
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
