using Microsoft.AspNetCore.Mvc;

namespace FakeProviders.webApi.Controllers;

[ApiController]
[Route("api/fake-providers")]
public class FakeProviderController : ControllerBase
{
    /// <summary>
    /// Provider 1 - استعلام تجمیعی (فقط مبلغ کل)
    /// </summary>
    [HttpPost("1/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider1Async([FromBody] VehicleInquiryRequest request)
    {
        // Fake response
        return Ok(new
        {
            success = true,
            provider = "Provider1",
            data = new
            {
                plateNumber = request.PlateNumber,
                totalAmount = 1250000,
                billId = "1234567890123",
                paymentId = "9876543210987",
                count = 3
            }
        });
    }

    /// <summary>
    /// Provider 2 - استعلام ریز خلافی (با جزئیات)
    /// </summary>
    [HttpPost("2/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider2Async([FromBody] VehicleInquiryRequest request)
    {
        return Ok(new
        {
            success = true,
            provider = "Provider2",
            data = new
            {
                plateNumber = request.PlateNumber,
                totalAmount = 1850000,
                count = 2,
                violations = new[]
                {
                    new
                    {
                        id = "A9F3C21B",
                        type = "توقف دوبله در معابر",
                        description = "الصاقی",
                        code = "2085",
                        price = 600000,
                        city = "تهران",
                        location = "تهران، خیابان ولیعصر",
                        datetime = "1403/05/12 14:35"
                    },
                    new
                    {
                        id = "B7D2E45F",
                        type = "تجاوز از سرعت مجاز",
                        description = "دوربینی",
                        code = "2002",
                        price = 1250000,
                        city = "کرج",
                        location = "آزادراه تهران-کرج",
                        datetime = "1403/06/03 09:12"
                    }
                }
            }
        });
    }

    /// <summary>
    /// Provider 3 - استعلام با عکس (شبیه‌سازی شده)
    /// </summary>
    [HttpPost("3/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider3Async([FromBody] VehicleInquiryRequest request)
    {
        return Ok(new
        {
            success = true,
            provider = "Provider3",
            data = new
            {
                plateNumber = request.PlateNumber,
                totalAmount = 950000,
                count = 1,
                violations = new[]
                {
                    new
                    {
                        id = "C1D8E92A",
                        type = "عبور از چراغ قرمز",
                        description = "دوربینی",
                        code = "2004",
                        price = 950000,
                        city = "اصفهان",
                        location = "چهارراه سی‌وسه‌پل",
                        datetime = "1403/07/01 18:22",
                        imageUrl = "https://picsum.photos/800/600?random=1"
                    }
                }
            }
        });
    }

    /// <summary>
    /// Provider 4 - استعلام ساده + وضعیت شکایت
    /// </summary>
    [HttpPost("4/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider4Async([FromBody] VehicleInquiryRequest request)
    {
        return Ok(new
        {
            success = true,
            provider = "Provider4",
            data = new
            {
                plateNumber = request.PlateNumber,
                totalAmount = 0,
                billId = null,
                paymentId = null,
                count = 0,
                complaintCode = "CMP-2024-001",
                complaintStatus = "در حال بررسی"
            }
        });
    }

    /// <summary>
    /// Provider 5 - استعلام با خطای تصادفی (برای تست سناریوهای خطا)
    /// </summary>
    [HttpPost("5/vehicle-violations/inquiry")]
    public async Task<IActionResult> Provider5Async([FromBody] VehicleInquiryRequest request)
    {
        // برای تست می‌تونی گاهی خطا برگردونی
        var random = new Random().Next(1, 10);

        if (random <= 3)
        {
            return BadRequest(new
            {
                success = false,
                provider = "Provider5",
                error = "PLATE_NOT_FOUND",
                message = "پلاک مورد نظر یافت نشد"
            });
        }

        return Ok(new
        {
            success = true,
            provider = "Provider5",
            data = new
            {
                plateNumber = request.PlateNumber,
                totalAmount = 450000,
                billId = "5555555555555",
                paymentId = "4444444444444",
                count = 1
            }
        });
    }
}