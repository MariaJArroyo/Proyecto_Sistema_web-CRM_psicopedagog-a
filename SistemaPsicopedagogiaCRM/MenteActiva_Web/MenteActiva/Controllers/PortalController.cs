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
                ? "todas"
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
                ? "todas"
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

            return File(contenido,"application/pdf",Path.GetFileName(nombre));
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


    #region Documentos

    // Documentos del estudiante que la profesional dejo visibles en el portal
    public async Task<IActionResult> Documentos(int? idEstudiante, CancellationToken cancelacion)
    {
        var modelo = new PortalDocumentosViewModel();

        try
        {
            using var client = _http.CreateClient();

            var rutaInicio = "portal/inicio";

            if (idEstudiante.HasValue)
            {
                rutaInicio += $"?idEstudiante={idEstudiante.Value}";
            }

            var inicio = await client.GetFromJsonAsync<PortalInicioViewModel>(UrlApi(rutaInicio), cancelacion);

            if (inicio is null || inicio.Resumen.IdEstudiante <= 0)
            {
                ViewBag.ErrorCarga = "No se pudo obtener el estudiante del portal.";
                return View(modelo);
            }

            modelo.Estudiantes = inicio.Estudiantes;
            modelo.IdEstudiante = inicio.Resumen.IdEstudiante;

            modelo.Documentos = await client.GetFromJsonAsync<List<PortalDocumentoViewModel>>(
                UrlApi($"portal/documentos?idEstudiante={modelo.IdEstudiante}"), cancelacion) ?? [];
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
            ViewBag.ErrorCarga = "No se pudieron cargar los documentos. Verifique la conexión e intente de nuevo.";
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            ViewBag.ErrorCarga = "El servicio tardó demasiado en responder. Intente nuevamente.";
        }
        catch (JsonException)
        {
            ViewBag.ErrorCarga = "No se pudo interpretar la información de los documentos.";
        }

        return View(modelo);
    }

    // ver = true lo abre en el navegador (PDF e imagenes); si no, lo descarga
    [HttpGet]
    public async Task<IActionResult> DescargarDocumento(
        int idEstudiante, int idDocumento, bool ver, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0 || idDocumento <= 0)
        {
            return NotFound();
        }

        try
        {
            using var client = _http.CreateClient();
            using var respuesta = await client.GetAsync(
                UrlApi($"portal/documentos/{idDocumento}/archivo?idEstudiante={idEstudiante}"),
                HttpCompletionOption.ResponseHeadersRead, cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return StatusCode((int)respuesta.StatusCode);
            }

            var tipo = respuesta.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var disposicion = respuesta.Content.Headers.ContentDisposition;
            var nombre = disposicion?.FileNameStar ?? disposicion?.FileName?.Trim('"') ?? "documento";
            var bytes = await respuesta.Content.ReadAsByteArrayAsync(cancelacion);

            Response.Headers.CacheControl = "no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            return ver ? File(bytes, tipo) : File(bytes, tipo, nombre);
        }
        catch (HttpRequestException)
        {
            return StatusCode(503);
        }
    }

    #endregion


    #region Recursos (HU-M11-2, 4 y 5)

    // Los recursos no son por estudiante: todo encargado con un estudiante activo
    // ve lo publicado. Se cargan los estudiantes solo para el selector del menu.
    public async Task<IActionResult> Recursos(int? idEstudiante, CancellationToken cancelacion)
    {
        var modelo = new PortalRecursosViewModel();

        try
        {
            using var client = _http.CreateClient();

            var rutaInicio = "portal/inicio";

            if (idEstudiante.HasValue)
            {
                rutaInicio += $"?idEstudiante={idEstudiante.Value}";
            }

            var inicio = await client.GetFromJsonAsync<PortalInicioViewModel>(UrlApi(rutaInicio), cancelacion);

            if (inicio is not null)
            {
                modelo.Estudiantes = inicio.Estudiantes;
                modelo.IdEstudiante = inicio.Resumen.IdEstudiante;
            }

            modelo.Recursos = await client
                .GetFromJsonAsync<List<PortalRecursoViewModel>>(UrlApi("portal/recursos"), cancelacion)
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
                "No se pudieron cargar los recursos. Verifique la conexión " + "y que tenga estudiantes activos vinculados a su cuenta.";
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            ViewBag.ErrorCarga =
                "El servicio tardó demasiado en responder. " + "Intente nuevamente.";
        }
        catch (JsonException)
        {
            ViewBag.ErrorCarga = "No se pudo interpretar la información de los recursos.";
        }

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> DescargarRecurso(int idMaterial, CancellationToken cancelacion)
    {
        if (idMaterial <= 0)
        {
            return BadRequest(new { mensaje = "Seleccione un recurso válido." });
        }

        try
        {
            using var client = _http.CreateClient();

            using var respuesta = await client.GetAsync(
                UrlApi($"portal/recursos/{idMaterial}/archivo"),
                HttpCompletionOption.ResponseHeadersRead,
                cancelacion);

            if (!respuesta.IsSuccessStatusCode)
            {
                return new ContentResult
                {
                    StatusCode = (int)respuesta.StatusCode,
                    ContentType = "application/json",
                    Content = await respuesta.Content.ReadAsStringAsync(cancelacion)
                };
            }

            var nombre =
                respuesta.Content.Headers.ContentDisposition?.FileNameStar
                ?? respuesta.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? "recurso.pdf";

            var contenido = await respuesta.Content.ReadAsByteArrayAsync(cancelacion);

            Response.Headers.CacheControl = "no-store";

            return File(contenido, "application/pdf", Path.GetFileName(nombre));
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new { mensaje = "No se pudo conectar con el servicio de archivos." });
        }
        catch (TaskCanceledException)
            when (!cancelacion.IsCancellationRequested)
        {
            return StatusCode(504, new { mensaje = "La descarga tardó demasiado en responder." });
        }
    }

    [HttpGet]
    public Task<IActionResult> VerificarEnlaceRecurso(int idMaterial, CancellationToken cancelacion)
        => ReenviarGetAsync($"portal/recursos/{idMaterial}/verificar-enlace", cancelacion);

    #endregion


    #region Pagos

    public async Task<IActionResult> Pagos(int? idEstudiante,string? filtro,CancellationToken cancelacion)
    {
        var modelo = new PortalPagosViewModel
        {
            Filtro = string.IsNullOrWhiteSpace(filtro)
                ? "todos"
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

            // Lo reportado por la familia. Si falla, la pantalla igual muestra los
            // cargos: solo se pierde la lista de reportes
            try
            {
                modelo.Reportes = await client.GetFromJsonAsync<List<PortalReportePagoViewModel>>(
                    UrlApi($"portal/reportes-pago?idEstudiante={modelo.IdEstudiante}"), cancelacion) ?? [];
            }
            catch (Exception ex) when (ex is HttpRequestException or JsonException)
            {
                ViewBag.ErrorReportes = "No se pudo cargar la lista de pagos reportados.";
            }
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

    // Reporte de un SINPE o transferencia, con la captura, la referencia o ambas.
    // Llega por fetch con el token antifalsificacion en el FormData (filtro global)
    // y se reenvia al API como multipart, con el archivo tal cual.
    [HttpPost]
    public async Task<IActionResult> ReportarPago(
        int idEstudiante, int idPago, int idMetodoPago, string? monto, string? fechaPago,
        string? referencia, string? comentario, IFormFile? comprobante, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0 || idPago <= 0)
        {
            return BadRequest(new { mensaje = "Seleccione un cargo válido." });
        }

        if (string.IsNullOrWhiteSpace(referencia) && (comprobante is null || comprobante.Length == 0))
        {
            return BadRequest(new { mensaje = "Adjunte el comprobante o escriba el número de referencia." });
        }

        using var cuerpo = new MultipartFormDataContent();

        cuerpo.Add(new StringContent(idEstudiante.ToString(System.Globalization.CultureInfo.InvariantCulture)), "idEstudiante");
        cuerpo.Add(new StringContent(idMetodoPago.ToString(System.Globalization.CultureInfo.InvariantCulture)), "idMetodoPago");
        cuerpo.Add(new StringContent(monto?.Trim() ?? string.Empty), "monto");
        cuerpo.Add(new StringContent(fechaPago?.Trim() ?? string.Empty), "fechaPago");

        if (!string.IsNullOrWhiteSpace(referencia))
        {
            cuerpo.Add(new StringContent(referencia.Trim()), "referencia");
        }

        if (!string.IsNullOrWhiteSpace(comentario))
        {
            cuerpo.Add(new StringContent(comentario.Trim()), "comentario");
        }

        Stream? flujo = null;

        try
        {
            if (comprobante is { Length: > 0 })
            {
                flujo = comprobante.OpenReadStream();
                var parte = new StreamContent(flujo);

                if (System.Net.Http.Headers.MediaTypeHeaderValue.TryParse(comprobante.ContentType, out var tipo))
                {
                    parte.Headers.ContentType = tipo;
                }

                cuerpo.Add(parte, "comprobante", comprobante.FileName);
            }

            using var client = _http.CreateClient();
            using var respuesta = await client.PostAsync(
                UrlApi($"portal/pagos/{idPago}/reportes"), cuerpo, cancelacion);

            var contenido = await respuesta.Content.ReadAsStringAsync(cancelacion);

            if (string.IsNullOrWhiteSpace(contenido))
            {
                contenido = respuesta.IsSuccessStatusCode
                    ? "{}"
                    : "{\"mensaje\":\"No se pudo enviar el reporte del pago.\"}";
            }

            return new ContentResult
            {
                Content = contenido,
                ContentType = "application/json",
                StatusCode = (int)respuesta.StatusCode
            };
        }
        catch (HttpRequestException)
        {
            return StatusCode(503, new { mensaje = "No se pudo conectar con el servicio de pagos." });
        }
        catch (TaskCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            return StatusCode(504, new { mensaje = "El servicio tardó demasiado en responder." });
        }
        finally
        {
            flujo?.Dispose();
        }
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


    #region Mensajes (HU-M10-4)

    // Los mensajes NO son por estudiante: la conversacion es entre el encargado
    // y el consultorio. Igual se cargan los estudiantes, porque _NavbarPortal los
    // necesita para su selector y para que los demas enlaces no pierdan el
    // estudiante que la persona venia viendo.
    public async Task<IActionResult> Mensajes(int? idEstudiante, CancellationToken cancelacion)
    {
        var modelo = new PortalMensajesViewModel();

        try
        {
            using var client = _http.CreateClient();

            var ruta = "portal/inicio";

            if (idEstudiante.HasValue)
            {
                ruta += $"?idEstudiante={idEstudiante.Value}";
            }

            var inicio = await client.GetFromJsonAsync<PortalInicioViewModel>(
                UrlApi(ruta), cancelacion);

            if (inicio is not null)
            {
                modelo.Estudiantes = inicio.Estudiantes;
                modelo.IdEstudiante = inicio.Resumen.IdEstudiante;
            }
        }
        catch (HttpRequestException)
        {
            // Sin los estudiantes la pantalla de mensajes igual sirve: lo unico
            // que se pierde es el selector de arriba
            ViewBag.ErrorCarga = "No se pudieron cargar sus estudiantes.";
        }

        return View(modelo);
    }

    // ---------- se reenvian tal cual al navegador ----------

    [HttpGet]
    public Task<IActionResult> ConsultarHilos(CancellationToken cancelacion)
        => ReenviarGetAsync("mensajes", cancelacion);

    [HttpGet]
    public Task<IActionResult> AbrirHilo(int idHilo, CancellationToken cancelacion)
        => ReenviarGetAsync($"mensajes/{idHilo}", cancelacion);

    [HttpGet]
    public Task<IActionResult> DestinatariosMensaje(CancellationToken cancelacion)
        => ReenviarGetAsync("mensajes/destinatarios", cancelacion);

    [HttpGet]
    public Task<IActionResult> MensajesSinLeer(CancellationToken cancelacion)
        => ReenviarGetAsync("mensajes/sin-leer", cancelacion);

    [HttpPost]
    public async Task<IActionResult> EnviarMensaje(
        EnviarMensajeViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            var errores = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Distinct();

            return BadRequest(new { mensaje = string.Join(" ", errores) });
        }

        using var client = _http.CreateClient();
        using var respuesta = await client.PostAsJsonAsync(UrlApi("mensajes"), modelo, cancelacion);

        var contenido = await respuesta.Content.ReadAsStringAsync(cancelacion);

        if (string.IsNullOrWhiteSpace(contenido))
        {
            contenido = respuesta.IsSuccessStatusCode
                ? "{}"
                : "{\"mensaje\":\"No se pudo enviar el mensaje.\"}";
        }

        return new ContentResult
        {
            Content = contenido,
            ContentType = "application/json",
            StatusCode = (int)respuesta.StatusCode
        };
    }

    // Reenvia la respuesta del API tal cual, con su codigo y su cuerpo
    private async Task<IActionResult> ReenviarGetAsync(string ruta, CancellationToken cancelacion)
    {
        using var client = _http.CreateClient();
        using var respuesta = await client.GetAsync(UrlApi(ruta), cancelacion);

        var contenido = await respuesta.Content.ReadAsStringAsync(cancelacion);

        if (!respuesta.IsSuccessStatusCode && string.IsNullOrWhiteSpace(contenido))
        {
            contenido = "{\"mensaje\":\"No se pudo completar la consulta.\"}";
        }

        return new ContentResult
        {
            Content = contenido,
            ContentType = "application/json",
            StatusCode = (int)respuesta.StatusCode
        };
    }

    #endregion

    #region perfin portal
    [HttpGet]
    public async Task<IActionResult> Perfil(int? idEstudiante, CancellationToken cancelacion)
    {
        var modelo = new PortalPerfilViewModel();

        try
        {
            using var client = _http.CreateClient();

            var perfil = await client.GetFromJsonAsync<PortalPerfilViewModel>(
                UrlApi("portal/perfil"), cancelacion);

            if (perfil is not null)
            {
                modelo.NombreCompleto = perfil.NombreCompleto;
                modelo.Correo = perfil.Correo;
                modelo.Telefono = perfil.Telefono;
                modelo.EstudiantesACargo = perfil.EstudiantesACargo;
            }

            // Estudiantes para el selector del navbar
            var ruta = "portal/inicio";
            if (idEstudiante.HasValue)
            {
                ruta += $"?idEstudiante={idEstudiante.Value}";
            }

            var inicio = await client.GetFromJsonAsync<PortalInicioViewModel>(UrlApi(ruta), cancelacion);

            if (inicio is not null)
            {
                modelo.Estudiantes = inicio.Estudiantes;
                modelo.IdEstudiante = inicio.Resumen.IdEstudiante;
            }
        }
        catch (HttpRequestException)
        {
            ViewBag.ErrorCarga = "No se pudo cargar su perfil. Intente nuevamente.";
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarPerfil(EditarMiPerfilViewModel modelo, CancellationToken cancelacion)
    {
        // Lo minimo antes de ir al API, que valida lo mismo y ademas el correo repetido
        if (string.IsNullOrWhiteSpace(modelo.NombreCompleto))
        {
            TempData["Error"] = "Ingrese su nombre completo.";
            return RedirectToAction(nameof(Perfil));
        }

        if (string.IsNullOrWhiteSpace(modelo.Correo) || !modelo.Correo.Contains('@'))
        {
            TempData["Error"] = "Ingrese un correo electrónico válido.";
            return RedirectToAction(nameof(Perfil));
        }

        await EnviarAsync(
            client => client.PutAsJsonAsync(
                UrlApi("portal/perfil"),
                new
                {
                    NombreCompleto = modelo.NombreCompleto.Trim(),
                    Correo = modelo.Correo.Trim(),
                    // Vacio viaja vacio: asi se puede borrar el telefono
                    Telefono = modelo.Telefono?.Trim() ?? string.Empty
                },
                cancelacion),
            "No se pudo actualizar el perfil.", cancelacion);

        return RedirectToAction(nameof(Perfil));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel modelo, CancellationToken cancelacion)
    {
        if (string.IsNullOrEmpty(modelo.ContrasenaActual))
        {
            TempData["Error"] = "Ingrese su contraseña actual.";
            return RedirectToAction(nameof(Perfil));
        }

        if (string.IsNullOrEmpty(modelo.ContrasenaNueva) || modelo.ContrasenaNueva.Length < 8)
        {
            TempData["Error"] = "La nueva contraseña debe tener al menos 8 caracteres.";
            return RedirectToAction(nameof(Perfil));
        }

        if (modelo.ContrasenaNueva != modelo.ConfirmarContrasena)
        {
            TempData["Error"] = "La nueva contraseña y su confirmación no coinciden.";
            return RedirectToAction(nameof(Perfil));
        }

        if (modelo.ContrasenaNueva == modelo.ContrasenaActual)
        {
            TempData["Error"] = "La nueva contraseña debe ser distinta de la actual.";
            return RedirectToAction(nameof(Perfil));
        }

        await EnviarAsync(
            client => client.PostAsJsonAsync(
                UrlApi("autenticacion/cambiar-contrasena"),
                new { modelo.ContrasenaActual, modelo.ContrasenaNueva },
                cancelacion),
            "No se pudo cambiar la contraseña.", cancelacion);

        return RedirectToAction(nameof(Perfil));
    }

    // Llama al API y deja el resultado en TempData. Si el API no responde o
    // contesta sin JSON (por ejemplo un 401 vacio), avisa en vez de romper la
    // pantalla con un error 500.
    private async Task EnviarAsync(
        Func<HttpClient, Task<HttpResponseMessage>> llamada, string textoPorDefecto, CancellationToken cancelacion)
    {
        try
        {
            using var client = _http.CreateClient();
            using var respuesta = await llamada(client);

            if (respuesta.StatusCode == HttpStatusCode.Unauthorized)
            {
                TempData["Error"] = "Su sesión venció. Cierre sesión y vuelva a ingresar.";
                return;
            }

            await GuardarAvisoAsync(respuesta, textoPorDefecto, cancelacion);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "No se pudo conectar con el servicio. Intente nuevamente.";
        }
        catch (TaskCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            TempData["Error"] = "El servicio tardó demasiado en responder. Intente nuevamente.";
        }
    }

    private async Task GuardarAvisoAsync(HttpResponseMessage respuesta, string textoPorDefecto, CancellationToken cancelacion)
    {
        string? mensaje = null;

        try
        {
            var cuerpo = await respuesta.Content
                .ReadFromJsonAsync<Dictionary<string, JsonElement>>(cancellationToken: cancelacion);

            if (cuerpo is not null
                && cuerpo.TryGetValue("mensaje", out var texto) && texto.ValueKind == JsonValueKind.String)
            {
                mensaje = texto.GetString();
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Sin cuerpo o sin JSON: se usa el texto por defecto
        }

        if (respuesta.IsSuccessStatusCode)
            TempData["Mensaje"] = mensaje ?? "Listo.";
        else
            TempData["Error"] = mensaje ?? textoPorDefecto;
    }
    #endregion

    private string UrlApi(string ruta)
        => _config.GetValue<string>("Valores:UrlAPI") + ruta;
}