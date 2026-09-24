document.addEventListener("DOMContentLoaded", function () {

    const buscador =
        document.querySelector("[data-buscar-texto]");

    const filtroEstado =
        document.querySelector("[data-buscar-estado]");

    const tabla =
        document.getElementById("TablaClientes");


    // =====================================================
    // FILTRAR CLIENTES
    // =====================================================

    function filtrarClientes() {

        if (!buscador || !filtroEstado || !tabla)
            return;

        const texto =
            buscador.value.toLowerCase().trim();

        const estado =
            filtroEstado.value.toLowerCase();

        const filas =
            tabla.querySelectorAll("tbody tr[data-fila]");

        filas.forEach(function (fila) {

            const contenido =
                fila.innerText.toLowerCase();

            const estadoFila =
                (fila.getAttribute("data-estado") || "")
                    .toLowerCase();

            const coincideTexto =
                texto === "" ||
                contenido.includes(texto);

            const coincideEstado =
                estado === "" ||
                estadoFila === estado;

            if (coincideTexto && coincideEstado) {

                fila.style.display = "";

            } else {

                fila.style.display = "none";

            }

        });
    }


    // =====================================================
    // BUSCADOR
    // =====================================================

    if (buscador) {

        buscador.addEventListener(
            "input",
            filtrarClientes
        );

    }


    // =====================================================
    // FILTRO DE ESTADO
    // =====================================================

    if (filtroEstado) {

        filtroEstado.addEventListener(
            "change",
            filtrarClientes
        );

    }


    // =====================================================
    // BOTONES DE ACCIONES
    // =====================================================

    if (tabla) {

        tabla.addEventListener("click", function (evento) {

            const boton =
                evento.target.closest("button[data-accion]");

            if (!boton)
                return;

            const accion =
                boton.getAttribute("data-accion");

            const id =
                boton.getAttribute("data-id");

            const fila =
                boton.closest("tr");


            // =============================================
            // VER CLIENTE
            // =============================================

            if (accion === "ver") {

                document.getElementById("DetalleEncargado").textContent =
                    fila.querySelector('[data-campo="encargado"]')
                        ?.innerText.trim() || "-";

                document.getElementById("DetalleTelefono").textContent =
                    fila.querySelector('[data-campo="telefono"]')
                        ?.innerText.trim() || "-";

                document.getElementById("DetalleCorreo").textContent =
                    fila.querySelector('[data-campo="correo"]')
                        ?.innerText.trim() || "-";

                document.getElementById("DetalleServicio").textContent =
                    fila.querySelector('[data-campo="servicio"]')
                        ?.innerText.trim() || "-";

                document.getElementById("DetalleEstudiante").textContent =
                    fila.querySelector('[data-campo="estudiante"]')
                        ?.innerText.trim() || "-";

                document.getElementById("DetalleEstado").textContent =
                    fila.querySelector('[data-campo-badge="estado"]')
                        ?.innerText.trim() || "-";


                const modal =
                    new bootstrap.Modal(
                        document.getElementById(
                            "ModalDetalleCliente"
                        )
                    );

                modal.show();
            }


            // =============================================
            // EDITAR CLIENTE
            // =============================================

            if (accion === "editar") {

                alert(
                    "La edición de clientes se conectará después."
                );

            }


            // =============================================
            // ELIMINAR CLIENTE
            // =============================================

            if (accion === "eliminar") {

                const nombre =
                    fila.querySelector(
                        '[data-campo="encargado"]'
                    )?.innerText.trim() || "";

                document.getElementById(
                    "NombreClienteEliminar"
                ).textContent = nombre;

                const botonConfirmar =
                    document.getElementById(
                        "BotonConfirmarEliminarCliente"
                    );

                botonConfirmar.setAttribute(
                    "data-id",
                    id
                );

                const modal =
                    new bootstrap.Modal(
                        document.getElementById(
                            "ModalEliminarCliente"
                        )
                    );

                modal.show();
            }

        });

    }


    // =====================================================
    // CONFIRMAR ELIMINACIÓN
    // =====================================================

    const botonConfirmarEliminar =
        document.getElementById(
            "BotonConfirmarEliminarCliente"
        );

    if (botonConfirmarEliminar) {

        botonConfirmarEliminar.addEventListener(
            "click",
            async function () {

                const id =
                    this.getAttribute("data-id");

                if (!id)
                    return;

                const response =
                    await fetch(
                        "/Admin/DesactivarCliente?id=" + id,
                        {
                            method: "POST"
                        }
                    );

                if (response.ok) {

                    location.reload();

                } else {

                    alert(
                        "No se pudo desactivar el cliente."
                    );

                }

            }
        );

    }


    // =====================================================
    // TOOLTIPS
    // =====================================================

    const tooltipTriggerList =
        document.querySelectorAll(
            '[data-bs-toggle="tooltip"]'
        );

    tooltipTriggerList.forEach(function (element) {

        new bootstrap.Tooltip(element);

    });

});