using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderManager.Business.Services;
using OrderManager.Core.Ports;
using OrderManager.Data;
using OrderManager.Data.Adapters;
using Scalar.AspNetCore;
using Serilog;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddSerilog(new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger());

builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddSchemaTransformer((schema, context, ct) =>
    {
        if (context.JsonTypeInfo.Type == typeof(DateTimeOffset) ||
            context.JsonTypeInfo.Type == typeof(DateTimeOffset?))
        {
            schema.Format = "date-time";
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddDbContext<OrderManagerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<EfCoreDataAdapter>();
builder.Services.AddScoped<IOrderDataPort>(sp => sp.GetRequiredService<EfCoreDataAdapter>());
builder.Services.AddScoped<IBoardDataPort>(sp => sp.GetRequiredService<EfCoreDataAdapter>());
builder.Services.AddScoped<IComponentDataPort>(sp => sp.GetRequiredService<EfCoreDataAdapter>());
builder.Services.AddScoped<IManufacturingService, ManufacturingService>();

builder.Services.AddSingleton<IProductionLineExporter, SimulatedFileExportAdapter>();

WebApplication app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<OrderManagerDbContext>().Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler("/error");
app.UseStatusCodePages();

app.Map("/error", HandleException).ExcludeFromDescription();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Consider extracting to a class implementing IExceptionHandler for testability and DI logging support.
static IResult HandleException(HttpContext ctx)
{
    IExceptionHandlerFeature? ex = ctx.Features.Get<IExceptionHandlerFeature>();
    return ex?.Error switch
    {
        KeyNotFoundException => Results.Problem(statusCode: StatusCodes.Status404NotFound),
        ArgumentException e => Results.Problem(detail: e.Message, statusCode: StatusCodes.Status400BadRequest),
        InvalidOperationException e => Results.Problem(detail: e.Message, statusCode: StatusCodes.Status409Conflict),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
    };
}