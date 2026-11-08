// La gemela en navegador de Services/FormatoDinero.cs. Las dos tienen que escribir
// el mismo texto para el mismo monto: antes el portal mostraba decimales y el panel
// no, y el mismo pago se leia distinto en cada pantalla.
//
// Las dos decisiones que importan viven igual en los dos lados:
//
// - Sin decimales, porque son colones.
// - El codigo de la moneda sale de TB_CONFIGURACION. El layout lo escribe en
//   data-moneda del body y aqui se lee de ahi; nadie lo tiene escrito a mano.
//
// Se midio que es-CR con CRC da los mismos caracteres en .NET y en el navegador.
// Si se cambia una de las dos, hay que cambiar la otra.
(function () {
    "use strict";

    var MONEDA_POR_DEFECTO = "CRC";
    var CULTURA_MONTOS = "es-CR";

    var formateador = null;
    var monedaFormateador = null;

    function monedaConfigurada() {
        var codigo = document.body && document.body.dataset
            ? (document.body.dataset.moneda || "").trim().toUpperCase()
            : "";

        return codigo || MONEDA_POR_DEFECTO;
    }

    function dinero(valor) {
        var codigo = monedaConfigurada();

        if (!formateador || monedaFormateador !== codigo) {
            try {
                formateador = new Intl.NumberFormat(CULTURA_MONTOS, {
                    style: "currency",
                    currency: codigo,
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 0
                });
            } catch {
                // Un codigo que no sea ISO valido hace reventar a Intl: se cae a colones
                formateador = new Intl.NumberFormat(CULTURA_MONTOS, {
                    style: "currency",
                    currency: MONEDA_POR_DEFECTO,
                    minimumFractionDigits: 0,
                    maximumFractionDigits: 0
                });
            }

            monedaFormateador = codigo;
        }

        return formateador.format(Number(valor) || 0);
    }

    window.Formato = {
        dinero: dinero,
        moneda: monedaConfigurada
    };
})();
