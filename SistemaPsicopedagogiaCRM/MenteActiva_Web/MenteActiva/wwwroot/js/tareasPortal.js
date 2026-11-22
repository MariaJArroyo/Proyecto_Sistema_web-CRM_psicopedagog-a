(function () {
    "use strict";
    const comun = window.PortalComun;
    const pantalla = comun.iniciarPantalla("PortalTareas");
    if (!pantalla) return;
    const alerta = document.getElementById("MensajeDescargaTarea");
    pantalla.addEventListener("click", async evento => {
        const boton = evento.target.closest("[data-descargar-tarea]");
        if (!boton || boton.disabled) return;
        const texto = boton.innerHTML;
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");
        boton.textContent = "Descargando…";
        comun.avisar(alerta, "");
        try {
            await comun.descargarArchivo(boton.dataset.url);
        } catch (error) {
            comun.avisar(alerta, error.message || "No se pudo descargar el archivo.");
        } finally {
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
            boton.innerHTML = texto;
        }
    });
})();


// Entregar una tarea: comentario y, si quiere, una foto o PDF del trabajo.
// Se manda como FormData a PortalController.EntregarTarea, que lo reenvia al API.
(function () {
    "use strict";

    const modalElemento = document.getElementById("ModalEntregarTarea");
    const formulario = document.getElementById("FormularioEntregarTarea");
    if (!modalElemento || !formulario) return;

    const TAMANO_MAXIMO = 10 * 1024 * 1024;
    const TIPOS = ["image/jpeg", "image/png", "image/webp", "application/pdf"];

    const alerta = document.getElementById("AlertaEntregarTarea");
    const avisoReemplazo = document.getElementById("AvisoReemplazoEntrega");
    const nombreTarea = document.getElementById("NombreTareaEntrega");
    const comentario = document.getElementById("ComentarioEntrega");
    const archivo = document.getElementById("ArchivoEntrega");
    const zona = document.getElementById("ZonaEntrega");
    const vacia = zona.querySelector(".ZonaComprobante-Vacia");
    const vista = zona.querySelector(".ZonaComprobante-Vista");
    const imagen = vista.querySelector("img");
    const iconoPdf = vista.querySelector(".ZonaComprobante-Pdf");
    const nombreArchivo = vista.querySelector(".ZonaComprobante-Nombre");
    const botonEnviar = document.getElementById("BotonEnviarEntrega");

    let idTareaAsignacion = 0;
    let archivoElegido = null;
    let urlVista = null;

    function mostrarError(texto) {
        alerta.textContent = texto;
        alerta.hidden = !texto;
        if (texto) alerta.scrollIntoView({ block: "nearest" });
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
            mostrarError("El archivo debe ser una foto (JPG, PNG o WEBP) o un PDF.");
            return;
        }

        if (nuevo.size > TAMANO_MAXIMO) {
            mostrarError("El archivo no puede pasar de 10 MB.");
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
    }

    // ---------- abrir desde la tarjeta ----------

    document.addEventListener("click", function (evento) {
        const boton = evento.target.closest("[data-entregar-tarea]");
        if (!boton) return;

        formulario.reset();
        limpiarVista();
        mostrarError("");

        idTareaAsignacion = Number(boton.dataset.idTareaAsignacion) || 0;
        nombreTarea.textContent = boton.dataset.titulo || "";
        avisoReemplazo.hidden = boton.dataset.reemplaza !== "1";

        window.bootstrap.Modal.getOrCreateInstance(modalElemento).show();
    });

    modalElemento.addEventListener("shown.bs.modal", () => comentario.focus());
    modalElemento.addEventListener("hidden.bs.modal", limpiarVista);

    // ---------- archivo: elegir, arrastrar o pegar ----------

    archivo.addEventListener("change", () => elegirArchivo(archivo.files?.[0]));

    document.getElementById("QuitarArchivoEntrega").addEventListener("click", function (evento) {
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

    formulario.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        mostrarError("");

        if (!idTareaAsignacion) {
            mostrarError("Seleccione una tarea válida.");
            return;
        }

        const datos = new FormData();
        datos.append("__RequestVerificationToken",
            document.querySelector('input[name="__RequestVerificationToken"]')?.value || "");
        datos.append("idEstudiante", formulario.dataset.idEstudiante);
        datos.append("idTareaAsignacion", String(idTareaAsignacion));
        datos.append("comentario", comentario.value.trim());
        if (archivoElegido) datos.append("archivo", archivoElegido, archivoElegido.name);

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
                throw new Error(cuerpo?.mensaje || "No se pudo entregar la tarea. Intente de nuevo.");
            }

            window.bootstrap.Modal.getOrCreateInstance(modalElemento).hide();

            if (typeof window.MostrarToast === "function") {
                window.MostrarToast(cuerpo?.mensaje || "Tarea entregada.", "success");
            }

            // Se recarga para ver la tarjeta "En revisión" con su entrega
            window.setTimeout(() => window.location.reload(), 900);
        } catch (error) {
            mostrarError(error.message || "No se pudo entregar la tarea.");
        } finally {
            botonEnviar.disabled = false;
            botonEnviar.querySelector(".spinner-border").classList.add("d-none");
        }
    });
})();
