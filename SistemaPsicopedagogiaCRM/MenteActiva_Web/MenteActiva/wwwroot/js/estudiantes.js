
// ---------- auxiliares ----------

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

function fechaAIso(texto) {
    if (!texto) return "";

    const partes = /^(\d{2})\/(\d{2})\/(\d{4})$/.exec(texto);
    if (!partes) return null;

    const dia = Number(partes[1]);
    const mes = Number(partes[2]);
    const anio = Number(partes[3]);

    const fecha = new Date(anio, mes - 1, dia);

    if (fecha.getFullYear() !== anio ||
        fecha.getMonth() !== mes - 1 ||
        fecha.getDate() !== dia) {
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
    const avisoFormulario = document.getElementById("NuevoEstudianteError");

    function mostrarAvisoFormulario(mensaje) {
        avisoFormulario.className = "alert alert-danger";
        avisoFormulario.textContent = mensaje;
    }

    function ocultarAvisoFormulario() {
        avisoFormulario.className = "alert d-none";
        avisoFormulario.textContent = "";
    }

    const hoy = new Date();
    campoFecha.max = `${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, "0")}-${String(hoy.getDate()).padStart(2, "0")}`;

    let temporizador = null;
    let numeroBusqueda = 0;

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
                formulario.dataset.urlEncargados + "?texto=" + encodeURIComponent(texto)
            );

            if (numero !== numeroBusqueda) return;

            if (!respuesta.ok) {
                mostrarTextoEnLista("No se pudo cargar la lista de encargados.");
                return;
            }

            const encargados = await respuesta.json();

            if (numero !== numeroBusqueda) return;

            if (!encargados.length) {
                mostrarTextoEnLista("No se encontraron personas encargadas.");
                return;
            }

            lista.replaceChildren();

            encargados.forEach(function (encargado) {
                const boton = document.createElement("button");
                boton.type = "button";
                boton.className = "list-group-item list-group-item-action";

                const nombre = document.createElement("div");
                nombre.className = "fw-semibold";
                nombre.textContent = encargado.nombreCompleto;

                const detalle = document.createElement("div");
                detalle.className = "small text-muted";
                detalle.textContent = [encargado.identificacion, encargado.correo]
                    .filter(Boolean)
                    .join(" · ");

                boton.append(nombre, detalle);
                boton.addEventListener("click", function () {
                    seleccionarEncargado(encargado);
                });

                lista.appendChild(boton);
            });

            lista.classList.remove("d-none");
        } catch {
            mostrarTextoEnLista("No se pudo cargar la lista de encargados.");
        }
    }

    campoBusqueda.addEventListener("input", function () {
        campoIdEncargado.value = "";
        campoBusqueda.classList.remove("is-invalid");

        aviso.className = "form-text";
        aviso.textContent = "";

        clearTimeout(temporizador);
        temporizador = setTimeout(buscarEncargados, 250);
    });

    campoBusqueda.addEventListener("focus", function () {
        if (!campoIdEncargado.value) buscarEncargados();
    });

    document.addEventListener("click", function (evento) {
        if (!campoBusqueda.contains(evento.target) && !lista.contains(evento.target)) {
            ocultarLista();
        }
    });

    modal.addEventListener("hidden.bs.modal", function () {
        formulario.reset();
        ocultarAvisoFormulario();

        formulario.querySelectorAll(".is-invalid").forEach(campo => {
            campo.classList.remove("is-invalid");
        });

        campoIdEncargado.value = "";
        campoBusqueda.classList.remove("is-invalid");
        aviso.className = "form-text";
        aviso.textContent = "";
        ocultarLista();
    });

    formulario.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        ocultarAvisoFormulario();

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
            aviso.textContent = "Seleccione una persona encargada de la lista.";
            campoBusqueda.focus();
            return;
        }

        if (!campoParentesco.value) {
            if (campoParentesco.options.length <= 1) {
                mostrarAvisoFormulario("No se cargó la lista de parentescos. Recargue la página.");
            }

            marcarInvalido(campoParentesco);
            return;
        }

        const fechaIso = campoFecha.value;
        if (campoFecha.validity.badInput || (fechaIso && fechaIso > campoFecha.max)) {
            marcarInvalido(campoFecha);
            return;
        }

        const datos = new FormData();
        datos.append("__RequestVerificationToken",
            formulario.querySelector('input[name="__RequestVerificationToken"]').value);

        datos.append("Nombre", campoNombre.value.trim());
        datos.append("PrimerApellido", campoApellido.value.trim());
        datos.append("SegundoApellido", document.getElementById("SegundoApellidoEstudiante").value.trim());
        datos.append("FechaNacimiento", fechaIso);
        datos.append("IdNivelEducativo", document.getElementById("NivelEducativo").value);
        datos.append("Institucion", document.getElementById("Institucion").value.trim());
        datos.append("IdEncargado", campoIdEncargado.value);
        datos.append("IdParentesco", campoParentesco.value);
        datos.append("Observaciones", document.getElementById("ObservacionesEstudiante").value.trim());

        const botonGuardar = formulario.querySelector('button[type="submit"]');
        botonGuardar.disabled = true;

        try {
            const respuesta = await fetch(formulario.dataset.url, {
                method: "POST",
                body: datos,
                headers: { Accept: "application/json" }
            });

            if (!respuesta.ok) {
                let mensaje = "No se pudo registrar el estudiante.";
                try {
                    const error = await respuesta.json();
                    mensaje = error.mensaje || mensaje;
                } catch { }
                throw new Error(mensaje);
            }

            sessionStorage.setItem("EstudiantesMensaje", "Estudiante registrado correctamente.");
            window.location.reload();
        } catch (error) {
            mostrarAvisoFormulario(error.message || "Ocurrió un error de conexión. Intente de nuevo.");
        } finally {
            botonGuardar.disabled = false;
        }
    });
}


