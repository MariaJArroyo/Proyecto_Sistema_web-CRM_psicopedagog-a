(function () {
    "use strict";
    const comun = window.PortalComun;
    const pantalla = comun.iniciarPantalla("PortalRecursos");
    if (!pantalla) return;
    const alerta = document.getElementById("MensajeRecurso");

    function ocupar(boton, texto) {
        const original = boton.innerHTML;
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");
        boton.textContent = texto;
        return () => {
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
            boton.innerHTML = original;
        };
    }

    async function descargar(boton) {
        const liberar = ocupar(boton, "Descargando…");
        comun.avisar(alerta, "");
        try {
            await comun.descargarArchivo(boton.dataset.url);
        } catch (error) {
            comun.avisar(alerta, error.message || "No se pudo descargar el archivo.");
        } finally {
            liberar();
        }
    }

    async function abrirEnlace(boton) {
        // La pestana se abre ya, con el clic, para que el navegador no la bloquee
        // por esperar la verificacion; si el enlace falla se cierra.
        const ventana = window.open("about:blank", "_blank");
        if (ventana) ventana.opener = null;
        const liberar = ocupar(boton, "Verificando…");
        comun.avisar(alerta, "");
        try {
            const resultado = await comun.consultarJson(boton.dataset.url);
            if (!resultado.disponible) {
                if (ventana) ventana.close();
                comun.avisar(alerta, resultado.mensaje || "El enlace no está disponible en este momento.");
                return;
            }
            if (ventana) ventana.location.href = boton.dataset.destino;
            else window.location.assign(boton.dataset.destino);
        } catch (error) {
            if (ventana) ventana.close();
            comun.avisar(alerta, error.message || "No se pudo verificar el enlace.");
        } finally {
            liberar();
        }
    }

    pantalla.addEventListener("click", evento => {
        const botonPdf = evento.target.closest("[data-descargar-recurso]");
        if (botonPdf && !botonPdf.disabled) return descargar(botonPdf);
        const botonEnlace = evento.target.closest("[data-abrir-enlace]");
        if (botonEnlace && !botonEnlace.disabled) return abrirEnlace(botonEnlace);
    });
})();
