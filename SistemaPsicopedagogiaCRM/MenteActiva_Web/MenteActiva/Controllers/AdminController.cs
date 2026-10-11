using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using MenteActiva.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using System.Globalization;

namespace MenteActiva.Controllers;


[Authorize(Roles = "Administrador,Psicopedagoga,Asistente")]
public class AdminController : Controller
{
    private readonly IHttpClientFactory _http;
    private readonly IConfiguration _config;

    public AdminController(IHttpClientFactory http, IConfiguration config)
    {
        _http = http;
        _config = config;
    }

    #region Agenda

    public async Task<IActionResult> Agenda(CancellationToken cancelacion)
    {
        var catalogos = new CatalogosAgendaViewModel();

        try
        {
            using var client = _http.CreateClient();

            catalogos = await client.GetFromJsonAsync<CatalogosAgendaViewModel>(
                UrlApi("citas/catalogos"), cancelacion) ?? catalogos;
        }
        catch (HttpRequestException)
        {
            // Sin catalogos la agenda igual se ve; solo "Nueva cita" queda deshabilitado
            ViewBag.ErrorCarga = "No se pudieron cargar los catálogos de la agenda.";
        }

        return View(catalogos);
    }

    // ---------- consultas: se reenvian tal cual al navegador ----------

    [HttpGet]
    public Task<IActionResult> ConsultarAgenda(DateOnly desde, DateOnly hasta, CancellationToken cancelacion)
        => ReenviarGetAsync($"citas?desde={FormatoFecha(desde)}&hasta={FormatoFecha(hasta)}", cancelacion);

    [HttpGet]
    public Task<IActionResult> BuscarEstudiantesCita(string? texto, CancellationToken cancelacion)
        => ReenviarGetAsync($"citas/estudiantes?texto={Uri.EscapeDataString(texto ?? string.Empty)}", cancelacion);

    [HttpGet]
    public Task<IActionResult> DisponibilidadCita(
            DateOnly fecha, int idTipoSesion, int? idCitaExcluir, int? idGrupoExcluir, CancellationToken cancelacion)
    {
        var ruta = $"citas/disponibilidad?fecha={FormatoFecha(fecha)}&idTipoSesion={idTipoSesion}";

        if (idCitaExcluir is not null)
        {
            ruta += $"&idCitaExcluir={idCitaExcluir}";
        }

        if (idGrupoExcluir is not null)
        {
            ruta += $"&idGrupoExcluir={idGrupoExcluir}";
        }

        return ReenviarGetAsync(ruta, cancelacion);
    }

    [HttpGet]
    public Task<IActionResult> DiasNoLaboralesAgenda(DateOnly desde, DateOnly hasta, CancellationToken cancelacion)
       => ReenviarGetAsync(
           $"agenda/dias-no-laborales?desde={FormatoFecha(desde)}&hasta={FormatoFecha(hasta)}", cancelacion);

    // ---------- acciones (por fetch, con token antifalsificacion en el FormData) ----------

    [HttpPost]
    public async Task<IActionResult> CrearCita(CrearCitaViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(UrlApi("citas"), modelo, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo agendar la cita.");
    }

    [HttpPost]
    public async Task<IActionResult> ReprogramarCita(int id, DateTime? fechaHoraInicio, CancellationToken cancelacion)
    {
        if (fechaHoraInicio is null)
        {
            return BadRequest(new { mensaje = "Seleccione el nuevo horario." });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"citas/{id}/reprogramar"), new { fechaHoraInicio }, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo reprogramar la cita.");
    }

    [HttpPost]
    public async Task<IActionResult> CancelarCita(int id, string? motivo, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return BadRequest(new { mensaje = "Indique el motivo de la cancelación." });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"citas/{id}/cancelar"), new { motivo }, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo cancelar la cita.");
    }

    [HttpPost]
    public async Task<IActionResult> MarcarNoAsistioCita(int id, CancellationToken cancelacion)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.PutAsync(UrlApi($"citas/{id}/no-asistio"), null, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo marcar la cita.");
    }

    // ---------- grupos ----------

    [HttpGet]
    public Task<IActionResult> GruposDisponiblesCita(DateOnly fecha, CancellationToken cancelacion)
        => ReenviarGetAsync($"citas/grupos/disponibles?fecha={FormatoFecha(fecha)}", cancelacion);

