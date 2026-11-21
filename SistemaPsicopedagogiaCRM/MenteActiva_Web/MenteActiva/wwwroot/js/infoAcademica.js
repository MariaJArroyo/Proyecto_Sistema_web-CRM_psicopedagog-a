// Información académica adicional del estudiante: listar, agregar, editar y quitar.
(function () {
    "use strict";

    const $ = id => document.getElementById(id);
    const panel = $("InfoAcademica");

    if (!panel) return;

    const lista = $("ListaInfoAcademica");
    const aviso = $("AvisoInfoAcademica");
    const formulario = $("FormularioInfoAcademica");
    const alerta = $("AlertaInfoAcademica");
    const token = () => panel.querySelector('[name="__RequestVerificationToken"]').value;

    let registros = [];
    let ocupado = false;

    function mostrarAviso(mensaje, tipo) {
        aviso.className = mensaje ? "alert alert-" + tipo : "alert d-none";
        aviso.textContent = mensaje || "";
    }

    async function mensajeError(respuesta, general) {
        try {
            const error = await respuesta.json();
            return error.mensaje || general;
        } catch {
            return general;
        }
    }

    function boton(icono, nombre, clase) {
        const b = document.createElement("button");
        b.type = "button";
        b.className = "btn btn-sm btn-outline-secondary " + clase;
        b.setAttribute("aria-label", nombre);
        const i = document.createElement("i");
        i.className = "bi " + icono;
        i.setAttribute("aria-hidden", "true");
        b.appendChild(i);
        return b;
    }

    function pintar() {
        lista.replaceChildren();

        if (!registros.length) {
            const p = document.createElement("p");
            p.className = "TextoSuave mb-0";
            p.textContent = "No hay información académica adicional registrada.";
            lista.appendChild(p);
            return;
        }

        registros.forEach(function (r) {
            const fila = document.createElement("div");
            fila.className = "list-group-item px-0 d-flex justify-content-between align-items-start gap-3";

            const datos = document.createElement("div");
            datos.className = "text-break";

            const titulo = document.createElement("div");
            titulo.className = "fw-semibold";
            titulo.textContent = r.titulo;

            const detalle = document.createElement("p");
            detalle.className = "TextoSuave small mb-0";
            detalle.style.whiteSpace = "pre-line";
            detalle.textContent = r.detalle;

            datos.append(titulo, detalle);

            const acciones = document.createElement("div");
            acciones.className = "d-flex gap-1";

            const editar = boton("bi-pencil", "Editar información", "BotonEditarInfo");
            editar.dataset.id = r.idInfoAcademica;

            const quitar = boton("bi-trash", "Quitar información", "BotonQuitarInfo");
            quitar.dataset.id = r.idInfoAcademica;

            acciones.append(editar, quitar);
            fila.append(datos, acciones);
            lista.appendChild(fila);
        });
    }

    async function cargar() {
        try {
            const respuesta = await fetch(
                panel.dataset.urlLista + "?idEstudiante=" + encodeURIComponent(panel.dataset.idEstudiante),
                { headers: { Accept: "application/json" } });

            if (!respuesta.ok) throw new Error();

            registros = await respuesta.json();
            pintar();
        } catch {
            lista.textContent = "No se pudo cargar la información académica.";
        }
    }

    function abrirModal(registro) {
        alerta.className = "alert d-none";
        $("IdInfoAcademica").value = registro ? registro.idInfoAcademica : "";
        $("TituloInfoAcademica").value = registro ? registro.titulo : "";
        $("DetalleInfoAcademica").value = registro ? registro.detalle : "";
        $("TituloModalInfoAcademica").textContent =
            registro ? "Editar información académica" : "Agregar información académica";
        formulario.querySelector('button[type="submit"]').textContent =
            registro ? "Guardar cambios" : "Agregar";

        bootstrap.Modal.getOrCreateInstance($("ModalInfoAcademica")).show();
    }

    $("BotonAgregarInfoAcademica").addEventListener("click", () => abrirModal(null));

    lista.addEventListener("click", async function (evento) {
        const editar = evento.target.closest(".BotonEditarInfo");
        const quitar = evento.target.closest(".BotonQuitarInfo");

        if (editar) {
            const registro = registros.find(r => r.idInfoAcademica === Number(editar.dataset.id));
            if (registro) abrirModal(registro);
            return;
        }

        if (!quitar || ocupado) return;

        ocupado = true;
        quitar.disabled = true;
        mostrarAviso("");

        try {
            const datos = new FormData();
            datos.set("__RequestVerificationToken", token());
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set("idInfo", quitar.dataset.id);

            const respuesta = await fetch(panel.dataset.urlQuitar, {
                method: "POST",
                body: datos,
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok) {
                throw new Error(await mensajeError(respuesta, "No se pudo quitar la información académica."));
            }

            await cargar();
        } catch (error) {
            mostrarAviso(error.message || "No se pudo conectar con el servidor.", "danger");
            quitar.disabled = false;
        } finally {
            ocupado = false;
        }
    });

    formulario.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        if (ocupado) return;

        alerta.className = "alert d-none";

        const titulo = $("TituloInfoAcademica").value.trim();
        const detalle = $("DetalleInfoAcademica").value.trim();
        const id = $("IdInfoAcademica").value;

        if (!titulo || !detalle) {
            alerta.className = "alert alert-warning";
            alerta.textContent = "Ingrese el título y el detalle.";
            return;
        }

        const boton = formulario.querySelector('button[type="submit"]');
        const textoBoton = boton.textContent;

        ocupado = true;
        boton.disabled = true;
        boton.textContent = "Guardando…";

        try {
            const datos = new FormData(formulario);
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
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok) {
                throw new Error(await mensajeError(respuesta, "No se pudo guardar la información académica."));
            }

            bootstrap.Modal.getOrCreateInstance($("ModalInfoAcademica")).hide();
            mostrarAviso(id ? "Información actualizada correctamente." : "Información registrada correctamente.", "success");
            await cargar();
        } catch (error) {
            alerta.className = "alert alert-danger";
            alerta.textContent = error.message || "No se pudo conectar con el servidor.";
        } finally {
            ocupado = false;
            boton.disabled = false;
            boton.textContent = textoBoton;
        }
    });

    $("ModalInfoAcademica").addEventListener("hide.bs.modal", function (evento) {
        if (ocupado) evento.preventDefault();
    });

    cargar();
})();