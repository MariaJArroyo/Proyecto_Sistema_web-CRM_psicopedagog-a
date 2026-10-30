document.addEventListener("DOMContentLoaded", function () {
 
    // =====================================================
    // REFERENCIAS Y CONSTANTES
    // =====================================================
 
    const buscador = document.querySelector("[data-buscar-texto]");
    const filtroEstado = document.querySelector("[data-buscar-estado]");
    const tabla = document.getElementById("TablaClientes");
    const sinResultados = document.getElementById("SinResultados");
 
    const PatronCorreo = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
 
    // Aspecto de cada estado de TB_ESTADO_CLIENTE. Se decide por el Id, no por el
    // nombre: si el nombre cambia en la base, el color se mantiene.
    // Mismo criterio que Models/EstiloEstadoCliente.cs
    const ESTADO_ACTIVO = 4;
    const ESTADO_INACTIVO = 5;
   
    const ESTILOS_ESTADO = {
        1: { clase: "chip-estado-nuevo", icono: "bi-stars" },
        2: { clase: "chip-estado-contactado", icono: "bi-telephone" },
        3: { clase: "chip-estado-cita", icono: "bi-calendar-check" },
        4: { clase: "chip-estado-activo", icono: "bi-check-circle" },
        5: { clase: "chip-estado-inactivo", icono: "bi-pause-circle" }
    };
 
    const ESTILO_POR_DEFECTO = { clase: "chip-estado-otro", icono: "bi-circle" };
 
 
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
 
    function chipEstado(idEstado, nombre) {
        const estilo = ESTILOS_ESTADO[idEstado] ?? ESTILO_POR_DEFECTO;
        return `<span class="ChipEstado ${estilo.clase}"><i class="bi ${estilo.icono}" aria-hidden="true"></i>${escaparHtml(valorTexto(nombre))}</span>`;
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
    // FILTRO Y PAGINACION
    // =====================================================
 
    const paginacion = document.getElementById("PaginacionClientes");
    const tamanoPagina = document.getElementById("TamanoPaginaClientes");
    const listaPaginas = document.getElementById("ListaPaginasClientes");
    const resumenPaginacion = document.getElementById("ResumenPaginacionClientes");
 
    let paginaActual = 1;
 
 // El filtro de estado puede traer uno o varios Ids separados por coma ("1,4")
    function filasCoincidentes(filas) {
        const texto = (buscador?.value ?? "").toLowerCase().trim();
        const filtro = filtroEstado?.value ?? "";
        const idsEstado = filtro === "" ? [] : filtro.split(",");

        return filas.filter(function (fila) {
            const cumpleEstado = idsEstado.length === 0 || idsEstado.includes(fila.dataset.idEstado);

            return cumpleEstado && (texto === "" || fila.textContent.toLowerCase().includes(texto));
        });
    }
 
    // Numeros a mostrar: 1 … 4 5 6 … 12
    function numerosDePagina(total, actual) {
        const numeros = [...new Set([1, actual - 1, actual, actual + 1, total])]
            .filter(n => n >= 1 && n <= total)
            .sort((a, b) => a - b);
 
        const resultado = [];
        numeros.forEach(function (numero, i) {
            if (i > 0 && numero - numeros[i - 1] > 1) resultado.push("…");
            resultado.push(numero);
        });
        return resultado;
    }
 
    function botonPagina(contenido, pagina, { activo = false, deshabilitado = false, etiqueta = "" } = {}) {
        return `
            <li class="page-item${activo ? " active" : ""}${deshabilitado ? " disabled" : ""}">
                <button type="button" class="page-link" data-pagina="${pagina}"
                        ${etiqueta ? `aria-label="${etiqueta}"` : ""}
                        ${activo ? 'aria-current="page"' : ""}
                        ${deshabilitado ? "disabled" : ""}>${contenido}</button>
            </li>`;
    }
 
    function actualizarTabla() {
 
        if (!tabla) return;
 
        const filas = [...tabla.querySelectorAll("tbody tr[data-fila]")];
        const coincidentes = filasCoincidentes(filas);
        const tamano = Number(tamanoPagina?.value) || 10;
        const totalPaginas = Math.max(1, Math.ceil(coincidentes.length / tamano));
 
        paginaActual = Math.min(Math.max(paginaActual, 1), totalPaginas);
 
        const inicio = (paginaActual - 1) * tamano;
        const visibles = new Set(coincidentes.slice(inicio, inicio + tamano));
 
        filas.forEach(fila => { fila.style.display = visibles.has(fila) ? "" : "none"; });
 
        if (sinResultados) {
            sinResultados.style.display = filas.length > 0 && coincidentes.length === 0 ? "block" : "none";
        }
 
        if (!paginacion) return;
 
        paginacion.classList.toggle("d-none", coincidentes.length === 0);
        resumenPaginacion.textContent =
            `${inicio + 1}–${Math.min(inicio + tamano, coincidentes.length)} de ${coincidentes.length} ` +
            (coincidentes.length === 1 ? "cliente" : "clientes");
 
        // Con una sola pagina no hace falta la botonera
        listaPaginas.closest("nav").classList.toggle("d-none", totalPaginas === 1);
 
        listaPaginas.innerHTML =
            botonPagina('<i class="bi bi-chevron-left" aria-hidden="true"></i>', paginaActual - 1,
                { deshabilitado: paginaActual === 1, etiqueta: "Página anterior" }) +
            numerosDePagina(totalPaginas, paginaActual).map(numero => numero === "…"
                ? '<li class="page-item disabled"><span class="page-link">…</span></li>'
                : botonPagina(numero, numero, { activo: numero === paginaActual, etiqueta: `Página ${numero}` })).join("") +
            botonPagina('<i class="bi bi-chevron-right" aria-hidden="true"></i>', paginaActual + 1,
                { deshabilitado: paginaActual === totalPaginas, etiqueta: "Página siguiente" });
    }
 
    // Cualquier cambio de filtro vuelve a la primera pagina
    function reiniciarYActualizar() {
        paginaActual = 1;
        actualizarTabla();
    }
 
    if (buscador) buscador.addEventListener("input", reiniciarYActualizar);
    if (filtroEstado) filtroEstado.addEventListener("change", reiniciarYActualizar);
    if (tamanoPagina) tamanoPagina.addEventListener("change", reiniciarYActualizar);
 
    if (listaPaginas) {
        listaPaginas.addEventListener("click", function (evento) {
            const boton = evento.target.closest("button[data-pagina]");
            if (!boton || boton.disabled) return;
 
            paginaActual = Number(boton.dataset.pagina);
            actualizarTabla();
            listaPaginas.querySelector('[aria-current="page"]')?.focus();
        });
    }
 
    actualizarTabla();
 
 
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
 
        // Un encargado desactivado se muestra como Inactivo aunque conserve su estado anterior
        document.getElementById("DetalleEstado").innerHTML = cliente.activo === false
            ? chipEstado(ESTADO_INACTIVO, "Inactivo")
            : chipEstado(cliente.idEstadoCliente, cliente.estado);
 
        const inactivo = !cliente.activo;
        document.getElementById("BotonNuevoEstudianteCliente").disabled = inactivo;
        document.getElementById("AvisoClienteInactivo").classList.toggle("d-none", !inactivo);
 
        renderEstudiantes(cliente.estudiantes || []);
        cargarPagosCliente(cliente);
    }
 
    function renderEstudiantes(estudiantes) {
 
        document.getElementById("DetalleCantidadEstudiantes").textContent = estudiantes.length;
        document.getElementById("SinEstudiantesCliente").classList.toggle("d-none", estudiantes.length > 0);
 
        cuerpoEstudiantes.innerHTML = estudiantes.map(function (est) {
 
            const estado = est.activo
                ? chipEstado(ESTADO_ACTIVO, "Activo")
                : chipEstado(ESTADO_INACTIVO, "Inactivo");
 
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
 
    // =====================================================
    // HISTORIAL DE PAGOS DEL ENCARGADO
    // =====================================================
    // Cobros de todos sus estudiantes, de citas y manuales. Usa el mismo listado
    // de la pantalla de Pagos y se queda con los de sus estudiantes.

    const ESTADOS_PAGO = {
        1: { nombre: "Pagado", clase: "chip-pago-pagado", icono: "bi-check-circle" },
        2: { nombre: "Pendiente", clase: "chip-pago-pendiente", icono: "bi-hourglass-split" },
        3: { nombre: "Vencido", clase: "chip-pago-vencido", icono: "bi-exclamation-circle" },
        4: { nombre: "Parcial", clase: "chip-pago-parcial", icono: "bi-pie-chart" },
        5: { nombre: "Anulado", clase: "chip-pago-anulado", icono: "bi-x-circle" }
    };
    const PAGO_ANULADO = 5;
    const PAGOS_CON_SALDO = [2, 3, 4];

    // Orden del historial: lo mas urgente primero.
    // Vencido (3), Pendiente (2), Parcial (4), Pagado (1) y al final Anulado (5)
    const ORDEN_ESTADO_PAGO = { 3: 0, 2: 1, 4: 2, 1: 3, 5: 4 };

    const moneda = new Intl.NumberFormat("es-CR", { style: "currency", currency: "CRC", maximumFractionDigits: 0 });
    const filtroEstudiantePagos = document.getElementById("FiltroEstudiantePagos");
    const cuerpoPagosCliente = document.querySelector("#TablaPagosCliente tbody");
    const resumenPagosCliente = document.getElementById("ResumenPagosCliente");

    let pagosCliente = [];
    let numeroConsultaPagos = 0;     // descarta respuestas viejas si se abre otro encargado

    function dinero(valor) {
        return moneda.format(Number(valor) || 0);
    }

    // El API manda fechas sin hora como "2026-10-30T00:00:00"
    function fechaCorta(texto) {
        if (!texto) return "-";
        const [anio, mes, dia] = String(texto).slice(0, 10).split("-").map(Number);
        return new Date(anio, mes - 1, dia).toLocaleDateString("es-CR", { day: "numeric", month: "short", year: "numeric" });
    }

    function chipPago(idEstado) {
        const estado = ESTADOS_PAGO[idEstado] ?? { nombre: "-", clase: "chip-estado-otro", icono: "bi-circle" };
        return `<span class="ChipEstado ${estado.clase}"><i class="bi ${estado.icono}" aria-hidden="true"></i>${estado.nombre}</span>`;
    }

    function mensajeTablaPagos(texto, clase = "TextoSuave") {
        cuerpoPagosCliente.innerHTML = `<tr><td colspan="7" class="text-center ${clase} py-3">${escaparHtml(texto)}</td></tr>`;
    }

    async function cargarPagosCliente(cliente) {
        const estudiantes = cliente.estudiantes || [];
        const ids = new Set(estudiantes.map(e => e.idEstudiante));
        const numero = ++numeroConsultaPagos;

        // El filtro se llena con los estudiantes del encargado (conserva la eleccion al refrescar)
        const anterior = filtroEstudiantePagos.value;
        filtroEstudiantePagos.innerHTML = '<option value="">Todos los estudiantes</option>' +
            estudiantes.map(e => `<option value="${e.idEstudiante}">${escaparHtml(`${e.nombre} ${e.apellido}`)}</option>`).join("");
        filtroEstudiantePagos.value = [...filtroEstudiantePagos.options].some(o => o.value === anterior) ? anterior : "";
        filtroEstudiantePagos.disabled = estudiantes.length < 2;

        resumenPagosCliente.textContent = "";
        mensajeTablaPagos("Cargando pagos…");

        try {
            const respuesta = await fetch("/Admin/ConsultarPagos", { headers: { "Accept": "application/json" } });
            if (!respuesta.ok) throw new Error();

            const datos = await respuesta.json();
            if (numero !== numeroConsultaPagos) return;

            pagosCliente = (datos?.pagos ?? []).filter(p => ids.has(p.idEstudiante));
        } catch {
            if (numero !== numeroConsultaPagos) return;

            pagosCliente = [];
            pintarContadorPagos();
            mensajeTablaPagos("No se pudo cargar el historial de pagos.", "text-danger");
            return;
        }

        pintarPagosCliente();
    }

    function pintarContadorPagos() {
        const contador = document.getElementById("ContadorPagosCliente");
        const conSaldo = pagosCliente.filter(p => PAGOS_CON_SALDO.includes(p.idEstadoPago));
        const hayVencidos = conSaldo.some(p => p.idEstadoPago === 3);

        contador.textContent = conSaldo.length;
        contador.title = `${conSaldo.length} ${conSaldo.length === 1 ? "cobro con saldo" : "cobros con saldo"}`;
        contador.classList.toggle("d-none", conSaldo.length === 0);
        contador.classList.toggle("ContadorPagosCliente-Vencido", hayVencidos);
    }

    // "Ir a Pagos" abre la pantalla de Pagos filtrada por el estudiante elegido,
    // o por todos los estudiantes del encargado si no hay uno elegido
    function actualizarEnlacePagos() {
        const enlace = document.getElementById("EnlacePagosCliente");
        const estudiantes = clienteActual?.estudiantes || [];
        const idElegido = filtroEstudiantePagos.value;
        const elegido = estudiantes.find(e => String(e.idEstudiante) === idElegido);

        if (!enlace || estudiantes.length === 0) {
            if (enlace) enlace.href = "/Admin/Pagos";
            return;
        }

        const ids = elegido ? [elegido.idEstudiante] : estudiantes.map(e => e.idEstudiante);
        const etiqueta = elegido
            ? `${elegido.nombre} ${elegido.apellido}`
            : `Estudiantes de ${clienteActual.nombreEncargado ?? ""} ${clienteActual.apellidoEncargado ?? ""}`.trim();

        enlace.href = `/Admin/Pagos?idEstudiantes=${ids.join(",")}&filtro=${encodeURIComponent(etiqueta)}`;
    }

    function pintarPagosCliente() {
        pintarContadorPagos();
        actualizarEnlacePagos();

        const idEstudiante = filtroEstudiantePagos.value;
        // Dentro de cada estado se conserva el orden del API: el mas reciente primero
        const lista = pagosCliente
            .filter(p => !idEstudiante || String(p.idEstudiante) === idEstudiante)
            .map((p, posicion) => ({ p, posicion }))
            .sort((a, b) => (ORDEN_ESTADO_PAGO[a.p.idEstadoPago] ?? 9) - (ORDEN_ESTADO_PAGO[b.p.idEstadoPago] ?? 9)
                || a.posicion - b.posicion)
            .map(x => x.p);

        // Los anulados se listan pero no suman
        const vigentes = lista.filter(p => p.idEstadoPago !== PAGO_ANULADO);
        const facturado = vigentes.reduce((t, p) => t + Number(p.monto || 0), 0);
        const pagado = vigentes.reduce((t, p) => t + Number(p.abonado || 0), 0);
        const saldo = vigentes.filter(p => PAGOS_CON_SALDO.includes(p.idEstadoPago))
            .reduce((t, p) => t + Number(p.saldo || 0), 0);

        resumenPagosCliente.innerHTML = lista.length === 0 ? "" :
            `Facturado <strong>${dinero(facturado)}</strong> · Pagado <strong class="ValorPagadoCliente">${dinero(pagado)}</strong> · ` +
            `Saldo <strong class="${saldo > 0 ? "ValorSaldoCliente" : ""}">${dinero(saldo)}</strong>`;

        if (lista.length === 0) {
            mensajeTablaPagos(idEstudiante ? "Este estudiante no tiene pagos registrados." : "No hay pagos registrados.");
            return;
        }

        cuerpoPagosCliente.innerHTML = lista.map(function (p) {
            const origen = p.idCita
                ? '<span class="ChipOrigen"><i class="bi bi-calendar-check" aria-hidden="true"></i>Cita</span>'
                : '<span class="ChipOrigen"><i class="bi bi-pencil" aria-hidden="true"></i>Manual</span>';

            return `
                <tr class="${p.idEstadoPago === PAGO_ANULADO ? "FilaAnulada" : ""}">
                    <td class="text-nowrap">${escaparHtml(fechaCorta(p.fechaEmision))}</td>
                    <td>${escaparHtml(p.estudiante)}</td>
                    <td>${escaparHtml(p.concepto)} ${origen}</td>
                    <td class="text-end text-nowrap">${dinero(p.monto)}</td>
                    <td class="text-end text-nowrap">${dinero(p.abonado)}</td>
                    <td class="text-end text-nowrap">${PAGOS_CON_SALDO.includes(p.idEstadoPago) ? dinero(p.saldo) : "-"}</td>
                    <td>${chipPago(p.idEstadoPago)}</td>
                </tr>`;
        }).join("");
    }

    filtroEstudiantePagos?.addEventListener("change", pintarPagosCliente);


    async function refrescarDetalle() {
        renderDetalle(await obtenerCliente(clienteActual.id));
    }
 
    function mostrarDetalle(cliente) {
        bootstrap.Tab.getOrCreateInstance(document.getElementById("TabDetalleEncargado")).show();
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
        // Mientras se agrega o edita un estudiante, el unico "guardar" es el del formulario
        document.getElementById("BotonEditarDesdeDetalle").classList.add("d-none");
        panelEstudiante.scrollIntoView({ behavior: "smooth", block: "nearest" });
        document.getElementById("EstudianteNombre").focus();
    }
 
    function cerrarPanelEstudiante() {
        panelEstudiante.classList.add("d-none");
        document.getElementById("BotonEditarDesdeDetalle").classList.remove("d-none");
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