document.addEventListener("DOMContentLoaded", function () {

    // =====================================================
    // REFERENCIAS
    // =====================================================

    const tabla = document.getElementById("TablaSolicitudes");
    const buscador = document.querySelector("[data-buscar-texto]");
    const filtroEstado = document.querySelector("[data-buscar-estado]");
    const sinResultados = document.getElementById("SinResultados");

    const modalDetalle = document.getElementById("ModalDetalleSolicitud");
    const alertaMensaje = document.getElementById("SolicitudMensaje");
    const campoNota = document.getElementById("SolicitudNota");

    const PENDIENTE = 1, CONTACTADA = 2, CONVERTIDA = 3, DESCARTADA = 4;

    const clasesEstado = {
        1: "badge-solicitud-pendiente",
        2: "badge-solicitud-contactada",
        3: "badge-solicitud-convertida",
        4: "badge-solicitud-descartada"
    };

    let solicitudActual = null;
    let huboCambios = false;
    let convertirAlCerrar = false;


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

    function formatearFechaHora(valor) {
        if (!valor) return "-";
        return new Date(valor).toLocaleString("es-CR", {
            day: "2-digit", month: "long", year: "numeric", hour: "2-digit", minute: "2-digit"
        });
    }

    function mostrarAlerta(mensaje, tipo = "danger") {
        alertaMensaje.className = "alert alert-" + tipo;
        alertaMensaje.textContent = mensaje;
    }

    function ocultarAlerta() {
        alertaMensaje.className = "alert d-none";
        alertaMensaje.textContent = "";
    }

    function tokenAntifalsificacion() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

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

    async function obtenerSolicitud(id) {
        const respuesta = await fetch("/Admin/ObtenerSolicitud/" + id, {
            headers: { "Accept": "application/json" }
        });

        if (!respuesta.ok) throw new Error("No se pudo cargar la solicitud.");

        return await respuesta.json();
    }

    function refrescarContador() {
        if (window.actualizarContadorSolicitudes) window.actualizarContadorSolicitudes();
    }


    // =====================================================
    // FILTROS (arranca mostrando solo las pendientes)
    // =====================================================

    function filtrar() {
        const texto = buscador.value.toLowerCase().trim();
        const estado = filtroEstado.value.toLowerCase().trim();
        const filas = tabla.querySelectorAll("tbody tr[data-fila]");

        let visibles = 0;

        filas.forEach(function (fila) {
            const coincide =
                (texto === "" || fila.innerText.toLowerCase().includes(texto)) &&
                (estado === "" || (fila.getAttribute("data-estado") || "").toLowerCase() === estado);

            fila.style.display = coincide ? "" : "none";
            if (coincide) visibles++;
        });

        sinResultados.style.display = filas.length > 0 && visibles === 0 ? "block" : "none";
    }

    buscador.addEventListener("input", filtrar);
    filtroEstado.addEventListener("change", filtrar);
    filtrar();


    // =====================================================
    // DETALLE
    // =====================================================

    function renderDetalle(s) {

        solicitudActual = s;

        document.getElementById("SolicitudNombre").textContent = `${s.nombre} ${s.apellido}`;
        document.getElementById("SolicitudFecha").textContent = formatearFechaHora(s.fechaRegistro);

        const badge = document.getElementById("SolicitudEstado");
        badge.textContent = s.estado;
        badge.className = "badge align-middle ms-2 " + (clasesEstado[s.idEstadoSolicitud] || "");

        const telefono = document.getElementById("SolicitudTelefono");
        telefono.textContent = valorTexto(s.telefono);
        telefono.href = "tel:" + (s.telefono ?? "");

        const correo = document.getElementById("SolicitudCorreo");
        correo.textContent = valorTexto(s.correo);
        correo.href = "mailto:" + (s.correo ?? "");

        document.getElementById("SolicitudServicio").textContent = valorTexto(s.servicio);
        document.getElementById("SolicitudTexto").textContent = valorTexto(s.mensaje);

        campoNota.value = s.notaInterna ?? "";

        const atencion = document.getElementById("SolicitudAtencion");
        atencion.classList.toggle("d-none", !s.atendidaPor);
        atencion.textContent = s.atendidaPor
            ? `Atendida por ${s.atendidaPor} el ${formatearFechaHora(s.fechaAtencion)}.`
            : "";

        const convertida = s.idEstadoSolicitud === CONVERTIDA;

        // Ya convertida: solo lectura
        const vinculada = document.getElementById("SolicitudVinculada");
        vinculada.classList.toggle("d-none", !convertida);
        vinculada.innerHTML = convertida
            ? `<i class="bi bi-check-circle me-1"></i>Convertida en el cliente
               <strong>${escaparHtml(s.clienteVinculado ?? "")}</strong>.
               <a href="/Admin/Clientes" class="alert-link ms-1">Ir a Clientes</a>`
            : "";

        campoNota.readOnly = convertida;

        // Posibles duplicados (solo si todavia no se convirtio)
        const coincidencias = convertida ? [] : (s.coincidencias || []);
        document.getElementById("SolicitudCoincidencias").classList.toggle("d-none", coincidencias.length === 0);

        document.getElementById("ListaCoincidencias").innerHTML = coincidencias.map(c => `
            <div class="list-group-item d-flex align-items-center justify-content-between gap-2">
                <div>
                    <div class="fw-semibold">${escaparHtml(c.encargado)}</div>
                    <div class="small TextoSuave">
                        ${escaparHtml(valorTexto(c.telefono))} · ${escaparHtml(valorTexto(c.correo))} · ${escaparHtml(c.estado)}
                    </div>
                </div>
                <button type="button" class="btn btn-sm btn-outline-primary"
                        data-vincular="${c.id}" data-nombre="${escaparHtml(c.encargado)}">
                    <i class="bi bi-link-45deg me-1"></i>Vincular
                </button>
            </div>`).join("");

        // Botones segun el estado
        const estado = s.idEstadoSolicitud;
        const ver = (id, visible) => document.getElementById(id).classList.toggle("d-none", !visible);

        ver("BotonDescartarSolicitud",   estado === PENDIENTE || estado === CONTACTADA);
        ver("BotonPendienteSolicitud",   estado === CONTACTADA || estado === DESCARTADA);
        ver("BotonGuardarNota",          !convertida);
        ver("BotonContactadaSolicitud",  estado === PENDIENTE);
        ver("BotonConvertirSolicitud",   estado === PENDIENTE || estado === CONTACTADA);
    }

    function mostrarDetalle(s, aviso) {
        ocultarAlerta();
        renderDetalle(s);
        if (aviso) mostrarAlerta(aviso, "warning");
        bootstrap.Modal.getOrCreateInstance(modalDetalle).show();
    }

    async function refrescarDetalle() {
        renderDetalle(await obtenerSolicitud(solicitudActual.idSolicitud));
    }


    // =====================================================
    // ACCIONES DEL DETALLE
    // =====================================================

    async function cambiarEstado(idEstado, mensajeOk) {

        const datos = new FormData();
        datos.append("__RequestVerificationToken", tokenAntifalsificacion());
        datos.append("idEstadoSolicitud", idEstado);
        datos.append("notaInterna", campoNota.value);

        ocultarAlerta();

        const resultado = await enviar(`/Admin/CambiarEstadoSolicitud/${solicitudActual.idSolicitud}`, datos);

        if (!resultado.ok) {
            mostrarAlerta(resultado.mensaje);
            return;
        }

        huboCambios = true;
        refrescarContador();

        try {
            await refrescarDetalle();
            mostrarAlerta(mensajeOk, "success");
        } catch (error) {
            mostrarAlerta(error.message);
        }
    }

    document.getElementById("BotonContactadaSolicitud")
        .addEventListener("click", () => cambiarEstado(CONTACTADA, "Solicitud marcada como contactada."));

    document.getElementById("BotonPendienteSolicitud")
        .addEventListener("click", () => cambiarEstado(PENDIENTE, "La solicitud volvió a pendiente."));

    document.getElementById("BotonGuardarNota")
        .addEventListener("click", () => cambiarEstado(solicitudActual.idEstadoSolicitud, "Nota guardada."));

    document.getElementById("BotonDescartarSolicitud").addEventListener("click", function () {
        if (!confirm("¿Descartar esta solicitud? Podrá volver a ponerla como pendiente después.")) return;
        cambiarEstado(DESCARTADA, "Solicitud descartada.");
    });

    // Vincular a un cliente existente
    document.getElementById("ListaCoincidencias").addEventListener("click", async function (evento) {

        const boton = evento.target.closest("[data-vincular]");
        if (!boton) return;

        const nombre = boton.getAttribute("data-nombre");
        if (!confirm(`¿Vincular esta solicitud al cliente ${nombre}?`)) return;

        const datos = new FormData();
        datos.append("__RequestVerificationToken", tokenAntifalsificacion());
        datos.append("idEncargado", boton.getAttribute("data-vincular"));

        boton.disabled = true;
        ocultarAlerta();

        const resultado = await enviar(`/Admin/VincularSolicitud/${solicitudActual.idSolicitud}`, datos);

        if (!resultado.ok) {
            boton.disabled = false;
            mostrarAlerta(resultado.mensaje);
            return;
        }

        huboCambios = true;
        refrescarContador();

        try {
            await refrescarDetalle();
            mostrarAlerta("Solicitud vinculada al cliente.", "success");
        } catch (error) {
            mostrarAlerta(error.message);
        }
    });


    // =====================================================
    // CONVERTIR A CLIENTE
    // =====================================================

    function datosParaCliente(s) {
        return {
            idSolicitud: s.idSolicitud,
            nombre: s.nombre,
            apellido: s.apellido,
            telefono: s.telefono,
            correo: s.correo,
            idServicioInteres: s.idServicioInteres,
            observaciones: "Mensaje recibido por el sitio web: " + (s.mensaje ?? "")
        };
    }

    // Desde el detalle: se cierra y, al terminar de cerrarse, se abre Nuevo cliente
    document.getElementById("BotonConvertirSolicitud").addEventListener("click", function () {
        convertirAlCerrar = true;
        bootstrap.Modal.getOrCreateInstance(modalDetalle).hide();
    });

    modalDetalle.addEventListener("hidden.bs.modal", function () {
        if (convertirAlCerrar) {
            convertirAlCerrar = false;
            window.abrirNuevoCliente(datosParaCliente(solicitudActual));
            return;
        }

        if (huboCambios) location.reload();
    });


    // =====================================================
    // BOTONES DE LA TABLA
    // =====================================================

    tabla.addEventListener("click", async function (evento) {

        const boton = evento.target.closest("button[data-accion]");
        if (!boton) return;

        const accion = boton.getAttribute("data-accion");
        boton.disabled = true;

        try {
            const solicitud = await obtenerSolicitud(boton.getAttribute("data-id"));

            if (accion === "ver") {
                mostrarDetalle(solicitud);
            }

            if (accion === "convertir") {
                // Si ya hay un cliente con ese telefono/correo, primero se muestra el aviso
                if ((solicitud.coincidencias || []).length > 0) {
                    mostrarDetalle(solicitud,
                        "Antes de convertir, revise si esta persona ya es cliente.");
                } else {
                    window.abrirNuevoCliente(datosParaCliente(solicitud));
                }
            }

        } catch (error) {
            alert(error.message);
        } finally {
            boton.disabled = false;
        }
    });


    // =====================================================
    // TOOLTIPS
    // =====================================================

    document.querySelectorAll('[data-bs-toggle="tooltip"]')
        .forEach(el => new bootstrap.Tooltip(el));

});