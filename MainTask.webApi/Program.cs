using Application;
using Infrastructure;
using MainTask.webApi.Configures;
using MainTask.webApi.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers().AddConfigureApiBehavior();
builder.Services.AddSwaggerGen();
builder.Services.AddDiInfrastructure();
builder.Services.AddDiApplication();
builder.AddLoggingBuildConfigure();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddMemoryCache();

builder.Services.AddControllers(options =>
{
    options.Filters.Add<IdempotentFilter>();
});

var app = builder.Build();
app.AddLoggingRuntimeConfigure();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseMiddleware<EventManagerMiddleware>();
app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

app.Run();
