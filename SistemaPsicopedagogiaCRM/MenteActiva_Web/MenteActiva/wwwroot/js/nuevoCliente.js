// Modal "Nuevo cliente" compartido por Clientes y Solicitudes.
//   - Boton "Nuevo cliente" (data-bs-toggle)  -> abre vacio
//   - window.abrirNuevoCliente(datos)         -> abre precargado (convertir solicitud)
document.addEventListener("DOMContentLoaded", function () {

    const modal = document.getElementById("ModalNuevoCliente");
    if (!modal) return;

    const form = document.getElementById("FormularioNuevoCliente");
    const lista = document.getElementById("ListaEstudiantesNuevo");
    const plantilla = document.getElementById("PlantillaEstudianteNuevo");
    const alertaError = document.getElementById("NuevoClienteError");
    const alertaOrigen = document.getElementById("NuevoClienteOrigen");
    const campoIdSolicitud = document.getElementById("NuevoClienteIdSolicitud");
    const titulo = document.getElementById("TituloNuevoCliente");

    const PatronCorreo = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;


    // ---------- utilidades ----------

    function hoyIso() {
        const hoy = new Date();
        hoy.setMinutes(hoy.getMinutes() - hoy.getTimezoneOffset());
        return hoy.toISOString().slice(0, 10);
    }

    function marcar(campo, valido) {
        campo.classList.toggle("is-invalid", !valido);
        return valido;
    }

    function mostrarError(mensaje) {
        alertaError.className = "alert alert-danger";
        alertaError.textContent = mensaje;
    }

    function ocultarError() {
        alertaError.className = "alert d-none";
        alertaError.textContent = "";
    }

    form.addEventListener("input", e => e.target.classList.remove("is-invalid"));
    form.addEventListener("change", e => e.target.classList.remove("is-invalid"));


    // ---------- lista de estudiantes ----------

    // name="Estudiantes[i].Campo" consecutivos para que ASP.NET arme la lista
    function renumerar() {
        const tarjetas = lista.querySelectorAll("[data-estudiante-nuevo]");

        tarjetas.forEach(function (tarjeta, i) {
            tarjeta.querySelector("[data-titulo]").textContent = "Estudiante " + (i + 1);

            tarjeta.querySelectorAll("[data-campo]").forEach(function (campo) {
                const nombre = campo.getAttribute("data-campo");
                campo.name = `Estudiantes[${i}].${nombre}`;
                campo.id = `NuevoEstudiante${i}_${nombre}`;

                const etiqueta = tarjeta.querySelector(`[data-label-para="${nombre}"]`);
                if (etiqueta) etiqueta.htmlFor = campo.id;
            });

            tarjeta.querySelector("[data-quitar-estudiante]").disabled = tarjetas.length === 1;
        });
    }

    function agregarEstudiante(enfocar) {
        const fragmento = plantilla.content.cloneNode(true);
        fragmento.querySelector('[data-campo="FechaNacimiento"]').max = hoyIso();
        lista.appendChild(fragmento);
        renumerar();

        if (enfocar) {
            const tarjetas = lista.querySelectorAll("[data-estudiante-nuevo]");
            tarjetas[tarjetas.length - 1].querySelector('[data-campo="Nombre"]').focus();
        }
    }

    document.getElementById("BotonAgregarEstudianteNuevo")
        .addEventListener("click", () => agregarEstudiante(true));

    lista.addEventListener("click", function (evento) {
        const boton = evento.target.closest("[data-quitar-estudiante]");
        if (!boton) return;
        boton.closest("[data-estudiante-nuevo]").remove();
        renumerar();
    });


    // ---------- abrir ----------

    function preparar(datos) {

        form.reset();
        form.querySelectorAll(".is-invalid").forEach(c => c.classList.remove("is-invalid"));
        ocultarError();
        lista.innerHTML = "";
        agregarEstudiante(false);

        const esConversion = !!datos;

        campoIdSolicitud.value = esConversion ? datos.idSolicitud : "";
        titulo.textContent = esConversion ? "Convertir solicitud en cliente" : "Nuevo cliente";
        alertaOrigen.classList.toggle("d-none", !esConversion);

        if (esConversion) {
            alertaOrigen.innerHTML =
                '<i class="bi bi-inbox me-1"></i>Datos tomados de la solicitud web. ' +
                'Revíselos y agregue los <strong>estudiantes a cargo</strong> para crear el cliente.';

            document.getElementById("NombreEncargado").value = datos.nombre ?? "";
            document.getElementById("ApellidoEncargado").value = datos.apellido ?? "";
            document.getElementById("TelefonoNuevoCliente").value = datos.telefono ?? "";
            document.getElementById("CorreoNuevoCliente").value = datos.correo ?? "";
            document.getElementById("ServicioInteresNuevoCliente").value = datos.idServicioInteres ?? "";
            document.getElementById("Observaciones").value = (datos.observaciones ?? "").slice(0, 1000);
        }
    }

    // Abierto con el boton "Nuevo cliente": trae relatedTarget y se abre vacio
    modal.addEventListener("show.bs.modal", function (evento) {
        if (evento.relatedTarget) preparar(null);
    });

    window.abrirNuevoCliente = function (datos) {
        preparar(datos || null);
        bootstrap.Modal.getOrCreateInstance(modal).show();
    };


    // ---------- guardar ----------

    form.addEventListener("submit", async function (evento) {

        evento.preventDefault();
        ocultarError();

        let valido = true;

        form.querySelectorAll("[required]").forEach(function (campo) {
            if (!marcar(campo, campo.value.trim() !== "")) valido = false;
        });

        const telefono = document.getElementById("TelefonoNuevoCliente");
        telefono.value = telefono.value.replace(/\D/g, "");
        valido = marcar(telefono, /^\d{8}$/.test(telefono.value)) && valido;

        const correo = document.getElementById("CorreoNuevoCliente");
        const valorCorreo = correo.value.trim();
        valido = marcar(correo, valorCorreo === "" || PatronCorreo.test(valorCorreo)) && valido;

        lista.querySelectorAll('[data-campo="FechaNacimiento"]').forEach(function (campo) {
            valido = marcar(campo, campo.value === "" || campo.value <= hoyIso()) && valido;
        });

        if (!valido) {
            mostrarError("Revise los campos marcados en rojo.");
            return;
        }

        renumerar();

        const boton = form.querySelector('button[type="submit"]');
        boton.disabled = true;

        try {
            const respuesta = await fetch(form.action, { method: "POST", body: new FormData(form) });

            if (respuesta.ok) {
                location.reload();
                return;
            }

            let mensaje = "No se pudo guardar el cliente.";
            try {
                const json = await respuesta.json();
                if (json && json.mensaje) mensaje = json.mensaje;
            } catch { }

            mostrarError(mensaje);

        } catch {
            mostrarError("No se pudo conectar con el servidor.");
        }

        boton.disabled = false;
    });

});