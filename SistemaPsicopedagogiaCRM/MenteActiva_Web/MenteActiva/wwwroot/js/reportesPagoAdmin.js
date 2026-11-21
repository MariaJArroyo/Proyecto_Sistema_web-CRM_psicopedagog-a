// Admin > Pagos: pagos que las familias reportaron desde el portal.
// Lista por estado, abre el comprobante y aprueba (crea el abono) o rechaza
// con motivo. Al aprobar avisa a pagos.js para que recargue la tabla.
document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    const panel = document.getElementById("PanelReportesPago");
    if (!panel) return;

    const RUTAS = {
        listar: "/Admin/ConsultarReportesPago",
        comprobante: "/Admin/ComprobanteReportePago",
        aprobar: "/Admin/AprobarReportePago",
        rechazar: "/Admin/RechazarReportePago"
    };

    const lista = document.getElementById("ListaReportesPago");
    const alerta = document.getElementById("AlertaReportesPago");
    const contador = document.getElementById("ContadorReportesPago");
    const filtro = document.getElementById("FiltroReportesPago");

    const modalElemento = document.getElementById("ModalRevisarReporte");
    const subtitulo = document.getElementById("SubtituloRevisarReporte");
    const visor = document.getElementById("VisorComprobante");
    const datosLista = document.getElementById("DatosRevisarReporte");
    const acciones = document.getElementById("AccionesRevisarReporte");
    const alertaModal = document.getElementById("AlertaRevisarReporte");
    const panelRechazo = document.getElementById("PanelRechazoReporte");
    const motivo = document.getElementById("MotivoRechazoReporte");
    const botonAprobar = document.getElementById("BotonAprobarReporte");
    const botonRechazar = document.getElementById("BotonConfirmarRechazo");

    let reportes = [];
    let actual = null;

    // ---------- utilidades ----------

    function escapar(texto) {
        const div = document.createElement("div");
        div.textContent = texto ?? "";
        return div.innerHTML.replaceAll('"', "&quot;");
    }

    function dinero(valor) {
        return window.Formato?.dinero ? window.Formato.dinero(valor) : String(valor);
    }

    function fecha(texto) {
        const iso = texto ? String(texto).slice(0, 10) : "";
        if (!iso) return "—";
        const [a, m, d] = iso.split("-").map(Number);
        return new Date(a, m - 1, d).toLocaleDateString("es-CR", { day: "numeric", month: "short", year: "numeric" });
    }

    function hace(texto) {
        if (!texto) return "";
        const minutos = Math.round((Date.now() - new Date(texto).getTime()) / 60000);
        if (minutos < 1) return "hace un momento";
        if (minutos < 60) return `hace ${minutos} min`;
        const horas = Math.round(minutos / 60);
        if (horas < 24) return `hace ${horas} h`;
        const dias = Math.round(horas / 24);
        return dias === 1 ? "ayer" : `hace ${dias} días`;
    }

    function token() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function avisar(elemento, texto, tipo = "danger") {
        elemento.className = texto ? `alert alert-${tipo}` : "alert d-none";
        elemento.textContent = texto || "";
    }

    function ocupado(boton, activo) {
        boton.disabled = activo;
        boton.querySelector(".spinner-border")?.classList.toggle("d-none", !activo);
    }

    async function leer(respuesta) {
        try { return await respuesta.json(); } catch { return null; }
    }

    function estadoChip(estado) {
        const mapa = {
            Pendiente: ["ChipReporte-Pendiente", "bi-hourglass-split", "Por validar"],
            Aprobado: ["ChipReporte-Aprobado", "bi-check-circle", "Aprobado"],
            Rechazado: ["ChipReporte-Rechazado", "bi-x-circle", "Rechazado"]
        };
        const [clase, icono, texto] = mapa[estado] || mapa.Pendiente;
        return `<span class="ChipReporte ${clase}"><i class="bi ${icono} me-1" aria-hidden="true"></i>${texto}</span>`;
    }

    function iconoMetodo(r) {
        return r.idMetodoPago === 2 ? "bi-phone" : "bi-bank";
    }

    // ---------- lista ----------

    function estadoElegido() {
        return filtro.querySelector('input[name="estadoReporte"]:checked')?.value ?? "Pendiente";
    }

    function pintar() {
        const estado = estadoElegido();

        if (!reportes.length) {
            const texto = estado === "Pendiente"
                ? "No hay pagos por validar. Cuando una familia reporte un SINPE o una transferencia, aparecerá aquí."
                : "No hay pagos reportados con este filtro.";
            lista.innerHTML = `<div class="ListaReportesPago-Vacia"><i class="bi bi-inbox" aria-hidden="true"></i>${escapar(texto)}</div>`;
            return;
        }

        lista.innerHTML = reportes.map(r => `
            <article class="FilaReportePago${r.estado === "Pendiente" ? " FilaReportePago-Pendiente" : ""}">
                <span class="FilaReportePago-Metodo" title="${escapar(r.metodoPago)}"><i class="bi ${iconoMetodo(r)}" aria-hidden="true"></i></span>
                <div class="FilaReportePago-Principal">
                    <div class="fw-semibold text-truncate">${escapar(r.estudiante)} <span class="TextoSuave fw-normal">· ${escapar(r.concepto)}</span></div>
                    <div class="TextoSuave small text-truncate">
                        Reportó ${escapar(r.encargado)} · ${escapar(r.metodoPago)} del ${fecha(r.fechaPago)}
                        ${r.referencia ? ` · Ref. <span class="text-body">${escapar(r.referencia)}</span>` : ""}
                        · ${hace(r.fechaCreacion)}
                    </div>
                </div>
                <div class="FilaReportePago-Monto">
                    <div class="fw-semibold">${dinero(r.monto)}</div>
                    <div class="TextoSuave small">Saldo ${dinero(r.saldoCargo)}</div>
                </div>
                <div class="FilaReportePago-Estado">
                    ${r.tieneComprobante ? '<i class="bi bi-image TextoSuave me-2" title="Tiene comprobante" aria-label="Tiene comprobante"></i>' : ""}
                    ${estadoChip(r.estado)}
                </div>
                <div class="d-flex gap-1">
                    ${r.estado === "Aprobado"
                        ? `<button type="button" class="btn btn-sm btn-outline-success" data-ver-cobro="${r.idPago}" title="Ver el cobro con el abono registrado">
                               <i class="bi bi-receipt me-1" aria-hidden="true"></i>Ver cobro</button>`
                        : ""}
                    <button type="button" class="btn btn-sm ${r.estado === "Pendiente" ? "btn-primary" : "btn-outline-secondary"}" data-revisar-reporte="${r.idReportePago}">
                        ${r.estado === "Pendiente" ? "Revisar" : "Ver"}
                    </button>
                </div>
            </article>`).join("");
    }

    function pintarContador(total) {
        contador.textContent = total ? `${total} por validar` : "";
        contador.classList.toggle("d-none", !total);
        panel.classList.toggle("PanelReportesPago-ConPendientes", total > 0);
    }

    async function cargar() {
        avisar(alerta, "");
        const estado = estadoElegido();

        try {
            const respuesta = await fetch(`${RUTAS.listar}?estado=${encodeURIComponent(estado)}`,
                { headers: { Accept: "application/json" } });
            const json = await leer(respuesta);
            if (!respuesta.ok) throw new Error(json?.mensaje || "No se pudieron cargar los pagos reportados.");
            reportes = Array.isArray(json) ? json : [];
        } catch (error) {
            reportes = [];
            avisar(alerta, error.message);
        }

        pintar();

        // El contador siempre muestra los pendientes, aunque el filtro sea otro
        if (estado === "Pendiente") {
            pintarContador(reportes.length);
        } else {
            try {
                const r = await fetch(`${RUTAS.listar}?estado=Pendiente`, { headers: { Accept: "application/json" } });
                const pendientes = r.ok ? await leer(r) : null;
                pintarContador(Array.isArray(pendientes) ? pendientes.length : 0);
            } catch { /* el contador se queda como estaba */ }
        }
    }

    filtro.addEventListener("change", cargar);

    lista.addEventListener("click", function (evento) {
        const verCobro = evento.target.closest("[data-ver-cobro]");
        if (verCobro) {
            document.dispatchEvent(new CustomEvent("pagos:abrirDetalle", { detail: { idPago: Number(verCobro.dataset.verCobro) } }));
            return;
        }

        const boton = evento.target.closest("[data-revisar-reporte]");
        if (!boton) return;
        const id = Number(boton.dataset.revisarReporte);
        actual = reportes.find(r => r.idReportePago === id) || null;
        if (actual) abrir();
    });

    // ---------- revision ----------

    function fila(etiqueta, valor) {
        return `<dt>${escapar(etiqueta)}</dt><dd>${valor}</dd>`;
    }

    function pintarVisor() {
        const r = actual;

        if (!r.tieneComprobante) {
            visor.innerHTML = `
                <div class="VisorComprobante-Vacio">
                    <i class="bi bi-receipt" aria-hidden="true"></i>
                    <p class="mb-1">La familia no adjuntó captura.</p>
                    ${r.referencia ? `<p class="mb-0">Verifique en el banco la referencia<br><strong class="VisorComprobante-Referencia">${escapar(r.referencia)}</strong></p>` : ""}
                </div>`;
            return;
        }

        const url = `${RUTAS.comprobante}?id=${r.idReportePago}`;

        if (r.tipoArchivo === "application/pdf") {
            visor.innerHTML = `
                <iframe src="${url}" title="Comprobante en PDF" class="VisorComprobante-Pdf"></iframe>
                <a href="${url}" target="_blank" rel="noopener" class="btn btn-sm btn-outline-secondary mt-2">
                    <i class="bi bi-box-arrow-up-right me-1" aria-hidden="true"></i>Abrir en otra pestaña
                </a>`;
            return;
        }

        visor.innerHTML = `
            <a href="${url}" target="_blank" rel="noopener" class="VisorComprobante-Imagen" title="Abrir en tamaño completo">
                <img src="${url}" alt="Comprobante enviado por la familia">
            </a>
            <p class="TextoSuave small text-center mt-2 mb-0"><i class="bi bi-zoom-in me-1" aria-hidden="true"></i>Toque la imagen para verla en tamaño completo</p>`;

        visor.querySelector("img").addEventListener("error", function () {
            visor.innerHTML = `<div class="VisorComprobante-Vacio"><i class="bi bi-exclamation-triangle" aria-hidden="true"></i><p class="mb-0">No se pudo cargar el comprobante.</p></div>`;
        });
    }

    function abrir() {
        const r = actual;
        avisar(alertaModal, "");
        panelRechazo.classList.add("d-none");
        motivo.value = "";
        motivo.classList.remove("is-invalid");

        subtitulo.textContent = `${r.estudiante} · ${r.concepto}`;

        const supera = r.estado === "Pendiente" && r.monto > r.saldoCargo + 0.001;

        datosLista.innerHTML =
            fila("Estado", estadoChip(r.estado)) +
            fila("Monto reportado", `<span class="fs-5 fw-semibold">${dinero(r.monto)}</span>`) +
            fila("Saldo actual del cobro", `${dinero(r.saldoCargo)} <span class="TextoSuave small">de ${dinero(r.montoCargo)}</span>`) +
            fila("Método", `<i class="bi ${iconoMetodo(r)} me-1" aria-hidden="true"></i>${escapar(r.metodoPago)}`) +
            fila("Fecha del pago", fecha(r.fechaPago)) +
            fila("Referencia", r.referencia ? `<code class="fs-6">${escapar(r.referencia)}</code>` : '<span class="TextoSuave">Sin referencia</span>') +
            fila("Reportó", `${escapar(r.encargado)} <span class="TextoSuave small">· ${hace(r.fechaCreacion)}</span>`) +
            (r.comentario ? fila("Comentario", escapar(r.comentario)) : "") +
            (r.estado !== "Pendiente"
                ? fila("Revisado", `${escapar(r.revisadoPor || "—")} <span class="TextoSuave small">· ${fecha(r.fechaRevision)}</span>`)
                : "") +
            (r.motivoRechazo ? fila("Motivo del rechazo", escapar(r.motivoRechazo)) : "");

        acciones.classList.toggle("d-none", r.estado !== "Pendiente");

        if (supera) {
            avisar(alertaModal,
                "El monto reportado es mayor que el saldo actual del cobro (puede que ya se registrara otro pago). No se puede aprobar: recházelo indicando el motivo.",
                "warning");
        }
        botonAprobar.disabled = supera;

        pintarVisor();
        window.bootstrap.Modal.getOrCreateInstance(modalElemento).show();
    }

    modalElemento.addEventListener("hidden.bs.modal", function () {
        visor.innerHTML = "";
        actual = null;
    });

    document.getElementById("BotonMostrarRechazo").addEventListener("click", function () {
        panelRechazo.classList.remove("d-none");
        motivo.focus();
    });

    document.getElementById("BotonCancelarRechazo").addEventListener("click", function () {
        panelRechazo.classList.add("d-none");
        motivo.classList.remove("is-invalid");
    });

    async function enviar(url, campos, boton, mensajePorDefecto) {
        const datos = new FormData();
        datos.append("__RequestVerificationToken", token());
        Object.entries(campos).forEach(([clave, valor]) => datos.append(clave, valor));

        ocupado(boton, true);
        try {
            const respuesta = await fetch(url, { method: "POST", body: datos });
            const json = await leer(respuesta);
            if (!respuesta.ok) throw new Error(json?.mensaje || mensajePorDefecto);
            return json;
        } finally {
            ocupado(boton, false);
        }
    }

    async function terminar(mensaje, idPagoAbrir) {
        // Al aprobar se abre el detalle del cobro para que se vea el abono nuevo.
        // Se espera a que este modal termine de cerrarse: dos modales a la vez
        // dejan el fondo oscuro trabado
        if (idPagoAbrir) {
            modalElemento.addEventListener("hidden.bs.modal", function () {
                document.dispatchEvent(new CustomEvent("pagos:abrirDetalle", { detail: { idPago: idPagoAbrir } }));
            }, { once: true });
        }

        window.bootstrap.Modal.getOrCreateInstance(modalElemento).hide();
        if (typeof window.MostrarToast === "function") window.MostrarToast(mensaje, "exito");
        await cargar();
        document.dispatchEvent(new CustomEvent("pagos:recargar"));
        if (window.actualizarContadores) window.actualizarContadores("pagosPendientes");
    }

    botonAprobar.addEventListener("click", async function () {
        if (!actual) return;
        avisar(alertaModal, "");
        try {
            const json = await enviar(`${RUTAS.aprobar}?id=${actual.idReportePago}`, {}, botonAprobar,
                "No se pudo aprobar el pago.");
            await terminar(json?.mensaje || "Pago aprobado.", actual.idPago);
        } catch (error) {
            avisar(alertaModal, error.message);
        }
    });

    botonRechazar.addEventListener("click", async function () {
        if (!actual) return;
        const texto = motivo.value.trim();
        motivo.classList.toggle("is-invalid", !texto);
        if (!texto) return;

        avisar(alertaModal, "");
        try {
            const json = await enviar(`${RUTAS.rechazar}?id=${actual.idReportePago}`, { motivo: texto }, botonRechazar,
                "No se pudo rechazar el reporte.");
            await terminar(json?.mensaje || "Reporte rechazado.");
        } catch (error) {
            avisar(alertaModal, error.message);
        }
    });

    cargar();
});
