

document.addEventListener("DOMContentLoaded", function () {
  InicializarSidebarAdmin();
  InicializarEnlaceActivo();
  InicializarScrollSuave();
  InicializarFiltrosLista();
  InicializarModalesDetalle();
  InicializarTablasBuscables();
  InicializarTooltips();
  InicializarSelectorEstudiantePortal();
});


function InicializarSidebarAdmin() {
  var wrapper = document.querySelector(".WrapperAdmin");
  if (!wrapper) {
    return;
  }

  var botonToggle = document.querySelector(".BotonSidebarToggle");
  var fondoMovil = document.querySelector(".FondoSidebarMovil");

  function EnVistaMovil() {
    return window.innerWidth <= 991.98;
  }

  function AlternarSidebar() {
    if (EnVistaMovil()) {
      wrapper.classList.toggle("sidebar-abierta");
    } else {
      wrapper.classList.toggle("sidebar-colapsada");
    }
  }

  if (botonToggle) {
    botonToggle.addEventListener("click", AlternarSidebar);
  }

  if (fondoMovil) {
    fondoMovil.addEventListener("click", function () {
      wrapper.classList.remove("sidebar-abierta");
    });
  }
}

function InicializarEnlaceActivo() {
  var paginaActual = window.location.pathname.split("/").pop().toLowerCase();
  var enlaces = document.querySelectorAll(".SidebarAdmin-Enlace, .NavbarPortal .nav-link");

  enlaces.forEach(function (enlace) {
    var destino = (enlace.getAttribute("href") || "").toLowerCase();
    if (destino && destino === paginaActual) {
      enlace.classList.add("activo", "active");
    }
  });
}

function InicializarScrollSuave() {
  var enlacesAncla = document.querySelectorAll('a[href^="#"]');

  enlacesAncla.forEach(function (enlace) {
    enlace.addEventListener("click", function (evento) {
      var destino = document.querySelector(enlace.getAttribute("href"));
      if (!destino) {
        return;
      }
      evento.preventDefault();
      destino.scrollIntoView({ behavior: "smooth", block: "start" });

      var navbarColapsable = document.querySelector(".navbar-collapse.show");
      if (navbarColapsable) {
        bootstrap.Collapse.getOrCreateInstance(navbarColapsable).hide();
      }
    });
  });
}

function InicializarFiltrosLista() {
  var gruposFiltro = document.querySelectorAll("[data-filtro-grupo]");

  gruposFiltro.forEach(function (grupo) {
    var grupoId = grupo.getAttribute("data-filtro-grupo");
    var items = document.querySelectorAll('[data-filtro-item="' + grupoId + '"]');
    var botones = grupo.querySelectorAll("[data-filtro-valor]");

    botones.forEach(function (boton) {
      boton.addEventListener("click", function () {
        botones.forEach(function (b) { b.classList.remove("active"); });
        boton.classList.add("active");
        var valor = boton.getAttribute("data-filtro-valor");

        items.forEach(function (item) {
          var coincide = valor === "Todas" || item.getAttribute("data-tipo") === valor;
          item.classList.toggle("d-none", !coincide);
        });
      });
    });
  });
}

function InicializarModalesDetalle() {
  var disparadores = document.querySelectorAll("[data-modal-detalle]");

  disparadores.forEach(function (disparador) {
    disparador.addEventListener("click", function () {
      var modalElemento = document.getElementById(disparador.getAttribute("data-modal-detalle"));
      if (!modalElemento) {
        return;
      }

      modalElemento.querySelectorAll("[data-campo]").forEach(function (campoModal) {
        var nombreCampo = campoModal.getAttribute("data-campo");
        var valor = disparador.getAttribute("data-valor-" + nombreCampo);
        if (valor !== null) {
          campoModal.textContent = valor;
        }
      });

      bootstrap.Modal.getOrCreateInstance(modalElemento).show();
    });
  });
}