// ---------- acciones de la tabla: editar, inactivar y reactivar ----------

function iniciarAccionesTabla() {
    const tabla = document.getElementById("TablaEstudiantes");
    const formEditar = document.getElementById("FormularioEditarEstudiante");
    const formDesactivar = document.getElementById("FormularioDesactivarEstudiante");

    if (!tabla || !formEditar || !formDesactivar) return;

    const campo = id => document.getElementById(id);

    const modalEditar = bootstrap.Modal.getOrCreateInstance(campo("ModalEditarEstudiante"));
    const modalDesactivar = bootstrap.Modal.getOrCreateInstance(campo("ModalDesactivarEstudiante"));

    const avisoEditar = campo("EditarEstError");
    const avisoDesactivar = campo("DesactivarEstError");
    const botonGuardar = campo("BotonGuardarEdicionEstudiante");
    const botonDesactivar = campo("BotonConfirmarDesactivar");

    const buscarEncargado = campo("EditarBuscarEncargado");
    const idEncargado = campo("EditarIdEncargado");
    const resultadosEncargado = campo("EditarResultadosEncargado");
    const avisoEncargado = campo("EditarEncargadoSeleccionado");
    const parentesco = campo("EditarEstParentesco");

    let idEditando = 0;
    let ocupado = false;
    let temporizadorBusqueda = null;
    let numeroBusqueda = 0;

    aplicarMascaraFecha(campo("EditarEstFecha"));

    function mostrarAviso(aviso, texto, tipo) {
        aviso.className = "alert alert-" + (tipo || "danger");
        aviso.textContent = texto;
    }

    function ocultarAviso(aviso) {
        aviso.className = "alert d-none";
        aviso.textContent = "";
    }

    function fechaParaMostrar(valor) {
        if (!valor) return "";
        const partes = String(valor).slice(0, 10).split("-");
        return partes.length === 3 ? partes[2] + "/" + partes[1] + "/" + partes[0] : "";
    }

    function ocultarResultadosEncargado() {
        resultadosEncargado.classList.add("d-none");
    }

    function mostrarMensajeResultados(texto) {
        resultadosEncargado.replaceChildren();

        const elemento = document.createElement("div");
        elemento.className = "list-group-item text-muted small";
        elemento.textContent = texto;

        resultadosEncargado.appendChild(elemento);
        resultadosEncargado.classList.remove("d-none");
    }

    function seleccionarEncargado(encargado) {
        idEncargado.value = encargado.idEncargado;
        buscarEncargado.value = encargado.nombreCompleto;
        buscarEncargado.classList.remove("is-invalid");

        avisoEncargado.className = "form-text text-success";
        avisoEncargado.textContent = "Persona encargada seleccionada.";

        ocultarResultadosEncargado();
    }

    async function buscarEncargadosEdicion() {
        const texto = buscarEncargado.value.trim();
        const numero = ++numeroBusqueda;

        try {
            const respuesta = await fetch(
                formEditar.dataset.urlEncargados + "?texto=" + encodeURIComponent(texto)
            );

            if (numero !== numeroBusqueda) return;

            if (!respuesta.ok) {
                mostrarMensajeResultados("No se pudo cargar la lista de encargados.");
                return;
            }

            const encargados = await respuesta.json();

            if (numero !== numeroBusqueda) return;

            if (!encargados.length) {
                mostrarMensajeResultados("No se encontraron personas encargadas.");
                return;
            }

            resultadosEncargado.replaceChildren();

            encargados.forEach(function (encargado) {
                const boton = document.createElement("button");
                boton.type = "button";
                boton.className = "list-group-item list-group-item-action";

                const nombre = document.createElement("div");
                nombre.className = "fw-semibold";
                nombre.textContent = encargado.nombreCompleto;

                const detalle = document.createElement("div");
                detalle.className = "small text-muted";
                detalle.textContent = [encargado.identificacion, encargado.correo]
                    .filter(Boolean)
                    .join(" · ");

                boton.append(nombre, detalle);
                boton.addEventListener("click", function () {
                    seleccionarEncargado(encargado);
                });

                resultadosEncargado.appendChild(boton);
            });

            resultadosEncargado.classList.remove("d-none");
        } catch {
            mostrarMensajeResultados("No se pudo cargar la lista de encargados.");
        }
    }

    buscarEncargado.addEventListener("input", function () {
        idEncargado.value = "";
        buscarEncargado.classList.remove("is-invalid");
        avisoEncargado.className = "form-text text-muted";
        avisoEncargado.textContent = "Seleccione una persona de los resultados.";

        clearTimeout(temporizadorBusqueda);
        temporizadorBusqueda = setTimeout(buscarEncargadosEdicion, 250);
    });

    buscarEncargado.addEventListener("focus", function () {
        if (!idEncargado.value) buscarEncargadosEdicion();
    });

    document.addEventListener("click", function (evento) {
        if (!buscarEncargado.contains(evento.target) &&
            !resultadosEncargado.contains(evento.target)) {
            ocultarResultadosEncargado();
        }
    });

    // Envía el formulario y muestra el error legible si algo falla.
    async function enviar(url, datos, mensajePorDefecto) {
        const respuesta = await fetch(url, {
            method: "POST",
            body: datos,
            headers: { Accept: "application/json" }
        });

        if (respuesta.redirected) {
            throw new Error("La sesión pudo haber vencido. Recargue la página e intente nuevamente.");
        }

        if (!respuesta.ok) {
            let mensaje = mensajePorDefecto;
            try {
                const error = await respuesta.json();
                mensaje = error.mensaje || mensaje;
            } catch { }
            throw new Error(mensaje);
        }
    }

    async function abrirEdicion(id) {
        idEditando = id;
        formEditar.reset();
        ocultarAviso(avisoEditar);
        ocultarResultadosEncargado();

        idEncargado.value = "";
        avisoEncargado.textContent = "";
        botonGuardar.disabled = true;
        modalEditar.show();

        try {
            const respuesta = await fetch(
                formEditar.dataset.urlFicha + "?id=" + encodeURIComponent(id),
                { headers: { Accept: "application/json" } }
            );

            if (!respuesta.ok) {
                throw new Error("No se pudo cargar la información del estudiante.");
            }

            const ficha = await respuesta.json();
            const encargados = ficha.encargados || [];
            const principal = encargados.find(e => e.esPrincipal) || encargados[0];

            campo("EditarEstNombre").value = ficha.nombre || "";
            campo("EditarEstPrimerApellido").value = ficha.primerApellido || "";
            campo("EditarEstSegundoApellido").value = ficha.segundoApellido || "";
            campo("EditarEstFecha").value = fechaParaMostrar(ficha.fechaNacimiento);
            campo("EditarEstNivel").value = ficha.idNivelEducativo || "";
            campo("EditarEstInstitucion").value = ficha.institucion || "";
            campo("EditarEstObservaciones").value = ficha.observaciones || "";
            campo("EditarEstNecesidades").value = ficha.necesidadesApoyo || "";

            if (principal) {
                idEncargado.value = principal.idEncargado || "";
                buscarEncargado.value = principal.nombreCompleto || "";
                parentesco.value = principal.idParentesco || "";

                
            } else {
                buscarEncargado.value = "";
                parentesco.value = "";
                avisoEncargado.className = "form-text text-warning";
                avisoEncargado.textContent = "Este estudiante no tiene un encargado principal asociado.";
            }

            botonGuardar.disabled = false;
        } catch (error) {
            mostrarAviso(avisoEditar, error.message || "No se pudo cargar la información del estudiante.");
        }
    }

    async function activarEstudiante(boton) {
        if (ocupado) return;

        const datos = new FormData();
        datos.set("idCliente", boton.dataset.encargado);
        datos.set("idEstudiante", boton.dataset.activarEstudiante);
        datos.set("activo", "true");

        const token = formEditar.querySelector('input[name="__RequestVerificationToken"]');
        if (token) datos.set("__RequestVerificationToken", token.value);

        ocupado = true;
        boton.disabled = true;

        try {
            await enviar(tabla.dataset.urlActivar, datos, "No se pudo reactivar al estudiante.");

            sessionStorage.setItem("EstudiantesMensaje", "Estudiante reactivado correctamente.");
            window.location.reload();
        } catch (error) {
            const contenedor = document.getElementById("ContenedorMensaje");

            if (contenedor) {
                const alerta = document.createElement("div");
                alerta.className = "alert alert-danger alert-dismissible fade show";
                alerta.setAttribute("role", "alert");
                alerta.textContent = error.message || "No se pudo reactivar al estudiante.";

                const cerrar = document.createElement("button");
                cerrar.type = "button";
                cerrar.className = "btn-close";
                cerrar.setAttribute("data-bs-dismiss", "alert");
                cerrar.setAttribute("aria-label", "Cerrar");

                alerta.appendChild(cerrar);
                contenedor.replaceChildren(alerta);
            }

            boton.disabled = false;
        } finally {
            ocupado = false;
        }
    }

    tabla.addEventListener("click", function (evento) {
        const editar = evento.target.closest("[data-editar-estudiante]");

        if (editar) {
            abrirEdicion(Number(editar.dataset.editarEstudiante));
            return;
        }

        const activar = evento.target.closest("[data-activar-estudiante]");

        if (activar) {
            activarEstudiante(activar);
            return;
        }

        const desactivar = evento.target.closest("[data-desactivar-estudiante]");

        if (desactivar) {
            campo("DesactivarIdEstudiante").value = desactivar.dataset.desactivarEstudiante;
            campo("DesactivarNombreEstudiante").textContent = desactivar.dataset.nombre || "";
            ocultarAviso(avisoDesactivar);
            modalDesactivar.show();
        }
    });

    formEditar.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        if (ocupado || idEditando <= 0) return;

        ocultarAviso(avisoEditar);

        const nombre = campo("EditarEstNombre").value.trim();
        const apellido = campo("EditarEstPrimerApellido").value.trim();
        const nacimiento = fechaAIso(campo("EditarEstFecha").value.trim());

        if (!nombre || !apellido) {
            mostrarAviso(avisoEditar, "El nombre y el primer apellido son obligatorios.", "warning");
            return;
        }

        if (nacimiento === null) {
            mostrarAviso(avisoEditar, "Ingrese una fecha de nacimiento válida en formato dd/mm/aaaa.", "warning");
            return;
        }

        if (!idEncargado.value) {
            buscarEncargado.classList.add("is-invalid");
            avisoEncargado.className = "form-text text-danger";
            avisoEncargado.textContent = "Seleccione una persona encargada de los resultados.";
            buscarEncargado.focus();
            return;
        }

        if (!parentesco.value) {
            parentesco.classList.add("is-invalid");
            parentesco.focus();
            parentesco.addEventListener("change", () => parentesco.classList.remove("is-invalid"), { once: true });
            mostrarAviso(avisoEditar, "Seleccione el parentesco de la persona encargada.", "warning");
            return;
        }

        const datos = new FormData(formEditar);

        datos.set("IdEstudiante", String(idEditando));
        datos.set("Nombre", nombre);
        datos.set("PrimerApellido", apellido);
        datos.set("SegundoApellido", campo("EditarEstSegundoApellido").value.trim());
        datos.set("FechaNacimiento", nacimiento);
        datos.set("IdNivelEducativo", campo("EditarEstNivel").value);
        datos.set("Institucion", campo("EditarEstInstitucion").value.trim());
        datos.set("Observaciones", campo("EditarEstObservaciones").value.trim());
        datos.set("NecesidadesApoyo", campo("EditarEstNecesidades").value);
        datos.set("IdEncargado", idEncargado.value);
        datos.set("IdParentesco", parentesco.value);

        ocupado = true;
        botonGuardar.disabled = true;

        try {
            await enviar(formEditar.dataset.url, datos, "No se pudo actualizar al estudiante.");

            sessionStorage.setItem("EstudiantesMensaje", "Estudiante actualizado correctamente.");
            window.location.reload();
        } catch (error) {
            mostrarAviso(avisoEditar, error.message || "No se pudo actualizar al estudiante.");
            botonGuardar.disabled = false;
        } finally {
            ocupado = false;
        }
    });

    formDesactivar.addEventListener("submit", async function (evento) {
        evento.preventDefault();

        if (ocupado) return;

        ocultarAviso(avisoDesactivar);
        ocupado = true;
        botonDesactivar.disabled = true;

        try {
            await enviar(
                formDesactivar.dataset.url,
                new FormData(formDesactivar),
                "No se pudo inactivar al estudiante."
            );

            sessionStorage.setItem("EstudiantesMensaje", "Estudiante inactivado correctamente.");
            window.location.reload();
        } catch (error) {
            mostrarAviso(avisoDesactivar, error.message || "No se pudo inactivar al estudiante.");
            botonDesactivar.disabled = false;
        } finally {
            ocupado = false;
        }
    });
}


// ---------- inicialización ----------

document.addEventListener("DOMContentLoaded", function () {
    mostrarMensajePendiente();

    // Buscador, nivel, estado y paginación los maneja Main.js.
    document.querySelectorAll('[data-bs-toggle="tooltip"]').forEach(function (elemento) {
        new bootstrap.Tooltip(elemento);
    });

    const formularioNuevo = document.getElementById("FormularioNuevoEstudiante");

    if (formularioNuevo) {
        iniciarFormularioNuevoEstudiante(formularioNuevo);
    }

    iniciarAccionesTabla();
});
