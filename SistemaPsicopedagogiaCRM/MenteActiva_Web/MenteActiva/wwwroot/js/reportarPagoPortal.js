// Portal > Pagos: la familia reporta un SINPE o una transferencia.
// Abre el modal con el cargo elegido, muestra la vista previa de la captura y
// envia todo como FormData a PortalController.ReportarPago, que lo reenvia al API.
(function () {
    "use strict";

    const modalElemento = document.getElementById("ModalReportarPago");
    const formulario = document.getElementById("FormularioReportarPago");
    if (!modalElemento || !formulario) return;

    const TAMANO_MAXIMO = 5 * 1024 * 1024;
    const TIPOS = ["image/jpeg", "image/png", "image/webp", "application/pdf"];

    const alerta = document.getElementById("AlertaReportarPago");
    const campoIdPago = document.getElementById("IdPagoReporte");
    const concepto = document.getElementById("ConceptoReportarPago");
    const monto = document.getElementById("MontoReporte");
    const ayudaMonto = document.getElementById("AyudaMontoReporte");
    const fecha = document.getElementById("FechaReporte");
    const referencia = document.getElementById("ReferenciaReporte");
    const comentario = document.getElementById("ComentarioReporte");
    const archivo = document.getElementById("ArchivoComprobante");
    const zona = document.getElementById("ZonaComprobante");
    const vacia = zona.querySelector(".ZonaComprobante-Vacia");
    const vista = zona.querySelector(".ZonaComprobante-Vista");
    const imagen = vista.querySelector("img");
    const iconoPdf = vista.querySelector(".ZonaComprobante-Pdf");
    const nombreArchivo = vista.querySelector(".ZonaComprobante-Nombre");
    const ayudaRespaldo = document.getElementById("AyudaRespaldo");
    const botonEnviar = document.getElementById("BotonEnviarReporte");

    let saldoMaximo = 0;
    let archivoElegido = null;
    let urlVista = null;

    function mostrarError(texto) {
        alerta.textContent = texto;
        alerta.hidden = !texto;
        if (texto) alerta.scrollIntoView({ block: "nearest" });
    }

    function leerMonto(texto) {
        const limpio = String(texto || "").trim().replace(/\s/g, "").replace(",", ".");
        return /^\d+(\.\d{1,2})?$/.test(limpio) ? Number(limpio) : NaN;
    }

    function limpiarVista() {
        if (urlVista) URL.revokeObjectURL(urlVista);
        urlVista = null;
        archivoElegido = null;
        archivo.value = "";
        imagen.hidden = true;
        imagen.removeAttribute("src");
        iconoPdf.hidden = true;
        vista.hidden = true;
        vacia.hidden = false;
        zona.classList.remove("TieneArchivo");
    }

    function elegirArchivo(nuevo) {
        if (!nuevo) return;

        if (!TIPOS.includes(nuevo.type)) {
            mostrarError("El comprobante debe ser una imagen (JPG, PNG o WEBP) o un PDF.");
            return;
        }

        if (nuevo.size > TAMANO_MAXIMO) {
            mostrarError("El comprobante no puede pasar de 5 MB.");
            return;
        }

        limpiarVista();
        mostrarError("");
        archivoElegido = nuevo;

        if (nuevo.type === "application/pdf") {
            iconoPdf.hidden = false;
        } else {
            urlVista = URL.createObjectURL(nuevo);
            imagen.src = urlVista;
            imagen.hidden = false;
        }

        nombreArchivo.textContent = nuevo.name;
        vacia.hidden = true;
        vista.hidden = false;
        zona.classList.add("TieneArchivo");
        ayudaRespaldo.classList.remove("text-danger");
    }

    // ---------- abrir el modal desde la fila del cargo ----------

    document.addEventListener("click", function (evento) {
        const boton = evento.target.closest("[data-reportar-pago]");
        if (!boton) return;

        formulario.reset();
        formulario.querySelectorAll(".is-invalid").forEach(c => c.classList.remove("is-invalid"));
        limpiarVista();
        mostrarError("");
        ayudaRespaldo.classList.remove("text-danger");

        saldoMaximo = Number(boton.dataset.saldo) || 0;
        campoIdPago.value = boton.dataset.idPago;
        concepto.textContent = boton.dataset.concepto || "";
        monto.value = saldoMaximo ? String(saldoMaximo) : "";
        ayudaMonto.textContent = `Saldo por reportar: ${boton.dataset.saldoTexto || ""}`;
        fecha.value = fecha.max;

        window.bootstrap.Modal.getOrCreateInstance(modalElemento).show();
    });

    modalElemento.addEventListener("shown.bs.modal", () => monto.focus());
    modalElemento.addEventListener("hidden.bs.modal", limpiarVista);

    // ---------- archivo: elegir, arrastrar o pegar ----------

    archivo.addEventListener("change", () => elegirArchivo(archivo.files?.[0]));

    document.getElementById("QuitarComprobante").addEventListener("click", function (evento) {
        // El boton vive dentro del <label>: sin esto se abriria el selector
        evento.preventDefault();
        evento.stopPropagation();
        limpiarVista();
    });

    ["dragenter", "dragover"].forEach(tipo => zona.addEventListener(tipo, evento => {
        evento.preventDefault();
        zona.classList.add("Arrastrando");
    }));

    ["dragleave", "drop"].forEach(tipo => zona.addEventListener(tipo, evento => {
        evento.preventDefault();
        zona.classList.remove("Arrastrando");
    }));

    zona.addEventListener("drop", evento => elegirArchivo(evento.dataTransfer?.files?.[0]));

    // Pegar una captura (Ctrl+V) mientras el modal esta abierto
    modalElemento.addEventListener("paste", function (evento) {
        const item = Array.from(evento.clipboardData?.items || []).find(i => i.kind === "file");
        if (!item) return;
        const pegado = item.getAsFile();
        if (pegado) {
            evento.preventDefault();
            elegirArchivo(new File([pegado], pegado.name || "captura.png", { type: pegado.type }));
        }
    });

    // ---------- enviar ----------

    function validar() {
        let valido = true;
        const valor = leerMonto(monto.value);
        const montoOk = Number.isFinite(valor) && valor > 0 && (!saldoMaximo || valor <= saldoMaximo + 0.001);
        monto.classList.toggle("is-invalid", !montoOk);
        valido = valido && montoOk;

        const fechaOk = Boolean(fecha.value) && fecha.value <= fecha.max;
        fecha.classList.toggle("is-invalid", !fechaOk);
        valido = valido && fechaOk;

        const respaldoOk = Boolean(referencia.value.trim()) || Boolean(archivoElegido);
        ayudaRespaldo.classList.toggle("text-danger", !respaldoOk);
        if (!respaldoOk) {
            mostrarError("Adjunte la captura del comprobante o escriba el número de referencia.");
        }

        return valido && respaldoOk;
    }

    formulario.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        mostrarError("");

        if (!validar()) return;

        const datos = new FormData();
        datos.append("__RequestVerificationToken",
            document.querySelector('input[name="__RequestVerificationToken"]')?.value || "");
        datos.append("idEstudiante", formulario.dataset.idEstudiante);
        datos.append("idPago", campoIdPago.value);
        datos.append("idMetodoPago", formulario.querySelector('input[name="idMetodoPago"]:checked')?.value || "");
        datos.append("monto", String(leerMonto(monto.value)));
        datos.append("fechaPago", fecha.value);
        datos.append("referencia", referencia.value.trim());
        datos.append("comentario", comentario.value.trim());
        if (archivoElegido) datos.append("comprobante", archivoElegido, archivoElegido.name);

        botonEnviar.disabled = true;
        botonEnviar.querySelector(".spinner-border").classList.remove("d-none");

        try {
            const respuesta = await fetch(formulario.dataset.url, {
                method: "POST",
                body: datos,
                credentials: "same-origin",
                headers: { Accept: "application/json" }
            });

            if (respuesta.redirected || respuesta.status === 401) {
                throw new Error("Su sesión venció. Vuelva a iniciar sesión.");
            }

            let cuerpo = null;
            try { cuerpo = await respuesta.json(); } catch { /* sin cuerpo */ }

            if (!respuesta.ok) {
                throw new Error(cuerpo?.mensaje || "No se pudo enviar el reporte. Intente de nuevo.");
            }

            window.bootstrap.Modal.getOrCreateInstance(modalElemento).hide();

            if (typeof window.MostrarToast === "function") {
                window.MostrarToast(cuerpo?.mensaje || "Pago reportado.", "success");
            }

            // Se recarga para ver el cargo "En revision" y el reporte en la lista
            window.setTimeout(() => {
                window.location.hash = "PagosReportados";
                window.location.reload();
            }, 900);
        } catch (error) {
            mostrarError(error.message || "No se pudo enviar el reporte.");
        } finally {
            botonEnviar.disabled = false;
            botonEnviar.querySelector(".spinner-border").classList.add("d-none");
        }
    });
})();

// Detalle de un pago reportado: la tabla solo lista; el boton "Ver" copia la
// tarjeta del reporte (un <template> que arma el servidor) dentro del modal.
(function () {
    "use strict";

    const modal = document.getElementById("ModalDetalleReporte");
    const contenido = document.getElementById("ContenidoDetalleReporte");
    if (!modal || !contenido) return;

    document.addEventListener("click", function (evento) {
        const boton = evento.target.closest("[data-ver-reporte]");
        if (!boton) return;

        const plantilla = document.getElementById(`DetalleReporte${boton.dataset.verReporte}`);
        if (!plantilla) return;

        contenido.replaceChildren(plantilla.content.cloneNode(true));
        window.bootstrap.Modal.getOrCreateInstance(modal).show();
    });

    modal.addEventListener("hidden.bs.modal", () => contenido.replaceChildren());
})();
