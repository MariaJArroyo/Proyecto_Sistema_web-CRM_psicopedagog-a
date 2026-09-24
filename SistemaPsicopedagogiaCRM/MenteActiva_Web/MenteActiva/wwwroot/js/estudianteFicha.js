document.addEventListener("DOMContentLoaded", function () {

    const formularioAgregarArea =
        document.getElementById("FormularioAgregarArea");

    const formularioEditarFicha =
        document.getElementById("FormularioEditarFicha");


    // Agregar área visualmente
    if (formularioAgregarArea) {

        formularioAgregarArea.addEventListener("submit", function (evento) {

            evento.preventDefault();

            const campoArea =
                document.getElementById("NombreArea");

            const campoObservacion =
                document.getElementById("ObservacionArea");

            if (!campoArea.value.trim()) {
                campoArea.focus();
                return;
            }

            const lista =
                document.getElementById("ListaAreasDificultad");

            const item =
                document.createElement("div");

            item.className =
                "list-group-item px-0";

            item.innerHTML =
                '<span class="badge badge-etiqueta-morado mb-1"></span>' +
                '<p class="mb-0 TextoSuave"></p>';

            item.querySelector(".badge").textContent =
                campoArea.value.trim();

            item.querySelector("p").textContent =
                campoObservacion.value.trim();

            lista.appendChild(item);

            formularioAgregarArea.reset();

            bootstrap.Modal
                .getOrCreateInstance(
                    document.getElementById("ModalAgregarArea")
                )
                .hide();

        });

    }


    // Editar ficha visualmente
    if (formularioEditarFicha) {

        formularioEditarFicha.addEventListener("submit", function (evento) {

            evento.preventDefault();

            const nombre =
                document.getElementById("EditarNombreFicha").value;

            const edad =
                document.getElementById("EditarEdadFicha").value;

            const nivel =
                document.getElementById("EditarNivelFicha").value;

            const institucion =
                document.getElementById("EditarInstitucionFicha").value;

            const areas =
                document.getElementById("EditarMateriaFicha").value;

            const observaciones =
                document.getElementById("EditarResumenFicha").value;


            document.getElementById("FichaNombre").textContent =
                nombre;

            document.getElementById("FichaDatoNombre").textContent =
                nombre;

            document.getElementById("FichaDatoEdad").textContent =
                edad;

            document.getElementById("FichaDatoNivel").textContent =
                nivel;

            document.getElementById("FichaDatoInstitucion").textContent =
                institucion;

            document.getElementById("FichaDatoMateria").textContent =
                areas;

            document.getElementById("FichaDatoResumen").textContent =
                observaciones;


            bootstrap.Modal
                .getOrCreateInstance(
                    document.getElementById("ModalEditarFicha")
                )
                .hide();

        });

    }

});