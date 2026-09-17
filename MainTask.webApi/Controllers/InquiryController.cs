using Application.Dtos.Inquiry.VehicleInquiry;
using Application.Services;
using Application.Services.Inquiry;
using Domain.Attributes.Shared;
using Domain.Interfaces.Contexts;
using Microsoft.AspNetCore.Mvc;

namespace MainTask.webApi.Controllers;

[ApiController]
[Route("/api/inquiries")]
public class InquiryController(
    TrafficFineInquiry service,
    IEventManagerContext eventManager)
    : ControllerBase
{

    [HttpPost]
    [Idempotent(cacheTimeInMinutes: 5)]
    public async Task<IActionResult> PostAsync(InquiryReqDto model, CancellationToken ct)
    {
        var (warns, res) = await service.ExecuteAsync(model, ct);

        return ResponseHelper.Success(res, eventManager.EventGuid.ToString(), warns);
    }
}
