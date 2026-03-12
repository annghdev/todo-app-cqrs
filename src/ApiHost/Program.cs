using ApiHost;
using JasperFx.Events.Projections;
using Marten;
using Scalar.AspNetCore;
using Wolverine;
using Wolverine.FluentValidation;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;
using Wolverine.Marten;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
var projectionLifecycle = builder.Environment.IsEnvironment("Testing")
    ? ProjectionLifecycle.Inline
    : ProjectionLifecycle.Async;

var martenRegistration = builder.Services.AddMarten(opts =>
{
    opts.Connection(builder.Configuration.GetConnectionString("tododb")!);

    // Full text search support
    opts.Schema.For<TopicView>().FullTextIndex(x => x.Title);

    opts.Schema.For<UserView>().NgramIndex(x => x.FullName);

    // Hỗ trợ xử lý tiếng Việt không dấu (Unaccent)
    opts.Advanced.UseNGramSearchWithUnaccent = true;

    opts.Projections.Add<TopicProjection>(projectionLifecycle);
    opts.Projections.Add<UserProjection>(projectionLifecycle);
})
    .IntegrateWithWolverine();

if (!builder.Environment.IsEnvironment("Testing"))
{
    martenRegistration.AddAsyncDaemon(JasperFx.Events.Daemon.DaemonMode.HotCold);
}

builder.Services.AddWolverineHttp();

builder.Host.UseWolverine(opts =>
{
    opts.UseFluentValidation();
    opts.Policies.AutoApplyTransactions();
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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