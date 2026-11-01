(function () {
    "use strict";
    const comun = window.PortalComun;
    const pantalla = comun.iniciarPantalla("PortalProgreso");
    if (!pantalla) return;
    const alerta = document.getElementById("AlertaProgreso");
    let consultando = false;
    const etiquetas = {
        estudiante: "Datos del estudiante", nombre: "Nombre", nivel: "Nivel educativo",
        institucion: "Institución", areasDificultad: "Áreas de dificultad", areasTrabajadas: "Áreas trabajadas",
        sesiones: "Sesiones", fecha: "Fecha", tipoAtencion: "Tipo de atención", duracionMinutos: "Duración en minutos",
        objetivo: "Objetivo", tema: "Tema trabajado", actividadesRealizadas: "Actividades realizadas",
        avances: "Avances", objetivosAlcanzados: "Objetivos alcanzados", habilidadesDesarrolladas: "Habilidades desarrolladas",
        acuerdos: "Acuerdos", recomendaciones: "Recomendaciones", observaciones: "Observaciones",
        planes: "Planes de intervención", titulo: "Título", estado: "Estado", objetivoGeneral: "Objetivo general",
        fechaInicio: "Fecha de inicio", fechaFin: "Fecha de finalización", estrategias: "Estrategias",
        descripcion: "Descripción", orden: "Orden", actividades: "Actividades", completada: "Completada",
        actividadesCompletadas: "Actividades completadas", totalActividades: "Total de actividades"
    };

    function valorTexto(valor) {
        if (typeof valor === "boolean") return valor ? "Sí" : "No";
        if (typeof valor === "string" && /^\d{4}-\d{2}-\d{2}(T|$)/.test(valor)) return comun.fecha(valor);
        return String(valor);
    }

    function agregarContenido(contenedor, valor, nivel = 0) {
        if (valor === null || valor === undefined || valor === "") return;
        if (nivel > 12) { contenedor.append(comun.nodo("p", "Consulte el PDF para ver el contenido completo.")); return; }
        if (Array.isArray(valor)) {
            if (!valor.length) { contenedor.append(comun.nodo("p", "Sin registros.")); return; }
            valor.forEach((item, indice) => {
                const bloque = comun.nodo("div", undefined, "border rounded p-3 mb-3");
                if (valor.length > 1) bloque.append(comun.nodo("p", `Registro ${indice + 1}`, "fw-semibold"));
                agregarContenido(bloque, item, nivel + 1);
                contenedor.append(bloque);
            });
        } else if (typeof valor === "object") {
            Object.entries(valor).forEach(([clave, dato]) => {
                if (dato === null || dato === undefined || dato === "") return;
                const nombre = etiquetas[clave] || clave.replace(/([a-z])([A-Z])/g, "$1 $2").replace(/_/g, " ");
                const bloque = comun.nodo("div", undefined, "mb-3");
                bloque.append(comun.nodo("p", nombre, "fw-semibold mb-1"));
                agregarContenido(bloque, dato, nivel + 1);
                contenedor.append(bloque);
            });
        } else {
            const parrafo = comun.nodo("p", valorTexto(valor));
            parrafo.style.whiteSpace = "pre-wrap";
            contenedor.append(parrafo);
        }
    }

    function mostrarDetalle(reporte) {
        document.getElementById("TituloDetalleReportePortal").textContent = reporte.titulo || "Detalle del reporte";
        const contenido = document.getElementById("ContenidoDetalleReportePortal");
        contenido.replaceChildren();
        contenido.append(
            comun.nodo("p", reporte.estudiante || "", "fw-semibold"),
            comun.nodo("p", `Período: ${comun.fecha(reporte.periodoInicio)} al ${comun.fecha(reporte.periodoFin)}`),
            comun.nodo("p", `Generado: ${comun.fecha(reporte.fechaGeneracion)}`, "TextoSuave")
        );
        let datos = reporte.contenido || "Sin contenido adicional registrado.";
        if (typeof datos === "string") {
            try { datos = JSON.parse(datos); } catch { /* Reporte guardado como texto. */ }
        }
        agregarContenido(contenido, datos);
        comun.mostrarModal("ModalReportePortal");
    }

    pantalla.addEventListener("click", async evento => {
        const boton = evento.target.closest("[data-reporte-accion]");
        if (!boton || consultando) return;
        consultando = true;
        const texto = boton.textContent;
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");
        boton.textContent = "Preparando…";
        comun.avisar(alerta, "");
        try {
            const reporte = await comun.consultarJson(boton.dataset.url);
            if (!reporte || !reporte.idReporte) throw new Error("No se recibió el reporte solicitado.");
            if (boton.dataset.reporteAccion === "pdf") {
                if (!window.pdfMake || !window.PortalPdf) throw new Error("No se cargó la herramienta PDF. Revise la conexión y recargue la página.");
                const nombre = String(reporte.titulo || "Reporte")
                    .replace(/[<>:"/\\|?*\u0000-\u001F]/g, "").trim().slice(0, 150).replace(/[. ]+$/, "") || "Reporte";
                await window.pdfMake.createPdf(window.PortalPdf.crearDefinicion(reporte)).download(`${nombre}.pdf`);
                comun.avisar(alerta, "El PDF se envió al navegador para descargar.", "success");
            } else {
                mostrarDetalle(reporte);
            }
        } catch (error) {
            comun.avisar(alerta, error.message || "No se pudo consultar el reporte.");
        } finally {
            consultando = false;
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
            boton.textContent = texto;
        }
    });
})();
