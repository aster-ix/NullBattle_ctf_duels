using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using NullBattle.Auth.Data;
using NullBattle.Auth.Middleware;
using NullBattle.Auth.Repositories;
using NullBattle.Auth.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<UserDbContext>(options => options.UseNpgsql(builder.Configuration.GetConnectionString("UserDb")).UseSnakeCaseNamingConvention());

builder.Services.AddScoped<UserRepository>();
builder.Services.AddSingleton<PasswordHasher>();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenService>();

builder.Services.AddScoped<AuthService>();

// проверка jwt
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key)),
            RoleClaimType = "role"
        };
    });
builder.Services.AddAuthorization();

// swagger+authorize
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>() });
});

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();