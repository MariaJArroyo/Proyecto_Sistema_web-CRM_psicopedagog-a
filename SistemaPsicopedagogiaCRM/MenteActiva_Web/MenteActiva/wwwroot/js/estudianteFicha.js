// ---------- auxiliares ----------

// Muestra el mensaje de éxito que quedó pendiente antes de recargar la página
function mostrarMensajePendiente() {

    const mensaje = sessionStorage.getItem("FichaMensaje");

    if (!mensaje) return;

    sessionStorage.removeItem("FichaMensaje");

    const contenedor = document.getElementById("ContenedorMensaje");

    if (!contenedor) return;

    const alerta = document.createElement("div");
    alerta.className = "alert alert-success alert-dismissible fade show";
    alerta.setAttribute("role", "alert");
    alerta.textContent = mensaje;

    const cerrar = document.createElement("button");
    cerrar.type = "button";
    cerrar.className = "btn-close";
    cerrar.setAttribute("data-bs-dismiss", "alert");
    cerrar.setAttribute("aria-label", "Cerrar");

    alerta.appendChild(cerrar);
    contenedor.appendChild(alerta);

}

// FormData con el token antifalsificación del formulario
function crearDatos(formulario) {

    const datos = new FormData();

    const token = formulario.querySelector(
        'input[name="__RequestVerificationToken"]'
    );

    if (token) {
        datos.append("__RequestVerificationToken", token.value);
    }

    return datos;

}

// Envía el formulario por fetch; si sale bien guarda el mensaje y recarga
async function enviarFormulario(formulario, datos, mensajeError, mensajeOk) {

    const boton = formulario.querySelector('button[type="submit"]');

    boton.disabled = true;

    try {

        const respuesta = await fetch(formulario.dataset.url, {
            method: "POST",
            body: datos
        });

        if (!respuesta.ok) {

            let mensaje = mensajeError;

            try {
                const error = await respuesta.json();
                mensaje = error.mensaje || mensaje;
            } catch {
                // Mantener mensaje predeterminado
            }

            alert(mensaje);
            return;
        }

        sessionStorage.setItem("FichaMensaje", mensajeOk);
        window.location.reload();

    } catch (error) {

        console.error(error);

        alert("Ocurrió un error de conexión. Intente de nuevo.");

    } finally {

        boton.disabled = false;

    }

}


// Escribe la fecha como dd/mm/aaaa: agrega las diagonales mientras se digita
function aplicarMascaraFecha(campo) {

    campo.addEventListener("input", function () {

        const digitos = campo.value.replace(/\D/g, "").slice(0, 8);

        let texto = digitos;

        if (digitos.length > 4) {
            texto = digitos.slice(0, 2) + "/" + digitos.slice(2, 4) + "/" + digitos.slice(4);
        } else if (digitos.length > 2) {
            texto = digitos.slice(0, 2) + "/" + digitos.slice(2);
        }

        campo.value = texto;

    });

}

// Convierte dd/mm/aaaa a yyyy-MM-dd (el formato que recibe el servidor).
// Devuelve "" si está vacía y null si no es una fecha válida.
function fechaAIso(texto) {

    if (!texto) return "";

    const partes = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(texto);

    if (!partes) return null;

    const dia = Number(partes[1]);
    const mes = Number(partes[2]);
    const anio = Number(partes[3]);

    const fecha = new Date(anio, mes - 1, dia);

    if (fecha.getFullYear() !== anio
        || fecha.getMonth() !== mes - 1
        || fecha.getDate() !== dia) {
        return null;
    }

    return partes[3] + "-" + partes[2] + "-" + partes[1];

}


