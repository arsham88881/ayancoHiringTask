using Domain.Attributes.Shared;
using Domain.Interfaces.Contexts;
using Domain.Interfaces.Repositories.Audit;
using Domain.Interfaces.Services;
using Domain.Models.Audit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Infrastructure.Security.Contexts;

public class EventManagerContext : IEventManagerContext
{
    private readonly ILogger<EventManagerContext> logger;
    private readonly IServiceScopeFactory scopeFactory;
    private readonly IExceptionLogService exceptionLogService;
    public EventManagerContext(ILogger<EventManagerContext> logger, IServiceScopeFactory scopeFactory, IExceptionLogService exceptionLogService)
    {
        this.logger = logger;
        this.scopeFactory = scopeFactory;
        this.exceptionLogService = exceptionLogService;
    }


    public Guid EventGuid { get; private set; }
    public string? Description { get; private set; }
    public Exception? InnerException { get; set; }

    private JsonSerializerOptions SerializationOptions { get; set; } = new JsonSerializerOptions()
    {
        TypeInfoResolver = new SkipNullJsonTypeInfoResolver(),
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public void GenerateEventGUID()
    {
        EventGuid = Guid.NewGuid();
    }

    public async Task SaveEventLog(SaveEventInModel eventParam)
    {
        using (var scope = scopeFactory.CreateScope())
        {

            try
            {
                ErrorLogModel? errorLogModel = new ErrorLogModel(this.InnerException?.GetType().FullName, this.InnerException?.StackTrace, this.InnerException?.Message);
                errorLogModel = (errorLogModel.InnerException == null && errorLogModel.ExceptionType == null && errorLogModel.StackTrace == null) ? null : errorLogModel;
                eventParam.EventErrorMessage = errorLogModel == null ? null : JsonSerializer.Serialize(errorLogModel);
                eventParam.Description = this.Description;

                eventParam.OutputData = string.IsNullOrEmpty(eventParam.OutputData) || eventParam.OutputData == "{}" ? null : eventParam.OutputData;

                var auditRepository = scope.ServiceProvider.GetRequiredService<IAuditRepository>();

                await auditRepository.SaveEventLog(eventParam);

            }
            catch (Exception ex)
            {
                await SaveBackstageLog(eventParam, ex);
            }
        }
    }
    public async Task SaveBackstageLog(SaveEventInModel eventParam, Exception? ex)
    {
        logger.LogError("save event With  EventID : {0}  feild and Save on Logs/Audit/NowDateFile ", this.EventGuid.ToString());

        var log = new ExceptionLogEntry
        {
            Exception = new ExceptionDetail
            {
                ExceptionType = ex!.GetType().FullName,
                StackTrace = ex.StackTrace ?? "No stack trace available",
                InnerException = (ex?.Message ?? string.Empty) + "inner exception is :" + ex?.InnerException?.Message ?? "No inner exception"
            },
            Object = eventParam,
            LoggedAt = DateTime.Now,
        };

        await exceptionLogService.LogExceptionAsync("Audit", log);
    }
    public void WithDescription(string description) => this.Description = description;
    public record ErrorLogModel(string? ExceptionType, string? StackTrace, string? InnerException);

}
