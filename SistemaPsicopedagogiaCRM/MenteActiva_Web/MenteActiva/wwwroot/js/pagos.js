// Pantalla de Pagos: tabla con filtros, totales, paginacion, nuevo pago y
// detalle del cobro (registrar o anular pagos). Los datos vienen del API por
// AdminController; el estado de cada cobro lo calcula la base de datos.
document.addEventListener("DOMContentLoaded", function () {
    "use strict";

    // =====================================================
    // CONSTANTES Y ESTADO
    // =====================================================

    // Ids de TB_ESTADO_PAGO
    const PAGADO = 1;
    const PENDIENTE = 2;
    const VENCIDO = 3;
    const PARCIAL = 4;
    const ANULADO = 5;

    const ESTADOS = {
        [PAGADO]: { nombre: "Pagado", clase: "chip-pago-pagado", icono: "bi-check-circle" },
        [PENDIENTE]: { nombre: "Pendiente", clase: "chip-pago-pendiente", icono: "bi-hourglass-split" },
        [VENCIDO]: { nombre: "Vencido", clase: "chip-pago-vencido", icono: "bi-exclamation-circle" },
        [PARCIAL]: { nombre: "Parcial", clase: "chip-pago-parcial", icono: "bi-pie-chart" },
        [ANULADO]: { nombre: "Anulado", clase: "chip-pago-anulado", icono: "bi-x-circle" }
    };

    const CON_SALDO = [PENDIENTE, VENCIDO, PARCIAL];

    // Mismo orden que el historial de pagos del encargado (Clientes)
    const ORDEN_ESTADO = { [VENCIDO]: 0, [PENDIENTE]: 1, [PARCIAL]: 2, [PAGADO]: 3, [ANULADO]: 4 };
    const FILTRO_ESTADO_POR_DEFECTO = "1,2,3,4";

    const RUTAS = {
        listar: "/Admin/ConsultarPagos",
        catalogos: "/Admin/CatalogosPagos",
        detalle: "/Admin/ObtenerPago",
        encargados: "/Admin/EncargadosPago",
        registrar: "/Admin/RegistrarPago",
        abonar: "/Admin/RegistrarAbonoPago",
        anularAbono: "/Admin/AnularAbonoPago",
        anular: "/Admin/AnularPago",
        buscarEstudiantes: "/Admin/BuscarEstudiantesCita"
    };

    const moneda = new Intl.NumberFormat("es-CR", { style: "currency", currency: "CRC", maximumFractionDigits: 0 });
    const esAdmin = document.getElementById("PaginaPagos").dataset.esAdmin === "true";

    // Filtro por estudiantes que llega en la URL desde el detalle del encargado:
    //   /Admin/Pagos?idEstudiantes=3,5&filtro=Estudiantes de Carlos Jimenez
    const parametrosUrl = new URLSearchParams(location.search);
    let idsEstudiantesFiltro = (parametrosUrl.get("idEstudiantes") || "")
        .split(",").map(Number).filter(n => Number.isInteger(n) && n > 0);
    const etiquetaFiltroEstudiante = parametrosUrl.get("filtro") || "Estudiantes seleccionados";

    let pagos = [];
    let catalogos = { metodos: [], tiposSesion: [] };
    let pagoActual = null;        // detalle abierto: { pago, abonos, encargados }
    let idAbonoPorAnular = null;


    // =====================================================
    // UTILIDADES
    // =====================================================

    function $(id) {
        return document.getElementById(id);
    }

    // Tambien escapa comillas: se usa dentro de atributos
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

    function modal(id) {
        return bootstrap.Modal.getOrCreateInstance($(id));
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

        return json;
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

    function hoyIso() {
        const hoy = new Date();
        return `${hoy.getFullYear()}-${String(hoy.getMonth() + 1).padStart(2, "0")}-${String(hoy.getDate()).padStart(2, "0")}`;
    }

    // El API manda "2026-10-30T00:00:00": para fechas sin hora basta la parte de la fecha
    function soloFecha(texto) {
        return texto ? String(texto).slice(0, 10) : "";
    }

    function formatoFecha(texto) {
        const iso = soloFecha(texto);
        if (!iso) return "—";

        const [anio, mes, dia] = iso.split("-").map(Number);
        return new Date(anio, mes - 1, dia).toLocaleDateString("es-CR", { day: "numeric", month: "short", year: "numeric" });
    }

    function formatoFechaHora(texto) {
        return texto
            ? new Date(texto).toLocaleString("es-CR", { day: "numeric", month: "short", year: "numeric", hour: "numeric", minute: "2-digit" })
            : "—";
    }

    function dinero(valor) {
        return moneda.format(Number(valor) || 0);
    }

    function chipEstado(idEstado) {
        const estado = ESTADOS[idEstado] ?? { nombre: "—", clase: "chip-estado-otro", icono: "bi-circle" };
        return `<span class="ChipEstado ${estado.clase}"><i class="bi ${estado.icono}" aria-hidden="true"></i>${estado.nombre}</span>`;
    }

    function chipOrigen(pago) {
        return pago.idCita
            ? '<span class="ChipOrigen"><i class="bi bi-calendar-check" aria-hidden="true"></i>Cita</span>'
            : '<span class="ChipOrigen"><i class="bi bi-pencil" aria-hidden="true"></i>Manual</span>';
    }

    function puedeAbonar(pago) {
        return CON_SALDO.includes(pago.idEstadoPago) && Number(pago.saldo) > 0;
    }

    function llenarSelect(select, opciones, textoVacio) {
        select.innerHTML = (textoVacio ? `<option value="">${escaparHtml(textoVacio)}</option>` : "") +
            opciones.map(o => `<option value="${o.valor}">${escaparHtml(o.texto)}</option>`).join("");
    }

    function opcionesEncargados(encargados) {
        return encargados.map(e => ({ valor: e.idEncargado, texto: e.nombre + (e.esPrincipal ? " (principal)" : "") }));
    }


    // =====================================================
    // LISTADO, FILTROS Y TOTALES
    // =====================================================

    function filtrados() {
        const texto = $("BuscarPago").value.trim().toLowerCase();
        const filtroEstado = $("FiltroEstadoPago").value;
        const estados = filtroEstado ? filtroEstado.split(",").map(Number) : [];
        const origen = $("FiltroOrigenPago").value;
        const desde = $("FiltroDesdePago").value;
        const hasta = $("FiltroHastaPago").value;

        return pagos.map((p, posicion) => ({ p, posicion })).filter(function ({ p }) {
            const fecha = soloFecha(p.fechaEmision);
            const textoPago = [p.estudiante, p.encargado, p.concepto].join(" ").toLowerCase();

            return (estados.length === 0 || estados.includes(p.idEstadoPago))
                && (!origen || p.origen === origen)
                && (!desde || fecha >= desde)
                && (!hasta || fecha <= hasta)
                && (!texto || textoPago.includes(texto))
                && (idsEstudiantesFiltro.length === 0 || idsEstudiantesFiltro.includes(p.idEstudiante));
        })
            // Dentro de cada estado se conserva el orden que manda el API
            .sort((a, b) => (ORDEN_ESTADO[a.p.idEstadoPago] ?? 9) - (ORDEN_ESTADO[b.p.idEstadoPago] ?? 9)
                || a.posicion - b.posicion)
            .map(x => x.p);
    }

    function hayFiltros() {
        return idsEstudiantesFiltro.length > 0 || Boolean($("BuscarPago").value.trim() || $("FiltroOrigenPago").value ||
            $("FiltroDesdePago").value || $("FiltroHastaPago").value ||
            $("FiltroEstadoPago").value !== FILTRO_ESTADO_POR_DEFECTO);
    }

    function textoCantidad(n, singular, plural) {
        return `${n} ${n === 1 ? singular : plural}`;
    }

    // Los anulados nunca suman: ya no son deuda ni ingreso
    function pintarTotales(lista) {
        const vigentes = lista.filter(p => p.idEstadoPago !== ANULADO);
        const conSaldo = vigentes.filter(p => CON_SALDO.includes(p.idEstadoPago));
        const vencidos = vigentes.filter(p => p.idEstadoPago === VENCIDO);
        const sumar = (filas, campo) => filas.reduce((total, p) => total + Number(p[campo] || 0), 0);

        $("TotalFacturado").textContent = dinero(sumar(vigentes, "monto"));
        $("DetalleFacturado").textContent = textoCantidad(vigentes.length, "cobro", "cobros");

        $("TotalCobrado").textContent = dinero(sumar(vigentes, "abonado"));
        $("DetalleCobrado").textContent = textoCantidad(vigentes.filter(p => p.idEstadoPago === PAGADO).length,
            "cobro pagado completo", "cobros pagados completos");

        $("TotalPendiente").textContent = dinero(sumar(conSaldo, "saldo"));
        $("DetallePendiente").textContent = textoCantidad(conSaldo.length, "cobro con saldo", "cobros con saldo");

        $("TotalVencido").textContent = dinero(sumar(vencidos, "saldo"));
        $("DetalleVencido").textContent = textoCantidad(vencidos.length, "cobro vencido", "cobros vencidos");
    }

    function filaPago(p) {
        const vence = p.fechaVencimiento
            ? `Vence ${formatoFecha(p.fechaVencimiento)}`
            : "Sin vencimiento";

        const botonAbonar = puedeAbonar(p)
            ? `<button type="button" class="btn btn-sm btn-outline-primary border-0" data-accion="abonar"
                       data-id="${p.idPago}" title="Registrar pago" aria-label="Registrar pago de ${escaparHtml(p.estudiante)}">
                   <i class="bi bi-cash-coin"></i></button>`
            : "";

        return `
            <tr class="${p.idEstadoPago === ANULADO ? "FilaAnulada" : ""}">
                <td class="text-nowrap">
                    <div>${escaparHtml(formatoFecha(p.fechaEmision))}</div>
                    <small class="TextoSuave">${escaparHtml(p.idEstadoPago === PAGADO ? "Pagado" : vence)}</small>
                </td>
                <td>
                    <div class="fw-semibold">${escaparHtml(p.estudiante)}</div>
                    <small class="TextoSuave">${escaparHtml(p.encargado || "")}</small>
                </td>
                <td>
                    <div>${escaparHtml(p.concepto)}</div>
                    ${chipOrigen(p)}
                    ${p.metodoPago ? `<small class="TextoSuave ms-1">${escaparHtml(p.metodoPago)}</small>` : ""}
                </td>
                <td class="text-end text-nowrap">${dinero(p.monto)}</td>
                <td class="text-end text-nowrap ${CON_SALDO.includes(p.idEstadoPago) ? "fw-semibold" : "TextoSuave"}">
                    ${CON_SALDO.includes(p.idEstadoPago) ? dinero(p.saldo) : "—"}
                </td>
                <td>${chipEstado(p.idEstadoPago)}</td>
                <td class="TablaAcciones text-nowrap">
                    <button type="button" class="btn btn-sm btn-outline-secondary border-0" data-accion="ver"
                            data-id="${p.idPago}" title="Ver detalle" aria-label="Ver detalle del cobro de ${escaparHtml(p.estudiante)}">
                        <i class="bi bi-eye"></i></button>
                    ${botonAbonar}
                </td>
            </tr>`;
    }

    function pintar(conservarPagina) {
        const lista = filtrados();

        pintarTotales(lista);

        $("CuerpoTablaPagos").innerHTML = lista.map(filaPago).join("");

        const vacio = lista.length === 0;
        $("VacioPagos").classList.toggle("d-none", !vacio);
        $("TextoVacioPagos").textContent = pagos.length === 0
            ? "No hay pagos registrados todavía."
            : "No hay pagos que coincidan con los filtros.";

        if (typeof PaginarTabla === "function") {
            PaginarTabla("TablaPagos", { conservarPagina: conservarPagina === true });
        }
    }

    function pintarAvisoSinPrecio(tipos) {
        const aviso = $("AvisoSinPrecio");

        if (!tipos || tipos.length === 0) {
            aviso.classList.add("d-none");
            return;
        }

        aviso.innerHTML = `
            <i class="bi bi-exclamation-triangle me-1" aria-hidden="true"></i>
            ${tipos.length === 1 ? "Este tipo de sesión no tiene precio" : "Estos tipos de sesión no tienen precio"}
            y sus citas no generan cobro automático: <strong>${tipos.map(escaparHtml).join(", ")}</strong>.
            ${esAdmin ? '<a href="/Admin/Configuracion" class="alert-link">Asignar precios en Configuración</a>' : "Pida al administrador que les asigne precio."}`;
        aviso.classList.remove("d-none");
    }

    async function cargar(conservarPagina) {
        ocultarAlerta($("AlertaPagos"));

        try {
            const datos = await obtener(RUTAS.listar);
            pagos = datos?.pagos ?? [];
            pintarAvisoSinPrecio(datos?.tiposSesionSinPrecio);
        } catch (error) {
            pagos = [];
            mostrarAlerta($("AlertaPagos"), error.message);
        }

        pintar(conservarPagina);
    }

    // Cualquier cambio de filtro vuelve a la primera pagina
    ["BuscarPago"].forEach(id => $(id).addEventListener("input", () => pintar(false)));
    ["FiltroEstadoPago", "FiltroOrigenPago", "FiltroDesdePago", "FiltroHastaPago"]
        .forEach(id => $(id).addEventListener("change", () => pintar(false)));

    function pintarFiltroEstudiante() {
        $("FiltroEstudianteActivo").classList.toggle("d-none", idsEstudiantesFiltro.length === 0);
        $("TextoFiltroEstudiante").textContent = etiquetaFiltroEstudiante;   // textContent: viene de la URL
    }

    // Quitar el filtro tambien limpia la URL, para que al recargar no vuelva
    function quitarFiltroEstudiante() {
        idsEstudiantesFiltro = [];
        history.replaceState(null, "", location.pathname);
        pintarFiltroEstudiante();
    }

    $("QuitarFiltroEstudiante").addEventListener("click", function () {
        quitarFiltroEstudiante();
        pintar(false);
    });

    $("BotonLimpiarFiltrosPago").addEventListener("click", function () {
        quitarFiltroEstudiante();
        $("BuscarPago").value = "";
        $("FiltroEstadoPago").value = FILTRO_ESTADO_POR_DEFECTO;
        $("FiltroOrigenPago").value = "";
        $("FiltroDesdePago").value = "";
        $("FiltroHastaPago").value = "";
        pintar(false);
    });

    $("CuerpoTablaPagos").addEventListener("click", function (evento) {
        const boton = evento.target.closest("button[data-accion]");
        if (!boton) return;

        abrirDetalle(Number(boton.dataset.id), boton.dataset.accion === "abonar");
    });


    // =====================================================
    // CATALOGOS
    // =====================================================

    async function cargarCatalogos() {
        try {
            const datos = await obtener(RUTAS.catalogos);
            catalogos = { metodos: datos?.metodos ?? [], tiposSesion: datos?.tiposSesion ?? [] };
        } catch {
            catalogos = { metodos: [], tiposSesion: [] };
        }

        document.querySelectorAll("select[data-metodos]").forEach(function (select) {
            llenarSelect(select, catalogos.metodos.map(m => ({ valor: m.idMetodoPago, texto: m.nombre })), "Seleccione");
        });

        llenarSelect($("TipoSesionPago"),
            catalogos.tiposSesion.map(t => ({
                valor: t.idTipoSesion,
                texto: t.precio > 0 ? `${t.nombre} · ${dinero(t.precio)}` : `${t.nombre} · sin precio`
            })),
            "Otro (escribir el concepto)");
    }


    // =====================================================
    // NUEVO PAGO
    // =====================================================

    const formNuevo = $("FormularioNuevoPago");
    const alertaNuevo = $("AlertaNuevoPago");
    const botonGuardarNuevo = $("BotonGuardarNuevoPago");
    const campoConcepto = $("ConceptoPago");
    const campoMonto = $("MontoPago");
    const campoFecha = $("FechaPago");
    const campoVence = $("VencimientoPago");
    const campoYaPagado = $("YaPagadoPago");
    const campoPagador = $("PagadorPago");
    const campoMetodo = $("MetodoPago");

    const buscadorPago = crearBuscadorEstudiantes($("BuscadorEstudiantePago"), {
        idEntrada: "BuscadorPago",
        nombreCampo: "IdEstudianteBuscado",
        url: RUTAS.buscarEstudiantes,
        alCambiar: cargarPagadores
    });

    // Quien puede pagar: los encargados del estudiante elegido
    async function cargarPagadores(estudiante) {
        if (!estudiante) {
            llenarSelect(campoPagador, [], "Primero elija el estudiante");
            campoPagador.disabled = true;
            return;
        }

        llenarSelect(campoPagador, [], "Cargando…");
        campoPagador.disabled = true;

        try {
            const encargados = await obtener(`${RUTAS.encargados}?idEstudiante=${estudiante.idEstudiante}`);

            if (!encargados || encargados.length === 0) {
                llenarSelect(campoPagador, [], "El estudiante no tiene encargados");
                return;
            }

            llenarSelect(campoPagador, opcionesEncargados(encargados), encargados.length > 1 ? "Seleccione" : null);
            campoPagador.disabled = false;
        } catch {
            llenarSelect(campoPagador, [], "No se pudieron cargar los encargados");
        }
    }

    // El servicio llena concepto y monto con el catalogo (se pueden cambiar)
    $("TipoSesionPago").addEventListener("change", function () {
        const tipo = catalogos.tiposSesion.find(t => String(t.idTipoSesion) === this.value);
        if (!tipo) return;

        campoConcepto.value = tipo.nombre;
        if (tipo.precio > 0) campoMonto.value = Math.round(tipo.precio);

        marcar(campoConcepto, true);
        marcar(campoMonto, true);
    });

    campoYaPagado.addEventListener("change", function () {
        $("PanelYaPagado").classList.toggle("d-none", !this.checked);
    });

    formNuevo.addEventListener("input", evento => evento.target.classList.remove("is-invalid"));

    $("ModalNuevoPago").addEventListener("show.bs.modal", function () {
        formNuevo.reset();
        formNuevo.querySelectorAll(".is-invalid").forEach(c => c.classList.remove("is-invalid"));
        ocultarAlerta(alertaNuevo);
        buscadorPago.limpiar();
        cargarPagadores(null);
        $("PanelYaPagado").classList.add("d-none");

        const hoy = hoyIso();
        campoFecha.value = hoy;
        campoFecha.max = hoy;
    });

    function validarNuevo() {
        const hoy = hoyIso();
        const monto = Number(campoMonto.value);

        let valido = buscadorPago.validar();
        valido = marcar(campoConcepto, campoConcepto.value.trim() !== "") && valido;
        valido = marcar(campoMonto, Number.isInteger(monto) && monto > 0) && valido;
        valido = marcar(campoFecha, campoFecha.value !== "" && campoFecha.value <= hoy) && valido;
        valido = marcar(campoVence, !campoVence.value || campoVence.value >= campoFecha.value) && valido;

        if (campoYaPagado.checked) {
            valido = marcar(campoPagador, campoPagador.value !== "") && valido;
            valido = marcar(campoMetodo, campoMetodo.value !== "") && valido;
        }

        return valido;
    }

    formNuevo.addEventListener("submit", async function (evento) {
        evento.preventDefault();
        ocultarAlerta(alertaNuevo);

        if (!validarNuevo()) {
            mostrarAlerta(alertaNuevo, "Revise los campos marcados en rojo.");
            return;
        }

        const yaPagado = campoYaPagado.checked;

        ocupado(botonGuardarNuevo, true);
        const resultado = await enviar(RUTAS.registrar, datosConToken({
            IdEstudiante: buscadorPago.valor,
            Concepto: campoConcepto.value.trim(),
            Monto: Number(campoMonto.value),
            FechaEmision: campoFecha.value,
            FechaVencimiento: campoVence.value,
            Observaciones: $("ObservacionesPago").value.trim(),
            YaPagado: yaPagado,
            IdEncargadoPagador: yaPagado ? campoPagador.value : "",
            IdMetodoPago: yaPagado ? campoMetodo.value : "",
            Referencia: yaPagado ? $("ReferenciaPago").value.trim() : ""
        }));
        ocupado(botonGuardarNuevo, false);

        if (!resultado.ok) {
            mostrarAlerta(alertaNuevo, resultado.mensaje);
            return;
        }

        modal("ModalNuevoPago").hide();
        notificar(resultado.datos?.mensaje || "Pago registrado correctamente.", "exito");
        await cargar(false);
    });


    // =====================================================
    // DETALLE DEL COBRO
    // =====================================================

    const alertaDetalle = $("AlertaDetallePago");
    const PANELES = ["PanelAbono", "PanelAnularAbono", "PanelAnularCobro"];
    let panelAbierto = false;

    // Con un panel abierto el pie solo deja "Cerrar": las acciones viven en el panel
    function actualizarPie() {
        const pago = pagoActual?.pago;
        const abonosActivos = pagoActual ? pagoActual.abonos.filter(a => a.activo).length : 0;

        $("BotonRegistrarAbono").classList.toggle("d-none", panelAbierto || !pago || !puedeAbonar(pago));
        $("BotonAnularCobro").classList.toggle("d-none",
            panelAbierto || !pago || pago.idEstadoPago === ANULADO || abonosActivos > 0);
    }

    // Sin panel abierto el detalle usa todo el ancho; con panel, el panel va a la derecha
    function cerrarPaneles() {
        PANELES.forEach(id => $(id).classList.add("d-none"));
        $("ColumnaPanelPago").classList.add("d-none");
        $("ColumnaDetallePago").className = "col-12";
        document.querySelectorAll("#ModalDetallePago .is-invalid").forEach(c => c.classList.remove("is-invalid"));
        panelAbierto = false;
        actualizarPie();
    }

    function mostrarPanel(id) {
        cerrarPaneles();
        $("ColumnaDetallePago").className = "col-lg-7";
        $("ColumnaPanelPago").classList.remove("d-none");
        $(id).classList.remove("d-none");
        $(id).querySelector("select, input, textarea")?.focus();
        panelAbierto = true;
        actualizarPie();
    }

    function filaDetalle(etiqueta, valor) {
        return `<dt class="col-sm-4 TextoSuave fw-normal">${etiqueta}</dt><dd class="col-sm-8">${valor}</dd>`;
    }

    function pintarDetalle() {
        const { pago, abonos } = pagoActual;
        const porcentaje = pago.monto > 0 ? Math.min(100, Math.round(pago.abonado * 100 / pago.monto)) : 0;
        const anulado = pago.idEstadoPago === ANULADO;
        const abonosActivos = abonos.filter(a => a.activo);

        $("DetalleEstadoPago").innerHTML = chipEstado(pago.idEstadoPago);

        const origen = pago.idCita
            ? `${chipOrigen(pago)} <span class="ms-1">${escaparHtml(formatoFechaHora(pago.fechaCita))}</span>`
            : `${chipOrigen(pago)} <span class="ms-1">Registrado a mano</span>`;

        const filasAbonos = abonos.length === 0
            ? '<tr><td colspan="6" class="text-center TextoSuave py-3">Todavía no hay pagos registrados.</td></tr>'
            : abonos.map(a => `
                <tr class="${a.activo ? "" : "FilaAnulada"}">
                    <td class="text-nowrap">${escaparHtml(formatoFecha(a.fechaAbono))}</td>
                    <td class="text-end text-nowrap">${dinero(a.monto)}</td>
                    <td>${escaparHtml(a.metodoPago)}</td>
                    <td>${escaparHtml(a.pagador)}</td>
                    <td>${escaparHtml(a.referencia || "—")}
                        ${a.activo ? "" : '<span class="ChipEstado chip-pago-anulado ms-1"><i class="bi bi-x-circle" aria-hidden="true"></i>Anulado</span>'}</td>
                    <td class="text-end">
                        ${a.activo && !anulado
                            ? `<button type="button" class="btn btn-sm btn-outline-danger border-0" data-anular-abono="${a.idAbono}"
                                       data-texto="${escaparHtml(`${dinero(a.monto)} del ${formatoFecha(a.fechaAbono)}`)}"
                                       title="Anular este pago" aria-label="Anular este pago"><i class="bi bi-arrow-counterclockwise"></i></button>`
                            : ""}
                    </td>
                </tr>`).join("");

        $("ContenidoDetallePago").innerHTML = `
            <dl class="row mb-3">
                ${filaDetalle("Estudiante", `<span class="fw-semibold">${escaparHtml(pago.estudiante)}</span>`)}
                ${filaDetalle("Concepto", escaparHtml(pago.concepto))}
                ${filaDetalle("Origen", origen)}
                ${filaDetalle("Fecha del cobro", escaparHtml(formatoFecha(pago.fechaEmision)))}
                ${filaDetalle("Vence", escaparHtml(pago.fechaVencimiento ? formatoFecha(pago.fechaVencimiento) : "Sin vencimiento"))}
                ${pago.observaciones ? filaDetalle("Observaciones", escaparHtml(pago.observaciones)) : ""}
            </dl>

            <div class="row g-2 text-center mb-2">
                <div class="col-4"><div class="ResumenCobro"><div class="TextoSuave small">Monto</div><div class="fw-semibold">${dinero(pago.monto)}</div></div></div>
                <div class="col-4"><div class="ResumenCobro"><div class="TextoSuave small">Pagado</div><div class="fw-semibold ValorCobrado">${dinero(pago.abonado)}</div></div></div>
                <div class="col-4"><div class="ResumenCobro"><div class="TextoSuave small">Saldo</div><div class="fw-semibold ${pago.saldo > 0 && !anulado ? "ValorPendiente" : ""}">${anulado ? "—" : dinero(pago.saldo)}</div></div></div>
            </div>
            <div class="progress mb-4" role="progressbar" aria-label="Porcentaje pagado"
                 aria-valuenow="${porcentaje}" aria-valuemin="0" aria-valuemax="100" style="height: 8px;">
                <div class="progress-bar BarraCobro" style="width: ${porcentaje}%"></div>
            </div>

            <h6 class="mb-2">Pagos recibidos</h6>
            <div class="table-responsive">
                <table class="table table-sm align-middle mb-0">
                    <thead>
                        <tr><th>Fecha</th><th class="text-end">Monto</th><th>Método</th><th>Pagó</th><th>Referencia</th><th></th></tr>
                    </thead>
                    <tbody>${filasAbonos}</tbody>
                </table>
            </div>`;

        // Botones del pie segun el estado del cobro (y si hay un panel abierto)
        actualizarPie();
    }

    async function abrirDetalle(idPago, abrirPanelAbono) {
        ocultarAlerta(alertaDetalle);
        cerrarPaneles();
        $("ContenidoDetallePago").innerHTML =
            '<div class="text-center py-4 TextoSuave"><span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>Cargando…</div>';
        $("BotonRegistrarAbono").classList.add("d-none");
        $("BotonAnularCobro").classList.add("d-none");
        modal("ModalDetallePago").show();

        try {
            pagoActual = await obtener(`${RUTAS.detalle}?id=${idPago}`);
            pintarDetalle();

            if (abrirPanelAbono && puedeAbonar(pagoActual.pago)) prepararAbono();
        } catch (error) {
            pagoActual = null;
            $("ContenidoDetallePago").innerHTML = "";
            mostrarAlerta(alertaDetalle, error.message);
        }
    }

    // Tras guardar: refresca el detalle y la tabla sin cambiar de pagina
    async function refrescar(mensaje) {
        notificar(mensaje, "exito");
        cerrarPaneles();
        await Promise.all([abrirDetalleSinAnimar(), cargar(true)]);
    }

    async function abrirDetalleSinAnimar() {
        if (!pagoActual) return;

        try {
            pagoActual = await obtener(`${RUTAS.detalle}?id=${pagoActual.pago.idPago}`);
            pintarDetalle();
        } catch (error) {
            mostrarAlerta(alertaDetalle, error.message);
        }
    }

    // ---------- registrar pago ----------

    function prepararAbono() {
        const { pago, encargados } = pagoActual;
        const hoy = hoyIso();

        llenarSelect($("PagadorAbono"), opcionesEncargados(encargados), encargados.length > 1 ? "Seleccione" : null);
        $("MetodoAbono").value = "";
        $("MontoAbono").value = Math.round(pago.saldo);
        $("MontoAbono").max = Math.round(pago.saldo);
        $("FechaAbono").value = hoy;
        $("FechaAbono").max = hoy;
        $("ReferenciaAbono").value = "";

        mostrarPanel("PanelAbono");
    }

    $("BotonRegistrarAbono").addEventListener("click", prepararAbono);

    $("BotonGuardarAbono").addEventListener("click", async function () {
        ocultarAlerta(alertaDetalle);

        const saldo = Math.round(pagoActual.pago.saldo);
        const monto = Number($("MontoAbono").value);
        const montoValido = Number.isInteger(monto) && monto > 0 && monto <= saldo;

        $("ErrorMontoAbono").textContent = monto > saldo
            ? `No puede pasar del saldo (${dinero(saldo)}).`
            : "Indique un monto mayor que cero, sin decimales.";

        let valido = marcar($("PagadorAbono"), $("PagadorAbono").value !== "");
        valido = marcar($("MetodoAbono"), $("MetodoAbono").value !== "") && valido;
        valido = marcar($("MontoAbono"), montoValido) && valido;
        valido = marcar($("FechaAbono"), $("FechaAbono").value !== "" && $("FechaAbono").value <= hoyIso()) && valido;

        if (!valido) return;

        ocupado(this, true);
        const resultado = await enviar(`${RUTAS.abonar}?id=${pagoActual.pago.idPago}`, datosConToken({
            IdEncargadoPagador: $("PagadorAbono").value,
            IdMetodoPago: $("MetodoAbono").value,
            Monto: monto,
            FechaAbono: $("FechaAbono").value,
            Referencia: $("ReferenciaAbono").value.trim()
        }));
        ocupado(this, false);

        if (!resultado.ok) {
            mostrarAlerta(alertaDetalle, resultado.mensaje);
            return;
        }

        await refrescar(resultado.datos?.mensaje || "Pago registrado correctamente.");
    });

    // ---------- anular un pago ----------

    $("ContenidoDetallePago").addEventListener("click", function (evento) {
        const boton = evento.target.closest("[data-anular-abono]");
        if (!boton) return;

        idAbonoPorAnular = Number(boton.dataset.anularAbono);
        $("TextoAbonoAnular").textContent = boton.dataset.texto;
        $("MotivoAnularAbono").value = "";
        mostrarPanel("PanelAnularAbono");
    });

    $("BotonConfirmarAnularAbono").addEventListener("click", async function () {
        const motivo = $("MotivoAnularAbono").value.trim();
        if (!marcar($("MotivoAnularAbono"), motivo !== "")) return;

        ocupado(this, true);
        const resultado = await enviar(RUTAS.anularAbono, datosConToken({ idAbono: idAbonoPorAnular, motivo }));
        ocupado(this, false);

        if (!resultado.ok) {
            mostrarAlerta(alertaDetalle, resultado.mensaje);
            return;
        }

        await refrescar(resultado.datos?.mensaje || "Pago anulado.");
    });

    // ---------- anular el cobro ----------

    $("BotonAnularCobro").addEventListener("click", function () {
        $("MotivoAnularCobro").value = "";
        mostrarPanel("PanelAnularCobro");
    });

    $("BotonConfirmarAnularCobro").addEventListener("click", async function () {
        const motivo = $("MotivoAnularCobro").value.trim();
        if (!marcar($("MotivoAnularCobro"), motivo !== "")) return;

        ocupado(this, true);
        const resultado = await enviar(RUTAS.anular, datosConToken({ id: pagoActual.pago.idPago, motivo }));
        ocupado(this, false);

        if (!resultado.ok) {
            mostrarAlerta(alertaDetalle, resultado.mensaje);
            return;
        }

        await refrescar(resultado.datos?.mensaje || "Cobro anulado.");
    });

    document.querySelectorAll("#ModalDetallePago [data-cerrar-panel]")
        .forEach(boton => boton.addEventListener("click", cerrarPaneles));

    $("ModalDetallePago").addEventListener("input", evento => evento.target.classList.remove("is-invalid"));
    $("ModalDetallePago").addEventListener("change", evento => evento.target.classList.remove("is-invalid"));

    $("ModalDetallePago").addEventListener("hidden.bs.modal", function () {
        pagoActual = null;
        cerrarPaneles();
    });


    // =====================================================
    // INICIO
    // =====================================================

    pintarFiltroEstudiante();
    cargarCatalogos();
    cargar(false);
});
