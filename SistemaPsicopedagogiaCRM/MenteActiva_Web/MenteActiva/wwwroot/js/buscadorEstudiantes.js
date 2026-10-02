// Buscador de estudiantes reutilizable (lookup).
//
// Uso:
//   const buscador = crearBuscadorEstudiantes(document.getElementById("Contenedor"), {
//       url: "/Admin/BuscarEstudiantesCita",   // GET ?texto=...
//       nombreCampo: "IdEstudiante",           // name del input oculto que viaja en el form
//       idEntrada: "EstudianteCita"            // id del input visible (para el <label for>)
//   });
//
//   buscador.valor       -> IdEstudiante elegido ("" si no hay)
//   buscador.validar()   -> marca en rojo si no hay seleccion; devuelve true/false
//   buscador.limpiar()   -> vuelve al estado inicial
//   buscador.enfocar()   -> pone el foco en el campo
(function () {
    "use strict";

    const ESPERA_MS = 300;
    const MINIMO_LETRAS = 2;

    let instancias = 0;

    function escaparHtml(texto) {
        const div = document.createElement("div");
        div.textContent = texto ?? "";
        return div.innerHTML;
    }

    window.crearBuscadorEstudiantes = function (contenedor, opciones) {

        const idLista = "BuscadorLista" + (++instancias);

        contenedor.classList.add("Buscador");
        contenedor.innerHTML = `
            <input type="text" class="form-control" id="${opciones.idEntrada}" autocomplete="off"
                   placeholder="${escaparHtml(opciones.placeholder ?? "Escriba el nombre del estudiante o del encargado")}"
                   role="combobox" aria-autocomplete="list" aria-expanded="false" aria-controls="${idLista}">
            <div class="Buscador-Seleccion d-none">
                <div class="text-truncate">
                    <div class="fw-semibold" data-seleccion-nombre></div>
                    <small class="TextoSuave" data-seleccion-detalle></small>
                </div>
                <button type="button" class="btn-close" aria-label="Cambiar estudiante"></button>
            </div>
            <div class="Buscador-Lista d-none" role="listbox" id="${idLista}"></div>
            <input type="hidden" name="${opciones.nombreCampo}">
            <div class="invalid-feedback">Seleccione un estudiante de la lista.</div>`;

        const entrada = contenedor.querySelector("input[type=text]");
        const lista = contenedor.querySelector(".Buscador-Lista");
        const seleccion = contenedor.querySelector(".Buscador-Seleccion");
        const nombreSeleccion = contenedor.querySelector("[data-seleccion-nombre]");
        const detalleSeleccion = contenedor.querySelector("[data-seleccion-detalle]");
        const botonQuitar = seleccion.querySelector(".btn-close");
        const oculto = contenedor.querySelector("input[type=hidden]");

        let resultados = [];
        let indiceActivo = -1;
        let temporizador = null;
        let abortador = null;

        // ---------- lista ----------

        function abrirLista() {
            lista.classList.remove("d-none");
            entrada.setAttribute("aria-expanded", "true");
        }

        function cerrarLista() {
            lista.classList.add("d-none");
            entrada.setAttribute("aria-expanded", "false");
            entrada.removeAttribute("aria-activedescendant");
            indiceActivo = -1;
        }

        function mostrarMensaje(html) {
            resultados = [];
            lista.innerHTML = `<div class="Buscador-Mensaje">${html}</div>`;
            abrirLista();
        }

        function describir(estudiante) {
            return [estudiante.nivelEducativo, estudiante.encargado ? "Encargado: " + estudiante.encargado : null]
                .filter(Boolean)
                .join(" · ");
        }

        function pintarResultados() {
            if (resultados.length === 0) {
                mostrarMensaje("No se encontraron estudiantes activos.");
                return;
            }

            lista.innerHTML = resultados.map((e, i) => `
                <div class="Buscador-Opcion" role="option" id="${idLista}_${i}" data-indice="${i}" aria-selected="false">
                    <div class="fw-semibold">${escaparHtml(e.estudiante)}</div>
                    <small>${escaparHtml(describir(e) || "Sin datos adicionales")}</small>
                </div>`).join("");

            abrirLista();
        }

        function marcarActiva(indice) {
            const opcionesLista = lista.querySelectorAll("[data-indice]");

            opcionesLista.forEach((op, i) => {
                op.classList.toggle("activa", i === indice);
                op.setAttribute("aria-selected", i === indice ? "true" : "false");
            });

            indiceActivo = indice;

            if (opcionesLista[indice]) {
                entrada.setAttribute("aria-activedescendant", opcionesLista[indice].id);
                opcionesLista[indice].scrollIntoView({ block: "nearest" });
            }
        }

        // ---------- busqueda ----------

        async function buscar(texto) {
            abortador?.abort();
            abortador = new AbortController();

            mostrarMensaje('<span class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>Buscando…');

            try {
                const respuesta = await fetch(`${opciones.url}?texto=${encodeURIComponent(texto)}`, {
                    headers: { "Accept": "application/json" },
                    signal: abortador.signal
                });

                if (!respuesta.ok) throw new Error();

                resultados = await respuesta.json();
                indiceActivo = -1;
                pintarResultados();

            } catch (error) {
                // Si se aborto es porque el usuario siguio escribiendo: no es un error
                if (error.name !== "AbortError") {
                    mostrarMensaje("No se pudo buscar. Intente de nuevo.");
                }
            }
        }

        // ---------- seleccion ----------

        function seleccionar(estudiante) {
            oculto.value = estudiante.idEstudiante;
            nombreSeleccion.textContent = estudiante.estudiante;
            detalleSeleccion.textContent = describir(estudiante);

            entrada.classList.add("d-none");
            entrada.classList.remove("is-invalid");
            seleccion.classList.remove("d-none");
            cerrarLista();

            opciones.alCambiar?.(estudiante);
        }

        function limpiar() {
            abortador?.abort();
            clearTimeout(temporizador);

            oculto.value = "";
            entrada.value = "";
            resultados = [];

            seleccion.classList.add("d-none");
            entrada.classList.remove("d-none", "is-invalid");
            cerrarLista();
        }

        // ---------- eventos ----------

        entrada.addEventListener("input", function () {
            entrada.classList.remove("is-invalid");
            clearTimeout(temporizador);

            const texto = entrada.value.trim();

            if (texto.length < MINIMO_LETRAS) {
                abortador?.abort();

                if (texto.length === 0) {
                    cerrarLista();
                } else {
                    mostrarMensaje(`Escriba al menos ${MINIMO_LETRAS} letras.`);
                }
                return;
            }

            // Espera a que el usuario deje de escribir para no consultar en cada tecla
            temporizador = setTimeout(() => buscar(texto), ESPERA_MS);
        });

        entrada.addEventListener("keydown", function (evento) {
            const abierta = !lista.classList.contains("d-none");

            if (evento.key === "Escape") {
                cerrarLista();
                return;
            }

            if (!abierta || resultados.length === 0) {
                // Enter con la lista abierta pero sin resultados no debe enviar el formulario
                if (abierta && evento.key === "Enter") evento.preventDefault();
                return;
            }

            switch (evento.key) {
                case "ArrowDown":
                    evento.preventDefault();
                    marcarActiva((indiceActivo + 1) % resultados.length);
                    break;
                case "ArrowUp":
                    evento.preventDefault();
                    marcarActiva((indiceActivo - 1 + resultados.length) % resultados.length);
                    break;
                case "Enter":
                    evento.preventDefault();
                    if (indiceActivo >= 0) seleccionar(resultados[indiceActivo]);
                    break;
            }
        });

        // mousedown (no click) + preventDefault: elige antes de que el input pierda el foco
        lista.addEventListener("mousedown", function (evento) {
            const opcion = evento.target.closest("[data-indice]");
            if (!opcion) return;

            evento.preventDefault();
            seleccionar(resultados[Number(opcion.dataset.indice)]);
        });

        entrada.addEventListener("blur", () => setTimeout(cerrarLista, 150));

        botonQuitar.addEventListener("click", function () {
            limpiar();
            entrada.focus();
            opciones.alCambiar?.(null);
        });

        // ---------- API publica ----------

        return {
            get valor() {
                return oculto.value;
            },
            validar() {
                const valido = oculto.value !== "";
                entrada.classList.toggle("is-invalid", !valido);
                return valido;
            },
            limpiar,
            enfocar() {
                (oculto.value ? botonQuitar : entrada).focus();
            }
        };
    };
})();