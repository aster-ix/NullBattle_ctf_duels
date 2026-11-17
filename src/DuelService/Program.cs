using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NullBattle.Duels.Clients;
using NullBattle.Duels.Data;
using NullBattle.Duels.Hubs;
using NullBattle.Duels.Messaging;
using NullBattle.Duels.Middleware;
using NullBattle.Duels.Repositories;
using NullBattle.Duels.Services;
using NullBattle.Duels.Workers;
using System.Text;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<DuelDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("DuelDb")).UseSnakeCaseNamingConvention());

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = token;
                }
                return Task.CompletedTask;
            }
        };
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement{[new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()});
});


builder.Services.AddSingleton<ServiceTokenProvider>();
builder.Services.AddHttpClient<TaskServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:TaskServiceUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<DuelRepository>();
builder.Services.AddScoped<DuelService>();
builder.Services.AddHostedService<DuelTimeoutWorker>();
builder.Services.AddSingleton<DuelEventPublisher>();

builder.Services.AddSignalR().AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseStaticFiles();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.MapHub<DuelHub>("/hubs/duel");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();