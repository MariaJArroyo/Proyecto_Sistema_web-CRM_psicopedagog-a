// Bitacora de inicios de sesion en el panel:
//   - "Historial de accesos" del menu de tres puntos (Usuarios internos y
//     externos): abre ModalHistorialAccesos con los ultimos intentos de la cuenta.
//   - Pestania "Bitacora de accesos" de Usuarios internos: todos los intentos,
//     con filtros que resuelve el servidor.
// Las dos piden a /Admin/..., que reenvia al API.
(function () {
    "use strict";

    const RUTAS = {
        historial: "/Admin/AccesosUsuario",
        bitacora: "/Admin/BitacoraAccesos"
    };

    // Los mismos textos que guarda TB_ACCESO_USUARIO.Resultado
    const RESULTADOS = {
        Exitoso: { texto: "Ingresó", clase: "badge-pago-pagado", icono: "bi-check-circle" },
        ContrasenaIncorrecta: { texto: "Contraseña incorrecta", clase: "badge-pago-vencido", icono: "bi-x-circle" },
        CorreoNoRegistrado: { texto: "Correo no registrado", clase: "badge-estado-inactivo", icono: "bi-question-circle" },
        CuentaBloqueada: { texto: "Cuenta bloqueada", clase: "badge-pago-vencido", icono: "bi-lock" },
        CuentaInactiva: { texto: "Cuenta desactivada", clase: "badge-estado-inactivo", icono: "bi-slash-circle" },
        PendienteActivacion: { texto: "Sin activar", clase: "badge-pago-pendiente", icono: "bi-hourglass-split" },
        SinRol: { texto: "Sin rol asignado", clase: "badge-pago-pendiente", icono: "bi-exclamation-circle" }
    };

    // ---------- utilidades ----------

    function $(id) {
        return document.getElementById(id);
    }

    function escapar(texto) {
        const div = document.createElement("div");
        div.textContent = texto ?? "";
        return div.innerHTML.replace(/"/g, "&quot;");
    }

    function fechaHora(iso) {
        const f = new Date(iso);
        if (isNaN(f)) return "";
        return f.toLocaleString("es-CR", {
            day: "numeric", month: "short", year: "numeric", hour: "numeric", minute: "2-digit"
        });
    }

    function insigniaResultado(resultado) {
        const r = RESULTADOS[resultado] || { texto: resultado, clase: "badge-estado-inactivo", icono: "bi-circle" };
        return `<span class="badge ${r.clase}"><i class="bi ${r.icono} me-1" aria-hidden="true"></i>${escapar(r.texto)}</span>`;
    }

    function origen(texto) {
        if (texto === "Portal") return '<span class="TextoSuave"><i class="bi bi-house-heart me-1" aria-hidden="true"></i>Portal</span>';
        if (texto === "Panel") return '<span class="TextoSuave"><i class="bi bi-speedometer2 me-1" aria-hidden="true"></i>Panel</span>';
        return '<span class="TextoSuave">—</span>';
    }

    // "Chrome · Windows" a partir del User-Agent. Solo para mostrar: el texto
    // completo queda en el title por si hace falta.
    function dispositivo(ua) {
        if (!ua) return '<span class="TextoSuave">—</span>';

        const navegador =
            /Edg\//.test(ua) ? "Edge" :
            /OPR\/|Opera/.test(ua) ? "Opera" :
            /Firefox\//.test(ua) ? "Firefox" :
            /Chrome\//.test(ua) ? "Chrome" :
            /Safari\//.test(ua) ? "Safari" : "Otro navegador";

        const sistema =
            /Android/.test(ua) ? "Android" :
            /iPhone|iPad|iPod/.test(ua) ? "iOS" :
            /Windows/.test(ua) ? "Windows" :
            /Mac OS X|Macintosh/.test(ua) ? "Mac" :
            /Linux/.test(ua) ? "Linux" : "";

        const movil = /Android|iPhone|iPad|iPod|Mobile/.test(ua);
        const texto = sistema ? `${navegador} · ${sistema}` : navegador;

        return `<span title="${escapar(ua)}"><i class="bi ${movil ? "bi-phone" : "bi-laptop"} me-1 TextoSuave" aria-hidden="true"></i>${escapar(texto)}</span>`;
    }

    function ip(texto) {
        if (!texto) return '<span class="TextoSuave">—</span>';
        if (/^(::1|127\.0\.0\.1|::ffff:127\.0\.0\.1)$/.test(texto)) {
            return '<span class="TextoSuave" title="' + escapar(texto) + '">Este equipo</span>';
        }
        return `<code class="small">${escapar(texto)}</code>`;
    }

    async function pedir(url) {
        try {
            const respuesta = await fetch(url, { headers: { Accept: "application/json" } });
            let cuerpo = null;
            try { cuerpo = await respuesta.json(); } catch { /* sin cuerpo */ }

            if (!respuesta.ok) {
                return { ok: false, mensaje: cuerpo?.mensaje || "No se pudo cargar la bitácora de accesos." };
            }

            return { ok: true, datos: cuerpo || [] };
        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }

    function chip(clase, icono, texto) {
        return `<span class="badge ${clase}"><i class="bi ${icono} me-1" aria-hidden="true"></i>${escapar(texto)}</span>`;
    }

    // ---------- historial de una cuenta ----------

    const modalElemento = $("ModalHistorialAccesos");

    if (modalElemento) {
        const cuerpo = $("CuerpoHistorialAccesos");
        const resumen = $("ResumenHistorialAccesos");
        const alerta = $("AlertaHistorialAccesos");

        document.addEventListener("click", async function (evento) {
            const boton = evento.target.closest("[data-historial-accesos]");
            if (!boton) return;

            const id = Number(boton.getAttribute("data-historial-accesos"));
            if (!id) return;

            $("SubtituloHistorialAccesos").textContent = boton.getAttribute("data-nombre") || "";
            alerta.classList.add("d-none");
            resumen.innerHTML = "";
            cuerpo.innerHTML = '<tr><td colspan="5" class="TextoSuave text-center py-4">Cargando…</td></tr>';
            bootstrap.Modal.getOrCreateInstance(modalElemento).show();

            const resultado = await pedir(`${RUTAS.historial}?id=${id}`);

            if (!resultado.ok) {
                cuerpo.innerHTML = "";
                alerta.textContent = resultado.mensaje;
                alerta.classList.remove("d-none");
                return;
            }

            const accesos = resultado.datos;

            if (accesos.length === 0) {
                cuerpo.innerHTML = FilaVaciaTabla(5, "bi-shield-lock", "Sin inicios de sesión",
                    "Esta cuenta no ha intentado entrar en los últimos 90 días.");
                return;
            }

            const ultimoExitoso = accesos.find(a => a.resultado === "Exitoso");
            const fallidos = accesos.filter(a => a.resultado !== "Exitoso").length;

            resumen.innerHTML =
                (ultimoExitoso
                    ? chip("badge-pago-pagado", "bi-box-arrow-in-right", `Último ingreso: ${fechaHora(ultimoExitoso.fechaHora)}`)
                    : chip("badge-estado-inactivo", "bi-box-arrow-in-right", "Sin ingresos exitosos"))
                + (fallidos > 0
                    ? chip("badge-pago-vencido", "bi-x-circle", `${fallidos} intento${fallidos === 1 ? "" : "s"} fallido${fallidos === 1 ? "" : "s"}`)
                    : "");

            cuerpo.innerHTML = accesos.map(a => `
                <tr>
                    <td class="text-nowrap">${escapar(fechaHora(a.fechaHora))}</td>
                    <td>${insigniaResultado(a.resultado)}</td>
                    <td class="text-nowrap">${origen(a.origen)}</td>
                    <td class="text-nowrap">${dispositivo(a.navegador)}</td>
                    <td class="text-nowrap">${ip(a.ip)}</td>
                </tr>`).join("");
        });
    }

    // ---------- bitacora completa ----------

    const panel = $("PanelBitacoraAccesos");

    if (panel) {
        const formulario = $("FiltrosBitacoraAccesos");
        const tabla = $("TablaBitacoraAccesos");
        const cuerpo = tabla.querySelector("tbody");
        const resumen = $("ResumenBitacoraAccesos");
        const alerta = $("AlertaBitacoraAccesos");
        let cargada = false;

        // Por defecto, los ultimos 7 dias
        const hoy = new Date();
        const haceSiete = new Date(hoy.getTime() - 6 * 24 * 60 * 60 * 1000);
        const iso = d => `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
        formulario.elements.desde.value = iso(haceSiete);
        formulario.elements.hasta.value = iso(hoy);

        async function cargar() {
            cargada = true;
            alerta.classList.add("d-none");
            cuerpo.innerHTML = '<tr><td colspan="7" class="TextoSuave text-center py-4">Cargando…</td></tr>';

            const parametros = new URLSearchParams();
            ["tipo", "resultado", "desde", "hasta", "texto"].forEach(nombre => {
                const valor = (formulario.elements[nombre]?.value || "").trim();
                if (valor) parametros.set(nombre, valor);
            });

            const resultado = await pedir(`${RUTAS.bitacora}?${parametros}`);

            if (!resultado.ok) {
                cuerpo.innerHTML = "";
                resumen.innerHTML = "";
                alerta.textContent = resultado.mensaje;
                alerta.classList.remove("d-none");
                tabla.dispatchEvent(new Event("tabla:actualizada"));
                return;
            }

            const accesos = resultado.datos;
            const exitosos = accesos.filter(a => a.resultado === "Exitoso").length;
            const fallidos = accesos.length - exitosos;
            const desconocidos = accesos.filter(a => a.resultado === "CorreoNoRegistrado").length;

            resumen.innerHTML = [
                chip("badge-estado-cita-agendada", "bi-list-ul", `${accesos.length} intento${accesos.length === 1 ? "" : "s"}${accesos.length >= 500 ? " (los 500 más recientes)" : ""}`),
                chip("badge-pago-pagado", "bi-check-circle", `${exitosos} exitoso${exitosos === 1 ? "" : "s"}`),
                chip("badge-pago-vencido", "bi-x-circle", `${fallidos} fallido${fallidos === 1 ? "" : "s"}`),
                desconocidos > 0 ? chip("badge-estado-inactivo", "bi-question-circle", `${desconocidos} con correo no registrado`) : ""
            ].join("");

            cuerpo.innerHTML = accesos.length === 0
                ? FilaVaciaTabla(7, "bi-shield-lock", "Sin inicios de sesión",
                    "No hay intentos con estos filtros. Pruebe con otras fechas o quite filtros.")
                : accesos.map(a => {
                    const tipo = a.tipo === "Externo" ? "Encargado" : a.tipo === "Interno" ? (a.rol || "Personal") : "—";
                    const nombre = a.usuario
                        ? `<div class="fw-semibold">${escapar(a.usuario)}</div>`
                        : '<div class="fw-semibold TextoSuave">Desconocido</div>';
                    const historial = a.idUsuario
                        ? `<button type="button" class="btn btn-sm btn-outline-secondary border-0" data-historial-accesos="${a.idUsuario}"
                                   data-nombre="${escapar(a.usuario || a.correo)}" title="Ver historial de esta cuenta" aria-label="Ver historial de esta cuenta">
                               <i class="bi bi-eye"></i></button>`
                        : "";

                    return `<tr class="${a.resultado === "Exitoso" ? "" : "FilaAccesoFallido"}">
                        <td class="text-nowrap">${escapar(fechaHora(a.fechaHora))}</td>
                        <td>${nombre}<small class="TextoSuave">${escapar(a.correo)}</small></td>
                        <td class="text-nowrap">${escapar(tipo)}</td>
                        <td>${insigniaResultado(a.resultado)}</td>
                        <td class="text-nowrap">${dispositivo(a.navegador)}</td>
                        <td class="text-nowrap">${ip(a.ip)}</td>
                        <td class="TablaAcciones text-end">${historial}</td>
                    </tr>`;
                }).join("");

            tabla.dispatchEvent(new Event("tabla:actualizada"));
        }

        formulario.addEventListener("submit", function (evento) {
            evento.preventDefault();

            const desde = formulario.elements.desde.value;
            const hasta = formulario.elements.hasta.value;

            if (desde && hasta && desde > hasta) {
                alerta.textContent = "La fecha inicial no puede ser posterior a la final.";
                alerta.classList.remove("d-none");
                return;
            }

            cargar();
        });

        formulario.addEventListener("reset", function () {
            // Despues del reset del navegador se vuelven a poner los 7 dias
            setTimeout(() => {
                formulario.elements.desde.value = iso(haceSiete);
                formulario.elements.hasta.value = iso(hoy);
                cargar();
            });
        });

        // Se carga la primera vez que se abre la pestania, no al entrar a Usuarios
        document.querySelectorAll('[data-bs-target="#PanelBitacoraAccesos"]').forEach(boton => {
            boton.addEventListener("shown.bs.tab", () => { if (!cargada) cargar(); });
        });

        if (panel.classList.contains("active")) cargar();
    }
})();
