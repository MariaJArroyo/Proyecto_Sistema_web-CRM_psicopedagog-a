using MenteActiva.Seguridad;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(opciones =>
{
    // Todo POST tiene que traer su token antifalsificacion. Se pone aqui y no
    // accion por accion para que no dependa de que alguien se acuerde.
    opciones.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ManejadorTokenApi>();

// El cliente sin nombre es el que usan los controladores con CreateClient(),
// asi que el manejador del token queda puesto para todas las llamadas al API.
builder.Services.AddHttpClient(string.Empty)
    .AddHttpMessageHandler<ManejadorTokenApi>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Cuenta/Login";
        opciones.LogoutPath = "/Cuenta/CerrarSesion";
        opciones.AccessDeniedPath = "/Cuenta/AccesoDenegado";
        opciones.ReturnUrlParameter = "urlRetorno";
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        // Sin renovacion por uso: la cookie vence cuando vence el token del API
        // que lleva dentro. Si se renovara sola, quedaria una sesion viva con un
        // token muerto.
        opciones.SlidingExpiration = false;
        opciones.Cookie.Name = "MenteActiva.Sesion";
        // HttpOnly: un script inyectado no puede leer la sesion
        opciones.Cookie.HttpOnly = true;
        // Strict: la cookie no viaja en peticiones que vengan de otro sitio
        opciones.Cookie.SameSite = SameSiteMode.Strict;
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// Acceso denegado por defecto, igual que en el API: un controlador nuevo nace
// protegido aunque a nadie se le ocurra ponerle el atributo. Lo publico se
// marca a mano con [AllowAnonymous].
builder.Services.AddAuthorization(opciones =>
{
    opciones.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Las pantallas con sesion no se guardan en el cache del navegador. Sin esto,
// despues de cerrar sesion el boton "atras" vuelve a mostrar la pagina.
app.Use(async (contexto, siguiente) =>
{
    if (contexto.User.Identity?.IsAuthenticated == true)
    {
        contexto.Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        contexto.Response.Headers.Pragma = "no-cache";
    }

    await siguiente();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
