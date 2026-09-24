document.addEventListener("DOMContentLoaded", function () {

    const buscador = document.getElementById("BuscarEstudiante");
    const filtroNivel = document.getElementById("FiltroNivel");
    const filas = document.querySelectorAll("#TablaEstudiantes tbody tr[data-fila]");
    const estadoVacio = document.getElementById("EstadoVacio");

    function filtrarEstudiantes() {

        const texto = buscador.value.toLowerCase().trim();
        const nivel = filtroNivel.value.toLowerCase().trim();

        let visibles = 0;

        filas.forEach(function (fila) {

            const contenido = fila.textContent.toLowerCase();
            const nivelFila = (fila.dataset.estado || "").toLowerCase();

            const coincideTexto =
                texto === "" || contenido.includes(texto);

            const coincideNivel =
                nivel === "" || nivelFila === nivel;

            if (coincideTexto && coincideNivel) {
                fila.style.display = "";
                visibles++;
            } else {
                fila.style.display = "none";
            }

        });

        if (visibles === 0) {
            estadoVacio.classList.remove("d-none");
        } else {
            estadoVacio.classList.add("d-none");
        }
    }

    if (buscador) {
        buscador.addEventListener("input", filtrarEstudiantes);
    }

    if (filtroNivel) {
        filtroNivel.addEventListener("change", filtrarEstudiantes);
    }


    // Tooltips
    const tooltips = document.querySelectorAll(
        '[data-bs-toggle="tooltip"]'
    );

    tooltips.forEach(function (elemento) {
        new bootstrap.Tooltip(elemento);
    });

});