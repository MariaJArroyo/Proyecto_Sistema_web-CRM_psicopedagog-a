
(function () {
    "use strict";

    const $ = id => document.getElementById(id);
    const panel = $("NecesidadesApoyo");

    if (!panel) return;

    const lista = $("ListaNecesidadesApoyo");
    const aviso = $("AvisoNecesidades");

    const formularioAgregar = $("FormularioAgregarNecesidad");
    const formularioEditar = $("FormularioEditarNecesidad");

    let necesidades = [];
    let ocupado = false;
    let necesidadSeleccionada = null;

    function token() {
        return panel.querySelector(
            '[name="__RequestVerificationToken"]'
        )?.value || "";
    }

    function mostrarAviso(mensaje, tipo = "success") {
        aviso.className = mensaje
            ? `alert alert-${tipo}`
            : "alert d-none";

        aviso.textContent = mensaje || "";
    }

    function mostrarAvisoEliminar(mensaje = "", tipo = "danger") {
        const elemento = $("AvisoEliminarNecesidad");

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

    function crearBoton(icono, titulo, clase) {
        const boton = document.createElement("button");

        boton.type = "button";
        boton.className = "btn btn-sm btn-outline-secondary " + clase;
        boton.title = titulo;
        boton.setAttribute("aria-label", titulo);

        const i = document.createElement("i");
        i.className = "bi " + icono;
        i.setAttribute("aria-hidden", "true");

        boton.appendChild(i);

        return boton;
    }

    // ==========================================
    // MOSTRAR NECESIDADES
    // ==========================================

    function pintar() {
        lista.replaceChildren();

        if (!necesidades.length) {
            const p = document.createElement("p");

            p.className = "TextoSuave mb-0";
            p.textContent =
                "No hay necesidades de apoyo registradas.";

            lista.appendChild(p);
            return;
        }

        necesidades.forEach(function (n, indice) {

            const fila = document.createElement("div");

            fila.className =
                "d-flex justify-content-between align-items-start gap-2 py-2";

            if (indice !== necesidades.length - 1) {
                fila.classList.add("border-bottom");
            }

            const datos = document.createElement("div");
            datos.className = "text-break flex-grow-1";

            const etiqueta = document.createElement("span");

            etiqueta.className = "badge badge-etiqueta-morado";
            etiqueta.textContent = n.nombre;

            datos.appendChild(etiqueta);

            if (n.observacion) {
                const observacion = document.createElement("p");

                observacion.className = "TextoSuave small mb-0 mt-1";
                observacion.style.whiteSpace = "pre-line";
                observacion.textContent = n.observacion;

                datos.appendChild(observacion);
            }

            const acciones = document.createElement("div");

            acciones.className =
                "d-flex gap-1 flex-shrink-0";

            const editar = crearBoton(
                "bi-pencil",
                "Editar necesidad",
                "BotonEditarNecesidad"
            );

            editar.dataset.idNecesidad = n.idNecesidadApoyo;

            const eliminar = crearBoton(
                "bi-slash-circle",
                "Eliminar necesidad",
                "BotonEliminarNecesidad"
            );

            eliminar.dataset.idNecesidad = n.idNecesidadApoyo;

            acciones.append(editar, eliminar);
            fila.append(datos, acciones);

            lista.appendChild(fila);
        });
    }

    // ==========================================
    // CARGAR NECESIDADES
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
                    await mensajeError(
                        respuesta,
                        "No se pudieron cargar las necesidades de apoyo."
                    )
                );
            }

            necesidades = await respuesta.json();

            pintar();

        } catch (error) {
            lista.textContent =
                error.message ||
                "No se pudieron cargar las necesidades de apoyo.";
        }
    }

    // ==========================================
    // ABRIR EDICIÓN
    // ==========================================

    const lookupEditar = LookupCatalogo.iniciar({
        campo: "EditarNombreNecesidad",
        lista: "ResultadosEditarNecesidad",
        aviso: "AvisoLookupEditarNecesidad",
        url: formularioEditar.dataset.urlBuscar,
        textoExiste: "Necesidad existente en el catálogo.",
        textoNuevo: "No existe en el catálogo: se creará como nueva."
    });

    function abrirEdicion(id) {
        const necesidad = necesidades.find(
            n => Number(n.idNecesidadApoyo) === Number(id)
        );

        if (!necesidad) return;

        $("EditarIdNecesidad").value = necesidad.idNecesidadApoyo;
        $("EditarNombreNecesidad").value = necesidad.nombre;
        $("EditarObservacionNecesidad").value =
            necesidad.observacion || "";

        $("AlertaEditarNecesidad").className = "alert d-none";
        $("AlertaEditarNecesidad").textContent = "";

        $("AvisoLookupEditarNecesidad").textContent =
            "Puede cambiar el nombre o la observación.";

        bootstrap.Modal
            .getOrCreateInstance($("ModalEditarNecesidad"))
            .show();
    }

    // ==========================================
    // ABRIR CONFIRMACIÓN DE ELIMINACIÓN
    // ==========================================

    function abrirConfirmacionEliminar(id) {
        necesidadSeleccionada = necesidades.find(
            n => Number(n.idNecesidadApoyo) === Number(id)
        );

        if (!necesidadSeleccionada) return;

        $("NombreEliminarNecesidad").textContent =
            necesidadSeleccionada.nombre;

        mostrarAvisoEliminar();

        bootstrap.Modal
            .getOrCreateInstance($("ModalEliminarNecesidad"))
            .show();
    }

    // ==========================================
    // CONFIRMAR ELIMINACIÓN
    // ==========================================

    async function eliminarNecesidad() {
        if (ocupado || !necesidadSeleccionada) return;

        ocupado = true;

        const boton = $("ConfirmarEliminarNecesidad");
        const textoOriginal = boton.textContent;

        boton.disabled = true;
        boton.textContent = "Eliminando…";

        mostrarAvisoEliminar();

        try {
            const datos = new FormData();

            datos.set("__RequestVerificationToken", token());
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set(
                "idNecesidad",
                necesidadSeleccionada.idNecesidadApoyo
            );

            const respuesta = await fetch(
                panel.dataset.urlQuitar,
                {
                    method: "POST",
                    body: datos,
                    headers: {
                        Accept: "application/json"
                    }
                }
            );

            if (!respuesta.ok) {
                throw new Error(
                    await mensajeError(
                        respuesta,
                        "No se pudo quitar la necesidad de apoyo."
                    )
                );
            }

            await cargar();

            necesidadSeleccionada = null;

            ocupado = false; // liberar antes de cerrar

            bootstrap.Modal
                .getOrCreateInstance($("ModalEliminarNecesidad"))
                .hide();

            mostrarAviso(
                "Necesidad de apoyo eliminada correctamente."
            );

        } catch (error) {
            mostrarAvisoEliminar(
                error.message || "No se pudo conectar con el servidor.",
                "danger"
            );

        } finally {
            ocupado = false;
            boton.disabled = false;
            boton.textContent = textoOriginal;
        }
    }

    // ==========================================
    // EVENTOS DE LOS BOTONES
    // ==========================================

    lista.addEventListener("click", function (evento) {

        const editar = evento.target.closest(
            ".BotonEditarNecesidad"
        );

        const eliminar = evento.target.closest(
            ".BotonEliminarNecesidad"
        );

        if (editar) {
            if (ocupado) return;

            abrirEdicion(editar.dataset.idNecesidad);
            return;
        }

        if (!eliminar || ocupado) return;

        abrirConfirmacionEliminar(eliminar.dataset.idNecesidad);
    });

    $("ConfirmarEliminarNecesidad").addEventListener(
        "click",
        eliminarNecesidad
    );

    // ==========================================
    // AGREGAR NECESIDAD
    // ==========================================

    const lookupAgregar = LookupCatalogo.iniciar({
        campo: "NombreNecesidad",
        lista: "ResultadosNecesidad",
        aviso: "AvisoLookupNecesidad",
        url: formularioAgregar.dataset.urlBuscar,
        textoExiste: "Necesidad existente en el catálogo.",
        textoNuevo: "No existe en el catálogo: se creará como nueva."
    });

    const alertaAgregar = $("AlertaAgregarNecesidad");

    formularioAgregar.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        if (ocupado) return;

        alertaAgregar.className = "alert d-none";

        const nombre = $("NombreNecesidad").value.trim();

        if (!nombre) {
            alertaAgregar.className = "alert alert-warning";
            alertaAgregar.textContent =
                "Ingrese o seleccione una necesidad de apoyo.";
            return;
        }

        const boton = formularioAgregar.querySelector(
            'button[type="submit"]'
        );

        const textoOriginal = boton.textContent;

        ocupado = true;
        boton.disabled = true;
        boton.textContent = "Guardando…";

        try {
            const datos = new FormData();

            datos.set("__RequestVerificationToken", token());
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set("nombreNecesidad", nombre);
            datos.set(
                "observacion",
                $("ObservacionNecesidad").value.trim()
            );

            const respuesta = await fetch(
                formularioAgregar.dataset.url,
                {
                    method: "POST",
                    body: datos,
                    headers: {
                        Accept: "application/json"
                    }
                }
            );

            if (!respuesta.ok) {
                throw new Error(
                    await mensajeError(
                        respuesta,
                        "No se pudo registrar la necesidad de apoyo."
                    )
                );
            }

            bootstrap.Modal
                .getOrCreateInstance($("ModalAgregarNecesidad"))
                .hide();

            formularioAgregar.reset();
            lookupAgregar?.reiniciar();

            await cargar();

            mostrarAviso(
                "Necesidad de apoyo registrada correctamente."
            );

        } catch (error) {
            alertaAgregar.className = "alert alert-danger";
            alertaAgregar.textContent =
                error.message || "No se pudo conectar con el servidor.";

        } finally {
            ocupado = false;
            boton.disabled = false;
            boton.textContent = textoOriginal;
        }
    });

    // ==========================================
    // GUARDAR EDICIÓN
    // ==========================================

    const alertaEditar = $("AlertaEditarNecesidad");

    formularioEditar.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        if (ocupado) return;

        alertaEditar.className = "alert d-none";

        const id = $("EditarIdNecesidad").value;
        const nombre = $("EditarNombreNecesidad").value.trim();
        const observacion =
            $("EditarObservacionNecesidad").value.trim();

        if (!id || !nombre) {
            alertaEditar.className = "alert alert-warning";
            alertaEditar.textContent =
                "Ingrese una necesidad de apoyo válida.";
            return;
        }

        const boton = formularioEditar.querySelector(
            'button[type="submit"]'
        );

        const textoOriginal = boton.textContent;

        ocupado = true;
        boton.disabled = true;
        boton.textContent = "Guardando…";

        try {
            const datos = new FormData();

            datos.set("__RequestVerificationToken", token());
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set("idNecesidad", id);
            datos.set("nombreNecesidad", nombre);
            datos.set("observacion", observacion);

            const respuesta = await fetch(
                formularioEditar.dataset.url,
                {
                    method: "POST",
                    body: datos,
                    headers: {
                        Accept: "application/json"
                    }
                }
            );

            if (!respuesta.ok) {
                throw new Error(
                    await mensajeError(
                        respuesta,
                        "No se pudo actualizar la necesidad de apoyo."
                    )
                );
            }

            bootstrap.Modal
                .getOrCreateInstance($("ModalEditarNecesidad"))
                .hide();

            await cargar();

            mostrarAviso(
                "Necesidad de apoyo actualizada correctamente."
            );

        } catch (error) {
            alertaEditar.className = "alert alert-danger";
            alertaEditar.textContent =
                error.message || "No se pudo conectar con el servidor.";

        } finally {
            ocupado = false;
            boton.disabled = false;
            boton.textContent = textoOriginal;
        }
    });

    // ==========================================
    // LIMPIAR MODALES Y CONTROLAR CIERRE
    // ==========================================

    $("ModalAgregarNecesidad").addEventListener(
        "hidden.bs.modal",
        function () {
            formularioAgregar.reset();
            lookupAgregar?.reiniciar();

            alertaAgregar.className = "alert d-none";
            alertaAgregar.textContent = "";
        }
    );

    $("ModalEditarNecesidad").addEventListener(
        "hidden.bs.modal",
        function () {
            formularioEditar.reset();
            lookupEditar?.reiniciar();

            alertaEditar.className = "alert d-none";
            alertaEditar.textContent = "";
        }
    );

    $("ModalEliminarNecesidad").addEventListener(
        "hidden.bs.modal",
        function () {
            necesidadSeleccionada = null;
            mostrarAvisoEliminar();
        }
    );

    $("ModalEliminarNecesidad").addEventListener(
        "hide.bs.modal",
        function (evento) {
            if (ocupado) evento.preventDefault();
        }
    );

    cargar();

})();
