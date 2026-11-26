// Ficha del estudiante: subir, cambiar o quitar la foto del encabezado.
// Pasa por /Admin/SubirFotoEstudiante y /Admin/QuitarFotoEstudiante, que
// reenvian al API. Solo se carga para Administrador y Psicopedagoga.
(function () {
    "use strict";

    const contenedor = document.getElementById("FotoEstudiante");
    const archivo = document.getElementById("ArchivoFotoEstudiante");
    if (!contenedor || !archivo) return;

    const TAMANO_MAXIMO = 3 * 1024 * 1024;
    const TIPOS = ["image/jpeg", "image/png", "image/webp"];

    const imagen = contenedor.querySelector(".FotoEstudiante-Imagen");
    const iniciales = contenedor.querySelector(".FotoEstudiante-Iniciales");
    const textoElegir = contenedor.querySelector("[data-texto-elegir]");
    const itemQuitar = contenedor.querySelector("[data-foto-quitar-item]");
    const idEstudiante = contenedor.dataset.idEstudiante;

    function avisar(mensaje, tipo) {
        if (typeof MostrarToast === "function") MostrarToast(mensaje, tipo);
    }

    function token() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function ocupado(si) {
        contenedor.classList.toggle("Cargando", si);
        contenedor.setAttribute("aria-busy", si ? "true" : "false");
    }

    function mostrar(tieneFoto, url) {
        contenedor.classList.toggle("TieneFoto", tieneFoto);

        if (tieneFoto) {
            imagen.src = url;
            imagen.hidden = false;
            iniciales.hidden = true;
        } else {
            imagen.removeAttribute("src");
            imagen.hidden = true;
            iniciales.hidden = false;
        }

        textoElegir.textContent = tieneFoto ? "Cambiar foto" : "Subir foto";
        itemQuitar.hidden = !tieneFoto;
    }

    async function enviar(url, datos) {
        const respuesta = await fetch(url, {
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
            throw new Error(cuerpo?.mensaje || "No se pudo guardar la foto.");
        }

        return cuerpo || {};
    }

    // ---------- subir o cambiar ----------

    contenedor.querySelector("[data-foto-elegir]").addEventListener("click", () => archivo.click());

    archivo.addEventListener("change", async function () {
        const elegido = archivo.files?.[0];
        archivo.value = "";
        if (!elegido) return;

        if (!TIPOS.includes(elegido.type)) {
            avisar("La foto debe ser una imagen JPG, PNG o WEBP.", "error");
            return;
        }

        if (elegido.size > TAMANO_MAXIMO) {
            avisar("La foto no puede pasar de 3 MB.", "error");
            return;
        }

        // Vista previa inmediata mientras sube; si falla se vuelve a la anterior
        const anterior = { tieneFoto: contenedor.classList.contains("TieneFoto"), url: imagen.src };
        const vistaPrevia = URL.createObjectURL(elegido);
        mostrar(true, vistaPrevia);
        ocupado(true);

        const datos = new FormData();
        datos.append("__RequestVerificationToken", token());
        datos.append("idEstudiante", idEstudiante);
        datos.append("foto", elegido, elegido.name);

        try {
            const cuerpo = await enviar(contenedor.dataset.urlSubir, datos);
            const version = cuerpo.version || Date.now();
            mostrar(true, `${contenedor.dataset.urlFoto}?id=${idEstudiante}&v=${version}`);
            avisar(cuerpo.mensaje || "Foto actualizada.", "success");
        } catch (error) {
            mostrar(anterior.tieneFoto, anterior.url);
            avisar(error.message, "error");
        } finally {
            ocupado(false);
            URL.revokeObjectURL(vistaPrevia);
        }
    });

    // ---------- quitar ----------

    contenedor.querySelector("[data-foto-quitar]").addEventListener("click", async function () {
        const acepto = typeof Confirmar === "function"
            ? await Confirmar({
                titulo: "Quitar foto",
                texto: "Se elimina la foto del estudiante y vuelven a mostrarse sus iniciales. ¿Desea continuar?",
                textoBoton: "Quitar foto",
                tipo: "peligro"
            })
            : window.confirm("¿Quitar la foto del estudiante?");

        if (!acepto) return;

        const datos = new FormData();
        datos.append("__RequestVerificationToken", token());
        datos.append("idEstudiante", idEstudiante);

        ocupado(true);

        try {
            const cuerpo = await enviar(contenedor.dataset.urlQuitar, datos);
            mostrar(false);
            avisar(cuerpo.mensaje || "Foto eliminada.", "success");
        } catch (error) {
            avisar(error.message, "error");
        } finally {
            ocupado(false);
        }
    });

    // Si la imagen no carga (por ejemplo, el archivo ya no esta en el
    // servidor), se muestran las iniciales en vez de un icono roto
    imagen.addEventListener("error", function () {
        if (imagen.getAttribute("src")?.startsWith("blob:")) return;
        mostrar(false);
        itemQuitar.hidden = false;
    });
})();
