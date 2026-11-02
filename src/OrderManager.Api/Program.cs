using Microsoft.EntityFrameworkCore;
using OrderManager.Business.Services;
using OrderManager.Core.Ports;
using OrderManager.Data;
using OrderManager.Data.Adapters;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<OrderManagerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IOrderDataPort, EfCoreDataAdapter>();
builder.Services.AddScoped<IBoardDataPort, EfCoreDataAdapter>();
builder.Services.AddScoped<IComponentDataPort, EfCoreDataAdapter>();
builder.Services.AddScoped<IManufacturingService, ManufacturingService>();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddSingleton<IProductionLineExporter, SimulatedFileExportAdapter>();
}

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();