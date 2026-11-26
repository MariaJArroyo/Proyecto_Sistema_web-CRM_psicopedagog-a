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
            var mensajeLectura = MensajeErrorDeLectura(contexto.ModelState);

            if (mensajeLectura is not null)
            {
                return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(new { mensaje = mensajeLectura });
            }

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
// Bitacora de inicios de sesion (la escribe el login, la leen Usuarios)
builder.Services.AddScoped<IRepositorioAcceso, RepositorioAcceso>();
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
builder.Services.AddScoped<IRepositorioConfiguracion, RepositorioConfiguracion>();
builder.Services.AddScoped<IServicioConfiguracion, ServicioConfiguracion>();
builder.Services.AddScoped<IRepositorioPago, RepositorioPago>();
builder.Services.AddScoped<IServicioPagos, ServicioPagos>();

// ---------- Sesiones ----------
builder.Services.AddScoped<IRepositorioSesion, RepositorioSesion>();
builder.Services.AddScoped<IServicioSesiones, ServicioSesiones>();

// ---------- Estudiantes ----------
builder.Services.AddScoped<RepositorioEstudiante>();

// ---------- Errores ----------
builder.Services.AddScoped<IRepositorioLogError, RepositorioLogError>();
builder.Services.AddScoped<IServicioLogError, ServicioLogError>();
builder.Services.AddExceptionHandler<ManejadorExcepciones>();
builder.Services.AddProblemDetails();

// ---------- Reportes ----------
builder.Services.AddScoped<IRepositorioReporte, RepositorioReporte>();
builder.Services.AddScoped<IServicioReportes, ServicioReportes>();

// ---------- Portal de padres ----------
builder.Services.AddScoped<IRepositorioPortal, RepositorioPortal>();
builder.Services.AddScoped<IServicioPortal, ServicioPortal>();

// ---------- Solicitudes de contacto ----------
builder.Services.AddScoped<IRepositorioContadores, RepositorioContadores>();
builder.Services.AddScoped<IServicioContadores, ServicioContadores>();

builder.Services.AddScoped<IRepositorioEncargado, RepositorioEncargado>();
builder.Services.AddScoped<IServicioEncargado, ServicioEncargado>();

builder.Services.AddScoped<IRepositorioSolicitud, RepositorioSolicitud>();
builder.Services.AddScoped<IServicioSolicitudes, ServicioSolicitudes>();

// ---------- Materiales ----------
builder.Services.AddScoped<IRepositorioMaterial, RepositorioMaterial>();
builder.Services.AddScoped<IServicioMateriales, ServicioMateriales>();

