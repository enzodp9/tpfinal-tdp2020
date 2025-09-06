using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Reflection;
using System.Text;
using TPFinal.Api.Application;
using TPFinal.Api.Infrastructure;

// Cargar variables de entorno desde .env
Env.Load();

var builder = WebApplication.CreateBuilder(args);

// -------------------- Services --------------------
builder.Services.AddControllers();

// Swagger (Swashbuckle)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TPFinal API",
        Version = "v1",
        Description = "API para gestión de películas/series"
    });

    // (Opcional) Soporte de autorización por JWT en Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Bearer. Usar: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference {
                Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
    var xml = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xml);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

// JWT
var jwt = builder.Configuration.GetSection("Jwt");
var jwtKey = jwt["Key"] ?? "dev-key-change-me";
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = signingKey
        };
    });

builder.Services.AddAuthorization();

// HttpClient para OMDb
builder.Services.AddHttpClient<IOmdbClient, OmdbClient>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IUserService, UserService>();


// (Opcional) CORS para tu front en Vite (5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("client", p =>
        p.WithOrigins("http://localhost:5173")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// DbContext con SQLite
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(builder.Configuration.GetConnectionString("Default")));

//DbContext con SQL Server
//builder.Services.AddDbContext<AppDbContext>(opts =>
//    opts.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();
app.UseStaticFiles(); // sirve wwwroot/*

// -------------------- Pipeline --------------------
// Swagger siempre en dev; si querés, podés condicionarlo por entorno
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TPFinal API v1");
});

// Si no configuraste https en launchSettings, podés omitir esto para evitar warnings.
// app.UseHttpsRedirection();

app.UseCors("client");

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint de prueba rápido
app.MapGet("/ping", () => "pong");

app.Run();
