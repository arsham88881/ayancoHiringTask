using Application.Exceptions;
using Domain.Models.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Application.Services;


public static class ResponseHelper
{
    /// <summary>
    /// return EndpointResult with default successfully message 
    /// </summary>
    public static ActionResult Success(MessageItem[]? messages = null, string? trackingId = null) =>
        new ObjectResult(new ApiResponse<object?>() { TrackingId = trackingId, Messages = messages, StatusCode = StatusCodes.Status200OK })
        { StatusCode = StatusCodes.Status200OK };

    /// <summary>
    /// return EndpointResult with setting value and totalCount
    /// </summary>
    public static ActionResult Success<T>(T value, long recordCount, string? trackingId = null, MessageItem[]? messages = null) =>
        new ObjectResult(new ApiResponse<T>(value) { TrackingId = trackingId, Messages = messages, TotalCount = recordCount })
        { StatusCode = StatusCodes.Status200OK };
    /// <summary>
    /// return EndpointResult with setting value
    /// </summary>
    public static ActionResult Success<T>(T value, string? trackingId = null, MessageItem[]? messages = null) =>
       new ObjectResult(new ApiResponse<T>(value) { TrackingId = trackingId, Messages = messages })
       { StatusCode = StatusCodes.Status200OK };
    /// <summary>
    /// return CRolling with result  
    /// </summary>
    public static BusinessException Failure(ApiResponse result) =>
        new BusinessException(result);
    /// <summary>
    /// return CRolling with result  
    /// </summary>
    public static BusinessException Failure(int statusCode, MessageItem[]? messages = null) =>
        new BusinessException(new ApiResponse(statusCode, messages));
    /// <summary>
    /// return CRolling with result  
    /// </summary>
    public static BusinessException<object> Failure(object resoponse, int statusCode, MessageItem[]? messages = null) =>
        new BusinessException<object>(new ApiResponse<object>(resoponse, statusCode, messages));

}