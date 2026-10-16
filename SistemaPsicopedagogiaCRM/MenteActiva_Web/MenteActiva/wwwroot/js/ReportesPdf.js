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

    let solicitudFicha = null;
    let fichaCargada = false;
    let generandoReporte = false;
    let tituloAutomatico = "";

    let estudiantesLookup = [];
    let coincidenciasLookup = [];
    let indiceLookup = -1;

    // =====================================================
    // UTILIDADES
    // =====================================================

    function $(id) {
        return document.getElementById(id);
    }

    function escaparHtml(texto) {
        const elemento = document.createElement("div");
        elemento.textContent = texto ?? "";
        return elemento.innerHTML;
    }

    function mostrarAlerta(mensaje, tipo = "danger") {
        $("AlertaReportes").className = `alert alert-${tipo} mt-3`;
        $("AlertaReportes").textContent = mensaje;
    }

    function ocultarAlerta() {
        $("AlertaReportes").className = "alert d-none mt-3";
        $("AlertaReportes").textContent = "";
    }

    async function leerMensaje(respuesta, porDefecto) {
        try {
            const datos = await respuesta.json();
            return datos?.mensaje || porDefecto;
        } catch {
            return porDefecto;
        }
    }

    function fechaParaApi(texto) {
        const partes = /^(\d{2})-(\d{2})-(\d{4})$/.exec(texto.trim());

        if (!partes) return null;

        const [, dia, mes, anio] = partes;
        const d = Number(dia);
        const m = Number(mes);
        const a = Number(anio);

        if (a < 1000 || m < 1 || m > 12 || d < 1) return null;
        if (d > new Date(Date.UTC(a, m, 0)).getUTCDate()) return null;

        return `${anio}-${mes}-${dia}`;
    }

    function formatoFecha(valor) {
        if (!valor) return "";

        const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(valor));
        return partes ? `${partes[3]}-${partes[2]}-${partes[1]}` : "";
    }

    function formatoFechaHora(valor) {
        if (!valor) return "";

        const fecha = new Date(valor);

        if (isNaN(fecha)) return "";

        return formatoFecha(valor) + ", " +
            fecha.toLocaleTimeString("es-CR", {
                hour: "2-digit",
                minute: "2-digit"
            });
    }

    function textoPeriodo(reporte) {
        return `${formatoFecha(reporte.periodoInicio)} – ${formatoFecha(reporte.periodoFin)}`;
    }

    function normalizarBusqueda(valor) {
        return String(valor ?? "")
            .normalize("NFD")
            .replace(/[\u0300-\u036f]/g, "")
            .toLocaleLowerCase("es")
            .trim();
    }

    // =====================================================
    // LISTADO Y FILTROS
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

        if (filtrosAplicados.buscar) {
            parametros.set("buscar", filtrosAplicados.buscar);
        }

        parametros.set("estado", filtrosAplicados.estado);
        parametros.set("orden", "az");

        $("TablaReportes").setAttribute("aria-busy", "true");
        $("CargandoReportes").classList.remove("d-none");

        try {
            const respuesta = await fetch(`${RUTAS.listar}?${parametros.toString()}`, {
                signal: control.signal,
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok) {
                throw new Error(await leerMensaje(respuesta, "No se pudieron cargar los reportes."));
            }

            const datos = await respuesta.json();

            if (control.signal.aborted || solicitudReportes !== control) return;

            if (!Array.isArray(datos)) {
                throw new Error("La respuesta de reportes no es válida.");
            }

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
        return aplicarFiltrosReportes({
            preventDefault() { },
            currentTarget: $("FormularioFiltrosReportes")
        });
    }

    function pintar() {
        const total = reportes.length;
        const totalPaginas = Math.max(1, Math.ceil(total / cantidadPorPagina));

        paginaActual = Math.max(1, Math.min(paginaActual, totalPaginas));

        const inicio = (paginaActual - 1) * cantidadPorPagina;
        const fin = Math.min(inicio + cantidadPorPagina, total);
        const reportesPagina = reportes.slice(inicio, fin);

        $("MensajeVacioReportes").textContent = hayFiltrosAplicados()
            ? "No se encontraron reportes con los criterios seleccionados."
            : "No hay reportes activos para mostrar.";

        $("RangoReportes").textContent = total === 0 ? "0 de 0" : `${inicio + 1}–${fin} de ${total}`;
        $("PaginaReportes").textContent = `Página ${paginaActual} de ${totalPaginas}`;
        $("AnteriorReportes").disabled = paginaActual <= 1;
        $("SiguienteReportes").disabled = paginaActual >= totalPaginas;

        $("ResumenReportes").textContent = total === 0
            ? "Sin reportes para mostrar."
            : (total === 1 ? "1 reporte generado." : `${total} reportes generados.`);

        $("VacioReportes").classList.toggle("d-none", total !== 0);
        $("TablaReportes").classList.toggle("d-none", total === 0);

        $("CuerpoTablaReportes").innerHTML = reportesPagina.map(reporte => {
            const activo = reporte.activo === true;

            return `
                <tr>
                    <td>${escaparHtml(reporte.titulo)}</td>
                    <td>${escaparHtml(reporte.estudiante)}</td>
                    <td>${escaparHtml(reporte.generadoPor)}</td>
                    <td class="TablaAcciones text-nowrap">
                        <button type="button" class="btn btn-sm btn-outline-secondary border-0" data-accion="ver" data-id="${escaparHtml(reporte.idReporte)}" title="${activo ? "Consultar reporte" : "Reporte inactivo: no disponible"}" aria-label="${activo ? "Consultar reporte" : "Reporte inactivo: no disponible"}" ${activo ? "" : "disabled"}>
                            <i class="bi bi-eye" aria-hidden="true"></i>
                        </button>

                        <button type="button" class="btn btn-sm text-danger border-0" data-accion="pdf" data-id="${escaparHtml(reporte.idReporte)}" title="Descargar PDF" aria-label="Descargar reporte en PDF" ${activo ? "" : "disabled"}>
                            <i class="bi bi-file-earmark-pdf" aria-hidden="true"></i>
                        </button>

                        <button type="button" class="btn btn-sm border-0 ${activo ? "text-primary" : "text-secondary"}" data-accion="estado" data-id="${escaparHtml(reporte.idReporte)}" role="switch" aria-checked="${activo}" title="${activo ? "Desactivar reporte" : "Reactivar reporte"}" aria-label="${activo ? "Desactivar reporte" : "Reactivar reporte"}">
                            <i class="bi ${activo ? "bi-toggle-on" : "bi-toggle-off"} fs-5" aria-hidden="true"></i>
                        </button>
                    </td>
                </tr>`;
        }).join("");
    }

    // =====================================================
    // ESTADO DEL REPORTE
    // =====================================================

    function abrirCambioEstadoReporte(id) {
        const reporte = reportes.find(item => Number(item.idReporte) === Number(id));

        if (!reporte || guardandoEstado) return;

        cambioEstado = {
            id: reporte.idReporte,
            activo: !reporte.activo
        };

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
                method: "POST",
                body: datos,
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok) {
                throw new Error(await leerMensaje(respuesta, "No se pudo cambiar el estado del reporte."));
            }

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
            $("AlertaEstadoReporte").textContent = error.message ||
                "No se pudo confirmar el cambio. Actualice el listado antes de reintentar.";
        } finally {
            guardandoEstado = false;
            $("ConfirmarEstadoReporte").disabled = false;
            $("ConfirmarEstadoReporte").textContent = cambio.activo ? "Reactivar" : "Desactivar";
        }
    }

    // =====================================================
    // DETALLE DEL REPORTE
    // =====================================================

    async function abrirDetalle(id) {
        ocultarAlerta();

        try {
            const respuesta = await fetch(`${RUTAS.detalle}?id=${encodeURIComponent(id)}`, {
                headers: { Accept: "application/json" }
            });

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

    function leerContenidoReporte(contenido) {
        if (!contenido) return null;

        try {
            const datos = JSON.parse(contenido);
            return datos && typeof datos === "object" && !Array.isArray(datos) ? datos : null;
        } catch {
            return null;
        }
    }

    function campoFichaReporte(etiqueta, valor) {
        const texto = valor === null || valor === undefined || String(valor).trim() === ""
            ? "No registrado en este reporte"
            : String(valor);

        return `
            <dt class="col-sm-5 mb-2">${escaparHtml(etiqueta)}</dt>
            <dd class="col-sm-7 mb-2">${escaparHtml(texto)}</dd>`;
    }

    function pintarDetalle(reporte) {
        $("TituloDetalleReporte").textContent = reporte.titulo;

        const datos = leerContenidoReporte(reporte.contenido);
        const estudiante = datos?.estudiante || {};

        const areasActuales = Array.isArray(estudiante.areasTrabajadas)
            ? (estudiante.areasDificultad || "Sin áreas actuales")
            : estudiante.areasDificultad;

        $("DatosDetalleReporte").innerHTML = `
            <div class="ReporteFicha p-3 p-lg-4">
                <div class="row g-4">
                    <section class="col-md-6">
                        <h6 class="mb-3">Estudiante</h6>
                        <dl class="row mb-0 small">
                            ${campoFichaReporte("Nombre", estudiante.nombre || reporte.estudiante)}
                            ${campoFichaReporte("Nivel educativo", estudiante.nivel)}
                            ${campoFichaReporte("Institución", estudiante.institucion)}
                            ${campoFichaReporte("Áreas de dificultad actuales", areasActuales)}
                        </dl>
                    </section>

                    <section class="col-md-6">
                        <h6 class="mb-3">Información del reporte</h6>
                        <dl class="row mb-0 small">
                            ${campoFichaReporte("Período", textoPeriodo(reporte))}
                            ${campoFichaReporte("Fecha de generación", formatoFechaHora(reporte.fechaGeneracion))}
                            ${campoFichaReporte("Generado por", reporte.generadoPor)}
                        </dl>
                    </section>
                </div>
            </div>`;

        $("ContenidoDetalleReporte").innerHTML = htmlContenido(reporte.contenido);
    }

    function htmlContenido(contenido) {
        if (!contenido || !contenido.trim()) {
            return '<p class="TextoSuave mb-0">Este reporte no tiene contenido adicional registrado.</p>';
        }

        const datos = leerContenidoReporte(contenido);

        if (!datos) {
            return `<div class="ReporteNota">${escaparHtml(contenido)}</div>`;
        }

        const bloques = [
            seccionSesiones(datos.sesiones),
            seccionPlanes(datos.planes)
        ].join("");

        return bloques || '<p class="TextoSuave mb-0">Este reporte incluye únicamente el resumen del estudiante.</p>';
    }

    function notaReporte(etiqueta, valor) {
        if (valor === null || valor === undefined || String(valor).trim() === "") return "";

        return `
            <div class="mb-2">
                <div class="TextoSuave small fw-semibold mb-1">${escaparHtml(etiqueta)}</div>
                <div class="ReporteNota">${escaparHtml(String(valor))}</div>
            </div>`;
    }

    function cuadriculaNotasReporte(campos) {
        return '<div class="row g-3">' + campos
            .filter(campo => campo[1] !== null && campo[1] !== undefined && String(campo[1]).trim() !== "")
            .map(campo => `
                <div class="${campo[2] ? "col-12" : "col-md-6"}">
                    ${notaReporte(campo[0], campo[1])}
                </div>
            `).join("") + "</div>";
    }

    function seccionSesiones(sesiones) {
        if (!Array.isArray(sesiones)) return "";
        if (!sesiones.length) return '<h6>Sesiones</h6><p class="TextoSuave mb-4">Sin sesiones registradas en este reporte.</p>';

        const ordenadas = [...sesiones].sort((a, b) =>
            String(b.fecha ?? "").localeCompare(String(a.fecha ?? ""))
        );

        return `<section class="mb-4"><h6 class="mb-3">Sesiones <span class="badge bg-light text-dark border ms-1">${sesiones.length}</span></h6>` + ordenadas.map(sesion => `
            <details class="ReporteBloque">
                <summary>
                    <div class="d-flex align-items-center gap-3">
                        <div class="flex-grow-1 ReporteResumenTexto">
                            <strong>${escaparHtml(formatoFecha(sesion.fecha))}</strong>
                            <div class="TextoSuave small text-truncate">${escaparHtml(sesion.tema || "Sin tema registrado")}</div>
                        </div>
                        <span class="badge bg-light text-dark border">${escaparHtml(sesion.tipoAtencion)}</span>
                        <i class="bi bi-chevron-down ReporteFlecha" aria-hidden="true"></i>
                    </div>
                </summary>
                <div class="p-3 p-lg-4">
                    ${cuadriculaNotasReporte([
            ["Duración", sesion.duracionMinutos != null ? `${sesion.duracionMinutos} minutos` : null],
            ["Objetivo de la sesión", sesion.objetivo],
            ["Tema trabajado", sesion.tema],
            ["Actividades realizadas", sesion.actividadesRealizadas],
            ["Avances", sesion.avances],
            ["Objetivos alcanzados", sesion.objetivosAlcanzados],
            ["Habilidades desarrolladas", sesion.habilidadesDesarrolladas],
            ["Acuerdos", sesion.acuerdos],
            ["Recomendaciones", sesion.recomendaciones],
            ["Observaciones", sesion.observaciones]
        ])}
                </div>
            </details>`).join("") + "</section>";
    }

    function seccionPlanes(planes) {
        if (!Array.isArray(planes)) return "";
        if (!planes.length) return '<h6>Planes de intervención</h6><p class="TextoSuave mb-3">Sin planes registrados en este reporte.</p>';

        return `<section><h6 class="mb-3">Planes de intervención <span class="badge bg-light text-dark border ms-1">${planes.length}</span></h6>` + planes.map(plan => `
            <details class="ReporteBloque">
                <summary>
                    <div class="d-flex align-items-center gap-3">
                        <div class="flex-grow-1 ReporteResumenTexto">
                            <strong>${escaparHtml(plan.titulo)}</strong>
                            <div class="TextoSuave small">${plan.actividades != null ? `${escaparHtml(plan.actividadesCompletadas ?? 0)} de ${escaparHtml(plan.actividades)} actividades completadas` : "Plan de intervención"}</div>
                        </div>
                        <span class="badge bg-light text-dark border">${escaparHtml(plan.estado)}</span>
                        <i class="bi bi-chevron-down ReporteFlecha" aria-hidden="true"></i>
                    </div>
                </summary>
                <div class="p-3 p-lg-4">
                    ${cuadriculaNotasReporte([
            ["Objetivo general", plan.objetivoGeneral, true],
            ["Fecha de inicio", plan.fechaInicio ? formatoFecha(plan.fechaInicio) : null],
            ["Fecha de finalización", plan.fechaFin ? formatoFecha(plan.fechaFin) : null],
            ["Observaciones", plan.observaciones, true]
        ])}
                    ${estrategiasReporte(plan.estrategias)}
                </div>
            </details>`).join("") + "</section>";
    }

    function estrategiasReporte(estrategias) {
        if (!Array.isArray(estrategias)) return "";
        if (!estrategias.length) return '<p class="TextoSuave mb-0 mt-3">Sin estrategias registradas.</p>';

        const ordenadas = [...estrategias].sort((a, b) =>
            (a.orden ?? Number.MAX_SAFE_INTEGER) -
            (b.orden ?? Number.MAX_SAFE_INTEGER)
        );

        return '<h6 class="mt-4 mb-3">Estrategias y actividades</h6>' + ordenadas.map((estrategia, indice) => {
            const actividades = Array.isArray(estrategia.actividades) ? estrategia.actividades : [];

            return `
                <details class="ReporteBloque">
                    <summary>
                        <div class="d-flex align-items-center gap-3">
                            <div class="flex-grow-1 ReporteResumenTexto">
                                <strong>Estrategia ${indice + 1}</strong>
                                <div class="TextoSuave small text-truncate">${escaparHtml(estrategia.descripcion)}</div>
                            </div>
                            <i class="bi bi-chevron-down ReporteFlecha" aria-hidden="true"></i>
                        </div>
                    </summary>

                    <div class="p-3">
                        ${notaReporte("Descripción", estrategia.descripcion)}

                        ${actividades.length ? '<ul class="row g-3 list-unstyled mb-0 mt-1">' + actividades.map(actividad => {
                const completada = actividad.completada === true || actividad.completada === 1;

                return `
                                <li class="col-md-6">
                                    <div class="border rounded p-3 h-100">
                                        <span class="badge ${completada ? "bg-success" : "bg-secondary"} mb-2">${completada ? "Completada" : "Pendiente"}</span>
                                        <div class="fw-semibold mb-2">${escaparHtml(actividad.nombre)}</div>
                                        ${notaReporte("Detalle", actividad.descripcion)}
                                    </div>
                                </li>`;
            }).join("") + "</ul>" : '<p class="TextoSuave mb-0">Sin actividades registradas.</p>'}
                    </div>
                </details>`;
        }).join("");
    }

    // =====================================================
    // LOOKUP DE ESTUDIANTES
    // =====================================================

    function alertaGeneracion(mensaje, tipo = "warning") {
        $("AlertaGenerarReporte").className = `alert alert-${tipo}`;
        $("AlertaGenerarReporte").textContent = mensaje;
    }

    function limpiarAlertaGeneracion() {
        $("AlertaGenerarReporte").className = "alert d-none";
        $("AlertaGenerarReporte").textContent = "";
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

        if (campo.disabled) return;

        const palabras = normalizarBusqueda(campo.value).split(/\s+/).filter(Boolean);

        const todas = estudiantesLookup.filter(estudiante => {
            const nombre = normalizarBusqueda(estudiante.estudiante);
            return palabras.every(palabra => nombre.includes(palabra));
        });

        coincidenciasLookup = todas.slice(0, 8);
        indiceLookup = -1;

        campo.removeAttribute("aria-activedescendant");

        const lista = $("ResultadosEstudiantesReporte");
        lista.replaceChildren();

        coincidenciasLookup.forEach((estudiante, indice) => {
            const boton = document.createElement("button");

            boton.type = "button";
            boton.id = `opcionEstudianteReporte${indice}`;
            boton.className = "list-group-item list-group-item-action text-start";
            boton.setAttribute("role", "option");
            boton.setAttribute("aria-selected", "false");
            boton.tabIndex = -1;
            boton.textContent = `${estudiante.estudiante} · ${estudiante.nivelEducativo || "Sin nivel registrado"} · ID ${estudiante.idEstudiante}`;

            boton.addEventListener("mousedown", evento => evento.preventDefault());
            boton.addEventListener("click", () => elegirEstudianteReporte(estudiante));

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
        if (generandoReporte) return;

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
        limpiarAlertaGeneracion();

        $("AyudaLookupReporte").textContent = "Cargando estudiantes…";

        try {
            const respuesta = await fetch($("FormularioGenerarReporte").dataset.urlEstudiantes, {
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok) {
                throw new Error(await leerMensaje(respuesta, "No se pudieron cargar los estudiantes."));
            }

            const estudiantes = await respuesta.json();

            if (!Array.isArray(estudiantes)) {
                throw new Error("La lista de estudiantes no es válida.");
            }

            estudiantesLookup = [...estudiantes].sort((a, b) =>
                String(a.estudiante || "").localeCompare(String(b.estudiante || ""), "es")
            );

            campo.disabled = estudiantesLookup.length === 0;

            $("AyudaLookupReporte").textContent = estudiantesLookup.length
                ? "Busque por nombre y seleccione una coincidencia."
                : "No hay estudiantes disponibles.";
        } catch (error) {
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
                signal: control.signal,
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok) {
                throw new Error(await leerMensaje(respuesta, "No se pudo cargar la ficha del estudiante."));
            }

            const estudiante = await respuesta.json();

            if (control.signal.aborted || $("EstudianteReporte").value !== id) return;

            const nombre = [
                estudiante.nombre,
                estudiante.primerApellido,
                estudiante.segundoApellido
            ].filter(Boolean).join(" ");

            const areas = estudiante.areasDificultad ||
                (Array.isArray(estudiante.areas)
                    ? estudiante.areas.map(area => area.nombreArea || area.nombre).filter(Boolean).join(", ")
                    : "");

            const encargados = Array.isArray(estudiante.encargados)
                ? estudiante.encargados.map(encargado =>
                    `${encargado.nombreCompleto}${encargado.parentesco ? ` (${encargado.parentesco})` : ""}`
                ).join("; ")
                : "";

            $("DatosEstudianteReporte").innerHTML = [
                notaReporte("Nombre", nombre),
                notaReporte("Fecha de nacimiento", estudiante.fechaNacimiento ? formatoFecha(estudiante.fechaNacimiento) : null),
                notaReporte("Nivel educativo", estudiante.nivelEducativo),
                notaReporte("Institución", estudiante.institucion),
                notaReporte("Áreas de dificultad actuales", areas),
                notaReporte("Necesidades de apoyo", estudiante.necesidadesApoyo),
                notaReporte("Observaciones", estudiante.observaciones),
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

    // =====================================================
    // GENERACIÓN DEL REPORTE
    // =====================================================

    function reiniciarFormularioReporte() {
        solicitudFicha?.abort();
        solicitudFicha = null;

        $("FormularioGenerarReporte").reset();
        invalidarEstudianteReporte();

        tituloAutomatico = "";
        coincidenciasLookup = [];

        $("TituloReporte").value = "";
        $("BuscarEstudianteReporte").value = "";
        $("BuscarEstudianteReporte").disabled = estudiantesLookup.length === 0;
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

        if (boton.disabled || generandoReporte) return;

        const datosFormulario = new FormData(formulario);

        datosFormulario.set("IncluirDatosGenerales", "true");
        datosFormulario.set("IncluirSesiones", String($("IncluirSesiones").checked));
        datosFormulario.set("IncluirPlanes", String($("IncluirPlanes").checked));
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
                headers: { Accept: "application/json" }
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
            boton.disabled = !fichaCargada;
            boton.textContent = "Generar reporte";
        }
    }

    // =====================================================
    // EVENTOS DEL LISTADO
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
        const boton = evento.target.closest("button[data-accion]");

        if (!boton || boton.disabled) return;

        if (boton.dataset.accion === "ver") {
            abrirDetalle(boton.dataset.id);
        }

        if (boton.dataset.accion === "estado") {
            abrirCambioEstadoReporte(boton.dataset.id);
        }

        // La descarga se atiende en ReportesPdf.js.
    });

    $("FormularioFiltrosReportes").addEventListener("submit", aplicarFiltrosReportes);

    $("BuscarNombreReporte").addEventListener("input", function () {
        clearTimeout(temporizadorBusqueda);
        temporizadorBusqueda = setTimeout(buscarConFiltrosActuales, 350);
    });

    $("EstadoFiltroReporte").addEventListener("change", buscarConFiltrosActuales);
    $("ConfirmarEstadoReporte").addEventListener("click", confirmarEstadoReporte);

    $("ModalEstadoReporte").addEventListener("hide.bs.modal", function (evento) {
        if (guardandoEstado) evento.preventDefault();
    });

    $("ModalEstadoReporte").addEventListener("hidden.bs.modal", function () {
        cambioEstado = null;
    });

    // =====================================================
    // EVENTOS DEL LOOKUP Y GENERACIÓN
    // =====================================================

    $("BuscarEstudianteReporte").addEventListener("input", function () {
        invalidarEstudianteReporte();
        limpiarAlertaGeneracion();
        mostrarCoincidenciasReporte();
    });

    $("BuscarEstudianteReporte").addEventListener("focus", mostrarCoincidenciasReporte);
    $("BuscarEstudianteReporte").addEventListener("blur", cerrarLookupReporte);

    $("BuscarEstudianteReporte").addEventListener("keydown", function (evento) {
        if (evento.key === "Escape") {
            evento.preventDefault();
            evento.stopPropagation();
            cerrarLookupReporte();
            return;
        }

        if (evento.key === "Enter") {
            evento.preventDefault();

            if (indiceLookup >= 0) {
                elegirEstudianteReporte(coincidenciasLookup[indiceLookup]);
            }

            return;
        }

        if (!["ArrowDown", "ArrowUp"].includes(evento.key)) return;

        evento.preventDefault();

        if ($("ResultadosEstudiantesReporte").classList.contains("d-none")) {
            mostrarCoincidenciasReporte();
        }

        if (!coincidenciasLookup.length) return;

        const salto = evento.key === "ArrowDown" ? 1 : -1;

        indiceLookup = indiceLookup < 0
            ? (salto > 0 ? 0 : coincidenciasLookup.length - 1)
            : (indiceLookup + salto + coincidenciasLookup.length) % coincidenciasLookup.length;

        Array.from($("ResultadosEstudiantesReporte").children).forEach((opcion, indice) => {
            opcion.classList.toggle("active", indice === indiceLookup);
            opcion.setAttribute("aria-selected", String(indice === indiceLookup));
        });

        const activa = $("ResultadosEstudiantesReporte").children[indiceLookup];

        $("BuscarEstudianteReporte").setAttribute("aria-activedescendant", activa.id);
        activa.scrollIntoView({ block: "nearest" });
    });

    $("RecargarEstudiantesReporte").addEventListener("click", cargarEstudiantesReporte);
    $("FormularioGenerarReporte").addEventListener("submit", generarReporte);

    $("ModalGenerarReporte").addEventListener("show.bs.modal", reiniciarFormularioReporte);
    $("ModalGenerarReporte").addEventListener("hidden.bs.modal", cerrarLookupReporte);

    $("ModalGenerarReporte").addEventListener("hide.bs.modal", function (evento) {
        if (generandoReporte) evento.preventDefault();
    });

    // =====================================================
    // INICIALIZACIÓN
    // =====================================================

    cargarEstudiantesReporte();
    cargar();
})();