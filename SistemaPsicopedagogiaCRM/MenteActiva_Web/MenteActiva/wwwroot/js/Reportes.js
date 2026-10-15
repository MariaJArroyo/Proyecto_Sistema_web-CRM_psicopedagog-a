// Reportes de estudiante (M7): listado y detalle (HU-M7-4).
//
// Todo pasa por /Admin/..., que reenvia al API: el navegador nunca habla con el
// API directamente.

(function () {
    "use strict";

    const RUTAS = {
        listar: "/Admin/ConsultarReportes",
        detalle: "/Admin/DetalleReporte"
    };

    let reportes = [];
    let paginaActual = 1;
    let cantidadPorPagina = 10;

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

    function mostrarAlerta(mensaje, tipo = "danger") {
        const alerta = $("AlertaReportes");
        alerta.className = "alert alert-" + tipo + " mt-3";
        alerta.textContent = mensaje;
    }

    function ocultarAlerta() {
        const alerta = $("AlertaReportes");
        alerta.className = "alert d-none mt-3";
        alerta.textContent = "";
    }

    async function leerMensaje(respuesta, porDefecto) {
        try {
            const json = await respuesta.json();
            return json?.mensaje || porDefecto;
        } catch {
            return porDefecto;
        }
    }

    function formatoFecha(iso) {
        const f = new Date(iso);
        if (isNaN(f)) return "";
        return f.toLocaleDateString("es-CR", { day: "2-digit", month: "short", year: "numeric" });
    }

    function formatoFechaHora(iso) {
        const f = new Date(iso);
        if (isNaN(f)) return "";
        return formatoFecha(iso) + ", " +
            f.toLocaleTimeString("es-CR", { hour: "2-digit", minute: "2-digit" });
    }

    function textoPeriodo(r) {
        return formatoFecha(r.periodoInicio) + " – " + formatoFecha(r.periodoFin);
    }

    // =====================================================
    // LISTADO
    // =====================================================

    async function cargar() {
        paginaActual = 1;
        ocultarAlerta();

        try {
            const respuesta = await fetch(RUTAS.listar, { headers: { "Accept": "application/json" } });

            if (!respuesta.ok) {
                reportes = [];
                pintar();
                mostrarAlerta(await leerMensaje(respuesta, "No se pudieron cargar los reportes."));
                return;
            }

            reportes = await respuesta.json();
            pintar();

        } catch {
            reportes = [];
            pintar();
            mostrarAlerta("No se pudo conectar con el servidor.");
        }
    }

    function pintar() {
        const n = reportes.length;
        const totalPaginas = Math.max(1, Math.ceil(n / cantidadPorPagina));
        paginaActual = Math.max(1, Math.min(paginaActual, totalPaginas));
        const inicio = (paginaActual - 1) * cantidadPorPagina;
        const fin = Math.min(inicio + cantidadPorPagina, n);
        const reportesPagina = reportes.slice(inicio, fin);

        $("RangoReportes").textContent = n === 0
            ? "0 de 0"
            : `${inicio + 1}–${fin} de ${n}`;
        $("PaginaReportes").textContent = `Página ${paginaActual} de ${totalPaginas}`;
        $("AnteriorReportes").disabled = paginaActual <= 1;
        $("SiguienteReportes").disabled = paginaActual >= totalPaginas;

        $("ResumenReportes").textContent = n === 0
            ? "Sin reportes para mostrar."
            : (n === 1 ? "1 reporte generado." : `${n} reportes generados.`);

        $("VacioReportes").classList.toggle("d-none", n !== 0);
        $("TablaReportes").classList.toggle("d-none", n === 0);

        $("CuerpoTablaReportes").innerHTML = reportesPagina.map(function (r) {
            return `
                <tr>
                  <td>${escaparHtml(r.titulo)}</td>
                  <td>${escaparHtml(r.estudiante)}</td>
                  <td>${escaparHtml(textoPeriodo(r))}</td>
                  <td>${escaparHtml(formatoFechaHora(r.fechaGeneracion))}</td>
                  <td>${escaparHtml(r.generadoPor)}</td>
                  <td class="TablaAcciones">
                    <button type="button" class="btn btn-sm btn-outline-secondary border-0"
                            data-accion="ver" data-id="${r.idReporte}" title="Ver detalle">
                      <i class="bi bi-eye"></i></button>
                  </td>
                </tr>`;
        }).join("");
    }

    // =====================================================
    // DETALLE (HU-M7-4)
    // =====================================================

    async function abrirDetalle(id) {
        ocultarAlerta();

        try {
            const respuesta = await fetch(`${RUTAS.detalle}?id=${encodeURIComponent(id)}`, {
                headers: { "Accept": "application/json" }
            });

            // Escenario 3: no existe o esta deshabilitado. Se avisa y se
            // refresca la lista, porque ese reporte ya no deberia estar ahi.
            if (!respuesta.ok) {
                const mensaje = await leerMensaje(respuesta, "El reporte solicitado no está disponible.");
                await cargar();
                mostrarAlerta(mensaje, "warning");
                return;
            }

            pintarDetalle(await respuesta.json());
            bootstrap.Modal.getOrCreateInstance($("ModalDetalleReporte")).show();

        } catch {
            mostrarAlerta("No se pudo conectar con el servidor.");
        }
    }

    // Escenarios 1 y 2: toda la informacion del reporte, con fecha de
    // generacion, estudiante y quien lo genero.
    function pintarDetalle(r) {
        $("TituloDetalleReporte").textContent = r.titulo;

        const datos = [
            ["Estudiante", r.estudiante],
            ["Período", textoPeriodo(r)],
            ["Fecha de generación", formatoFechaHora(r.fechaGeneracion)],
            ["Generado por", r.generadoPor],
            ["Visible en el portal", r.visibleEnPortal ? "Sí" : "No"]
        ];

        $("DatosDetalleReporte").innerHTML = datos.map(function ([etiqueta, valor]) {
            return `
                <dt class="col-sm-4 TextoSuave fw-normal">${escaparHtml(etiqueta)}</dt>
                <dd class="col-sm-8">${escaparHtml(valor)}</dd>`;
        }).join("");

        $("ContenidoDetalleReporte").innerHTML = htmlContenido(r.contenido);
    }

    // El contenido es la foto guardada al generar el reporte: JSON con
    // secciones o, en los reportes anteriores, texto plano.
    function htmlContenido(contenido) {
        if (!contenido || !contenido.trim()) {
            return '<p class="TextoSuave mb-0">Este reporte no tiene contenido registrado.</p>';
        }

        let datos = null;
        try {
            datos = JSON.parse(contenido);
        } catch {
            datos = null;
        }

        if (datos === null || typeof datos !== "object" || Array.isArray(datos)) {
            return `<p class="mb-0" style="white-space: pre-wrap;">${escaparHtml(contenido)}</p>`;
        }

        return [
            seccionEstudiante(datos.estudiante),
            seccionSesiones(datos.sesiones),
            seccionPlanes(datos.planes),
            seccionPagos(datos.pagos)
        ].join("");
    }

    function filaDato(etiqueta, valor) {
        if (!valor) return "";
        return `<p class="mb-1"><span class="TextoSuave">${escaparHtml(etiqueta)}:</span> ${escaparHtml(valor)}</p>`;
    }

    function seccionEstudiante(e) {
        if (!e) return "";
        return `
            <h6 class="mb-2">Datos del estudiante</h6>
            <div class="mb-3">
              ${filaDato("Nombre", e.nombre)}
              ${filaDato("Nivel educativo", e.nivel)}
              ${filaDato("Institución", e.institucion)}
              ${filaDato("Áreas de dificultad", e.areasDificultad)}
            </div>`;
    }

    function notaReporte(etiqueta, valor) {
        if (valor === null || valor === undefined || String(valor).trim() === "") return "";
        return `<div class="mb-3">
            <div class="TextoSuave small fw-semibold">${escaparHtml(etiqueta)}</div>
            <div style="white-space: pre-wrap; overflow-wrap: anywhere;">${escaparHtml(String(valor))}</div>
        </div>`;
    }

    function seccionSesiones(sesiones) {
        if (!Array.isArray(sesiones)) return "";
        if (!sesiones.length) {
            return '<h6>Sesiones</h6><p class="TextoSuave mb-3">Sin sesiones en el período.</p>';
        }
        const ordenadas = [...sesiones].sort((a, b) =>
            String(b.fecha ?? "").localeCompare(String(a.fecha ?? "")));
        return `<h6 class="mb-3">Historial y notas de sesiones</h6>` +
            ordenadas.map(s => `<article class="border rounded p-3 mb-3">
                <div class="d-flex flex-wrap justify-content-between gap-2 mb-3">
                    <strong>${escaparHtml(formatoFecha(s.fecha))}</strong>
                    <span class="TextoSuave">${escaparHtml(s.tipoAtencion)}</span>
                </div>
                ${notaReporte("Duración", s.duracionMinutos != null ? `${s.duracionMinutos} minutos` : null)}
                ${notaReporte("Objetivo de la sesión", s.objetivo)}
                ${notaReporte("Tema trabajado", s.tema)}
                ${notaReporte("Actividades realizadas", s.actividadesRealizadas)}
                ${notaReporte("Avances", s.avances)}
                ${notaReporte("Objetivos alcanzados", s.objetivosAlcanzados)}
                ${notaReporte("Habilidades desarrolladas", s.habilidadesDesarrolladas)}
                ${notaReporte("Acuerdos", s.acuerdos)}
                ${notaReporte("Recomendaciones", s.recomendaciones)}
                ${notaReporte("Observaciones de la sesión", s.observaciones)}
            </article>`).join("");
    }

    function seccionPlanes(planes) {
        if (!Array.isArray(planes)) return "";
        if (!planes.length) {
            return '<h6>Planes de intervención</h6><p class="TextoSuave mb-3">Sin planes de intervención.</p>';
        }
        return `<h6 class="mb-3">Planes de intervención</h6>` +
            planes.map(p => `<article class="border rounded p-3 mb-3">
                <div class="d-flex flex-wrap justify-content-between gap-2 mb-3">
                    <strong>${escaparHtml(p.titulo)}</strong>
                    <span class="badge bg-light text-dark border">${escaparHtml(p.estado)}</span>
                </div>
                ${notaReporte("Objetivo general", p.objetivoGeneral)}
                ${notaReporte("Fecha de inicio", p.fechaInicio ? formatoFecha(p.fechaInicio) : null)}
                ${notaReporte("Fecha de finalización", p.fechaFin ? formatoFecha(p.fechaFin) : null)}
                ${notaReporte("Observaciones del plan", p.observaciones)}
                ${notaReporte("Cumplimiento", p.actividades != null
                ? `${p.actividadesCompletadas ?? 0} de ${p.actividades} actividades completadas` : null)}
                ${estrategiasReporte(p.estrategias)}
            </article>`).join("");
    }

    function estrategiasReporte(estrategias) {
        if (!Array.isArray(estrategias)) return "";
        if (!estrategias.length) return '<p class="TextoSuave mb-0">Sin estrategias registradas.</p>';
        const ordenadas = [...estrategias].sort((a, b) =>
            (a.orden ?? Number.MAX_SAFE_INTEGER) - (b.orden ?? Number.MAX_SAFE_INTEGER));
        return ordenadas.map((e, indice) => {
            const actividades = Array.isArray(e.actividades) ? e.actividades : [];
            return `<div class="border-top pt-3 mt-3">
                <div class="fw-semibold mb-2">Estrategia ${indice + 1}</div>
                ${notaReporte("Descripción", e.descripcion)}
                ${actividades.length ? `<ul class="list-unstyled mb-0">` + actividades.map(a => {
                const completada = a.completada === true || a.completada === 1;
                return `<li class="mb-3">
                        <span class="badge ${completada ? "bg-success" : "bg-secondary"}">
                            ${completada ? "Completada" : "Pendiente"}
                        </span>
                        <span class="fw-semibold">${escaparHtml(a.nombre)}</span>
                        ${notaReporte("Detalle de la actividad", a.descripcion)}
                    </li>`;
            }).join("") + `</ul>` : '<p class="TextoSuave mb-0">Sin actividades registradas.</p>'}
            </div>`;
        }).join("");
    }
    function seccionPagos(p) {
        if (!p) return "";
        const moneda = (v) => "₡" + Number(v ?? 0).toLocaleString("es-CR");

        return `
            <h6 class="mb-2">Estado de pagos</h6>
            <div class="mb-0">
              ${filaDato("Total cobrado", moneda(p.totalCobrado))}
              ${filaDato("Total abonado", moneda(p.totalAbonado))}
              ${filaDato("Saldo", moneda(p.saldo))}
            </div>`;
    }

    // =====================================================
    // ARRANQUE
    // =====================================================

    $("CantidadReportes").addEventListener("change", function (evento) {
        const cantidad = Number(evento.target.value);
        cantidadPorPagina = [5, 10, 25, 50].includes(cantidad) ? cantidad : 10;
        paginaActual = 1;
        pintar();
    });

    $("AnteriorReportes").addEventListener("click", function () {
        if (paginaActual > 1) {
            paginaActual--;
            pintar();
        }
    });

    $("SiguienteReportes").addEventListener("click", function () {
        const totalPaginas = Math.ceil(reportes.length / cantidadPorPagina);
        if (paginaActual < totalPaginas) {
            paginaActual++;
            pintar();
        }
    });
    $("CuerpoTablaReportes").addEventListener("click", function (evento) {
        const boton = evento.target.closest("button[data-accion='ver']");
        if (boton) abrirDetalle(boton.dataset.id);
    });

    let solicitudFicha = null;
    let fichaCargada = false;
    let generandoReporte = false;
    let tituloAutomatico = "";

    function alertaGeneracion(mensaje, tipo = "warning") {
        const alerta = $("AlertaGenerarReporte");
        alerta.className = `alert alert-${tipo}`;
        alerta.textContent = mensaje;
    }

    function limpiarAlertaGeneracion() {
        $("AlertaGenerarReporte").className = "alert d-none";
        $("AlertaGenerarReporte").textContent = "";
    }

    async function cargarEstudiantesReporte() {
        solicitudFicha?.abort();
        fichaCargada = false;
        $("GenerarReporteBtn").disabled = true;
        $("FichaEstudianteReporte").classList.add("d-none");
        const selector = $("EstudianteReporte");
        const actualizar = $("RecargarEstudiantesReporte");
        selector.disabled = true;
        actualizar.disabled = true;
        selector.replaceChildren(new Option("Cargando estudiantes…", ""));
        limpiarAlertaGeneracion();
        try {
            const formulario = $("FormularioGenerarReporte");
            const respuesta = await fetch(formulario.dataset.urlEstudiantes, {
                headers: { "Accept": "application/json" }
            });
            if (!respuesta.ok) throw new Error(await leerMensaje(respuesta, "No se pudieron cargar los estudiantes."));
            const estudiantes = await respuesta.json();
            if (!Array.isArray(estudiantes)) throw new Error("La lista de estudiantes no es válida.");
            selector.replaceChildren(new Option("Seleccione un estudiante", ""));
            [...estudiantes].sort((a, b) => a.estudiante.localeCompare(b.estudiante, "es"))
                .forEach(e => selector.add(new Option(`${e.estudiante} · ID ${e.idEstudiante}`, e.idEstudiante)));
            selector.disabled = estudiantes.length === 0;
            if (!estudiantes.length) alertaGeneracion("No hay estudiantes disponibles.");
        } catch (error) {
            selector.replaceChildren(new Option("No se pudieron cargar los estudiantes", ""));
            alertaGeneracion(error.message || "No se pudo cargar la lista.");
        } finally {
            actualizar.disabled = false;
        }
    }

    async function seleccionarEstudianteReporte() {
        solicitudFicha?.abort();
        fichaCargada = false;
        $("GenerarReporteBtn").disabled = true;
        $("FichaEstudianteReporte").classList.add("d-none");
        $("DatosEstudianteReporte").innerHTML = "";
        limpiarAlertaGeneracion();
        const id = $("EstudianteReporte").value;
        if (!id) return;
        const control = new AbortController();
        solicitudFicha = control;
        alertaGeneracion("Cargando información del estudiante…", "info");
        try {
            const url = new URL($("FormularioGenerarReporte").dataset.urlFicha, window.location.href);
            url.searchParams.set("id", id);
            const respuesta = await fetch(url, {
                signal: control.signal, headers: { "Accept": "application/json" }
            });
            if (!respuesta.ok) throw new Error(await leerMensaje(respuesta, "No se pudo cargar la ficha del estudiante."));
            const e = await respuesta.json();
            if (control.signal.aborted || $("EstudianteReporte").value !== id) return;
            const nombre = [e.nombre, e.primerApellido, e.segundoApellido].filter(Boolean).join(" ");
            const areas = e.areasDificultad || (Array.isArray(e.areas)
                ? e.areas.map(a => a.nombreArea || a.nombre).filter(Boolean).join(", ") : "");
            const encargados = Array.isArray(e.encargados)
                ? e.encargados.map(x => `${x.nombreCompleto}${x.parentesco ? ` (${x.parentesco})` : ""}`).join("; ") : "";
            $("DatosEstudianteReporte").innerHTML = [
                notaReporte("Nombre", nombre),
                notaReporte("Fecha de nacimiento", e.fechaNacimiento ? formatoFecha(e.fechaNacimiento) : null),
                notaReporte("Nivel educativo", e.nivelEducativo),
                notaReporte("Institución", e.institucion),
                notaReporte("Áreas de dificultad", areas),
                notaReporte("Necesidades de apoyo", e.necesidadesApoyo),
                notaReporte("Observaciones", e.observaciones),
                notaReporte("Personas encargadas", encargados)
            ].join("");
            const titulo = $("TituloReporte");
            if (!titulo.value.trim() || titulo.value === tituloAutomatico) {
                tituloAutomatico = `Reporte de progreso - ${nombre}`.slice(0, 150);
                titulo.value = tituloAutomatico;
            }
            $("FichaEstudianteReporte").classList.remove("d-none");
            fichaCargada = true;
            $("GenerarReporteBtn").disabled = false;
            limpiarAlertaGeneracion();
        } catch (error) {
            if (error.name === "AbortError" || control.signal.aborted) return;
            alertaGeneracion(error.message || "No se pudo cargar la ficha.");
        }
    }

    async function cerrarModalGeneracion() {
        const modal = $("ModalGenerarReporte");
        if (!modal.classList.contains("show")) return;
        await new Promise(resolve => {
            modal.addEventListener("hidden.bs.modal", resolve, { once: true });
            bootstrap.Modal.getOrCreateInstance(modal).hide();
        });
    }
    async function generarReporte(evento) {
        evento.preventDefault();
        const formulario = evento.currentTarget;
        if (!formulario.reportValidity()) return;
        if ($("FinReporte").value < $("InicioReporte").value) {
            alertaGeneracion("La fecha final debe ser igual o posterior a la inicial.");
            return;
        }
        if (!$("EstudianteReporte").value || !fichaCargada) {
            alertaGeneracion("Seleccione un estudiante y espere a que cargue su ficha.");
            return;
        }
        const boton = $("GenerarReporteBtn");
        if (boton.disabled) return;
        const datosFormulario = new FormData(formulario);
        generandoReporte = true;
        $("EstudianteReporte").disabled = true;
        $("RecargarEstudiantesReporte").disabled = true;
        boton.disabled = true;
        boton.textContent = "Generando…";
        limpiarAlertaGeneracion();
        let guardado = false;
        try {
            const respuesta = await fetch(formulario.action, {
                method: "POST",
                body: datosFormulario,
                headers: { "Accept": "application/json" }
            });
            if (!respuesta.ok) {
                alertaGeneracion(await leerMensaje(respuesta, "No se pudo generar el reporte."));
                return;
            }
            guardado = true;
            const reporte = await respuesta.json();
            await cargar();
            generandoReporte = false;
            await cerrarModalGeneracion();
            pintarDetalle(reporte);
            bootstrap.Modal.getOrCreateInstance($("ModalDetalleReporte")).show();
            mostrarAlerta("Reporte generado correctamente. Puede revisar la vista previa.", "success");
        } catch {
            alertaGeneracion(guardado
                ? "El reporte se guardó, pero no se pudo abrir la vista previa. Actualice el listado para consultarlo."
                : "No se pudo confirmar la generación. Revise el listado antes de intentar nuevamente.");
        } finally {
            generandoReporte = false;
            $("EstudianteReporte").disabled = false;
            $("RecargarEstudiantesReporte").disabled = false;
            boton.disabled = false;
            boton.textContent = "Generar reporte";
        }
    }

    $("EstudianteReporte").addEventListener("change", seleccionarEstudianteReporte);
    $("RecargarEstudiantesReporte").addEventListener("click", cargarEstudiantesReporte);
    $("FormularioGenerarReporte").addEventListener("submit", generarReporte);
    $("ModalGenerarReporte").addEventListener("hide.bs.modal", evento => {
        if (generandoReporte) evento.preventDefault();
    });
    cargarEstudiantesReporte();
    cargar();
})();


