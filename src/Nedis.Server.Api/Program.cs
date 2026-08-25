using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nedis.Server.Common;
using Nedis.Server.Modules.Chat;
using Nedis.Server.Modules.Identity;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine($"ENV = {builder.Environment.EnvironmentName}");
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionString:Default is not defined");

// Modules
builder.Services.AddIdentityModule(builder.Configuration, connectionString);
builder.Services.AddChatModule(connectionString);

// Host-specific services
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ConnectionTicketStore>();

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = jwt["SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not defined");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var hubPaths = context.HttpContext.RequestServices.GetRequiredService<IOptions<HubPathOptions>>().Value;
                var isHubRequest = hubPaths.Prefixes.Any(prefix => context.HttpContext.Request.Path.StartsWithSegments(prefix));

                if (!isHubRequest) return Task.CompletedTask;

                var ticket = context.Request.Query["access_token"];
                if (string.IsNullOrEmpty(ticket)) return Task.CompletedTask;
                var tickets = context.HttpContext.RequestServices.GetRequiredService<ConnectionTicketStore>();
                if (tickets.TryConsume(ticket!, out var accessToken))
                    context.Token = accessToken;

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddValidation();

builder.Services.AddCors(options =>
{
    options.AddPolicy("client", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var app = builder.Build();

app.UseCors("client");
app.UseAuthentication();
app.UseAuthorization();

// Module routes
app.MapIdentityModule();
app.MapChatModule();

// Host-specific routes
app.MapPost("/api/connection-tickets", (HttpContext http, ConnectionTicketStore tickets) =>
{
    var authHeader = http.Request.Headers.Authorization.ToString();
    var accessToken = authHeader["Bearer ".Length..];

    var ticket = tickets.Issue(accessToken, TimeSpan.FromSeconds(30)); // token has already been validated. no checks needed.
    return Results.Ok(new { ticket });
})
.RequireAuthorization();

app.Run();
