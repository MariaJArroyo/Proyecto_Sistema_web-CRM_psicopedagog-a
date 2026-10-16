(function () {
    "use strict";
    const $ = id => document.getElementById(id);
    const panel = $("AreasEstado");
    if (!panel) return;
    let areas = [], cargando = false, guardando = false, seleccionada = null;
    const fecha = valor => {
        const m = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(valor || ""));
        return m ? `${m[3]}-${m[2]}-${m[1]}` : "No registrada";
    };
    function aviso(id, mensaje = "", tipo = "danger") {
        $(id).className = mensaje ? `alert alert-${tipo}` : "alert d-none";
        $(id).textContent = mensaje;
    }
    function nodo(tag, clase, texto) {
        const e = document.createElement(tag);
        e.className = clase;
        if (texto != null) e.textContent = texto;
        return e;
    }
    function boton(icono, nombre) {
        const b = nodo("button", "btn btn-sm btn-outline-secondary border-0");
        b.type = "button";
        b.title = nombre;
        b.setAttribute("aria-label", nombre);
        const i = nodo("i", `bi ${icono}`);
        i.setAttribute("aria-hidden", "true");
        b.append(i);
        return b;
    }
    function pintarLista(id, lista, activa) {
        const contenedor = $(id);
        contenedor.replaceChildren();
        if (!lista.length) {
            contenedor.append(nodo("p", "TextoSuave mb-0", activa ? "No hay áreas de dificultad actuales." : "No hay áreas inactivas."));
            return;
        }
        for (const a of lista) {
            const fila = nodo("div", "list-group-item px-0 d-flex justify-content-between align-items-start gap-3");
            const datos = nodo("div", "text-break");
            datos.append(nodo("span", "badge badge-etiqueta-morado mb-1", a.nombre));
            if (a.observacion) datos.append(nodo("p", "TextoSuave small mb-1", a.observacion));
            if (!activa) datos.append(nodo("p", "TextoSuave small mb-0", "Trabajada · " + fecha(a.fechaDesactivacion)));
            const acciones = nodo("div", "d-flex gap-1");
            if (activa) {
                const editar = boton("bi-pencil", "Editar área");
                editar.classList.add("BotonEditarArea");
                editar.dataset.idArea = a.idAreaDificultad;
                editar.dataset.nombre = a.nombre;
                editar.dataset.observacion = a.observacion || "";
                acciones.append(editar);
            }
            const estado = boton(activa ? "bi-toggle-on" : "bi-toggle-off", activa ? "Desactivar área" : "Reactivar área");
            estado.setAttribute("role", "switch");
            estado.setAttribute("aria-checked", String(activa));
            estado.dataset.estadoArea = a.idAreaDificultad;
            acciones.append(estado);
            fila.append(datos, acciones);
            contenedor.append(fila);
        }
    }
    function pintar() {
        const activas = areas.filter(a => a.activo === true);
        const inactivas = areas.filter(a => a.activo === false);
        pintarLista("ListaAreasDificultad", activas, true);
        pintarLista("ListaAreasInactivas", inactivas, false);
        $("CantidadAreasInactivas").textContent = inactivas.length;
        if ($("FichaDatoMateria")) $("FichaDatoMateria").textContent = activas.map(a => a.nombre).join(", ") || "No registradas";
    }
    async function mensajeError(respuesta) {
        try { return (await respuesta.json()).mensaje || "No se pudo completar la operación."; }
        catch { return "No se pudo completar la operación. Revise la sesión e intente nuevamente."; }
    }
    async function cargar() {
        if (cargando) return;
        cargando = true;
        $("ListaAreasDificultad").setAttribute("aria-busy", "true");
        $("RecargarAreasEstado").disabled = true;
        try {
            const respuesta = await fetch(`${panel.dataset.urlLista}?idEstudiante=${encodeURIComponent(panel.dataset.idEstudiante)}`, { headers: { Accept: "application/json" } });
            if (!respuesta.ok) throw new Error(await mensajeError(respuesta));
            const datos = await respuesta.json();
            if (!Array.isArray(datos)) throw new Error("El servidor no devolvió una lista de áreas válida.");
            areas = datos;
            pintar();
            return true;
        } catch (error) {
            aviso("AvisoAreasEstado", error.message);
            aviso("AvisoAreasInactivas", error.message);
            return false;
        } finally {
            cargando = false;
            $("ListaAreasDificultad").setAttribute("aria-busy", "false");
            $("RecargarAreasEstado").disabled = false;
        }
    }
    async function cambiar(area, activo) {
        if (guardando || cargando) return;
        guardando = true;
        const alerta = activo ? "AvisoAreasInactivas" : "AvisoDesactivarArea";
        aviso(alerta);
        const botones = document.querySelectorAll("[data-estado-area], #ConfirmarDesactivarArea, .BotonEditarArea");
        botones.forEach(b => b.disabled = true);
        let actualizado = false;
        try {
            const datos = new FormData();
            datos.set("__RequestVerificationToken", panel.querySelector('[name="__RequestVerificationToken"]').value);
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set("idArea", String(area.idAreaDificultad));
            datos.set("activo", String(activo));
            const respuesta = await fetch(panel.dataset.urlEstado, { method: "POST", body: datos, headers: { Accept: "application/json" } });
            if (!respuesta.ok) throw new Error(await mensajeError(respuesta));
            actualizado = true;
            const refrescado = await cargar();
            const mensaje = refrescado
                ? (activo ? "Área reactivada correctamente." : "Área marcada como trabajada.")
                : "El estado se guardó, pero no se pudo actualizar la lista. Recargue la página.";
            aviso("AvisoAreasEstado", mensaje, refrescado ? "success" : "warning");
            if (activo) aviso("AvisoAreasInactivas", mensaje, refrescado ? "success" : "warning");
        } catch (error) {
            aviso(alerta, error.message || "No se pudo confirmar el cambio. Actualice la lista antes de reintentar.");
        } finally {
            guardando = false;
            botones.forEach(b => b.disabled = false);
            if (actualizado && !activo) bootstrap.Modal.getOrCreateInstance($("ModalDesactivarArea")).hide();
        }
    }
    document.addEventListener("click", evento => {
        const b = evento.target.closest("button[data-estado-area]");
        if (!b || b.disabled || guardando || cargando) return;
        const area = areas.find(a => a.idAreaDificultad === Number(b.dataset.estadoArea));
        if (!area) return;
        if (!area.activo) { cambiar(area, true); return; }
        seleccionada = area;
        $("NombreDesactivarArea").textContent = area.nombre;
        aviso("AvisoDesactivarArea");
        bootstrap.Modal.getOrCreateInstance($("ModalDesactivarArea")).show();
    });
    $("ConfirmarDesactivarArea").addEventListener("click", () => { if (seleccionada) cambiar(seleccionada, false); });
    for (const id of ["ModalAreasInactivas", "ModalDesactivarArea"])
        $(id).addEventListener("hide.bs.modal", evento => { if (guardando) evento.preventDefault(); });
    $("RecargarAreasEstado").addEventListener("click", () => { if (!guardando) { aviso("AvisoAreasEstado"); aviso("AvisoAreasInactivas"); cargar(); } });
    cargar();
})();
