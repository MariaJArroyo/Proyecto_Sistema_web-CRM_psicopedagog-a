// Detalles de movimiento del portal de familias. Nada de esto es necesario
// para usar el portal: si el script no carga, todo se ve y funciona igual.
//
//   - Las tarjetas aparecen en cascada al cargar (y al cambiar de pestana).
//   - Los numeros con data-contar suben desde 0.
//   - Las pestanas internas abren la que pide el #hash de la URL.
//   - Los textos largos recortados ganan un "Ver mas".
//   - En celular, la pestana activa del menu queda a la vista.
(function () {
    "use strict";

    const sinMovimiento = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    const SELECTOR_ANIMABLES = [
        ".BienvenidaPortal",
        ".AvisoSaldoPortal",
        ".IndicadorPortal",
        ".FranjaPortal",
        "article.Tarjeta",
        ".LineaTiempoPortal > li",
        ".AcordeonPortal > details",
        ".EstadoVacio"
    ].join(",");

    function animar(contenedor) {
        if (sinMovimiento || !contenedor) return;

        const elementos = Array.from(contenedor.querySelectorAll(SELECTOR_ANIMABLES))
            // Lo que esta dentro de una pestana oculta se anima cuando se muestre
            .filter(el => !el.closest(".tab-pane:not(.active)"));

        elementos.forEach((el, i) => {
            el.style.setProperty("--retraso", `${Math.min(i, 10) * 55}ms`);
            el.classList.add("Animar");
            // Al terminar se quita la clase: si se queda, la animacion
            // congela el transform y el efecto hover deja de funcionar
            el.addEventListener("animationend", () => {
                el.classList.remove("Animar");
                el.style.removeProperty("--retraso");
            }, { once: true });
        });
    }

    function contar(el) {
        const final = Number(el.dataset.contar);
        if (!Number.isFinite(final) || final <= 0 || sinMovimiento) return;

        const duracion = 700;
        const inicio = performance.now();

        function paso(ahora) {
            const t = Math.min((ahora - inicio) / duracion, 1);
            const suavizado = 1 - Math.pow(1 - t, 3);
            el.textContent = String(Math.round(final * suavizado));
            if (t < 1) requestAnimationFrame(paso);
        }

        el.textContent = "0";
        requestAnimationFrame(paso);
    }

    function abrirPestanaDelHash() {
        const id = decodeURIComponent(window.location.hash.slice(1));
        if (!id || !window.bootstrap?.Tab) return;

        const panel = document.getElementById(id);
        if (!panel || !panel.classList.contains("tab-pane")) return;

        const boton = document.querySelector(`[data-bs-target="#${CSS.escape(id)}"]`);
        if (boton) window.bootstrap.Tab.getOrCreateInstance(boton).show();
    }

    function prepararVerMas(raiz) {
        raiz.querySelectorAll(".ItemPortal-Texto").forEach(texto => {
            if (texto.dataset.verMas === "1") return;
            if (texto.scrollHeight <= texto.clientHeight + 2) return;

            texto.dataset.verMas = "1";

            const boton = document.createElement("button");
            boton.type = "button";
            boton.className = "VerMasPortal";
            boton.textContent = "Ver más";
            boton.setAttribute("aria-expanded", "false");

            boton.addEventListener("click", () => {
                const abierto = texto.classList.toggle("Expandido");
                boton.textContent = abierto ? "Ver menos" : "Ver más";
                boton.setAttribute("aria-expanded", String(abierto));
            });

            texto.after(boton);
        });
    }

    document.addEventListener("DOMContentLoaded", () => {
        const principal = document.querySelector("main");

        abrirPestanaDelHash();
        animar(principal);

        document.querySelectorAll("[data-contar]").forEach(contar);

        if (principal) prepararVerMas(principal);

        // Al cambiar de pestana interna: animar lo nuevo, medir los textos
        // (en una pestana oculta no tienen alto) y dejar el #hash en la URL
        // para que recargar o compartir el enlace abra la misma pestana
        document.addEventListener("shown.bs.tab", evento => {
            const destino = evento.target.getAttribute("data-bs-target");
            const panel = destino ? document.querySelector(destino) : null;
            if (!panel) return;

            animar(panel);
            prepararVerMas(panel);

            if (panel.id && window.history?.replaceState) {
                window.history.replaceState(null, "", `#${panel.id}`);
            }
        });

        window.addEventListener("hashchange", abrirPestanaDelHash);

        // En celular el menu se desplaza de lado: mostrar la pestana activa
        const activa = document.querySelector(".PestanasPortal-Enlace.active");
        if (activa && activa.scrollIntoView) {
            activa.scrollIntoView({ block: "nearest", inline: "center" });
        }
    });
})();
