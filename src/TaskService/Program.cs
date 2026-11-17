using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NullBattle.TaskBank.Data;
using NullBattle.TaskBank.Middleware;
using NullBattle.TaskBank.Repositories;
using NullBattle.TaskBank.Services;
using NullBattle.TaskBank.Storage;

var builder = WebApplication.CreateBuilder(args);

// EASY, MEDIUM, HARD
builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDbContext<TaskDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("TaskDb")) .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<TaskRepository>();
builder.Services.AddSingleton<FlagHasher>();
builder.Services.AddSingleton<FileStorage>();


// Проверка JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) .AddJwtBearer(options =>
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
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement{[new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>() });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();