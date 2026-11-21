// Ficha del estudiante > Documentos: lista, subida, visibilidad en el portal
// y eliminacion. Todo pasa por AdminController, que lo reenvia al API.
document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    const seccion = document.getElementById("DocumentosEstudiante");
    if (!seccion) return;

    const TAMANO_MAXIMO = 15 * 1024 * 1024;
    const EXTENSIONES = [".pdf", ".jpg", ".jpeg", ".png", ".webp", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx"];

    const idEstudiante = seccion.dataset.idEstudiante;
    const rutas = {
        lista: seccion.dataset.urlLista,
        subir: seccion.dataset.urlSubir,
        ver: seccion.dataset.urlVer,
        visibilidad: seccion.dataset.urlVisibilidad,
        eliminar: seccion.dataset.urlEliminar
    };

    const cuerpo = document.getElementById("CuerpoDocumentos");
    const tabla = document.getElementById("TablaDocumentos");
    const vacio = document.getElementById("VacioDocumentos");
    const textoVacio = document.getElementById("TextoVacioDocumentos");
    const contador = document.getElementById("ContadorDocumentos");
    const aviso = document.getElementById("AvisoDocumentos");
    const buscador = document.getElementById("BuscarDocumento");

    const modalSubir = document.getElementById("ModalSubirDocumento");
    const formSubir = document.getElementById("FormularioSubirDocumento");
    const avisoSubir = document.getElementById("AvisoSubirDocumento");
    const zona = document.getElementById("ZonaDocumento");
    const campoArchivo = document.getElementById("ArchivoDocumento");
    const campoTitulo = document.getElementById("TituloDocumento");
    const campoDescripcion = document.getElementById("DescripcionDocumento");
    const campoVisible = document.getElementById("VisibleDocumento");
    const botonSubir = document.getElementById("BotonSubirDocumento");

    const modalEliminar = document.getElementById("ModalEliminarDocumento");
    const botonEliminar = document.getElementById("ConfirmarEliminarDocumento");
    const avisoEliminar = document.getElementById("AvisoEliminarDocumento");

    let documentos = [];
    let archivoElegido = null;
    let idPorEliminar = null;

    // ---------- utilidades ----------

    function escapar(texto) {
        const div = document.createElement("div");
        div.textContent = texto ?? "";
        return div.innerHTML.replaceAll('"', "&quot;");
    }

    function token() {
        return seccion.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function mostrar(elemento, texto, tipo = "danger") {
        elemento.className = texto ? `alert alert-${tipo}` : "alert d-none";
        elemento.textContent = texto || "";
    }

    function ocupado(boton, activo) {
        boton.disabled = activo;
        boton.querySelector(".spinner-border")?.classList.toggle("d-none", !activo);
    }

    function notificar(texto, tipo = "exito") {
        if (typeof window.MostrarToast === "function") window.MostrarToast(texto, tipo);
    }

    async function leer(respuesta) {
        try { return await respuesta.json(); } catch { return null; }
    }

    async function enviar(url, campos) {
        const datos = campos instanceof FormData ? campos : new FormData();
        if (!(campos instanceof FormData)) {
            Object.entries(campos || {}).forEach(([k, v]) => datos.append(k, v));
        }
        datos.append("__RequestVerificationToken", token());

        const respuesta = await fetch(url, { method: "POST", body: datos });
        const json = await leer(respuesta);
        if (!respuesta.ok) throw new Error(json?.mensaje || "No se pudo completar la operación.");
        return json;
    }

    function tamano(bytes) {
        if (!bytes) return "—";
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${Math.round(bytes / 1024)} KB`;
        return `${(bytes / 1024 / 1024).toFixed(1).replace(".", ",")} MB`;
    }

    function fecha(texto) {
        return texto
            ? new Date(texto).toLocaleDateString("es-CR", { day: "numeric", month: "short", year: "numeric" })
            : "—";
    }

    // Icono y color segun el tipo
    function tipoVisual(tipo) {
        const t = String(tipo || "");
        if (t === "application/pdf") return { icono: "bi-file-earmark-pdf", clase: "DocTipo-Pdf", previa: true };
        if (t.startsWith("image/")) return { icono: "bi-file-earmark-image", clase: "DocTipo-Imagen", previa: true };
        if (t.includes("word")) return { icono: "bi-file-earmark-word", clase: "DocTipo-Word", previa: false };
        if (t.includes("sheet") || t.includes("excel")) return { icono: "bi-file-earmark-excel", clase: "DocTipo-Excel", previa: false };
        if (t.includes("presentation") || t.includes("powerpoint")) return { icono: "bi-file-earmark-slides", clase: "DocTipo-Ppt", previa: false };
        return { icono: "bi-file-earmark", clase: "", previa: false };
    }

    // ---------- lista ----------

    function fila(d) {
        const visual = tipoVisual(d.tipoArchivo);
        const urlVer = `${rutas.ver}?id=${d.idDocumento}`;
        const urlBajar = `${rutas.ver}?id=${d.idDocumento}&descargar=true`;
        const idInterruptor = `VisibleDoc${d.idDocumento}`;

        return `
            <tr data-id="${d.idDocumento}">
                <td>
                    <div class="d-flex align-items-center gap-3">
                        <span class="DocTipo ${visual.clase}" aria-hidden="true"><i class="bi ${visual.icono}"></i></span>
                        <div class="min-w-0">
                            <a href="${visual.previa ? urlVer : urlBajar}" ${visual.previa ? 'target="_blank" rel="noopener"' : ""}
                               class="fw-semibold text-decoration-none d-block text-truncate">${escapar(d.titulo)}</a>
                            ${d.descripcion ? `<div class="TextoSuave small text-truncate">${escapar(d.descripcion)}</div>` : ""}
                            <div class="TextoSuave small text-truncate">${escapar(d.nombreOriginal)}</div>
                        </div>
                    </div>
                </td>
                <td class="d-none d-md-table-cell small">
                    ${fecha(d.fechaCreacion)}
                    ${d.subidoPor ? `<div class="TextoSuave">${escapar(d.subidoPor)}</div>` : ""}
                </td>
                <td class="d-none d-lg-table-cell text-end small TextoSuave">${tamano(d.tamanoBytes)}</td>
                <td>
                    <div class="form-check form-switch mb-0 InterruptorPortal">
                        <input class="form-check-input" type="checkbox" role="switch" id="${idInterruptor}"
                               data-visibilidad="${d.idDocumento}" ${d.visibleEnPortal ? "checked" : ""}>
                        <label class="form-check-label small" for="${idInterruptor}">
                            ${d.visibleEnPortal ? '<i class="bi bi-eye me-1"></i>Visible' : '<i class="bi bi-eye-slash me-1"></i>Oculto'}
                        </label>
                    </div>
                </td>
                <td class="text-end text-nowrap">
                    ${visual.previa
                        ? `<a href="${urlVer}" target="_blank" rel="noopener" class="btn btn-sm btn-outline-secondary border-0" title="Ver" aria-label="Ver ${escapar(d.titulo)}"><i class="bi bi-eye"></i></a>`
                        : ""}
                    <a href="${urlBajar}" class="btn btn-sm btn-outline-secondary border-0" title="Descargar" aria-label="Descargar ${escapar(d.titulo)}"><i class="bi bi-download"></i></a>
                    <button type="button" class="btn btn-sm btn-outline-danger border-0" data-eliminar="${d.idDocumento}"
                            title="Eliminar" aria-label="Eliminar ${escapar(d.titulo)}"><i class="bi bi-trash"></i></button>
                </td>
            </tr>`;
    }

    function pintar() {
        const texto = buscador.value.trim().toLowerCase();
        const lista = texto
            ? documentos.filter(d => [d.titulo, d.descripcion, d.nombreOriginal].some(v => String(v || "").toLowerCase().includes(texto)))
            : documentos;

        const visibles = documentos.filter(d => d.visibleEnPortal).length;
        contador.textContent = documentos.length;
        contador.title = `${visibles} visible${visibles === 1 ? "" : "s"} en el portal`;

        cuerpo.innerHTML = lista.map(fila).join("");
        tabla.classList.toggle("d-none", lista.length === 0);
        vacio.classList.toggle("d-none", lista.length > 0);
        textoVacio.textContent = documentos.length
            ? "Ningún documento coincide con la búsqueda."
            : "Todavía no hay documentos para este estudiante. Use “Subir documento”.";
    }

    async function cargar() {
        mostrar(aviso, "");
        try {
            const respuesta = await fetch(`${rutas.lista}?idEstudiante=${idEstudiante}`, { headers: { Accept: "application/json" } });
            const json = await leer(respuesta);
            if (!respuesta.ok) throw new Error(json?.mensaje || "No se pudieron cargar los documentos.");
            documentos = Array.isArray(json) ? json : [];
        } catch (error) {
            documentos = [];
            mostrar(aviso, error.message);
        }
        pintar();
    }

    buscador.addEventListener("input", pintar);

    // ---------- visibilidad ----------

    cuerpo.addEventListener("change", async function (evento) {
        const interruptor = evento.target.closest("[data-visibilidad]");
        if (!interruptor) return;

        const id = Number(interruptor.dataset.visibilidad);
        const visible = interruptor.checked;
        interruptor.disabled = true;

        try {
            const json = await enviar(`${rutas.visibilidad}?id=${id}`, { visible });
            const doc = documentos.find(d => d.idDocumento === id);
            if (doc) doc.visibleEnPortal = visible;
            pintar();
            notificar(json?.mensaje || (visible ? "Visible en el portal." : "Oculto en el portal."));
        } catch (error) {
            interruptor.checked = !visible;
            interruptor.disabled = false;
            mostrar(aviso, error.message);
        }
    });

    // ---------- subir ----------

    function elegir(archivo) {
        mostrar(avisoSubir, "");
        if (!archivo) return;

        const extension = (archivo.name.match(/\.[^.]+$/)?.[0] || "").toLowerCase();
        if (!EXTENSIONES.includes(extension)) {
            mostrar(avisoSubir, "Formato no permitido. Suba un PDF, una imagen o un documento de Word, Excel o PowerPoint.");
            return;
        }
        if (archivo.size > TAMANO_MAXIMO) {
            mostrar(avisoSubir, "El archivo no puede pasar de 15 MB.");
            return;
        }

        archivoElegido = archivo;
        zona.classList.add("TieneArchivo");
        zona.querySelector(".ZonaArchivo-Vacia").classList.add("d-none");
        const elegido = zona.querySelector(".ZonaArchivo-Elegido");
        elegido.classList.remove("d-none");
        elegido.querySelector(".ZonaArchivo-Nombre").textContent = archivo.name;
        elegido.querySelector(".ZonaArchivo-Tamano").textContent = tamano(archivo.size);

        // Si no escribio titulo, se propone el nombre del archivo sin extension
        if (!campoTitulo.value.trim()) {
            campoTitulo.value = archivo.name.replace(/\.[^.]+$/, "").replace(/[_-]+/g, " ").trim().slice(0, 150);
        }
        campoTitulo.classList.remove("is-invalid");
    }

    function reiniciar() {
        formSubir.reset();
        archivoElegido = null;
        campoArchivo.value = "";
        zona.classList.remove("TieneArchivo", "Arrastrando");
        zona.querySelector(".ZonaArchivo-Vacia").classList.remove("d-none");
        zona.querySelector(".ZonaArchivo-Elegido").classList.add("d-none");
        campoTitulo.classList.remove("is-invalid");
        mostrar(avisoSubir, "");
    }

    modalSubir.addEventListener("show.bs.modal", reiniciar);
    campoArchivo.addEventListener("change", () => elegir(campoArchivo.files?.[0]));

    ["dragenter", "dragover"].forEach(t => zona.addEventListener(t, e => { e.preventDefault(); zona.classList.add("Arrastrando"); }));
    ["dragleave", "drop"].forEach(t => zona.addEventListener(t, e => { e.preventDefault(); zona.classList.remove("Arrastrando"); }));
    zona.addEventListener("drop", e => elegir(e.dataTransfer?.files?.[0]));

    formSubir.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        mostrar(avisoSubir, "");

        if (!archivoElegido) {
            mostrar(avisoSubir, "Seleccione un archivo.");
            return;
        }

        const titulo = campoTitulo.value.trim();
        campoTitulo.classList.toggle("is-invalid", !titulo);
        if (!titulo) return;

        const datos = new FormData();
        datos.append("idEstudiante", idEstudiante);
        datos.append("archivo", archivoElegido, archivoElegido.name);
        datos.append("titulo", titulo);
        datos.append("descripcion", campoDescripcion.value.trim());
        datos.append("visibleEnPortal", campoVisible.checked ? "true" : "false");

        ocupado(botonSubir, true);
        try {
            const json = await enviar(rutas.subir, datos);
            bootstrap.Modal.getOrCreateInstance(modalSubir).hide();
            notificar(json?.mensaje || "Documento subido.");
            await cargar();
        } catch (error) {
            mostrar(avisoSubir, error.message);
        } finally {
            ocupado(botonSubir, false);
        }
    });

    // ---------- eliminar ----------

    cuerpo.addEventListener("click", function (evento) {
        const boton = evento.target.closest("[data-eliminar]");
        if (!boton) return;

        idPorEliminar = Number(boton.dataset.eliminar);
        const doc = documentos.find(d => d.idDocumento === idPorEliminar);
        document.getElementById("NombreEliminarDocumento").textContent = doc?.titulo || "";
        avisoEliminar.classList.add("d-none");
        bootstrap.Modal.getOrCreateInstance(modalEliminar).show();
    });

    botonEliminar.addEventListener("click", async function () {
        if (!idPorEliminar) return;

        ocupado(botonEliminar, true);
        try {
            const json = await enviar(`${rutas.eliminar}?id=${idPorEliminar}`, {});
            bootstrap.Modal.getOrCreateInstance(modalEliminar).hide();
            notificar(json?.mensaje || "Documento eliminado.");
            idPorEliminar = null;
            await cargar();
        } catch (error) {
            avisoEliminar.textContent = error.message;
            avisoEliminar.classList.remove("d-none");
        } finally {
            ocupado(botonEliminar, false);
        }
    });

    cargar();
});
