// Seguimiento de sesiones (M5).
//
// Todo pasa por /Admin/..., que reenvia al API: el navegador nunca habla con el
// API directamente. El historial y el historial de avances salen de la MISMA
// consulta, solo cambia como se pintan.

(function () {
    "use strict";

    const RUTAS = {
        listar: "/Admin/ConsultarSesiones",
        detalle: "/Admin/DetalleSesion",
        buscarEstudiantes: "/Admin/BuscarEstudiantesSesion",
        citasDisponibles: "/Admin/CitasDisponiblesSesion",
        registrar: "/Admin/RegistrarSesion",
        editar: "/Admin/EditarSesion",
        inactivar: "/Admin/InactivarSesion"
    };

    // Campos de texto que se copian igual entre el formulario y el modelo
    const CAMPOS_TEXTO = [
        "Objetivo", "TemaTrabajado", "ActividadesRealizadas", "Acuerdos",
        "ObjetivosAlcanzados", "HabilidadesDesarrolladas", "Avances",
        "Recomendaciones", "Observaciones"
    ];

    let sesiones = [];
    let idSesionInactivar = null;

    // =====================================================
    // UTILIDADES
    // =====================================================

    function $(id) {
        return document.getElementById(id);
    }

    function escaparHtml(texto) {
        const d = document.createElement("div");
        d.textContent = texto ?? "";
        return d.innerHTML;
    }

    function tokenAntifalsificacion() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function datosConToken(campos) {
        const datos = new FormData();
        datos.append("__RequestVerificationToken", tokenAntifalsificacion());
        Object.entries(campos).forEach(([clave, valor]) => datos.append(clave, valor ?? ""));
        return datos;
    }

    function modal(id) {
        return bootstrap.Modal.getOrCreateInstance($(id));
    }

    function notificar(mensaje, tipo) {
        if (typeof MostrarToast === "function") MostrarToast(mensaje, tipo);
    }

    function mostrarAlerta(elemento, mensaje, tipo = "danger") {
        elemento.className = "alert alert-" + tipo;
        elemento.textContent = mensaje;
    }

    function ocultarAlerta(elemento) {
        elemento.className = "alert d-none";
        elemento.textContent = "";
    }

    function marcar(campo, valido) {
        campo.classList.toggle("is-invalid", !valido);
        return valido;
    }

    async function leerMensaje(respuesta, porDefecto) {
        try {
            const json = await respuesta.json();
            return json?.mensaje || porDefecto;
        } catch {
            return porDefecto;
        }
    }

    async function enviar(url, datos) {
        try {
            const respuesta = await fetch(url, { method: "POST", body: datos });
            if (respuesta.ok) return { ok: true };
            return { ok: false, mensaje: await leerMensaje(respuesta, "No se pudo completar la operación.") };
        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }

    // El input datetime-local maneja "2026-10-05T09:00", sin zona ni segundos
    function aValorLocal(iso) {
        if (!iso) return "";
        return String(iso).slice(0, 16);
    }

    function formatoFechaHora(iso) {
        const f = new Date(iso);
        if (isNaN(f)) return "";
        return f.toLocaleDateString("es-CR", { day: "2-digit", month: "short", year: "numeric" }) +
            ", " + f.toLocaleTimeString("es-CR", { hour: "2-digit", minute: "2-digit" });
    }

    function textoDuracion(minutos) {
        return minutos + " min";
    }

    // =====================================================
    // HISTORIAL
    // =====================================================

    function filtros() {
        const parametros = new URLSearchParams();
        const idEstudiante = buscadorFiltro?.valor;
        const desde = $("FiltroDesde").value;
        const hasta = $("FiltroHasta").value;

        if (idEstudiante) parametros.set("idEstudiante", idEstudiante);
        if (desde) parametros.set("desde", desde);
        if (hasta) parametros.set("hasta", hasta);

        return parametros.toString();
    }

    async function cargar() {
        ocultarAlerta($("AlertaSesiones"));

        try {
            const respuesta = await fetch(`${RUTAS.listar}?${filtros()}`, {
                headers: { "Accept": "application/json" }
            });

            if (!respuesta.ok) {
                sesiones = [];
                pintar();
                mostrarAlerta($("AlertaSesiones"),
                    await leerMensaje(respuesta, "No se pudo cargar el historial."));
                return;
            }

            sesiones = await respuesta.json();
            pintar();

        } catch {
            sesiones = [];
            pintar();
            mostrarAlerta($("AlertaSesiones"), "No se pudo conectar con el servidor.");
        }
    }

    function pintar() {
        pintarTabla();
        pintarAvances();
        pintarResumen();

        const vacio = sesiones.length === 0;
        $("VacioSesiones").classList.toggle("d-none", !vacio);

        // HU-M5-6 escenario 3: el mensaje distingue "no tiene ninguna" de
        // "no hay con estos filtros", que para la profesora no es lo mismo
        $("TextoVacioSesiones").textContent = hayFiltros()
            ? "No hay sesiones registradas con esos filtros."
            : "No hay sesiones registradas todavía.";
    }

    function hayFiltros() {
        return Boolean(buscadorFiltro?.valor || $("FiltroDesde").value || $("FiltroHasta").value);
    }

    function pintarResumen() {
        const n = sesiones.length;
        $("ResumenSesiones").textContent = n === 0
            ? "Sin sesiones para mostrar."
            : (n === 1 ? "1 sesión registrada." : `${n} sesiones registradas.`);
    }

    function pintarTabla() {
        $("CuerpoTablaSesiones").innerHTML = sesiones.map(function (s) {
            return `
                <tr>
                  <td>${escaparHtml(formatoFechaHora(s.fechaHoraInicio))}
                      <small class="TextoSuave d-block">${escaparHtml(textoDuracion(s.duracionMinutos))}</small></td>
                  <td>${escaparHtml(s.estudiante)}</td>
                  <td>${escaparHtml(s.tipoSesion)}</td>
                  <td><span class="badge badge-estado-activo">${escaparHtml(s.tipoAtencion)}</span></td>
                  <td>${escaparHtml(s.temaTrabajado)}</td>
                  <td class="TablaAcciones">
                    <button type="button" class="btn btn-sm btn-outline-secondary border-0"
                            data-accion="ver" data-id="${s.idSesion}" title="Ver detalle">
                      <i class="bi bi-eye"></i></button>
                    <button type="button" class="btn btn-sm btn-outline-secondary border-0"
                            data-accion="editar" data-id="${s.idSesion}" title="Editar">
                      <i class="bi bi-pencil"></i></button>
                    <button type="button" class="btn btn-sm btn-outline-danger border-0"
                            data-accion="inactivar" data-id="${s.idSesion}" title="Inactivar">
                      <i class="bi bi-slash-circle"></i></button>
                  </td>
                </tr>`;
        }).join("");
    }

    // HU-M5-5 escenario 3: los mismos datos, mostrando solo el avance
    function pintarAvances() {
        const conAvance = sesiones.filter(s =>
            s.objetivosAlcanzados || s.habilidadesDesarrolladas || s.avances || s.observaciones);

        if (conAvance.length === 0) {
            $("ListaAvances").innerHTML =
                '<p class="TextoSuave mb-0">No hay avances registrados en estas sesiones.</p>';
            return;
        }

        $("ListaAvances").innerHTML = conAvance.map(function (s) {
            return `
                <div class="Tarjeta p-3 mb-2">
                  <div class="d-flex justify-content-between flex-wrap gap-2 mb-2">
                    <strong>${escaparHtml(s.estudiante)}</strong>
                    <span class="TextoSuave">${escaparHtml(formatoFechaHora(s.fechaHoraInicio))}</span>
                  </div>
                  <dl class="row mb-0">
                    ${filaAvance("Objetivos alcanzados", s.objetivosAlcanzados)}
                    ${filaAvance("Habilidades desarrolladas", s.habilidadesDesarrolladas)}
                    ${filaAvance("Avances", s.avances)}
                    ${filaAvance("Observaciones", s.observaciones)}
                  </dl>
                </div>`;
        }).join("");
    }

    function filaAvance(etiqueta, valor) {
        if (!valor) return "";
        return `<dt class="col-sm-4 TextoSuave fw-normal">${etiqueta}</dt>
                <dd class="col-sm-8">${escaparHtml(valor)}</dd>`;
    }

    // =====================================================
    // DETALLE
    // =====================================================

    async function abrirDetalle(id) {
        try {
            const respuesta = await fetch(`${RUTAS.detalle}?id=${id}`, {
                headers: { "Accept": "application/json" }
            });

            if (!respuesta.ok) {
                notificar(await leerMensaje(respuesta, "No se pudo cargar el detalle."), "danger");
                return;
            }

            const s = await respuesta.json();

            $("DetalleSesionInactiva").classList.toggle("d-none", s.activo !== false);

            const campos = [
                ["Estudiante", s.estudiante],
                ["Fecha y hora", formatoFechaHora(s.fechaHoraInicio)],
                ["Duración", textoDuracion(s.duracionMinutos)],
                ["Tipo de sesión", s.tipoSesion],
                ["Tipo de atención", s.tipoAtencion],
                ["Cita de origen", s.idCita ? formatoFechaHora(s.fechaHoraCita) : "Sesión suelta"],
                ["Objetivo", s.objetivo],
                ["Tema trabajado", s.temaTrabajado],
                ["Actividades realizadas", s.actividadesRealizadas],
                ["Acuerdos", s.acuerdos],
                ["Objetivos alcanzados", s.objetivosAlcanzados],
                ["Habilidades desarrolladas", s.habilidadesDesarrolladas],
                ["Avances", s.avances],
                ["Recomendaciones", s.recomendaciones],
                ["Observaciones", s.observaciones],
                ["Registrada por", s.registradaPor],
                ["Última modificación", s.fechaModificacion ? formatoFechaHora(s.fechaModificacion) : "Sin cambios"]
            ];

            $("DetalleSesionCampos").innerHTML = campos.map(function ([etiqueta, valor]) {
                return `<dt class="col-sm-4 TextoSuave fw-normal">${etiqueta}</dt>
                        <dd class="col-sm-8">${escaparHtml(valor || "—")}</dd>`;
            }).join("");

            modal("ModalDetalleSesion").show();

        } catch {
            notificar("No se pudo conectar con el servidor.", "danger");
        }
    }

    // =====================================================
    // REGISTRAR
    // =====================================================

    const buscadorSesion = crearBuscadorEstudiantes($("BuscadorEstudianteSesion"), {
        idEntrada: "BuscadorNuevaSesion",
        nombreCampo: "IdEstudiante",
        url: RUTAS.buscarEstudiantes,
        alCambiar: function (estudiante) {
            if (estudiante) cargarCitasDisponibles(); else limpiarCitasDisponibles();
        }
    });

    const buscadorFiltro = crearBuscadorEstudiantes($("BuscadorEstudianteFiltro"), {
        idEntrada: "BuscadorFiltroSesion",
        nombreCampo: "IdEstudianteFiltro",
        placeholder: "Todos los estudiantes",
        url: RUTAS.buscarEstudiantes,
        alCambiar: cargar
    });

    function limpiarCitasDisponibles() {
        const select = $("CitaOrigenSesion");
        select.innerHTML = '<option value="">Sin cita (sesión suelta)</option>';
        select.disabled = true;
    }

    async function cargarCitasDisponibles() {
        const select = $("CitaOrigenSesion");
        limpiarCitasDisponibles();

        if (!buscadorSesion.valor) return;

        try {
            const respuesta = await fetch(
                `${RUTAS.citasDisponibles}?idEstudiante=${buscadorSesion.valor}`,
                { headers: { "Accept": "application/json" } });

            if (!respuesta.ok) return;

            const citas = await respuesta.json();

            citas.forEach(function (c) {
                const opcion = document.createElement("option");
                opcion.value = c.idCita;
                opcion.textContent = `${formatoFechaHora(c.fechaHoraInicio)} — ${c.tipoSesion}`;
                opcion.dataset.inicio = c.fechaHoraInicio;
                opcion.dataset.duracion = c.duracionMinutos;
                opcion.dataset.tipo = c.idTipoSesion;
                select.appendChild(opcion);
            });

            select.disabled = citas.length === 0;

        } catch {
            // Sin citas el formulario sigue sirviendo: la sesion queda suelta
        }
    }

    // Elegir la cita llena fecha, duracion y tipo, pero se pueden cambiar: la
    // sesion real pudo durar distinto de lo agendado
    function alElegirCita() {
        const opcion = $("CitaOrigenSesion").selectedOptions[0];
        if (!opcion || !opcion.value) return;

        $("FechaHoraNuevaSesion").value = aValorLocal(opcion.dataset.inicio);
        $("DuracionNuevaSesion").value = opcion.dataset.duracion;
        $("TipoSesionNuevaSesion").value = opcion.dataset.tipo;
    }

    function validarFormulario(prefijo) {
        let valido = true;

        if (prefijo === "nueva") {
            valido = buscadorSesion.validar() && valido;
        }

        const fecha = prefijo === "nueva" ? $("FechaHoraNuevaSesion") : $("EditarFechaHoraSesion");
        const duracion = prefijo === "nueva" ? $("DuracionNuevaSesion") : $("EditarDuracionSesion");
        const tipoSesion = prefijo === "nueva" ? $("TipoSesionNuevaSesion") : $("EditarTipoSesionSesion");
        const tipoAtencion = prefijo === "nueva" ? $("TipoAtencionNuevaSesion") : $("EditarTipoAtencionSesion");
        const objetivo = prefijo === "nueva" ? $("ObjetivoNuevaSesion") : $("EditarObjetivoSesion");
        const tema = prefijo === "nueva" ? $("TemaNuevaSesion") : $("EditarTemaSesion");

        valido = marcar(fecha, fecha.value !== "") && valido;
        valido = marcar(duracion, duracion.value >= 15 && duracion.value <= 240) && valido;
        valido = marcar(tipoSesion, tipoSesion.value !== "") && valido;
        valido = marcar(tipoAtencion, tipoAtencion.value !== "") && valido;
        valido = marcar(objetivo, objetivo.value.trim() !== "") && valido;
        valido = marcar(tema, tema.value.trim() !== "") && valido;

        return valido;
    }

    function camposDelFormulario(sufijo) {
        const datos = {};

        CAMPOS_TEXTO.forEach(function (campo) {
            const elemento = document.querySelector(`#${sufijo} [name="${campo}"]`);
            datos[campo] = elemento ? elemento.value : "";
        });

        return datos;
    }

    async function guardarNueva(evento) {
        evento.preventDefault();
        ocultarAlerta($("AlertaNuevaSesion"));

        if (!validarFormulario("nueva")) return;

        const datos = datosConToken(Object.assign({
            IdEstudiante: buscadorSesion.valor,
            IdCita: $("CitaOrigenSesion").value,
            IdTipoSesion: $("TipoSesionNuevaSesion").value,
            IdTipoAtencion: $("TipoAtencionNuevaSesion").value,
            FechaHoraInicio: $("FechaHoraNuevaSesion").value,
            DuracionMinutos: $("DuracionNuevaSesion").value
        }, camposDelFormulario("FormularioNuevaSesion")));

        const resultado = await enviar(RUTAS.registrar, datos);

        if (!resultado.ok) {
            mostrarAlerta($("AlertaNuevaSesion"), resultado.mensaje);
            return;
        }

        modal("ModalNuevaSesion").hide();
        notificar("Sesión registrada correctamente.", "success");
        await cargar();
    }

    // =====================================================
    // EDITAR
    // =====================================================

    async function abrirEditar(id) {
        ocultarAlerta($("AlertaEditarSesion"));

        try {
            const respuesta = await fetch(`${RUTAS.detalle}?id=${id}`, {
                headers: { "Accept": "application/json" }
            });

            if (!respuesta.ok) {
                notificar(await leerMensaje(respuesta, "No se pudo cargar la sesión."), "danger");
                return;
            }

            const s = await respuesta.json();

            $("FormularioEditarSesion").dataset.id = s.idSesion;
            $("EditarSesionEstudiante").textContent =
                `${s.estudiante} · el estudiante y la cita de origen no se cambian al editar.`;

            $("EditarFechaHoraSesion").value = aValorLocal(s.fechaHoraInicio);
            $("EditarDuracionSesion").value = s.duracionMinutos;
            $("EditarTipoSesionSesion").value = s.idTipoSesion;
            $("EditarTipoAtencionSesion").value = s.idTipoAtencion;

            $("EditarObjetivoSesion").value = s.objetivo ?? "";
            $("EditarTemaSesion").value = s.temaTrabajado ?? "";
            $("EditarActividadesSesion").value = s.actividadesRealizadas ?? "";
            $("EditarAcuerdosSesion").value = s.acuerdos ?? "";
            $("EditarObjetivosAlcanzadosSesion").value = s.objetivosAlcanzados ?? "";
            $("EditarHabilidadesSesion").value = s.habilidadesDesarrolladas ?? "";
            $("EditarAvancesSesion").value = s.avances ?? "";
            $("EditarRecomendacionesSesion").value = s.recomendaciones ?? "";
            $("EditarObservacionesSesion").value = s.observaciones ?? "";

            modal("ModalEditarSesion").show();

        } catch {
            notificar("No se pudo conectar con el servidor.", "danger");
        }
    }

    async function guardarEdicion(evento) {
        evento.preventDefault();
        ocultarAlerta($("AlertaEditarSesion"));

        if (!validarFormulario("editar")) return;

        const id = $("FormularioEditarSesion").dataset.id;

        const datos = datosConToken(Object.assign({
            id: id,
            IdTipoSesion: $("EditarTipoSesionSesion").value,
            IdTipoAtencion: $("EditarTipoAtencionSesion").value,
            FechaHoraInicio: $("EditarFechaHoraSesion").value,
            DuracionMinutos: $("EditarDuracionSesion").value
        }, camposDelFormulario("FormularioEditarSesion")));

        const resultado = await enviar(RUTAS.editar, datos);

        if (!resultado.ok) {
            mostrarAlerta($("AlertaEditarSesion"), resultado.mensaje);
            return;
        }

        modal("ModalEditarSesion").hide();
        notificar("Sesión actualizada correctamente.", "success");
        await cargar();
    }

    // =====================================================
    // INACTIVAR
    // =====================================================

    function abrirInactivar(id) {
        const s = sesiones.find(x => x.idSesion === Number(id));
        if (!s) return;

        idSesionInactivar = id;
        ocultarAlerta($("AlertaInactivarSesion"));
        $("NombreSesionInactivar").textContent =
            `${s.estudiante}, ${formatoFechaHora(s.fechaHoraInicio)}`;

        modal("ModalInactivarSesion").show();
    }

    async function confirmarInactivar() {
        if (!idSesionInactivar) return;

        const resultado = await enviar(RUTAS.inactivar, datosConToken({ id: idSesionInactivar }));

        if (!resultado.ok) {
            mostrarAlerta($("AlertaInactivarSesion"), resultado.mensaje);
            return;
        }

        modal("ModalInactivarSesion").hide();
        notificar("Sesión inactivada correctamente.", "success");
        idSesionInactivar = null;
        await cargar();
    }

    // =====================================================
    // ARRANQUE
    // =====================================================

    $("CuerpoTablaSesiones").addEventListener("click", function (evento) {
        const boton = evento.target.closest("button[data-accion]");
        if (!boton) return;

        const id = boton.dataset.id;

        if (boton.dataset.accion === "ver") abrirDetalle(id);
        if (boton.dataset.accion === "editar") abrirEditar(id);
        if (boton.dataset.accion === "inactivar") abrirInactivar(id);
    });

    $("CitaOrigenSesion").addEventListener("change", alElegirCita);
    $("FormularioNuevaSesion").addEventListener("submit", guardarNueva);
    $("FormularioEditarSesion").addEventListener("submit", guardarEdicion);
    $("BotonConfirmarInactivarSesion").addEventListener("click", confirmarInactivar);

    $("FiltroDesde").addEventListener("change", cargar);
    $("FiltroHasta").addEventListener("change", cargar);

    $("BotonLimpiarFiltros").addEventListener("click", function () {
        buscadorFiltro.limpiar();
        $("FiltroDesde").value = "";
        $("FiltroHasta").value = "";
        cargar();
    });

    // Al cerrar el modal de registro se deja limpio para la proxima vez
    $("ModalNuevaSesion").addEventListener("hidden.bs.modal", function () {
        $("FormularioNuevaSesion").reset();
        $("FormularioNuevaSesion").querySelectorAll(".is-invalid")
            .forEach(c => c.classList.remove("is-invalid"));
        buscadorSesion.limpiar();
        limpiarCitasDisponibles();
        ocultarAlerta($("AlertaNuevaSesion"));
    });

    $("ModalNuevaSesion").addEventListener("shown.bs.modal", () => buscadorSesion.enfocar());

    cargar();
})();
