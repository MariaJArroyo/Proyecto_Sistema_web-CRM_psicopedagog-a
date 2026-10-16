
// ---------- auxiliares ----------

// Muestra el mensaje de éxito pendiente después de recargar.
function mostrarMensajePendiente() {
    const mensaje = sessionStorage.getItem("EstudiantesMensaje");

    if (!mensaje) return;

    sessionStorage.removeItem("EstudiantesMensaje");

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

// Agrega las diagonales de dd/mm/aaaa mientras se digita.
function aplicarMascaraFecha(campo) {
    campo.addEventListener("input", function () {
        const digitos = campo.value.replace(/\D/g, "").slice(0, 8);

        let texto = digitos;

        if (digitos.length > 4) {
            texto = digitos.slice(0, 2)
                + "/" + digitos.slice(2, 4)
                + "/" + digitos.slice(4);
        } else if (digitos.length > 2) {
            texto = digitos.slice(0, 2) + "/" + digitos.slice(2);
        }

        campo.value = texto;
    });
}

// Convierte dd/mm/aaaa a yyyy-MM-dd.
// Devuelve "" si está vacía y null si no es válida.
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

// ---------- formulario de nuevo estudiante ----------

function iniciarFormularioNuevoEstudiante(formulario) {
    const modal = document.getElementById("ModalNuevoEstudiante");

    const campoNombre = document.getElementById("NombreEstudiante");
    const campoApellido = document.getElementById("PrimerApellidoEstudiante");
    const campoFecha = document.getElementById("FechaNacimientoEstudiante");
    const campoParentesco = document.getElementById("ParentescoEstudiante");

    const campoBusqueda = document.getElementById("BuscarEncargadoLookup");
    const campoIdEncargado = document.getElementById("IdEncargadoSeleccionado");
    const lista = document.getElementById("ResultadosEncargado");
    const aviso = document.getElementById("EncargadoSeleccionado");

    aplicarMascaraFecha(campoFecha);

    let temporizador = null;
    let numeroBusqueda = 0;

    // Marca el campo inválido y quita la marca al corregirlo.
    function marcarInvalido(campo) {
        campo.classList.add("is-invalid");
        campo.focus();

        const quitar = function () {
            campo.classList.remove("is-invalid");
            campo.removeEventListener("input", quitar);
            campo.removeEventListener("change", quitar);
        };

        campo.addEventListener("input", quitar);
        campo.addEventListener("change", quitar);
    }

    // ---------- lookup de persona encargada ----------

    function ocultarLista() {
        lista.classList.add("d-none");
    }

    function mostrarTextoEnLista(texto) {
        lista.replaceChildren();

        const item = document.createElement("div");
        item.className = "list-group-item text-muted small";
        item.textContent = texto;

        lista.appendChild(item);
        lista.classList.remove("d-none");
    }

    function seleccionarEncargado(encargado) {
        campoIdEncargado.value = encargado.idEncargado;
        campoBusqueda.value = encargado.nombreCompleto;
        campoBusqueda.classList.remove("is-invalid");

        aviso.className = "form-text";
        aviso.textContent = "Persona encargada seleccionada.";

        ocultarLista();
    }

    async function buscarEncargados() {
        const texto = campoBusqueda.value.trim();
        const numero = ++numeroBusqueda;

        try {
            const respuesta = await fetch(
                formulario.dataset.urlEncargados
                + "?texto=" + encodeURIComponent(texto)
            );

            if (numero !== numeroBusqueda) return;

            if (!respuesta.ok) {
                mostrarTextoEnLista(
                    "No se pudo cargar la lista de encargados."
                );
                return;
            }

            const encargados = await respuesta.json();

            if (numero !== numeroBusqueda) return;

            if (!encargados.length) {
                mostrarTextoEnLista(
                    "No se encontraron personas encargadas."
                );
                return;
            }

            lista.replaceChildren();

            encargados.forEach(function (encargado) {
                const boton = document.createElement("button");
                boton.type = "button";
                boton.className =
                    "list-group-item list-group-item-action";

                const nombre = document.createElement("div");
                nombre.className = "fw-semibold";
                nombre.textContent = encargado.nombreCompleto;

                const detalle = document.createElement("div");
                detalle.className = "small text-muted";
                detalle.textContent = [
                    encargado.identificacion,
                    encargado.correo
                ]
                    .filter(Boolean)
                    .join(" · ");

                boton.append(nombre, detalle);

                boton.addEventListener("click", function () {
                    seleccionarEncargado(encargado);
                });

                lista.appendChild(boton);
            });

            lista.classList.remove("d-none");

        } catch (error) {
            console.error("Error al buscar encargados:", error);

            mostrarTextoEnLista(
                "No se pudo cargar la lista de encargados."
            );
        }
    }

    // Al escribir, descarta la selección anterior y busca de nuevo.
    campoBusqueda.addEventListener("input", function () {
        campoIdEncargado.value = "";
        campoBusqueda.classList.remove("is-invalid");

        aviso.className = "form-text";
        aviso.textContent = "";

        clearTimeout(temporizador);
        temporizador = setTimeout(buscarEncargados, 250);
    });

    // Muestra los primeros resultados al entrar sin selección.
    campoBusqueda.addEventListener("focus", function () {
        if (!campoIdEncargado.value) {
            buscarEncargados();
        }
    });

    // Cierra la lista al hacer clic fuera.
    document.addEventListener("click", function (evento) {
        if (!campoBusqueda.contains(evento.target)
            && !lista.contains(evento.target)) {
            ocultarLista();
        }
    });

    // Limpia el formulario al cerrar el modal.
    modal.addEventListener("hidden.bs.modal", function () {
        formulario.reset();

        formulario.querySelectorAll(".is-invalid").forEach(function (campo) {
            campo.classList.remove("is-invalid");
        });

        campoIdEncargado.value = "";
        campoBusqueda.classList.remove("is-invalid");

        aviso.className = "form-text";
        aviso.textContent = "";

        ocultarLista();
    });

    // ---------- guardar ----------

    formulario.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        if (!campoNombre.value.trim()) {
            marcarInvalido(campoNombre);
            return;
        }

        if (!campoApellido.value.trim()) {
            marcarInvalido(campoApellido);
            return;
        }

        if (!campoIdEncargado.value) {
            campoBusqueda.classList.add("is-invalid");

            aviso.className = "form-text text-danger";
            aviso.textContent =
                "Seleccione una persona encargada de la lista.";

            campoBusqueda.focus();
            return;
        }

        if (!campoParentesco.value) {
            if (campoParentesco.options.length <= 1) {
                alert(
                    "No se cargó la lista de parentescos. "
                    + "Recargue la página; si sigue igual, "
                    + "avise al equipo técnico."
                );
            }

            marcarInvalido(campoParentesco);
            return;
        }

        const fechaIso = fechaAIso(campoFecha.value.trim());

        if (fechaIso === null) {
            alert(
                "Ingrese la fecha de nacimiento con el formato dd/mm/aaaa."
            );
            campoFecha.focus();
            return;
        }

        if (fechaIso && new Date(fechaIso + "T00:00:00") > new Date()) {
            alert("La fecha de nacimiento no puede ser futura.");
            campoFecha.focus();
            return;
        }

        const datos = new FormData();

        const token = formulario.querySelector(
            'input[name="__RequestVerificationToken"]'
        );

        if (token) {
            datos.append("__RequestVerificationToken", token.value);
        }

        datos.append("nombre", campoNombre.value.trim());
        datos.append("primerApellido", campoApellido.value.trim());

        datos.append(
            "segundoApellido",
            document.getElementById("SegundoApellidoEstudiante").value.trim()
        );

        datos.append("fechaNacimiento", fechaIso);

        datos.append(
            "idNivelEducativo",
            document.getElementById("NivelEducativo").value
        );

        datos.append(
            "institucion",
            document.getElementById("Institucion").value.trim()
        );

        datos.append(
            "observaciones",
            document.getElementById("ObservacionesEstudiante").value.trim()
        );

        datos.append("idEncargado", campoIdEncargado.value);
        datos.append("idParentesco", campoParentesco.value);

        const botonGuardar =
            formulario.querySelector('button[type="submit"]');

        botonGuardar.disabled = true;

        try {
            const respuesta = await fetch(formulario.dataset.url, {
                method: "POST",
                body: datos
            });

            if (!respuesta.ok) {
                let mensaje = "No se pudo registrar el estudiante.";

                try {
                    const error = await respuesta.json();
                    mensaje = error.mensaje || mensaje;
                } catch {
                    // Mantener el mensaje predeterminado.
                }

                alert(mensaje);
                return;
            }

            sessionStorage.setItem(
                "EstudiantesMensaje",
                "Estudiante registrado correctamente."
            );

            window.location.reload();

        } catch (error) {
            console.error("Error al registrar el estudiante:", error);

            alert("Ocurrió un error de conexión. Intente de nuevo.");

        } finally {
            botonGuardar.disabled = false;
        }
    });
}
// ---------- Agregar área de dificultad ----------

