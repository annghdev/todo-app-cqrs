using ApiHost;
using ApiHost.Middlewares;
using JasperFx.Events.Projections;
using Marten;
using Scalar.AspNetCore;
using Serilog;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình Serilog thay thế default logger của ASP.NET Core
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig.ReadFrom.Configuration(context.Configuration);
});

builder.AddServiceDefaults();
var projectionLifecycle = builder.Environment.IsEnvironment("Testing")
    ? ProjectionLifecycle.Inline
    : ProjectionLifecycle.Async;

var martenRegistration = builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("tododb")!);

    // Tách schema theo từng module
    opts.Schema.For<TopicView>().DatabaseSchemaName("topic");
    opts.Schema.For<UserView>().DatabaseSchemaName("users");

    // Tách schema cho event store/projections của Marten
    opts.Events.DatabaseSchemaName = "events";

    // Full text search support
    opts.Schema.For<TopicView>().FullTextIndex(x => x.Title);
    opts.Schema.For<UserView>().FullTextIndex(x => x.FullName);

    opts.Projections.Add<TopicProjection>(projectionLifecycle);
    opts.Projections.Add<UserProjection>(projectionLifecycle);
})
    .IntegrateWithWolverine(x =>
    {
        x.MessageStorageSchemaName = "wolverine";
        x.TransportSchemaName = "wolverine";
    });

if (!builder.Environment.IsEnvironment("Testing"))
{
    martenRegistration.AddAsyncDaemon(JasperFx.Events.Daemon.DaemonMode.HotCold);
}

builder.Services.AddWolverineHttp();

builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
    opts.Policies.AutoApplyTransactions();
    //opts.Policies.AddMiddleware<LoggingMiddleware>();
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Serilog HTTP request logging — ghi log mỗi HTTP request với StatusCode, Elapsed, Path
app.UseSerilogRequestLogging();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/", () => Results.Redirect("scalar/v1"));

app.MapWolverineEndpoints(opts =>
{
    opts.UseFluentValidationProblemDetailMiddleware();
});

app.Run();


public partial class Program
{
}