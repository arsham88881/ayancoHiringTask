using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Audit;

public class SaveEventInModel
{
    public Guid Guid { get; set; }
    public DateTime? StartDate { get; set; }
    public int? Duration { get; set; }
    public string? Source { get; set; }
    public bool? IsSuccess { get; set; }
    public string? IpAddress { get; set; }
    public int? StatusCode { get; set; }
    public string? InputData { get; set; }
    public string? OutputData { get; set; }
    public string? EventErrorMessage { get; set; }
    public string? Description { get; set; }
}