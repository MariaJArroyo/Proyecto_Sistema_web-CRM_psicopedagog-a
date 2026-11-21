// Necesidades de apoyo del estudiante: lista, agregar (lookup) y quitar.
(function() {
    "use strict";

    const $ = id => document.getElementById(id);
    const panel = $("NecesidadesApoyo");

    if (!panel) return;

    const lista = $("ListaNecesidadesApoyo");
    const aviso = $("AvisoNecesidades");
    const formulario = $("FormularioAgregarNecesidad");
    const token = () => panel.querySelector('[name="__RequestVerificationToken"]').value;

    let ocupado = false;

    function mostrarAviso(mensaje, tipo)
    {
        aviso.className = mensaje ? "alert alert-" + tipo : "alert d-none";
        aviso.textContent = mensaje || "";
    }

    async function mensajeError(respuesta, general)
    {
        try
        {
            const error = await respuesta.json();
            return error.mensaje || general;
        }
        catch
        {
            return general;
        }
    }

    function pintar(necesidades)
    {
        lista.replaceChildren();

        if (!necesidades.length)
        {
            const p = document.createElement("p");
            p.className = "TextoSuave mb-0";
            p.textContent = "No hay necesidades de apoyo registradas.";
            lista.appendChild(p);
            return;
        }

        necesidades.forEach(function(n) {
            const fila = document.createElement("div");
            fila.className = "d-flex justify-content-between align-items-start gap-2 mb-2";

            const datos = document.createElement("div");
            datos.className = "text-break";

            const etiqueta = document.createElement("span");
            etiqueta.className = "badge badge-etiqueta-morado";
            etiqueta.textContent = n.nombre;
            datos.appendChild(etiqueta);

            if (n.observacion)
            {
                const obs = document.createElement("p");
                obs.className = "TextoSuave small mb-0";
                obs.textContent = n.observacion;
                datos.appendChild(obs);
            }

            const quitar = document.createElement("button");
            quitar.type = "button";
            quitar.className = "btn btn-sm btn-outline-secondary";
            quitar.setAttribute("aria-label", "Quitar " + n.nombre);
            quitar.dataset.idNecesidad = n.idNecesidadApoyo;
            quitar.innerHTML = '<i class="bi bi-x-lg" aria-hidden="true"></i>';

            fila.append(datos, quitar);
            lista.appendChild(fila);
        });
    }

    async function cargar()
    {
        try
        {
            const respuesta = await fetch(
                panel.dataset.urlLista + "?idEstudiante=" + encodeURIComponent(panel.dataset.idEstudiante),
                { headers: { Accept: "application/json" } });

            if (!respuesta.ok) throw new Error();

            pintar(await respuesta.json());
        }
        catch
        {
            lista.textContent = "No se pudieron cargar las necesidades de apoyo.";
        }
    }

    lista.addEventListener("click", async function(evento) {
        const boton = evento.target.closest("button[data-id-necesidad]");

        if (!boton || ocupado) return;

        ocupado = true;
        boton.disabled = true;
        mostrarAviso("");

        try
        {
            const datos = new FormData();
            datos.set("__RequestVerificationToken", token());
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set("idNecesidad", boton.dataset.idNecesidad);

            const respuesta = await fetch(panel.dataset.urlQuitar, {
            method: "POST",
                body: datos,
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok)
            {
                throw new Error(await mensajeError(respuesta, "No se pudo quitar la necesidad de apoyo."));
            }

            await cargar();
        }
        catch (error)
        {
            mostrarAviso(error.message || "No se pudo conectar con el servidor.", "danger");
            boton.disabled = false;
        }
        finally
        {
            ocupado = false;
        }
    });

    // ---------- Modal agregar ----------
    const lookup = LookupCatalogo.iniciar({
    campo: "NombreNecesidad",
        lista: "ResultadosNecesidad",
        aviso: "AvisoLookupNecesidad",
        url: formulario.dataset.urlBuscar,
        textoExiste: "Necesidad existente en el catálogo.",
        textoNuevo: "No existe en el catálogo: se creará como nueva."
    });

    const alerta = $("AlertaAgregarNecesidad");

    formulario.addEventListener("submit", async function(evento) {
        evento.preventDefault();

        if (ocupado) return;

        alerta.className = "alert d-none";

        const nombre = $("NombreNecesidad").value.trim();

        if (!nombre)
        {
            alerta.className = "alert alert-warning";
            alerta.textContent = "Ingrese o seleccione la necesidad de apoyo.";
            $("NombreNecesidad").focus();
            return;
        }

        const boton = formulario.querySelector('button[type="submit"]');
        const textoBoton = boton.textContent;

        ocupado = true;
        boton.disabled = true;
        boton.textContent = "Guardando…";

        try
        {
            const datos = new FormData(formulario);
            datos.set("idEstudiante", panel.dataset.idEstudiante);
            datos.set("nombreNecesidad", nombre);
            datos.set("observacion", $("ObservacionNecesidad").value.trim());

            const respuesta = await fetch(formulario.dataset.url, {
            method: "POST",
                body: datos,
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok)
            {
                throw new Error(await mensajeError(respuesta, "No se pudo registrar la necesidad de apoyo."));
            }

            bootstrap.Modal.getOrCreateInstance($("ModalAgregarNecesidad")).hide();
            formulario.reset();
            lookup?.reiniciar();
            mostrarAviso("Necesidad de apoyo registrada correctamente.", "success");
            await cargar();
        }
        catch (error)
        {
            alerta.className = "alert alert-danger";
            alerta.textContent = error.message || "No se pudo conectar con el servidor.";
        }
        finally
        {
            ocupado = false;
            boton.disabled = false;
            boton.textContent = textoBoton;
        }
    });

    $("ModalAgregarNecesidad").addEventListener("hidden.bs.modal", function() {
        formulario.reset();
        lookup?.reiniciar();
        alerta.className = "alert d-none";
    });

    cargar();
})();