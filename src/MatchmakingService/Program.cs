using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NullBattle.Matchmaking.Clients;
using NullBattle.Matchmaking.Hubs;
using NullBattle.Matchmaking.Middleware;
using NullBattle.Matchmaking.Services;
using NullBattle.Matchmaking.Storage;
using StackExchange.Redis;
using NullBattle.Matchmaking.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSignalR().AddJsonProtocol(options =>options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!));
builder.Services.AddSingleton<QueueStore>();

builder.Services.AddSingleton<ServiceTokenProvider>();
builder.Services.AddHttpClient<RatingServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:RatingServiceUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddHttpClient<DuelServiceClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:DuelServiceUrl"]!);
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddSingleton<MatchNotifier>();
builder.Services.AddHostedService<MatchmakingWorker>();

builder.Services.AddScoped<MatchmakingService>();

var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
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
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<MatchmakingHub>("/hubs/matchmaking");

await app.Services.GetRequiredService<QueueStore>().ClearAsync();

app.Run();