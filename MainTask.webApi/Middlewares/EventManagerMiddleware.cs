using Application.Exceptions;
using Application.Services;
using Domain.Attributes.Shared;
using Domain.Interfaces.Contexts;
using Domain.Models.Audit;
using Domain.Models.Shared;
using Domain.ValueObjects.Shared;
using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace MainTask.webApi.Middlewares;

public class EventManagerMiddleware
{
    private readonly RequestDelegate next;
    private JsonSerializerOptions SerializationOptions { get; set; } = new JsonSerializerOptions()
    {
        TypeInfoResolver = new SkipNullJsonTypeInfoResolver(),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public EventManagerMiddleware(RequestDelegate next)
    {
        this.next = next;
    }

    public async Task InvokeAsync(HttpContext context, ILogger<EventManagerMiddleware> logger, IEventManagerContext eventManager)
    {
        var stopwatch = Stopwatch.StartNew();
        var requestTime = DateTime.Now;
        eventManager.GenerateEventGUID();
        logger.LogInformation("request com with this tracking Id {0}", eventManager.EventGuid.ToString());
        context.Request.EnableBuffering();

        var originalResponseBody = context.Response.Body;

        await using var responseBody = new MemoryStream();

        context.Response.Body = responseBody;

        var SaveEventParam = new SaveEventInModel();
        try
        {
            await next(context);

            stopwatch.Stop();

            var statusCode = context!.Response.StatusCode;
            SaveEventParam.Guid = eventManager.EventGuid;
            SaveEventParam.StartDate = requestTime;
            SaveEventParam.Duration = stopwatch.Elapsed.Milliseconds;
            SaveEventParam.Source = context.Request.Path.Value;
            SaveEventParam.IsSuccess = HttpStatusCodes.IsSuccess(statusCode) || HttpStatusCodes.IsRedirect(statusCode) ? true : false;
            SaveEventParam.StatusCode = statusCode;
            SaveEventParam.IpAddress = GetClientIpAddress(context!);

            var routeMothod = context!.Request.Method;
            var querystring = context.Request.QueryString;
            var routeValues = context!.GetRouteData()?.Values;

            if (statusCode == StatusCodes.Status404NotFound)
            {
                SaveEventParam.InputData = await ReadRequestBodyAsync(context.Request);

                var apiResponse = new ApiResponse(statusCode, [new MessageItem(MessageItemContexts.Error
                    , "خطای درخواست", "منبع درخواستی یافت نشد")])
                { TrackingId = eventManager.EventGuid.ToString() };

                SaveEventParam.OutputData = JsonSerializer.Serialize(apiResponse, SerializationOptions);

                throw ResponseHelper.Failure(apiResponse);
            }


            var endpoint = context.GetEndpoint();

            var routePattern = endpoint switch
            {
                RouteEndpoint routeEndpoint =>
                    routeEndpoint.RoutePattern.RawText,

                _ => context.Request.Path.Value
            };

            SaveEventParam.Source = string.Join("-", routeMothod, routePattern);


            if (statusCode == StatusCodes.Status405MethodNotAllowed)
            {
                var apiResponse = new ApiResponse(statusCode, [new MessageItem(MessageItemContexts.Error
                    , "خطای درخواست", "عملیات مجاز نمی باشد")])
                { TrackingId = eventManager.EventGuid.ToString() };
                SaveEventParam.InputData = await ReadRequestBodyAsync(context.Request);
                SaveEventParam.OutputData = JsonSerializer.Serialize(apiResponse, SerializationOptions);

                throw ResponseHelper.Failure(apiResponse);
            }

            SaveEventParam.InputData = await ReadRequestBodyAsync(context.Request);
            responseBody.Position = 0;
            using var reader = new StreamReader(
                responseBody,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            SaveEventParam.OutputData = await reader.ReadToEndAsync();
            responseBody.Position = 0;

            await responseBody.CopyToAsync(originalResponseBody);

            await eventManager.SaveEventLog(SaveEventParam);
        }
        catch (Exception ex)
        {
            if (ex is BusinessException response)
            {
                var responseBodyApi = JsonSerializer.Serialize(response.Result, SerializationOptions);
                await using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(responseBodyApi)))
                {
                    context.Response.ContentLength = memoryStream.Length;
                    await memoryStream.CopyToAsync(context.Response.Body);
                }
            }

            await eventManager.SaveEventLog(SaveEventParam);
        }

    }
    private string GetClientIpAddress(HttpContext context)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
            return realIp.Trim();

        return context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
    //private async Task<string?> ReadRequestBodyAsync(Stream body)
    //{
    //    body.Seek(0, SeekOrigin.Begin);
    //    using var reader = new StreamReader(body, leaveOpen: true);
    //    var bodyAsText = await reader.ReadToEndAsync();
    //    body.Seek(0, SeekOrigin.End);
    //    return string.IsNullOrEmpty(bodyAsText) || bodyAsText == "{}" ? null : bodyAsText;
    //}
    private async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (!request.Body.CanSeek)
            return null;

        request.Body.Position = 0;

        try
        {
            using var reader = new StreamReader(
                request.Body,
                Encoding.UTF8,
                detectEncodingFromByteOrderMarks: false,
                leaveOpen: true);

            var body = await reader.ReadToEndAsync();

            return string.IsNullOrWhiteSpace(body) || body == "{}"
                ? null
                : body;
        }
        finally
        {
            request.Body.Position = 0;
        }
    }
}
