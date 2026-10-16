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
    let filtrosAplicados = { buscar: "", estado: "activos" };
    let solicitudReportes = null;
    let temporizadorBusqueda = null;
    let cambioEstado = null;
    let guardandoEstado = false;

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

    function fechaParaApi(texto) {
        const partes = /^(\d{2})-(\d{2})-(\d{4})$/.exec(texto.trim());
        if (!partes) return null;
        const [, dia, mes, anio] = partes;
        const d = Number(dia), m = Number(mes), a = Number(anio);
        if (a < 1000 || m < 1 || m > 12 || d < 1) return null;
        if (d > new Date(Date.UTC(a, m, 0)).getUTCDate()) return null;
        return anio + "-" + mes + "-" + dia;
    }

    function formatoFecha(iso) {
        if (!iso) return "";
        const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(iso));
        return partes ? `${partes[3]}-${partes[2]}-${partes[1]}` : "";
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

    function hayFiltrosAplicados() {
        return Boolean(filtrosAplicados.buscar || filtrosAplicados.estado !== "activos");
    }

    async function cargar() {
        solicitudReportes?.abort();
        const control = new AbortController();
        solicitudReportes = control;
        paginaActual = 1;
        ocultarAlerta();
        const parametros = new URLSearchParams();
        if (filtrosAplicados.buscar) parametros.set("buscar", filtrosAplicados.buscar);
        parametros.set("estado", filtrosAplicados.estado);
        parametros.set("orden", "az");
        $("TablaReportes").setAttribute("aria-busy", "true");
        $("CargandoReportes").classList.remove("d-none");
        try {
            const respuesta = await fetch(RUTAS.listar + (parametros.size ? "?" + parametros.toString() : ""), {
                signal: control.signal, headers: { "Accept": "application/json" }
            });
            if (!respuesta.ok) throw new Error(await leerMensaje(respuesta, "No se pudieron cargar los reportes."));
            const datos = await respuesta.json();
            if (control.signal.aborted || solicitudReportes !== control) return;
            if (!Array.isArray(datos)) throw new Error("La respuesta de reportes no es válida.");
            reportes = datos;
            pintar();
        } catch (error) {
            if (error.name === "AbortError" || control.signal.aborted || solicitudReportes !== control) return;
            reportes = [];
            pintar();
            $("VacioReportes").classList.add("d-none");
            $("ResumenReportes").textContent = "No se pudo cargar el listado.";
            mostrarAlerta(error.message || "No se pudo conectar con el servidor.");
        } finally {
            if (solicitudReportes === control) {
                $("TablaReportes").setAttribute("aria-busy", "false");
                $("CargandoReportes").classList.add("d-none");
            }
        }
    }

    async function aplicarFiltrosReportes(evento) {
        evento.preventDefault();
        clearTimeout(temporizadorBusqueda);
        if (!evento.currentTarget.reportValidity()) return;
        filtrosAplicados = {
            buscar: $("BuscarNombreReporte").value.trim(),
            estado: $("EstadoFiltroReporte").value
        };
        await cargar();
    }

    function buscarConFiltrosActuales() {
        return aplicarFiltrosReportes({ preventDefault() {}, currentTarget: $("FormularioFiltrosReportes") });
    }

    function pintar() {
        const n = reportes.length;
        $("MensajeVacioReportes").textContent = hayFiltrosAplicados()
            ? "No se encontraron reportes con los criterios seleccionados."
            : "No hay reportes activos para mostrar.";
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
            const activo = r.activo === true;

            return `
        <tr>
            <td>${escaparHtml(r.titulo)}</td>
            <td>${escaparHtml(r.estudiante)}</td>
            <td>${escaparHtml(r.generadoPor)}</td>

            <td class="TablaAcciones text-nowrap">
                <button type="button" class="btn btn-sm btn-outline-secondary border-0" data-accion="ver" data-id="${escaparHtml(r.idReporte)}" 
                title="Consultar reporte" aria-label="Consultar reporte" ${activo ? "" : "disabled"}>
                    <i class="bi bi-eye" aria-hidden="true"></i>
                </button>

                <button type="button" class="btn btn-sm text-danger border-0" data-accion="pdf" data-id="${escaparHtml(r.idReporte)}" 
                title="Descargar PDF" aria-label="Descargar reporte en PDF" ${activo ? "" : "disabled"}>
                    <i class="bi bi-file-earmark-pdf" aria-hidden="true"></i>
                </button>

                <button type="button" class="btn btn-sm border-0 ${activo ? "text-primary" : "text-secondary"}" 
                data-accion="estado" data-id="${escaparHtml(r.idReporte)}" role="switch" aria-checked="${activo}" 
                title="${activo ? "Desactivar reporte" : "Reactivar reporte"}" aria-label="${activo ? "Desactivar reporte" : "Reactivar reporte"}">
                    <i class="bi ${activo ? "bi-toggle-on" : "bi-toggle-off"} fs-5" aria-hidden="true"></i>
                </button>
            </td>
        </tr>`;
        }).join("");


    }

    // =====================================================
    // DETALLE (HU-M7-4)
    // =====================================================

    function abrirCambioEstadoReporte(id) {
        const reporte = reportes.find(r => r.idReporte === id);
        if (!reporte || guardandoEstado) return;
        cambioEstado = { id: reporte.idReporte, activo: !reporte.activo };
        $("TituloEstadoReporte").textContent = cambioEstado.activo ? "Reactivar reporte" : "Desactivar reporte";
        $("NombreEstadoReporte").textContent = reporte.titulo;
        $("MensajeEstadoReporte").textContent = cambioEstado.activo
            ? "El reporte volverá a estar disponible. Si estaba marcado como visible en el portal, también volverá a mostrarse allí."
            : "El reporte se conservará en el historial, pero dejará de estar disponible en el listado de activos y en el portal.";
        $("AlertaEstadoReporte").className = "alert d-none";
        $("AlertaEstadoReporte").textContent = "";
        $("ConfirmarEstadoReporte").textContent = cambioEstado.activo ? "Reactivar" : "Desactivar";
        $("ConfirmarEstadoReporte").className = "btn " + (cambioEstado.activo ? "btn-success" : "btn-danger");
        $("ConfirmarEstadoReporte").disabled = false;
        bootstrap.Modal.getOrCreateInstance($("ModalEstadoReporte")).show();
    }

    async function confirmarEstadoReporte() {
        if (!cambioEstado || guardandoEstado) return;
        const cambio = { ...cambioEstado };
        guardandoEstado = true;
        $("ConfirmarEstadoReporte").disabled = true;
        $("ConfirmarEstadoReporte").textContent = "Guardando…";
        try {
            const formulario = $("FormularioFiltrosReportes");
            const datos = new FormData(formulario);
            datos.set("id", String(cambio.id));
            datos.set("activo", String(cambio.activo));
            const respuesta = await fetch(formulario.dataset.urlEstado, {
                method: "POST", body: datos, headers: { "Accept": "application/json" }
            });
            if (!respuesta.ok) throw new Error(await leerMensaje(respuesta, "No se pudo cambiar el estado del reporte."));
            const mensaje = await leerMensaje(respuesta, "Estado actualizado correctamente.");
            guardandoEstado = false;
            const modal = $("ModalEstadoReporte");
            await new Promise(resolve => {
                modal.addEventListener("hidden.bs.modal", resolve, { once: true });
                bootstrap.Modal.getOrCreateInstance(modal).hide();
            });
            await cargar();
            mostrarAlerta(mensaje, "success");
        } catch (error) {
            $("AlertaEstadoReporte").className = "alert alert-danger";
            $("AlertaEstadoReporte").textContent = error.message || "No se pudo confirmar el cambio. Actualice el listado antes de reintentar.";
        } finally {
            guardandoEstado = false;
            $("ConfirmarEstadoReporte").disabled = false;
            $("ConfirmarEstadoReporte").textContent = cambio.activo ? "Reactivar" : "Desactivar";
        }
    }


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
    function leerContenidoReporte(contenido) {
        if (!contenido) return null;
        try {
            const datos = JSON.parse(contenido);
            return datos && typeof datos === "object" && !Array.isArray(datos) ? datos : null;
        } catch { return null; }
    }

    function campoFichaReporte(etiqueta, valor) {
        const texto = valor === null || valor === undefined || String(valor).trim() === ""
            ? "No registrado en este reporte" : String(valor);
        return `<dt class="col-sm-5 mb-2">${escaparHtml(etiqueta)}</dt><dd class="col-sm-7 mb-2">${escaparHtml(texto)}</dd>`;
    }

    function pintarDetalle(r) {
        $("TituloDetalleReporte").textContent = r.titulo;
        const datos = leerContenidoReporte(r.contenido);
        const e = datos?.estudiante || {};
        $("DatosDetalleReporte").innerHTML = `
            <div class="ReporteFicha p-3 p-lg-4">
                <div class="row g-4">
                    <section class="col-md-6">
                        <h6 class="mb-3">Estudiante</h6>
                        <dl class="row mb-0 small">
                            ${campoFichaReporte("Nombre", e.nombre || r.estudiante)}
                            ${campoFichaReporte("Nivel educativo", e.nivel)}
                            ${campoFichaReporte("Institución", e.institucion)}
                            ${campoFichaReporte("Áreas de dificultad", e.areasDificultad)}
                        </dl>
                    </section>
                    <section class="col-md-6">
                        <h6 class="mb-3">Información del reporte</h6>
                        <dl class="row mb-0 small">
                            ${campoFichaReporte("Período", textoPeriodo(r))}
                            ${campoFichaReporte("Fecha de generación", formatoFechaHora(r.fechaGeneracion))}
                            ${campoFichaReporte("Generado por", r.generadoPor)}
                            ${campoFichaReporte("Visible en el portal", r.visibleEnPortal ? "Sí" : "No")}
                        </dl>
                    </section>
                </div>
            </div>`;
        $("ContenidoDetalleReporte").innerHTML = htmlContenido(r.contenido);
    }

    function htmlContenido(contenido) {
        if (!contenido || !contenido.trim()) return '<p class="TextoSuave mb-0">Este reporte no tiene contenido adicional registrado.</p>';
        const datos = leerContenidoReporte(contenido);
        if (!datos) return `<div class="ReporteNota">${escaparHtml(contenido)}</div>`;
        const bloques = [seccionSesiones(datos.sesiones), seccionPlanes(datos.planes)].join("");
        return bloques || '<p class="TextoSuave mb-0">Este reporte incluye únicamente el resumen del estudiante.</p>';
    }

    function notaReporte(etiqueta, valor) {
        if (valor === null || valor === undefined || String(valor).trim() === "") return "";
        return `<div class="mb-2"><div class="TextoSuave small fw-semibold mb-1">${escaparHtml(etiqueta)}</div><div class="ReporteNota" style="white-space: pre-wrap; overflow-wrap: anywhere;">${escaparHtml(String(valor))}</div></div>`;
    }

    function cuadriculaNotasReporte(campos) {
        return `<div class="row g-3">` + campos.filter(c => c[1] !== null && c[1] !== undefined && String(c[1]).trim() !== "")
            .map(c => `<div class="${c[2] ? "col-12" : "col-md-6"}">${notaReporte(c[0], c[1])}</div>`).join("") + `</div>`;
    }

    function seccionSesiones(sesiones) {
        if (!Array.isArray(sesiones)) return "";
        if (!sesiones.length) return '<h6>Sesiones</h6><p class="TextoSuave mb-4">Sin sesiones registradas en este reporte.</p>';
        const ordenadas = [...sesiones].sort((a,b) => String(b.fecha ?? "").localeCompare(String(a.fecha ?? "")));
        return `<section class="mb-4"><h6 class="mb-3">Sesiones <span class="badge bg-light text-dark border ms-1">${sesiones.length}</span></h6>` + ordenadas.map(s => `
            <details class="ReporteBloque">
                <summary>
                    <div class="d-flex align-items-center gap-3">
                        <div class="flex-grow-1 ReporteResumenTexto">
                            <strong>${escaparHtml(formatoFecha(s.fecha))}</strong>
                            <div class="TextoSuave small text-truncate">${escaparHtml(s.tema || "Sin tema registrado")}</div>
                        </div>
                        <span class="badge bg-light text-dark border">${escaparHtml(s.tipoAtencion)}</span>
                        <i class="bi bi-chevron-down ReporteFlecha" aria-hidden="true"></i>
                    </div>
                </summary>
                <div class="p-3 p-lg-4">
                    ${cuadriculaNotasReporte([
                        ["Duración", s.duracionMinutos != null ? `${s.duracionMinutos} minutos` : null],
                        ["Objetivo de la sesión", s.objetivo],
                        ["Tema trabajado", s.tema],
                        ["Actividades realizadas", s.actividadesRealizadas],
                        ["Avances", s.avances],
                        ["Objetivos alcanzados", s.objetivosAlcanzados],
                        ["Habilidades desarrolladas", s.habilidadesDesarrolladas],
                        ["Acuerdos", s.acuerdos],
                        ["Recomendaciones", s.recomendaciones, true],
                        ["Observaciones", s.observaciones, true]
                    ])}
                </div>
            </details>`).join("") + `</section>`;
    }

    function seccionPlanes(planes) {
        if (!Array.isArray(planes)) return "";
        if (!planes.length) return '<h6>Planes de intervención</h6><p class="TextoSuave mb-3">Sin planes registrados en este reporte.</p>';
        return `<section><h6 class="mb-3">Planes de intervención <span class="badge bg-light text-dark border ms-1">${planes.length}</span></h6>` + planes.map(p => `
            <details class="ReporteBloque">
                <summary>
                    <div class="d-flex align-items-center gap-3">
                        <div class="flex-grow-1 ReporteResumenTexto">
                            <strong>${escaparHtml(p.titulo)}</strong>
                            <div class="TextoSuave small">${p.actividades != null ? `${escaparHtml(p.actividadesCompletadas ?? 0)} de ${escaparHtml(p.actividades)} actividades completadas` : "Plan de intervención"}</div>
                        </div>
                        <span class="badge bg-light text-dark border">${escaparHtml(p.estado)}</span>
                        <i class="bi bi-chevron-down ReporteFlecha" aria-hidden="true"></i>
                    </div>
                </summary>
                <div class="p-3 p-lg-4">
                    ${cuadriculaNotasReporte([
                        ["Objetivo general", p.objetivoGeneral, true],
                        ["Fecha de inicio", p.fechaInicio ? formatoFecha(p.fechaInicio) : null],
                        ["Fecha de finalización", p.fechaFin ? formatoFecha(p.fechaFin) : null],
                        ["Observaciones", p.observaciones, true]
                    ])}
                    ${estrategiasReporte(p.estrategias)}
                </div>
            </details>`).join("") + `</section>`;
    }

    function estrategiasReporte(estrategias) {
        if (!Array.isArray(estrategias)) return "";
        if (!estrategias.length) return '<p class="TextoSuave mb-0 mt-3">Sin estrategias registradas.</p>';
        const ordenadas = [...estrategias].sort((a,b) => (a.orden ?? Number.MAX_SAFE_INTEGER) - (b.orden ?? Number.MAX_SAFE_INTEGER));
        return `<h6 class="mt-4 mb-3">Estrategias y actividades</h6>` + ordenadas.map((e, indice) => {
            const actividades = Array.isArray(e.actividades) ? e.actividades : [];
            return `<details class="ReporteBloque">
                <summary><div class="d-flex align-items-center gap-3"><div class="flex-grow-1 ReporteResumenTexto"><strong>Estrategia ${indice + 1}</strong><div class="TextoSuave small text-truncate">${escaparHtml(e.descripcion)}</div></div><i class="bi bi-chevron-down ReporteFlecha" aria-hidden="true"></i></div></summary>
                <div class="p-3">
                    ${notaReporte("Descripción", e.descripcion)}
                    ${actividades.length ? `<ul class="row g-3 list-unstyled mb-0 mt-1">` + actividades.map(a => {
                        const completada = a.completada === true || a.completada === 1;
                        return `<li class="col-md-6"><div class="border rounded p-3 h-100"><span class="badge ${completada ? "bg-success" : "bg-secondary"} mb-2">${completada ? "Completada" : "Pendiente"}</span><div class="fw-semibold mb-2">${escaparHtml(a.nombre)}</div>${notaReporte("Detalle", a.descripcion)}</div></li>`;
                    }).join("") + `</ul>` : '<p class="TextoSuave mb-0">Sin actividades registradas.</p>'}
                </div>
            </details>`;
        }).join("");
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
        if (boton && !boton.disabled) abrirDetalle(boton.dataset.id);
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


    let estudiantesLookup = [];
    let coincidenciasLookup = [];
    let indiceLookup = -1;

    function normalizarBusqueda(valor) {
        return String(valor ?? "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").toLocaleLowerCase("es").trim();
    }

    function cerrarLookupReporte() {
        $("ResultadosEstudiantesReporte").classList.add("d-none");
        $("BuscarEstudianteReporte").setAttribute("aria-expanded", "false");
        $("BuscarEstudianteReporte").removeAttribute("aria-activedescendant");
        indiceLookup = -1;
    }

    function invalidarEstudianteReporte() {
        solicitudFicha?.abort();
        $("EstudianteReporte").value = "";
        fichaCargada = false;
        $("GenerarReporteBtn").disabled = true;
        $("FichaEstudianteReporte").classList.add("d-none");
        $("DatosEstudianteReporte").innerHTML = "";
    }

    function mostrarCoincidenciasReporte() {
        const campo = $("BuscarEstudianteReporte");
        if(campo.disabled) return;
        const consulta = normalizarBusqueda(campo.value);
        const palabras = consulta.split(/\s+/).filter(Boolean);
        const todas = estudiantesLookup.filter(e => {
            const nombre = normalizarBusqueda(e.estudiante);
            return palabras.every(palabra => nombre.includes(palabra));
        });
        coincidenciasLookup = todas.slice(0, 8);
        indiceLookup = -1;
        campo.removeAttribute("aria-activedescendant");
        const lista = $("ResultadosEstudiantesReporte");
        lista.replaceChildren();
        coincidenciasLookup.forEach((e, indice) => {
            const boton = document.createElement("button");
            boton.type = "button";
            boton.id = `opcionEstudianteReporte${indice}`;
            boton.className = "list-group-item list-group-item-action text-start";
            boton.setAttribute("role", "option");
            boton.setAttribute("aria-selected", "false");
            boton.tabIndex = -1;
            boton.textContent = `${e.estudiante} · ${e.nivelEducativo || "Sin nivel registrado"} · ID ${e.idEstudiante}`;
            boton.addEventListener("mousedown", evento => evento.preventDefault());
            boton.addEventListener("click", () => elegirEstudianteReporte(e));
            lista.append(boton);
        });
        $("AyudaLookupReporte").textContent = todas.length === 0
            ? "No se encontraron estudiantes."
            : todas.length > 8
                ? `${todas.length} coincidencias. Se muestran 8; escriba más para precisar.`
                : `${todas.length} coincidencia(s). Seleccione un estudiante.`;
        lista.classList.toggle("d-none", coincidenciasLookup.length === 0);
        campo.setAttribute("aria-expanded", String(coincidenciasLookup.length > 0));
    }

    function elegirEstudianteReporte(estudiante) {
        if(generandoReporte) return;
        $("EstudianteReporte").value = estudiante.idEstudiante;
        $("BuscarEstudianteReporte").value = estudiante.estudiante;
        $("AyudaLookupReporte").textContent = "Estudiante seleccionado.";
        cerrarLookupReporte();
        seleccionarEstudianteReporte();
    }

    async function cargarEstudiantesReporte() {
        invalidarEstudianteReporte();
        estudiantesLookup = [];
        const campo = $("BuscarEstudianteReporte");
        const actualizar = $("RecargarEstudiantesReporte");
        campo.value = "";
        campo.disabled = true;
        actualizar.disabled = true;
        cerrarLookupReporte();
        $("AyudaLookupReporte").textContent = "Cargando estudiantes…";
        limpiarAlertaGeneracion();
        try {
            const respuesta = await fetch($("FormularioGenerarReporte").dataset.urlEstudiantes, {
                headers: { "Accept": "application/json" }
            });
            if (!respuesta.ok) throw new Error(await leerMensaje(respuesta, "No se pudieron cargar los estudiantes."));
            const estudiantes = await respuesta.json();
            if (!Array.isArray(estudiantes)) throw new Error("La lista de estudiantes no es válida.");
            estudiantesLookup = [...estudiantes].sort((a,b) => a.estudiante.localeCompare(b.estudiante,"es"));
            campo.disabled = estudiantesLookup.length === 0;
            $("AyudaLookupReporte").textContent = estudiantesLookup.length
                ? "Busque por nombre y seleccione una coincidencia."
                : "No hay estudiantes disponibles.";
        } catch(error) {
            alertaGeneracion(error.message || "No se pudo cargar la lista.");
            $("AyudaLookupReporte").textContent = "Use Actualizar estudiantes para volver a intentarlo.";
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

    function reiniciarFormularioReporte() {
        solicitudFicha?.abort();
        solicitudFicha = null;
        $("FormularioGenerarReporte").reset();
        invalidarEstudianteReporte();
        tituloAutomatico = "";
        $("TituloReporte").value = "";
        $("BuscarEstudianteReporte").value = "";
        $("BuscarEstudianteReporte").disabled = estudiantesLookup.length === 0;
        coincidenciasLookup = [];
        $("ResultadosEstudiantesReporte").replaceChildren();
        cerrarLookupReporte();
        limpiarAlertaGeneracion();
        $("GenerarReporteBtn").textContent = "Generar reporte";
        $("SeccionesReporte").disabled = false;
        $("AyudaLookupReporte").textContent = estudiantesLookup.length
            ? "Busque por nombre y seleccione una coincidencia."
            : "Use Actualizar estudiantes para cargar la lista.";
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
        const desde = fechaParaApi($("InicioReporte").value);
        const hasta = fechaParaApi($("FinReporte").value);
        if (!desde || !hasta) {
            alertaGeneracion("Ingrese fechas válidas en formato dd-mm-aaaa.");
            return;
        }
        if (hasta < desde) {
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
        datosFormulario.set("IncluirDatosGenerales", "true");
        ["IncluirSesiones", "IncluirPlanes"].forEach(nombre =>
            datosFormulario.set(nombre, String($(nombre).checked)));
        datosFormulario.set("PeriodoInicio", desde);
        datosFormulario.set("PeriodoFin", hasta);
        cerrarLookupReporte();
        generandoReporte = true;
        $("SeccionesReporte").disabled = true;
        $("BuscarEstudianteReporte").disabled = true;
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
            $("SeccionesReporte").disabled = false;
            $("BuscarEstudianteReporte").disabled = estudiantesLookup.length === 0;
            $("RecargarEstudiantesReporte").disabled = false;
            boton.disabled = false;
            boton.textContent = "Generar reporte";
        }
    }


    $("BuscarEstudianteReporte").addEventListener("input", () => {
        invalidarEstudianteReporte();
        limpiarAlertaGeneracion();
        mostrarCoincidenciasReporte();
    });
    $("BuscarEstudianteReporte").addEventListener("focus", mostrarCoincidenciasReporte);
    $("BuscarEstudianteReporte").addEventListener("blur", cerrarLookupReporte);
    $("BuscarEstudianteReporte").addEventListener("keydown", evento => {
        if(evento.key === "Escape") {
            evento.preventDefault();
            evento.stopPropagation();
            cerrarLookupReporte();
            return;
        }
        if(evento.key === "Enter") {
            evento.preventDefault();
            if(indiceLookup >= 0) elegirEstudianteReporte(coincidenciasLookup[indiceLookup]);
            return;
        }
        if(!["ArrowDown", "ArrowUp"].includes(evento.key)) return;
        evento.preventDefault();
        if($("ResultadosEstudiantesReporte").classList.contains("d-none")) mostrarCoincidenciasReporte();
        if(!coincidenciasLookup.length) return;
        const salto = evento.key === "ArrowDown" ? 1 : -1;
        indiceLookup = indiceLookup < 0 ? (salto > 0 ? 0 : coincidenciasLookup.length - 1) : (indiceLookup + salto + coincidenciasLookup.length) % coincidenciasLookup.length;
        Array.from($("ResultadosEstudiantesReporte").children).forEach((opcion, indice) => {
            opcion.classList.toggle("active", indice === indiceLookup);
            opcion.setAttribute("aria-selected", String(indice === indiceLookup));
        });
        const activa = $("ResultadosEstudiantesReporte").children[indiceLookup];
        $("BuscarEstudianteReporte").setAttribute("aria-activedescendant", activa.id);
        activa.scrollIntoView({block:"nearest"});
    });
    $("ModalGenerarReporte").addEventListener("hidden.bs.modal", cerrarLookupReporte);

    $("RecargarEstudiantesReporte").addEventListener("click", cargarEstudiantesReporte);
    $("FormularioGenerarReporte").addEventListener("submit", generarReporte);
    $("ModalGenerarReporte").addEventListener("hide.bs.modal", evento => {
        if (generandoReporte) evento.preventDefault();
    });
    $("ModalGenerarReporte").addEventListener("show.bs.modal", reiniciarFormularioReporte);
    $("FormularioFiltrosReportes").addEventListener("submit", aplicarFiltrosReportes);
    $("BuscarNombreReporte").addEventListener("input", () => {
        clearTimeout(temporizadorBusqueda);
        temporizadorBusqueda = setTimeout(buscarConFiltrosActuales, 350);
    });
    $("EstadoFiltroReporte").addEventListener("change", buscarConFiltrosActuales);
    $("CuerpoTablaReportes").addEventListener("click", evento => {
        const boton = evento.target.closest("button[data-accion='estado']");
        if (boton) abrirCambioEstadoReporte(Number(boton.dataset.id));
    });
    $("ConfirmarEstadoReporte").addEventListener("click", confirmarEstadoReporte);
    $("ModalEstadoReporte").addEventListener("hide.bs.modal", evento => {
        if (guardandoEstado) evento.preventDefault();
    });
    $("ModalEstadoReporte").addEventListener("hidden.bs.modal", () => { cambioEstado = null; });

    cargarEstudiantesReporte();
    cargar();
})();




