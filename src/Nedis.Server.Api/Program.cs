using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Nedis.Server.Modules.Identity;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionString:Defauld is not defined");

// Modules
builder.Services.AddIdentityModule(builder.Configuration, connectionString);

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
            ClockSkew = TimeSpan.FromSeconds(30), // допуск на рассинхрон часов (дефолт 5 минут — слишком щедро)
        };
    });

builder.Services.AddAuthorization();

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

app.MapIdentityModule();

app.Run();
