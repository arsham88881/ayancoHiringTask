using Application.Dtos.Inquiry.FakeProvider;
using Microsoft.AspNetCore.Mvc;

namespace FakeProviders.webApi.Controllers;

[ApiController]
[Route("api/fake-providers")]
public class FakeProviderController : ControllerBase
{
    private readonly ILogger<FakeProviderController> logger;

    public FakeProviderController(ILogger<FakeProviderController> logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Provider 1 - پاسخ درست (موفق)
    /// </summary>
    [HttpPost("1/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider1Async(P1VehicleInquiryReqDto model)
    {
        logger.LogInformation("Request came to Fake Provider 1 with data: {@Model}", model);

        return Ok(new
        {
            success = true,
            provider = "Provider1",
            data = new
            {
                plateNumber = model.PlateNumber,
                totalAmount = 1250000,
                billId = "1234567890123",
                paymentId = "9876543210987",
                count = 3
            }
        });
    }

    /// <summary>
    /// Provider 2 - خطای بیزینسی (پلاک نامعتبر)
    /// </summary>
    [HttpPost("2/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider2Async(P2VehicleInquiryReqDto model)
    {
        logger.LogInformation("Request came to Fake Provider 2 with data: {@Model}", model);
        return BadRequest(new
        {
            success = false,
            provider = "Provider2",
            error = "INVALID_PLATE",
            message = "پلاک نامعتبر است"
        });
    }

    /// <summary>
    /// Provider 3 - خطای فنی مدیریت‌شده (Status 500 ولی پاسخ در فرمت درست)
    /// </summary>
    [HttpPost("3/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider3Async(P3VehicleInquiryReqDto model)
    {
        logger.LogInformation("Request came to Fake Provider 3 with data: {@Model}", model);

        return StatusCode(500, new
        {
            success = false,
            provider = "Provider3",
            error = "INTERNAL_SERVER_ERROR",
            message = "خطای فنی رخ داده است"
        });
    }

    /// <summary>
    /// Provider 4 - پاسخ بعد از ۱۰ ثانیه (برای تست Timeout)
    /// </summary>
    [HttpPost("4/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider4Async(P4VehicleInquiryReqDto model)
    {
        logger.LogInformation("Request came to Fake Provider 4 with data: {@Model}", model);
        await Task.Delay(10000); // ۱۰ ثانیه تأخیر

        return Ok(new
        {
            success = true,
            provider = "Provider4",
            data = new
            {
                plateNumber = model.PlateNumber,
                totalAmount = 780000,
                billId = "4444333322221",
                paymentId = "1111222233334",
                count = 2
            }
        });
    }

    /// <summary>
    /// Provider 5 - خطای ۵۰۰ مدیریت‌نشده (Exception پرتاب می‌کند و دیتا می‌ریزد بیرون)
    /// </summary>
    [HttpPost("5/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider5Async(P5VehicleInquiryReqDto model)
    {
        logger.LogInformation("Request came to Fake Provider 5 with data: {@Model}", model);
        // عمداً Exception پرتاب می‌کنیم تا خطای مدیریت‌نشده رخ بده
        throw new Exception("Unhandled exception from Provider5 - Data leaked!");
    }
}

