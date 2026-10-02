document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    // =====================================================
    // CONSTANTES
    // =====================================================

    const DIAS = [
        { numero: 1, nombre: "Lunes" },
        { numero: 2, nombre: "Martes" },
        { numero: 3, nombre: "Miércoles" },
        { numero: 4, nombre: "Jueves" },
        { numero: 5, nombre: "Viernes" },
        { numero: 6, nombre: "Sábado" },
        { numero: 7, nombre: "Domingo" }
    ];
    const DIAS_A_COPIAR = [2, 3, 4, 5];               // martes a viernes
    const FRANJA_POR_DEFECTO = { horaInicio: "08:00", horaFin: "17:00" };
    const ULTIMA_HORA = 23 * 60 + 30;
    const CARGANDO =
        '<div class="text-center py-4 TextoSuave"><span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Cargando…</div>';
    const CERRADO = '<span class="TextoSuave small" data-cerrado>Cerrado</span>';


    // =====================================================
    // UTILIDADES
    // =====================================================

    // Tambien escapa comillas: se usa dentro de atributos value="..."
    function escaparHtml(texto) {
        const div = document.createElement("div");
        div.textContent = texto ?? "";
        return div.innerHTML.replaceAll('"', "&quot;");
    }

    function tokenAntifalsificacion() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    function datosConToken(campos) {
        const datos = new FormData();
        datos.append("__RequestVerificationToken", tokenAntifalsificacion());
        Object.entries(campos).forEach(([clave, valor]) => datos.append(clave, valor ?? ""));
        return datos;
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

    function ocupado(boton, activo) {
        boton.disabled = activo;
        boton.querySelector(".spinner-border")?.classList.toggle("d-none", !activo);
    }

    function marcar(campo, valido) {
        campo.classList.toggle("is-invalid", !valido);
        return valido;
    }

    function aIso(fecha) {
        return `${fecha.getFullYear()}-${String(fecha.getMonth() + 1).padStart(2, "0")}-${String(fecha.getDate()).padStart(2, "0")}`;
    }

    function formatoFechaLarga(iso) {
        const [anio, mes, dia] = iso.split("-").map(Number);
        const texto = new Date(anio, mes - 1, dia).toLocaleDateString("es-CR", {
            weekday: "long", day: "numeric", month: "long", year: "numeric"
        });
        return texto.charAt(0).toUpperCase() + texto.slice(1);
    }

    function formatoFechaHora(texto) {
        return new Date(texto).toLocaleString("es-CR", {
            weekday: "short", day: "numeric", month: "short", hour: "numeric", minute: "2-digit"
        });
    }

    function minutos(hora) {
        const [h, m] = hora.split(":").map(Number);
        return h * 60 + m;
    }

    function aHora(total) {
        const valor = Math.min(Math.max(total, 0), ULTIMA_HORA);
        return `${String(Math.floor(valor / 60)).padStart(2, "0")}:${String(valor % 60).padStart(2, "0")}`;
    }

    async function leerJson(respuesta) {
        try {
            return await respuesta.json();
        } catch {
            return null;
        }
    }

    async function obtener(url) {
        const respuesta = await fetch(url, { headers: { "Accept": "application/json" } });
        const json = await leerJson(respuesta);

        if (!respuesta.ok) throw new Error(json?.mensaje || "No se pudo cargar la información.");

        return json ?? [];
    }

    async function enviar(url, datos) {
        try {
            const respuesta = await fetch(url, { method: "POST", body: datos });
            const json = await leerJson(respuesta);

            return respuesta.ok
                ? { ok: true, datos: json }
                : { ok: false, mensaje: json?.mensaje || "No se pudo completar la operación." };
        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }


    // =====================================================
    // 1. HORARIO DE ATENCION
    // =====================================================

    const listaHorario = document.getElementById("ListaHorario");
    const alertaHorario = document.getElementById("AlertaHorario");
    const citasFueraHorario = document.getElementById("CitasFueraHorario");
    const botonGuardarHorario = document.getElementById("BotonGuardarHorario");

    function htmlFranja(franja) {
        return `
            <div class="HorarioFranja" data-franja>
                <input type="time" class="form-control form-control-sm" step="1800" value="${franja.horaInicio}"
                       data-inicio aria-label="Hora de apertura">
                <span class="TextoSuave small">a</span>
                <input type="time" class="form-control form-control-sm" step="1800" value="${franja.horaFin}"
                       data-fin aria-label="Hora de cierre">
                <button type="button" class="btn btn-sm btn-outline-danger" data-quitar-franja aria-label="Quitar franja">
                    <i class="bi bi-trash" aria-hidden="true"></i>
                </button>
            </div>`;
    }

    function htmlDia(dia, franjas) {
        const abierto = franjas.length > 0;

        return `
            <div class="HorarioDia" data-dia="${dia.numero}">
                <div class="form-check form-switch HorarioDia-Nombre">
                    <input class="form-check-input" type="checkbox" role="switch" id="Abierto${dia.numero}"
                           data-abierto ${abierto ? "checked" : ""}>
                    <label class="form-check-label fw-semibold" for="Abierto${dia.numero}">${dia.nombre}</label>
                </div>
                <div class="HorarioDia-Franjas" data-franjas>
                    ${abierto ? franjas.map(htmlFranja).join("") : CERRADO}
                </div>
                <button type="button" class="btn btn-sm btn-link text-decoration-none" data-agregar-franja
                        ${abierto ? "" : "disabled"} aria-label="Agregar franja al ${dia.nombre.toLowerCase()}">
                    <i class="bi bi-plus-lg me-1" aria-hidden="true"></i>Franja
                </button>
            </div>`;
    }

    function dibujarHorario(franjas) {
        listaHorario.innerHTML = DIAS
            .map(dia => htmlDia(dia, franjas.filter(f => f.diaSemana === dia.numero)))
            .join("");
    }

    function filaDia(numero) {
        return listaHorario.querySelector(`[data-dia="${numero}"]`);
    }

    function franjasDe(fila) {
        return [...fila.querySelectorAll("[data-franja]")].map(franja => ({
            horaInicio: franja.querySelector("[data-inicio]").value,
            horaFin: franja.querySelector("[data-fin]").value
        }));
    }

    function reemplazarDia(numero, franjas) {
        const dia = DIAS.find(d => d.numero === numero);
        filaDia(numero).outerHTML = htmlDia(dia, franjas);
    }

    // La franja nueva arranca una hora despues de la anterior (ej.: 08-12 -> 13-16)
    function agregarFranja(fila) {
        const ultima = franjasDe(fila).at(-1);
        const nueva = ultima?.horaFin
            ? { horaInicio: aHora(minutos(ultima.horaFin) + 60), horaFin: aHora(minutos(ultima.horaFin) + 240) }
            : { ...FRANJA_POR_DEFECTO };

        const contenedorFranjas = fila.querySelector("[data-franjas]");
        contenedorFranjas.querySelector("[data-cerrado]")?.remove();
        contenedorFranjas.insertAdjacentHTML("beforeend", htmlFranja(nueva));
        contenedorFranjas.querySelector("[data-franja]:last-child [data-inicio]").focus();
    }

    listaHorario.addEventListener("change", function (evento) {
        const interruptor = evento.target.closest("[data-abierto]");
        if (!interruptor) return;

        const numero = Number(interruptor.closest("[data-dia]").dataset.dia);
        reemplazarDia(numero, interruptor.checked ? [{ ...FRANJA_POR_DEFECTO }] : []);
        filaDia(numero).querySelector("[data-abierto]").focus();
    });

    listaHorario.addEventListener("click", function (evento) {
        const fila = evento.target.closest("[data-dia]");
        if (!fila) return;

        if (evento.target.closest("[data-agregar-franja]")) {
            agregarFranja(fila);
            return;
        }

        const quitar = evento.target.closest("[data-quitar-franja]");
        if (quitar) {
            quitar.closest("[data-franja]").remove();

            // Sin franjas el dia queda cerrado
            if (!fila.querySelector("[data-franja]")) {
                reemplazarDia(Number(fila.dataset.dia), []);
            }
        }
    });

    document.getElementById("BotonCopiarLunes").addEventListener("click", function () {
        const lunes = franjasDe(filaDia(1));

        if (lunes.length === 0) {
            mostrarAlerta(alertaHorario, "El lunes está cerrado: defina sus franjas antes de copiarlas.", "warning");
            return;
        }

        DIAS_A_COPIAR.forEach(numero => reemplazarDia(numero, lunes));
        ocultarAlerta(alertaHorario);
        notificar("Se copió el horario del lunes de martes a viernes. Recuerde guardar.", "info");
    });

    function leerHorario() {
        const franjas = [];
        let valido = true;

        listaHorario.querySelectorAll("[data-dia]").forEach(fila => {
            if (!fila.querySelector("[data-abierto]").checked) return;

            fila.querySelectorAll("[data-franja]").forEach(franja => {
                const inicio = franja.querySelector("[data-inicio]");
                const fin = franja.querySelector("[data-fin]");
                const correcta = inicio.value !== "" && fin.value !== "" && fin.value > inicio.value;

                marcar(inicio, correcta);
                marcar(fin, correcta);
                valido = valido && correcta;

                franjas.push({ diaSemana: Number(fila.dataset.dia), horaInicio: inicio.value, horaFin: fin.value });
            });
        });

        return { franjas, valido };
    }

    function mostrarCitasFuera(lista) {
        if (lista.length === 0) {
            citasFueraHorario.innerHTML = "";
            return;
        }

        citasFueraHorario.innerHTML = `
            <div class="alert alert-warning mt-3 mb-0">
                <div class="fw-semibold mb-2">
                    <i class="bi bi-exclamation-triangle me-1" aria-hidden="true"></i>
                    Estas citas quedaron fuera del nuevo horario y conviene reprogramarlas:
                </div>
                <ul class="mb-2">
                    ${lista.map(c => `<li>${escaparHtml(formatoFechaHora(c.fechaHoraInicio))} · ${escaparHtml(c.estudiante)}${c.idGrupoCita ? " (grupo)" : ""}</li>`).join("")}
                </ul>
                <a href="/Admin/Agenda" class="alert-link">Ir a la agenda</a>
            </div>`;
    }

    botonGuardarHorario.addEventListener("click", async function () {
        ocultarAlerta(alertaHorario);
        mostrarCitasFuera([]);

        const { franjas, valido } = leerHorario();

        if (!valido) {
            mostrarAlerta(alertaHorario, "Revise las franjas marcadas: la hora de cierre debe ser posterior a la de apertura.");
            return;
        }

        if (franjas.length === 0) {
            mostrarAlerta(alertaHorario, "Abra al menos un día: sin horario no se pueden agendar citas.");
            return;
        }

        // Nombres indexados para que MVC arme la lista Franjas
        const datos = datosConToken({});
        franjas.forEach((franja, i) => {
            datos.append(`Franjas[${i}].DiaSemana`, franja.diaSemana);
            datos.append(`Franjas[${i}].HoraInicio`, franja.horaInicio);
            datos.append(`Franjas[${i}].HoraFin`, franja.horaFin);
        });

        ocupado(botonGuardarHorario, true);
        const resultado = await enviar("/Admin/GuardarHorario", datos);
        ocupado(botonGuardarHorario, false);

        if (!resultado.ok) {
            mostrarAlerta(alertaHorario, resultado.mensaje);
            return;
        }

        const fuera = resultado.datos?.citasFueraDeHorario ?? [];
        notificar(resultado.datos?.mensaje || "Horario guardado correctamente.", fuera.length ? "info" : "exito");
        mostrarCitasFuera(fuera);
    });

    async function cargarHorario() {
        listaHorario.innerHTML = CARGANDO;

        try {
            dibujarHorario(await obtener("/Admin/ConfiguracionHorario"));
        } catch (error) {
            listaHorario.innerHTML = "";
            botonGuardarHorario.disabled = true;      // sin datos no se debe sobrescribir el horario
            mostrarAlerta(alertaHorario, error.message);
        }
    }


    // =====================================================
    // 2. DIAS NO LABORALES
    // =====================================================

    const formDia = document.getElementById("FormularioDiaNoLaboral");
    const campoFechaDia = document.getElementById("FechaNoLaboral");
    const campoMotivo = document.getElementById("MotivoNoLaboral");
    const campoFeriado = document.getElementById("EsFeriado");
    const botonAgregarDia = document.getElementById("BotonAgregarDia");
    const tablaDias = document.getElementById("TablaDiasNoLaborales");
    const alertaDias = document.getElementById("AlertaDias");
    const modalEliminarDia = bootstrap.Modal.getOrCreateInstance(document.getElementById("ModalEliminarDia"));
    const botonConfirmarEliminarDia = document.getElementById("BotonConfirmarEliminarDia");

    let idDiaPorEliminar = null;

    campoFechaDia.min = aIso(new Date());

    function dibujarDias(lista) {
        if (lista.length === 0) {
            tablaDias.innerHTML =
                '<tr><td colspan="4" class="text-center TextoSuave py-4">No hay días no laborales próximos.</td></tr>';
            return;
        }

        tablaDias.innerHTML = lista.map(dia => `
            <tr>
                <td>${escaparHtml(formatoFechaLarga(dia.fecha))}</td>
                <td>${escaparHtml(dia.motivo)}</td>
                <td>
                    <span class="badge ${dia.esFeriado ? "text-bg-info" : "text-bg-secondary"}">
                        ${dia.esFeriado ? "Feriado" : "No laboral"}
                    </span>
                </td>
                <td class="text-end">
                    <button type="button" class="btn btn-sm btn-outline-danger"
                            data-eliminar-dia="${dia.idDiaNoLaboral}" data-fecha="${dia.fecha}"
                            aria-label="Quitar ${escaparHtml(formatoFechaLarga(dia.fecha))}">
                        <i class="bi bi-trash" aria-hidden="true"></i>
                    </button>
                </td>
            </tr>`).join("");
    }

    async function cargarDias() {
        tablaDias.innerHTML = `<tr><td colspan="4">${CARGANDO}</td></tr>`;

        try {
            dibujarDias(await obtener("/Admin/ConfiguracionDiasNoLaborales"));
        } catch (error) {
            tablaDias.innerHTML = "";
            mostrarAlerta(alertaDias, error.message);
        }
    }

    formDia.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        ocultarAlerta(alertaDias);

        const fechaValida = marcar(campoFechaDia, campoFechaDia.value !== "" && campoFechaDia.value >= campoFechaDia.min);
        const motivoValido = marcar(campoMotivo, campoMotivo.value.trim() !== "");

        if (!fechaValida || !motivoValido) return;

        ocupado(botonAgregarDia, true);
        const resultado = await enviar("/Admin/AgregarDiaNoLaboral", datosConToken({
            fecha: campoFechaDia.value,
            motivo: campoMotivo.value.trim(),
            esFeriado: campoFeriado.checked
        }));
        ocupado(botonAgregarDia, false);

        if (!resultado.ok) {
            mostrarAlerta(alertaDias, resultado.mensaje);
            return;
        }

        formDia.reset();
        notificar(resultado.datos?.mensaje || "Día no laboral agregado.", "exito");
        cargarDias();
    });

    tablaDias.addEventListener("click", function (evento) {
        const boton = evento.target.closest("[data-eliminar-dia]");
        if (!boton) return;

        idDiaPorEliminar = Number(boton.dataset.eliminarDia);
        document.getElementById("TextoEliminarDia").textContent = formatoFechaLarga(boton.dataset.fecha);
        modalEliminarDia.show();
    });

    botonConfirmarEliminarDia.addEventListener("click", async function () {
        ocultarAlerta(alertaDias);

        ocupado(botonConfirmarEliminarDia, true);
        const resultado = await enviar("/Admin/EliminarDiaNoLaboral", datosConToken({ id: idDiaPorEliminar }));
        ocupado(botonConfirmarEliminarDia, false);
        modalEliminarDia.hide();

        if (!resultado.ok) {
            mostrarAlerta(alertaDias, resultado.mensaje);
            return;
        }

        notificar(resultado.datos?.mensaje || "Día no laboral eliminado.", "exito");
        cargarDias();
    });


    // =====================================================
    // 3. TIPOS DE SESION
    // =====================================================

    const tablaTipos = document.getElementById("TablaTiposSesion");
    const alertaTipos = document.getElementById("AlertaTipos");
    const campoNombreNuevo = document.getElementById("NombreTipoNuevo");
    const campoDuracionNuevo = document.getElementById("DuracionTipoNuevo");
    const botonAgregarTipo = document.getElementById("BotonAgregarTipo");

    function dibujarTipos(lista) {
        if (lista.length === 0) {
            tablaTipos.innerHTML =
                '<tr><td colspan="3" class="text-center TextoSuave py-4">No hay tipos de sesión.</td></tr>';
            return;
        }

        tablaTipos.innerHTML = lista.map(tipo => `
            <tr data-tipo="${tipo.idTipoSesion}">
                <td>
                    <input type="text" class="form-control form-control-sm" maxlength="50"
                           value="${escaparHtml(tipo.nombre)}" data-nombre aria-label="Nombre del tipo de sesión">
                </td>
                <td>
                    <input type="number" class="form-control form-control-sm" min="15" max="240" step="15"
                           value="${tipo.duracionMinutos}" data-duracion aria-label="Duración en minutos">
                </td>
                <td class="text-end">
                    <button type="button" class="btn btn-sm btn-outline-primary" data-guardar-tipo>
                        <span class="spinner-border spinner-border-sm me-1 d-none" aria-hidden="true"></span>Guardar
                    </button>
                </td>
            </tr>`).join("");
    }

    async function cargarTipos() {
        tablaTipos.innerHTML = `<tr><td colspan="3">${CARGANDO}</td></tr>`;

        try {
            dibujarTipos(await obtener("/Admin/ConfiguracionTiposSesion"));
        } catch (error) {
            tablaTipos.innerHTML = "";
            mostrarAlerta(alertaTipos, error.message);
        }
    }

    function leerTipo(campoNombre, campoDuracion) {
        const nombre = campoNombre.value.trim();
        const duracion = Number(campoDuracion.value);

        const nombreValido = marcar(campoNombre, nombre !== "");
        const duracionValida = marcar(campoDuracion, Number.isInteger(duracion) && duracion >= 15 && duracion <= 240);

        return nombreValido && duracionValida ? { nombre, duracionMinutos: duracion } : null;
    }

    async function guardarTipo(idTipoSesion, tipo, boton) {
        ocultarAlerta(alertaTipos);

        ocupado(boton, true);
        const resultado = await enviar("/Admin/GuardarTipoSesion", datosConToken({ idTipoSesion, ...tipo }));
        ocupado(boton, false);

        if (!resultado.ok) {
            mostrarAlerta(alertaTipos, resultado.mensaje);
            return false;
        }

        notificar(resultado.datos?.mensaje || "Tipo de sesión guardado.", "exito");
        return true;
    }

    tablaTipos.addEventListener("click", async function (evento) {
        const boton = evento.target.closest("[data-guardar-tipo]");
        if (!boton) return;

        const fila = boton.closest("[data-tipo]");
        const tipo = leerTipo(fila.querySelector("[data-nombre]"), fila.querySelector("[data-duracion]"));

        if (tipo) await guardarTipo(Number(fila.dataset.tipo), tipo, boton);
    });

    botonAgregarTipo.addEventListener("click", async function () {
        const tipo = leerTipo(campoNombreNuevo, campoDuracionNuevo);
        if (!tipo) return;

        // idTipoSesion vacio = crear
        if (await guardarTipo(null, tipo, botonAgregarTipo)) {
            campoNombreNuevo.value = "";
            campoDuracionNuevo.value = "60";
            cargarTipos();
        }
    });


    // =====================================================
    // 4. DATOS DEL CONSULTORIO
    // =====================================================

    const formConsultorio = document.getElementById("FormularioConsultorio");
    const alertaConsultorio = document.getElementById("AlertaConsultorio");
    const botonGuardarConsultorio = document.getElementById("BotonGuardarConsultorio");

    // propiedad del JSON -> id del campo
    const CAMPOS_CONSULTORIO = {
        nombreConsultorio: "NombreConsultorio",
        correoContacto: "CorreoContacto",
        telefonoContacto: "TelefonoContacto",
        direccionConsultorio: "DireccionConsultorio",
        horasRecordatorioCita: "HorasRecordatorioCita",
        moneda: "Moneda"
    };

    async function cargarConsultorio() {
        try {
            const datos = await obtener("/Admin/ConfiguracionConsultorio");

            Object.entries(CAMPOS_CONSULTORIO).forEach(([propiedad, id]) => {
                document.getElementById(id).value = datos[propiedad] ?? "";
            });
        } catch (error) {
            botonGuardarConsultorio.disabled = true;
            mostrarAlerta(alertaConsultorio, error.message);
        }
    }

    formConsultorio.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        ocultarAlerta(alertaConsultorio);

        formConsultorio.classList.add("was-validated");
        if (!formConsultorio.checkValidity()) return;

        const datos = new FormData(formConsultorio);
        datos.set("__RequestVerificationToken", tokenAntifalsificacion());

        ocupado(botonGuardarConsultorio, true);
        const resultado = await enviar("/Admin/GuardarConsultorio", datos);
        ocupado(botonGuardarConsultorio, false);

        if (!resultado.ok) {
            mostrarAlerta(alertaConsultorio, resultado.mensaje);
            return;
        }

        formConsultorio.classList.remove("was-validated");
        notificar(resultado.datos?.mensaje || "Datos guardados correctamente.", "exito");
        cargarConsultorio();          // muestra el telefono ya normalizado
    });


    // =====================================================
    // INICIO
    // =====================================================

    cargarHorario();
    cargarDias();
    cargarTipos();
    cargarConsultorio();
});