builder.Services.AddOptions<OpcionesMateriales>()
    .Bind(builder.Configuration.GetSection(OpcionesMateriales.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<AlmacenMateriales>();
builder.Services.AddSingleton<IAlmacenMateriales>(sp => sp.GetRequiredService<AlmacenMateriales>());

// Pagos que reportan las familias desde el portal (comprobantes en Archivos/Comprobantes)
builder.Services.AddSingleton<AlmacenComprobantes>();
builder.Services.AddSingleton<IAlmacenComprobantes>(sp => sp.GetRequiredService<AlmacenComprobantes>());
builder.Services.AddScoped<IRepositorioReportePago, RepositorioReportePago>();
builder.Services.AddScoped<IServicioReportesPago, ServicioReportesPago>();

// Tareas que entregan las familias (archivos en Archivos/EntregasTareas)
// Fotos de los estudiantes (Archivos/FotosEstudiantes)
builder.Services.AddSingleton<AlmacenFotosEstudiante>();
builder.Services.AddSingleton<IAlmacenFotosEstudiante>(sp => sp.GetRequiredService<AlmacenFotosEstudiante>());
builder.Services.AddScoped<IRepositorioFotoEstudiante, RepositorioFotoEstudiante>();

builder.Services.AddSingleton<AlmacenEntregasTarea>();
builder.Services.AddSingleton<IAlmacenEntregasTarea>(sp => sp.GetRequiredService<AlmacenEntregasTarea>());
builder.Services.AddScoped<IRepositorioEntregaTarea, RepositorioEntregaTarea>();
builder.Services.AddScoped<IServicioEntregasTarea, ServicioEntregasTarea>();

// Documentos del estudiante (Archivos/Documentos): la ficha los sube, el portal ve los visibles
builder.Services.AddSingleton<AlmacenDocumentos>();
builder.Services.AddSingleton<IAlmacenDocumentos>(sp => sp.GetRequiredService<AlmacenDocumentos>());
builder.Services.AddScoped<IRepositorioDocumento, RepositorioDocumento>();
builder.Services.AddScoped<IServicioDocumentos, ServicioDocumentos>();

// Cliente propio para comprobar enlaces: sin redirecciones automaticas, sin
// proxy y conectando solo a direcciones publicas (ver VerificadorEnlaces)
builder.Services.AddHttpClient(VerificadorEnlaces.NombreCliente, cliente =>
    {
        cliente.Timeout = Timeout.InfiniteTimeSpan;
        cliente.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (compatible; VerificadorEnlaces/1.0)");
    })
    .ConfigurePrimaryHttpMessageHandler(VerificadorEnlaces.CrearManejador);
builder.Services.AddScoped<IVerificadorEnlaces, VerificadorEnlaces>();

// ---------- Tareas ----------
builder.Services.AddScoped<IRepositorioTarea, RepositorioTarea>();
builder.Services.AddScoped<IServicioTareas, ServicioTareas>();

// ---------- Comunicacion ----------
builder.Services.AddScoped<IRepositorioPlantilla, RepositorioPlantilla>();
builder.Services.AddScoped<IServicioPlantillas, ServicioPlantillas>();
builder.Services.AddScoped<IRepositorioComunicacion, RepositorioComunicacion>();
builder.Services.AddScoped<IServicioComunicacion, ServicioComunicacion>();

builder.Services.AddOptions<OpcionesNotificaciones>()
    .Bind(builder.Configuration.GetSection(OpcionesNotificaciones.Seccion))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddScoped<IServicioNotificaciones, ServicioNotificaciones>();
builder.Services.AddHostedService<ServicioEnviarNotificaciones>();

builder.Services.AddScoped<IRepositorioMensaje, RepositorioMensaje>();
builder.Services.AddScoped<IServicioMensajes, ServicioMensajes>();


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

Console.WriteLine(
    $"Longitud de la clave JWT: {Encoding.UTF8.GetByteCount(llaveToken)} bytes"
);

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

// La carpeta de los PDF no va en git; se crea aqui si falta
app.Services.GetRequiredService<AlmacenMateriales>().CrearCarpeta();
app.Services.GetRequiredService<AlmacenComprobantes>().CrearCarpeta();
app.Services.GetRequiredService<AlmacenEntregasTarea>().CrearCarpeta();
app.Services.GetRequiredService<AlmacenFotosEstudiante>().CrearCarpeta();
app.Services.GetRequiredService<AlmacenDocumentos>().CrearCarpeta();

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

// Los errores al leer el cuerpo los escribe el framework, en ingles, y llegarian
// tal cual al aviso de la pantalla. La llave de un error de JSON es su ruta ("$",
// "$.titulo"); el del formulario sale de Kestrel cuando corta un cuerpo grande
// enviado por partes, sin Content-Length (LimiteSubidaPdf solo ve el declarado).
static string? MensajeErrorDeLectura(Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary estado)
{
    if (estado.Keys.Any(llave => llave.StartsWith('$')))
    {
        return "Los datos enviados no tienen un formato v\u00e1lido.";
    }

    var mensajes = estado.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

    if (mensajes.Any(m => m.Contains("Request body too large", StringComparison.Ordinal)))
    {
        return "Lo enviado pasa del tama\u00f1o m\u00e1ximo permitido.";
    }

    if (mensajes.Any(m => m.StartsWith("Failed to read the request form", StringComparison.Ordinal)))
    {
        return "No se pudo leer el formulario enviado.";
    }

    if (mensajes.Any(m => m.StartsWith("A non-empty request body is required", StringComparison.Ordinal)))
    {
        return "La petici\u00f3n no trae datos.";
    }

    return null;
}
