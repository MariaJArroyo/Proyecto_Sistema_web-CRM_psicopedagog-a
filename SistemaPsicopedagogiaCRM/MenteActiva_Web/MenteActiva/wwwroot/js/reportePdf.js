// Definicion unica del PDF de un reporte, para el panel y para el portal.
//
// Antes cada pantalla traia su propia copia de este codigo y los dos PDF salian
// con un estilo suelto. Ahora los dos llaman a ReportePdf.crearDefinicion y el
// documento sigue el mismo lenguaje visual que los correos del sistema
// (CascaronCorreo.cs): banda azul con el nombre del consultorio, tarjetas con
// borde y acento lateral, y un pie claro.
//
// Si se cambia la paleta de CascaronCorreo.cs, se cambia tambien aqui.
(function () {
    "use strict";

    // =====================================================
    // PALETA (la misma de los correos)
    // =====================================================

    const COLOR = {
        primario: "#4F6FAE",
        primarioClaro: "#DCE5F4",
        fondo: "#F7F9FC",
        borde: "#E3E8EF",
        texto: "#2E3A48",
        suave: "#6B7A8C",
        exito: "#3E9B6E",
        pendiente: "#C98A1B"
    };

    const CONSULTORIO = "Mente Activa Psicopedagogía";

    // A4 en puntos y los margenes de la pagina
    const PAGINA = { ancho: 595.28, alto: 841.89 };
    const MARGEN = { izquierda: 40, derecha: 40, superior: 40, inferior: 56 };
    const ALTO_BANDA = 84;
    const TAMANO_LOGO = 64;

    // =====================================================
    // UTILIDADES
    // =====================================================

    function texto(valor) {
        return valor == null || String(valor).trim() === ""
            ? "No registrado"
            : String(valor);
    }

    function fecha(valor) {
        const partes = /^(\d{4})-(\d{2})-(\d{2})/.exec(String(valor || ""));
        return partes ? `${partes[3]}-${partes[2]}-${partes[1]}` : "No registrada";
    }

    // =====================================================
    // PIEZAS DEL DOCUMENTO
    // =====================================================

    // Un dato con su etiqueta pequena arriba
    function campo(nombre, valor) {
        return {
            stack: [
                {
                    text: String(nombre).toUpperCase(),
                    bold: true,
                    color: COLOR.suave,
                    fontSize: 7.5,
                    characterSpacing: 0.4,
                    margin: [0, 0, 0, 3]
                },
                {
                    text: texto(valor),
                    color: COLOR.texto
                }
            ],
            margin: [0, 0, 0, 10]
        };
    }

    function filas(campos) {
        const resultado = [];

        for (let i = 0; i < campos.length; i += 2) {
            resultado.push({
                columns: [
                    { width: "*", ...campo(...campos[i]) },
                    campos[i + 1]
                        ? { width: "*", ...campo(...campos[i + 1]) }
                        : { width: "*", text: "" }
                ],
                columnGap: 22
            });
        }

        return resultado;
    }

    // Mensaje de "no hay nada" en gris, en cursiva
    function vacio(valor) {
        return {
            text: valor,
            italics: true,
            color: COLOR.suave,
            margin: [0, 0, 0, 10]
        };
    }

    // Titulo de seccion: texto fuerte y una linea fina debajo, como el titulo y
    // el separador de los correos. Va en un solo nodo para que el salto de
    // pagina no deje la linea sola en la hoja siguiente.
    function tituloSeccion(valor) {
        return {
            style: "seccion",
            margin: [0, 18, 0, 12],
            stack: [
                { text: valor, margin: [0, 0, 0, 5] },
                {
                    canvas: [{
                        type: "line",
                        x1: 0,
                        y1: 0,
                        x2: PAGINA.ancho - MARGEN.izquierda - MARGEN.derecha,
                        y2: 0,
                        lineWidth: 1,
                        lineColor: COLOR.borde
                    }]
                }
            ]
        };
    }

    // Cabecera de cada sesion o plan: una franja suave con el texto en azul
    function subtitulo(valor) {
        return {
            style: "subtitulo",
            margin: [0, 10, 0, 10],
            table: {
                widths: ["*"],
                body: [[{ text: valor, color: COLOR.primario, bold: true, fontSize: 10.5 }]]
            },
            layout: {
                fillColor: COLOR.fondo,
                hLineWidth: () => 0,
                vLineWidth: (i) => (i === 0 ? 3 : 0),
                vLineColor: () => COLOR.primario,
                paddingLeft: () => 10,
                paddingRight: () => 10,
                paddingTop: () => 6,
                paddingBottom: () => 6
            }
        };
    }

    // El recuadro de los correos: fondo claro, borde fino y acento azul a la izquierda
    function tarjeta(contenido, margen) {
        return {
            margin: margen || [0, 0, 0, 6],
            table: {
                widths: ["*"],
                body: [[{ stack: contenido }]]
            },
            layout: {
                fillColor: COLOR.fondo,
                hLineWidth: () => 1,
                hLineColor: () => COLOR.borde,
                vLineWidth: (i) => (i === 0 ? 4 : 1),
                vLineColor: (i) => (i === 0 ? COLOR.primario : COLOR.borde),
                paddingLeft: () => 16,
                paddingRight: () => 16,
                paddingTop: () => 14,
                paddingBottom: () => 4
            },
            // La tarjeta de datos no se parte entre dos hojas
            unbreakable: true
        };
    }

    // Una actividad del plan con su punto de color segun el estado
    function actividad(completada, nombre, descripcion) {
        const color = completada ? COLOR.exito : COLOR.pendiente;

        const filaActividad = {
            columns: [
                {
                    width: 12,
                    canvas: [{ type: "ellipse", x: 4, y: 6, r1: 3.5, r2: 3.5, color }]
                },
                {
                    width: "*",
                    text: [
                        { text: completada ? "Completada:" : "Pendiente:", bold: true, color: COLOR.texto },
                        { text: `  ${texto(nombre)}`, bold: true, color: COLOR.texto }
                    ]
                }
            ],
            margin: [8, 0, 0, descripcion ? 2 : 6]
        };

        if (!descripcion) return [filaActividad];

        return [
            filaActividad,
            {
                text: String(descripcion),
                color: COLOR.suave,
                margin: [28, 0, 0, 8]
            }
        ];
    }

    // =====================================================
    // MARCO DE LA PAGINA (banda, fondo del pie y pie)
    // =====================================================

    function fondoPagina(subtituloBanda, tituloCorrido) {
        return (pagina) => {
            const anchoUtil = PAGINA.ancho - MARGEN.izquierda - MARGEN.derecha;
            const yPie = PAGINA.alto - MARGEN.inferior;

            // Fondo y linea del pie, en todas las hojas
            // Todo lo del fondo va con posicion absoluta: sin ella los dibujos
            // se apilan uno debajo del otro y los de abajo caen fuera de la hoja.
            const origen = { x: 0, y: 0 };

            const pie = [
                {
                    absolutePosition: origen,
                    canvas: [
                        { type: "rect", x: 0, y: yPie, w: PAGINA.ancho, h: MARGEN.inferior, color: COLOR.fondo },
                        { type: "line", x1: 0, y1: yPie, x2: PAGINA.ancho, y2: yPie, lineWidth: 1, lineColor: COLOR.borde }
                    ]
                }
            ];

            if (pagina === 1) {
                // Banda grande, igual que la de los correos
                return [
                    ...pie,
                    {
                        absolutePosition: origen,
                        canvas: [{ type: "rect", x: 0, y: 0, w: PAGINA.ancho, h: ALTO_BANDA, color: COLOR.primario }]
                    },
                    {
                        absolutePosition: { x: MARGEN.izquierda, y: 24 },
                        text: CONSULTORIO,
                        bold: true,
                        fontSize: 18,
                        color: "#FFFFFF"
                    },
                    {
                        absolutePosition: { x: MARGEN.izquierda, y: 49 },
                        width: anchoUtil - TAMANO_LOGO - 16,
                        text: subtituloBanda,
                        fontSize: 10,
                        color: COLOR.primarioClaro
                    },
                    ...(window.ReportePdfLogo
                        ? [{
                            absolutePosition: {
                                x: PAGINA.ancho - MARGEN.derecha - TAMANO_LOGO,
                                y: (ALTO_BANDA - TAMANO_LOGO) / 2
                            },
                            image: window.ReportePdfLogo,
                            width: TAMANO_LOGO,
                            height: TAMANO_LOGO
                        }]
                        : [])
                ];
            }

            // Hojas siguientes: una franja delgada y el titulo corrido
            return [
                ...pie,
                {
                    absolutePosition: origen,
                    canvas: [{ type: "rect", x: 0, y: 0, w: PAGINA.ancho, h: 6, color: COLOR.primario }]
                },
                {
                    absolutePosition: { x: MARGEN.izquierda, y: 17 },
                    width: anchoUtil,
                    text: `${CONSULTORIO}  ·  ${tituloCorrido}`,
                    fontSize: 8,
                    color: COLOR.suave
                }
            ];
        };
    }

    function pie(pagina, total) {
        return {
            margin: [MARGEN.izquierda, 22, MARGEN.derecha, 0],
            columns: [
                {
                    width: "*",
                    text: "Documento confidencial de uso exclusivo de la familia y el consultorio.",
                    fontSize: 8,
                    color: COLOR.suave
                },
                {
                    width: "auto",
                    text: `Página ${pagina} de ${total}`,
                    fontSize: 8,
                    color: COLOR.suave
                }
            ]
        };
    }

    // =====================================================
    // CONTENIDO DEL REPORTE
    // =====================================================

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
        const hora = reporte.fechaGeneracion ? new Date(reporte.fechaGeneracion) : null;

        const fechaGeneracion = fecha(reporte.fechaGeneracion) +
            (hora && !isNaN(hora)
                ? ", " + hora.toLocaleTimeString("es-CR", {
                    hour: "2-digit",
                    minute: "2-digit"
                })
                : "");

        const areasActuales = Array.isArray(estudiante.areasTrabajadas)
            ? (estudiante.areasDificultad || "Sin áreas actuales")
            : estudiante.areasDificultad;

        const tituloReporte = texto(reporte.titulo);

        const contenidoPdf = [
            {
                text: tituloReporte,
                style: "titulo",
                // En la primera hoja hay que bajar de la banda
                margin: [0, ALTO_BANDA - MARGEN.superior + 14, 0, 14]
            },
            tarjeta([
                {
                    columns: [
                        {
                            width: "*",
                            stack: [
                                campo("Estudiante", estudiante.nombre || reporte.estudiante),
                                campo("Nivel educativo", estudiante.nivel),
                                campo("Institución", estudiante.institucion),
                                campo("Áreas de dificultad actuales", areasActuales)
                            ]
                        },
                        {
                            width: "*",
                            stack: [
                                campo("Período", `${fecha(reporte.periodoInicio)} - ${fecha(reporte.periodoFin)}`),
                                campo("Fecha de generación", fechaGeneracion),
                                campo("Generado por", reporte.generadoPor)
                            ]
                        }
                    ],
                    columnGap: 22
                }
            ])
        ];

        // Reportes anteriores guardados como texto.
        if (!datos) {
            contenidoPdf.push({
                margin: [0, 12, 0, 0],
                ...tarjeta([
                    { text: reporte.contenido || "Sin contenido adicional registrado.", margin: [0, 0, 0, 10] }
                ])
            });
        }

        // Cada sesion y cada plan van en un bloque que no se parte entre hojas.
        // El titulo de la seccion viaja dentro del primer bloque para que no
        // se quede solo al final de una hoja.
        const bloque = (piezas) => ({ unbreakable: true, stack: piezas });

        // Historial de sesiones.
        if (Array.isArray(datos?.sesiones)) {
            if (!datos.sesiones.length) {
                contenidoPdf.push(tituloSeccion("Historial de sesiones"));
                contenidoPdf.push(vacio("Sin sesiones registradas en este reporte."));
            }

            const sesiones = [...datos.sesiones].sort((a, b) =>
                String(b.fecha || "").localeCompare(String(a.fecha || ""))
            );

            sesiones.forEach((sesion, indice) => {
                const piezas = [
                    subtitulo(`${fecha(sesion.fecha)} · ${texto(sesion.tipoAtencion)}`),
                    ...filas([
                        ["Duración", sesion.duracionMinutos != null ? `${sesion.duracionMinutos} minutos` : null],
                        ["Objetivo de la sesión", sesion.objetivo],
                        ["Tema trabajado", sesion.tema],
                        ["Actividades realizadas", sesion.actividadesRealizadas],
                        ["Avances", sesion.avances],
                        ["Objetivos alcanzados", sesion.objetivosAlcanzados],
                        ["Habilidades desarrolladas", sesion.habilidadesDesarrolladas],
                        ["Acuerdos", sesion.acuerdos],
                        ["Recomendaciones", sesion.recomendaciones],
                        ["Observaciones de la sesión", sesion.observaciones]
                    ])
                ];

                if (indice === 0) piezas.unshift(tituloSeccion("Historial de sesiones"));

                contenidoPdf.push(bloque(piezas));
            });
        }

        // Planes de intervención.
        if (Array.isArray(datos?.planes)) {
            if (!datos.planes.length) {
                contenidoPdf.push(tituloSeccion("Planes de intervención"));
                contenidoPdf.push(vacio("Sin planes registrados en este reporte."));
            }

            datos.planes.forEach((plan, indicePlan) => {
                const cabecera = [
                    subtitulo(`${texto(plan.titulo)} · ${texto(plan.estado)}`),
                    campo("Objetivo general", plan.objetivoGeneral),
                    ...filas([
                        ["Fecha de inicio", fecha(plan.fechaInicio)],
                        ["Fecha de finalización", fecha(plan.fechaFin)]
                    ])
                ];

                if (plan.actividades != null) {
                    cabecera.push(campo(
                        "Cumplimiento",
                        `${plan.actividadesCompletadas ?? 0} de ${plan.actividades} actividades completadas`
                    ));
                }

                if (plan.observaciones) {
                    cabecera.push(campo("Observaciones del plan", plan.observaciones));
                }

                if (indicePlan === 0) cabecera.unshift(tituloSeccion("Planes de intervención"));

                contenidoPdf.push(bloque(cabecera));

                const estrategias = Array.isArray(plan.estrategias)
                    ? [...plan.estrategias]
                    : [];

                estrategias.sort((a, b) =>
                    (a.orden ?? Number.MAX_SAFE_INTEGER) -
                    (b.orden ?? Number.MAX_SAFE_INTEGER)
                );

                if (!estrategias.length) {
                    contenidoPdf.push(vacio("Sin estrategias registradas."));
                }

                estrategias.forEach((estrategia, indice) => {
                    const piezas = [
                        campo(`Estrategia ${indice + 1}`, estrategia.descripcion)
                    ];

                    const actividades = Array.isArray(estrategia.actividades)
                        ? estrategia.actividades
                        : [];

                    if (!actividades.length) {
                        piezas.push(vacio("Sin actividades registradas."));
                    }

                    actividades.forEach(item => {
                        const completada = item.completada === true || item.completada === 1;

                        piezas.push(...actividad(completada, item.nombre, item.descripcion));
                    });

                    contenidoPdf.push(bloque(piezas));
                });
            });
        }
        return {
            pageSize: "A4",
            pageMargins: [MARGEN.izquierda, MARGEN.superior, MARGEN.derecha, MARGEN.inferior],

            info: {
                title: tituloReporte,
                author: texto(reporte.generadoPor),
                creator: CONSULTORIO
            },

            defaultStyle: {
                font: "Roboto",
                fontSize: 10,
                color: COLOR.texto,
                lineHeight: 1.25
            },

            styles: {
                titulo: {
                    fontSize: 20,
                    bold: true,
                    color: COLOR.texto
                },
                seccion: {
                    fontSize: 13,
                    bold: true,
                    color: COLOR.texto
                },
                subtitulo: {
                    fontSize: 10.5,
                    bold: true
                }
            },

            background: fondoPagina("Reporte de seguimiento psicopedagógico", tituloReporte),

            content: contenidoPdf,

            footer: pie,

            pageBreakBefore: (nodo, contenedor) =>
                (nodo.style === "seccion" || nodo.style === "subtitulo") &&
                contenedor.getFollowingNodesOnPage().length === 0
        };
    }

    // Carga el logo una sola vez y permite esperar a que esté listo antes de exportar.
    window.ReportePdfLogoPromise = (async function cargarLogo() {
        try {
            const respuesta = await fetch("/img/LogoMenteActiva.jpeg");
            if (!respuesta.ok) return;

            const blob = await respuesta.blob();
            const url = await new Promise((resolver, rechazar) => {
                const lector = new FileReader();
                lector.onload = () => resolver(lector.result);
                lector.onerror = rechazar;
                lector.readAsDataURL(blob);
            });

            window.ReportePdfLogo = url;
        } catch {
            // El PDF puede generarse sin logo si la imagen no está disponible.
        }
    })();

    window.ReportePdf = { crearDefinicion };
})();
