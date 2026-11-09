using System.Text.Json.Serialization;
using Kairos.Api.Errors;
using Kairos.Api.Realtime;
using Kairos.Application;
using Kairos.Application.Abstractions;
using Kairos.Application.Replay;
using Kairos.Infrastructure;
using Kairos.Infrastructure.Persistence;
using Serilog;

const string ImportOnlyArgument = "--import-only";

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services
    .AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHealthChecks().AddDbContextCheck<KairosDbContext>("database", tags: ["ready"]);

builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<IMarketBroadcaster, SignalRBroadcaster>();
builder.Services.AddSingleton<ITradeNotifier, SignalRTradeNotifier>();
builder.Services.AddSingleton<ICurrentActor, DemoActor>();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync(app.Lifetime.ApplicationStopping);

if (args.Contains(ImportOnlyArgument))
{
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseSerilogRequestLogging();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHub<MarketHub>("/hubs/market");

// Liveness only says the process answers. Readiness also checks the database.
app.MapHealthChecks("/health", new() { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });

await app.RunAsync();

// Makes the entry point visible to the integration tests.
public partial class Program;
