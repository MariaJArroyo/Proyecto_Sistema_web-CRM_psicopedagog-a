document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    // =====================================================
    // CONSTANTES Y ESTADO
    // =====================================================

    const DIAS_CORTOS = ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];
    const DIAS_SEMANA_VISIBLES = 6;                 // lunes a sabado
    const REFRESCO_MS = 5 * 60 * 1000;
    const ESTADO_CANCELADA = 4;
    const ESTADOS_PENDIENTES = [1, 2];

    const CLASES_ESTADO = {
        1: "programada",
        2: "confirmada",
        3: "completada",
        4: "cancelada",
        5: "no-asistio"
    };

    const contenedor = document.getElementById("VistaAgenda");
    const textoRango = document.getElementById("RangoAgenda");

    let vista = "semana";
    let fechaReferencia = soloFecha(new Date());
    let citas = [];
    let eventos = [];          // citas individuales + grupos ya armados
    let citaActual = null;
    let idGrupoActual = null;
    let cargaEnCurso = null;
    let diasNoLaborales = new Map();   // "yyyy-mm-dd" -> { motivo, esFeriado }


    // =====================================================
    // UTILIDADES
    // =====================================================

    function escaparHtml(texto) {
        const div = document.createElement("div");
        div.textContent = texto ?? "";
        return div.innerHTML;
    }

    function valorTexto(valor) {
        const texto = (valor ?? "").toString().trim();
        return texto === "" ? "-" : texto;
    }

    function capitalizar(texto) {
        return texto.charAt(0).toUpperCase() + texto.slice(1);
    }

    function soloFecha(fecha) {
        return new Date(fecha.getFullYear(), fecha.getMonth(), fecha.getDate());
    }

    function sumarDias(fecha, dias) {
        const resultado = new Date(fecha);
        resultado.setDate(resultado.getDate() + dias);
        return resultado;
    }

    function inicioSemana(fecha) {
        const dia = soloFecha(fecha);
        return sumarDias(dia, -((dia.getDay() + 6) % 7));
    }

    function mismoDia(a, b) {
        return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
    }

    function esMesActual(fecha) {
        const hoy = new Date();
        return fecha.getFullYear() === hoy.getFullYear() && fecha.getMonth() === hoy.getMonth();
    }

    function aIso(fecha) {
        return `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, "0")}-${String(fecha.getDate()).padStart(2, "0")}`;
    }

    function desdeIso(iso) {
        const [anio, mes, dia] = iso.split("-").map(Number);
        return new Date(anio, mes - 1, dia);
    }

    function leerFechaHora(texto) {
        return new Date(texto);
    }

    function formatoHora(fecha) {
        return fecha.toLocaleTimeString("es-CR", { hour: "numeric", minute: "2-digit" });
    }

    function formatoFechaLarga(fecha) {
        return capitalizar(fecha.toLocaleDateString("es-CR", {
            weekday: "long", day: "numeric", month: "long", year: "numeric"
        }));
    }

    function mesCorto(fecha) {
        return fecha.toLocaleDateString("es-CR", { month: "short" }).replace(".", "");
    }

    function claseEstado(cita) {
        return CLASES_ESTADO[cita.idEstadoCita] || "programada";
    }

    function hayModalAbierto() {
        return document.querySelector(".modal.show") !== null;
    }

    function tokenAntifalsificacion() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function notificar(mensaje, tipo) {
        if (typeof MostrarToast === "function") MostrarToast(mensaje, tipo);
    }

    function mostrarAlerta(elemento, mensaje, tipo = "danger") {
        elemento.className = "alert alert-" + tipo;
        elemento.textContent = mensaje;
    }

    function ocultarAlerta(elemento) {
        elemento.className = "alert d-none";
        elemento.textContent = "";
    }

    function marcar(campo, valido) {
        campo.classList.toggle("is-invalid", !valido);
        return valido;
    }

    function mostrarError(campoError, visible) {
        campoError.classList.toggle("d-block", visible);
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

    function datosConToken(campos) {
        const datos = new FormData();
        datos.append("__RequestVerificationToken", tokenAntifalsificacion());
        Object.entries(campos).forEach(([clave, valor]) => datos.append(clave, valor ?? ""));
        return datos;
    }

    function modal(id) {
        return bootstrap.Modal.getOrCreateInstance(document.getElementById(id));
    }


    // =====================================================
    // GRUPOS: se arman a partir de las citas
    // =====================================================

    // Las citas con IdGrupoCita se juntan en un solo "evento grupo"
    function armarEventos(lista) {
        const grupos = new Map();
        const resultado = [];

        lista.forEach(function (cita) {
            if (cita.idGrupoCita == null) {
                resultado.push({ tipo: "cita", inicio: cita.inicio, fin: cita.fin, cita });
                return;
            }

            let grupo = grupos.get(cita.idGrupoCita);

            if (!grupo) {
                grupo = {
                    tipo: "grupo",
                    idGrupoCita: cita.idGrupoCita,
                    nombre: cita.nombreGrupo,
                    cupo: cita.cupoGrupo,
                    inicio: cita.inicio,
                    fin: cita.fin,
                    idTipoSesion: cita.idTipoSesion,
                    tipoSesion: cita.tipoSesion,
                    modalidad: cita.modalidad,
                    puedeGestionar: false,
                    miembros: []
                };
                grupos.set(cita.idGrupoCita, grupo);
                resultado.push(grupo);
            }

            grupo.miembros.push(cita);
            if (cita.puedeGestionarGrupo) grupo.puedeGestionar = true;

            // Las canceladas conservan la hora original: la del grupo es la de los pendientes
            if (ESTADOS_PENDIENTES.includes(cita.idEstadoCita)) {
                grupo.inicio = cita.inicio;
                grupo.fin = cita.fin;
            }
        });

        return resultado.sort((a, b) => a.inicio - b.inicio);
    }

    function nombreGrupo(grupo) {
        return grupo.nombre || "Grupo";
    }

    function inscritos(grupo) {
        return grupo.miembros.filter(m => m.idEstadoCita !== ESTADO_CANCELADA);
    }

    // Color del grupo segun sus integrantes
    function claseGrupo(grupo) {
        const activos = inscritos(grupo);

        if (activos.length === 0) return "cancelada";
        if (activos.some(m => ESTADOS_PENDIENTES.includes(m.idEstadoCita))) return "programada";
        if (activos.every(m => m.idEstadoCita === 5)) return "no-asistio";
        return "completada";
    }

    function buscarGrupo(idGrupoCita) {
        return eventos.find(e => e.tipo === "grupo" && e.idGrupoCita === idGrupoCita);
    }


    // =====================================================
    // RANGO VISIBLE Y CARGA
    // =====================================================

    function calcularRango() {
        if (vista === "dia") {
            return { desde: fechaReferencia, hasta: sumarDias(fechaReferencia, 1) };
        }

        if (vista === "semana") {
            const lunes = inicioSemana(fechaReferencia);
            return { desde: lunes, hasta: sumarDias(lunes, 7) };
        }

        const inicio = inicioSemana(new Date(fechaReferencia.getFullYear(), fechaReferencia.getMonth(), 1));
        return { desde: inicio, hasta: sumarDias(inicio, 42) };
    }

    function actualizarTitulo() {
        if (vista === "dia") {
            textoRango.textContent = formatoFechaLarga(fechaReferencia);
            return;
        }

        if (vista === "mes") {
            textoRango.textContent = capitalizar(
                fechaReferencia.toLocaleDateString("es-CR", { month: "long", year: "numeric" }));
            return;
        }

        const lunes = inicioSemana(fechaReferencia);
        const sabado = sumarDias(lunes, DIAS_SEMANA_VISIBLES - 1);
        const mesFin = sabado.toLocaleDateString("es-CR", { month: "long" });

        textoRango.textContent = lunes.getMonth() === sabado.getMonth()
            ? `Semana del ${lunes.getDate()} al ${sabado.getDate()} de ${mesFin} de ${sabado.getFullYear()}`
            : `Semana del ${lunes.getDate()} de ${lunes.toLocaleDateString("es-CR", { month: "long" })} al ${sabado.getDate()} de ${mesFin} de ${sabado.getFullYear()}`;
    }

    async function cargar() {
        const { desde, hasta } = calcularRango();

        actualizarTitulo();

        cargaEnCurso?.abort();
        cargaEnCurso = new AbortController();

        contenedor.setAttribute("aria-busy", "true");
        contenedor.innerHTML =
            '<div class="text-center py-5 TextoSuave"><span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Cargando agenda…</div>';

        const consulta = `desde=${aIso(desde)}&hasta=${aIso(hasta)}`;
        const opciones = { headers: { "Accept": "application/json" }, signal: cargaEnCurso.signal };

        try {
            const [respuesta, respuestaDias] = await Promise.all([
                fetch(`/Admin/ConsultarAgenda?${consulta}`, opciones),
                fetch(`/Admin/DiasNoLaboralesAgenda?${consulta}`, opciones).catch(function (error) {
                    if (error.name === "AbortError") throw error;
                    return null;
                })
            ]);

            if (!respuesta.ok) {
                throw new Error(await leerMensaje(respuesta, "No se pudo cargar la agenda."));
            }

            citas = (await respuesta.json()).map(c => ({
                ...c,
                inicio: leerFechaHora(c.fechaHoraInicio),
                fin: leerFechaHora(c.fechaHoraFin)
            }));

            diasNoLaborales = await leerDiasNoLaborales(respuestaDias);
            eventos = armarEventos(citas);
            dibujar();

        } catch (error) {
            if (error.name === "AbortError") return;
            contenedor.innerHTML = `<div class="alert alert-danger mb-0">${escaparHtml(error.message)}</div>`;
        } finally {
            contenedor.removeAttribute("aria-busy");
        }
    }

    // Si esta consulta falla la agenda se dibuja igual: las SPs bloquean esos dias de todas formas
    async function leerDiasNoLaborales(respuesta) {
        if (!respuesta?.ok) return new Map();

        try {
            const lista = await respuesta.json();
            return new Map(lista.map(d => [d.fecha, d]));
        } catch {
            return new Map();
        }
    }

    function diaNoLaboral(dia) {
        return diasNoLaborales.get(aIso(dia));
    }

    function textoNoLaboral(info) {
        return `${info.esFeriado ? "Feriado" : "No laboral"}: ${info.motivo}`;
    }

    function eventosDelDia(dia) {
        return eventos.filter(e => mismoDia(e.inicio, dia));
    }


    // =====================================================
    // DIBUJO DE LAS VISTAS
    // =====================================================

    function dibujar() {
        if (vista === "semana") dibujarSemana();
        else if (vista === "dia") dibujarDia();
        else dibujarMes();
    }

    function tarjetaCita(cita) {
        return `
            <div class="TarjetaCita TarjetaCita-${claseEstado(cita)}" role="button" tabindex="0"
                 data-id-cita="${cita.idCita}"
                 aria-label="${escaparHtml(`${formatoHora(cita.inicio)}, ${cita.estudiante}, ${cita.estado}`)}">
                <div class="TarjetaCita-hora">${formatoHora(cita.inicio)}</div>
                <div class="small">${escaparHtml(cita.estudiante)}</div>
                <div class="small TextoSuave">${escaparHtml(cita.tipoSesion)}</div>
            </div>`;
    }

    function tarjetaGrupo(grupo) {
        const cantidad = inscritos(grupo).length;

        return `
            <div class="TarjetaCita TarjetaCita-${claseGrupo(grupo)} TarjetaCita-grupo" role="button" tabindex="0"
                 data-id-grupo="${grupo.idGrupoCita}"
                 aria-label="${escaparHtml(`${formatoHora(grupo.inicio)}, ${nombreGrupo(grupo)}, ${cantidad} estudiantes`)}">
                <div class="TarjetaCita-hora">
                    <i class="bi bi-people-fill me-1" aria-hidden="true"></i>${formatoHora(grupo.inicio)}
                </div>
                <div class="small">${escaparHtml(nombreGrupo(grupo))}</div>
                <div class="small TextoSuave">${cantidad} de ${grupo.cupo} · ${escaparHtml(grupo.tipoSesion)}</div>
            </div>`;
    }

    function tarjetaEvento(evento) {
        return evento.tipo === "grupo" ? tarjetaGrupo(evento) : tarjetaCita(evento.cita);
    }

    function dibujarSemana() {
        const lunes = inicioSemana(fechaReferencia);
        const hoy = soloFecha(new Date());
        let html = '<div class="AgendaSemana">';

        for (let i = 0; i < DIAS_SEMANA_VISIBLES; i++) {
            const dia = sumarDias(lunes, i);
            const delDia = eventosDelDia(dia);
            const noLaboral = diaNoLaboral(dia);

            const clases = ["AgendaSemana-Columna"];
            if (mismoDia(dia, hoy)) clases.push("hoy");
            if (noLaboral) clases.push("no-laboral");

            const aviso = noLaboral
                ? `<div class="AvisoNoLaboral"><i class="bi bi-slash-circle me-1" aria-hidden="true"></i>${escaparHtml(textoNoLaboral(noLaboral))}</div>`
                : "";

            const contenido = delDia.length
                ? delDia.map(tarjetaEvento).join("")
                : (noLaboral ? "" : '<div class="AgendaSemana-Vacio">Sin citas</div>');

            html += `
                <div class="${clases.join(" ")}">
                    <div class="AgendaSemana-DiaTitulo">${DIAS_CORTOS[dia.getDay()]} ${dia.getDate()} ${mesCorto(dia)}</div>
                    ${aviso}
                    ${contenido}
                </div>`;
        }

        contenedor.innerHTML = html + "</div>";
    }

    function dibujarDia() {
        const delDia = eventosDelDia(fechaReferencia);
        const noLaboral = diaNoLaboral(fechaReferencia);

        const aviso = noLaboral ? `
            <div class="alert alert-secondary d-flex align-items-center gap-2 mb-3">
                <i class="bi bi-slash-circle" aria-hidden="true"></i>
                <div><strong>${noLaboral.esFeriado ? "Feriado" : "Día no laboral"}:</strong>
                    ${escaparHtml(noLaboral.motivo)}. No se pueden agendar citas este día.</div>
            </div>` : "";

        if (delDia.length === 0) {
            contenedor.innerHTML = aviso || '<p class="TextoSuave text-center py-4 mb-0">No hay citas para este día.</p>';
            return;
        }

        contenedor.innerHTML = aviso + '<div class="d-flex flex-column gap-2">' + delDia.map(function (evento) {

            if (evento.tipo === "grupo") {
                const activos = inscritos(evento);
                const clase = claseGrupo(evento);

                return `
                    <div class="AgendaDia-Item TarjetaCita TarjetaCita-${clase} TarjetaCita-grupo" role="button" tabindex="0"
                         data-id-grupo="${evento.idGrupoCita}">
                        <div class="AgendaDia-Hora"><i class="bi bi-people-fill me-1"></i>${formatoHora(evento.inicio)}</div>
                        <div>
                            <div class="fw-semibold">${escaparHtml(nombreGrupo(evento))} — ${escaparHtml(evento.tipoSesion)}</div>
                            <div class="TextoSuave small">
                                ${escaparHtml(evento.modalidad)} · Hasta ${formatoHora(evento.fin)} ·
                                ${activos.length ? escaparHtml(activos.map(m => m.estudiante).join(", ")) : "Sin participantes activos"}
                            </div>
                        </div>
                        <span class="badge badge-cita-${clase} ms-auto">${activos.length}/${evento.cupo}</span>
                    </div>`;
            }

            const cita = evento.cita;

            return `
                <div class="AgendaDia-Item TarjetaCita TarjetaCita-${claseEstado(cita)}" role="button" tabindex="0"
                     data-id-cita="${cita.idCita}">
                    <div class="AgendaDia-Hora">${formatoHora(cita.inicio)}</div>
                    <div>
                        <div class="fw-semibold">${escaparHtml(cita.estudiante)} — ${escaparHtml(cita.tipoSesion)}</div>
                        <div class="TextoSuave small">
                            ${escaparHtml(cita.modalidad)} · Hasta ${formatoHora(cita.fin)}
                            ${cita.encargado ? " · Encargado: " + escaparHtml(cita.encargado) : ""}
                        </div>
                    </div>
                    <span class="badge badge-cita-${claseEstado(cita)} ms-auto">${escaparHtml(cita.estado)}</span>
                </div>`;
        }).join("") + "</div>";
    }

    function dibujarMes() {
        const { desde } = calcularRango();
        const hoy = soloFecha(new Date());
        const mesActual = fechaReferencia.getMonth();
        const MAXIMO_PUNTOS = 8;

        let html = '<div class="CalendarioMes">';

        ["Lun", "Mar", "Mié", "Jue", "Vie", "Sáb", "Dom"].forEach(d => {
            html += `<div class="CalendarioMes-DiaSemana">${d}</div>`;
        });

        for (let i = 0; i < 42; i++) {
            const dia = sumarDias(desde, i);
            const delDia = eventosDelDia(dia);
            const extra = delDia.length - MAXIMO_PUNTOS;
            const noLaboral = diaNoLaboral(dia);

            const clases = ["CalendarioMes-Celda"];
            if (dia.getMonth() !== mesActual) clases.push("fuera-de-mes");
            if (mismoDia(dia, hoy)) clases.push("hoy");
            if (noLaboral) clases.push("no-laboral");

            const resumen = delDia.length === 0 ? "sin citas"
                : delDia.length === 1 ? "1 cita"
                : `${delDia.length} citas`;

            const detalle = delDia.map(e => e.tipo === "grupo"
                ? `${formatoHora(e.inicio)} ${nombreGrupo(e)} (${inscritos(e).length} estudiantes)`
                : `${formatoHora(e.inicio)} ${e.cita.estudiante} (${e.cita.estado})`).join("\n");

            const titulo = [noLaboral ? textoNoLaboral(noLaboral) : "", detalle].filter(Boolean).join("\n");

            const puntos = delDia.length === 0 ? "" : `
                <div class="CalendarioMes-Puntos">
                    ${delDia.slice(0, MAXIMO_PUNTOS).map(e => e.tipo === "grupo"
                        ? `<span class="CalendarioMes-Punto punto-${claseGrupo(e)} punto-grupo"></span>`
                        : `<span class="CalendarioMes-Punto punto-${claseEstado(e.cita)}"></span>`).join("")}
                    ${extra > 0 ? `<span class="CalendarioMes-Mas">+${extra}</span>` : ""}
                </div>`;

            html += `
                <div class="${clases.join(" ")}" role="button" tabindex="0" data-dia="${aIso(dia)}"
                     aria-label="${escaparHtml(`${formatoFechaLarga(dia)}, ${noLaboral ? textoNoLaboral(noLaboral) + ", " : ""}${resumen}`)}"
                     ${titulo ? `title="${escaparHtml(titulo)}"` : ""}>
                    <div class="CalendarioMes-Numero">${dia.getDate()}</div>
                    ${noLaboral ? `<div class="CalendarioMes-NoLaboral">${escaparHtml(noLaboral.motivo)}</div>` : ""}
                    ${puntos}
                </div>`;
        }

        contenedor.innerHTML = html + "</div>";
    }


    // =====================================================
    // NAVEGACION
    // =====================================================

    function cambiarVista(nueva, conservarFecha = false) {
        if (vista === "mes" && nueva !== "mes" && !conservarFecha) {
            fechaReferencia = esMesActual(fechaReferencia)
                ? soloFecha(new Date())
                : new Date(fechaReferencia.getFullYear(), fechaReferencia.getMonth(), 1);
        }

        vista = nueva;

        document.querySelectorAll("#SelectorVistaAgenda [data-vista]").forEach(boton => {
            boton.classList.toggle("active", boton.dataset.vista === nueva);
        });

        cargar();
    }

    function mover(direccion) {
        if (vista === "dia") {
            fechaReferencia = sumarDias(fechaReferencia, direccion);
        } else if (vista === "semana") {
            fechaReferencia = sumarDias(fechaReferencia, 7 * direccion);
        } else {
            const destino = new Date(fechaReferencia.getFullYear(), fechaReferencia.getMonth() + direccion, 1);
            fechaReferencia = esMesActual(destino) ? soloFecha(new Date()) : destino;
        }

        cargar();
    }

    document.querySelectorAll("#SelectorVistaAgenda [data-vista]").forEach(boton => {
        boton.addEventListener("click", () => cambiarVista(boton.dataset.vista));
    });

    document.getElementById("BotonAnterior").addEventListener("click", () => mover(-1));
    document.getElementById("BotonSiguiente").addEventListener("click", () => mover(1));
    document.getElementById("BotonHoy").addEventListener("click", function () {
        fechaReferencia = soloFecha(new Date());
        cargar();
    });

    function activarElemento(objetivo) {
        const cita = objetivo.closest("[data-id-cita]");
        if (cita) {
            abrirDetalle(Number(cita.dataset.idCita));
            return;
        }

        const grupo = objetivo.closest("[data-id-grupo]");
        if (grupo) {
            abrirDetalleGrupo(Number(grupo.dataset.idGrupo));
            return;
        }

        const dia = objetivo.closest("[data-dia]");
        if (dia) {
            fechaReferencia = desdeIso(dia.dataset.dia);
            cambiarVista("dia", true);
        }
    }

    contenedor.addEventListener("click", evento => activarElemento(evento.target));

    contenedor.addEventListener("keydown", function (evento) {
        if ((evento.key === "Enter" || evento.key === " ")
            && evento.target.matches("[data-id-cita], [data-id-grupo], [data-dia]")) {
            evento.preventDefault();
            activarElemento(evento.target);
        }
    });


    // =====================================================
    // HORARIOS DISPONIBLES (compartido)
    // =====================================================

    let consultaEspacios = 0;

    async function cargarEspacios(contenedorEspacios, campoOculto, fechaIso, idTipoSesion, excluir = {}) {
        campoOculto.value = "";
        contenedorEspacios.classList.remove("is-invalid");

        if (!fechaIso || !idTipoSesion) {
            contenedorEspacios.innerHTML =
                '<p class="small TextoSuave mb-0">Seleccione la fecha y el tipo de sesión para ver los horarios libres.</p>';
            return;
        }

        const numeroConsulta = ++consultaEspacios;

        contenedorEspacios.innerHTML =
            '<p class="small TextoSuave mb-0"><span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>Buscando horarios…</p>';

        let url = `/Admin/DisponibilidadCita?fecha=${fechaIso}&idTipoSesion=${idTipoSesion}`;
        if (excluir.idCita) url += `&idCitaExcluir=${excluir.idCita}`;
        if (excluir.idGrupo) url += `&idGrupoExcluir=${excluir.idGrupo}`;

        try {
            const respuesta = await fetch(url, { headers: { "Accept": "application/json" } });
            if (!respuesta.ok) throw new Error();

            const espacios = await respuesta.json();
            if (numeroConsulta !== consultaEspacios) return;

            contenedorEspacios.innerHTML = espacios.length === 0
                ? '<p class="small text-warning-emphasis mb-0"><i class="bi bi-calendar-x me-1"></i>No hay horarios disponibles ese día.</p>'
                : espacios.map(e => `
                    <button type="button" class="btn btn-sm btn-outline-primary" aria-pressed="false"
                            data-inicio="${e.inicio}">${formatoHora(leerFechaHora(e.inicio))}</button>`).join("");

        } catch {
            if (numeroConsulta !== consultaEspacios) return;
            contenedorEspacios.innerHTML = '<p class="small text-danger mb-0">No se pudieron cargar los horarios.</p>';
        }
    }

    // Botones "seleccionables" (horarios y grupos): uno activo a la vez
    function activarSeleccion(contenedorOpciones, atributo, campoOculto, campoError) {
        contenedorOpciones.addEventListener("click", function (evento) {
            const boton = evento.target.closest(`[${atributo}]`);
            if (!boton) return;

            contenedorOpciones.querySelectorAll(`[${atributo}]`).forEach(b => {
                b.classList.remove("active");
                b.setAttribute("aria-pressed", "false");
            });

            boton.classList.add("active");
            boton.setAttribute("aria-pressed", "true");
            campoOculto.value = boton.getAttribute(atributo);

            contenedorOpciones.classList.remove("is-invalid");
            if (campoError) mostrarError(campoError, false);
        });
    }


    // =====================================================
    // NUEVA CITA: individual, grupo nuevo o unirse a un grupo
    // =====================================================

    const modalNueva = document.getElementById("ModalNuevaCita");
    const formNueva = document.getElementById("FormularioNuevaCita");
    const errorNueva = document.getElementById("NuevaCitaError");
    const tituloNueva = document.getElementById("TituloNuevaCita");
    const botonGuardar = document.getElementById("BotonGuardarNuevaCita");

    const campoTipo = document.getElementById("TipoSesionCita");
    const campoFecha = document.getElementById("FechaCita");
    const campoHora = document.getElementById("HoraCita");
    const errorHora = document.getElementById("HoraCitaError");
    const espaciosNueva = document.getElementById("EspaciosNuevaCita");
    const campoObservaciones = document.getElementById("ObservacionesCita");

    const listaParticipantes = document.getElementById("ListaParticipantes");
    const contadorParticipantes = document.getElementById("ContadorParticipantes");
    const errorParticipantes = document.getElementById("ParticipantesError");
    const campoNombreGrupo = document.getElementById("NombreGrupoCita");
    const campoCupo = document.getElementById("CupoGrupoCita");

    const gruposDisponibles = document.getElementById("GruposDisponibles");
    const campoGrupoElegido = document.getElementById("GrupoElegido");
    const errorGrupoElegido = document.getElementById("GrupoElegidoError");

    const TEXTOS_MODO = {
        individual: { titulo: "Nueva cita", boton: "Guardar cita", etiqueta: "Estudiante" },
        grupo: { titulo: "Nuevo grupo", boton: "Crear grupo", etiqueta: "Agregar estudiante al grupo" },
        unirse: { titulo: "Unir estudiante a un grupo", boton: "Agregar al grupo", etiqueta: "Estudiante" }
    };

    let modo = "individual";
    let participantes = [];

    const buscador = crearBuscadorEstudiantes(document.getElementById("BuscadorEstudianteCita"), {
        url: "/Admin/BuscarEstudiantesCita",
        nombreCampo: "IdEstudiante",
        idEntrada: "EstudianteCita",
        // En modo grupo, elegir un estudiante lo suma a la lista y deja el buscador listo para el siguiente
        alCambiar: function (estudiante) {
            if (modo !== "grupo" || !estudiante) return;
            agregarParticipante(estudiante);
            buscador.limpiar();
            buscador.enfocar();
        }
    });

    activarSeleccion(espaciosNueva, "data-inicio", campoHora, errorHora);
    activarSeleccion(gruposDisponibles, "data-id-grupo-opcion", campoGrupoElegido, errorGrupoElegido);

    // ----- modo -----

    function aplicarModo(nuevo) {
        modo = nuevo;

        formNueva.querySelectorAll("[data-modos]").forEach(seccion => {
            seccion.classList.toggle("d-none", !seccion.dataset.modos.split(" ").includes(modo));
        });

        tituloNueva.textContent = TEXTOS_MODO[modo].titulo;
        botonGuardar.textContent = TEXTOS_MODO[modo].boton;
        document.getElementById("EtiquetaEstudianteCita").textContent = TEXTOS_MODO[modo].etiqueta;

        ocultarAlerta(errorNueva);
        buscador.limpiar();
        recargarOpcionesHorario();
    }

    formNueva.querySelectorAll('input[name="ModoCita"]').forEach(radio => {
        radio.addEventListener("change", () => aplicarModo(radio.value));
    });

    // ----- participantes (modo grupo) -----

    function cupoActual() {
        return Number(campoCupo.value) || 0;
    }

    function pintarParticipantes() {
        listaParticipantes.innerHTML = participantes.length === 0
            ? '<span class="small TextoSuave px-1">Todavía no hay participantes.</span>'
            : participantes.map(p => `
                <span class="ChipParticipante">
                    ${escaparHtml(p.estudiante)}
                    <button type="button" class="btn-close" aria-label="Quitar a ${escaparHtml(p.estudiante)}"
                            data-quitar-participante="${p.idEstudiante}"></button>
                </span>`).join("");

        contadorParticipantes.textContent = `${participantes.length} de ${cupoActual() || "?"}`;
    }

    function agregarParticipante(estudiante) {
        ocultarAlerta(errorNueva);

        if (participantes.some(p => p.idEstudiante === estudiante.idEstudiante)) {
            mostrarAlerta(errorNueva, `${estudiante.estudiante} ya está en la lista.`, "warning");
            return;
        }

        if (participantes.length >= cupoActual()) {
            mostrarAlerta(errorNueva, "Se llenó el cupo. Aumente el cupo para agregar más estudiantes.", "warning");
            return;
        }

        participantes.push({ idEstudiante: estudiante.idEstudiante, estudiante: estudiante.estudiante });
        listaParticipantes.classList.remove("is-invalid");
        mostrarError(errorParticipantes, false);
        pintarParticipantes();
    }

    listaParticipantes.addEventListener("click", function (evento) {
        const boton = evento.target.closest("[data-quitar-participante]");
        if (!boton) return;

        participantes = participantes.filter(p => p.idEstudiante !== Number(boton.dataset.quitarParticipante));
        pintarParticipantes();
    });

    campoCupo.addEventListener("input", function () {
        campoCupo.classList.remove("is-invalid");
        pintarParticipantes();
    });

    // ----- horarios / grupos del dia -----

    async function cargarGruposDisponibles() {
        campoGrupoElegido.value = "";
        gruposDisponibles.classList.remove("is-invalid");

        if (!campoFecha.value) {
            gruposDisponibles.innerHTML = '<p class="small TextoSuave mb-0">Seleccione la fecha para ver los grupos.</p>';
            return;
        }

        gruposDisponibles.innerHTML =
            '<p class="small TextoSuave mb-0"><span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>Buscando grupos…</p>';

        try {
            const respuesta = await fetch(`/Admin/GruposDisponiblesCita?fecha=${campoFecha.value}`, {
                headers: { "Accept": "application/json" }
            });
            if (!respuesta.ok) throw new Error();

            const grupos = await respuesta.json();

            gruposDisponibles.innerHTML = grupos.length === 0
                ? '<p class="small text-warning-emphasis mb-0"><i class="bi bi-people me-1"></i>No hay grupos con cupo ese día.</p>'
                : grupos.map(g => `
                    <button type="button" class="btn btn-outline-primary OpcionGrupo" aria-pressed="false"
                            data-id-grupo-opcion="${g.idGrupoCita}" data-inicio="${g.inicio}">
                        <strong>${formatoHora(leerFechaHora(g.inicio))} · ${escaparHtml(g.nombre || "Grupo")}</strong>
                        <small>${escaparHtml(g.tipoSesion)} · ${escaparHtml(g.modalidad)} · ${g.inscritos} de ${g.cupoMaximo} cupos</small>
                    </button>`).join("");

        } catch {
            gruposDisponibles.innerHTML = '<p class="small text-danger mb-0">No se pudieron cargar los grupos.</p>';
        }
    }

    function recargarOpcionesHorario() {
        if (modo === "unirse") {
            cargarGruposDisponibles();
        } else {
            cargarEspacios(espaciosNueva, campoHora, campoFecha.value, campoTipo.value);
        }
    }

    campoTipo.addEventListener("change", function () {
        campoTipo.classList.remove("is-invalid");
        recargarOpcionesHorario();
    });

    campoFecha.addEventListener("change", function () {
        campoFecha.classList.remove("is-invalid");
        recargarOpcionesHorario();
    });

    // ----- abrir -----

    modalNueva.addEventListener("show.bs.modal", function () {
        formNueva.reset();
        formNueva.querySelectorAll(".is-invalid").forEach(c => c.classList.remove("is-invalid"));
        [errorHora, errorParticipantes, errorGrupoElegido].forEach(e => mostrarError(e, false));

        participantes = [];
        pintarParticipantes();

        const hoy = soloFecha(new Date());
        campoFecha.min = aIso(hoy);
        campoFecha.value = aIso(fechaReferencia >= hoy ? fechaReferencia : hoy);

        aplicarModo("individual");
    });

    modalNueva.addEventListener("shown.bs.modal", () => buscador.enfocar());

    // ----- guardar -----

    function validarHorario() {
        const hay = campoHora.value !== "";
        espaciosNueva.classList.toggle("is-invalid", !hay);
        mostrarError(errorHora, !hay);
        return hay;
    }

    function modalidadElegida() {
        return formNueva.querySelector('input[name="IdModalidad"]:checked')?.value ?? "";
    }

    function validarComunes() {
        let valido = marcar(campoTipo, campoTipo.value !== "");
        valido = marcar(campoFecha, campoFecha.value !== "" && campoFecha.value >= campoFecha.min) && valido;
        return validarHorario() && valido;
    }

    async function guardarIndividual() {
        let valido = buscador.validar();
        valido = validarComunes() && valido;
        if (!valido) return null;

        return {
            fecha: leerFechaHora(campoHora.value),
            resultado: await enviar(formNueva.action, new FormData(formNueva)),
            exito: "Cita agendada correctamente."
        };
    }

    async function guardarGrupo() {
        const hayParticipantes = participantes.length > 0;
        listaParticipantes.classList.toggle("is-invalid", !hayParticipantes);
        mostrarError(errorParticipantes, !hayParticipantes);

        const cupo = cupoActual();
        let valido = marcar(campoCupo, cupo >= 2 && cupo <= 30 && cupo >= participantes.length);
        valido = validarComunes() && hayParticipantes && valido;
        if (!valido) return null;

        const datos = datosConToken({
            IdTipoSesion: campoTipo.value,
            IdModalidad: modalidadElegida(),
            FechaHoraInicio: campoHora.value,
            Nombre: campoNombreGrupo.value.trim(),
            CupoMaximo: cupo,
            Observaciones: campoObservaciones.value.trim()
        });

        // Lista: la misma clave repetida -> List<int> IdsEstudiantes
        participantes.forEach(p => datos.append("IdsEstudiantes", p.idEstudiante));

        return {
            fecha: leerFechaHora(campoHora.value),
            resultado: await enviar("/Admin/CrearGrupoCita", datos),
            exito: "Grupo agendado correctamente."
        };
    }

    async function guardarUnirse() {
        let valido = buscador.validar();
        valido = marcar(campoFecha, campoFecha.value !== "") && valido;

        const hayGrupo = campoGrupoElegido.value !== "";
        gruposDisponibles.classList.toggle("is-invalid", !hayGrupo);
        mostrarError(errorGrupoElegido, !hayGrupo);
        if (!valido || !hayGrupo) return null;

        const opcion = gruposDisponibles.querySelector(`[data-id-grupo-opcion="${campoGrupoElegido.value}"]`);

        return {
            fecha: leerFechaHora(opcion.dataset.inicio),
            resultado: await enviar(`/Admin/AgregarEstudianteGrupo/${campoGrupoElegido.value}`,
                datosConToken({ IdEstudiante: buscador.valor })),
            exito: "Estudiante agregado al grupo."
        };
    }

    formNueva.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        ocultarAlerta(errorNueva);

        botonGuardar.disabled = true;

        const guardar = { individual: guardarIndividual, grupo: guardarGrupo, unirse: guardarUnirse }[modo];
        const envio = await guardar();

        botonGuardar.disabled = false;

        if (!envio) {
            mostrarAlerta(errorNueva, "Revise los campos marcados en rojo.");
            return;
        }

        if (!envio.resultado.ok) {
            mostrarAlerta(errorNueva, envio.resultado.mensaje);
            recargarOpcionesHorario();   // el horario o el cupo pudo ocuparse mientras tanto
            return;
        }

        modal("ModalNuevaCita").hide();
        notificar(envio.exito);

        fechaReferencia = soloFecha(envio.fecha);
        cargar();
    });


    // =====================================================
    // DETALLE DE CITA INDIVIDUAL
    // =====================================================

    const modalDetalle = document.getElementById("ModalDetalleCita");
    const mensajeDetalle = document.getElementById("DetalleCitaMensaje");
    const panelReprogramar = document.getElementById("PanelReprogramar");
    const panelCancelar = document.getElementById("PanelCancelar");
    const campoFechaReprogramar = document.getElementById("ReprogramarFecha");
    const campoHoraReprogramar = document.getElementById("HoraReprogramar");
    const espaciosReprogramar = document.getElementById("EspaciosReprogramar");
    const campoMotivo = document.getElementById("MotivoCancelacion");

    activarSeleccion(espaciosReprogramar, "data-inicio", campoHoraReprogramar, null);

    function cerrarPaneles() {
        panelReprogramar.classList.add("d-none");
        panelCancelar.classList.add("d-none");
    }

    function abrirDetalle(idCita) {
        const cita = citas.find(c => c.idCita === idCita);
        if (!cita) return;

        citaActual = cita;

        document.getElementById("DetalleCitaFecha").textContent = formatoFechaLarga(cita.inicio);
        document.getElementById("DetalleCitaHora").textContent = `${formatoHora(cita.inicio)} – ${formatoHora(cita.fin)}`;
        document.getElementById("DetalleCitaEstudiante").textContent = cita.estudiante;
        document.getElementById("DetalleCitaEncargado").textContent = valorTexto(cita.encargado);
        document.getElementById("DetalleCitaTipo").textContent = cita.tipoSesion;
        document.getElementById("DetalleCitaModalidad").textContent = cita.modalidad;
        document.getElementById("DetalleCitaObservaciones").textContent = valorTexto(cita.observaciones);

        const tieneMotivo = !!cita.motivoCancelacion;
        document.getElementById("EtiquetaMotivoCancelacion").classList.toggle("d-none", !tieneMotivo);
        document.getElementById("DetalleCitaMotivo").classList.toggle("d-none", !tieneMotivo);
        document.getElementById("DetalleCitaMotivo").textContent = cita.motivoCancelacion ?? "";

        const badge = document.getElementById("DetalleCitaEstado");
        badge.className = `badge align-middle ms-2 badge-cita-${claseEstado(cita)}`;
        badge.textContent = cita.estado;

        document.getElementById("BotonReprogramarCita").classList.toggle("d-none", !cita.puedeReprogramar);
        document.getElementById("BotonCancelarCita").classList.toggle("d-none", !cita.puedeCancelar);
        document.getElementById("BotonNoAsistio").classList.toggle("d-none", !cita.puedeMarcarNoAsistio);

        ocultarAlerta(mensajeDetalle);
        cerrarPaneles();

        modal("ModalDetalleCita").show();
    }

    function terminarAccion(idModal, mensaje, tipo, nuevaFecha) {
        modal(idModal).hide();
        notificar(mensaje, tipo);

        if (nuevaFecha) fechaReferencia = soloFecha(nuevaFecha);
        cargar();
    }

    modalDetalle.querySelectorAll("[data-cerrar-panel]").forEach(boton => {
        boton.addEventListener("click", cerrarPaneles);
    });

    document.getElementById("BotonReprogramarCita").addEventListener("click", function () {
        panelCancelar.classList.add("d-none");
        panelReprogramar.classList.remove("d-none");
        ocultarAlerta(mensajeDetalle);

        const hoy = soloFecha(new Date());
        const fechaCita = soloFecha(citaActual.inicio);

        campoFechaReprogramar.min = aIso(hoy);
        campoFechaReprogramar.value = aIso(fechaCita >= hoy ? fechaCita : hoy);

        cargarEspacios(espaciosReprogramar, campoHoraReprogramar,
            campoFechaReprogramar.value, citaActual.idTipoSesion, { idCita: citaActual.idCita });
    });

    campoFechaReprogramar.addEventListener("change", function () {
        cargarEspacios(espaciosReprogramar, campoHoraReprogramar,
            campoFechaReprogramar.value, citaActual.idTipoSesion, { idCita: citaActual.idCita });
    });

    document.getElementById("BotonConfirmarReprogramar").addEventListener("click", async function () {
        if (!campoHoraReprogramar.value) {
            espaciosReprogramar.classList.add("is-invalid");
            mostrarAlerta(mensajeDetalle, "Seleccione el nuevo horario.");
            return;
        }

        const nuevoInicio = leerFechaHora(campoHoraReprogramar.value);

        this.disabled = true;
        const resultado = await enviar(`/Admin/ReprogramarCita/${citaActual.idCita}`,
            datosConToken({ fechaHoraInicio: campoHoraReprogramar.value }));
        this.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(mensajeDetalle, resultado.mensaje);
            return;
        }

        terminarAccion("ModalDetalleCita", "Cita reprogramada correctamente.", "exito", nuevoInicio);
    });

    document.getElementById("BotonCancelarCita").addEventListener("click", function () {
        panelReprogramar.classList.add("d-none");
        panelCancelar.classList.remove("d-none");
        ocultarAlerta(mensajeDetalle);

        campoMotivo.value = "";
        campoMotivo.classList.remove("is-invalid");
        campoMotivo.focus();
    });

    campoMotivo.addEventListener("input", () => campoMotivo.classList.remove("is-invalid"));

    document.getElementById("BotonConfirmarCancelar").addEventListener("click", async function () {
        if (!marcar(campoMotivo, campoMotivo.value.trim() !== "")) return;

        this.disabled = true;
        const resultado = await enviar(`/Admin/CancelarCita/${citaActual.idCita}`,
            datosConToken({ motivo: campoMotivo.value.trim() }));
        this.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(mensajeDetalle, resultado.mensaje);
            return;
        }

        terminarAccion("ModalDetalleCita", "Cita cancelada.", "info");
    });

    document.getElementById("BotonNoAsistio").addEventListener("click", async function () {
        if (!confirm(`¿Marcar que ${citaActual.estudiante} no asistió a esta cita?`)) return;

        this.disabled = true;
        const resultado = await enviar(`/Admin/MarcarNoAsistioCita/${citaActual.idCita}`, datosConToken({}));
        this.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(mensajeDetalle, resultado.mensaje);
            return;
        }

        terminarAccion("ModalDetalleCita", "La cita quedó marcada como no asistió.", "info");
    });


    // =====================================================
    // DETALLE DE GRUPO
    // =====================================================

    const modalGrupo = document.getElementById("ModalDetalleGrupo");
    const mensajeGrupo = document.getElementById("GrupoMensaje");
    const cuerpoParticipantes = document.querySelector("#TablaParticipantes tbody");

    const panelesGrupo = {
        agregar: document.getElementById("PanelGrupoAgregar"),
        editar: document.getElementById("PanelGrupoEditar"),
        reprogramar: document.getElementById("PanelGrupoReprogramar"),
        cancelar: document.getElementById("PanelGrupoCancelar")
    };

    const campoEditarNombre = document.getElementById("EditarGrupoNombre");
    const campoEditarCupo = document.getElementById("EditarGrupoCupo");
    const campoFechaReprogramarGrupo = document.getElementById("ReprogramarGrupoFecha");
    const campoHoraReprogramarGrupo = document.getElementById("HoraReprogramarGrupo");
    const espaciosReprogramarGrupo = document.getElementById("EspaciosReprogramarGrupo");
    const campoMotivoGrupo = document.getElementById("MotivoCancelarGrupo");

    // A quien cancela el panel: "grupo" o el IdCita de un participante
    let objetivoCancelacion = "grupo";

    const buscadorGrupo = crearBuscadorEstudiantes(document.getElementById("BuscadorAgregarGrupo"), {
        url: "/Admin/BuscarEstudiantesCita",
        nombreCampo: "IdEstudianteAgregar",
        idEntrada: "EstudianteAgregarGrupo"
    });

    activarSeleccion(espaciosReprogramarGrupo, "data-inicio", campoHoraReprogramarGrupo, null);

    function mostrarPanelGrupo(nombre) {
        Object.entries(panelesGrupo).forEach(([clave, panel]) => {
            panel.classList.toggle("d-none", clave !== nombre);
        });
        ocultarAlerta(mensajeGrupo);
    }

    function renderDetalleGrupo(grupo) {
        const activos = inscritos(grupo);
        const hayCupo = activos.length < grupo.cupo;

        document.getElementById("GrupoTitulo").textContent = nombreGrupo(grupo);
        document.getElementById("GrupoCupo").textContent = `${activos.length}/${grupo.cupo}`;
        document.getElementById("GrupoSubtitulo").textContent =
            `${formatoFechaLarga(grupo.inicio)} · ${formatoHora(grupo.inicio)} – ${formatoHora(grupo.fin)} · ` +
            `${grupo.tipoSesion} · ${grupo.modalidad}`;

        cuerpoParticipantes.innerHTML = grupo.miembros.map(m => `
            <tr class="${m.idEstadoCita === ESTADO_CANCELADA ? "text-muted" : ""}">
                <td class="fw-semibold">${escaparHtml(m.estudiante)}</td>
                <td>${escaparHtml(valorTexto(m.encargado))}</td>
                <td><span class="badge badge-cita-${claseEstado(m)}">${escaparHtml(m.estado)}</span></td>
                <td class="text-end text-nowrap">
                    ${m.puedeCancelar ? `
                        <button type="button" class="btn btn-sm btn-outline-danger border-0"
                                data-cancelar-miembro="${m.idCita}" title="Cancelar solo a este estudiante">
                            <i class="bi bi-person-dash"></i>
                        </button>` : ""}
                    ${m.puedeMarcarNoAsistio ? `
                        <button type="button" class="btn btn-sm btn-outline-warning border-0"
                                data-no-asistio-miembro="${m.idCita}" title="Marcar como no asistió">
                            <i class="bi bi-person-x"></i>
                        </button>` : ""}
                </td>
            </tr>`).join("");

        // Acciones del grupo: solo si todavia no empieza y tiene a alguien pendiente
        document.getElementById("BotonGrupoAgregar").classList.toggle("d-none", !(grupo.puedeGestionar && hayCupo));
        document.getElementById("BotonGrupoEditar").classList.toggle("d-none", !grupo.puedeGestionar);
        document.getElementById("BotonGrupoReprogramar").classList.toggle("d-none", !grupo.puedeGestionar);
        document.getElementById("BotonGrupoCancelar").classList.toggle("d-none", !grupo.puedeGestionar);
    }

    function abrirDetalleGrupo(idGrupoCita) {
        const grupo = buscarGrupo(idGrupoCita);
        if (!grupo) return;

        idGrupoActual = idGrupoCita;
        renderDetalleGrupo(grupo);
        mostrarPanelGrupo(null);

        modal("ModalDetalleGrupo").show();
    }

    // Tras un cambio que no cierra el modal: recarga la agenda y repinta el grupo
    async function refrescarGrupo(mensaje) {
        await cargar();

        const grupo = buscarGrupo(idGrupoActual);

        if (!grupo) {
            modal("ModalDetalleGrupo").hide();
            notificar(mensaje);
            return;
        }

        renderDetalleGrupo(grupo);
        mostrarPanelGrupo(null);
        mostrarAlerta(mensajeGrupo, mensaje, "success");
    }

    modalGrupo.querySelectorAll("[data-cerrar-panel-grupo]").forEach(boton => {
        boton.addEventListener("click", () => mostrarPanelGrupo(null));
    });

    // ----- agregar participante -----

    document.getElementById("BotonGrupoAgregar").addEventListener("click", function () {
        mostrarPanelGrupo("agregar");
        buscadorGrupo.limpiar();
        buscadorGrupo.enfocar();
    });

    document.getElementById("BotonConfirmarAgregar").addEventListener("click", async function () {
        if (!buscadorGrupo.validar()) return;

        this.disabled = true;
        const resultado = await enviar(`/Admin/AgregarEstudianteGrupo/${idGrupoActual}`,
            datosConToken({ IdEstudiante: buscadorGrupo.valor }));
        this.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(mensajeGrupo, resultado.mensaje);
            return;
        }

        buscadorGrupo.limpiar();
        refrescarGrupo("Estudiante agregado al grupo.");
    });

    // ----- editar nombre y cupo -----

    document.getElementById("BotonGrupoEditar").addEventListener("click", function () {
        const grupo = buscarGrupo(idGrupoActual);

        campoEditarNombre.value = grupo.nombre ?? "";
        campoEditarCupo.value = grupo.cupo;
        campoEditarCupo.min = Math.max(2, inscritos(grupo).length);
        campoEditarCupo.classList.remove("is-invalid");

        mostrarPanelGrupo("editar");
        campoEditarNombre.focus();
    });

    document.getElementById("BotonConfirmarEditarGrupo").addEventListener("click", async function () {
        const grupo = buscarGrupo(idGrupoActual);
        const cupo = Number(campoEditarCupo.value) || 0;

        if (!marcar(campoEditarCupo, cupo >= 2 && cupo <= 30 && cupo >= inscritos(grupo).length)) return;

        this.disabled = true;
        const resultado = await enviar(`/Admin/EditarGrupoCita/${idGrupoActual}`,
            datosConToken({ nombre: campoEditarNombre.value.trim(), cupoMaximo: cupo }));
        this.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(mensajeGrupo, resultado.mensaje);
            return;
        }

        refrescarGrupo("Grupo actualizado.");
    });

    // ----- reprogramar el grupo completo -----

    function recargarEspaciosGrupo() {
        const grupo = buscarGrupo(idGrupoActual);

        cargarEspacios(espaciosReprogramarGrupo, campoHoraReprogramarGrupo,
            campoFechaReprogramarGrupo.value, grupo.idTipoSesion, { idGrupo: idGrupoActual });
    }

    document.getElementById("BotonGrupoReprogramar").addEventListener("click", function () {
        const grupo = buscarGrupo(idGrupoActual);
        const hoy = soloFecha(new Date());
        const fechaGrupo = soloFecha(grupo.inicio);

        campoFechaReprogramarGrupo.min = aIso(hoy);
        campoFechaReprogramarGrupo.value = aIso(fechaGrupo >= hoy ? fechaGrupo : hoy);

        mostrarPanelGrupo("reprogramar");
        recargarEspaciosGrupo();
    });

    campoFechaReprogramarGrupo.addEventListener("change", recargarEspaciosGrupo);

    document.getElementById("BotonConfirmarReprogramarGrupo").addEventListener("click", async function () {
        if (!campoHoraReprogramarGrupo.value) {
            espaciosReprogramarGrupo.classList.add("is-invalid");
            mostrarAlerta(mensajeGrupo, "Seleccione el nuevo horario.");
            return;
        }

        const nuevoInicio = leerFechaHora(campoHoraReprogramarGrupo.value);

        this.disabled = true;
        const resultado = await enviar(`/Admin/ReprogramarGrupoCita/${idGrupoActual}`,
            datosConToken({ fechaHoraInicio: campoHoraReprogramarGrupo.value }));
        this.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(mensajeGrupo, resultado.mensaje);
            return;
        }

        terminarAccion("ModalDetalleGrupo", "Grupo reprogramado correctamente.", "exito", nuevoInicio);
    });

    // ----- cancelar (grupo completo o un participante) -----

    function abrirPanelCancelar(objetivo, titulo) {
        objetivoCancelacion = objetivo;
        document.getElementById("TituloCancelarGrupo").textContent = titulo;

        campoMotivoGrupo.value = "";
        campoMotivoGrupo.classList.remove("is-invalid");

        mostrarPanelGrupo("cancelar");
        campoMotivoGrupo.focus();
    }

    document.getElementById("BotonGrupoCancelar").addEventListener("click", function () {
        abrirPanelCancelar("grupo", "Cancelar el grupo completo");
    });

    campoMotivoGrupo.addEventListener("input", () => campoMotivoGrupo.classList.remove("is-invalid"));

    document.getElementById("BotonConfirmarCancelarGrupo").addEventListener("click", async function () {
        if (!marcar(campoMotivoGrupo, campoMotivoGrupo.value.trim() !== "")) return;

        const motivo = campoMotivoGrupo.value.trim();
        const esGrupo = objetivoCancelacion === "grupo";

        this.disabled = true;
        const resultado = await enviar(
            esGrupo ? `/Admin/CancelarGrupoCita/${idGrupoActual}` : `/Admin/CancelarCita/${objetivoCancelacion}`,
            datosConToken({ motivo }));
        this.disabled = false;

        if (!resultado.ok) {
            mostrarAlerta(mensajeGrupo, resultado.mensaje);
            return;
        }

        if (esGrupo) {
            terminarAccion("ModalDetalleGrupo", "Grupo cancelado.", "info");
        } else {
            refrescarGrupo("Se canceló la cita del estudiante y se liberó su cupo.");
        }
    });

    // ----- acciones por participante -----

    cuerpoParticipantes.addEventListener("click", async function (evento) {
        const botonCancelar = evento.target.closest("[data-cancelar-miembro]");
        const botonNoAsistio = evento.target.closest("[data-no-asistio-miembro]");

        if (botonCancelar) {
            const cita = citas.find(c => c.idCita === Number(botonCancelar.dataset.cancelarMiembro));
            abrirPanelCancelar(cita.idCita, `Cancelar a ${cita.estudiante}`);
            return;
        }

        if (botonNoAsistio) {
            const cita = citas.find(c => c.idCita === Number(botonNoAsistio.dataset.noAsistioMiembro));
            if (!confirm(`¿Marcar que ${cita.estudiante} no asistió?`)) return;

            botonNoAsistio.disabled = true;
            const resultado = await enviar(`/Admin/MarcarNoAsistioCita/${cita.idCita}`, datosConToken({}));

            if (!resultado.ok) {
                botonNoAsistio.disabled = false;
                mostrarAlerta(mensajeGrupo, resultado.mensaje);
                return;
            }

            refrescarGrupo(`${cita.estudiante} quedó marcado como no asistió.`);
        }
    });


    // =====================================================
    // ARRANQUE Y REFRESCO
    // =====================================================

    cargar();

    // Cada 5 minutos se recarga (si no hay un modal abierto) para que las citas
    // que ya terminaron aparezcan como completadas sin recargar la pagina
    setInterval(function () {
        if (!hayModalAbierto() && document.visibilityState === "visible") cargar();
    }, REFRESCO_MS);

});
