using Application.Exceptions;
using Domain.Interfaces.Contexts;
using Domain.Models.Shared;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using RestSharp;
using System.Text;

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
        RestRequestDto request,
        RestAdvanceOptions? options,
        CancellationToken cancellationToken = default)
    {
        HttpClient httpClient = new HttpClient();
        var rqst = new RestRequest();
        //var guidRequest = Guid.NewGuid().ToString();
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

            excutedCurl = ExcutionCurlCreator(restClient, rqst, options, request);

            RestResult<OUTPUT> response;
            var overallStartDate = DateTime.UtcNow;

            if (options.RetryCount is not null && options.RetryCount >= 1)
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
                    ExcutedCurl = excutedCurl,
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
                    ExcutedCurl = excutedCurl,
                    RetryAttempt = 1
                };
            }

            var finalUrl = restClient.BuildUri(rqst).ToString();
            logger.LogInformation($"Request sent successfully to: {finalUrl}");

            response = response with { FinalSendedUrl = finalUrl, RestResponseLogData = responseModel };




            return response;
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();


            var finalUrl = restClient.BuildUri(rqst).ToString();
            logger.LogError(ex, "Error in RestService for URL: {Url} with curl: {Curl}", finalUrl, excutedCurl ?? "");

            throw new InfrastructureException(ex, "خطا ی سرویس ارسال درخواست");
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
        //var guidRequest = Guid.NewGuid().ToString();
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

            excutedCurl = ExcutionCurlCreator(restClient, rqst, options, request);

            RestResult response;
            var overallStartDate = DateTime.UtcNow;

            if (options.RetryCount is not null && options.RetryCount >= 1)
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
                    ExcutedCurl = excutedCurl,
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
                    ExcutedCurl = excutedCurl,
                    CompleteDate = DateTime.UtcNow,
                    RetryAttempt = 1
                };
            }

            var finalUrl = restClient.BuildUri(rqst).ToString();
            logger.LogInformation($"Request sent successfully to: {finalUrl}");

            response = response with
            {
                FinalSendedUrl = finalUrl,
                RestResponseLogData = responseModel
            };


            return response;
        }
        catch (Exception ex)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();

            var finalUrl = restClient.BuildUri(rqst).ToString();
            logger.LogError(ex, "Error in RestService for URL: {Url} with curl: {Curl}", finalUrl, excutedCurl ?? "");

            throw new InfrastructureException(ex, "خطا ی سرویس ارسال درخواست");
        }
        finally
        {
            // تمیز کردن منابع
            restClient?.Dispose();
            httpClient?.Dispose();
        }
    }
    private string ExcutionCurlCreator(RestClient client, RestRequest request, RestAdvanceOptions options, RestRequestDto requestDto)
    {
        var finalUrl = client.BuildUri(request).ToString();
        var curlBuilder = new StringBuilder();

        // تخمین ظرفیت اولیه برای جلوگیری از reallocation
        // URL + method + headers + body (تقریبی)
        int estimatedCapacity = 256 + (request.Parameters.Count * 64);
        if (requestDto.BodyParams != null)
            estimatedCapacity += 1024;

        var optimizedBuilder = new StringBuilder(estimatedCapacity);

        optimizedBuilder.Append("curl --location '")
                       .Append(finalUrl)
                       .Append("' \\\n    --request ")
                       .Append(request.Method.ToString().ToUpper());

        var headers = request.Parameters.Where(p => p.Type == ParameterType.HttpHeader).ToList();

        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            optimizedBuilder.Append(" \\\n    --header '")
                           .Append(header.Name)
                           .Append(": ")
                           .Append(header.Value?.ToString() ?? "")
                           .Append("'");
        }


        if (!headers.Any(h => h.Name?.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) == true) &&
            requestDto.BodyParams != null)
            optimizedBuilder.Append(" \\\n    --header 'Content-Type: application/json'");

        if (requestDto.BodyParams != null &&
            !new[] { Method.Get, Method.Delete }.Contains(request.Method))
        {
            var jsonBody = JsonConvert.SerializeObject(requestDto.BodyParams, Formatting.None);

            if (jsonBody.Contains("'"))
                jsonBody = jsonBody.Replace("'", "'\"'\"'");

            optimizedBuilder.Append(" \\\n    --data '")
                           .Append(jsonBody)
                           .Append("'");
        }

        if (options.DisableSsl)
            optimizedBuilder.Append(" \\\n    --insecure");

        optimizedBuilder.Append(" \\\n    --max-time ")
                       .Append(options.TimeoutConfig);

        return optimizedBuilder.ToString();
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
