using System;
using System.Collections.Generic;
using System.Net;
using RestSharp;
using System.Text;

namespace Domain.Models.Shared;


public record class RestResult
{
    public RestRequest? Request { get; set; }
    public Exception? InnerRequestException { get; set; }
    //
    // Summary:
    //     MIME content type of response
    public string? ContentType { get; set; }

    //
    // Summary:
    //     Length in bytes of the response content
    public long? ContentLength { get; set; }

    //
    // Summary:
    //     Encoding of the response content
    public ICollection<string> ContentEncoding { get; set; } = Array.Empty<string>();


    //
    // Summary:
    //     String representation of response content
    public string? Content { get; set; }

    //
    // Summary:
    //     HTTP response status code
    public HttpStatusCode StatusCode { get; set; }

    //
    // Summary:
    //     Whether the HTTP response status code indicates success
    public bool IsSuccessStatusCode { get; set; }

    //
    // Summary:
    //     Whether the HTTP response status code indicates success and no other error occurred
    //     (deserialization, timeout, ...)
    public bool IsSuccessful
    {
        get
        {
            if (IsSuccessStatusCode)
            {
                return ResponseStatus == ResponseStatus.Completed;
            }

            return false;
        }
    }

    //
    // Summary:
    //     Description of HTTP status returned
    public string? StatusDescription { get; set; }

    //
    // Summary:
    //     Response content
    public byte[]? RawBytes { get; set; }

    //
    // Summary:
    //     The URL that actually responded to the content (different from request if redirected)
    public Uri? ResponseUri { get; set; }

    //
    // Summary:
    //     Server header value
    public string? Server { get; set; }

    //
    // Summary:
    //     Cookies returned by server with the response
    public CookieCollection? Cookies { get; set; }

    //
    // Summary:
    //     Response headers returned by server with the response
    public IReadOnlyCollection<HeaderParameter>? Headers { get; set; }

    //
    // Summary:
    //     Content headers returned by server with the response
    public IReadOnlyCollection<HeaderParameter>? ContentHeaders { get; set; }
    //
    // Summary:
    //     Status of the request. Will return Error for transport errors. HTTP errors will
    //     still return ResponseStatus.Completed, check StatusCode instead
    public ResponseStatus ResponseStatus { get; set; }
    //
    // Summary:
    //     Transport or another non-HTTP error generated while attempting request
    public string? ErrorMessage { get; set; }
    public string? FinalSendedUrl { get; set; }
    public RestResponseLogData? RestResponseLogData { get; set; }

    public static implicit operator RestResult(RestResponse response)
    {
        return new RestResult()
        {
            Request = response.Request,
            ContentType = response.ContentType,
            ContentLength = response.ContentLength,
            ContentEncoding = response.ContentEncoding,
            Content = response.Content,
            StatusCode = response.StatusCode,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            StatusDescription = response.StatusDescription,
            RawBytes = response.RawBytes,
            ResponseUri = response.ResponseUri,
            Server = response.Server,
            Cookies = response.Cookies,
            Headers = response.Headers,
            ContentHeaders = response.ContentHeaders,
            ResponseStatus = response.ResponseStatus,
            ErrorMessage = response.ErrorMessage,
            InnerRequestException = response.ErrorException,
        };

    }

}

public record class RestResult<OUTPUT> : RestResult
{
    public OUTPUT? Value { get; set; }
    public static implicit operator RestResult<OUTPUT>(RestResponse<OUTPUT> response)
    {
        return new RestResult<OUTPUT>()
        {
            Request = response.Request,
            ContentType = response.ContentType,
            ContentLength = response.ContentLength,
            ContentEncoding = response.ContentEncoding,
            Content = response.Content,
            StatusCode = response.StatusCode,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            StatusDescription = response.StatusDescription,
            RawBytes = response.RawBytes,
            ResponseUri = response.ResponseUri,
            Server = response.Server,
            Cookies = response.Cookies,
            Headers = response.Headers,
            ContentHeaders = response.ContentHeaders,
            ResponseStatus = response.ResponseStatus,
            ErrorMessage = response.ErrorMessage,
            InnerRequestException = response.ErrorException,
            Value = response.Data,
        };

    }

}



public class RestResponseLogData
{
    public bool IsSuccess { get; set; }
    public short StatusCode { get; set; }
    public string? Response { get; set; } = null;
    public int PeriodTime { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime CompleteDate { get; set; }
    public int RetryAttempt { get; set; } = 0; // شماره تلاش
    public string? ExceptionType { get; set; } // نوع Exception در صورت وجود
    public string? ExcutedCurl { get; set; }
}