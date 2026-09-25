

document.addEventListener("DOMContentLoaded", function () {
  InicializarFormularioContacto();
  InicializarFormularioLogin();
});

var PatronCorreo = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
var PatronTelefonoCR = /^[2-8]\d{3}-?\d{4}$/;

function MarcarCampoInvalido(campo, mensaje) {
  campo.classList.add("is-invalid");
  campo.classList.remove("is-valid");
  var contenedorError = campo.parentElement.querySelector(".invalid-feedback");
  if (contenedorError && mensaje) {
    contenedorError.textContent = mensaje;
  }
}


function MarcarCampoValido(campo) {
  campo.classList.remove("is-invalid");
  campo.classList.add("is-valid");
}

function ValidarObligatorio(campo, mensaje) {
  if (!campo.value || campo.value.trim() === "") {
    MarcarCampoInvalido(campo, mensaje || "Este campo es obligatorio.");
    return false;
  }
  MarcarCampoValido(campo);
  return true;
}


function ValidarCorreo(campo) {
  if (!campo.value || campo.value.trim() === "") {
    MarcarCampoInvalido(campo, "El correo es obligatorio.");
    return false;
  }
  if (!PatronCorreo.test(campo.value.trim())) {
    MarcarCampoInvalido(campo, "Ingrese un correo electrónico válido.");
    return false;
  }
  MarcarCampoValido(campo);
  return true;
}


function ValidarTelefono(campo) {
  var valor = campo.value.trim();
  if (!valor) {
    MarcarCampoInvalido(campo, "El teléfono es obligatorio.");
    return false;
  }
  if (!PatronTelefonoCR.test(valor)) {
    MarcarCampoInvalido(campo, "Ingrese un teléfono válido (ej. 8888-8888).");
    return false;
  }
  MarcarCampoValido(campo);
  return true;
}

function InicializarFormularioContacto() {
  var formulario = document.getElementById("FormularioContacto");
  if (!formulario) {
    return;
  }

  var campoNombre = document.getElementById("Nombre");
  var campoApellido = document.getElementById("Apellido");
  var campoTelefono = document.getElementById("Telefono");
  var campoCorreo = document.getElementById("CorreoElectronico");
  var campoServicio = document.getElementById("ServicioInteres");
  var campoMensaje = document.getElementById("Mensaje");
  var alertaExito = document.getElementById("ConfirmacionContacto");
  var alertaError = document.getElementById("ErrorContacto");
  var boton = document.getElementById("BotonEnviarContacto");
  var textoBoton = boton.innerHTML;

  function MostrarError(mensaje) {
    alertaExito.classList.add("d-none");
    alertaError.textContent = mensaje;
    alertaError.classList.remove("d-none");
    alertaError.scrollIntoView({ behavior: "smooth", block: "center" });
  }

  formulario.addEventListener("submit", async function (evento) {
    evento.preventDefault();
    evento.stopPropagation();

    alertaError.classList.add("d-none");

    var esValido = true;
    esValido = ValidarObligatorio(campoNombre, "Ingrese su nombre.") && esValido;
    esValido = ValidarObligatorio(campoApellido, "Ingrese su apellido.") && esValido;
    esValido = ValidarTelefono(campoTelefono) && esValido;
    esValido = ValidarCorreo(campoCorreo) && esValido;

    // Si el API no cargo servicios, el select queda solo con "Seleccione" y no se exige
    if (campoServicio.options.length > 1) {
      esValido = ValidarObligatorio(campoServicio, "Seleccione un servicio de interés.") && esValido;
    }

    esValido = ValidarObligatorio(campoMensaje, "Escriba un breve mensaje.") && esValido;

    if (!esValido) {
      alertaExito.classList.add("d-none");
      return;
    }

    boton.disabled = true;
    boton.innerHTML = '<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Enviando...';

    try {
      var respuesta = await fetch(formulario.action, {
        method: "POST",
        body: new FormData(formulario)
      });

      if (respuesta.ok) {
        formulario.reset();
        formulario.querySelectorAll(".is-valid, .is-invalid").forEach(function (campo) {
          campo.classList.remove("is-valid", "is-invalid");
        });

        alertaExito.classList.remove("d-none");
        alertaExito.scrollIntoView({ behavior: "smooth", block: "center" });
      } else {
        var mensaje = "No se pudo enviar el mensaje. Intente de nuevo más tarde.";
        try {
          var json = await respuesta.json();
          if (json && json.mensaje) {
            mensaje = json.mensaje;
          }
        } catch (e) { }

        MostrarError(mensaje);
      }
    } catch (e) {
      MostrarError("No se pudo conectar. Revise su conexión e intente de nuevo.");
    } finally {
      boton.disabled = false;
      boton.innerHTML = textoBoton;
    }
  });
}



function InicializarFormularioLogin() {
  var formulario = document.getElementById("FormularioLogin");
  if (!formulario) {
    return;
  }

  var campoCorreo = document.getElementById("CorreoElectronico");
  var campoContrasena = document.getElementById("Contrasena");
  var campoTipoAcceso = document.getElementById("TipoAcceso");
  var selectorTipoAcceso = document.getElementById("SelectorTipoAcceso");

  if (selectorTipoAcceso && campoTipoAcceso) {
    selectorTipoAcceso.querySelectorAll("[data-tipo-acceso]").forEach(function (boton) {
      boton.addEventListener("click", function () {
        selectorTipoAcceso.querySelectorAll("[data-tipo-acceso]").forEach(function (b) {
          b.classList.remove("active");
        });
        boton.classList.add("active");
        campoTipoAcceso.value = boton.getAttribute("data-tipo-acceso");
      });
    });
  }

  // El formulario se envia al servidor. A donde entra la persona lo decide el
  // rol que trae la base, no el tipo de acceso que eligio aqui.
  formulario.addEventListener("submit", function (evento) {
    var esValido = true;
    esValido = ValidarCorreo(campoCorreo) && esValido;
    esValido = ValidarObligatorio(campoContrasena, "Ingrese su contraseña.") && esValido;

    if (!esValido) {
      evento.preventDefault();
      evento.stopPropagation();
    }
  });
}