    [HttpPost]
    public async Task<IActionResult> CrearGrupoCita(CrearGrupoViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(UrlApi("citas/grupos"), modelo, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo crear el grupo.");
    }

    [HttpPost]
    public async Task<IActionResult> AgregarEstudianteGrupo(int id, int idEstudiante, CancellationToken cancelacion)
    {
        if (idEstudiante <= 0)
        {
            return BadRequest(new { mensaje = "Seleccione un estudiante." });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            UrlApi($"citas/grupos/{id}/estudiantes"), new { idEstudiante }, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo agregar el estudiante al grupo.");
    }

    [HttpPost]
    public async Task<IActionResult> EditarGrupoCita(int id, string? nombre, int cupoMaximo, CancellationToken cancelacion)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"citas/grupos/{id}"), new { nombre, cupoMaximo }, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo actualizar el grupo.");
    }

    [HttpPost]
    public async Task<IActionResult> ReprogramarGrupoCita(int id, DateTime? fechaHoraInicio, CancellationToken cancelacion)
    {
        if (fechaHoraInicio is null)
        {
            return BadRequest(new { mensaje = "Seleccione el nuevo horario." });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"citas/grupos/{id}/reprogramar"), new { fechaHoraInicio }, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo reprogramar el grupo.");
    }

    [HttpPost]
    public async Task<IActionResult> CancelarGrupoCita(int id, string? motivo, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(motivo))
        {
            return BadRequest(new { mensaje = "Indique el motivo de la cancelación." });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"citas/grupos/{id}/cancelar"), new { motivo }, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo cancelar el grupo.");
    }

    // ---------- auxiliares ----------

    // Reenvia la respuesta del API (JSON y codigo) sin deserializar: para consultas
    // que la pantalla consume directo
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

    private static string FormatoFecha(DateOnly fecha)
        => fecha.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    #endregion

    #region Clientes

    public IActionResult Clientes()
    {
        using var client = _http.CreateClient();

        var clientes = client
            .GetFromJsonAsync<List<ClienteViewModel>>(UrlApi("clientes"))
            .Result;

        CargarCatalogosClientes();

        return View(clientes ?? new List<ClienteViewModel>());
    }

    #region catalogos de clientes
    private void CargarCatalogosClientes()
    {
        using var client = _http.CreateClient();

        ViewBag.Servicios = client
            .GetFromJsonAsync<List<ServicioViewModel>>(UrlApi("servicios")).Result
            ?? new List<ServicioViewModel>();

        ViewBag.EstadosCliente = client
            .GetFromJsonAsync<List<EstadoClienteViewModel>>(UrlApi("clientes/estados")).Result
            ?? new List<EstadoClienteViewModel>();

        ViewBag.Parentescos = client
            .GetFromJsonAsync<List<ParentescoViewModel>>(UrlApi("clientes/parentescos")).Result
            ?? new List<ParentescoViewModel>();

        ViewBag.NivelesEducativos = client
            .GetFromJsonAsync<List<NivelEducativoViewModel>>(UrlApi("clientes/niveles")).Result
            ?? new List<NivelEducativoViewModel>();
    }
    #endregion

    #region registrar cliente
    [HttpGet]
    public IActionResult RegistrarCliente() => RedirectToAction("Clientes");

    [HttpPost]
    public async Task<IActionResult> RegistrarCliente(ClienteRegistroViewModel modelo)
    {
        if (modelo.Estudiantes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Debe agregar al menos un estudiante.");
        }

        if (modelo.IdServicioInteres <= 0)
        {
            ModelState.AddModelError(nameof(modelo.IdServicioInteres), "Debe seleccionar un servicio de interes.");
        }

        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(UrlApi("clientes"), modelo);

        return await ResponderSegunApi(respuesta, "No se pudo registrar el cliente.");
    }
    #endregion

    #region obtener cliente
    [HttpGet]
    public async Task<IActionResult> ObtenerCliente(int id)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.GetAsync(UrlApi("clientes/" + id));

        if (respuesta.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound(new { mensaje = "El cliente no existe." });
        }

        if (!respuesta.IsSuccessStatusCode)
        {
            return StatusCode((int)respuesta.StatusCode,
                new { mensaje = "No se pudo cargar el cliente." });
        }

        var cliente = await respuesta.Content.ReadFromJsonAsync<ClienteDetalleViewModel>();

        return Json(cliente);
    }
    #endregion

    #region editar cliente
    [HttpPost]
    public async Task<IActionResult> EditarCliente(ClienteEditarViewModel modelo)
    {
        if (modelo.IdServicioInteres <= 0)
        {
            ModelState.AddModelError(nameof(modelo.IdServicioInteres), "Debe seleccionar un servicio de interes.");
        }

        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(UrlApi("clientes/" + modelo.Id), modelo);

        return await ResponderSegunApi(respuesta, "No se pudo editar el cliente.");
    }
    #endregion

    #region desactivar cliente

    [HttpPost]
    [IgnoreAntiforgeryToken]
    public IActionResult DesactivarCliente(int id)
    {
        using var client = _http.CreateClient();

        var result = client.PutAsync(UrlApi("clientes/" + id + "/desactivar"), null).Result;

        if (result.IsSuccessStatusCode)
        {
            return Ok();
        }

        return BadRequest();
    }
    #endregion

    #region estudiantes del cliente
    [HttpPost]
    public async Task<IActionResult> AgregarEstudianteCliente(int idCliente, EstudianteClienteViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            UrlApi($"clientes/{idCliente}/estudiantes"), modelo);

        return await ResponderSegunApi(respuesta, "No se pudo agregar el estudiante.");
    }

    [HttpPost]
    public async Task<IActionResult> EditarEstudianteCliente(int idCliente, int idEstudiante, EstudianteClienteViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"clientes/{idCliente}/estudiantes/{idEstudiante}"), modelo);

        return await ResponderSegunApi(respuesta, "No se pudo editar el estudiante.");
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstadoEstudianteCliente(int idCliente, int idEstudiante, bool activo)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"clientes/{idCliente}/estudiantes/{idEstudiante}/estado"), new { activo });

        return await ResponderSegunApi(respuesta, "No se pudo cambiar el estado del estudiante.");
    }
    #endregion

    #region auxiliares de clientes
    private string UrlApi(string ruta)
        => _config.GetValue<string>("Valores:UrlAPI") + ruta;

    // Junta los mensajes de validacion en un solo texto para mostrarlo en el modal
    private IActionResult ErrorDeModelo()
    {
        var errores = ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Distinct();

        return BadRequest(new { mensaje = string.Join(" ", errores) });
    }


    private static async Task<IActionResult> ResponderSegunApi(HttpResponseMessage respuesta, string mensajePorDefecto)
    {
        if (respuesta.IsSuccessStatusCode)
        {
            return new OkResult();
        }

        var mensaje = mensajePorDefecto;

        try
        {
            var json = await respuesta.Content.ReadFromJsonAsync<JsonElement>();
            if (json.TryGetProperty("mensaje", out var valor) && !string.IsNullOrWhiteSpace(valor.GetString()))
            {
                mensaje = valor.GetString()!;
            }
        }
        catch
        {

        }

        return new ObjectResult(new { mensaje }) { StatusCode = (int)respuesta.StatusCode };
    }
    #endregion

    #endregion

    #region Solicitudes

    public IActionResult Solicitudes()
    {
        using var client = _http.CreateClient();

        var solicitudes = client
            .GetFromJsonAsync<List<SolicitudViewModel>>(UrlApi("solicitudes"))
            .Result;

        ViewBag.EstadosSolicitud = client
            .GetFromJsonAsync<List<EstadoSolicitudViewModel>>(UrlApi("solicitudes/estados")).Result
            ?? new List<EstadoSolicitudViewModel>();

        // El modal de Nuevo cliente (convertir) necesita servicios, parentescos y niveles
        CargarCatalogosClientes();

        return View(solicitudes ?? new List<SolicitudViewModel>());
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerSolicitud(int id)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.GetAsync(UrlApi("solicitudes/" + id));

        if (!respuesta.IsSuccessStatusCode)
        {
            return StatusCode((int)respuesta.StatusCode,
                new { mensaje = "No se pudo cargar la solicitud." });
        }

        // El API ya devuelve JSON en camelCase: se reenvia tal cual
        return Content(await respuesta.Content.ReadAsStringAsync(), "application/json");
    }

    [HttpPost]
    public async Task<IActionResult> CambiarEstadoSolicitud(int id, int idEstadoSolicitud, string? notaInterna)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"solicitudes/{id}/estado"), new { idEstadoSolicitud, notaInterna });

        return await ResponderSegunApi(respuesta, "No se pudo actualizar la solicitud.");
    }

    [HttpPost]
    public async Task<IActionResult> VincularSolicitud(int id, int idEncargado)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"solicitudes/{id}/vincular"), new { idEncargado });

        return await ResponderSegunApi(respuesta, "No se pudo vincular la solicitud.");
    }

    // Lo consulta el contador del menu en todas las pantallas. Si algo falla
    // devuelve 0: un contador caido no debe romper el panel.
    [HttpGet]
    public async Task<IActionResult> ContarSolicitudesPendientes()
    {
        try
        {
            using var client = _http.CreateClient();

            var json = await client.GetFromJsonAsync<JsonElement>(UrlApi("solicitudes/pendientes"));

            return Json(new { pendientes = json.GetProperty("pendientes").GetInt32() });
        }
        catch
        {
            return Json(new { pendientes = 0 });
        }
    }

    #endregion


    #region Estudiantes

    public IActionResult Estudiantes()
    {
        using var client = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI")
                  + "estudiantes";

        var estudiantes = client
            .GetFromJsonAsync<List<EstudianteViewModel>>(url)
            .Result;

        return View(estudiantes ?? new List<EstudianteViewModel>());
    }

    #region Ficha Estudiante

    public IActionResult EstudianteFicha(int id)
    {
        using var client = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI")
                  + "estudiantes/"
                  + id;

        var estudiante = client
            .GetFromJsonAsync<FichaEstudianteViewModel>(url)
            .Result;

        if (estudiante == null)
        {
            return RedirectToAction("Estudiantes");
        }

        return View(estudiante);
    }

    #endregion

    #endregion


    public IActionResult Comunicacion() => View();
    public IActionResult Dashboard() => View();
    public IActionResult Materiales() => View();
    public IActionResult MiPerfil() => View();
    public IActionResult Pagos() => View();

    public async Task<IActionResult> Planes()
    {
        using var client = _http.CreateClient();

        var planes = await client.GetFromJsonAsync<List<PlanViewModel>>(
            UrlApi("planes"));

        var estudiantes = await client.GetFromJsonAsync<List<EstudianteViewModel>>(
            UrlApi("estudiantes"));

        ViewBag.Estudiantes = estudiantes ?? new List<EstudianteViewModel>();

        return View(planes ?? new List<PlanViewModel>());
    }


    [HttpPost]
    public async Task<IActionResult> CrearPlan(
        [FromBody] PlanCrearViewModel modelo,
        CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensaje = "Revise los datos del plan."
            });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            UrlApi("planes"),
            modelo,
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return await ReenviarRespuestaAsync(
                respuesta,
                "No se pudo crear el plan.",
                cancelacion);
        }

        return Ok(new
        {
            mensaje = "Plan de intervención creado correctamente."
        });
    }


    [HttpPost]
    public async Task<IActionResult> EditarPlan(
        int id,
        [FromBody] PlanEditarViewModel modelo,
        CancellationToken cancelacion)
    {

        if (!ModelState.IsValid)
        {
            var errores = ModelState
                .Where(campo => campo.Value?.Errors.Count > 0)
                .SelectMany(campo => campo.Value!.Errors.Select(error => new
                {
                    campo = campo.Key,
                    mensaje = string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? error.Exception?.Message
                        : error.ErrorMessage
                }))
                .ToList();

            return BadRequest(new
            {
                mensaje = "Revise los datos del plan.",
                errores
            });
        }


        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"planes/{id}"),
            modelo,
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return await ReenviarRespuestaAsync(
                respuesta,
                "No se pudo editar el plan.",
                cancelacion);
        }

        return Ok(new
        {
            mensaje = "Plan de intervención actualizado correctamente."
        });
    }




    [HttpPost]
    public async Task<IActionResult> InactivarPlan(
        int id,
        int idEstadoPlan,
        CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El plan indicado no es válido."
            });
        }

        if (idEstadoPlan != 2 && idEstadoPlan != 5)
        {
            return BadRequest(new
            {
                mensaje = "El estado solicitado no es válido."
            });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(
            UrlApi($"planes/{id}/estado"),
            new { IdEstadoPlan = idEstadoPlan },
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return await ReenviarRespuestaAsync(
                respuesta,
                "No se pudo cambiar el estado del plan.",
                cancelacion);
        }

        return Ok(new
        {
            mensaje = idEstadoPlan == 2
                ? "Plan reactivado correctamente."
                : "Plan inactivado correctamente."
        });
    }



    [HttpPost]
    public async Task<IActionResult> EliminarPlan(
        int id,
        CancellationToken cancelacion)
    {
        if (id <= 0)
        {
            return BadRequest(new
            {
                mensaje = "El identificador del plan no es válido."
            });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.DeleteAsync(
            UrlApi($"planes/{id}"),
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return await ReenviarRespuestaAsync(
                respuesta,
                "No se pudo eliminar el plan.",
                cancelacion);
        }

        return Ok(new
        {
            mensaje = "Plan de intervención eliminado correctamente."
        });
    }


    [HttpGet]
    public async Task<IActionResult> HistorialPlan(
        int id,
        CancellationToken cancelacion)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.GetAsync(
            UrlApi($"planes/{id}/historial"),
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return BadRequest(new
            {
                mensaje = "No se pudo consultar el historial del plan."
            });
        }

        var historial = await respuesta.Content.ReadFromJsonAsync<
            List<PlanHistorialViewModel>
        >(cancellationToken: cancelacion);

        return Ok(historial ?? new List<PlanHistorialViewModel>());
    }

    [HttpPost]
    public async Task<IActionResult> CrearEstrategia(
        [FromBody] EstrategiaCrearViewModel modelo,
        CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensaje = "Revise los datos de la estrategia."
            });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            UrlApi("estrategias"),
            modelo,
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return await ReenviarRespuestaAsync(
                respuesta,
                "No se pudo agregar la estrategia.",
                cancelacion);
        }

        var resultado = await respuesta.Content.ReadFromJsonAsync<
            EstrategiaCrearRespuestaViewModel
        >(cancellationToken: cancelacion);

        return Ok(resultado ?? new EstrategiaCrearRespuestaViewModel
        {
            Mensaje = "Estrategia agregada correctamente."
        });
    }


    [HttpGet]
    public async Task<IActionResult> ListarEstrategias(
        int idPlan,
        CancellationToken cancelacion)
    {
        if (idPlan <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe indicar un plan válido."
            });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.GetAsync(
            UrlApi($"estrategias/plan/{idPlan}"),
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return await ReenviarRespuestaAsync(
                respuesta,
                "No se pudieron consultar las estrategias.",
                cancelacion);
        }

        var resultado = await respuesta.Content.ReadFromJsonAsync<
            List<EstrategiaViewModel>
        >(cancellationToken: cancelacion);

        return Ok(resultado ?? new List<EstrategiaViewModel>());
    }


    [HttpDelete]
    public async Task<IActionResult> EliminarEstrategia(
        int idEstrategia,
        CancellationToken cancelacion)
    {
        if (idEstrategia <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe indicar una estrategia válida."
            });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.DeleteAsync(
            UrlApi($"estrategias/{idEstrategia}"),
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return await ReenviarRespuestaAsync(
                respuesta,
                "No se pudo eliminar la estrategia.",
                cancelacion);
        }

        return Ok(new
        {
            mensaje = "Estrategia eliminada correctamente."
        });
    }


    [HttpPost]
    public async Task<IActionResult> CrearActividad(
        [FromBody] ActividadCrearViewModel modelo,
        CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                mensaje = "Revise los datos de la actividad."
            });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(
            UrlApi("actividades"),
            modelo,
            cancelacion);
        System.Diagnostics.Debug.WriteLine(">>> SE EJECUTÓ CrearActividad EN EL PROYECTO WEB <<<");

        var detalle = await respuesta.Content.ReadAsStringAsync(cancelacion);

        Console.WriteLine($"ESTADO API: {(int)respuesta.StatusCode}");
        Console.WriteLine($"RESPUESTA API: {detalle}");


        if (!respuesta.IsSuccessStatusCode)
        {
            return StatusCode((int)respuesta.StatusCode, new
            {
                mensaje = "La API rechazó la creación de la actividad.",
                status = (int)respuesta.StatusCode,
                detalle = detalle
            });
        }


        var resultado = await respuesta.Content.ReadFromJsonAsync<
            ActividadCrearRespuestaViewModel
        >(cancellationToken: cancelacion);

        return Ok(resultado ?? new ActividadCrearRespuestaViewModel
        {
            Mensaje = "Actividad agregada correctamente."
        });
    }


    [HttpGet]
    public async Task<IActionResult> ListarActividades(
        int idEstrategia,
        CancellationToken cancelacion)
    {
        if (idEstrategia <= 0)
        {
            return BadRequest(new
            {
                mensaje = "Debe indicar una estrategia válida."
            });
        }

        using var client = _http.CreateClient();

        var respuesta = await client.GetAsync(
            UrlApi($"actividades/estrategia/{idEstrategia}"),
            cancelacion);

        if (!respuesta.IsSuccessStatusCode)
        {
            return await ReenviarRespuestaAsync(
                respuesta,
                "No se pudieron consultar las actividades.",
                cancelacion);
        }

        var resultado = await respuesta.Content.ReadFromJsonAsync<
            List<ActividadViewModel>
        >(cancellationToken: cancelacion);

        return Ok(resultado ?? new List<ActividadViewModel>());
    }






    public IActionResult Reportes() => View();

    #region Sesiones

    // La clase permite 3 roles; esta region exige ademas quitar al Asistente,
    // porque la sesion lleva la nota clinica del menor. Dos [Authorize] se
    // combinan: hay que cumplir los dos, asi que el Asistente no entra.

    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> Sesiones(CancellationToken cancelacion)
    {
        var catalogos = new CatalogosSesionViewModel();

        try
        {
            using var client = _http.CreateClient();

            catalogos = await client.GetFromJsonAsync<CatalogosSesionViewModel>(
                UrlApi("sesiones/catalogos"), cancelacion) ?? catalogos;
        }
        catch (HttpRequestException)
        {
            // Sin catalogos el historial igual se ve; solo "Registrar sesion" queda deshabilitado
            ViewBag.ErrorCarga = "No se pudieron cargar los catálogos de sesiones.";
        }

        return View(catalogos);
    }

    // ---------- consultas: se reenvian tal cual al navegador ----------

    [HttpGet]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public Task<IActionResult> ConsultarSesiones(
            int? idEstudiante, DateOnly? desde, DateOnly? hasta, CancellationToken cancelacion)
    {
        var ruta = "sesiones?";

        if (idEstudiante is > 0) ruta += $"idEstudiante={idEstudiante}&";
        if (desde is not null) ruta += $"desde={FormatoFecha(desde.Value)}&";
        if (hasta is not null) ruta += $"hasta={FormatoFecha(hasta.Value)}";

        return ReenviarGetAsync(ruta, cancelacion);
    }

    [HttpGet]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public Task<IActionResult> DetalleSesion(int id, CancellationToken cancelacion)
        => ReenviarGetAsync($"sesiones/{id}", cancelacion);

    [HttpGet]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public Task<IActionResult> BuscarEstudiantesSesion(string? texto, CancellationToken cancelacion)
        => ReenviarGetAsync($"sesiones/estudiantes?texto={Uri.EscapeDataString(texto ?? string.Empty)}", cancelacion);

    [HttpGet]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public Task<IActionResult> CitasDisponiblesSesion(int idEstudiante, CancellationToken cancelacion)
        => ReenviarGetAsync($"sesiones/citas-disponibles?idEstudiante={idEstudiante}", cancelacion);

    // ---------- escrituras ----------

    [HttpPost]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> RegistrarSesion(
        RegistrarSesionViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PostAsJsonAsync(UrlApi("sesiones"), modelo, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo registrar la sesión.");
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> EditarSesion(
        int id, EditarSesionViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();

        var respuesta = await client.PutAsJsonAsync(UrlApi($"sesiones/{id}"), modelo, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo actualizar la sesión.");
    }

    // Baja logica: la sesion no se borra nunca
    [HttpPost]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> InactivarSesion(int id, CancellationToken cancelacion)
    {
        using var client = _http.CreateClient();

        var respuesta = await client.PutAsync(UrlApi($"sesiones/{id}/inactivar"), null, cancelacion);

        return await ResponderSegunApi(respuesta, "No se pudo inactivar la sesión.");
    }

    #endregion

    #region Configuracion

    // La clase permite 3 roles; cada accion de esta region exige ademas Administrador
    // (dos [Authorize] se combinan: hay que cumplir los dos).

    [Authorize(Roles = "Administrador")]
    public IActionResult Configuracion() => View();

    // ---------- consultas: se reenvian tal cual al navegador ----------

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> ConfiguracionHorario(CancellationToken cancelacion)
        => ReenviarGetAsync("configuracion/horario", cancelacion);

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> ConfiguracionDiasNoLaborales(CancellationToken cancelacion)
        => ReenviarGetAsync("configuracion/dias-no-laborales", cancelacion);

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> ConfiguracionTiposSesion(CancellationToken cancelacion)
        => ReenviarGetAsync("configuracion/tipos-sesion", cancelacion);

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public Task<IActionResult> ConfiguracionConsultorio(CancellationToken cancelacion)
        => ReenviarGetAsync("configuracion/consultorio", cancelacion);

    // ---------- acciones (por fetch, con token antifalsificacion en el FormData) ----------

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GuardarHorario(GuardarHorarioViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();
        using var respuesta = await client.PutAsJsonAsync(UrlApi("configuracion/horario"), modelo, cancelacion);

        return await ReenviarRespuestaAsync(respuesta, "No se pudo guardar el horario.", cancelacion);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AgregarDiaNoLaboral(AgregarDiaNoLaboralViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();
        using var respuesta = await client.PostAsJsonAsync(UrlApi("configuracion/dias-no-laborales"), modelo, cancelacion);

        return await ReenviarRespuestaAsync(respuesta, "No se pudo agregar el día no laboral.", cancelacion);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EliminarDiaNoLaboral(int id, CancellationToken cancelacion)
    {
        using var client = _http.CreateClient();
        using var respuesta = await client.DeleteAsync(UrlApi($"configuracion/dias-no-laborales/{id}"), cancelacion);

        return await ReenviarRespuestaAsync(respuesta, "No se pudo eliminar el día no laboral.", cancelacion);
    }

    // idTipoSesion vacio = crear; con valor = editar
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GuardarTipoSesion(
        int? idTipoSesion, GuardarTipoSesionViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();
        using var respuesta = idTipoSesion is null
            ? await client.PostAsJsonAsync(UrlApi("configuracion/tipos-sesion"), modelo, cancelacion)
            : await client.PutAsJsonAsync(UrlApi($"configuracion/tipos-sesion/{idTipoSesion}"), modelo, cancelacion);

        return await ReenviarRespuestaAsync(respuesta, "No se pudo guardar el tipo de sesión.", cancelacion);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> GuardarConsultorio(DatosConsultorioViewModel modelo, CancellationToken cancelacion)
    {
        if (!ModelState.IsValid)
        {
            return ErrorDeModelo();
        }

        using var client = _http.CreateClient();
        using var respuesta = await client.PutAsJsonAsync(UrlApi("configuracion/consultorio"), modelo, cancelacion);

        return await ReenviarRespuestaAsync(respuesta, "No se pudieron guardar los datos del consultorio.", cancelacion);
    }

    // ---------- auxiliares ----------

    // Como ResponderSegunApi, pero tambien en exito reenvia el JSON del API
    // (mensaje, id creado, citas que quedaron fuera del horario...)
    private static async Task<IActionResult> ReenviarRespuestaAsync(
        HttpResponseMessage respuesta, string mensajePorDefecto, CancellationToken cancelacion)
    {
        var contenido = await respuesta.Content.ReadAsStringAsync(cancelacion);

        if (string.IsNullOrWhiteSpace(contenido))
        {
            contenido = JsonSerializer.Serialize(new
            {
                mensaje = respuesta.IsSuccessStatusCode ? "Operación completada." : mensajePorDefecto
            });
        }

        return new ContentResult
        {
            Content = contenido,
            ContentType = "application/json",
            StatusCode = (int)respuesta.StatusCode
        };
    }

    #endregion

    #region Usuarios

    // La administracion de cuentas internas es solo del Administrador
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Usuarios()
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "usuarios";

        var usuarios = await cliente.GetFromJsonAsync<List<UsuarioViewModel>>(url);

        return View(usuarios ?? new List<UsuarioViewModel>());
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise los datos: el nombre, el correo y el rol son obligatorios.";
            return RedirectToAction(nameof(Usuarios));
        }

        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "usuarios";

        var respuesta = await cliente.PostAsJsonAsync(url, modelo);

        if (respuesta.IsSuccessStatusCode)
        {
            TempData["Mensaje"] = "Usuario creado. Se le envió el enlace para definir su contraseña.";
        }
        else
        {
            var error = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            TempData["Error"] = error is not null && error.TryGetValue("mensaje", out var mensaje)
                ? mensaje
                : "No se pudo crear el usuario.";
        }

        return RedirectToAction(nameof(Usuarios));
    }

    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> UsuariosExternos()
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "usuarios-externos";

        var encargados = await cliente.GetFromJsonAsync<List<UsuarioExternoViewModel>>(url);

        return View(encargados ?? new List<UsuarioExternoViewModel>());
    }

    // HU-M13-1
    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> AsignarRol(int idUsuario, int idRol)
    {
        if (idRol <= 0)
        {
            TempData["Error"] = "Seleccione un rol válido.";
            return RedirectToAction(nameof(Usuarios));
        }

        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + $"usuarios/{idUsuario}/rol";

        var respuesta = await cliente.PutAsJsonAsync(url, new { idRol });

        if (!respuesta.IsSuccessStatusCode)
        {
            var error = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, string>>();

            TempData["Error"] = error is not null && error.TryGetValue("mensaje", out var mensaje)
                ? mensaje
                : "No se pudo asignar el rol.";

            return RedirectToAction(nameof(Usuarios));
        }

        // Si se cambio el rol propio, la sesion actual sigue llevando el rol
        // viejo. Se cierra para que la persona entre de nuevo con el que le
        // acaban de poner.
        if (idUsuario == ObtenerIdUsuarioActual())
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["Mensaje"] = "Cambió su propio rol. Vuelva a iniciar sesión.";

            return RedirectToAction("Login", "Cuenta");
        }

        TempData["Mensaje"] = "El rol quedó asignado.";

        return RedirectToAction(nameof(Usuarios));
    }


    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> EditarUsuario(EditarUsuarioViewModel modelo)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Revise los datos: el nombre, el correo y el rol son obligatorios.";
            return RedirectToAction(nameof(Usuarios));
        }

        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + $"usuarios/{modelo.IdUsuario}";

        var respuesta = await cliente.PutAsJsonAsync(url, new
        {
            modelo.NombreCompleto,
            modelo.Correo,
            modelo.IdRol
        });

        await GuardarAvisoAsync(respuesta, "No se pudo guardar el usuario.");

        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> CambiarEstadoUsuario(int idUsuario, int idEstadoUsuario)
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + $"usuarios/{idUsuario}/estado";

        var respuesta = await cliente.PutAsJsonAsync(url, new { idEstadoUsuario });

        await GuardarAvisoAsync(respuesta, "No se pudo cambiar el estado de la cuenta.");

        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ReenviarInvitacion(int idUsuario)
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + $"usuarios/{idUsuario}/invitacion";

        var respuesta = await cliente.PostAsync(url, null);

        await GuardarAvisoAsync(respuesta, "No se pudo enviar el enlace.");

        return RedirectToAction(nameof(Usuarios));
    }
    private int ObtenerIdUsuarioActual()
        => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> InvitarEncargado(int idEncargado)
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + "usuarios-externos";

        var respuesta = await cliente.PostAsJsonAsync(url, new { idEncargado });

        await GuardarAvisoAsync(respuesta, "No se pudo enviar la invitación.");

        return RedirectToAction(nameof(UsuariosExternos));
    }


    [HttpPost]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> ReenviarInvitacionPortal(int idUsuario)
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + $"usuarios-externos/{idUsuario}/invitacion";

        var respuesta = await cliente.PostAsync(url, null);

        await GuardarAvisoAsync(respuesta, "No se pudo enviar el enlace.");

        return RedirectToAction(nameof(UsuariosExternos));
    }

    [HttpPost]
    [Authorize(Roles = "Administrador,Psicopedagoga")]
    public async Task<IActionResult> CambiarAccesoPortal(int idUsuario, bool suspender)
    {
        using var cliente = _http.CreateClient();

        var url = _config.GetValue<string>("Valores:UrlAPI") + $"usuarios-externos/{idUsuario}/acceso";

        var respuesta = await cliente.PutAsJsonAsync(url, new { suspender });

        await GuardarAvisoAsync(respuesta, "No se pudo cambiar el acceso al portal.");

        return RedirectToAction(nameof(UsuariosExternos));
    }

    // El aviso lo escribe el API, no esta pantalla. Importa sobre todo cuando
    // la operacion sale bien a medias: la cuenta queda creada pero el correo no
    // salio, y el texto que corresponde es ese y no "listo".
    private async Task GuardarAvisoAsync(HttpResponseMessage respuesta, string textoPorDefecto)
    {
        var cuerpo = await respuesta.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>();

        var mensaje = cuerpo is not null
            && cuerpo.TryGetValue("mensaje", out var texto)
            && texto.ValueKind == JsonValueKind.String
                ? texto.GetString()
                : null;

        if (!respuesta.IsSuccessStatusCode)
        {
            TempData["Error"] = mensaje ?? textoPorDefecto;
            return;
        }

        var correoSalio = cuerpo is null
            || !cuerpo.TryGetValue("correoEnviado", out var enviado)
            || enviado.ValueKind != JsonValueKind.False;

        if (correoSalio)
        {
            TempData["Mensaje"] = mensaje ?? "Listo.";
        }
        else
        {
            TempData["Error"] = mensaje ?? "La operación se guardó, pero el correo no salió.";
        }
    }

    #endregion

}
