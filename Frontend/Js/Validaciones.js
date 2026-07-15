

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
  var campoTelefono = document.getElementById("Telefono");
  var campoCorreo = document.getElementById("CorreoElectronico");
  var campoServicio = document.getElementById("ServicioInteres");
  var campoMensaje = document.getElementById("Mensaje");
  var alertaExito = document.getElementById("ConfirmacionContacto");

  formulario.addEventListener("submit", function (evento) {
    evento.preventDefault();
    evento.stopPropagation();

    var esValido = true;
    esValido = ValidarObligatorio(campoNombre, "Ingrese su nombre completo.") && esValido;
    esValido = ValidarTelefono(campoTelefono) && esValido;
    esValido = ValidarCorreo(campoCorreo) && esValido;
    esValido = ValidarObligatorio(campoServicio, "Seleccione un servicio de interés.") && esValido;
    esValido = ValidarObligatorio(campoMensaje, "Escriba un breve mensaje.") && esValido;

    if (!esValido) {
      if (alertaExito) {
        alertaExito.classList.add("d-none");
      }
      return;
    }

    formulario.reset();
    formulario.querySelectorAll(".is-valid").forEach(function (campo) {
      campo.classList.remove("is-valid");
    });

    if (alertaExito) {
      alertaExito.classList.remove("d-none");
      alertaExito.scrollIntoView({ behavior: "smooth", block: "center" });
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

  var destinosPorTipoAcceso = {
    Admin: "Admin/Dashboard.html",
    Portal: "Portal/Inicio.html"
  };

  formulario.addEventListener("submit", function (evento) {
    evento.preventDefault();
    evento.stopPropagation();

    var esValido = true;
    esValido = ValidarCorreo(campoCorreo) && esValido;
    esValido = ValidarObligatorio(campoContrasena, "Ingrese su contraseña.") && esValido;

    if (!esValido) {
      return;
    }

    var tipoAcceso = campoTipoAcceso ? campoTipoAcceso.value : "Admin";
    window.location.href = destinosPorTipoAcceso[tipoAcceso] || destinosPorTipoAcceso.Admin;
  });
}
