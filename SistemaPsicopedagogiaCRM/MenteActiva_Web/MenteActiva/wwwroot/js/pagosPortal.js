(function () {
    "use strict";
    const comun = window.PortalComun;
    const pantalla = comun.iniciarPantalla("PortalPagos");
    if (!pantalla) return;
    const alerta = document.getElementById("AlertaPagosPortal");
    let consultando = false;

    function mostrarDetalle(datos) {
        const pago = datos.pago;
        if (!pago || !Array.isArray(datos.abonos)) throw new Error("No se recibió el detalle del cargo.");
        document.getElementById("TituloDetallePagoPortal").textContent = pago.concepto || "Detalle del cargo";
        const contenido = document.getElementById("ContenidoDetallePagoPortal");
        contenido.replaceChildren();
        const resumen = comun.nodo("dl", undefined, "row");
        const campos = [
            ["Estado", pago.estado], ["Monto", comun.dinero(pago.monto)],
            ["Abonado", comun.dinero(pago.abonado)], ["Saldo", comun.dinero(pago.saldo)],
            ["Emisión", comun.fecha(pago.fechaEmision)],
            ["Vencimiento", pago.fechaVencimiento ? comun.fecha(pago.fechaVencimiento) : "Sin vencimiento"]
        ];
        campos.forEach(([nombre, valor]) => {
            resumen.append(comun.nodo("dt", nombre, "col-sm-4"), comun.nodo("dd", valor || "—", "col-sm-8"));
        });
        contenido.append(resumen, comun.nodo("h3", "Historial de abonos activos", "h5 mt-4"));
        if (!datos.abonos.length) {
            contenido.append(comun.nodo("p", "Este cargo no tiene abonos activos registrados."));
        } else {
            const contenedor = comun.nodo("div", undefined, "table-responsive");
            const tabla = comun.nodo("table", undefined, "table");
            const cabecera = comun.nodo("thead");
            const filaCabecera = comun.nodo("tr");
            ["Fecha", "Monto", "Método", "Referencia"].forEach(nombre => {
                const celda = comun.nodo("th", nombre);
                celda.scope = "col";
                filaCabecera.append(celda);
            });
            cabecera.append(filaCabecera);
            const cuerpo = comun.nodo("tbody");
            datos.abonos.forEach(abono => {
                const fila = comun.nodo("tr");
                [comun.fecha(abono.fechaAbono), comun.dinero(abono.monto), abono.metodoPago || "—", abono.referencia || "—"]
                    .forEach(valor => fila.append(comun.nodo("td", valor)));
                cuerpo.append(fila);
            });
            tabla.append(cabecera, cuerpo);
            contenedor.append(tabla);
            contenido.append(contenedor);
        }
        comun.mostrarModal("ModalPagoPortal");
    }

    pantalla.addEventListener("click", async evento => {
        const boton = evento.target.closest("[data-detalle-pago]");
        if (!boton || consultando) return;
        consultando = true;
        const texto = boton.textContent;
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");
        boton.textContent = "Consultando…";
        comun.avisar(alerta, "");
        try {
            mostrarDetalle(await comun.consultarJson(boton.dataset.url));
        } catch (error) {
            comun.avisar(alerta, error.message || "No se pudo consultar el cargo.");
        } finally {
            consultando = false;
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
            boton.textContent = texto;
        }
    });
})();
