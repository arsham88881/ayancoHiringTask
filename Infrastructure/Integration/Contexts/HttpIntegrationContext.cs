using Domain.Interfaces.Contexts;
using Domain.Models.Shared;
using Microsoft.Extensions.Logging;
using Polly;
using RestSharp;

namespace Infrastructure.Integration.Contexts;

internal class HttpIntegrationContext : IHttpIntegrationContext
{
    private readonly ILogger<HttpIntegrationContext> logger;

    public HttpIntegrationContext(ILogger<HttpIntegrationContext> logger)
    {
        this.logger = logger;
    }


    public async Task<RestResult<OUTPUT>> GetAsync<OUTPUT>(
       string baseUrlAddress,
       RestRequestDto request,
       RestAdvanceOptions? options,
       CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<OUTPUT>(Method.Get, baseUrlAddress, request, options, cancellationToken);
    }

    public async Task<RestResult<OUTPUT>> PostAsync<OUTPUT>(
        string baseUrlAddress,
        RestRequestDto request,
        RestAdvanceOptions? options,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync<OUTPUT>(Method.Post, baseUrlAddress, request, options, cancellationToken);
    }


    /// //////////////////////////////////////////////

    public async Task<RestResult> GetAsync(
        string baseUrlAddress,
        RestRequestDto request,
        RestAdvanceOptions? options,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(Method.Get, baseUrlAddress, request, options, cancellationToken);
    }

    public async Task<RestResult> PostAsync(
        string baseUrlAddress,
        RestRequestDto request,
        RestAdvanceOptions? options,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(Method.Post, baseUrlAddress, request, options, cancellationToken);
    }

    private async Task<RestResult<OUTPUT>> ExecuteAsync<OUTPUT>(
        Method requestMethod,
        string baseUrlAddress,
        //bool isKeyUrlAddress,
        RestRequestDto request,
        RestAdvanceOptions? options,
        CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = new HttpClient();
        var rqst = new RestRequest();
        var guidRequest = Guid.NewGuid().ToString();
        var restClient = new RestClient();
        string? excutedCurl = string.Empty;
        var responseModel = new RestResponseLogData();
        int retryAttemptCounter = 0;
        string? exceptionType = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            httpClient.BaseAddress = new Uri(baseUrlAddress);

            options = options ?? new RestAdvanceOptions();
            var restOptions = new RestClientOptions()
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => options.DisableSsl
            };

            restClient = new RestClient(httpClient, restOptions);
            rqst = new RestRequest(request.EndPoint, requestMethod);
            rqst.Timeout = TimeSpan.FromSeconds(options.TimeoutConfig);

            if (request.Token is not null)
                rqst.AddHeader("Authorization", request.Token);

            if (request.Headers.Any())
                foreach (var header in request.Headers)
                    rqst.AddHeader(header.Key, header.Value);

            if (request.BodyParams is not null && !new[] { Method.Get, Method.Delete }.Contains(requestMethod))
                rqst.AddBody(request.BodyParams);

            logger.LogInformation($"sending request with method: {rqst.Method.ToString().ToUpper()}");

            if (request.SegmentParams is not null)
                AddSegment(rqst, request.SegmentParams);

            if (request.QuaryParams is not null)
                AddQuary(rqst, request.QuaryParams);

            //excutedCurl = ExcutionCurlCreator(restClient, rqst, options, request);

            RestResult<OUTPUT> response;
            var overallStartDate = DateTime.UtcNow;

