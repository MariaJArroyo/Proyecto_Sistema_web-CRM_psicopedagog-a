// Mensajeria interna (HU-M10-4). El mismo archivo lo usan el panel y el portal:
// la pantalla es la misma de los dos lados y lo unico que cambia son las rutas
// del controlador, que llegan en window.MENSAJERIA_RUTAS.
//
// Todo pasa por el controlador del Web, que reenvia al API. El API devuelve solo
// los hilos del usuario autenticado y sus procedimientos validan que participe,
// asi que desde aqui no hace falta filtrar nada por seguridad.

(function () {
    "use strict";

    const RUTAS = window.MENSAJERIA_RUTAS;

    // Sin rutas la pantalla no es de mensajeria: no hay nada que hacer
    if (!RUTAS) return;

    let hilos = [];
    let idHiloAbierto = null;

    function $(id) {
        return document.getElementById(id);
    }

    function escaparHtml(texto) {
        const d = document.createElement("div");
        d.textContent = texto ?? "";
        return d.innerHTML;
    }

    function tokenAntifalsificacion() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function datosConToken(campos) {
        const datos = new FormData();
        datos.append("__RequestVerificationToken", tokenAntifalsificacion());
        Object.entries(campos).forEach(([clave, valor]) => {
            if (valor !== null && valor !== undefined && valor !== "") datos.append(clave, valor);
        });
        return datos;
    }

    function notificar(mensaje, tipo) {
        if (typeof MostrarToast === "function") MostrarToast(mensaje, tipo);
    }

    function mostrarAlerta(elemento, mensaje, tipo = "danger") {
        if (!elemento) return;
        elemento.className = "alert alert-" + tipo;
        elemento.textContent = mensaje;
    }

    function ocultarAlerta(elemento) {
        if (!elemento) return;
        elemento.className = "alert d-none";
        elemento.textContent = "";
    }

    function marcar(campo, valido, mensaje) {
        campo.classList.toggle("is-invalid", !valido);
        const aviso = campo.parentElement.querySelector(".invalid-feedback");
        if (aviso) aviso.textContent = valido ? "" : (mensaje || "");
        return valido;
    }

    async function leerMensaje(respuesta, porDefecto) {
        try {
            const json = await respuesta.json();
            return json?.mensaje || porDefecto;
        } catch {
            return porDefecto;
        }
    }

    async function enviarDatos(url, campos) {
        try {
            const respuesta = await fetch(url, { method: "POST", body: datosConToken(campos) });

            if (!respuesta.ok) {
                return { ok: false, mensaje: await leerMensaje(respuesta, "No se pudo enviar el mensaje.") };
            }

            return { ok: true, datos: await respuesta.json().catch(() => ({})) };
        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }

    function recortar(texto, largo) {
        const limpio = String(texto ?? "").replace(/\s+/g, " ").trim();
        return limpio.length <= largo ? limpio : limpio.slice(0, largo - 1) + "…";
    }

    function formatoFechaHora(iso) {
        if (!iso) return "";
        const f = new Date(iso);
        if (isNaN(f)) return "";
        return f.toLocaleDateString("es-CR", { day: "2-digit", month: "short" }) +
            ", " + f.toLocaleTimeString("es-CR", { hour: "2-digit", minute: "2-digit" });
    }

    // =====================================================
    // BANDEJA
    // =====================================================

    function pintarHilos() {
        const lista = $("ListaHilos");
        if (!lista) return;

        if (hilos.length === 0) {
            lista.innerHTML = '<div class="list-group-item TextoSuave">No hay conversaciones todavía.</div>';
            return;
        }

        lista.innerHTML = hilos.map(h => {
            const activo = h.idHilo === idHiloAbierto ? " active" : "";
            const sinLeer = h.sinLeer > 0
                ? `<span class="badge badge-estado-nuevo">${h.sinLeer}</span>`
                : "";
            const negrita = h.sinLeer > 0 ? " fw-semibold" : "";

            return `<button type="button" class="list-group-item list-group-item-action${activo}"
                data-hilo="${h.idHilo}">
                <div class="d-flex justify-content-between align-items-start gap-2">
                    <div class="text-start">
                        <div class="${negrita.trim()}">${escaparHtml(h.contraparte)}</div>
                        <div class="small">${escaparHtml(recortar(h.asunto, 34))}</div>
                        <div class="TextoSuave small">${escaparHtml(recortar(h.ultimoMensaje, 40))}</div>
                    </div>
                    <div class="text-end text-nowrap">
                        ${sinLeer}
                        <div class="TextoSuave small">${escaparHtml(formatoFechaHora(h.ultimaFecha))}</div>
                    </div>
                </div>
            </button>`;
        }).join("");
    }

    async function cargarHilos() {
        const lista = $("ListaHilos");
        const alerta = $("AlertaMensajeria");
        if (!lista) return;

        ocultarAlerta(alerta);

        try {
            const respuesta = await fetch(RUTAS.hilos);

            if (!respuesta.ok) {
                hilos = [];
                lista.innerHTML = "";
                mostrarAlerta(alerta, await leerMensaje(respuesta, "No se pudieron cargar las conversaciones."));
                return;
            }

            hilos = await respuesta.json();
            pintarHilos();
        } catch {
            hilos = [];
            lista.innerHTML = "";
            mostrarAlerta(alerta, "No se pudo conectar con el servidor.");
        }
    }

    // =====================================================
    // CONVERSACION
    // =====================================================

    function burbuja(m) {
        // Lo mio a la derecha, lo de la contraparte a la izquierda
        const alineacion = m.mio ? "ms-auto text-end" : "me-auto";
        const fondo = m.mio ? "bg-primary text-white" : "bg-body-tertiary";

        return `<div class="mb-3 ${alineacion}" style="max-width: 80%;">
            <div class="TextoSuave small mb-1">
                ${escaparHtml(m.mio ? "Usted" : m.remitente)} &middot; ${escaparHtml(formatoFechaHora(m.fechaEnvio))}
            </div>
            <div class="rounded-3 p-2 px-3 ${fondo}" style="white-space: pre-wrap;">${escaparHtml(m.cuerpo)}</div>
        </div>`;
    }

    async function abrirHilo(idHilo) {
        const panel = $("PanelConversacion");
        const alerta = $("AlertaMensajeria");

        ocultarAlerta(alerta);
        panel.innerHTML = '<p class="TextoSuave mb-0">Cargando…</p>';

        try {
            const respuesta = await fetch(`${RUTAS.hilo}?idHilo=${idHilo}`);

            if (!respuesta.ok) {
                panel.innerHTML = "";
                mostrarAlerta(alerta, await leerMensaje(respuesta, "No se pudo abrir la conversación."));
                return;
            }

            const mensajes = await respuesta.json();

            idHiloAbierto = idHilo;

            const hilo = hilos.find(h => h.idHilo === idHilo);

            $("EncabezadoHilo").classList.remove("d-none");
            $("TituloHilo").textContent = hilo?.asunto || mensajes[0]?.asunto || "";
            $("ContraparteHilo").textContent = hilo?.contraparte || "";

            panel.innerHTML = `<div class="d-flex flex-column">${mensajes.map(burbuja).join("")}</div>`;
            panel.scrollTop = panel.scrollHeight;

            $("FormularioRespuesta").classList.remove("d-none");

            // Abrir marca como leido en el API: la bandeja y la burbuja del
            // menu tienen que reflejarlo sin esperar el refresco del minuto
            await cargarHilos();

            if (window.actualizarContadorMensajes) window.actualizarContadorMensajes();
        } catch {
            panel.innerHTML = "";
            mostrarAlerta(alerta, "No se pudo conectar con el servidor.");
        }
    }

    async function responder(evento) {
        evento.preventDefault();

        if (idHiloAbierto === null) return;

        const campo = $("TextoRespuesta");
        const boton = $("BotonResponder");

        if (!marcar(campo, campo.value.trim().length > 0, "Escriba el mensaje.")) return;

        boton.disabled = true;

        const resultado = await enviarDatos(RUTAS.enviar, {
            IdHilo: idHiloAbierto,
            Cuerpo: campo.value.trim()
        });

        boton.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta($("AlertaMensajeria"), resultado.mensaje);
            return;
        }

        campo.value = "";
        campo.classList.remove("is-invalid");

        await abrirHilo(idHiloAbierto);
    }

    // =====================================================
    // CONVERSACION NUEVA
    // =====================================================

    async function abrirModalNuevo() {
        const selector = $("DestinatarioHilo");
        const alerta = $("AlertaNuevoHilo");

        ocultarAlerta(alerta);
        ["DestinatarioHilo", "AsuntoHilo", "CuerpoHilo"].forEach(id => {
            const campo = $(id);
            if (!campo) return;
            if (id !== "DestinatarioHilo") campo.value = "";
            campo.classList.remove("is-invalid");
        });

        selector.innerHTML = '<option value="">Cargando…</option>';

        bootstrap.Modal.getOrCreateInstance($("ModalNuevoHilo")).show();

        try {
            const respuesta = await fetch(RUTAS.destinatarios);

            if (!respuesta.ok) {
                selector.innerHTML = '<option value="">—</option>';
                mostrarAlerta(alerta, await leerMensaje(respuesta, "No se pudieron cargar los destinatarios."));
                return;
            }

            const destinatarios = await respuesta.json();

            if (destinatarios.length === 0) {
                selector.innerHTML = '<option value="">—</option>';
                mostrarAlerta(alerta,
                    "No hay nadie con cuenta activa a quien escribirle todavía.", "warning");
                return;
            }

            selector.innerHTML = '<option value="">Seleccione a quién le quiere escribir</option>' +
                destinatarios.map(d => {
                    const rol = d.roles ? ` — ${escaparHtml(d.roles)}` : "";
                    return `<option value="${d.idUsuario}">${escaparHtml(d.nombreCompleto)}${rol}</option>`;
                }).join("");
        } catch {
            selector.innerHTML = '<option value="">—</option>';
            mostrarAlerta(alerta, "No se pudo conectar con el servidor.");
        }
    }

    async function crearHilo(evento) {
        evento.preventDefault();

        const destinatario = $("DestinatarioHilo");
        const asunto = $("AsuntoHilo");
        const cuerpo = $("CuerpoHilo");
        const boton = $("BotonCrearHilo");
        const alerta = $("AlertaNuevoHilo");

        ocultarAlerta(alerta);

        let valido = marcar(destinatario, Boolean(destinatario.value), "Seleccione un destinatario.");
        valido = marcar(asunto, asunto.value.trim().length > 0, "Indique el asunto del mensaje.") && valido;
        valido = marcar(cuerpo, cuerpo.value.trim().length > 0, "Escriba el mensaje.") && valido;

        if (!valido) return;

        boton.disabled = true;

        const resultado = await enviarDatos(RUTAS.enviar, {
            IdUsuarioDestinatario: destinatario.value,
            Asunto: asunto.value.trim(),
            Cuerpo: cuerpo.value.trim()
        });

        boton.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(alerta, resultado.mensaje);
            return;
        }

        bootstrap.Modal.getInstance($("ModalNuevoHilo"))?.hide();
        notificar(resultado.datos?.mensaje || "Conversación iniciada.", "success");

        await cargarHilos();

        if (resultado.datos?.idHilo) {
            await abrirHilo(resultado.datos.idHilo);
        }
    }

    // =====================================================
    // ARRANQUE
    // =====================================================

    document.addEventListener("DOMContentLoaded", function () {
        if (!$("ListaHilos")) return;

        $("ListaHilos").addEventListener("click", function (evento) {
            const boton = evento.target.closest("[data-hilo]");
            if (boton) abrirHilo(Number(boton.getAttribute("data-hilo")));
        });

        $("BotonRecargarHilos")?.addEventListener("click", cargarHilos);
        $("FormularioRespuesta")?.addEventListener("submit", responder);
        $("BotonNuevoHilo")?.addEventListener("click", abrirModalNuevo);
        $("FormularioNuevoHilo")?.addEventListener("submit", crearHilo);

        cargarHilos();
    });
})();
