
document.addEventListener("DOMContentLoaded", function () {

    // Validación de datos personales
    document.getElementById("FormularioDatosPersonales")
        .addEventListener("submit", function (evento) {

            var campoNombre = document.getElementById("NombrePerfilAdmin");
            var campoTelefono = document.getElementById("TelefonoPerfilAdmin");
            var campoCorreo = document.getElementById("CorreoPerfilAdmin");

            var esValido = true;

            esValido = ValidarObligatorio(
                campoNombre,
                "Ingrese su nombre completo."
            ) && esValido;

            // El teléfono es opcional.
            // Solo se valida cuando tiene contenido.
            if (campoTelefono.value.trim() !== "") {
                esValido = ValidarTelefono(campoTelefono) && esValido;
            } else {
                MarcarCampoValido(campoTelefono);
            }

            esValido = ValidarCorreo(campoCorreo) && esValido;

            if (!esValido) {
                evento.preventDefault();
            }
        });


    // Validación de cambio de contraseña
    document.getElementById("FormularioCambiarContrasenaAdmin")
        .addEventListener("submit", function (evento) {

            var campoActual = document.getElementById("ContrasenaActualAdmin");
            var campoNueva = document.getElementById("ContrasenaNuevaAdmin");
            var campoConfirmar = document.getElementById("ContrasenaConfirmarAdmin");

            var esValido = true;

            esValido = ValidarObligatorio(
                campoActual,
                "Ingrese su contraseña actual."
            ) && esValido;

            if (!campoNueva.value || campoNueva.value.length < 8) {
                MarcarCampoInvalido(
                    campoNueva,
                    "La nueva contraseña debe tener al menos 8 caracteres."
                );

                esValido = false;
            } else {
                MarcarCampoValido(campoNueva);
            }

            if (!campoConfirmar.value || campoConfirmar.value !== campoNueva.value) {
                MarcarCampoInvalido(
                    campoConfirmar,
                    "Las contraseñas no coinciden."
                );

                esValido = false;
            } else {
                MarcarCampoValido(campoConfirmar);
            }

            if (!esValido) {
                evento.preventDefault();
            }
        });

});