            if (options.RetryCount is not null && options.RetryCount > 0)
            {
                // ساخت Retry Policy برای Generic
                var retryPolicy = Policy<RestResult<OUTPUT>>
                    .Handle<HttpRequestException>()
                    .OrResult(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(
                        retryCount: options.RetryCount.Value,
                        sleepDurationProvider: options.SleepDurationRetryFunc,
                        onRetry: (outcome, timespan, currentRetryCount, context) =>
                        {
                            if (outcome.Exception != null)
                            {
                                exceptionType = outcome.Exception.GetType().Name;
                                logger.LogWarning($"Retry {currentRetryCount} after {timespan.TotalSeconds}s. Exception: {outcome.Exception.GetType().Name} - {outcome.Exception.Message}");
                            }
                            else if (outcome.Result != null)
                                logger.LogWarning($"Retry {currentRetryCount} after {timespan.TotalSeconds}s. Status Code: {outcome.Result.StatusCode}");

                            retryAttemptCounter = currentRetryCount;
                        });

                //// ایجاد Context برای Polly
                var policyContext = new Polly.Context
                {
                    ["AttemptStartDate"] = overallStartDate
                };
                // اجرای درخواست با Retry Policy
                response = await retryPolicy.ExecuteAsync(
                    async (ctx, ct) =>
                    {
                        var attemptStart = DateTime.UtcNow;
                        try
                        {
                            return await restClient.ExecuteAsync<OUTPUT>(rqst, ct);
                        }
                        finally
                        {
                            ctx["AttemptStartDate"] = attemptStart;
                        }
                    },
                    policyContext,
                    cancellationToken);

                // افزودن پاسخ نهایی موفق به لیست
                var finalElapsed = DateTime.UtcNow - overallStartDate;
                responseModel = new RestResponseLogData
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (short)response.StatusCode,
                    ExceptionType = exceptionType,
                    Response = (response.Content ?? "") + (response.ErrorMessage ?? ""),
                    PeriodTime = (int)finalElapsed.TotalMilliseconds,
                    StartDate = overallStartDate,
                    CompleteDate = DateTime.UtcNow,
                    RetryAttempt = retryAttemptCounter + 1
                };
            }
            else
            {
                // بدون retry
                var startDate = DateTime.UtcNow;
                response = await restClient.ExecuteAsync<OUTPUT>(rqst, cancellationToken);
                var elapsed = DateTime.UtcNow - startDate;
                responseModel = new RestResponseLogData
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (short)response.StatusCode,
                    Response = (response.Content ?? "") + (response.ErrorMessage ?? ""),
                    PeriodTime = (int)elapsed.TotalMilliseconds,
                    StartDate = startDate,
                    CompleteDate = DateTime.UtcNow,
                    RetryAttempt = 1
                };
            }

            var finalUrl = restClient.BuildUri(rqst).ToString();
            logger.LogInformation($"Request sent successfully to: {finalUrl}");

            response = response with { RequestLogGuid = guidRequest, FinalSendedUrl = finalUrl, RestResponseLogData = responseModel };




            return response;
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();


            var finalUrl = restClient.BuildUri(rqst).ToString();
            logger.LogError(ex, "Error in RestService for URL: {Url} with curl: {Curl}", finalUrl, excutedCurl ?? "");

            throw;
        }
        finally
        {
            restClient?.Dispose();
            httpClient?.Dispose();
        }
    }
    private async Task<RestResult> ExecuteAsync(
        Method requestMethod,
        string baseUrlAddress,
        RestRequestDto request,
        RestAdvanceOptions? options,
        CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = new HttpClient();
        var rqst = new RestRequest();
        var guidRequest = Guid.NewGuid().ToString();
        var restClient = new RestClient();
        string? excutedCurl = string.Empty;
        var responseModel = new RestResponseLogData();
        int retryAttemptCounter = 0;
        string? exceptionType = null;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            //if (isKeyUrlAddress)
            //    httpClient = httpClientFactory.CreateClient(urlAddressKey);
            //else
            httpClient.BaseAddress = new Uri(baseUrlAddress);

            options = options ?? new RestAdvanceOptions();
            var restOptions = new RestClientOptions()
            {
                RemoteCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => options.DisableSsl
            };

            //if (options.LogConfig != ExcutionLogConfig.None && request.EventManager is null)
            //    throw ResponseHandler.Failure(StatusCodes.Status500InternalServerError, [new ErrorItem("DeveloperError",
            //        "When You Want Save Log on Errors Or All Sended request from Ingration you need pass   EventManagerService from RequestDto")]);

            restClient = new RestClient(httpClient, restOptions);
            rqst = new RestRequest(request.EndPoint, requestMethod);
            rqst.Timeout = TimeSpan.FromSeconds(options.TimeoutConfig);

            if (request.Token is not null)
                rqst.AddHeader("Authorization", request.Token);

            if (request.Headers.Any())
                foreach (var header in request.Headers)
                    rqst.AddHeader(header.Key, header.Value);

            if (request.BodyParams is not null && !new[] { Method.Get, Method.Delete }.Contains(requestMethod))
                rqst.AddBody(request.BodyParams);

            logger.LogInformation($"sending request with method: {rqst.Method.ToString().ToUpper()}");

            if (request.SegmentParams is not null)
                AddSegment(rqst, request.SegmentParams);

            if (request.QuaryParams is not null)
                AddQuary(rqst, request.QuaryParams);

            //excutedCurl = ExcutionCurlCreator(restClient, rqst, options, request);

            RestResult response;
            var overallStartDate = DateTime.UtcNow;

            if (options.RetryCount is not null && options.RetryCount > 0)
            {
                // ساخت Retry Policy
                var retryPolicy = Policy<RestResult>
                    .Handle<HttpRequestException>()
                    .OrResult(r => !r.IsSuccessStatusCode)
                    .WaitAndRetryAsync(
                        retryCount: options.RetryCount.Value,
                        sleepDurationProvider: options.SleepDurationRetryFunc,
                        onRetry: (outcome, timespan, currentRetryCount, context) =>
                        {
                            if (outcome.Exception != null)
                            {
                                exceptionType = outcome.Exception.GetType().Name;
                                logger.LogWarning($"Retry {currentRetryCount} after {timespan.TotalSeconds}s. Exception: {outcome.Exception.GetType().Name} - {outcome.Exception.Message}");
                            }
                            else if (outcome.Result != null)
                                logger.LogWarning($"Retry {currentRetryCount} after {timespan.TotalSeconds}s. Status Code: {outcome.Result.StatusCode}");

                            retryAttemptCounter = currentRetryCount;
                        });

                // اجرای درخواست با Retry Policy
                var policyContext = new Polly.Context
                {
                    ["AttemptStartDate"] = overallStartDate
                };

                response = await retryPolicy.ExecuteAsync(
                    async (ctx, ct) =>
                    {
                        var attemptStart = DateTime.UtcNow;
                        try
                        {
                            return await restClient.ExecuteAsync(rqst, ct);
                        }
                        finally
                        {
                            ctx["AttemptStartDate"] = attemptStart;
                        }
                    },
                    policyContext,
                    cancellationToken);

                // افزودن پاسخ نهایی موفق به لیست
                var finalElapsed = DateTime.UtcNow - overallStartDate;
                responseModel = new RestResponseLogData
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (short)response.StatusCode,
                    Response = (response.Content ?? "") + (response.ErrorMessage ?? ""),
                    PeriodTime = (int)finalElapsed.TotalMilliseconds,
                    StartDate = overallStartDate,
                    CompleteDate = DateTime.UtcNow,
                    RetryAttempt = retryAttemptCounter + 1
                };
            }
            else
            {
                // بدون retry
                response = await restClient.ExecuteAsync(rqst, cancellationToken);
                var elapsed = DateTime.UtcNow - overallStartDate;
                responseModel = new RestResponseLogData
                {
                    IsSuccess = response.IsSuccessStatusCode,
                    StatusCode = (short)response.StatusCode,
                    Response = (response.Content ?? "") + (response.ErrorMessage ?? ""),
                    PeriodTime = (int)elapsed.TotalMilliseconds,
                    StartDate = overallStartDate,
                    CompleteDate = DateTime.UtcNow,
                    RetryAttempt = 1
                };
            }

            var finalUrl = restClient.BuildUri(rqst).ToString();
            logger.LogInformation($"Request sent successfully to: {finalUrl}");

            response = response with { RequestLogGuid = guidRequest, FinalSendedUrl = finalUrl, RestResponseLogData = responseModel };


            return response;
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            var finalUrl = restClient.BuildUri(rqst).ToString();
            logger.LogError(ex, "Error in RestService for URL: {Url} with curl: {Curl}", finalUrl, excutedCurl ?? "");

            throw;
        }
        finally
        {
            // تمیز کردن منابع
            restClient?.Dispose();
            httpClient?.Dispose();
        }
    }

    private void AddSegment(RestRequest request, object prms)
    {
        var propertyList = prms.GetType().GetProperties();
        foreach (var prop in propertyList)
        {
            var key = prop.Name;
            var value = prop.GetValue(prms);

            if (value == null) continue;

            request.AddUrlSegment(key, value?.ToString() ?? "");

        }
    }
    private void AddQuary(RestRequest request, object prms)
    {
        var propertyList = prms.GetType().GetProperties();
        foreach (var prop in propertyList)
        {
            var key = prop.Name;
            var value = prop.GetValue(prms);

            if (value == null) continue;
            request.AddQueryParameter(key, value?.ToString() ?? "");
        }
    }



}
