using Domain.Models.Shared;
using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace Domain.Interfaces.Contexts;


public interface IHttpIntegrationContext
{
    Task<RestResult<OUTPUT>> GetAsync<OUTPUT>(
        string baseUrlAddress,
        RestRequestDto request,
        RestAdvanceOptions? options,
        CancellationToken cancellationToken = default);

    Task<RestResult<OUTPUT>> PostAsync<OUTPUT>(
      string baseUrlAddress,
      RestRequestDto request,
      RestAdvanceOptions? options,
      CancellationToken cancellationToken = default);

    /// ///////////////////////////////////////////////////////////////////////
    
    Task<RestResult> GetAsync(
      string baseUrlAddress,
      RestRequestDto request,
      RestAdvanceOptions? options,
      CancellationToken cancellationToken = default);

    Task<RestResult> PostAsync(
      string baseUrlAddress,
      RestRequestDto request,
      RestAdvanceOptions? options,
      CancellationToken cancellationToken = default);
}
