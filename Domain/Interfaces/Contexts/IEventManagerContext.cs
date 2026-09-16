using Domain.Models.Audit;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Interfaces.Contexts;

public interface IEventManagerContext
{

    Guid EventGuid { get; }
    Exception? InnerException { get; set; }
    void WithDescription(string description);
    void GenerateEventGUID();
    Task SaveEventLog(SaveEventInModel eventParam);

}
