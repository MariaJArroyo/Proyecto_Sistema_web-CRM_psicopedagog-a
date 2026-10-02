using System.Text;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using MenteActiva.Api.Infraestructura;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(opciones =>
    {
        opciones.InvalidModelStateResponseFactory = contexto =>
        {
            var errores = contexto.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct()
                .ToList();

            var mensaje = errores.Count > 0 ? string.Join(" ", errores) : "Revise los datos enviados.";

            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { mensaje });
        };
    });

builder.Services.AddOpenApi();

builder.Services.AddScoped<IRepositorioUsuario, RepositorioUsuario>();
builder.Services.AddScoped<IGeneradorTokenAcceso, GeneradorTokenAcceso>();
builder.Services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
builder.Services.AddScoped<IServicioUsuarios, ServicioUsuarios>();

// ---------- Agenda ----------
builder.Services.AddOptions<OpcionesAgenda>()
    .Bind(builder.Configuration.GetSection(OpcionesAgenda.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IRelojNegocio, RelojNegocio>();
builder.Services.AddScoped<IRepositorioCita, RepositorioCita>();
builder.Services.AddScoped<IServicioCitas, ServicioCitas>();
builder.Services.AddHostedService<ServicioCompletarCitasVencidas>();

// ---------- Errores ----------
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddProblemDetails();

// El envio de correo se cambia desde la configuracion, sin tocar codigo.
// "Registro" escribe el enlace en la consola; "Gmail" lo manda de verdad.
if (string.Equals(builder.Configuration["Correo:Proveedor"], "Gmail", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IServicioCorreo, CorreoGmail>();
}
else
{
    builder.Services.AddScoped<IServicioCorreo, CorreoEnRegistro>();
}
builder.Services.AddSingleton<IPasswordHasher<UsuarioAutenticado>, PasswordHasher<UsuarioAutenticado>>();

var llaveToken = builder.Configuration["Jwt:Llave"]
    ?? throw new InvalidOperationException("Falta Jwt:Llave en la configuracion.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Emisor"],
            ValidAudience = builder.Configuration["Jwt:Audiencia"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(llaveToken)),
            // Por defecto son cinco minutos de tolerancia; con uno alcanza para
            // cubrir relojes desfasados sin alargar tanto la sesion vencida
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Acceso denegado por defecto: cualquier endpoint que no diga lo contrario
// exige usuario autenticado. Lo publico se marca con [AllowAnonymous].
builder.Services.AddAuthorization(opciones =>
{
    opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

