using TickerScout.Backend;
using TickerScout.Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<QuoteOptions>(builder.Configuration.GetSection("Quote"));
builder.Services.AddSingleton<QuoteStore>();
builder.Services.AddHostedService<QuoteSimulatorService>();
builder.Services.AddSignalR();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("DevCors");

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapHub<QuoteHub>("/hubs/quotes");

app.Run();
