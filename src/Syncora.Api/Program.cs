using System.ComponentModel;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Syncora.Data;
using Syncora.Helpers;
using Syncora.Services;

var builder = WebApplication.CreateBuilder(args);

var webRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");
builder.WebHost.UseWebRoot(webRootPath);

builder.Services.AddDbContext<SyncoraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CalendarService>();
builder.Services.AddScoped<EventService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<MeetingService>();
builder.Services.AddScoped<MeetingSearchService>();
builder.Services.AddScoped<ShoppingListService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<SettingsService>();


var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero 
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new Syncora.Helpers.TimeSpanConverter());
    });


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Syncora API",
        Version = "v1",
        Description = "Syncora API — calendars, meetings, shopping lists"
    });

    c.MapType<TimeSpan>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "time",
        Example = new OpenApiString("10:00:00")
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token (without 'Bearer')"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath!, "avatars"));

app.UseStaticFiles();

app.UseCors("AllowFrontend");

app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<SyncoraDbContext>();
        await context.Database.MigrateAsync();
        await DbInitializer.InitializeAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "?????? ??? ????????????? ???? ??????.");
    }
}

app.Run();