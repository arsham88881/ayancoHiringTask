using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Shared;


public record class RestAdvanceOptions
{
    public RestAdvanceOptions() { }
    public RestAdvanceOptions(bool disableSsl = false)
    {
        DisableSsl = disableSsl;
    }

    /// <summary>
    /// disable ssl connection 
    /// </summary>
    public bool DisableSsl { get; set; } = false;
    /// <summary>
    /// timout unit is secound default = 500 
    /// </summary>
    public int TimeoutConfig { get; set; } = 500;
    ///// <summary>
    ///// default on error only
    ///// </summary>
    //public ExcutionLogConfig LogConfig { get; set; } = ExcutionLogConfig.OnlyErrors;
    public string? ResponseRefTitle { get; set; }
    /// <summary>
    /// default (attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt))) =||> result _> 2 ^ retryCount => ex: 2 , 4 , 8 , ...
    /// </summary>
    public Func<int, TimeSpan> SleepDurationRetryFunc { get; set; } = attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt));
    /// <summary>
    /// default null if set it active polly Retry
    /// </summary>
    public int? RetryCount { get; set; }
    /// <summary>
    /// when want setting custom achive method for error (when work  isExceptionHandledManual == true)
    /// </summary>
    public void SetAchiveErrorFunc<RESPONSE>(Func<RESPONSE, string> func) => errorFormatterFunc = func;
    private Delegate? errorFormatterFunc;
    public string FormattedErrorMessage<RESPONSE>(RESPONSE input)
    {
        if (errorFormatterFunc is Func<RESPONSE, string> typedFunc)
            return typedFunc(input);

        throw new InvalidOperationException("No error function registered for this type. FormattedErrorMessage in RestWebService");
    }

}
