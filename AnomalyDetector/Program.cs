using AnomalyDetector.Model;
using AnomalyDetector.Options;
using AnomalyDetector.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;
builder.Services.AddOptions();
builder.Services.Configure<AzureStorageOptions>(configuration.GetSection(AzureStorageOptions.Section));
builder.Services.Configure<AzureAnomalyDetectorOptions>(configuration.GetSection(AzureAnomalyDetectorOptions.Section));

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<VariableStoreContext>(options =>
                    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers();
builder.Services.AddScoped<IModelStorage, AzureStorage>();
builder.Services.AddScoped<IAnomalyDetector, AzureAnomalyDetector>();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new OpenApiInfo { Title = "Anomaly detector api", Version = "v1" }));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Anomaly detector api");
    c.RoutePrefix = string.Empty;
});

app.UseStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints => endpoints.MapControllers());

app.Run();
