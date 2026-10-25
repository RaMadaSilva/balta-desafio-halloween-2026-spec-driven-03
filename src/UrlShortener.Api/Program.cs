using Microsoft.AspNetCore.HttpOverrides;
using Scalar.AspNetCore;
using UrlShortener.Api.Features;
using UrlShortener.Api.Shared;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCreationRateLimiting();
builder.Services.AddSingleton(provider => new SqliteConnectionFactory(
    provider.GetRequiredService<IConfiguration>(), builder.Environment.ContentRootPath));
builder.Services.Configure<RouteHandlerOptions>(options => options.ThrowOnBadRequest = false);

var app = builder.Build();
DatabaseInitializer.Initialize(app.Services.GetRequiredService<SqliteConnectionFactory>());
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    ForwardLimit = 1
});
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.MapCreateLink();
app.MapRedirectLink();
app.MapGetLinkStats();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs/scalar");
}
app.Run();

public partial class Program;
