// Lookup reutilizable para catálogos (áreas de dificultad, necesidades de apoyo).
// Busca mientras se escribe; si lo escrito no existe, ofrece crearlo.
// El nombre se envía tal cual: el SP lo busca y, si no existe, lo crea.
window.LookupCatalogo = (function () {
    "use strict";

    function iniciar(opciones) {
        const campo = document.getElementById(opciones.campo);
        const lista = document.getElementById(opciones.lista);
        const aviso = document.getElementById(opciones.aviso);

        if (!campo || !lista) return null;

        let temporizador = null;
        let numeroBusqueda = 0;

        function ocultar() {
            lista.classList.add("d-none");
            lista.replaceChildren();
        }

        function actualizarAviso(nombres) {
            if (!aviso) return;

            const texto = campo.value.trim();

            if (!texto) {
                aviso.textContent = "";
                return;
            }

            const existe = nombres.some(n => n.toLowerCase() === texto.toLowerCase());

            aviso.textContent = existe
                ? opciones.textoExiste
                : opciones.textoNuevo;
        }

        function elegir(nombre) {
            campo.value = nombre;
            ocultar();
            actualizarAviso([nombre]);
        }

        function itemTexto(texto) {
            const p = document.createElement("div");
            p.className = "list-group-item text-muted small";
            p.textContent = texto;
            return p;
        }

        async function buscar() {
            const texto = campo.value.trim();
            const numero = ++numeroBusqueda;

            try {
                const respuesta = await fetch(
                    opciones.url + "?texto=" + encodeURIComponent(texto),
                    { headers: { Accept: "application/json" } });

                if (numero !== numeroBusqueda) return;

                if (!respuesta.ok) {
                    lista.replaceChildren(itemTexto("No se pudo cargar la lista."));
                    lista.classList.remove("d-none");
                    return;
                }

                const opcionesCatalogo = await respuesta.json();

                if (numero !== numeroBusqueda) return;

                const nombres = opcionesCatalogo.map(o => o.nombre);

                lista.replaceChildren();

                opcionesCatalogo.forEach(function (o) {
                    const boton = document.createElement("button");
                    boton.type = "button";
                    boton.className = "list-group-item list-group-item-action";
                    boton.textContent = o.nombre;
                    boton.addEventListener("click", () => elegir(o.nombre));
                    lista.appendChild(boton);
                });

                // Si no hay coincidencia exacta, se ofrece crear lo escrito
                const hayExacta = nombres.some(n => n.toLowerCase() === texto.toLowerCase());

                if (texto && !hayExacta) {
                    const crear = document.createElement("button");
                    crear.type = "button";
                    crear.className = "list-group-item list-group-item-action text-primary";

                    const icono = document.createElement("i");
                    icono.className = "bi bi-plus-circle me-2";
                    icono.setAttribute("aria-hidden", "true");

                    crear.append(icono, document.createTextNode("Crear «" + texto + "»"));
                    crear.addEventListener("click", () => elegir(texto));
                    lista.appendChild(crear);
                }

                if (!lista.children.length) {
                    lista.appendChild(itemTexto("Escriba para buscar."));
                }

                lista.classList.remove("d-none");
                actualizarAviso(nombres);
            } catch {
                lista.replaceChildren(itemTexto("No se pudo cargar la lista."));
                lista.classList.remove("d-none");
            }
        }

        campo.addEventListener("input", function () {
            clearTimeout(temporizador);
            temporizador = setTimeout(buscar, 250);
        });

        campo.addEventListener("focus", buscar);

        document.addEventListener("click", function (evento) {
            if (!campo.contains(evento.target) && !lista.contains(evento.target)) {
                lista.classList.add("d-none");
            }
        });

        campo.addEventListener("keydown", function (evento) {
            if (evento.key === "Escape") lista.classList.add("d-none");
        });

        return {
            reiniciar: function () {
                campo.value = "";
                if (aviso) aviso.textContent = "";
                ocultar();
            }
        };
    }

    return { iniciar };
})();