(function () {
    "use strict";

    function iniciarAgregarArea() {
        const formulario = document.getElementById("FormularioAgregarArea");

        if (!formulario) return;

        const campoNombre = document.getElementById("NombreArea");
        const campoObservacion = document.getElementById("ObservacionArea");
        const boton = formulario.querySelector('button[type="submit"]');

        const alerta = document.createElement("div");
        alerta.className = "alert d-none";
        alerta.setAttribute("role", "alert");

        formulario.querySelector(".modal-body").prepend(alerta);

        let guardando = false;

        formulario.addEventListener("submit", async function (evento) {
            evento.preventDefault();

            if (guardando) return;

            alerta.className = "alert d-none";
            alerta.textContent = "";

            const nombre = campoNombre.value.trim();
            const idEstudiante = Number(formulario.dataset.idEstudiante);

            if (!nombre) {
                alerta.className = "alert alert-warning";
                alerta.textContent = "Ingrese el nombre del área de dificultad.";
                campoNombre.focus();
                return;
            }

            if (!Number.isInteger(idEstudiante) || idEstudiante <= 0) {
                alerta.className = "alert alert-danger";
                alerta.textContent = "No se pudo identificar al estudiante. Recargue la página.";
                return;
            }

            if (!formulario.reportValidity()) return;

            const datos = new FormData(formulario);

            datos.set("idEstudiante", String(idEstudiante));
            datos.set("nombreArea", nombre);
            datos.set("observacion", campoObservacion.value.trim());

            guardando = true;

            const textoBoton = boton?.textContent;

            if (boton) {
                boton.disabled = true;
                boton.textContent = "Guardando…";
            }

            try {
                const respuesta = await fetch(formulario.dataset.url, {
                    method: "POST",
                    body: datos,
                    headers: {
                        Accept: "application/json"
                    }
                });

                if (!respuesta.ok) {
                    let mensaje = "No se pudo registrar el área de dificultad.";

                    try {
                        const error = await respuesta.json();
                        mensaje = error.mensaje || mensaje;
                    } catch {
                        // Conserva el mensaje general.
                    }

                    throw new Error(mensaje);
                }

                window.location.reload();
            } catch (error) {
                alerta.className = "alert alert-danger";
                alerta.textContent = error.message ||
                    "No se pudo conectar con el servidor.";
            } finally {
                guardando = false;

                if (boton) {
                    boton.disabled = false;
                    boton.textContent = textoBoton;
                }
            }
        });

        document.getElementById("ModalAgregarArea")
            ?.addEventListener("hide.bs.modal", function (evento) {
                if (guardando) evento.preventDefault();
            });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciarAgregarArea);
    } else {
        iniciarAgregarArea();
    }
})();

