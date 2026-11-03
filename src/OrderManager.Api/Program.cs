using Microsoft.EntityFrameworkCore;
using OrderManager.Business.Services;
using OrderManager.Core.Ports;
using OrderManager.Data;
using OrderManager.Data.Adapters;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

app.MapGet("/error", (HttpContext ctx) =>
{
    Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature? ex =
        ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();

    return ex?.Error switch
    {
        KeyNotFoundException => Results.Problem(statusCode: StatusCodes.Status404NotFound),
        ArgumentException e => Results.Problem(detail: e.Message, statusCode: StatusCodes.Status400BadRequest),
        _ => Results.Problem(statusCode: StatusCodes.Status500InternalServerError),
    };
}).ExcludeFromDescription();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();