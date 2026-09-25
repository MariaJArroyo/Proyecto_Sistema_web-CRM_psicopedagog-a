document.addEventListener("DOMContentLoaded", function () {

    // =====================================================
    // REFERENCIAS Y CONSTANTES
    // =====================================================

    const buscador = document.querySelector("[data-buscar-texto]");
    const filtroEstado = document.querySelector("[data-buscar-estado]");
    const tabla = document.getElementById("TablaClientes");
    const sinResultados = document.getElementById("SinResultados");

    const PatronCorreo = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

    const clasesEstado = {
        "nuevo": "badge-estado-nuevo",
        "contactado": "badge-estado-contactado",
        "cita agendada": "badge-estado-cita-agendada",
        "cliente activo": "badge-estado-activo",
        "inactivo": "badge-estado-inactivo",
        "en seguimiento": "badge-estado-seguimiento"
    };


    // =====================================================
    // UTILIDADES
    // =====================================================

    function valorTexto(valor) {
        const texto = (valor ?? "").toString().trim();
        return texto === "" ? "-" : texto;
    }

    function escaparHtml(texto) {
        const div = document.createElement("div");
        div.textContent = texto ?? "";
        return div.innerHTML;
    }

    function mostrarModal(id) {
        bootstrap.Modal.getOrCreateInstance(document.getElementById(id)).show();
    }

    function ocultarModal(id) {
        bootstrap.Modal.getOrCreateInstance(document.getElementById(id)).hide();
    }

    function tokenAntifalsificacion() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    // Fecha de hoy en formato yyyy-MM-dd (hora local)
    function hoyIso() {
        const hoy = new Date();
        hoy.setMinutes(hoy.getMinutes() - hoy.getTimezoneOffset());
        return hoy.toISOString().slice(0, 10);
    }

    function formatearFecha(valor) {
        if (!valor) return "-";
        return new Date(valor).toLocaleDateString("es-CR", {
            day: "2-digit", month: "long", year: "numeric"
        });
    }

    function mostrarAlerta(elemento, mensaje, tipo = "danger") {
        elemento.className = "alert alert-" + tipo;
        elemento.textContent = mensaje;
    }

    function ocultarAlerta(elemento) {
        elemento.className = "alert d-none";
        elemento.textContent = "";
    }

    // POST con FormData. Devuelve { ok, mensaje }.
    async function enviar(url, datos) {
        try {
            const respuesta = await fetch(url, { method: "POST", body: datos });

            if (respuesta.ok) return { ok: true };

            let mensaje = "No se pudo completar la operación.";
            try {
                const json = await respuesta.json();
                if (json && json.mensaje) mensaje = json.mensaje;
            } catch { }

            return { ok: false, mensaje };

        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }

    async function obtenerCliente(id) {
        const respuesta = await fetch("/Admin/ObtenerCliente/" + id, {
            headers: { "Accept": "application/json" }
        });

        if (!respuesta.ok) {
            throw new Error("No se pudo cargar la información del cliente.");
        }

        return await respuesta.json();
    }


    // =====================================================
    // VALIDACIONES
    // =====================================================

    function marcar(campo, valido) {
        campo.classList.toggle("is-invalid", !valido);
        return valido;
    }

    function limpiarValidacion(contenedor) {
        contenedor.querySelectorAll(".is-invalid")
            .forEach(campo => campo.classList.remove("is-invalid"));
    }

    function validarRequeridos(contenedor) {
        let valido = true;
        contenedor.querySelectorAll("[required]").forEach(function (campo) {
            if (!marcar(campo, campo.value.trim() !== "")) valido = false;
        });
        return valido;
    }

    // Acepta "8888-8888" o "8888 8888": se guardan solo los digitos
    function validarTelefono(campo) {
        campo.value = campo.value.replace(/\D/g, "");
        return marcar(campo, /^\d{8}$/.test(campo.value));
    }

    function validarCorreo(campo) {
        const valor = campo.value.trim();
        return marcar(campo, valor === "" || PatronCorreo.test(valor));
    }

    function validarFecha(campo) {
        return marcar(campo, campo.value === "" || campo.value <= hoyIso());
    }

    // Al corregir un campo se le quita el rojo
    document.addEventListener("input", e => e.target.classList?.remove("is-invalid"));
    document.addEventListener("change", e => e.target.classList?.remove("is-invalid"));


    // =====================================================
    // FILTRAR CLIENTES
    // =====================================================

    function filtrarClientes() {

        if (!buscador || !filtroEstado || !tabla) return;

        const texto = buscador.value.toLowerCase().trim();
        const estado = filtroEstado.value.toLowerCase().trim();
        const filas = tabla.querySelectorAll("tbody tr[data-fila]");

        let visibles = 0;

        filas.forEach(function (fila) {
            const contenido = fila.innerText.toLowerCase();
            const estadoFila = (fila.getAttribute("data-estado") || "").toLowerCase().trim();

            const coincide =
                (texto === "" || contenido.includes(texto)) &&
                (estado === "" || estadoFila === estado);

            fila.style.display = coincide ? "" : "none";
            if (coincide) visibles++;
        });

        if (sinResultados) {
            sinResultados.style.display = filas.length > 0 && visibles === 0 ? "block" : "none";
        }
    }

    if (buscador) buscador.addEventListener("input", filtrarClientes);
    if (filtroEstado) filtroEstado.addEventListener("change", filtrarClientes);


    // =====================================================
    // NUEVO CLIENTE (con lista de estudiantes)
    // =====================================================

    const modalNuevo = document.getElementById("ModalNuevoCliente");
    const formNuevo = document.getElementById("FormularioNuevoCliente");
    const listaNuevo = document.getElementById("ListaEstudiantesNuevo");
    const plantillaEstudiante = document.getElementById("PlantillaEstudianteNuevo");
    const errorNuevo = document.getElementById("NuevoClienteError");

    // Pone name="Estudiantes[i].Campo" consecutivos para que ASP.NET los enlace a la lista
    function renumerarEstudiantesNuevo() {
        const tarjetas = listaNuevo.querySelectorAll("[data-estudiante-nuevo]");

        tarjetas.forEach(function (tarjeta, i) {
            tarjeta.querySelector("[data-titulo]").textContent = "Estudiante " + (i + 1);

            tarjeta.querySelectorAll("[data-campo]").forEach(function (campo) {
                const nombre = campo.getAttribute("data-campo");
                campo.name = `Estudiantes[${i}].${nombre}`;
                campo.id = `NuevoEstudiante${i}_${nombre}`;

                const etiqueta = tarjeta.querySelector(`[data-label-para="${nombre}"]`);
                if (etiqueta) etiqueta.htmlFor = campo.id;
            });

            // Siempre debe quedar al menos un estudiante
            tarjeta.querySelector("[data-quitar-estudiante]").disabled = tarjetas.length === 1;
        });
    }

    function agregarEstudianteNuevo(enfocar) {
        const fragmento = plantillaEstudiante.content.cloneNode(true);
        fragmento.querySelector('[data-campo="FechaNacimiento"]').max = hoyIso();
        listaNuevo.appendChild(fragmento);
        renumerarEstudiantesNuevo();

        if (enfocar) {
            const tarjetas = listaNuevo.querySelectorAll("[data-estudiante-nuevo]");
            tarjetas[tarjetas.length - 1].querySelector('[data-campo="Nombre"]').focus();
        }
    }

    if (modalNuevo && formNuevo) {

        modalNuevo.addEventListener("show.bs.modal", function () {
            formNuevo.reset();
            limpiarValidacion(formNuevo);
            ocultarAlerta(errorNuevo);
            listaNuevo.innerHTML = "";
            agregarEstudianteNuevo(false);
        });

        document.getElementById("BotonAgregarEstudianteNuevo")
            .addEventListener("click", () => agregarEstudianteNuevo(true));

        listaNuevo.addEventListener("click", function (evento) {
            const boton = evento.target.closest("[data-quitar-estudiante]");
            if (!boton) return;
            boton.closest("[data-estudiante-nuevo]").remove();
            renumerarEstudiantesNuevo();
        });

        formNuevo.addEventListener("submit", async function (evento) {

            evento.preventDefault();
            ocultarAlerta(errorNuevo);

            let valido = validarRequeridos(formNuevo);
            valido = validarTelefono(document.getElementById("TelefonoNuevoCliente")) && valido;
            valido = validarCorreo(document.getElementById("CorreoNuevoCliente")) && valido;
            listaNuevo.querySelectorAll('[data-campo="FechaNacimiento"]')
                .forEach(campo => { valido = validarFecha(campo) && valido; });

            if (!valido) {
                mostrarAlerta(errorNuevo, "Revise los campos marcados en rojo.");
                return;
            }

            renumerarEstudiantesNuevo();

            const boton = formNuevo.querySelector('button[type="submit"]');
            boton.disabled = true;

            const resultado = await enviar(formNuevo.action, new FormData(formNuevo));

            if (resultado.ok) {
                location.reload();
                return;
            }

            mostrarAlerta(errorNuevo, resultado.mensaje);
            boton.disabled = false;
        });
    }


    // =====================================================
    // DETALLE DEL CLIENTE
    // =====================================================

    const modalDetalle = document.getElementById("ModalDetalleCliente");
    const mensajeDetalle = document.getElementById("DetalleClienteMensaje");
    const cuerpoEstudiantes = document.querySelector("#TablaEstudiantesCliente tbody");
    const panelEstudiante = document.getElementById("PanelEstudianteCliente");
    const formEstudiante = document.getElementById("FormularioEstudianteCliente");
    const errorEstudiante = document.getElementById("EstudianteClienteError");

    let clienteActual = null;
    let huboCambios = false;        // si se toco un estudiante, al cerrar se recarga la tabla
    let abrirEditarAlCerrar = false;

    function renderDetalle(cliente) {

        clienteActual = cliente;

        document.getElementById("DetalleEncargado").textContent =
            valorTexto(`${cliente.nombreEncargado ?? ""} ${cliente.apellidoEncargado ?? ""}`);
        document.getElementById("DetalleFechaRegistro").textContent = formatearFecha(cliente.fechaRegistro);
        document.getElementById("DetalleTelefono").textContent = valorTexto(cliente.telefono);
        document.getElementById("DetalleCorreo").textContent = valorTexto(cliente.correo);
        document.getElementById("DetalleServicio").textContent = valorTexto(cliente.servicio);
        document.getElementById("DetalleObservaciones").textContent = valorTexto(cliente.observaciones);

        const badge = document.getElementById("DetalleEstado");
        const estado = (cliente.estado || "").trim();
        badge.textContent = valorTexto(estado);
        badge.className = "badge align-middle ms-2 " + (clasesEstado[estado.toLowerCase()] || "");

        const inactivo = !cliente.activo;
        document.getElementById("BotonNuevoEstudianteCliente").disabled = inactivo;
        document.getElementById("AvisoClienteInactivo").classList.toggle("d-none", !inactivo);

        renderEstudiantes(cliente.estudiantes || []);
    }

    function renderEstudiantes(estudiantes) {

        document.getElementById("DetalleCantidadEstudiantes").textContent = estudiantes.length;
        document.getElementById("SinEstudiantesCliente").classList.toggle("d-none", estudiantes.length > 0);

        cuerpoEstudiantes.innerHTML = estudiantes.map(function (est) {

            const estado = est.activo
                ? '<span class="badge badge-estado-activo">Activo</span>'
                : '<span class="badge badge-estado-inactivo">Inactivo</span>';

            const botonEstado = est.activo
                ? `<button type="button" class="btn btn-sm btn-outline-danger border-0"
                           data-accion-estudiante="desactivar" data-id="${est.idEstudiante}" title="Desactivar">
                       <i class="bi bi-person-dash"></i>
                   </button>`
                : `<button type="button" class="btn btn-sm btn-outline-success border-0"
                           data-accion-estudiante="activar" data-id="${est.idEstudiante}" title="Activar">
                       <i class="bi bi-person-check"></i>
                   </button>`;

            return `
                <tr class="${est.activo ? "" : "text-muted"}">
                    <td class="fw-semibold">${escaparHtml(`${est.nombre} ${est.apellido}`)}</td>
                    <td>${escaparHtml(est.parentesco)}</td>
                    <td>${escaparHtml(valorTexto(est.nivelEducativo))}</td>
                    <td>${est.edad != null ? est.edad + " años" : "-"}</td>
                    <td>${estado}</td>
                    <td class="text-nowrap">
                        <a class="btn btn-sm btn-outline-secondary border-0"
                           href="/Admin/EstudianteFicha/${est.idEstudiante}" title="Ver ficha">
                            <i class="bi bi-file-earmark-person"></i>
                        </a>
                        <button type="button" class="btn btn-sm btn-outline-secondary border-0"
                                data-accion-estudiante="editar" data-id="${est.idEstudiante}" title="Editar">
                            <i class="bi bi-pencil"></i>
                        </button>
                        ${botonEstado}
                    </td>
                </tr>`;
        }).join("");
    }

    async function refrescarDetalle() {
        renderDetalle(await obtenerCliente(clienteActual.id));
    }

    function mostrarDetalle(cliente) {
        ocultarAlerta(mensajeDetalle);
        cerrarPanelEstudiante();
        renderDetalle(cliente);
        mostrarModal("ModalDetalleCliente");
    }

    // ---------- Panel agregar / editar estudiante ----------

    function abrirPanelEstudiante(estudiante) {

        formEstudiante.reset();
        limpiarValidacion(formEstudiante);
        ocultarAlerta(errorEstudiante);
        ocultarAlerta(mensajeDetalle);

        const esNuevo = !estudiante;
        formEstudiante.dataset.idEstudiante = esNuevo ? "" : estudiante.idEstudiante;

        document.getElementById("TituloPanelEstudiante").textContent =
            esNuevo ? "Agregar estudiante" : "Editar estudiante";

        const fecha = document.getElementById("EstudianteFechaNacimiento");
        fecha.max = hoyIso();

        if (!esNuevo) {
            document.getElementById("EstudianteNombre").value = estudiante.nombre ?? "";
            document.getElementById("EstudianteApellido").value = estudiante.apellido ?? "";
            document.getElementById("EstudianteParentesco").value = estudiante.idParentesco ?? "";
            document.getElementById("EstudianteNivel").value = estudiante.idNivelEducativo ?? "";
            fecha.value = estudiante.fechaNacimiento ? estudiante.fechaNacimiento.substring(0, 10) : "";
        }

        panelEstudiante.classList.remove("d-none");
        panelEstudiante.scrollIntoView({ behavior: "smooth", block: "nearest" });
        document.getElementById("EstudianteNombre").focus();
    }

    function cerrarPanelEstudiante() {
        panelEstudiante.classList.add("d-none");
    }

    if (modalDetalle) {

        document.getElementById("BotonNuevoEstudianteCliente")
            .addEventListener("click", () => abrirPanelEstudiante(null));

        document.getElementById("BotonCancelarEstudiante")
            .addEventListener("click", cerrarPanelEstudiante);

        formEstudiante.addEventListener("submit", async function (evento) {

            evento.preventDefault();
            ocultarAlerta(errorEstudiante);

            let valido = validarRequeridos(formEstudiante);
            valido = validarFecha(document.getElementById("EstudianteFechaNacimiento")) && valido;
            if (!valido) return;

            const idEstudiante = formEstudiante.dataset.idEstudiante;

            const url = idEstudiante
                ? `/Admin/EditarEstudianteCliente?idCliente=${clienteActual.id}&idEstudiante=${idEstudiante}`
                : `/Admin/AgregarEstudianteCliente?idCliente=${clienteActual.id}`;

            const boton = formEstudiante.querySelector('button[type="submit"]');
            boton.disabled = true;

            const resultado = await enviar(url, new FormData(formEstudiante));

            boton.disabled = false;

            if (!resultado.ok) {
                mostrarAlerta(errorEstudiante, resultado.mensaje);
                return;
            }

            huboCambios = true;
            cerrarPanelEstudiante();

            try {
                await refrescarDetalle();
                mostrarAlerta(mensajeDetalle,
                    idEstudiante ? "Estudiante actualizado correctamente." : "Estudiante agregado correctamente.",
                    "success");
            } catch (error) {
                mostrarAlerta(mensajeDetalle, error.message);
            }
        });

        // Acciones de cada fila de estudiante
        document.getElementById("TablaEstudiantesCliente").addEventListener("click", async function (evento) {

            const boton = evento.target.closest("button[data-accion-estudiante]");
            if (!boton) return;

            const idEstudiante = Number(boton.getAttribute("data-id"));
            const estudiante = (clienteActual.estudiantes || [])
                .find(e => e.idEstudiante === idEstudiante);
            if (!estudiante) return;

            const accion = boton.getAttribute("data-accion-estudiante");

            if (accion === "editar") {
                abrirPanelEstudiante(estudiante);
                return;
            }

            const activar = accion === "activar";
            const nombre = `${estudiante.nombre} ${estudiante.apellido}`;

            if (!confirm(activar ? `¿Activar a ${nombre}?` : `¿Desactivar a ${nombre}?`)) return;

            const datos = new FormData();
            datos.append("__RequestVerificationToken", tokenAntifalsificacion());
            datos.append("activo", activar);

            boton.disabled = true;
            ocultarAlerta(mensajeDetalle);

            const resultado = await enviar(
                `/Admin/CambiarEstadoEstudianteCliente?idCliente=${clienteActual.id}&idEstudiante=${idEstudiante}`,
                datos);

            if (!resultado.ok) {
                boton.disabled = false;
                mostrarAlerta(mensajeDetalle, resultado.mensaje);
                return;
            }

            huboCambios = true;

            try {
                await refrescarDetalle();
                mostrarAlerta(mensajeDetalle,
                    activar ? "Estudiante activado." : "Estudiante desactivado.",
                    "success");
            } catch (error) {
                mostrarAlerta(mensajeDetalle, error.message);
            }
        });

        // Editar cliente desde el detalle: se abre cuando el detalle termina de cerrarse
        document.getElementById("BotonEditarDesdeDetalle").addEventListener("click", function () {
            abrirEditarAlCerrar = true;
            ocultarModal("ModalDetalleCliente");
        });

        modalDetalle.addEventListener("hidden.bs.modal", function () {
            if (abrirEditarAlCerrar) {
                abrirEditarAlCerrar = false;
                abrirEditar(clienteActual);
                return;
            }

            // La tabla principal muestra la cantidad de estudiantes: se refresca
            if (huboCambios) location.reload();
        });
    }


    // =====================================================
    // EDITAR CLIENTE
    // =====================================================

    const modalEditar = document.getElementById("ModalEditarCliente");
    const formEditar = document.getElementById("FormularioEditarCliente");
    const errorEditar = document.getElementById("EditarClienteError");

    function abrirEditar(cliente) {

        formEditar.reset();
        limpiarValidacion(formEditar);
        ocultarAlerta(errorEditar);

        document.getElementById("EditarId").value = cliente.id;
        document.getElementById("EditarNombreEncargado").value = cliente.nombreEncargado ?? "";
        document.getElementById("EditarApellidoEncargado").value = cliente.apellidoEncargado ?? "";
        document.getElementById("EditarTelefono").value = cliente.telefono ?? "";
        document.getElementById("EditarCorreo").value = cliente.correo ?? "";
        document.getElementById("EditarServicio").value = cliente.idServicioInteres ?? "";
        document.getElementById("EditarEstado").value = cliente.idEstadoCliente ?? "";
        document.getElementById("EditarObservaciones").value = cliente.observaciones ?? "";

        mostrarModal("ModalEditarCliente");
    }

    if (formEditar) {

        formEditar.addEventListener("submit", async function (evento) {

            evento.preventDefault();
            ocultarAlerta(errorEditar);

            let valido = validarRequeridos(formEditar);
            valido = validarTelefono(document.getElementById("EditarTelefono")) && valido;
            valido = validarCorreo(document.getElementById("EditarCorreo")) && valido;

            if (!valido) return;

            const boton = formEditar.querySelector('button[type="submit"]');
            boton.disabled = true;

            const resultado = await enviar(formEditar.action, new FormData(formEditar));

            if (resultado.ok) {
                location.reload();
                return;
            }

            mostrarAlerta(errorEditar, resultado.mensaje);
            boton.disabled = false;
        });

        // Si se tocaron estudiantes y luego se cancela la edicion, igual se refresca
        modalEditar.addEventListener("hidden.bs.modal", function () {
            if (huboCambios) location.reload();
        });
    }


    // =====================================================
    // BOTONES DE LA TABLA DE CLIENTES
    // =====================================================

    if (tabla) {

        tabla.addEventListener("click", async function (evento) {

            const boton = evento.target.closest("button[data-accion]");
            if (!boton) return;

            const accion = boton.getAttribute("data-accion");
            const id = boton.getAttribute("data-id");
            const fila = boton.closest("tr");

            if (accion === "ver" || accion === "editar") {

                boton.disabled = true;

                try {
                    const cliente = await obtenerCliente(id);

                    if (accion === "ver") mostrarDetalle(cliente);
                    else abrirEditar(cliente);

                } catch (error) {
                    alert(error.message);
                } finally {
                    boton.disabled = false;
                }
            }

            if (accion === "eliminar") {

                document.getElementById("NombreClienteEliminar").textContent =
                    fila.querySelector('[data-campo="encargado"]')?.innerText.trim() || "";

                document.getElementById("BotonConfirmarEliminarCliente")
                    .setAttribute("data-id", id);

                mostrarModal("ModalEliminarCliente");
            }
        });
    }


    // =====================================================
    // CONFIRMAR DESACTIVACIÓN (sin cambios de logica)
    // =====================================================

    const botonConfirmarEliminar = document.getElementById("BotonConfirmarEliminarCliente");

    if (botonConfirmarEliminar) {

        botonConfirmarEliminar.addEventListener("click", async function () {

            const id = this.getAttribute("data-id");
            if (!id) return;

            const response = await fetch("/Admin/DesactivarCliente?id=" + id, { method: "POST" });

            if (response.ok) {
                location.reload();
            } else {
                alert("No se pudo desactivar el cliente.");
            }
        });
    }


    // =====================================================
    // TOOLTIPS
    // =====================================================

    document.querySelectorAll('[data-bs-toggle="tooltip"]')
        .forEach(elemento => new bootstrap.Tooltip(elemento));

});