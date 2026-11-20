(function () {
    "use strict";

    let exportando = false;

    // El contenido y el estilo del PDF viven en reportePdf.js, compartido con el
    // portal, para que los dos documentos salgan iguales.

    function aviso(mensaje, tipo) {
        const alerta = document.getElementById("AlertaReportes");
        alerta.className = `alert alert-${tipo} mt-3`;
        alerta.textContent = mensaje;
    }

    // =====================================================
    // DESCARGA
    // =====================================================

    async function exportar(id, boton) {
        if (exportando) return;

        exportando = true;
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");

        const iconoOriginal = boton.innerHTML;
        boton.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';

        aviso("Preparando el PDF…", "info");

        try {
            if (!window.pdfMake || !window.ReportePdf) {
                throw new Error("No se cargó la herramienta de PDF. Revise la conexión y recargue la página.");
            }

            const respuesta = await fetch(`/Admin/DetalleReporte?id=${encodeURIComponent(id)}`, {
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok) {
                let mensaje = "El reporte solicitado no está disponible.";

                try {
                    const error = await respuesta.json();
                    mensaje = error.mensaje || mensaje;
                } catch {
                    // Conserva el mensaje general.
                }

                throw new Error(mensaje);
            }

            const reporte = await respuesta.json();

            const tituloArchivo = String(reporte.titulo || "Reporte")
                .replace(/[<>:"/\\|?*\u0000-\u001F]/g, "")
                .trim()
                .slice(0, 150)
                .replace(/[. ]+$/, "");

            const nombreArchivo = `${tituloArchivo || "Reporte"}.pdf`;

            if (window.ReportePdfLogoPromise) {
                await window.ReportePdfLogoPromise;
            }

            await window.pdfMake
                .createPdf(window.ReportePdf.crearDefinicion(reporte))
                .download(nombreArchivo);

            aviso("Reporte exportado exitosamente. La descarga fue enviada al navegador.", "success");
        } catch (error) {
            aviso(error.message || "No se pudo exportar el reporte. Inténtelo nuevamente.", "danger");
        } finally {
            exportando = false;
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
            boton.innerHTML = iconoOriginal;
        }
    }

    // =====================================================
    // EVENTOS
    // =====================================================

    document.getElementById("CuerpoTablaReportes").addEventListener("click", function (evento) {
        const boton = evento.target.closest('button[data-accion="pdf"]');

        if (boton && !boton.disabled) {
            exportar(boton.dataset.id, boton);
        }
    });
})();