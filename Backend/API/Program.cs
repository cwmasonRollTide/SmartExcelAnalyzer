using Persistence;
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

[assembly: ExcludeFromCodeCoverage]

var builder = WebApplication.CreateBuilder(args);

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

builder.Services.AddLogging();
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();

// Database
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") ?? builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IVectorDbRepository, VectorDbRepository>();

// LLM Service Options
var llmServiceUrl = Environment.GetEnvironmentVariable("LLM_SERVICE_URL");
if (!string.IsNullOrEmpty(llmServiceUrl)) builder.Services.Configure<LLMServiceOptions>(options => options.LLM_SERVICE_URL = llmServiceUrl);
else builder.Services.ConfigureOptions(builder.Configuration.GetSection("LLMServiceOptions"));
builder.Services.AddScoped<ILLMRepository, LLMRepository>();

// MediatR
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SubmitQueryHandler>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UploadFileCommandHandler>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SubmitQuery>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UploadFileCommand>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<SubmitQueryValidator>());
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<UploadFileCommandValidator>());
// builder.Services.AddFluentValidation(cfg => cfg.RegisterValidatorsFromAssemblyContaining<SubmitQueryValidator>());
// builder.Services.AddFluentValidation(cfg => cfg.RegisterValidatorsFromAssemblyContaining<UploadFileCommandValidator>());

// Services
builder.Services.AddScoped<IExcelFileService, ExcelFileService>();

// Repositories
builder.Services.AddScoped<IWebRepository<float[]?>, WebRepository<float[]?>>();
builder.Services.AddScoped<IWebRepository<QueryAnswer>, WebRepository<QueryAnswer>>();

// Swagger
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

// Middleware
app.UseRouting();
app.MapControllers();
app.MapHealthChecks("/health");
app.UseMiddleware<ExceptionMiddleware>();
app.Run();