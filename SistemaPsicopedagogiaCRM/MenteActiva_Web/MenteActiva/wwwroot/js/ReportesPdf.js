(function () {
    "use strict";

    let exportando = false;

    function texto(valor) {
        return valor == null || String(valor).trim() === ""
            ? "No registrado"
            : String(valor);
    }

    function fecha(valor) {
        const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(valor || ""));

        return partes
            ? `${partes[3]}-${partes[2]}-${partes[1]}`
            : "No registrada";
    }

    function campo(nombre, valor) {
        return {
            stack: [
                {
                    text: nombre,
                    bold: true,
                    color: "#617087",
                    fontSize: 9,
                    margin: [0, 0, 0, 3]
                },
                {
                    text: texto(valor)
                }
            ],
            margin: [0, 0, 0, 10]
        };
    }

    function tituloSeccion(valor) {
        return {
            text: valor,
            style: "seccion"
        };
    }

    function filas(campos) {
        const resultado = [];

        for (let i = 0; i < campos.length; i += 2) {
            resultado.push({
                columns: [
                    {
                        width: "*",
                        ...campo(...campos[i])
                    },
                    campos[i + 1]
                        ? {
                            width: "*",
                            ...campo(...campos[i + 1])
                        }
                        : {
                            width: "*",
                            text: ""
                        }
                ],
                columnGap: 22
            });
        }

        return resultado;
    }

    function crearDefinicion(reporte) {
        let datos = null;

        try {
            const contenido = JSON.parse(reporte.contenido);

            if (contenido && typeof contenido === "object" && !Array.isArray(contenido)) {
                datos = contenido;
            }
        } catch {
            // También admite reportes anteriores guardados como texto.
        }

        const estudiante = datos?.estudiante || {};
        const hora = reporte.fechaGeneracion
            ? new Date(reporte.fechaGeneracion)
            : null;

        const fechaGeneracion = fecha(reporte.fechaGeneracion) +
            (hora && !isNaN(hora)
                ? ", " + hora.toLocaleTimeString("es-CR", {
                    hour: "2-digit",
                    minute: "2-digit"
                })
                : "");

        const contenidoPdf = [
            {
                text: texto(reporte.titulo),
                style: "titulo"
            },
            {
                columns: [
                    {
                        width: "*",
                        stack: [
                            campo("Estudiante", estudiante.nombre || reporte.estudiante),
                            campo("Nivel educativo", estudiante.nivel),
                            campo("Institución", estudiante.institucion),
                            campo("Áreas de dificultad", estudiante.areasDificultad)
                        ]
                    },
                    {
                        width: "*",
                        stack: [
                            campo(
                                "Período",
                                `${fecha(reporte.periodoInicio)} - ${fecha(reporte.periodoFin)}`
                            ),
                            campo("Fecha de generación", fechaGeneracion),
                            campo("Generado por", reporte.generadoPor)
                        ]
                    }
                ],
                columnGap: 22
            }
        ];

        if (!datos) {
            contenidoPdf.push({
                text: reporte.contenido || "Sin contenido adicional registrado.",
                margin: [0, 12, 0, 0]
            });
        }

        if (Array.isArray(datos?.sesiones)) {
            contenidoPdf.push(tituloSeccion("Historial de sesiones"));

            if (!datos.sesiones.length) {
                contenidoPdf.push({
                    text: "Sin sesiones registradas en este reporte."
                });
            }

            const sesiones = [...datos.sesiones].sort((a, b) =>
                String(b.fecha || "").localeCompare(String(a.fecha || ""))
            );

            sesiones.forEach(sesion => {
                contenidoPdf.push({
                    text: `${fecha(sesion.fecha)} · ${texto(sesion.tipoAtencion)}`,
                    style: "subtitulo"
                });

                contenidoPdf.push(...filas([
                    [
                        "Duración",
                        sesion.duracionMinutos != null
                            ? `${sesion.duracionMinutos} minutos`
                            : null
                    ],
                    ["Objetivo de la sesión", sesion.objetivo],
                    ["Tema trabajado", sesion.tema],
                    ["Actividades realizadas", sesion.actividadesRealizadas],
                    ["Avances", sesion.avances],
                    ["Objetivos alcanzados", sesion.objetivosAlcanzados],
                    ["Habilidades desarrolladas", sesion.habilidadesDesarrolladas],
                    ["Acuerdos", sesion.acuerdos],
                    ["Recomendaciones", sesion.recomendaciones],
                    ["Observaciones de la sesión", sesion.observaciones]
                ]));
            });
        }

        if (Array.isArray(datos?.planes)) {
            contenidoPdf.push(tituloSeccion("Planes de intervención"));

            if (!datos.planes.length) {
                contenidoPdf.push({
                    text: "Sin planes registrados en este reporte."
                });
            }

            datos.planes.forEach(plan => {
                contenidoPdf.push({
                    text: `${texto(plan.titulo)} · ${texto(plan.estado)}`,
                    style: "subtitulo"
                });

                contenidoPdf.push(campo("Objetivo general", plan.objetivoGeneral));

                contenidoPdf.push(...filas([
                    ["Fecha de inicio", fecha(plan.fechaInicio)],
                    ["Fecha de finalización", fecha(plan.fechaFin)]
                ]));

                if (plan.actividades != null) {
                    contenidoPdf.push(campo(
                        "Cumplimiento",
                        `${plan.actividadesCompletadas ?? 0} de ${plan.actividades} actividades completadas`
                    ));
                }

                if (plan.observaciones) {
                    contenidoPdf.push(campo(
                        "Observaciones del plan",
                        plan.observaciones
                    ));
                }

                const estrategias = Array.isArray(plan.estrategias)
                    ? [...plan.estrategias]
                    : [];

                estrategias.sort((a, b) =>
                    (a.orden ?? Number.MAX_SAFE_INTEGER) -
                    (b.orden ?? Number.MAX_SAFE_INTEGER)
                );

                if (!estrategias.length) {
                    contenidoPdf.push({
                        text: "Sin estrategias registradas.",
                        margin: [0, 0, 0, 10]
                    });
                }

                estrategias.forEach((estrategia, indice) => {
                    contenidoPdf.push(campo(
                        `Estrategia ${indice + 1}`,
                        estrategia.descripcion
                    ));

                    const actividades = Array.isArray(estrategia.actividades)
                        ? estrategia.actividades
                        : [];

                    if (!actividades.length) {
                        contenidoPdf.push({
                            text: "Sin actividades registradas.",
                            margin: [0, 0, 0, 10]
                        });
                    }

                    actividades.forEach(actividad => {
                        const completada = actividad.completada === true ||
                            actividad.completada === 1;

                        contenidoPdf.push({
                            text: `${completada ? "Completada" : "Pendiente"}: ${texto(actividad.nombre)}`,
                            bold: true,
                            margin: [8, 0, 0, 4]
                        });

                        if (actividad.descripcion) {
                            contenidoPdf.push({
                                text: String(actividad.descripcion),
                                margin: [8, 0, 0, 10]
                            });
                        }
                    });
                });
            });
        }

        return {
            pageSize: "A4",
            pageMargins: [40, 40, 40, 48],

            info: {
                title: texto(reporte.titulo),
                author: texto(reporte.generadoPor)
            },

            defaultStyle: {
                font: "Roboto",
                fontSize: 10,
                color: "#283444",
                lineHeight: 1.2
            },

            styles: {
                titulo: {
                    fontSize: 17,
                    bold: true,
                    margin: [0, 0, 0, 18]
                },
                seccion: {
                    fontSize: 13,
                    bold: true,
                    color: "#34527a",
                    margin: [0, 15, 0, 12]
                },
                subtitulo: {
                    fontSize: 11,
                    bold: true,
                    margin: [0, 8, 0, 10]
                }
            },

            content: contenidoPdf,

            footer: (pagina, total) => ({
                text: `Página ${pagina} de ${total}`,
                alignment: "right",
                margin: [40, 12, 40, 0],
                fontSize: 8,
                color: "#617087"
            }),

            pageBreakBefore: (nodo, contenedor) =>
                (nodo.style === "seccion" || nodo.style === "subtitulo") &&
                contenedor.getFollowingNodesOnPage().length === 0
        };
    }

    function aviso(mensaje, tipo) {
        const alerta = document.getElementById("AlertaReportes");

        alerta.className = `alert alert-${tipo} mt-3`;
        alerta.textContent = mensaje;
    }

    async function exportar(id, boton) {
        if (exportando) return;

        exportando = true;
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");

        const iconoOriginal = boton.innerHTML;

        boton.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';

        aviso("Preparando el PDF…", "info");

        try {
            if (!window.pdfMake) {
                throw new Error(
                    "No se cargó la herramienta de PDF. Revise la conexión y recargue la página."
                );
            }

            const respuesta = await fetch(
                `/Admin/DetalleReporte?id=${encodeURIComponent(id)}`,
                {
                    headers: {
                        Accept: "application/json"
                    }
                }
            );

            if (!respuesta.ok) {
                let mensaje = "El reporte solicitado no está disponible.";

                try {
                    const error = await respuesta.json();
                    mensaje = error.mensaje || mensaje;
                } catch {
                    // Conserva el mensaje general.
                }

                throw new Error(mensaje);
            }

            const reporte = await respuesta.json();

            const tituloArchivo = String(reporte.titulo || "Reporte")
                .replace(/[<>:"/\\|?*\u0000-\u001F]/g, "")
                .trim()
                .slice(0, 150)
                .replace(/[. ]+$/, "");

            const nombreArchivo = `${tituloArchivo || "Reporte"}.pdf`;

            await window.pdfMake
                .createPdf(crearDefinicion(reporte))
                .download(nombreArchivo);

            aviso(
                "Reporte exportado exitosamente. La descarga fue enviada al navegador.",
                "success"
            );
        } catch (error) {
            aviso(
                error.message || "No se pudo exportar el reporte. Inténtelo nuevamente.",
                "danger"
            );
        } finally {
            exportando = false;
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
            boton.innerHTML = iconoOriginal;
        }
    }

    document.getElementById("CuerpoTablaReportes").addEventListener("click", evento => {
        const boton = evento.target.closest('button[data-accion="pdf"]');

        if (boton && !boton.disabled) {
            exportar(boton.dataset.id, boton);
        }
    });
})();(function () {
    "use strict";

    let exportando = false;

    function texto(valor) {
        return valor == null || String(valor).trim() === ""
            ? "No registrado"
            : String(valor);
    }

    function fecha(valor) {
        const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(valor || ""));

        return partes
            ? `${partes[3]}-${partes[2]}-${partes[1]}`
            : "No registrada";
    }

    function campo(nombre, valor) {
        return {
            stack: [
                {
                    text: nombre,
                    bold: true,
                    color: "#617087",
                    fontSize: 9,
                    margin: [0, 0, 0, 3]
                },
                {
                    text: texto(valor)
                }
            ],
            margin: [0, 0, 0, 10]
        };
    }

    function tituloSeccion(valor) {
        return {
            text: valor,
            style: "seccion"
        };
    }

    function filas(campos) {
        const resultado = [];

        for (let i = 0; i < campos.length; i += 2) {
            resultado.push({
                columns: [
                    {
                        width: "*",
                        ...campo(...campos[i])
                    },
                    campos[i + 1]
                        ? {
                            width: "*",
                            ...campo(...campos[i + 1])
                        }
                        : {
                            width: "*",
                            text: ""
                        }
                ],
                columnGap: 22
            });
        }

        return resultado;
    }

    function crearDefinicion(reporte) {
        let datos = null;

        try {
            const contenido = JSON.parse(reporte.contenido);

            if (contenido && typeof contenido === "object" && !Array.isArray(contenido)) {
                datos = contenido;
            }
        } catch {
            // También admite reportes anteriores guardados como texto.
        }

        const estudiante = datos?.estudiante || {};
        const hora = reporte.fechaGeneracion
            ? new Date(reporte.fechaGeneracion)
            : null;

        const fechaGeneracion = fecha(reporte.fechaGeneracion) +
            (hora && !isNaN(hora)
                ? ", " + hora.toLocaleTimeString("es-CR", {
                    hour: "2-digit",
                    minute: "2-digit"
                })
                : "");

        const contenidoPdf = [
            {
                text: texto(reporte.titulo),
                style: "titulo"
            },
            {
                columns: [
                    {
                        width: "*",
                        stack: [
                            campo("Estudiante", estudiante.nombre || reporte.estudiante),
                            campo("Nivel educativo", estudiante.nivel),
                            campo("Institución", estudiante.institucion),
                            campo("Áreas de dificultad", estudiante.areasDificultad)
                        ]
                    },
                    {
                        width: "*",
                        stack: [
                            campo(
                                "Período",
                                `${fecha(reporte.periodoInicio)} - ${fecha(reporte.periodoFin)}`
                            ),
                            campo("Fecha de generación", fechaGeneracion),
                            campo("Generado por", reporte.generadoPor)
                        ]
                    }
                ],
                columnGap: 22
            }
        ];

        if (!datos) {
            contenidoPdf.push({
                text: reporte.contenido || "Sin contenido adicional registrado.",
                margin: [0, 12, 0, 0]
            });
        }

        if (Array.isArray(datos?.sesiones)) {
            contenidoPdf.push(tituloSeccion("Historial de sesiones"));

            if (!datos.sesiones.length) {
                contenidoPdf.push({
                    text: "Sin sesiones registradas en este reporte."
                });
            }

            const sesiones = [...datos.sesiones].sort((a, b) =>
                String(b.fecha || "").localeCompare(String(a.fecha || ""))
            );

            sesiones.forEach(sesion => {
                contenidoPdf.push({
                    text: `${fecha(sesion.fecha)} · ${texto(sesion.tipoAtencion)}`,
                    style: "subtitulo"
                });

                contenidoPdf.push(...filas([
                    [
                        "Duración",
                        sesion.duracionMinutos != null
                            ? `${sesion.duracionMinutos} minutos`
                            : null
                    ],
                    ["Objetivo de la sesión", sesion.objetivo],
                    ["Tema trabajado", sesion.tema],
                    ["Actividades realizadas", sesion.actividadesRealizadas],
                    ["Avances", sesion.avances],
                    ["Objetivos alcanzados", sesion.objetivosAlcanzados],
                    ["Habilidades desarrolladas", sesion.habilidadesDesarrolladas],
                    ["Acuerdos", sesion.acuerdos],
                    ["Recomendaciones", sesion.recomendaciones],
                    ["Observaciones de la sesión", sesion.observaciones]
                ]));
            });
        }

        if (Array.isArray(datos?.planes)) {
            contenidoPdf.push(tituloSeccion("Planes de intervención"));

            if (!datos.planes.length) {
                contenidoPdf.push({
                    text: "Sin planes registrados en este reporte."
                });
            }

            datos.planes.forEach(plan => {
                contenidoPdf.push({
                    text: `${texto(plan.titulo)} · ${texto(plan.estado)}`,
                    style: "subtitulo"
                });

                contenidoPdf.push(campo("Objetivo general", plan.objetivoGeneral));

                contenidoPdf.push(...filas([
                    ["Fecha de inicio", fecha(plan.fechaInicio)],
                    ["Fecha de finalización", fecha(plan.fechaFin)]
                ]));

                if (plan.actividades != null) {
                    contenidoPdf.push(campo(
                        "Cumplimiento",
                        `${plan.actividadesCompletadas ?? 0} de ${plan.actividades} actividades completadas`
                    ));
                }

                if (plan.observaciones) {
                    contenidoPdf.push(campo(
                        "Observaciones del plan",
                        plan.observaciones
                    ));
                }

                const estrategias = Array.isArray(plan.estrategias)
                    ? [...plan.estrategias]
                    : [];

                estrategias.sort((a, b) =>
                    (a.orden ?? Number.MAX_SAFE_INTEGER) -
                    (b.orden ?? Number.MAX_SAFE_INTEGER)
                );

                if (!estrategias.length) {
                    contenidoPdf.push({
                        text: "Sin estrategias registradas.",
                        margin: [0, 0, 0, 10]
                    });
                }

                estrategias.forEach((estrategia, indice) => {
                    contenidoPdf.push(campo(
                        `Estrategia ${indice + 1}`,
                        estrategia.descripcion
                    ));

                    const actividades = Array.isArray(estrategia.actividades)
                        ? estrategia.actividades
                        : [];

                    if (!actividades.length) {
                        contenidoPdf.push({
                            text: "Sin actividades registradas.",
                            margin: [0, 0, 0, 10]
                        });
                    }

                    actividades.forEach(actividad => {
                        const completada = actividad.completada === true ||
                            actividad.completada === 1;

                        contenidoPdf.push({
                            text: `${completada ? "Completada" : "Pendiente"}: ${texto(actividad.nombre)}`,
                            bold: true,
                            margin: [8, 0, 0, 4]
                        });

                        if (actividad.descripcion) {
                            contenidoPdf.push({
                                text: String(actividad.descripcion),
                                margin: [8, 0, 0, 10]
                            });
                        }
                    });
                });
            });
        }

        return {
            pageSize: "A4",
            pageMargins: [40, 40, 40, 48],

            info: {
                title: texto(reporte.titulo),
                author: texto(reporte.generadoPor)
            },

            defaultStyle: {
                font: "Roboto",
                fontSize: 10,
                color: "#283444",
                lineHeight: 1.2
            },

            styles: {
                titulo: {
                    fontSize: 17,
                    bold: true,
                    margin: [0, 0, 0, 18]
                },
                seccion: {
                    fontSize: 13,
                    bold: true,
                    color: "#34527a",
                    margin: [0, 15, 0, 12]
                },
                subtitulo: {
                    fontSize: 11,
                    bold: true,
                    margin: [0, 8, 0, 10]
                }
            },

            content: contenidoPdf,

            footer: (pagina, total) => ({
                text: `Página ${pagina} de ${total}`,
                alignment: "right",
                margin: [40, 12, 40, 0],
                fontSize: 8,
                color: "#617087"
            }),

            pageBreakBefore: (nodo, contenedor) =>
                (nodo.style === "seccion" || nodo.style === "subtitulo") &&
                contenedor.getFollowingNodesOnPage().length === 0
        };
    }

    function aviso(mensaje, tipo) {
        const alerta = document.getElementById("AlertaReportes");

        alerta.className = `alert alert-${tipo} mt-3`;
        alerta.textContent = mensaje;
    }

    async function exportar(id, boton) {
        if (exportando) return;

        exportando = true;
        boton.disabled = true;
        boton.setAttribute("aria-busy", "true");

        const iconoOriginal = boton.innerHTML;

        boton.innerHTML = '<span class="spinner-border spinner-border-sm" aria-hidden="true"></span>';

        aviso("Preparando el PDF…", "info");

        try {
            if (!window.pdfMake) {
                throw new Error(
                    "No se cargó la herramienta de PDF. Revise la conexión y recargue la página."
                );
            }

            const respuesta = await fetch(
                `/Admin/DetalleReporte?id=${encodeURIComponent(id)}`,
                {
                    headers: {
                        Accept: "application/json"
                    }
                }
            );

            if (!respuesta.ok) {
                let mensaje = "El reporte solicitado no está disponible.";

                try {
                    const error = await respuesta.json();
                    mensaje = error.mensaje || mensaje;
                } catch {
                    // Conserva el mensaje general.
                }

                throw new Error(mensaje);
            }

            const reporte = await respuesta.json();

            const nombreArchivo =
                `Reporte_${String(reporte.idReporte).replace(/[^0-9]/g, "")}.pdf`;

            await window.pdfMake
                .createPdf(crearDefinicion(reporte))
                .download(nombreArchivo);

            aviso(
                "Reporte exportado exitosamente. La descarga fue enviada al navegador.",
                "success"
            );
        } catch (error) {
            aviso(
                error.message || "No se pudo exportar el reporte. Inténtelo nuevamente.",
                "danger"
            );
        } finally {
            exportando = false;
            boton.disabled = false;
            boton.removeAttribute("aria-busy");
            boton.innerHTML = iconoOriginal;
        }
    }

    document.getElementById("CuerpoTablaReportes").addEventListener("click", evento => {
        const boton = evento.target.closest('button[data-accion="pdf"]');

        if (boton && !boton.disabled) {
            exportar(boton.dataset.id, boton);
        }
    });
})();