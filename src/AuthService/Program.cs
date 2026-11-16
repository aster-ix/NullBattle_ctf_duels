using Microsoft.EntityFrameworkCore;
using NullBattle.Auth.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<UserDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("UserDb"))
           .UseSnakeCaseNamingConvention());

var app = builder.Build();

app.MapControllers();

app.Run();