// Pestana Mensajeria de Configuracion (HU-M10-4b): que roles del personal ven la
// bandeja de mensajes del consultorio. Va aparte de configuracion.js porque la
// Psicopedagoga solo ve esta pestana y aquel archivo carga las otras cuatro, que
// son del Administrador.
//
// El aviso de exito sale solo si el API respondio bien, y despues se vuelve a
// leer la lista para mostrar lo que de verdad quedo guardado.

document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    const lista = document.getElementById("ListaRolesMensajeria");
    const formulario = document.getElementById("FormularioAccesoMensajeria");
    const boton = document.getElementById("BotonGuardarAccesoMensajeria");
    const alerta = document.getElementById("AlertaMensajeria");

    if (!lista || !formulario || !boton) return;

    function escaparHtml(texto) {
        const div = document.createElement("div");
        div.textContent = texto ?? "";
        return div.innerHTML.replaceAll('"', "&quot;");
    }

    function mostrarAlerta(mensaje, tipo = "danger") {
        alerta.className = "alert alert-" + tipo;
        alerta.textContent = mensaje;
    }

    function ocultarAlerta() {
        alerta.className = "alert d-none";
        alerta.textContent = "";
    }

    function ocupado(activo) {
        boton.disabled = activo;
        boton.querySelector(".spinner-border")?.classList.toggle("d-none", !activo);
    }

    async function leerMensaje(respuesta, porDefecto) {
        try {
            const json = await respuesta.json();
            return json?.mensaje || porDefecto;
        } catch {
            return porDefecto;
        }
    }

    async function cargar() {
        ocultarAlerta();
        boton.disabled = true;

        try {
            const respuesta = await fetch("/Admin/ConfiguracionMensajeria");

            if (!respuesta.ok) {
                lista.innerHTML = "";
                mostrarAlerta(await leerMensaje(respuesta, "No se pudo cargar el acceso a los mensajes."));
                return;
            }

            const roles = await respuesta.json();

            lista.innerHTML = roles.map(r => `
                <div class="form-check mb-2">
                    <input class="form-check-input" type="checkbox" id="RolMensajeria${r.idRol}"
                           value="${r.idRol}" ${r.tieneAcceso ? "checked" : ""}>
                    <label class="form-check-label" for="RolMensajeria${r.idRol}">${escaparHtml(r.nombre)}</label>
                </div>`).join("");

            boton.disabled = false;
        } catch {
            lista.innerHTML = "";
            mostrarAlerta("No se pudo conectar con el servidor.");
        }
    }

    formulario.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        ocultarAlerta();

        const marcados = [...lista.querySelectorAll("input[type=checkbox]:checked")].map(c => c.value);

        if (marcados.length === 0) {
            mostrarAlerta("Marque al menos un rol: si nadie tiene acceso, los mensajes de las familias no le llegan a nadie.", "warning");
            return;
        }

        const datos = new FormData();
        datos.append("__RequestVerificationToken",
            document.querySelector('input[name="__RequestVerificationToken"]')?.value || "");
        marcados.forEach(id => datos.append("Roles", id));

        ocupado(true);

        try {
            const respuesta = await fetch("/Admin/GuardarAccesoMensajeria", { method: "POST", body: datos });

            if (!respuesta.ok) {
                mostrarAlerta(await leerMensaje(respuesta, "No se pudo guardar el acceso a los mensajes."));
                return;
            }

            const json = await respuesta.json().catch(() => ({}));
            if (typeof MostrarToast === "function") {
                MostrarToast(json?.mensaje || "Acceso a los mensajes guardado correctamente.", "exito");
            }
        } catch {
            mostrarAlerta("No se pudo conectar con el servidor.");
            return;
        } finally {
            ocupado(false);
        }

        await cargar();
    });

    cargar();
});
