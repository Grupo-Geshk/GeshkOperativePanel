using System.Text;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ControlPanelGeshk.Data;
using ControlPanelGeshk.Security;

// 0) Cargar .env ANTES de crear el builder (silencioso si falta)
try { Env.Load(); } catch { }

var builder = WebApplication.CreateBuilder(args);

// Logs a consola
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// ---- DB: SOLO usamos ConnectionStrings__Postgres del .env ----
var conn =
    builder.Configuration.GetConnectionString("Postgres") // <-- mapea ConnectionStrings__Postgres
    ?? builder.Configuration["ConnectionStrings__Postgres"];

if (string.IsNullOrWhiteSpace(conn))
    throw new InvalidOperationException("Falta ConnectionStrings__Postgres en el entorno/.env.");

builder.Services.AddDbContext<ApplicationDbContext>(opt => opt.UseNpgsql(conn));
builder.Services.AddHealthChecks().AddNpgSql(conn);

builder.Services.AddSingleton<IPasswordHasher, Argon2PasswordHasher>();
builder.Services.AddSingleton<ISecretCrypto, AesGcmSecretCrypto>();
builder.Services.AddSingleton<ICredentialUnlockService, CredentialUnlockService>();
builder.Services.AddResponseCompression();
builder.Services.AddControllers();

// Swagger con seguridad JWT
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ControlPanelGeshk API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Introduce tu token JWT aquí",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    c.AddSecurityDefinition(jwtScheme.Reference.Id, jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

// Auth (JWT) – acepta Jwt__* o Jwt:* en env
var key = builder.Configuration["Jwt:Key"] ?? builder.Configuration["Jwt__Key"];
var issuer = builder.Configuration["Jwt:Issuer"] ?? builder.Configuration["Jwt__Issuer"];
var audience = builder.Configuration["Jwt:Audience"] ?? builder.Configuration["Jwt__Audience"];
if (string.IsNullOrWhiteSpace(key))
    throw new InvalidOperationException("Falta Jwt__Key en el entorno/.env.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// CORS
var allowedOrigins =
    (builder.Configuration["Cors:AllowedOrigins"] ?? builder.Configuration["Cors__AllowedOrigins"])
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

builder.Services.AddCors(opt =>
{
    opt.AddPolicy("app", p =>
    {
        if (allowedOrigins.Length > 0)
            p.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        else
            p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// Rate limiting básico
builder.Services.AddRateLimiter(_ => _.AddFixedWindowLimiter("fixed", o =>
{
    o.Window = TimeSpan.FromMinutes(1);
    o.PermitLimit = 100;
    o.QueueLimit = 0;
}));

var app = builder.Build();

// Middleware
app.UseResponseCompression();

// Swagger disponible en Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ControlPanelGeshk API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("app");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

// Auto-migrar en arranque (también sirve en Railway)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

// Endpoint básico
app.MapGet("/", () => Results.Ok(new { ok = true, service = "ControlPanelGeshk API" }));

app.Run();
