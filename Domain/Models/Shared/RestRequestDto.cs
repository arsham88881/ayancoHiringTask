using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Shared;

public record class RestRequestDto
{
    /// <summary>
    /// use it for pass paramenter in QueryString and Route Segment (For Parameter GET and DELETE only use this) 
    /// </summary>
    public object? SegmentParams { get; set; }
    /// <summary>
    /// add to quary string
    /// </summary>
    public object? QuaryParams { get; set; }
    /// <summary>
    /// only avalible in POST , PUT ,PATCH
    /// </summary>
    public object? BodyParams { get; set; }
    public string EndPoint { get; set; } = string.Empty;
    /// <summary>
    /// default set token to --header 'Authorization:{token} if you want use another set to headers custom 
    /// </summary>
    public string? Token { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
}