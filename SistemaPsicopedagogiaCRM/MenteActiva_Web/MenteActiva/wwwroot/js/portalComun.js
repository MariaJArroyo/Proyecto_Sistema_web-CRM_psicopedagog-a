(function () {
    "use strict";

    function nodo(etiqueta, texto, clase) {
        const elemento = document.createElement(etiqueta);
        if (texto !== undefined && texto !== null) elemento.textContent = String(texto);
        if (clase) elemento.className = clase;
        return elemento;
    }

    function fecha(valor) {
        const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(valor || ""));
        return partes ? `${partes[3]}/${partes[2]}/${partes[1]}` : "Sin fecha";
    }

    function dinero(valor) {
        return new Intl.NumberFormat("es-CR", {
            style: "currency", currency: "CRC"
        }).format(Number(valor) || 0);
    }

    function avisar(elemento, texto, tipo = "danger") {
        if (!elemento) return;
        elemento.className = `alert alert-${tipo}`;
        elemento.textContent = texto;
        elemento.hidden = !texto;
    }

    async function comprobar(respuesta) {
        if (respuesta.redirected || respuesta.status === 401) {
            throw new Error("Su sesión no está disponible. Vuelva a iniciar sesión.");
        }
        if (respuesta.status === 403) {
            throw new Error("No tiene permiso para consultar esta información.");
        }
        if (!respuesta.ok) {
            let mensaje = "No se pudo completar la consulta. Intente nuevamente.";
            try {
                const error = await respuesta.json();
                mensaje = error.mensaje || error.detail || mensaje;
            } catch { /* Se conserva el mensaje general. */ }
            throw new Error(mensaje);
        }
    }

    async function consultarJson(url) {
        let respuesta;
        try {
            respuesta = await fetch(url, {
                credentials: "same-origin", cache: "no-store",
                headers: { Accept: "application/json" }
            });
        } catch {
            throw new Error("No se pudo conectar con el portal. Revise la conexión.");
        }
        await comprobar(respuesta);
        if (!(respuesta.headers.get("Content-Type") || "").includes("json")) {
            throw new Error("No se recibió la información esperada del portal.");
        }
        return respuesta.json();
    }

    function nombreArchivo(respuesta) {
        const cabecera = respuesta.headers.get("Content-Disposition") || "";
        const utf8 = cabecera.match(/filename\*=UTF-8''([^;]+)/i);
        if (utf8) {
            try { return decodeURIComponent(utf8[1].trim()); }
            catch { return "tarea"; }
        }
        const nombre = cabecera.match(/filename="([^"]+)"|filename=([^;]+)/i);
        return nombre ? (nombre[1] || nombre[2]).trim() : "tarea";
    }

    async function descargarArchivo(url) {
        let respuesta;
        try {
            respuesta = await fetch(url, {
                credentials: "same-origin", cache: "no-store"
            });
        } catch {
            throw new Error("No se pudo conectar con el servicio de archivos.");
        }
        await comprobar(respuesta);
        const tipo = respuesta.headers.get("Content-Type") || "";
        if (/text\/html|application\/(?:problem\+)?json/i.test(tipo)) {
            throw new Error("El servicio no devolvió un archivo descargable.");
        }
        const archivo = await respuesta.blob();
        const urlTemporal = URL.createObjectURL(archivo);
        try {
            const enlace = nodo("a");
            enlace.href = urlTemporal;
            enlace.download = nombreArchivo(respuesta);
            document.body.appendChild(enlace);
            enlace.click();
            enlace.remove();
        } finally {
            window.setTimeout(() => URL.revokeObjectURL(urlTemporal), 1000);
        }
    }

    function iniciarPantalla(id) {
        const pantalla = document.getElementById(id);
        if (!pantalla) return null;
        if (pantalla.dataset.portalIniciado === "1") return pantalla;
        pantalla.dataset.portalIniciado = "1";
        const idEstudiante = Number(pantalla.dataset.portalEstudiante);
        if (Number.isInteger(idEstudiante) && idEstudiante > 0) {
            document.querySelectorAll("a[href]").forEach(enlace => {
                // Cada opción del selector conserva el estudiante al que apunta.
                if (enlace.hasAttribute("data-portal-cambiar-estudiante")) return;
                const url = new URL(enlace.href, window.location.origin);
                const esPortal = /\/Portal\/(Inicio|Citas|Progreso|Tareas|Pagos)\/?$/i.test(url.pathname);
                if (url.origin !== window.location.origin || !esPortal) return;
                // Evita duplicar IdEstudiante e idEstudiante por diferencias de mayúsculas.
                for (const clave of Array.from(url.searchParams.keys())) {
                    if (clave.toLowerCase() === "idestudiante") url.searchParams.delete(clave);
                }
                url.searchParams.set("idEstudiante", String(idEstudiante));
                enlace.href = url.toString();
            });
        }
        pantalla.querySelectorAll("[data-portal-filtros]").forEach(formulario => {
            formulario.addEventListener("change", evento => {
                if (evento.target.matches("select")) formulario.requestSubmit();
            });
        });
        return pantalla;
    }

    function mostrarModal(id) {
        const elemento = document.getElementById(id);
        if (!elemento || !window.bootstrap?.Modal) {
            throw new Error("No se pudo abrir el detalle. Recargue la página.");
        }
        window.bootstrap.Modal.getOrCreateInstance(elemento).show();
    }

    window.PortalComun = {
        iniciarPantalla, nodo, fecha, dinero, avisar,
        consultarJson, descargarArchivo, mostrarModal
    };
})();
