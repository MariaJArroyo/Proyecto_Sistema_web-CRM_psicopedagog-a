using System.Text;
using MenteActiva.Api.Models;
using MenteActiva.Api.Repositories;
using MenteActiva.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<IRepositorioUsuario, RepositorioUsuario>();
builder.Services.AddScoped<IGeneradorTokenAcceso, GeneradorTokenAcceso>();
builder.Services.AddScoped<IServicioAutenticacion, ServicioAutenticacion>();
builder.Services.AddScoped<IServicioUsuarios, ServicioUsuarios>();

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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