// ---------- Editar área de dificultad ----------

(function () {
    "use strict";

    function iniciarEditarArea() {
        const formulario = document.getElementById("FormularioEditarArea");
        const modal = document.getElementById("ModalEditarArea");

        if (!formulario || !modal) return;

        const campoId = document.getElementById("EditarIdArea");
        const campoNombre = document.getElementById("EditarNombreArea");
        const campoObservacion = document.getElementById("EditarObservacionArea");
        const botonGuardar = formulario.querySelector('button[type="submit"]');

        const alerta = document.createElement("div");
        alerta.className = "alert d-none";
        alerta.setAttribute("role", "alert");

        formulario.querySelector(".modal-body").prepend(alerta);

        let guardando = false;

        document.addEventListener("click", function (evento) {
            const boton = evento.target.closest(".BotonEditarArea");

            if (!boton || guardando) return;

            evento.preventDefault();

            campoId.value = boton.dataset.idArea || "";
            campoNombre.value = boton.dataset.nombre || "";
            campoObservacion.value = boton.dataset.observacion || "";

            alerta.className = "alert d-none";
            alerta.textContent = "";

            bootstrap.Modal.getOrCreateInstance(modal).show();
        });

        formulario.addEventListener("submit", async function (evento) {
            evento.preventDefault();

            if (guardando) return;

            alerta.className = "alert d-none";
            alerta.textContent = "";

            const idEstudiante = Number(formulario.dataset.idEstudiante);
            const idArea = Number(campoId.value);
            const nombre = campoNombre.value.trim();

            if (!Number.isInteger(idEstudiante) || idEstudiante <= 0 ||
                !Number.isInteger(idArea) || idArea <= 0 || !nombre) {
                alerta.className = "alert alert-warning";
                alerta.textContent = "Seleccione un área válida e ingrese su nombre.";
                return;
            }

            if (!formulario.reportValidity()) return;

            const datos = new FormData(formulario);

            datos.set("idEstudiante", String(idEstudiante));
            datos.set("idArea", String(idArea));
            datos.set("nombreArea", nombre);
            datos.set("observacion", campoObservacion.value.trim());

            guardando = true;

            const textoOriginal = botonGuardar?.textContent;

            if (botonGuardar) {
                botonGuardar.disabled = true;
                botonGuardar.textContent = "Guardando…";
            }

            try {
                const respuesta = await fetch(formulario.dataset.url, {
                    method: "POST",
                    body: datos,
                    headers: {
                        Accept: "application/json"
                    }
                });

                if (!respuesta.ok) {
                    let mensaje = "No se pudo actualizar el área de dificultad.";

                    try {
                        const error = await respuesta.json();
                        mensaje = error.mensaje || mensaje;
                    } catch {
                        // Conserva el mensaje general.
                    }

                    throw new Error(mensaje);
                }

                window.location.reload();
            } catch (error) {
                alerta.className = "alert alert-danger";
                alerta.textContent = error.message ||
                    "No se pudo conectar con el servidor.";
            } finally {
                guardando = false;

                if (botonGuardar) {
                    botonGuardar.disabled = false;
                    botonGuardar.textContent = textoOriginal;
                }
            }
        });

        modal.addEventListener("hide.bs.modal", function (evento) {
            if (guardando) evento.preventDefault();
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", iniciarEditarArea);
    } else {
        iniciarEditarArea();
    }
})();

// ---------- inicialización ----------

document.addEventListener("DOMContentLoaded", function () {
    mostrarMensajePendiente();

    // Main.js inicializa filtros, paginación y tooltips.

    const formularioNuevo =
        document.getElementById("FormularioNuevoEstudiante");

    if (formularioNuevo) {
        iniciarFormularioNuevoEstudiante(formularioNuevo);
    }
});