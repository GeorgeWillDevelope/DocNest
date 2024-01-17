using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Service;
using webapi.Utils.ConfigOptions;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<DocNestDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.Configure<GCSConfigOptions>(builder.Configuration);
builder.Services.AddSingleton<ICloudStorageService, CloudStorageService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(options.DefaultPolicyName,
                        policy => policy.AllowAnyOrigin()
                                .AllowAnyHeader()
                                .AllowAnyMethod());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapControllers();

app.Run();
