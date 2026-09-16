using Application.Services;
using Domain.Attributes.Shared;
using Domain.Models.Shared;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MainTask.webApi.Configures;

public static class ApiBehaviorConfigureExtensions
{
    public static void AddConfigureApiBehavior(this IMvcBuilder mvc)
    {

        //builder.Services.AddControllers()
        mvc.AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.TypeInfoResolver = new SkipNullJsonTypeInfoResolver();
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            // Add other options as needed
        }).ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var messages = context.ModelState
                      .Where(ms => ms.Value!.Errors.Count > 0)
                      .SelectMany(ms => ms.Value!.Errors
                          .Select(e => new MessageItem(
                              context: MessageItemContexts.Error,
                              Message: e.ErrorMessage,
                              Code: ms.Key
                          )))
                      .ToArray();

                var response = new ApiResponse()
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Messages = messages,
                };
                throw ResponseHelper.Failure(response);

            };
        });


    }
}
