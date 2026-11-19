using System.Text;
using Lunara.Api.Data;
using Lunara.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Registra la API, la base de datos y los servicios que se inyectan en los controladores.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(new CsvDataStore(
    Path.Combine(builder.Environment.ContentRootPath, "data")));
builder.Services.AddDbContext<LunaraDbContext>(options =>
    options.UseInMemoryDatabase("LunaraMemoryDb"));

builder.Services.AddScoped<RepairWorkflowService>();
builder.Services.AddScoped<JwtTokenService>();

var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "Falta Jwt:Secret en la configuración.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Falta Jwt:Issuer en la configuración.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Falta Jwt:Audience en la configuración.");

if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException(
        "Jwt:Secret debe tener al menos 32 bytes.");

// Valida JWT para proteger las rutas que requieran una sesión autenticada.
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userId, out var id))
                {
                    context.Fail("La sesión no identifica una cuenta válida.");
                    return;
                }

                var db = context.HttpContext.RequestServices
                    .GetRequiredService<LunaraDbContext>();
                var user = await db.AuthUsers.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
                var sessionKey = context.Principal?.FindFirst("sessionKey")?.Value;
                if (user is null || user.SessionKey != sessionKey)
                {
                    context.Fail("La sesión fue desactivada.");
                    return;
                }

                if (user.TechnicianId is int technicianId &&
                    !await db.Technicians.AsNoTracking()
                        .AnyAsync(x => x.Id == technicianId && x.Active))
                    context.Fail("La cuenta del técnico está desactivada.");
            }
        };
    });

builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ??
    [
        "http://localhost:5173",
        "http://127.0.0.1:5173"
    ];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// Habilita llamadas desde el servidor local de Vite y configura middleware de la aplicación.
app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LunaraDbContext>();
    db.DisableCsvPersistence();
    await scope.ServiceProvider.GetRequiredService<CsvDataStore>().LoadIntoAsync(db);

    // Los ejemplos se agregan solo cuando hacen falta; el estado final queda en CSV tras sembrar.
    await DemoSeeder.SeedAsync(db);

    var clientCsvPath = Path.Combine(
        app.Environment.ContentRootPath,
        "lunara_clientes_prueba.csv");

    await ClientDemoSeeder.SeedFromCsvAsync(db, clientCsvPath);

    var csvPath = Path.Combine(
        app.Environment.ContentRootPath,
        "lunara_cuentas_desarrollo.csv");

    await AuthSeeder.SeedFromCsvAsync(
        db,
        csvPath,
        app.Configuration["Admin:InitialPassword"] ??
            (app.Environment.IsDevelopment() ? "admin" : null),
        resetExistingAdminPassword: app.Environment.IsDevelopment());
    db.EnableCsvPersistence();
    await db.SaveCsvSnapshotAsync();
}

app.Run();
