document.addEventListener("DOMContentLoaded", function () {

    document.getElementById("FormularioDatosPersonales")
        .addEventListener("submit", function (evento) {
            var campoTelefono = document.getElementById("TelefonoPerfil");
            var esValido = true;

            esValido = ValidarObligatorio(document.getElementById("NombrePerfil"), "Ingrese su nombre completo.") && esValido;

            if (campoTelefono.value.trim() !== "") {
                esValido = ValidarTelefono(campoTelefono) && esValido;
            } else {
                MarcarCampoValido(campoTelefono);
            }

            esValido = ValidarCorreo(document.getElementById("CorreoPerfil")) && esValido;

            if (!esValido) evento.preventDefault();
        });

    document.getElementById("FormularioCambiarContrasena")
        .addEventListener("submit", function (evento) {
            var campoActual = document.getElementById("ContrasenaActual");
            var campoNueva = document.getElementById("ContrasenaNueva");
            var campoConfirmar = document.getElementById("ContrasenaConfirmar");
            var esValido = true;

            esValido = ValidarObligatorio(campoActual, "Ingrese su contraseña actual.") && esValido;

            if (!campoNueva.value || campoNueva.value.length < 8) {
                MarcarCampoInvalido(campoNueva, "La nueva contraseña debe tener al menos 8 caracteres.");
                esValido = false;
            } else {
                MarcarCampoValido(campoNueva);
            }

            if (!campoConfirmar.value || campoConfirmar.value !== campoNueva.value) {
                MarcarCampoInvalido(campoConfirmar, "Las contraseñas no coinciden.");
                esValido = false;
            } else {
                MarcarCampoValido(campoConfirmar);
            }

            if (!esValido) evento.preventDefault();
        });
});