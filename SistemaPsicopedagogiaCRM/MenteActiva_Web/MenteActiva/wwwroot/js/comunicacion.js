// Comunicacion y notificaciones (M10).
//
// Conectadas: Correo (HU-M10-1) y Plantillas (HU-M10-5). Recordatorios y
// Mensajeria muestran su aviso de pendiente y no tienen ningun boton que finja
// guardar: eso era lo que hacia la maqueta y es justo lo que no se vale.
//
// El selector de cita es lo que permite que [Fecha], [Hora] y [Motivo] se
// resuelvan en un correo escrito a mano. Antes esos tres solo existian en los
// avisos automaticos, pero el selector de plantillas ofrecia igual las
// plantillas de cita, que los traen: elegir una dejaba en el textarea un texto
// que el envio despues rechazaba.
//
// Todo pasa por /Admin/..., que reenvia al API: el navegador nunca habla con el
// API directamente.

(function () {
    "use strict";

    const RUTAS = {
        plantillas: "/Admin/ConsultarPlantillas",
        plantilla: "/Admin/DetallePlantilla",
        crearPlantilla: "/Admin/CrearPlantilla",
        editarPlantilla: "/Admin/EditarPlantilla",
        estadoPlantilla: "/Admin/CambiarEstadoPlantilla",
        notificaciones: "/Admin/ConsultarNotificaciones",
        citasCorreo: "/Admin/CitasCorreo",
        vistaPreviaCorreo: "/Admin/VistaPreviaCorreo",
        enviarCorreo: "/Admin/EnviarCorreo"
    };

    const VISTAS = ["Correo", "Recordatorios", "Mensajeria", "Plantillas"];

    const puedeAdministrar = window.COMUNICACION_PUEDE_ADMINISTRAR === true;
    const destinatarios = window.COMUNICACION_DESTINATARIOS || [];
    const plantillasCatalogo = window.COMUNICACION_PLANTILLAS || [];
    const marcadoresPermitidos = (window.COMUNICACION_MARCADORES || [])
        .map(m => String(m).replace(/^\[|\]$/g, "").toLowerCase());

    // Los que salen de una cita. Antes no se podian resolver aqui y el aviso
    // decia que se quitaran del texto; ahora se resuelven si se elige la cita,
    // asi que lo que avisa es que falta elegirla. [Motivo] es el raro: en
    // TB_CITA solo existe el motivo de cancelacion, asi que solo sale si la
    // cita elegida esta cancelada.
    const DE_CITA = ["fecha", "hora", "motivo"];

    let plantillas = [];
    let idPlantillaEditando = null;
    let estadoPendiente = null;

    // Las citas del estudiante que esta elegido en el formulario
    let citas = [];

    // Lo que ya se trajo de los dos listados, para abrir el detalle sin volver a
    // consultar. Es un mapa y no un arreglo porque las dos pestanias cargan por
    // separado y el id de notificacion es unico entre las dos.
    const notificacionesVistas = new Map();

    // =====================================================
    // UTILIDADES
    // =====================================================

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
            if (valor !== null && valor !== undefined) datos.append(clave, valor);
        });
        return datos;
    }

    function modal(id) {
        return bootstrap.Modal.getOrCreateInstance($(id));
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

    async function enviar(url, datos) {
        try {
            const respuesta = await fetch(url, { method: "POST", body: datos });
            if (respuesta.ok) return { ok: true };
            return { ok: false, mensaje: await leerMensaje(respuesta, "No se pudo completar la operación.") };
        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }

    // Para los endpoints que devuelven cuerpo y no solo un codigo
    async function enviarConRespuesta(url, datos) {
        try {
            const respuesta = await fetch(url, { method: "POST", body: datos });

            if (!respuesta.ok) {
                return { ok: false, mensaje: await leerMensaje(respuesta, "No se pudo completar la operación.") };
            }

            return { ok: true, datos: await respuesta.json() };
        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }

    function recortar(texto, largo) {
        const limpio = String(texto ?? "").replace(/\s+/g, " ").trim();
        if (limpio.length <= largo) return limpio;
        return limpio.slice(0, largo - 1) + "…";
    }

    function formatoFechaHora(iso) {
        if (!iso) return "";
        const f = new Date(iso);
        if (isNaN(f)) return "";
        return f.toLocaleDateString("es-CR", { day: "2-digit", month: "short", year: "numeric" }) +
            ", " + f.toLocaleTimeString("es-CR", { hour: "2-digit", minute: "2-digit" });
    }

    // =====================================================
    // MARCADORES
    // =====================================================

    function marcadoresDe(texto) {
        const encontrados = [];
        const vistos = new Set();
        const patron = /\[([^[\]]*)\]/g;
        let coincidencia;

        while ((coincidencia = patron.exec(String(texto ?? ""))) !== null) {
            const nombre = coincidencia[1].trim();
            const clave = nombre.toLowerCase();

            if (!vistos.has(clave)) {
                vistos.add(clave);
                encontrados.push(nombre);
            }
        }

        return encontrados;
    }

    function esPermitido(nombre) {
        return marcadoresPermitidos.includes(String(nombre).toLowerCase());
    }

    function pastilla(nombre, bien, titulo) {
        const clase = bien ? "text-success" : "text-danger fw-semibold";
        const icono = bien ? "bi-check-circle" : "bi-exclamation-triangle";
        const tip = titulo ? ` title="${escaparHtml(titulo)}"` : "";
        return `<span class="me-3 ${clase}"${tip}><i class="bi ${icono} me-1"></i>[${escaparHtml(nombre)}]</span>`;
    }

    // =====================================================
    // CORREO
    // =====================================================

    function destinatarioElegido() {
        const id = Number($("DestinatarioCorreo")?.value || 0);
        return destinatarios.find(d => d.idEncargado === id) || null;
    }

    function llenarEstudiantes() {
        const selector = $("EstudianteCorreo");
        const ayuda = $("AyudaEstudianteCorreo");
        if (!selector) return;

        const destinatario = destinatarioElegido();

        selector.innerHTML = '<option value="">Sin estudiante</option>';

        if (!destinatario) {
            selector.disabled = true;
            if (ayuda) ayuda.textContent = "Primero elija un destinatario.";
            cargarCitas();
            return;
        }

        if (destinatario.estudiantes.length === 0) {
            selector.disabled = true;
            if (ayuda) {
                ayuda.textContent = "Este encargado no tiene estudiantes activos a cargo, "
                    + "así que no se puede usar [Estudiante].";
            }
            cargarCitas();
            return;
        }

        destinatario.estudiantes.forEach(e => {
            const opcion = document.createElement("option");
            opcion.value = String(e.idEstudiante);
            opcion.textContent = e.estudiante;
            selector.appendChild(opcion);
        });

        selector.disabled = false;

        // Con un solo estudiante se elige solo: es lo que va a querer siempre
        if (destinatario.estudiantes.length === 1) {
            selector.value = String(destinatario.estudiantes[0].idEstudiante);
        }

        if (ayuda) ayuda.textContent = "Solo hace falta si el texto usa [Estudiante].";

        // Las citas cuelgan del estudiante, asi que se recargan con el
        cargarCitas();
    }

    // =====================================================
    // CITAS DEL FORMULARIO
    // =====================================================

    function citaElegida() {
        const id = Number($("CitaCorreo")?.value || 0);
        return citas.find(c => c.idCita === id) || null;
    }

    // Trae las citas del estudiante elegido y rellena el selector. Se llama sola
    // cada vez que cambia el estudiante, y deja el selector apagado cuando no
    // hay de donde sacar citas: asi el marcador avisa que falta elegir una en
    // vez de dejar que el envio se caiga en el servidor.
    async function cargarCitas() {
        const selector = $("CitaCorreo");
        const ayuda = $("AyudaCitaCorreo");
        if (!selector) return;

        const idEstudiante = Number($("EstudianteCorreo")?.value || 0);

        citas = [];
        selector.innerHTML = '<option value="">Sin cita</option>';
        selector.disabled = true;

        if (!idEstudiante) {
            if (ayuda) {
                ayuda.textContent = "Elija un estudiante para poder elegir una de sus citas.";
            }
            pintarMarcadoresCorreo();
            return;
        }

        if (ayuda) ayuda.textContent = "Buscando las citas del estudiante…";

        try {
            const respuesta = await fetch(`${RUTAS.citasCorreo}?idEstudiante=${idEstudiante}`);

            if (!respuesta.ok) {
                if (ayuda) ayuda.textContent = "No se pudieron cargar las citas del estudiante.";
                pintarMarcadoresCorreo();
                return;
            }

            citas = await respuesta.json();
        } catch {
            if (ayuda) ayuda.textContent = "No se pudo conectar para traer las citas.";
            pintarMarcadoresCorreo();
            return;
        }

        if (citas.length === 0) {
            if (ayuda) {
                ayuda.textContent = "Este estudiante no tiene citas de los últimos 90 días "
                    + "ni por delante, así que [Fecha], [Hora] y [Motivo] no se pueden resolver.";
            }
            pintarMarcadoresCorreo();
            return;
        }

        citas.forEach(cita => {
            const opcion = document.createElement("option");
            opcion.value = String(cita.idCita);
            opcion.textContent = `${formatoFechaHora(cita.fechaHoraInicio)} — `
                + `${cita.tipoSesion} (${cita.estado})`;
            selector.appendChild(opcion);
        });

        selector.disabled = false;

        if (ayuda) {
            ayuda.textContent = "Al elegirla, [Fecha] y [Hora] salen de ella. "
                + "[Motivo] solo sale si la cita está cancelada.";
        }

        pintarMarcadoresCorreo();
    }

    function aplicarPlantilla() {
        const id = Number($("PlantillaCorreo")?.value || 0);
        if (!id) return;

        const plantilla = plantillasCatalogo.find(p => p.idPlantilla === id);
        if (!plantilla) return;

        $("AsuntoCorreo").value = plantilla.asunto || "";
        $("CuerpoCorreo").value = plantilla.cuerpo || "";

        actualizarContadorCorreo();
        pintarMarcadoresCorreo();
    }

    function actualizarContadorCorreo() {
        const contador = $("ContadorCuerpoCorreo");
        if (contador) contador.textContent = String(($("CuerpoCorreo")?.value || "").length);
    }

    function pintarMarcadoresCorreo() {
        const destino = $("MarcadoresCorreo");
        if (!destino) return;

        const texto = ($("AsuntoCorreo")?.value || "") + " " + ($("CuerpoCorreo")?.value || "");
        const encontrados = marcadoresDe(texto);

        if (encontrados.length === 0) {
            destino.innerHTML = '<span class="TextoSuave">Al enviar se reemplazan por los datos '
                + 'reales. Revise la vista previa antes de mandar.</span>';
            return;
        }

        const hayEstudiante = Boolean($("EstudianteCorreo")?.value);
        const cita = citaElegida();

        destino.innerHTML = encontrados.map(nombre => {
            const clave = nombre.toLowerCase();

            if (!esPermitido(nombre)) {
                return pastilla(nombre, false, "Este marcador no existe.");
            }

            if (clave === "estudiante" && !hayEstudiante) {
                return pastilla(nombre, false, "Elija un estudiante para que se pueda resolver.");
            }

            if (DE_CITA.includes(clave)) {
                if (!cita) {
                    return pastilla(nombre, false, "Elija la cita de la que habla el correo.");
                }

                if (clave === "motivo" && !cita.motivoCancelacion) {
                    return pastilla(nombre, false,
                        "La cita elegida no está cancelada, así que no tiene motivo.");
                }
            }

            return pastilla(nombre, true, "Se va a reemplazar al enviar.");
        }).join("");
    }

    function datosCorreo() {
        return {
            IdEncargado: Number($("DestinatarioCorreo").value || 0),
            IdEstudiante: $("EstudianteCorreo").value || null,
            IdCita: $("CitaCorreo")?.value || null,
            Asunto: $("AsuntoCorreo").value.trim(),
            Cuerpo: $("CuerpoCorreo").value.trim()
        };
    }

    function validarCorreo() {
        const destinatario = $("DestinatarioCorreo");
        const asunto = $("AsuntoCorreo");
        const cuerpo = $("CuerpoCorreo");

        let valido = marcar(destinatario, Boolean(destinatario.value), "Seleccione un destinatario.");
        valido = marcar(asunto, asunto.value.trim().length > 0, "Indique el asunto del correo.") && valido;
        valido = marcar(cuerpo, cuerpo.value.trim().length > 0, "Indique el mensaje del correo.") && valido;

        return valido;
    }

    async function verVistaPrevia() {
        const alerta = $("AlertaCorreo");
        ocultarAlerta(alerta);

        if (!validarCorreo()) return;

        const boton = $("BotonVistaPreviaCorreo");
        boton.disabled = true;

        const resultado = await enviarConRespuesta(
            RUTAS.vistaPreviaCorreo, datosConToken(datosCorreo()));

        boton.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(alerta, resultado.mensaje);
            return;
        }

        const vista = resultado.datos;
        const destinatario = destinatarioElegido();

        $("VistaPreviaPara").textContent = destinatario
            ? `${destinatario.encargado} — ${destinatario.correo}`
            : "";
        $("VistaPreviaAsunto").textContent = vista.asunto || "";
        $("VistaPreviaCuerpo").textContent = vista.cuerpo || "";

        const alertaVista = $("AlertaVistaPreviaCorreo");
        const botonEnviar = $("BotonEnviarDesdeVistaPrevia");

        if (vista.sePuedeEnviar) {
            ocultarAlerta(alertaVista);
            botonEnviar.disabled = false;
        } else {
            mostrarAlerta(alertaVista, vista.advertencia, "warning");
            botonEnviar.disabled = true;
        }

        modal("ModalVistaPreviaCorreo").show();
    }

    async function enviarCorreo(evento) {
        if (evento) evento.preventDefault();

        const alerta = $("AlertaCorreo");
        ocultarAlerta(alerta);

        if (!validarCorreo()) return;

        const botones = [$("BotonEnviarCorreo"), $("BotonEnviarDesdeVistaPrevia")].filter(Boolean);
        botones.forEach(b => b.disabled = true);

        const resultado = await enviarConRespuesta(RUTAS.enviarCorreo, datosConToken(datosCorreo()));

        botones.forEach(b => b.disabled = false);

        if (!resultado.ok) {
            // El modal de vista previa se cierra para que el motivo se lea en
            // el formulario, que es donde se corrige
            bootstrap.Modal.getInstance($("ModalVistaPreviaCorreo"))?.hide();
            mostrarAlerta(alerta, resultado.mensaje);
            notificar("El correo no se envió.", "danger");
            return;
        }

        bootstrap.Modal.getInstance($("ModalVistaPreviaCorreo"))?.hide();

        notificar(resultado.datos?.mensaje || "Correo enviado.", "success");

        $("FormularioCorreo").reset();
        llenarEstudiantes();
        actualizarContadorCorreo();
        pintarMarcadoresCorreo();
        ["DestinatarioCorreo", "AsuntoCorreo", "CuerpoCorreo"].forEach(
            id => $(id).classList.remove("is-invalid"));

        await cargarHistorialCorreo();
    }

    function filaHistorial(n) {
        const estado = n.enviada
            ? '<span class="badge badge-cita-completada">Enviado</span>'
            : (n.intentos > 0
                ? '<span class="badge text-bg-danger">Falló</span>'
                : '<span class="badge badge-cita-programada">Pendiente</span>');

        const error = n.mensajeError
            ? `<div class="small text-danger mt-1">${escaparHtml(recortar(n.mensajeError, 120))}</div>`
            : "";

        const cuando = formatoFechaHora(n.fechaEnvio || n.fechaProgramada);

        // Boton y no div: el texto completo del correo se lee en el modal, y asi
        // se llega con el teclado igual que con el mouse
        return `<button type="button" class="list-group-item list-group-item-action px-0 text-start"
                        data-notificacion="${n.idNotificacion}">
            <div class="d-flex justify-content-between align-items-start gap-2">
                <div class="me-2">
                    <div class="fw-semibold">${escaparHtml(n.asunto || "(sin asunto)")}</div>
                    <div class="TextoSuave small">
                        ${escaparHtml(n.destinatario || "")} &middot; ${escaparHtml(n.correoDestino || "")}
                    </div>
                    <div class="TextoSuave small">
                        ${escaparHtml(cuando)}${n.remitente ? " &middot; " + escaparHtml(n.remitente) : ""}
                    </div>
                    ${error}
                </div>
                <div class="text-nowrap">${estado}</div>
            </div>
        </button>`;
    }

    // El detalle sale de lo que ya se trajo: el cuerpo viene en el mismo listado
    // y no hace falta otra consulta para leerlo.
    function abrirDetalleNotificacion(id) {
        const n = notificacionesVistas.get(id);
        if (!n) return;

        const esAutomatico = Boolean(n.clavePlantilla);

        $("TituloModalDetalleCorreo").textContent = esAutomatico
            ? (TIPOS_AVISO[n.clavePlantilla] || "Aviso automático")
            : "Correo enviado";

        $("DetalleCorreoPara").textContent =
            [n.destinatario, n.correoDestino].filter(Boolean).join(" — ") || "—";

        const cuando = n.enviada
            ? "Salió el " + formatoFechaHora(n.fechaEnvio)
            : "Programado para el " + formatoFechaHora(n.fechaProgramada);

        $("DetalleCorreoEstado").innerHTML = estadoAviso(n)
            + ` <span class="TextoSuave small">${escaparHtml(cuando)}</span>`;

        $("EtiquetaDetalleCorreoQuien").textContent = esAutomatico ? "Origen" : "Enviado por";
        $("DetalleCorreoQuien").textContent = esAutomatico
            ? "Aviso automático del sistema"
            : (n.remitente || "—");

        $("DetalleCorreoAsunto").textContent = n.asunto || "(sin asunto)";
        $("DetalleCorreoCuerpo").textContent = n.cuerpo || "";

        const error = $("DetalleCorreoError");

        if (n.mensajeError) {
            error.classList.remove("d-none");
            error.textContent = n.intentos > 0
                ? `${n.mensajeError} (${n.intentos} intento${n.intentos === 1 ? "" : "s"})`
                : n.mensajeError;
        } else {
            error.classList.add("d-none");
            error.textContent = "";
        }

        modal("ModalDetalleCorreo").show();
    }

    async function cargarHistorialCorreo() {
        const destino = $("HistorialCorreo");
        const alerta = $("AlertaHistorialCorreo");
        if (!destino) return;

        ocultarAlerta(alerta);
        destino.innerHTML = '<div class="list-group-item px-0 TextoSuave">Cargando…</div>';

        try {
            const respuesta = await fetch(`${RUTAS.notificaciones}?soloManuales=true&limite=15`);

            if (!respuesta.ok) {
                destino.innerHTML = "";
                mostrarAlerta(alerta, await leerMensaje(respuesta, "No se pudo cargar el historial."));
                return;
            }

            const filas = await respuesta.json();

            filas.forEach(n => notificacionesVistas.set(n.idNotificacion, n));

            destino.innerHTML = filas.length === 0
                ? '<div class="list-group-item px-0 TextoSuave">Todavía no se ha enviado ningún correo.</div>'
                : filas.map(filaHistorial).join("");
        } catch {
            destino.innerHTML = "";
            mostrarAlerta(alerta, "No se pudo conectar con el servidor.");
        }
    }

    // =====================================================
    // RECORDATORIOS Y AVISOS AUTOMATICOS
    // =====================================================

    // El nombre de la clave es para el codigo; esto es lo que lee la usuaria
    const TIPOS_AVISO = {
        RecordatorioCita: "Recordatorio",
        ConfirmacionCita: "Confirmación",
        ActualizacionCita: "Cambio de fecha",
        CancelacionCita: "Cancelación"
    };

    function estadoAviso(n) {
        if (n.enviada) return '<span class="badge badge-cita-completada">Enviado</span>';
        if (n.intentos > 0) return '<span class="badge text-bg-danger">Falló</span>';
        return '<span class="badge badge-cita-programada">Programado</span>';
    }

    function filaAviso(n) {
        const tipo = TIPOS_AVISO[n.clavePlantilla] || n.clavePlantilla || "Aviso";

        const error = n.mensajeError
            ? `<div class="small text-danger mt-1">${escaparHtml(recortar(n.mensajeError, 110))}
               ${n.intentos > 0 ? ` (${n.intentos} intento${n.intentos === 1 ? "" : "s"})` : ""}</div>`
            : "";

        // Enviado muestra cuando salio; lo demas, cuando le toca salir
        const cuando = n.enviada
            ? formatoFechaHora(n.fechaEnvio)
            : formatoFechaHora(n.fechaProgramada);

        return `<tr role="button" style="cursor: pointer;" data-notificacion="${n.idNotificacion}">
            <td><span class="fw-semibold">${escaparHtml(tipo)}</span></td>
            <td>
                <div>${escaparHtml(n.destinatario || "—")}</div>
                <div class="TextoSuave small">${escaparHtml(n.correoDestino || "")}</div>
            </td>
            <td class="TextoSuave">${escaparHtml(recortar(n.cuerpo, 70))}${error}</td>
            <td class="TextoSuave text-nowrap">${escaparHtml(cuando)}</td>
            <td>${estadoAviso(n)}</td>
        </tr>`;
    }

    async function cargarRecordatorios() {
        const cuerpo = $("CuerpoTablaRecordatorios");
        const alerta = $("AlertaRecordatorios");
        if (!cuerpo) return;

        ocultarAlerta(alerta);
        cuerpo.innerHTML = '<tr><td colspan="5" class="TextoSuave text-center py-4">Cargando…</td></tr>';

        try {
            const respuesta = await fetch(`${RUTAS.notificaciones}?soloManuales=false&limite=100`);

            if (!respuesta.ok) {
                cuerpo.innerHTML = "";
                mostrarAlerta(alerta, await leerMensaje(respuesta, "No se pudieron cargar los avisos."));
                return;
            }

            // El endpoint trae todo; aqui se dejan solo los automaticos, que son
            // los que tienen plantilla. El correo escrito a mano va en su propia
            // pestania y no hacia falta un tercer parametro en el procedimiento.
            const filas = (await respuesta.json()).filter(n => n.clavePlantilla);

            filas.forEach(n => notificacionesVistas.set(n.idNotificacion, n));

            cuerpo.innerHTML = filas.length === 0
                ? '<tr><td colspan="5" class="TextoSuave text-center py-4">Todavía no hay avisos automáticos.</td></tr>'
                : filas.map(filaAviso).join("");
        } catch {
            cuerpo.innerHTML = "";
            mostrarAlerta(alerta, "No se pudo conectar con el servidor.");
        }
    }

    // =====================================================
    // PLANTILLAS
    // =====================================================

    function filaVacia(mensaje) {
        return `<tr><td colspan="5" class="TextoSuave text-center py-4">${escaparHtml(mensaje)}</td></tr>`;
    }

    function botonesDe(plantilla) {
        if (!puedeAdministrar) return "";

        const editar = `<button type="button" class="btn btn-sm btn-outline-secondary"
            data-accion="editar" data-id="${plantilla.idPlantilla}" title="Editar">
            <i class="bi bi-pencil" aria-hidden="true"></i><span class="visually-hidden">Editar</span></button>`;

        // La del sistema no se apaga: en vez de un boton que va a dar error, se
        // muestra deshabilitado con el motivo en el tooltip
        if (plantilla.esSistema) {
            return editar + ` <button type="button" class="btn btn-sm btn-outline-secondary" disabled
                title="La usa el sistema y no se puede desactivar">
                <i class="bi bi-slash-circle" aria-hidden="true"></i>
                <span class="visually-hidden">No se puede desactivar</span></button>`;
        }

        const icono = plantilla.activa ? "bi-slash-circle" : "bi-check-circle";
        const etiqueta = plantilla.activa ? "Desactivar" : "Activar";

        return editar + ` <button type="button" class="btn btn-sm btn-outline-secondary"
            data-accion="estado" data-id="${plantilla.idPlantilla}" title="${etiqueta}">
            <i class="bi ${icono}" aria-hidden="true"></i>
            <span class="visually-hidden">${etiqueta}</span></button>`;
    }

    function pintarTabla() {
        const cuerpo = $("CuerpoTablaPlantillas");
        const conteo = $("ConteoPlantillas");
        if (!cuerpo) return;

        if (plantillas.length === 0) {
            cuerpo.innerHTML = filaVacia("No hay plantillas registradas.");
            if (conteo) conteo.textContent = "";
            return;
        }

        cuerpo.innerHTML = plantillas.map(p => {
            const sistema = p.esSistema
                ? ' <span class="badge text-bg-light border" title="La usa el sistema">Sistema</span>'
                : "";
            const estado = p.activa
                ? '<span class="badge badge-cita-completada">Activa</span>'
                : '<span class="badge text-bg-secondary">Inactiva</span>';

            return `<tr>
                <td><span class="fw-semibold">${escaparHtml(p.nombre)}</span>${sistema}</td>
                <td class="TextoSuave">${escaparHtml(p.asunto || "—")}</td>
                <td class="TextoSuave">${escaparHtml(recortar(p.cuerpo, 70))}</td>
                <td>${estado}</td>
                <td class="text-end text-nowrap">${botonesDe(p)}</td>
            </tr>`;
        }).join("");

        if (conteo) {
            conteo.textContent = plantillas.length === 1
                ? "1 plantilla"
                : plantillas.length + " plantillas";
        }
    }

    async function cargarPlantillas() {
        const alerta = $("AlertaPlantillas");
        const cuerpo = $("CuerpoTablaPlantillas");
        if (!cuerpo) return;

        const soloActivas = $("SoloActivasPlantillas")?.checked ? "true" : "false";

        ocultarAlerta(alerta);
        cuerpo.innerHTML = filaVacia("Cargando…");

        try {
            const respuesta = await fetch(`${RUTAS.plantillas}?soloActivas=${soloActivas}`);

            if (!respuesta.ok) {
                plantillas = [];
                cuerpo.innerHTML = filaVacia("No se pudieron cargar las plantillas.");
                mostrarAlerta(alerta, await leerMensaje(respuesta, "No se pudieron cargar las plantillas."));
                return;
            }

            plantillas = await respuesta.json();
            pintarTabla();
        } catch {
            plantillas = [];
            cuerpo.innerHTML = filaVacia("No se pudieron cargar las plantillas.");
            mostrarAlerta(alerta, "No se pudo conectar con el servidor.");
        }
    }

    function limpiarFormularioPlantilla() {
        ["NombrePlantilla", "AsuntoPlantilla", "CuerpoPlantilla"].forEach(id => {
            const campo = $(id);
            if (!campo) return;
            campo.value = "";
            campo.classList.remove("is-invalid");
        });

        ocultarAlerta($("AlertaFormularioPlantilla"));
        actualizarContadorPlantilla();
        pintarMarcadoresDetectados();
    }

    function actualizarContadorPlantilla() {
        const contador = $("ContadorCuerpoPlantilla");
        if (contador) contador.textContent = String(($("CuerpoPlantilla")?.value || "").length);
    }

    function pintarMarcadoresDetectados() {
        const destino = $("MarcadoresDetectados");
        if (!destino) return;

        const texto = ($("AsuntoPlantilla")?.value || "") + " " + ($("CuerpoPlantilla")?.value || "");
        const encontrados = marcadoresDe(texto);

        if (encontrados.length === 0) {
            destino.innerHTML = '<span class="TextoSuave">Ninguno todavía.</span>';
            return;
        }

        destino.innerHTML = encontrados
            .map(nombre => pastilla(nombre, esPermitido(nombre),
                esPermitido(nombre) ? "Se va a reemplazar al enviar." : "Este marcador no existe."))
            .join("");
    }

    function abrirNuevaPlantilla() {
        idPlantillaEditando = null;
        limpiarFormularioPlantilla();

        $("TituloModalPlantilla").textContent = "Nueva plantilla";
        $("AvisoPlantillaSistema").classList.add("d-none");

        modal("ModalPlantilla").show();
    }

    async function abrirEdicionPlantilla(id) {
        idPlantillaEditando = id;
        limpiarFormularioPlantilla();
        $("TituloModalPlantilla").textContent = "Editar plantilla";

        try {
            const respuesta = await fetch(`${RUTAS.plantilla}?id=${id}`);

            if (!respuesta.ok) {
                notificar(await leerMensaje(respuesta, "No se pudo abrir la plantilla."), "danger");
                return;
            }

            const plantilla = await respuesta.json();

            $("NombrePlantilla").value = plantilla.nombre || "";
            $("AsuntoPlantilla").value = plantilla.asunto || "";
            $("CuerpoPlantilla").value = plantilla.cuerpo || "";
            $("AvisoPlantillaSistema").classList.toggle("d-none", !plantilla.esSistema);

            actualizarContadorPlantilla();
            pintarMarcadoresDetectados();

            modal("ModalPlantilla").show();
        } catch {
            notificar("No se pudo conectar con el servidor.", "danger");
        }
    }

    function validarPlantilla() {
        const nombre = $("NombrePlantilla");
        const cuerpo = $("CuerpoPlantilla");
        const asunto = $("AsuntoPlantilla");

        let valido = marcar(nombre, nombre.value.trim().length > 0, "Indique el nombre de la plantilla.");
        valido = marcar(cuerpo, cuerpo.value.trim().length > 0, "Indique el contenido de la plantilla.") && valido;

        // Se avisa aqui para no mandar un viaje al servidor que ya se sabe que
        // falla. El servidor lo valida igual: esto es comodidad, no la regla.
        const malos = marcadoresDe(asunto.value + " " + cuerpo.value).filter(m => !esPermitido(m));

        if (malos.length > 0) {
            mostrarAlerta(
                $("AlertaFormularioPlantilla"),
                "Estos marcadores no existen: " + malos.map(m => "[" + m + "]").join(", ") + ".");
            valido = false;
        }

        return valido;
    }

    async function guardarPlantilla(evento) {
        evento.preventDefault();

        const alerta = $("AlertaFormularioPlantilla");
        const boton = $("BotonGuardarPlantilla");

        ocultarAlerta(alerta);

        if (!validarPlantilla()) return;

        const campos = {
            Nombre: $("NombrePlantilla").value.trim(),
            Asunto: $("AsuntoPlantilla").value.trim(),
            Cuerpo: $("CuerpoPlantilla").value.trim()
        };

        const url = idPlantillaEditando === null
            ? RUTAS.crearPlantilla
            : `${RUTAS.editarPlantilla}?id=${idPlantillaEditando}`;

        boton.disabled = true;

        const resultado = await enviar(url, datosConToken(campos));

        boton.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(alerta, resultado.mensaje);
            return;
        }

        modal("ModalPlantilla").hide();
        notificar(idPlantillaEditando === null
            ? "Plantilla creada correctamente."
            : "Plantilla actualizada correctamente.", "success");

        await cargarPlantillas();
    }

    function pedirConfirmacionEstado(id) {
        const plantilla = plantillas.find(p => p.idPlantilla === id);
        if (!plantilla) return;

        estadoPendiente = { id, activa: !plantilla.activa };

        $("TituloModalEstadoPlantilla").textContent = plantilla.activa
            ? "Desactivar plantilla"
            : "Activar plantilla";

        $("TextoModalEstadoPlantilla").textContent = plantilla.activa
            ? `La plantilla "${plantilla.nombre}" deja de aparecer al redactar mensajes. No se borra y se puede volver a activar.`
            : `La plantilla "${plantilla.nombre}" vuelve a aparecer al redactar mensajes.`;

        modal("ModalEstadoPlantilla").show();
    }

    async function confirmarEstado() {
        if (estadoPendiente === null) return;

        const boton = $("BotonConfirmarEstadoPlantilla");
        const { id, activa } = estadoPendiente;

        boton.disabled = true;

        const resultado = await enviar(
            `${RUTAS.estadoPlantilla}?id=${id}&activa=${activa}`, datosConToken({}));

        boton.disabled = false;
        modal("ModalEstadoPlantilla").hide();
        estadoPendiente = null;

        if (!resultado.ok) {
            notificar(resultado.mensaje, "danger");
            return;
        }

        notificar(activa ? "Plantilla activada correctamente." : "Plantilla desactivada correctamente.", "success");

        await cargarPlantillas();
    }

    // =====================================================
    // PESTANIAS
    // =====================================================

    function inicializarPestanias() {
        const selector = $("SelectorComunicacion");
        if (!selector) return;

        selector.querySelectorAll("[data-vista]").forEach(boton => {
            boton.addEventListener("click", function () {
                selector.querySelectorAll(".nav-link").forEach(b => b.classList.remove("active"));
                boton.classList.add("active");

                const elegida = boton.getAttribute("data-vista");

                VISTAS.forEach(nombre => {
                    const vista = $("Vista" + nombre);
                    if (vista) vista.classList.toggle("d-none", nombre !== elegida);
                });

                // Cada pestania carga sus datos la primera vez que se abre
                if (elegida === "Plantillas" && plantillas.length === 0) {
                    cargarPlantillas();
                }

                if (elegida === "Recordatorios") {
                    cargarRecordatorios();
                }
            });
        });
    }

    // =====================================================
    // ARRANQUE
    // =====================================================

    document.addEventListener("DOMContentLoaded", function () {
        inicializarPestanias();

        // ---------- correo ----------

        if ($("FormularioCorreo")) {
            $("DestinatarioCorreo").addEventListener("change", function () {
                llenarEstudiantes();
                pintarMarcadoresCorreo();
            });

            $("EstudianteCorreo").addEventListener("change", function () {
                cargarCitas();
                pintarMarcadoresCorreo();
            });

            $("CitaCorreo")?.addEventListener("change", pintarMarcadoresCorreo);
            $("PlantillaCorreo").addEventListener("change", aplicarPlantilla);
            $("AsuntoCorreo").addEventListener("input", pintarMarcadoresCorreo);

            $("CuerpoCorreo").addEventListener("input", function () {
                actualizarContadorCorreo();
                pintarMarcadoresCorreo();
            });

            $("BotonVistaPreviaCorreo").addEventListener("click", verVistaPrevia);
            $("FormularioCorreo").addEventListener("submit", enviarCorreo);
            $("BotonEnviarDesdeVistaPrevia")?.addEventListener("click", () => enviarCorreo(null));

            llenarEstudiantes();
        }

        $("BotonRecargarHistorialCorreo")?.addEventListener("click", cargarHistorialCorreo);
        $("BotonRecargarRecordatorios")?.addEventListener("click", cargarRecordatorios);

        // Los dos listados abren el mismo modal. Delegado, porque las filas se
        // vuelven a pintar en cada recarga.
        ["HistorialCorreo", "CuerpoTablaRecordatorios"].forEach(id => {
            $(id)?.addEventListener("click", function (evento) {
                const fila = evento.target.closest("[data-notificacion]");
                if (!fila) return;

                abrirDetalleNotificacion(Number(fila.getAttribute("data-notificacion")));
            });
        });

        if ($("HistorialCorreo")) {
            cargarHistorialCorreo();
        }

        // ---------- plantillas ----------

        if (!$("VistaPlantillas")) return;

        $("SoloActivasPlantillas")?.addEventListener("change", cargarPlantillas);

        if (puedeAdministrar) {
            $("BotonNuevaPlantilla")?.addEventListener("click", abrirNuevaPlantilla);
            $("FormularioPlantilla")?.addEventListener("submit", guardarPlantilla);
            $("BotonConfirmarEstadoPlantilla")?.addEventListener("click", confirmarEstado);

            $("CuerpoPlantilla")?.addEventListener("input", function () {
                actualizarContadorPlantilla();
                pintarMarcadoresDetectados();
            });

            $("AsuntoPlantilla")?.addEventListener("input", pintarMarcadoresDetectados);

            $("CuerpoTablaPlantillas")?.addEventListener("click", function (evento) {
                const boton = evento.target.closest("[data-accion]");
                if (!boton) return;

                const id = Number(boton.getAttribute("data-id"));

                if (boton.getAttribute("data-accion") === "editar") {
                    abrirEdicionPlantilla(id);
                } else {
                    pedirConfirmacionEstado(id);
                }
            });
        }
    });
})();
