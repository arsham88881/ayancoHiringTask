using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.ValueObjects.Shared;

public static class HttpStatusCodes
{
    // کدهای موفق
    public static readonly int[] Success = { 200, 201, 202, 204 };

    //  کدهای ریدایرکت
    public static readonly int[] Redirect = { 301, 302, 304 };

    // کدهای خطای کلاینت
    public static readonly int[] ClientError = {
        400, 401, 403, 404, 405, 409, 422, 423, 429
    };

    // کدهای خطای سرور
    public static readonly int[] ServerError = {
        500, 501, 502, 503, 504
    };

    // همه کدها
    public static readonly int[] All = Success
        .Concat(Redirect)
        .Concat(ClientError)
        .Concat(ServerError)
        .ToArray();

    public static bool IsSuccess(int statusCode) => Success.Contains(statusCode);
    public static bool IsRedirect(int statusCode) => Redirect.Contains(statusCode);
    public static bool IsClientError(int statusCode) => ClientError.Contains(statusCode);
    public static bool IsServerError(int statusCode) => ServerError.Contains(statusCode);
}