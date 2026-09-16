using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using ActionResult = Microsoft.AspNetCore.Mvc.ActionResult;
using Domain.Attributes.Shared;

namespace Domain.Models.Shared;


public record class ApiResponse<OUTPUT>
{
    [JsonSkipNull]
    public string? TrackingId { get; set; }
    public int StatusCode { get; set; }
    [JsonSkipNull]
    public long? TotalCount { get; set; }
    [JsonSkipNull]
    public MessageItem[]? Messages { get; set; }
    [JsonSkipNull]
    public OUTPUT? Value { get; set; }

    // سازنده پیش‌فرض
    public ApiResponse()
    {
        StatusCode = StatusCodes.Status200OK;
    }

    // سازنده فقط با Value
    public ApiResponse(OUTPUT? value)
    {
        Value = value;
        StatusCode = StatusCodes.Status200OK;
    }

    // سازنده با پیام‌ها (برای حالت خطا)
    public ApiResponse(MessageItem[] messages)
    {
        StatusCode = StatusCodes.Status400BadRequest;
        Messages = messages;
    }

    // سازنده کامل
    public ApiResponse(OUTPUT? value, int? statusCode = null, MessageItem[]? messages = null, long? totalCount = null)
    {
        Value = value;
        Messages = messages;
        StatusCode = statusCode ?? StatusCodes.Status200OK;
        TotalCount = totalCount;
    }

    // سازنده فقط با StatusCode و Messages
    public ApiResponse(int statusCode, MessageItem[]? messages = null)
    {
        StatusCode = statusCode;
        Messages = messages;
    }

    // تبدیل به ActionResult
    public static implicit operator ActionResult(ApiResponse<OUTPUT> response)
    {
        return new ObjectResult(response) { StatusCode = response.StatusCode };
    }
}

public record class ApiResponse
{
    [JsonSkipNull]
    public string? TrackingId { get; set; } //شناسه برای پیگیری خطا ها ایونت ها
    public int StatusCode { get; set; }
    [JsonSkipNull]
    public MessageItem[]? Messages { get; set; }
    public ApiResponse()
    {
        StatusCode = StatusCodes.Status200OK;
    }
    public ApiResponse(int statusCode, MessageItem[]? messages = null)
    {
        StatusCode = statusCode;
        Messages = messages;
    }
    public ApiResponse(MessageItem[] messages)
    {
        StatusCode = StatusCodes.Status400BadRequest; //for pupUp handeling 
        Messages = messages;
    }

    public static implicit operator ActionResult(ApiResponse response)
    {
        return new ObjectResult(response) { StatusCode = response.StatusCode };
    }


    public static implicit operator ApiResponse<object?>(ApiResponse response) =>
        new ApiResponse<object?> { Messages = response.Messages, TrackingId = response.TrackingId, StatusCode = response.StatusCode };

}