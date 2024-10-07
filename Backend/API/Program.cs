using Persistence;
using API.Middleware;
using Application.Queries;
using Application.Services;
using Application.Commands;
using Domain.Persistence.DTOs;
using Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Domain.Persistence.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Database
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

// LLM Service Options
var llmServiceUrl = Environment.GetEnvironmentVariable("LLM_SERVICE_URL");
if (!string.IsNullOrEmpty(llmServiceUrl))
    builder.Services.Configure<LLMServiceOptions>(options => options.LLM_SERVICE_URL = llmServiceUrl);
else
    builder.Services.Configure<LLMServiceOptions>(builder.Configuration.GetSection("LLMServiceOptions"));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SubmitQuery>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UploadFileCommand>());

// Validators

// Services
builder.Services.AddScoped<IExcelFileService, ExcelFileService>();

// Repositories
builder.Services.AddScoped<ILLMRepository, LLMRepository>();
builder.Services.AddScoped<IVectorDbRepository, VectorDbRepository>();
builder.Services.AddScoped<IWebRepository<float[]?>, WebRepository<float[]?>>();
builder.Services.AddScoped<IWebRepository<QueryAnswer>, WebRepository<QueryAnswer>>();

builder.Services.AddSwaggerGen();
var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}

// app.UseHttpsRedirection();
// app.UseAuthorization();
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");
app.UseMiddleware<ExceptionMiddleware>();

app.Run();