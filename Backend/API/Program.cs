using System.Text;
using API.Middleware;
using Application.Queries;
using Application.Services;
using Application.Commands;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Domain.Persistence.Configuration;
using Persistence.Database;
using MongoDB.Driver;
using Microsoft.OpenApi.Models;
using API.Properties;
using MediatR;
using FluentValidation;
using Persistence.Cache;

var builder = WebApplication.CreateBuilder(args);
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
builder.Logging.ClearProviders();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
builder.Logging.AddConsole();
builder.Services.AddLogging();
builder.Services.AddHttpClient("DefaultClient", client => 
{ 
    client.Timeout = TimeSpan.FromMinutes(5);
});
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Database
// var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? builder.Configuration.GetConnectionString("DefaultConnection");
// builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING") 
    ?? builder.Configuration.GetConnectionString("MongoDB");
var mongoDatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
    ?? builder.Configuration["MongoDB:DatabaseName"];
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoConnectionString));
builder.Services.AddScoped(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoDatabaseName);
});
builder.Services.AddScoped<IDatabaseWrapper, NoSqlDatabaseWrapper>();
builder.Services.AddScoped<IVectorDbRepository, VectorRepository>();
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1_000_000;
});
builder.Services.AddSingleton<IEmbeddingCache, MemoryCacheEmbeddingCache>();

// LLM Service Options
var llmServiceUrl = Environment.GetEnvironmentVariable("LLM_SERVICE_URL");
if (!string.IsNullOrEmpty(llmServiceUrl))
    builder.Services.Configure<LLMServiceOptions>(options => options.LLM_SERVICE_URL = llmServiceUrl);
else
    builder.Services.Configure<LLMServiceOptions>(builder.Configuration.GetSection("LLMServiceOptions"));
builder.Services.AddScoped<ILLMRepository, LLMRepository>();
builder.Services.AddOptions<LLMServiceOptions>()
    .Validate(options => !string.IsNullOrEmpty(options.LLM_SERVICE_URL), "LLM_SERVICE_URL must be set.");

// MediatR
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SubmitQueryHandler>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UploadFileCommandHandler>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SubmitQuery>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UploadFileCommand>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SubmitQueryValidator>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UploadFileCommandValidator>());

// Services
builder.Services.AddScoped<IExcelFileService, ExcelFileService>();
builder.Services.AddScoped(typeof(IWebRepository<>), typeof(WebRepository<>));

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Smart Excel File Analyzer API", Version = "v1" });
    c.OperationFilter<SwaggerFileOperationFilter>();
});

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
// app.UseHttpsRedirection();
// app.UseAuthorization();

// Middleware
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");
app.UseMiddleware<ExceptionMiddleware>();
app.Run();