
(function () {
    "use strict";

    const $ = id => document.getElementById(id);
    const panel = $("InfoAcademica");

    if (!panel) return;

    const lista = $("ListaInfoAcademica");
    const aviso = $("AvisoInfoAcademica");
    const formulario = $("FormularioInfoAcademica");
    const alerta = $("AlertaInfoAcademica");

    let registros = [];
    let ocupado = false;
    let registroSeleccionado = null;

    function token() {
        const campo = panel.querySelector(
            '[name="__RequestVerificationToken"]'
        );

        return campo ? campo.value : "";
    }

    function mostrarAviso(mensaje, tipo = "success") {
        aviso.className = mensaje
            ? `alert alert-${tipo}`
            : "alert d-none";

        aviso.textContent = mensaje || "";
    }

    function mostrarAvisoEliminar(mensaje = "", tipo = "danger") {
        const elemento = $("AvisoEliminarInfoAcademica");

        elemento.className = mensaje
            ? `alert alert-${tipo}`
            : "alert d-none";

        elemento.textContent = mensaje;
    }

    async function mensajeError(respuesta, general) {
        try {
            const error = await respuesta.json();
            return error.mensaje || error.title || general;
        } catch {
            return general;
        }
    }

    function boton(icono, nombre, clase) {
        const b = document.createElement("button");

        b.type = "button";
        b.className = "btn btn-sm btn-outline-secondary " + clase;
        b.setAttribute("aria-label", nombre);
        b.title = nombre;

        const i = document.createElement("i");
        i.className = "bi " + icono;
        i.setAttribute("aria-hidden", "true");

        b.appendChild(i);

        return b;
    }

    // ==========================================
    // LISTAR INFORMACIÓN ACADÉMICA
    // ==========================================

    function pintar() {
        lista.replaceChildren();

        if (!registros.length) {
            const p = document.createElement("p");

            p.className = "TextoSuave mb-0";
            p.textContent =
                "No hay información académica adicional registrada.";

            lista.appendChild(p);
            return;
        }

        registros.forEach(function (r) {

            const fila = document.createElement("div");

            fila.className =
                "list-group-item px-0 d-flex justify-content-between align-items-start gap-3";

            const datos = document.createElement("div");
            datos.className = "text-break";

            const titulo = document.createElement("div");
            titulo.className = "fw-semibold";
            titulo.textContent = r.titulo;

            datos.appendChild(titulo);

            if (r.detalle) {
                const detalle = document.createElement("p");
                detalle.className = "TextoSuave small mb-0";
                detalle.style.whiteSpace = "pre-line";
                detalle.textContent = r.detalle;

                datos.appendChild(detalle);
            }

            const acciones = document.createElement("div");
            acciones.className = "d-flex gap-1 flex-shrink-0";

            const editar = boton(
                "bi-pencil",
                "Editar información",
                "BotonEditarInfo"
            );

            editar.dataset.id = r.idInfoAcademica;

            const quitar = boton(
                "bi-slash-circle",
                "Eliminar información",
                "BotonQuitarInfo"
            );

            quitar.dataset.id = r.idInfoAcademica;

            acciones.append(editar, quitar);

            fila.append(datos, acciones);

            lista.appendChild(fila);
        });
    }

    // ==========================================
    // CARGAR INFORMACIÓN ACADÉMICA
    // ==========================================

    async function cargar() {
        try {
            const respuesta = await fetch(
                panel.dataset.urlLista +
                "?idEstudiante=" +
                encodeURIComponent(panel.dataset.idEstudiante),
                {
                    headers: {
                        Accept: "application/json"
                    }
                }
            );

            if (!respuesta.ok) {
                throw new Error(
                    "No se pudo cargar la información académica."
                );
            }

            registros = await respuesta.json();

            pintar();

        } catch (error) {

            lista.textContent =
                error.message ||
                "No se pudo cargar la información académica.";
        }
    }

    // ==========================================
    // AGREGAR Y EDITAR INFORMACIÓN ACADÉMICA
    // ==========================================

    function abrirModal(registro) {

        alerta.className = "alert d-none";
        alerta.textContent = "";

        $("IdInfoAcademica").value =
            registro ? registro.idInfoAcademica : "";

        $("TituloInfoAcademica").value =
            registro ? registro.titulo : "";

        $("DetalleInfoAcademica").value =
            registro ? registro.detalle : "";

        $("TituloModalInfoAcademica").textContent =
            registro
                ? "Editar información académica"
                : "Agregar información académica";

        formulario.querySelector(
            'button[type="submit"]'
        ).textContent =
            registro ? "Guardar cambios" : "Agregar";

        bootstrap.Modal
            .getOrCreateInstance($("ModalInfoAcademica"))
            .show();
    }

    $("BotonAgregarInfoAcademica").addEventListener(
        "click",
        () => {
            if (!ocupado) abrirModal(null);
        }
    );

    // ==========================================
    // ABRIR CONFIRMACIÓN DE ELIMINACIÓN
    // ==========================================

    function abrirConfirmacionEliminar(id) {
        registroSeleccionado = registros.find(
            r => Number(r.idInfoAcademica) === Number(id)
        );

        if (!registroSeleccionado) return;

        $("NombreEliminarInfoAcademica").textContent =
            registroSeleccionado.titulo;

        mostrarAvisoEliminar();

        bootstrap.Modal
            .getOrCreateInstance($("ModalEliminarInfoAcademica"))
            .show();
    }

    // ==========================================
    // CONFIRMAR ELIMINACIÓN
    // ==========================================

    async function eliminarInformacion() {
        if (ocupado || !registroSeleccionado) return;

        ocupado = true;

        const botonEliminar = $("ConfirmarEliminarInfoAcademica");
        const textoOriginal = botonEliminar.textContent;

        botonEliminar.disabled = true;
        botonEliminar.textContent = "Eliminando…";

        mostrarAvisoEliminar();

        try {

            const datos = new FormData();

            datos.set("__RequestVerificationToken", token());
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set("idInfo", registroSeleccionado.idInfoAcademica);

            const respuesta = await fetch(panel.dataset.urlQuitar, {
                method: "POST",
                body: datos,
                headers: {
                    Accept: "application/json"
                }
            });

            if (!respuesta.ok) {
                throw new Error(
                    await mensajeError(
                        respuesta,
                        "No se pudo eliminar la información académica."
                    )
                );
            }

            await cargar();

            registroSeleccionado = null;

            ocupado = false; // liberar antes de cerrar

            bootstrap.Modal
                .getOrCreateInstance($("ModalEliminarInfoAcademica"))
                .hide();

        } catch (error) {

            mostrarAvisoEliminar(
                error.message || "No se pudo conectar con el servidor.",
                "danger"
            );

        } finally {

            ocupado = false;
            botonEliminar.disabled = false;
            botonEliminar.textContent = textoOriginal;
        }
    }

    // ==========================================
    // EVENTOS DE LOS BOTONES
    // ==========================================

    lista.addEventListener("click", function (evento) {

        const editar = evento.target.closest(".BotonEditarInfo");
        const quitar = evento.target.closest(".BotonQuitarInfo");

        if (editar) {

            if (ocupado) return;

            const registro = registros.find(
                r => Number(r.idInfoAcademica) === Number(editar.dataset.id)
            );

            if (registro) abrirModal(registro);

            return;
        }

        if (!quitar || ocupado) return;

        abrirConfirmacionEliminar(quitar.dataset.id);
    });

    $("ConfirmarEliminarInfoAcademica").addEventListener(
        "click",
        eliminarInformacion
    );

    // ==========================================
    // GUARDAR INFORMACIÓN ACADÉMICA
    // ==========================================

    formulario.addEventListener("submit", async function (evento) {

        evento.preventDefault();

        if (ocupado) return;

        alerta.className = "alert d-none";
        alerta.textContent = "";

        const titulo = $("TituloInfoAcademica").value.trim();
        const detalle = $("DetalleInfoAcademica").value.trim();
        const id = $("IdInfoAcademica").value;

        if (!titulo) {
            alerta.className = "alert alert-warning";
            alerta.textContent = "Ingrese el título.";
            return;
        }

        const botonGuardar = formulario.querySelector(
            'button[type="submit"]'
        );

        const textoBoton = botonGuardar.textContent;

        ocupado = true;
        botonGuardar.disabled = true;
        botonGuardar.textContent = "Guardando…";

        try {

            const datos = new FormData(formulario);

            datos.set("__RequestVerificationToken", token());
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set("titulo", titulo);
            datos.set("detalle", detalle);

            let url = formulario.dataset.urlAgregar;

            if (id) {
                datos.set("idInfo", id);
                url = formulario.dataset.urlEditar;
            }

            const respuesta = await fetch(url, {
                method: "POST",
                body: datos,
                headers: {
                    Accept: "application/json"
                }
            });

            if (!respuesta.ok) {
                throw new Error(
                    await mensajeError(
                        respuesta,
                        "No se pudo guardar la información académica."
                    )
                );
            }

            ocupado = false; // liberar antes de cerrar

            bootstrap.Modal
                .getOrCreateInstance($("ModalInfoAcademica"))
                .hide();

            await cargar();

            mostrarAviso(
                id
                    ? "Información académica actualizada correctamente."
                    : "Información académica registrada correctamente."
            );

        } catch (error) {

            alerta.className = "alert alert-danger";

            alerta.textContent =
                error.message || "No se pudo conectar con el servidor.";

        } finally {

            ocupado = false;
            botonGuardar.disabled = false;
            botonGuardar.textContent = textoBoton;
        }
    });

    // ==========================================
    // CONTROLAR CIERRE DE MODALES
    // ==========================================

    $("ModalInfoAcademica").addEventListener(
        "hide.bs.modal",
        function (evento) {
            if (ocupado) evento.preventDefault();
        }
    );

    $("ModalEliminarInfoAcademica").addEventListener(
        "hide.bs.modal",
        function (evento) {
            if (ocupado) evento.preventDefault();
        }
    );

    $("ModalEliminarInfoAcademica").addEventListener(
        "hidden.bs.modal",
        function () {
            registroSeleccionado = null;
            mostrarAvisoEliminar();
        }
    );

    // ==========================================
    // CARGA INICIAL
    // ==========================================

    cargar();

})();