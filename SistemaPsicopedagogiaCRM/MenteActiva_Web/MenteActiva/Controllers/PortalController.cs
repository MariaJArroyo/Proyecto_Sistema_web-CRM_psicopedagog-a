using MenteActiva.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace MenteActiva.Controllers;

[Authorize(Roles = "Encargado")]
public class PortalController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public PortalController(IHttpClientFactory http,IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    #region Inicio

    public async Task<IActionResult> Inicio(
        int? idEstudiante,
        CancellationToken cancelacion)
    {
        var modelo = new PortalInicioViewModel();

        var ruta = "portal/inicio";

        if (idEstudiante.HasValue)
        {
            ruta += $"?idEstudiante={idEstudiante.Value}";
        }

        try
        {
            using var client = _http.CreateClient();

            using var respuesta = await client.GetAsync(
                UrlApi(ruta),
                cancelacion);

            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                ViewBag.ErrorCarga =
                    "No se pudo validar su sesión con el servicio. " +
                    "Cierre sesión y vuelva a ingresar.";
            }
            else if (respuesta.StatusCode == HttpStatusCode.Forbidden)
            {
                return Forbid();
            }
            else if (!respuesta.IsSuccessStatusCode)
            {
                ViewBag.ErrorCarga =
                    "No se pudo cargar el portal. Verifique que tenga " +
                    "estudiantes activos vinculados a su cuenta.";
            }
            else
            {
                modelo = await respuesta.Content
                    .ReadFromJsonAsync<PortalInicioViewModel>(
                        cancellationToken: cancelacion)
                    ?? modelo;
            }
        }
        catch (HttpRequestException)
        {
            ViewBag.ErrorCarga =
                "No se pudo conectar con el servicio del portal.";
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            ViewBag.ErrorCarga =
                "El servicio tardó demasiado en responder. " +
                "Intente nuevamente.";
        }
        catch (JsonException)
        {
            ViewBag.ErrorCarga =
                "No se pudo interpretar la información del portal.";
        }

        return View(modelo);
    }

    #endregion


    #region Citas

    public async Task<IActionResult> Citas(int? idEstudiante, string? filtro, CancellationToken cancelacion)
    {
        var modelo = new PortalCitasViewModel
        {
            Filtro = string.IsNullOrWhiteSpace(filtro)
                ? "proximas"
                : filtro.Trim().ToLowerInvariant()
        };

        try
        {
            using var client = _http.CreateClient();

            // Obtiene los estudiantes del encargado y valida la selección.
            var rutaInicio = "portal/inicio";

            if (idEstudiante.HasValue)
            {
                rutaInicio +=
                    $"?idEstudiante={idEstudiante.Value}";
            }

            var inicio = await client
                .GetFromJsonAsync<PortalInicioViewModel>( UrlApi(rutaInicio), cancelacion);

            if (inicio is null || inicio.Resumen.IdEstudiante <= 0)
            {
                ViewBag.ErrorCarga =
                    "No se pudo obtener el estudiante del portal.";

                return View(modelo);
            }

            modelo.Estudiantes = inicio.Estudiantes;
            modelo.IdEstudiante = inicio.Resumen.IdEstudiante;

            // Consulta las citas del estudiante seleccionado.
            var rutaCitas =
                $"portal/citas?idEstudiante={modelo.IdEstudiante}" +
                $"&filtro={Uri.EscapeDataString(modelo.Filtro)}";

            modelo.Citas = await client
                .GetFromJsonAsync<List<PortalCitaViewModel>>(UrlApi(rutaCitas),cancelacion)
                ?? [];
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            ViewBag.ErrorCarga =
                "No se pudo validar su sesión con el servicio. " +
                "Cierre sesión y vuelva a ingresar.";
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException)
        {
            ViewBag.ErrorCarga =
                "No se pudieron cargar las citas. Verifique la conexión " +
                "y que el estudiante y el filtro sean válidos.";
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            ViewBag.ErrorCarga =
                "El servicio tardó demasiado en responder. " +
                "Intente nuevamente.";
        }
        catch (JsonException)
        {
            ViewBag.ErrorCarga =
                "No se pudo interpretar la información de las citas.";
        }

        return View(modelo);
    }

    #endregion


    #region Progreso

    public async Task<IActionResult> Progreso( int? idEstudiante, string? filtroReportes, CancellationToken cancelacion)
    {
        var modelo = new PortalProgresoViewModel
        {
            FiltroReportes = string.IsNullOrWhiteSpace(filtroReportes)
                ? "todos"
                : filtroReportes.Trim().ToLowerInvariant()
        };

        try
        {
            using var client = _http.CreateClient();

            // Obtiene los estudiantes y valida el seleccionado.
            var rutaInicio = "portal/inicio";

            if (idEstudiante.HasValue)
            {
                rutaInicio +=
                    $"?idEstudiante={idEstudiante.Value}";
            }

            var inicio = await client
                .GetFromJsonAsync<PortalInicioViewModel>( UrlApi(rutaInicio),cancelacion);

            if (inicio is null || inicio.Resumen.IdEstudiante <= 0)
            {
                ViewBag.ErrorCarga =
                    "No se pudo obtener el estudiante del portal.";

                return View(modelo);
            }

            modelo.Estudiantes = inicio.Estudiantes;
            modelo.IdEstudiante = inicio.Resumen.IdEstudiante;

            // Consulta sesiones y reportes del estudiante.
            var rutaProgreso =
                $"portal/progreso?idEstudiante={modelo.IdEstudiante}" +
                $"&filtroReportes={Uri.EscapeDataString(modelo.FiltroReportes)}";

            var progreso = await client.GetFromJsonAsync<PortalProgresoViewModel>( UrlApi(rutaProgreso),cancelacion);

            if (progreso is null)
            {
                ViewBag.ErrorCarga ="No se pudo obtener la información de progreso.";

                return View(modelo);
            }

            modelo.Sesiones = progreso.Sesiones;
            modelo.Reportes = progreso.Reportes;
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            ViewBag.ErrorCarga =
                "No se pudo validar su sesión con el servicio. " + "Cierre sesión y vuelva a ingresar.";
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException)
        {
            ViewBag.ErrorCarga =
                "No se pudo cargar el progreso. Verifique la conexión " + "y que el estudiante y el filtro sean válidos.";
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            ViewBag.ErrorCarga =
                "El servicio tardó demasiado en responder. " + "Intente nuevamente.";
        }
        catch (JsonException)
        {
            ViewBag.ErrorCarga =
                "No se pudo interpretar la información de progreso.";
        }

        return View(modelo);
    }

    #endregion

    #region Tareas

    public async Task<IActionResult> Tareas( int? idEstudiante, string? filtro, CancellationToken cancelacion)
    {
        var modelo = new PortalTareasViewModel
        {
            Filtro = string.IsNullOrWhiteSpace(filtro)
                ? "pendientes"
                : filtro.Trim().ToLowerInvariant()
        };

        try
        {
            using var client = _http.CreateClient();

            // Obtiene los estudiantes y valida la selección.
            var rutaInicio = "portal/inicio";

            if (idEstudiante.HasValue)
            {
                rutaInicio +=
                    $"?idEstudiante={idEstudiante.Value}";
            }

            var inicio = await client.GetFromJsonAsync<PortalInicioViewModel>(UrlApi(rutaInicio), cancelacion);

            if (inicio is null || inicio.Resumen.IdEstudiante <= 0)
            {
                ViewBag.ErrorCarga ="No se pudo obtener el estudiante del portal.";

                return View(modelo);
            }

            modelo.Estudiantes = inicio.Estudiantes;
            modelo.IdEstudiante = inicio.Resumen.IdEstudiante;

            // Consulta las tareas con el filtro seleccionado.
            var rutaTareas =
                $"portal/tareas?idEstudiante={modelo.IdEstudiante}" +
                $"&filtro={Uri.EscapeDataString(modelo.Filtro)}";

            modelo.Tareas = await client
                .GetFromJsonAsync<List<PortalTareaViewModel>>(UrlApi(rutaTareas),cancelacion)
                ?? [];
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            ViewBag.ErrorCarga =
                "No se pudo validar su sesión con el servicio. " + "Cierre sesión y vuelva a ingresar.";
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException)
        {
            ViewBag.ErrorCarga =
                "No se pudieron cargar las tareas. Verifique la conexión " + "y que el estudiante y el filtro sean válidos.";
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            ViewBag.ErrorCarga =
                "El servicio tardó demasiado en responder. " + "Intente nuevamente.";
        }
        catch (JsonException)
        {
            ViewBag.ErrorCarga = "No se pudo interpretar la información de las tareas.";
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> DescargarTarea(int idEstudiante,int idTareaAsignacion,CancellationToken cancelacion)
    {
        if (idEstudiante <= 0 || idTareaAsignacion <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Seleccione un estudiante y una tarea válidos."
            });
        }

        try
        {
            using var client = _http.CreateClient();

            var ruta =
                $"portal/tareas/{idTareaAsignacion}/archivo" +
                $"?idEstudiante={idEstudiante}";

            using var respuesta = await client.GetAsync(UrlApi(ruta),HttpCompletionOption.ResponseHeadersRead,cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return new ContentResult
                {
                    StatusCode = (int)respuesta.StatusCode,ContentType = "application/json",Content = await respuesta.Content.ReadAsStringAsync(cancelacion)
                };
            }

            var nombre =
                respuesta.Content.Headers.ContentDisposition?.FileNameStar
                ?? respuesta.Content.Headers.ContentDisposition?
                    .FileName?.Trim('"')
                ?? "tarea";

            var contenido = await respuesta.Content.ReadAsByteArrayAsync(cancelacion);

            Response.Headers.CacheControl = "no-store";

            return File(contenido,"application/octet-stream",Path.GetFileName(nombre));
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new
            {
                mensaje = "No se pudo conectar con el servicio de archivos."
            });
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                mensaje = "La descarga tardó demasiado en responder."
            });
        }
    }
    #endregion


    #region Pagos

    public async Task<IActionResult> Pagos(int? idEstudiante,string? filtro,CancellationToken cancelacion)
    {
        var modelo = new PortalPagosViewModel
        {
            Filtro = string.IsNullOrWhiteSpace(filtro)
                ? "pendientes"
                : filtro.Trim().ToLowerInvariant()
        };

        try
        {
            using var client = _http.CreateClient();

            // Obtiene los estudiantes y valida la selección.
            var rutaInicio = "portal/inicio";

            if (idEstudiante.HasValue)
            {
                rutaInicio +=
                    $"?idEstudiante={idEstudiante.Value}";
            }

            var inicio = await client.GetFromJsonAsync<PortalInicioViewModel>(UrlApi(rutaInicio), cancelacion);

            if (inicio is null || inicio.Resumen.IdEstudiante <= 0)
            {
                ViewBag.ErrorCarga ="No se pudo obtener el estudiante del portal.";

                return View(modelo);
            }

            modelo.Estudiantes = inicio.Estudiantes;
            modelo.IdEstudiante = inicio.Resumen.IdEstudiante;

            // Consulta los cargos con el filtro seleccionado.
            var rutaPagos =
                $"portal/pagos?idEstudiante={modelo.IdEstudiante}" +
                $"&filtro={Uri.EscapeDataString(modelo.Filtro)}";

            modelo.Pagos = await client .GetFromJsonAsync<List<PortalPagoViewModel>>(UrlApi(rutaPagos),cancelacion)
                ?? [];
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            ViewBag.ErrorCarga =
                "No se pudo validar su sesión con el servicio. " + "Cierre sesión y vuelva a ingresar.";
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == HttpStatusCode.Forbidden)
        {
            return Forbid();
        }
        catch (HttpRequestException)
        {
            ViewBag.ErrorCarga =
                "No se pudieron cargar los pagos. Verifique la conexión " + "y que el estudiante y el filtro sean válidos.";
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            ViewBag.ErrorCarga =
                "El servicio tardó demasiado en responder. " +  "Intente nuevamente.";
        }
        catch (JsonException)
        {
            ViewBag.ErrorCarga ="No se pudo interpretar la información de los pagos.";
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> DetallePago(int idEstudiante, int idPago, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0 || idPago <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Seleccione un estudiante y un cargo válidos."
            });
        }

        try
        {
            using var client = _http.CreateClient();

            var ruta =
                $"portal/pagos/{idPago}" +
                $"?idEstudiante={idEstudiante}";

            using var respuesta = await client.GetAsync(UrlApi(ruta),cancelacion);

            var contenido = await respuesta.Content.ReadAsStringAsync(cancelacion);

            return new ContentResult
            {
                StatusCode = (int)respuesta.StatusCode,
                ContentType = "application/json",
                Content = contenido
            };
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new
            {
                mensaje = "No se pudo conectar con el servicio de pagos."
            });
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                mensaje = "El servicio tardó demasiado en responder."
            });
        }
    }

    #endregion


    [HttpGet]
    public async Task<IActionResult> DetalleReporte( int idEstudiante,int idReporte,CancellationToken cancelacion)
    {
        if (idEstudiante <= 0 || idReporte <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Seleccione un estudiante y un reporte válidos."
            });
        }

        try
        {
            using var client = _http.CreateClient();

            var ruta = $"portal/reportes/{idReporte}?idEstudiante={idEstudiante}";

            using var respuesta = await client.GetAsync(
                UrlApi(ruta), cancelacion);

            Response.Headers.CacheControl = "no-store";

            return new ContentResult
            {
                StatusCode = (int)respuesta.StatusCode,
                ContentType = "application/json",
                Content = await respuesta.Content.ReadAsStringAsync(cancelacion)
            };
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new
            {
                mensaje = "No se pudo conectar con el servicio de reportes."
            });
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            return StatusCode(504, new
            {
                mensaje = "El servicio tardó demasiado en responder."
            });
        }
    }


    #region Otras pantallas



    public IActionResult Perfil() => View();

    #endregion

    private string UrlApi(string ruta)
        => _config.GetValue<string>("Valores:UrlAPI") + ruta;
}