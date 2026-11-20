
document.addEventListener("DOMContentLoaded", function () {

    const filtros = document.querySelectorAll("select.FiltroPortal");

    filtros.forEach(function (select) {

        if (select.dataset.personalizado === "true") return;

        select.dataset.personalizado = "true";

        const contenedor = document.createElement("div");
        contenedor.className = "FiltroPortal-Contenedor";

        select.parentNode.insertBefore(contenedor, select);
        contenedor.appendChild(select);

        select.classList.add("FiltroPortal-Nativo");
        select.tabIndex = -1;

        const boton = document.createElement("button");
        boton.type = "button";
        boton.className = "FiltroPortal-Boton";
        boton.setAttribute("aria-haspopup", "listbox");
        boton.setAttribute("aria-expanded", "false");

        const texto = document.createElement("span");

        const flecha = document.createElement("i");
        flecha.className = "bi bi-chevron-down FiltroPortal-Flecha";
        flecha.setAttribute("aria-hidden", "true");

        boton.append(texto, flecha);

        const opciones = document.createElement("div");
        opciones.className = "FiltroPortal-Opciones";
        opciones.setAttribute("role", "listbox");
        opciones.hidden = true;

        function actualizar() {
            const seleccionada = select.options[select.selectedIndex];

            texto.textContent = seleccionada
                ? seleccionada.textContent
                : "Seleccione una opción";

            opciones.querySelectorAll("button").forEach(function (item, index) {
                item.setAttribute(
                    "aria-selected",
                    index === select.selectedIndex ? "true" : "false"
                );
            });
        }

        Array.from(select.options).forEach(function (option, index) {

            const elemento = document.createElement("button");
            elemento.type = "button";
            elemento.className = "FiltroPortal-Opcion";
            elemento.textContent = option.textContent;
            elemento.setAttribute("role", "option");
            elemento.disabled = option.disabled;

            elemento.addEventListener("click", function () {

                select.selectedIndex = index;

                select.dispatchEvent(
                    new Event("change", { bubbles: true })
                );

                actualizar();
                cerrar();
                boton.focus();
            });

            opciones.appendChild(elemento);
        });

        function abrir() {
            opciones.hidden = false;
            boton.setAttribute("aria-expanded", "true");
        }

        function cerrar() {
            opciones.hidden = true;
            boton.setAttribute("aria-expanded", "false");
        }

        boton.addEventListener("click", function () {
            if (opciones.hidden) {
                abrir();
            } else {
                cerrar();
            }
        });

        boton.addEventListener("keydown", function (event) {
            if (event.key === "Escape") {
                cerrar();
            }

            if (event.key === "ArrowDown") {
                event.preventDefault();
                abrir();

                const primera = opciones.querySelector(
                    "button:not(:disabled)"
                );

                if (primera) primera.focus();
            }
        });

        opciones.addEventListener("keydown", function (event) {
            const elementos = Array.from(
                opciones.querySelectorAll("button:not(:disabled)")
            );

            const actual = elementos.indexOf(document.activeElement);

            if (event.key === "Escape") {
                cerrar();
                boton.focus();
            }

            if (event.key === "ArrowDown") {
                event.preventDefault();
                elementos[Math.min(actual + 1, elementos.length - 1)]?.focus();
            }

            if (event.key === "ArrowUp") {
                event.preventDefault();
                elementos[Math.max(actual - 1, 0)]?.focus();
            }
        });

        select.addEventListener("change", actualizar);

        contenedor.append(boton, opciones);

        actualizar();

        document.addEventListener("click", function (event) {
            if (!contenedor.contains(event.target)) {
                cerrar();
            }
        });

    });

});
