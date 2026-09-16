using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Models.Shared;

public record class MessageItem
{

    public MessageItem() { }
    public MessageItem(MessageItemContexts context, string Message, string Code)
    {
        this.Context = context;
        this.Message = Message;
        this.Code = Code;
    }

    public MessageItemContexts? Context { get; set; }
    public string? Message { get; set; }
    public string? Code { get; set; }

}

public enum MessageItemContexts
{
    Warning,
    Error,
    Info
}


