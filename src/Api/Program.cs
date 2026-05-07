using System.Text.Json;
using MultiAgentLab.Api;
using MultiAgentLab.Api.Application.Agents;
using MultiAgentLab.Api.Application.Supervisor;
using MultiAgentLab.Api.Infrastructure.LLM;
using MultiAgentLab.Api.Infrastructure.Logging;
using MultiAgentLab.Api.Infrastructure.Mocks;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.AddCors();

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "MultiAgentLab API", Version = "v1", Description = "POC Multiagente - Revision de Historias de Usuario" });
});
builder.Services.AddHttpClient();

builder.Services.AddSingleton<IExecutionLogger>(new JsonlExecutionLogger());
builder.Services.AddSingleton<IModelRouter, ModelRouter>();
builder.Services.AddSingleton<AgentSelectionRules>();
builder.Services.AddSingleton<ConflictResolver>();
builder.Services.AddSingleton<MockCaseLoader>();

builder.Services.AddSingleton<IReviewAgent, ClarityAgent>();
builder.Services.AddSingleton<IReviewAgent, QaAgent>();
builder.Services.AddSingleton<IReviewAgent, TechnicalAgent>();
builder.Services.AddSingleton<IReviewAgent, UxAgent>();
builder.Services.AddSingleton<IReviewAgent, ComplianceAgent>();

builder.Services.AddSingleton<ReviewSupervisor>();

var app = builder.Build();

app.UseRouting();
app.UseCors(policy =>
    policy.WithOrigins("http://localhost:4200", "http://localhost:4201")
          .AllowAnyHeader()
          .AllowAnyMethod());

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MultiAgentLab API v1");
    c.RoutePrefix = string.Empty;
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapReviewEndpoints();

app.Run();
