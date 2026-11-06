// Contadores de las burbujas rojas del menu, uno solo para todos.
//
// Antes esto eran dos archivos, contadorSolicitudes.js y contadorMensajes.js,
// que hacian lo mismo palabra por palabra: el mismo intervalo, la misma
// animacion de latido y el mismo manejo de error. La diferencia era que el de
// mensajes ya leia su ruta de un atributo y el de solicitudes la tenia escrita
// adentro. Con cuatro burbujas mas eso eran seis copias, asi que se fusionaron.
//
// Cada burbuja se describe sola en el HTML:
//
//   <sup class="SidebarAdmin-Contador d-none"
//        data-contador="/Admin/ContarAlgo"      ruta que devuelve el numero
//        data-contador-campo="total"            propiedad del json
//        data-contador-nombre="algo"            para refrescarla por nombre
//        data-contador-singular="cosa sin ver"  titulo cuando es 1
//        data-contador-plural="cosas sin ver"   titulo cuando es 0 o varios
//        data-contador-campo-titulo="detalle">  opcional: si la respuesta trae
//   </sup>                                      ese texto, se usa en vez de los
//                                               dos de arriba. Es para las
//                                               burbujas que suman dos cosas y
//                                               tienen que decir de que estan
//                                               hechas.
//
// Sirve igual en el panel y en el portal: lo unico que cambia es la ruta, y la
// ruta la pone cada vista.
(function () {

    const INTERVALO = 60000;

    const burbujas = [...document.querySelectorAll("[data-contador]")].map(nodo => ({
        nodo: nodo,
        ruta: nodo.getAttribute("data-contador"),
        campo: nodo.getAttribute("data-contador-campo") || "total",
        campoTitulo: nodo.getAttribute("data-contador-campo-titulo") || "",
        nombre: nodo.getAttribute("data-contador-nombre") || "",
        singular: nodo.getAttribute("data-contador-singular") || "pendiente",
        plural: nodo.getAttribute("data-contador-plural") || "pendientes",
        anterior: null
    })).filter(b => b.ruta);

    if (burbujas.length === 0) return;

    function pintar(burbuja, cantidad, titulo) {
        burbuja.nodo.textContent = cantidad > 99 ? "99+" : cantidad;
        burbuja.nodo.classList.toggle("d-none", cantidad === 0);
        burbuja.nodo.title = titulo || (cantidad === 1
            ? "1 " + burbuja.singular
            : cantidad + " " + burbuja.plural);

        // Si subio desde la ultima consulta, la burbuja "late"
        if (burbuja.anterior !== null && cantidad > burbuja.anterior) {
            burbuja.nodo.classList.remove("pulso");
            void burbuja.nodo.offsetWidth;   // reinicia la animacion
            burbuja.nodo.classList.add("pulso");
        }

        burbuja.anterior = cantidad;
    }

    async function actualizar(burbuja) {
        try {
            const respuesta = await fetch(burbuja.ruta, {
                headers: { "Accept": "application/json" }
            });

            if (!respuesta.ok) return;

            const cuerpo = await respuesta.json();
            const cantidad = Number(cuerpo?.[burbuja.campo]);

            if (!Number.isFinite(cantidad)) return;

            const titulo = burbuja.campoTitulo ? cuerpo?.[burbuja.campoTitulo] : "";

            pintar(burbuja, cantidad, typeof titulo === "string" ? titulo : "");

        } catch {
            // Si el API no responde, la burbuja se queda como estaba: es un
            // adorno y no vale la pena molestar a la persona con un error
        }
    }

    function actualizarTodas(nombre) {
        burbujas
            .filter(b => !nombre || b.nombre === nombre)
            .forEach(actualizar);
    }

    // Las pantallas lo llaman despues de un cambio, que es cuando lo que habia
    // pendiente deja de estarlo
    window.actualizarContadores = actualizarTodas;

    // Los dos nombres de antes siguen respondiendo: los llaman mensajeria.js y
    // solicitudes.js, y no hay por que tocarlos
    window.actualizarContadorMensajes = () => actualizarTodas("mensajes");
    window.actualizarContadorSolicitudes = () => actualizarTodas("solicitudes");

    actualizarTodas();
    setInterval(() => actualizarTodas(), INTERVALO);
})();