function InicializarTablasBuscables() {
  var contenedores = document.querySelectorAll("[data-tabla-buscador]");

  contenedores.forEach(function (contenedor) {
    var tablaId = contenedor.getAttribute("data-tabla-buscador");
    var tabla = document.getElementById(tablaId);
    if (!tabla) {
      return;
    }

    var campoTexto = contenedor.querySelector("[data-buscar-texto]");
    var campoEstado = contenedor.querySelector("[data-buscar-estado]");
    var estadoVacio = document.querySelector('[data-tabla-vacio="' + tablaId + '"]');

    function Filtrar() {
      var texto = campoTexto ? campoTexto.value.trim().toLowerCase() : "";
      var estado = campoEstado ? campoEstado.value : "";
      var filas = tabla.querySelectorAll("tbody tr");
      var visibles = 0;

      filas.forEach(function (fila) {
        var coincideTexto = !texto || fila.textContent.toLowerCase().indexOf(texto) !== -1;
        var coincideEstado = !estado || fila.getAttribute("data-estado") === estado;
        var mostrar = coincideTexto && coincideEstado;
        fila.classList.toggle("d-none", !mostrar);
        if (mostrar) {
          visibles++;
        }
      });

      if (estadoVacio) {
        estadoVacio.classList.toggle("d-none", visibles !== 0);
      }
    }

    if (campoTexto) {
      campoTexto.addEventListener("input", Filtrar);
    }
    if (campoEstado) {
      campoEstado.addEventListener("change", Filtrar);
    }
  });
}


function InicializarTooltips() {
  var disparadores = document.querySelectorAll('[data-bs-toggle="tooltip"]');
  disparadores.forEach(function (disparador) {
    bootstrap.Tooltip.getOrCreateInstance(disparador);
  });
}

var CLAVE_ESTUDIANTE_PORTAL = "PortalEstudianteSeleccionado";

function InicializarSelectorEstudiantePortal() {
  var selector = document.querySelector("[data-selector-estudiante]");
  if (!selector) {
    return;
  }

  var etiqueta = selector.querySelector("[data-estudiante-etiqueta]");
  var opciones = selector.querySelectorAll("[data-estudiante-valor]");
  var bloques = document.querySelectorAll("[data-bloque-estudiante]");

  function AplicarSeleccion(valor, textoEtiqueta) {
    if (etiqueta && textoEtiqueta) {
      etiqueta.textContent = textoEtiqueta;
    }
    bloques.forEach(function (bloque) {
      bloque.classList.toggle("d-none", bloque.getAttribute("data-bloque-estudiante") !== valor);
    });
  }

  opciones.forEach(function (opcion) {
    opcion.addEventListener("click", function (evento) {
      evento.preventDefault();
      var valor = opcion.getAttribute("data-estudiante-valor");
      var textoEtiqueta = opcion.textContent.trim();
      AplicarSeleccion(valor, textoEtiqueta);
      sessionStorage.setItem(CLAVE_ESTUDIANTE_PORTAL, JSON.stringify({ valor: valor, etiqueta: textoEtiqueta }));
    });
  });

  var seleccionGuardada = sessionStorage.getItem(CLAVE_ESTUDIANTE_PORTAL);
  if (seleccionGuardada) {
    var datos = JSON.parse(seleccionGuardada);
    AplicarSeleccion(datos.valor, datos.etiqueta);
  }
}


function MostrarToast(mensaje, tipo) {
  var contenedor = document.querySelector(".ContenedorToasts");
  if (!contenedor) {
    return;
  }

  var clasesTipo = {
    exito: "text-bg-success",
    error: "text-bg-danger",
    info: "text-bg-primary"
  };
  var claseFondo = clasesTipo[tipo] || clasesTipo.exito;

  var toast = document.createElement("div");
  toast.className = "toast align-items-center " + claseFondo + " border-0";
  toast.setAttribute("role", "alert");
  toast.innerHTML =
    '<div class="d-flex">' +
    '<div class="toast-body">' + mensaje + "</div>" +
    '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
    "</div>";

  contenedor.appendChild(toast);
  var instanciaToast = new bootstrap.Toast(toast, { delay: 4000 });
  instanciaToast.show();

  toast.addEventListener("hidden.bs.toast", function () {
    toast.remove();
  });
}