document.addEventListener("DOMContentLoaded", function () {

    mostrarMensajePendiente();


    // ---------- Agregar área de dificultad ----------
    const formularioAgregarArea =
        document.getElementById("FormularioAgregarArea");

    if (formularioAgregarArea) {

        formularioAgregarArea.addEventListener("submit", async function (evento) {

            evento.preventDefault();

            const campoArea = document.getElementById("NombreArea");
            const nombreArea = campoArea.value.trim();

            if (!nombreArea) {
                campoArea.focus();
                return;
            }

            const datos = crearDatos(formularioAgregarArea);

            datos.append("idEstudiante", formularioAgregarArea.dataset.idEstudiante);
            datos.append("nombreArea", nombreArea);
            datos.append(
                "observacion",
                document.getElementById("ObservacionArea").value.trim()
            );

            await enviarFormulario(
                formularioAgregarArea,
                datos,
                "No se pudo registrar el área de dificultad.",
                "Área de dificultad agregada correctamente."
            );

        });

    }


    // ---------- Editar área de dificultad (precarga los datos actuales) ----------
    const formularioEditarArea =
        document.getElementById("FormularioEditarArea");

    if (formularioEditarArea) {

        const modalEditarArea = bootstrap.Modal.getOrCreateInstance(
            document.getElementById("ModalEditarArea")
        );

        document.addEventListener("click", function (evento) {

            const boton = evento.target.closest(".BotonEditarArea");

            if (!boton) return;

            document.getElementById("EditarIdArea").value =
                boton.dataset.idArea;

            document.getElementById("EditarNombreArea").value =
                boton.dataset.nombre || "";

            document.getElementById("EditarObservacionArea").value =
                boton.dataset.observacion || "";

            modalEditarArea.show();

        });

        formularioEditarArea.addEventListener("submit", async function (evento) {

            evento.preventDefault();

            const campoNombre = document.getElementById("EditarNombreArea");
            const nombreArea = campoNombre.value.trim();

            if (!nombreArea) {
                campoNombre.focus();
                return;
            }

            const datos = crearDatos(formularioEditarArea);

            datos.append("idEstudiante", formularioEditarArea.dataset.idEstudiante);
            datos.append("idArea", document.getElementById("EditarIdArea").value);
            datos.append("nombreArea", nombreArea);
            datos.append(
                "observacion",
                document.getElementById("EditarObservacionArea").value.trim()
            );

            await enviarFormulario(
                formularioEditarArea,
                datos,
                "No se pudo actualizar el área de dificultad.",
                "Área de dificultad actualizada correctamente."
            );

        });

    }


    // ---------- Editar ficha del estudiante (guarda en la base de datos) ----------
    const formularioEditarFicha =
        document.getElementById("FormularioEditarFicha");

    if (formularioEditarFicha) {

        const campoFecha =
            document.getElementById("EditarFechaNacimientoFicha");

        aplicarMascaraFecha(campoFecha);

        formularioEditarFicha.addEventListener("submit", async function (evento) {

            evento.preventDefault();

            const campoNombre = document.getElementById("EditarNombreFicha");
            const campoApellido = document.getElementById("EditarPrimerApellidoFicha");

            if (!campoNombre.value.trim()) {
                campoNombre.focus();
                return;
            }

            if (!campoApellido.value.trim()) {
                campoApellido.focus();
                return;
            }

            const fechaIso = fechaAIso(campoFecha.value.trim());

            if (fechaIso === null) {
                alert("Ingrese la fecha de nacimiento con el formato dd/mm/aaaa.");
                campoFecha.focus();
                return;
            }

            if (fechaIso && new Date(fechaIso + "T00:00:00") > new Date()) {
                alert("La fecha de nacimiento no puede ser futura.");
                campoFecha.focus();
                return;
            }

            const datos = crearDatos(formularioEditarFicha);

            datos.append("idEstudiante", formularioEditarFicha.dataset.idEstudiante);
            datos.append("nombre", campoNombre.value.trim());
            datos.append("primerApellido", campoApellido.value.trim());
            datos.append(
                "segundoApellido",
                document.getElementById("EditarSegundoApellidoFicha").value.trim()
            );
            datos.append("fechaNacimiento", fechaIso);
            datos.append(
                "idNivelEducativo",
                document.getElementById("EditarNivelFicha").value
            );
            datos.append(
                "institucion",
                document.getElementById("EditarInstitucionFicha").value.trim()
            );
            datos.append(
                "observaciones",
                document.getElementById("EditarResumenFicha").value.trim()
            );
            datos.append(
                "necesidadesApoyo",
                document.getElementById("EditarNecesidadesFicha").value.trim()
            );

            await enviarFormulario(
                formularioEditarFicha,
                datos,
                "No se pudo actualizar la ficha del estudiante.",
                "Ficha del estudiante actualizada correctamente."
            );

        });

    }

});
