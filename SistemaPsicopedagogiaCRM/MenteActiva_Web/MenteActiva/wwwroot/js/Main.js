

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

// Tablas con buscador: data-tabla-buscador="IdTabla" en el contenedor de filtros.
// Opcional: data-tabla-paginar="10" pagina las filas que pasan el filtro.
// El filtro se aplica tambien al cargar la pagina, para respetar la opcion
// marcada como "selected" en el filtro de estado (por ejemplo, Activo).
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

    var tamanoInicial = parseInt(contenedor.getAttribute("data-tabla-paginar"), 10) || 0;
    var paginador = tamanoInicial > 0 ? CrearPaginador(tabla, tablaId, tamanoInicial) : null;
    var paginaActual = 1;

    function Filtrar() {
      var texto = campoTexto ? campoTexto.value.trim().toLowerCase() : "";
      var estado = campoEstado ? campoEstado.value : "";
      var filas = Array.prototype.slice.call(tabla.querySelectorAll("tbody tr"));

      var coincidentes = filas.filter(function (fila) {
        var coincideTexto = !texto || fila.textContent.toLowerCase().indexOf(texto) !== -1;
        var coincideEstado = !estado || fila.getAttribute("data-estado") === estado;
        return coincideTexto && coincideEstado;
      });

      // Sin paginador se muestran todas las que coinciden
      var tamano = paginador ? parseInt(paginador.tamano.value, 10) : Math.max(coincidentes.length, 1);
      var totalPaginas = Math.max(1, Math.ceil(coincidentes.length / tamano));
      paginaActual = Math.min(Math.max(paginaActual, 1), totalPaginas);

      var inicio = (paginaActual - 1) * tamano;
      var visibles = coincidentes.slice(inicio, inicio + tamano);

      filas.forEach(function (fila) {
        fila.classList.toggle("d-none", visibles.indexOf(fila) === -1);
      });

      // El aviso es de "sin resultados": una tabla vacia de origen no lo muestra
      if (estadoVacio) {
        estadoVacio.classList.toggle("d-none", filas.length === 0 || coincidentes.length !== 0);
      }

      if (paginador) {
        DibujarPaginador(paginador, coincidentes.length, inicio, tamano, paginaActual, totalPaginas);
      }
    }

    // Cualquier cambio de filtro vuelve a la primera pagina
    function Reiniciar() {
      paginaActual = 1;
      Filtrar();
    }

    if (campoTexto) {
      campoTexto.addEventListener("input", Reiniciar);
    }
    if (campoEstado) {
      campoEstado.addEventListener("change", Reiniciar);
    }
    if (paginador) {
      paginador.tamano.addEventListener("change", Reiniciar);
      paginador.lista.addEventListener("click", function (evento) {
        var boton = evento.target.closest("button[data-pagina]");
        if (!boton || boton.disabled) {
          return;
        }
        paginaActual = parseInt(boton.getAttribute("data-pagina"), 10);
        Filtrar();
      });
    }

    Filtrar();
  });
}

// Inserta debajo de la tabla: selector de filas por pagina, resumen y botones
function CrearPaginador(tabla, tablaId, tamanoInicial) {
  var envoltura = tabla.closest(".table-responsive") || tabla;
  var idTamano = "TamanoPagina" + tablaId;

  var opciones = [10, 25, 50];
  if (opciones.indexOf(tamanoInicial) === -1) {
    opciones.push(tamanoInicial);
    opciones.sort(function (a, b) { return a - b; });
  }

  var bloque = document.createElement("div");
  bloque.className = "PaginacionTabla d-flex flex-wrap align-items-center justify-content-between gap-2 mt-3";
  bloque.innerHTML =
    '<div class="d-flex align-items-center gap-2 small TextoSuave">' +
      '<label for="' + idTamano + '" class="mb-0">Mostrar</label>' +
      '<select class="form-select form-select-sm w-auto" id="' + idTamano + '">' +
        opciones.map(function (n) {
          return '<option value="' + n + '"' + (n === tamanoInicial ? " selected" : "") + ">" + n + "</option>";
        }).join("") +
      "</select>" +
      '<span aria-live="polite" data-resumen></span>' +
    "</div>" +
    '<nav aria-label="Paginación"><ul class="pagination pagination-sm mb-0"></ul></nav>';

  envoltura.insertAdjacentElement("afterend", bloque);

  return {
    bloque: bloque,
    tamano: bloque.querySelector("select"),
    resumen: bloque.querySelector("[data-resumen]"),
    nav: bloque.querySelector("nav"),
    lista: bloque.querySelector("ul")
  };
}

function DibujarPaginador(paginador, total, inicio, tamano, actual, totalPaginas) {
  paginador.bloque.classList.toggle("d-none", total === 0);
  paginador.resumen.textContent = (inicio + 1) + "–" + Math.min(inicio + tamano, total) + " de " + total;

  // Con una sola pagina no hace falta la botonera
  paginador.nav.classList.toggle("d-none", totalPaginas === 1);

  var html = BotonPagina('<i class="bi bi-chevron-left" aria-hidden="true"></i>', actual - 1,
    false, actual === 1, "Página anterior");

  NumerosDePagina(totalPaginas, actual).forEach(function (numero) {
    html += numero === "…"
      ? '<li class="page-item disabled"><span class="page-link">…</span></li>'
      : BotonPagina(numero, numero, numero === actual, false, "Página " + numero);
  });

  html += BotonPagina('<i class="bi bi-chevron-right" aria-hidden="true"></i>', actual + 1,
    false, actual === totalPaginas, "Página siguiente");

  paginador.lista.innerHTML = html;
}

// Numeros a mostrar: 1 … 4 5 6 … 12
function NumerosDePagina(total, actual) {
  var numeros = [1, actual - 1, actual, actual + 1, total]
    .filter(function (n, i, lista) { return n >= 1 && n <= total && lista.indexOf(n) === i; })
    .sort(function (a, b) { return a - b; });

  var resultado = [];
  numeros.forEach(function (numero, i) {
    if (i > 0 && numero - numeros[i - 1] > 1) {
      resultado.push("…");
    }
    resultado.push(numero);
  });
  return resultado;
}

function BotonPagina(contenido, pagina, activo, deshabilitado, etiqueta) {
  return '<li class="page-item' + (activo ? " active" : "") + (deshabilitado ? " disabled" : "") + '">' +
    '<button type="button" class="page-link" data-pagina="' + pagina + '" aria-label="' + etiqueta + '"' +
    (activo ? ' aria-current="page"' : "") + (deshabilitado ? " disabled" : "") + ">" + contenido + "</button></li>";
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
