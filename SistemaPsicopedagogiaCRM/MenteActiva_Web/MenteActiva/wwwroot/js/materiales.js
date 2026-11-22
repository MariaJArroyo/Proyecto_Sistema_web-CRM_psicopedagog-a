// Materiales educativos (M11): PDF, guias, tareas, actividades y enlaces.
//
// Todo pasa por /Admin/..., que reenvia al API: el navegador nunca habla con el
// API directamente. Cada aviso de exito sale solo cuando el API respondio bien;
// despues de guardar se vuelve a pedir la lista en vez de armar la fila a mano,
// asi lo que se ve es lo que quedo en la base.

(function () {
    "use strict";

    const RUTAS = {
        materiales: "/Admin/ConsultarMateriales",
        subirPdf: "/Admin/SubirPdfMaterial",
        descargarPdf: "/Admin/DescargarPdfMaterial",
        crearGuia: "/Admin/CrearGuiaMaterial",
        crearEnlace: "/Admin/CrearEnlaceMaterial",
        crearActividad: "/Admin/CrearActividadMaterial",
        editar: "/Admin/EditarMaterial",
        publicar: "/Admin/PublicarMaterial",
        eliminar: "/Admin/EliminarMaterial",
        verificarEnlace: "/Admin/VerificarEnlaceMaterial",
        tareas: "/Admin/ConsultarTareas",
        detalleTarea: "/Admin/DetalleTarea",
        crearTarea: "/Admin/CrearTarea",
        editarTarea: "/Admin/EditarTarea",
        eliminarTarea: "/Admin/EliminarTarea",
        entregasTarea: "/Admin/EntregasTarea",
        archivoEntrega: "/Admin/ArchivoEntregaTarea",
        revisarEntrega: "/Admin/RevisarEntregaTarea"
    };

    // Ids de TB_TIPO_MATERIAL
    const GUIA = 1;
    const PDF = 2;
    const ENLACE = 3;
    const ACTIVIDAD = 4;

    const VISTAS = ["Pdf", "Guias", "Tareas", "Actividades", "Enlaces"];

    const NOMBRE_TIPO = { [GUIA]: "Guía", [PDF]: "PDF", [ENLACE]: "Enlace", [ACTIVIDAD]: "Actividad" };

    const ETIQUETA_ACTIVIDAD = { Ludica: "Lúdica", Cognitiva: "Cognitiva", Motriz: "Motriz" };

    const niveles = window.MATERIALES_NIVELES || [];
    const estudiantes = window.MATERIALES_ESTUDIANTES || [];
    const tamanoMaximoMb = Number(window.MATERIALES_TAMANO_MAXIMO_MB) || 10;

    let materiales = [];
    let tareas = [];

    let materialEditando = null;   // el material abierto en el modal de edicion
    let idTareaEditando = null;    // null = el modal de tarea esta creando
    let tareaEntregas = null;      // la tarea abierta en el modal de entregas
    let entregas = [];             // sus estudiantes con la entrega de cada uno
    let entregaActual = null;      // la entrega que se esta revisando

    // Resultado de "Verificar" por id de enlace. Vive aparte de la lista para que
    // sobreviva a cada repintado.
    const verificaciones = new Map();

    // =====================================================
    // UTILIDADES
    // =====================================================

    function $(id) {
        return document.getElementById(id);
    }

    function escaparHtml(texto) {
        const d = document.createElement("div");
        d.textContent = texto ?? "";
        return d.innerHTML.replace(/"/g, "&quot;");
    }

    function notificar(mensaje, tipo) {
        if (typeof MostrarToast === "function") MostrarToast(mensaje, tipo);
    }

    function modal(id) {
        return bootstrap.Modal.getOrCreateInstance($(id));
    }

    function mostrarAlerta(elemento, mensaje) {
        if (!elemento) return;
        elemento.textContent = mensaje;
        elemento.classList.remove("d-none");
    }

    function ocultarAlerta(elemento) {
        if (!elemento) return;
        elemento.textContent = "";
        elemento.classList.add("d-none");
    }

    function marcar(campo, valido, mensaje) {
        campo.classList.toggle("is-invalid", !valido);

        const aviso = campo.parentElement.querySelector(".invalid-feedback");
        if (aviso) aviso.textContent = valido ? "" : (mensaje || "");

        return valido;
    }

    function limpiarFormulario(formulario) {
        formulario.reset();
        formulario.querySelectorAll(".is-invalid").forEach(c => c.classList.remove("is-invalid"));
    }

    function tokenAntifalsificacion() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    // Los arreglos se mandan como el mismo campo repetido, que es lo que el
    // controlador entiende como int[]. Lo nulo o vacio no se manda: el
    // controlador lo lee como "no vino".
    function datosConToken(campos) {
        const datos = new FormData();
        datos.append("__RequestVerificationToken", tokenAntifalsificacion());

        Object.entries(campos).forEach(([clave, valor]) => {
            if (valor === null || valor === undefined || valor === "") return;

            if (Array.isArray(valor)) {
                valor.forEach(v => datos.append(clave, v));
            } else {
                datos.append(clave, valor);
            }
        });

        return datos;
    }

    async function leerMensaje(respuesta, porDefecto) {
        try {
            const json = await respuesta.json();
            return json?.mensaje || porDefecto;
        } catch {
            return porDefecto;
        }
    }

    // POST con FormData. Devuelve { ok, datos } o { ok: false, mensaje }.
    async function enviar(url, datos) {
        try {
            const respuesta = await fetch(url, { method: "POST", body: datos });

            if (!respuesta.ok) {
                return { ok: false, mensaje: await leerMensaje(respuesta, "No se pudo completar la operación.") };
            }

            let cuerpo = {};
            try { cuerpo = await respuesta.json(); } catch { /* sin cuerpo */ }

            return { ok: true, datos: cuerpo };
        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }

    async function pedir(url) {
        try {
            const respuesta = await fetch(url);

            if (!respuesta.ok) {
                return { ok: false, mensaje: await leerMensaje(respuesta, "No se pudo completar la consulta.") };
            }

            return { ok: true, datos: await respuesta.json() };
        } catch {
            return { ok: false, mensaje: "No se pudo conectar con el servidor." };
        }
    }

    // Deshabilita el boton mientras corre la peticion para que un doble clic no
    // guarde dos veces
    async function conBoton(boton, textoOcupado, accion) {
        const textoOriginal = boton.innerHTML;
        boton.disabled = true;
        boton.textContent = textoOcupado;

        try {
            return await accion();
        } finally {
            boton.disabled = false;
            boton.innerHTML = textoOriginal;
        }
    }

    function recortar(texto, largo) {
        const limpio = String(texto ?? "").replace(/\s+/g, " ").trim();
        return limpio.length <= largo ? limpio : limpio.slice(0, largo - 1) + "…";
    }

    function formatoFecha(iso) {
        if (!iso) return "";
        const f = new Date(iso);
        if (isNaN(f)) return "";
        return f.toLocaleDateString("es-CR", { day: "numeric", month: "short", year: "numeric" });
    }

    // yyyy-MM-dd en hora local. toISOString da la fecha en UTC y despues de las
    // 6 pm en Costa Rica ya seria el dia siguiente.
    function fechaLocal(fecha) {
        const d = fecha || new Date();
        const mes = String(d.getMonth() + 1).padStart(2, "0");
        const dia = String(d.getDate()).padStart(2, "0");
        return `${d.getFullYear()}-${mes}-${dia}`;
    }

    function soloFecha(iso) {
        return iso ? String(iso).slice(0, 10) : "";
    }

    function esUrlWeb(texto) {
        try {
            const u = new URL(texto);
            return u.protocol === "http:" || u.protocol === "https:";
        } catch {
            return false;
        }
    }

    function porTipo(tipo) {
        return materiales.filter(m => m.idTipoMaterial === tipo);
    }

    // =====================================================
    // SELECTORES DE NIVEL
    // =====================================================

    function llenarNiveles() {
        document.querySelectorAll("[data-selector-nivel]").forEach(select => {
            const obligatorio = select.hasAttribute("data-obligatorio");

            select.innerHTML = "";

            const vacia = document.createElement("option");
            vacia.value = "";
            vacia.textContent = obligatorio ? "Seleccione" : "Sin nivel";
            select.appendChild(vacia);

            niveles.forEach(n => {
                const opcion = document.createElement("option");
                opcion.value = String(n.idNivelEducativo);
                opcion.textContent = n.nombre;
                select.appendChild(opcion);
            });
        });
    }

    // =====================================================
    // LISTADOS DE MATERIAL
    // =====================================================

    function insigniaPublicacion(m) {
        return m.publicado
            ? '<span class="badge badge-estado-activo">Publicado</span>'
            : '<span class="badge badge-estado-inactivo">Borrador</span>';
    }

    function botonIcono(accion, id, icono, etiqueta, clase) {
        return `<button type="button" class="btn btn-sm ${clase || "btn-outline-secondary"}"
                    data-accion="${accion}" data-id="${id}" title="${etiqueta}" aria-label="${etiqueta}">
                    <i class="bi ${icono}" aria-hidden="true"></i></button>`;
    }

    // Mismo orden que el resto de tablas: Editar y Desactivar a la vista; publicar,
    // descargar o verificar van en el menu de tres puntos (extras = opciones de
    // MenuAccionesFila)
    function botonesMaterial(m, extras) {
        const publicar = {
            texto: m.publicado ? "Despublicar" : "Publicar",
            icono: m.publicado ? "bi-eye-slash" : "bi-megaphone",
            atributos: { "data-accion": "publicar", "data-id": m.idMaterial }
        };

        return `<div class="d-flex align-items-center gap-1 flex-wrap justify-content-end TablaAcciones">
            <span class="me-1">${insigniaPublicacion(m)}</span>
            ${botonIcono("editar", m.idMaterial, "bi-pencil", "Editar", "btn-outline-secondary border-0")}
            ${botonIcono("eliminar", m.idMaterial, "bi-slash-circle", "Desactivar", "btn-outline-danger border-0")}
            ${MenuAccionesFila([...(extras || []), publicar], `Más acciones de ${m.titulo}`)}
        </div>`;
    }

    function lineaNivel(m, partes) {
        const lista = [];
        if (m.nivelEducativo) lista.push("Nivel: " + m.nivelEducativo);
        (partes || []).forEach(p => { if (p) lista.push(p); });
        return lista.map(escaparHtml).join(" · ");
    }

    // Sin icono: mensajes de error o de carga. Con icono: la lista esta vacia.
    function filaVacia(mensaje, icono, titulo) {
        return icono
            ? `<div class="list-group-item px-0 border-0">${EstadoVacioHtml(icono, titulo, mensaje, { compacto: true })}</div>`
            : `<div class="list-group-item px-0 TextoSuave">${escaparHtml(mensaje)}</div>`;
    }

    function filaMaterial(cuerpoIzquierdo, m, extraBotones) {
        return `<div class="list-group-item px-0 d-flex flex-wrap justify-content-between align-items-center gap-2">
            <div class="me-2">${cuerpoIzquierdo}</div>
            ${botonesMaterial(m, extraBotones)}
        </div>`;
    }

    function pintarPdf() {
        const lista = porTipo(PDF);

        $("ListaMaterialesPdf").innerHTML = lista.length === 0
            ? filaVacia("Suba el primero con el formulario de arriba.", "bi-file-earmark-pdf", "Todavía no hay PDF")
            : lista.map(m => {
                const descargar = m.tieneArchivo
                    ? [{ texto: "Descargar", icono: "bi-download", href: `${RUTAS.descargarPdf}?id=${m.idMaterial}` }]
                    : [];

                const detalle = lineaNivel(m, [
                    m.descripcion ? recortar(m.descripcion, 80) : "",
                    m.registradoPor ? "Subido por " + m.registradoPor : ""
                ]);

                return filaMaterial(
                    `<div class="fw-semibold"><i class="bi bi-file-earmark-pdf text-danger me-2" aria-hidden="true"></i>${escaparHtml(m.titulo)}</div>
                     <div class="TextoSuave small">${detalle}</div>`,
                    m, descargar);
            }).join("");
    }

    function pintarGuias() {
        const lista = porTipo(GUIA);

        $("ListaGuias").innerHTML = lista.length === 0
            ? filaVacia("Cree la primera con el botón Nueva guía.", "bi-book", "Todavía no hay guías")
            : lista.map(m => filaMaterial(
                `<div class="fw-semibold">${escaparHtml(m.titulo)}</div>
                 <div class="TextoSuave small">${lineaNivel(m, [m.descripcion ? recortar(m.descripcion, 80) : ""])}</div>`,
                m)).join("");
    }

    function pintarActividades() {
        const lista = porTipo(ACTIVIDAD);

        $("ListaActividadesEducativas").innerHTML = lista.length === 0
            ? filaVacia("Registre la primera con el formulario de arriba.", "bi-puzzle", "Todavía no hay actividades")
            : lista.map(m => {
                const tipo = ETIQUETA_ACTIVIDAD[m.tipoActividad] || m.tipoActividad || "";
                const insigniaTipo = tipo
                    ? `<span class="badge badge-estado-cita-agendada ms-2">${escaparHtml(tipo)}</span>`
                    : "";

                return filaMaterial(
                    `<div><span class="fw-semibold">${escaparHtml(m.titulo)}</span>${insigniaTipo}</div>
                     <div class="TextoSuave small">${lineaNivel(m, [
                        formatoFecha(m.fechaActividad),
                        m.descripcion ? recortar(m.descripcion, 80) : ""])}</div>`,
                    m);
            }).join("");
    }

    function insigniaEnlace(id) {
        const v = verificaciones.get(id);

        if (!v) {
            return '<span class="badge badge-estado-inactivo" title="Todavía no se ha comprobado">Sin verificar</span>';
        }

        return v.disponible
            ? '<span class="badge badge-estado-activo">Disponible</span>'
            : `<span class="badge badge-pago-vencido" title="${escaparHtml(v.mensaje)}">
                   <i class="bi bi-exclamation-triangle me-1" aria-hidden="true"></i>Enlace roto</span>`;
    }

    function pintarEnlaces() {
        const lista = porTipo(ENLACE);

        $("ListaEnlaces").innerHTML = lista.length === 0
            ? filaVacia("Agregue el primero con el formulario de arriba.", "bi-link-45deg", "Todavía no hay enlaces")
            : lista.map(m => {
                // Solo se arma el enlace si de verdad es http o https
                const nombre = esUrlWeb(m.url)
                    ? `<a href="${escaparHtml(m.url)}" target="_blank" rel="noopener noreferrer">${escaparHtml(m.titulo)}</a>`
                    : `<span>${escaparHtml(m.titulo)}</span>`;

                const verificar = [{
                    texto: "Verificar enlace", icono: "bi-link-45deg",
                    atributos: { "data-accion": "verificar", "data-id": m.idMaterial }
                }];

                return filaMaterial(
                    `<div class="fw-semibold">${nombre} ${insigniaEnlace(m.idMaterial)}</div>
                     <div class="TextoSuave small">${lineaNivel(m, [m.descripcion ? recortar(m.descripcion, 80) : ""])}</div>`,
                    m, verificar);
            }).join("");
    }

    function pintarMateriales() {
        pintarPdf();
        pintarGuias();
        pintarActividades();
        pintarEnlaces();
    }

    async function cargarMateriales() {
        const alerta = $("AlertaMateriales");
        const resultado = await pedir(RUTAS.materiales);

        if (!resultado.ok) {
            mostrarAlerta(alerta, resultado.mensaje);
            ["ListaMaterialesPdf", "ListaGuias", "ListaActividadesEducativas", "ListaEnlaces"]
                .forEach(id => { $(id).innerHTML = filaVacia("No se pudieron cargar los materiales."); });
            return false;
        }

        ocultarAlerta(alerta);
        materiales = resultado.datos;
        pintarMateriales();
        return true;
    }

    // =====================================================
    // ACCIONES SOBRE UN MATERIAL (publicar, eliminar, verificar, editar)
    // =====================================================

    async function alternarPublicacion(m) {
        const resultado = await enviar(
            RUTAS.publicar, datosConToken({ id: m.idMaterial, publicado: !m.publicado }));

        if (!resultado.ok) {
            notificar(resultado.mensaje, "error");
            return;
        }

        notificar(resultado.datos?.mensaje || "Publicación actualizada.", "exito");
        await cargarMateriales();
    }

    async function eliminarMaterial(m) {
        const acepto = await Confirmar({
            titulo: "Desactivar material",
            texto: `Se desactiva "${m.titulo}" y deja de verse en el portal. ¿Desea continuar?`,
            textoBoton: "Desactivar",
            tipo: "peligro"
        });

        if (!acepto) return;

        const resultado = await enviar(RUTAS.eliminar, datosConToken({ id: m.idMaterial }));

        if (!resultado.ok) {
            notificar(resultado.mensaje, "error");
            return;
        }

        verificaciones.delete(m.idMaterial);
        notificar(resultado.datos?.mensaje || "Material eliminado.", "exito");
        await cargarMateriales();
    }

    async function verificarEnlace(m, boton) {
        await conBoton(boton, "Verificando…", async () => {
            const resultado = await pedir(`${RUTAS.verificarEnlace}?id=${m.idMaterial}`);

            if (!resultado.ok) {
                notificar(resultado.mensaje, "error");
                return;
            }

            verificaciones.set(m.idMaterial, {
                disponible: Boolean(resultado.datos.disponible),
                mensaje: resultado.datos.mensaje || ""
            });

            if (!resultado.datos.disponible) {
                notificar(resultado.datos.mensaje || "El enlace no responde.", "error");
            }
        });

        pintarEnlaces();
    }

    function abrirEdicion(m) {
        materialEditando = m;

        const formulario = $("FormularioEditarMaterial");
        limpiarFormulario(formulario);
        ocultarAlerta($("AlertaEditarMaterial"));

        $("TituloModalEditarMaterial").textContent = "Editar " + (NOMBRE_TIPO[m.idTipoMaterial] || "material").toLowerCase();
        $("EditarTituloMaterial").value = m.titulo || "";
        $("EditarDescripcionMaterial").value = m.descripcion || "";
        $("EditarNivelMaterial").value = m.idNivelEducativo ? String(m.idNivelEducativo) : "";

        // El nivel solo es obligatorio en la guia
        $("EtiquetaEditarNivel").classList.toggle("CampoObligatorio", m.idTipoMaterial === GUIA);

        $("GrupoEditarUrl").classList.toggle("d-none", m.idTipoMaterial !== ENLACE);
        $("EditarUrlMaterial").value = m.url || "";

        $("GrupoEditarActividad").classList.toggle("d-none", m.idTipoMaterial !== ACTIVIDAD);
        $("EditarTipoActividad").value = m.tipoActividad || "";
        $("EditarFechaActividad").value = soloFecha(m.fechaActividad);

        modal("ModalEditarMaterial").show();
    }

    function validarEdicion() {
        const m = materialEditando;
        const titulo = $("EditarTituloMaterial");
        let valido = marcar(titulo, titulo.value.trim().length > 0, "Indique el título.");

        if (m.idTipoMaterial === GUIA) {
            const nivel = $("EditarNivelMaterial");
            valido = marcar(nivel, Boolean(nivel.value), "Seleccione el nivel educativo.") && valido;
        }

        if (m.idTipoMaterial === ENLACE) {
            const url = $("EditarUrlMaterial");
            valido = marcar(url, esUrlWeb(url.value.trim()),
                "Ingrese una dirección válida que empiece con http:// o https://.") && valido;
        }

        if (m.idTipoMaterial === ACTIVIDAD) {
            const tipo = $("EditarTipoActividad");
            const fecha = $("EditarFechaActividad");
            valido = marcar(tipo, Boolean(tipo.value), "Seleccione el tipo.") && valido;
            valido = marcar(fecha, Boolean(fecha.value), "Seleccione la fecha.") && valido;
        }

        return valido;
    }

    async function guardarEdicion(evento) {
        evento.preventDefault();

        const alerta = $("AlertaEditarMaterial");
        ocultarAlerta(alerta);

        if (!materialEditando || !validarEdicion()) return;

        const m = materialEditando;

        const resultado = await conBoton($("BotonGuardarMaterial"), "Guardando…", () => enviar(
            RUTAS.editar,
            datosConToken({
                id: m.idMaterial,
                titulo: $("EditarTituloMaterial").value.trim(),
                descripcion: $("EditarDescripcionMaterial").value.trim(),
                idNivelEducativo: $("EditarNivelMaterial").value,
                url: m.idTipoMaterial === ENLACE ? $("EditarUrlMaterial").value.trim() : null,
                tipoActividad: m.idTipoMaterial === ACTIVIDAD ? $("EditarTipoActividad").value : null,
                fechaActividad: m.idTipoMaterial === ACTIVIDAD ? $("EditarFechaActividad").value : null
            })));

        if (!resultado.ok) {
            mostrarAlerta(alerta, resultado.mensaje);
            return;
        }

        // Si cambio la direccion, lo que se verifico antes ya no vale
        verificaciones.delete(m.idMaterial);

        CerrarModal($("ModalEditarMaterial"));
        notificar(resultado.datos?.mensaje || "Material actualizado.", "exito");
        await cargarMateriales();
    }

    function alHacerClicEnLista(evento) {
        const boton = evento.target.closest("[data-accion]");
        if (!boton) return;

        const m = materiales.find(x => x.idMaterial === Number(boton.getAttribute("data-id")));
        if (!m) return;

        switch (boton.getAttribute("data-accion")) {
            case "editar": abrirEdicion(m); break;
            case "publicar": alternarPublicacion(m); break;
            case "eliminar": eliminarMaterial(m); break;
            case "verificar": verificarEnlace(m, boton); break;
        }
    }

    // =====================================================
    // ALTAS: PDF, GUIA, ACTIVIDAD, ENLACE
    // =====================================================

    // Antes de subir se comprueba lo que el API comprueba otra vez (extension,
    // tamanio y que el contenido empiece con %PDF), para avisar sin esperar la
    // subida. El API sigue siendo quien manda.
    async function validarArchivoPdf(campo) {
        const archivo = campo.files[0];

        if (!archivo) return marcar(campo, false, "Seleccione un archivo.");

        if (!archivo.name.toLowerCase().endsWith(".pdf")) {
            return marcar(campo, false, "Solo se permiten archivos en formato PDF.");
        }

        if (archivo.size === 0) return marcar(campo, false, "El archivo está vacío.");

        if (archivo.size > tamanoMaximoMb * 1024 * 1024) {
            return marcar(campo, false, `El archivo pasa de ${tamanoMaximoMb} MB.`);
        }

        const inicio = await archivo.slice(0, 4).text();

        if (inicio !== "%PDF") {
            return marcar(campo, false, "El archivo no es un PDF válido.");
        }

        return marcar(campo, true);
    }

    async function subirPdf(evento) {
        evento.preventDefault();

        const campo = $("ArchivoPdf");

        if (!await validarArchivoPdf(campo)) return;

        const datos = datosConToken({
            titulo: $("TituloPdf").value.trim(),
            idNivelEducativo: $("NivelPdf").value,
            publicar: $("PublicarPdf").checked
        });
        datos.append("archivo", campo.files[0]);

        const resultado = await conBoton($("BotonSubirPdf"), "Subiendo…", () => enviar(RUTAS.subirPdf, datos));

        if (!resultado.ok) {
            marcar(campo, false, resultado.mensaje);
            notificar(resultado.mensaje, "error");
            return;
        }

        limpiarFormulario($("FormularioSubirPdf"));
        notificar(resultado.datos?.mensaje || "Archivo cargado correctamente.", "exito");
        await cargarMateriales();
    }

    function abrirNuevaGuia() {
        limpiarFormulario($("FormularioNuevaGuia"));
        ocultarAlerta($("AlertaNuevaGuia"));
        modal("ModalNuevaGuia").show();
    }

    async function guardarGuia(evento) {
        evento.preventDefault();

        const alerta = $("AlertaNuevaGuia");
        ocultarAlerta(alerta);

        const titulo = $("TituloGuia");
        const nivel = $("NivelGuia");

        let valido = marcar(titulo, titulo.value.trim().length > 0, "Ingrese el título de la guía.");
        valido = marcar(nivel, Boolean(nivel.value), "Seleccione el nivel educativo.") && valido;

        if (!valido) return;

        const resultado = await conBoton($("BotonGuardarGuia"), "Guardando…", () => enviar(
            RUTAS.crearGuia,
            datosConToken({
                titulo: titulo.value.trim(),
                descripcion: $("DescripcionGuia").value.trim(),
                idNivelEducativo: nivel.value,
                publicar: $("PublicarGuia").checked
            })));

        if (!resultado.ok) {
            mostrarAlerta(alerta, resultado.mensaje);
            return;
        }

        CerrarModal($("ModalNuevaGuia"));
        notificar($("PublicarGuia").checked ? "Guía guardada y publicada." : "Guía guardada como borrador.", "exito");
        await cargarMateriales();
    }

    async function registrarActividad(evento) {
        evento.preventDefault();

        const nombre = $("NombreActividadEducativa");
        const tipo = $("TipoActividadEducativa");
        const fecha = $("FechaActividadEducativa");

        let valido = marcar(nombre, nombre.value.trim().length > 0, "Ingrese el nombre de la actividad.");
        valido = marcar(tipo, Boolean(tipo.value), "Seleccione el tipo.") && valido;
        valido = marcar(fecha, Boolean(fecha.value), "Seleccione la fecha.") && valido;

        if (!valido) return;

        const publicada = $("PublicarActividad").checked;

        const resultado = await conBoton($("BotonRegistrarActividad"), "Registrando…", () => enviar(
            RUTAS.crearActividad,
            datosConToken({
                titulo: nombre.value.trim(),
                descripcion: $("DescripcionActividad").value.trim(),
                tipoActividad: tipo.value,
                fechaActividad: fecha.value,
                idNivelEducativo: $("NivelActividad").value,
                publicar: publicada
            })));

        if (!resultado.ok) {
            notificar(resultado.mensaje, "error");
            marcar(nombre, false, resultado.mensaje);
            return;
        }

        limpiarFormulario($("FormularioActividad"));
        fecha.value = fechaLocal();
        notificar(publicada ? "Actividad registrada y publicada." : "Actividad registrada como borrador.", "exito");
        await cargarMateriales();
    }

    async function agregarEnlace(evento) {
        evento.preventDefault();

        const nombre = $("NombreEnlace");
        const url = $("UrlEnlace");

        let valido = marcar(nombre, nombre.value.trim().length > 0, "Ingrese el nombre del enlace.");
        valido = marcar(url, esUrlWeb(url.value.trim()),
            "Ingrese una dirección válida que empiece con http:// o https://.") && valido;

        if (!valido) return;

        const publicado = $("PublicarEnlace").checked;

        const resultado = await conBoton($("BotonAgregarEnlace"), "Agregando…", () => enviar(
            RUTAS.crearEnlace,
            datosConToken({
                titulo: nombre.value.trim(),
                url: url.value.trim(),
                descripcion: $("DescripcionEnlace").value.trim(),
                idNivelEducativo: $("NivelEnlace").value,
                publicar: publicado
            })));

        if (!resultado.ok) {
            notificar(resultado.mensaje, "error");
            marcar(url, false, resultado.mensaje);
            return;
        }

        limpiarFormulario($("FormularioNuevoEnlace"));
        notificar(publicado ? "Enlace agregado y publicado." : "Enlace agregado como borrador.", "exito");
        await cargarMateriales();
    }

    // =====================================================
    // TAREAS
    // =====================================================

    function pintarTareas() {
        const cuerpo = $("CuerpoTablaTareas");

        if (tareas.length === 0) {
            cuerpo.innerHTML = FilaVaciaTabla(4, "bi-journal-check", "Todavía no hay tareas",
                "Asigne la primera con el botón Nueva tarea.");
            return;
        }

        const hoy = fechaLocal();

        cuerpo.innerHTML = tareas.map(t => {
            const limite = soloFecha(t.fechaLimite);
            const vencida = limite && limite < hoy;
            const insigniaVencida = vencida
                ? ' <span class="badge badge-pago-vencido">Vencida</span>'
                : "";

            const material = t.material
                ? `<div class="TextoSuave small"><i class="bi bi-paperclip me-1" aria-hidden="true"></i>${escaparHtml(t.material)}</div>`
                : "";

            return `<tr>
                <td>
                    <div class="fw-semibold">${escaparHtml(t.titulo)}</div>
                    ${material}
                </td>
                <td class="text-nowrap">${escaparHtml(formatoFecha(t.fechaLimite))}${insigniaVencida}</td>
                <td class="text-nowrap">
                    <button type="button" class="btn btn-link btn-sm p-0 text-decoration-none"
                            data-accion="entregas-tarea" data-id="${t.idTarea}">${t.entregaron} de ${t.asignados}</button>
                    ${t.porRevisar > 0
                        ? `<span class="badge badge-pago-pendiente ms-1">${t.porRevisar} por revisar</span>`
                        : ""}
                </td>
                <td class="TablaAcciones text-end text-nowrap">
                    ${botonIcono("entregas-tarea", t.idTarea, "bi-eye", "Ver entregas", "btn-outline-secondary border-0")}
                    ${botonIcono("editar-tarea", t.idTarea, "bi-pencil", "Editar", "btn-outline-secondary border-0")}
                    ${botonIcono("eliminar-tarea", t.idTarea, "bi-slash-circle", "Desactivar", "btn-outline-danger border-0")}
                </td>
            </tr>`;
        }).join("");
    }

    async function cargarTareas() {
        const resultado = await pedir(RUTAS.tareas);

        if (!resultado.ok) {
            $("CuerpoTablaTareas").innerHTML =
                '<tr><td colspan="4" class="TextoSuave text-center py-4">No se pudieron cargar las tareas.</td></tr>';
            mostrarAlerta($("AlertaMateriales"), resultado.mensaje);
            return;
        }

        tareas = resultado.datos;
        pintarTareas();
    }

    function pintarEstudiantesTarea(marcados) {
        const contenedor = $("EstudiantesTarea");

        if (estudiantes.length === 0) {
            contenedor.innerHTML = '<div class="TextoSuave small">No hay estudiantes activos.</div>';
            return;
        }

        contenedor.innerHTML = estudiantes.map(e => `
            <div class="form-check">
                <input class="form-check-input" type="checkbox" id="EstudianteTarea${e.idEstudiante}"
                       value="${e.idEstudiante}"${marcados.has(e.idEstudiante) ? " checked" : ""}>
                <label class="form-check-label" for="EstudianteTarea${e.idEstudiante}">${escaparHtml(e.estudiante)}</label>
            </div>`).join("");
    }

    function estudiantesMarcados() {
        return [...$("EstudiantesTarea").querySelectorAll("input:checked")].map(c => Number(c.value));
    }

    // Todas las guias, PDF, actividades y enlaces activos: cualquiera puede ser el
    // material de apoyo de una tarea
    function llenarMaterialesTarea(idSeleccionado) {
        const select = $("MaterialTarea");

        select.innerHTML = '<option value="">Sin material</option>';

        materiales.forEach(m => {
            const opcion = document.createElement("option");
            opcion.value = String(m.idMaterial);
            opcion.textContent = `${m.titulo} (${NOMBRE_TIPO[m.idTipoMaterial] || "Material"})`;
            select.appendChild(opcion);
        });

        select.value = idSeleccionado ? String(idSeleccionado) : "";
    }

    function prepararModalTarea(titulo, botonTexto) {
        limpiarFormulario($("FormularioTarea"));
        ocultarAlerta($("AlertaTarea"));
        $("ErrorEstudiantesTarea").classList.add("d-none");
        $("TituloModalTarea").textContent = titulo;
        $("BotonGuardarTarea").textContent = botonTexto;
    }

    function abrirNuevaTarea() {
        idTareaEditando = null;
        prepararModalTarea("Nueva tarea", "Asignar tarea");

        $("FechaLimiteTarea").min = fechaLocal();
        llenarMaterialesTarea(null);
        pintarEstudiantesTarea(new Set());

        modal("ModalTarea").show();
    }

    async function abrirEdicionTarea(id) {
        const resultado = await pedir(`${RUTAS.detalleTarea}?id=${id}`);

        if (!resultado.ok) {
            notificar(resultado.mensaje, "error");
            return;
        }

        const t = resultado.datos;

        idTareaEditando = id;
        prepararModalTarea("Editar tarea", "Guardar cambios");

        // Sin minimo: una tarea ya vencida se puede seguir editando con su fecha
        $("FechaLimiteTarea").removeAttribute("min");
        $("TituloTarea").value = t.titulo || "";
        $("DescripcionTarea").value = t.descripcion || "";
        $("FechaLimiteTarea").value = soloFecha(t.fechaLimite);

        llenarMaterialesTarea(t.idMaterial);
        pintarEstudiantesTarea(new Set((t.estudiantes || []).map(e => e.idEstudiante)));

        modal("ModalTarea").show();
    }

    function validarTarea() {
        const titulo = $("TituloTarea");
        const fecha = $("FechaLimiteTarea");
        const aviso = $("ErrorEstudiantesTarea");

        let valido = marcar(titulo, titulo.value.trim().length > 0, "Ingrese el título de la tarea.");

        const fechaValida = Boolean(fecha.value);
        const enElPasado = idTareaEditando === null && fechaValida && fecha.value < fechaLocal();

        valido = marcar(fecha, fechaValida && !enElPasado,
            fechaValida ? "La fecha límite no puede estar en el pasado." : "Seleccione la fecha límite.") && valido;

        // Al editar, dejar la lista vacia no quita a nadie (el API la toma como
        // "no tocar"), asi que se pide al menos uno para no prometer eso. Para
        // sacar a todos se elimina la tarea.
        if (estudiantesMarcados().length === 0) {
            aviso.textContent = "Seleccione al menos un estudiante.";
            aviso.classList.remove("d-none");
            valido = false;
        } else {
            aviso.classList.add("d-none");
        }

        return valido;
    }

    async function guardarTarea(evento) {
        evento.preventDefault();

        const alerta = $("AlertaTarea");
        ocultarAlerta(alerta);

        if (!validarTarea()) return;

        const campos = {
            titulo: $("TituloTarea").value.trim(),
            descripcion: $("DescripcionTarea").value.trim(),
            fechaLimite: $("FechaLimiteTarea").value,
            idMaterial: $("MaterialTarea").value,
            idEstudiantes: estudiantesMarcados()
        };

        const editando = idTareaEditando !== null;

        if (editando) campos.id = idTareaEditando;

        const resultado = await conBoton($("BotonGuardarTarea"), "Guardando…", () => enviar(
            editando ? RUTAS.editarTarea : RUTAS.crearTarea, datosConToken(campos)));

        if (!resultado.ok) {
            mostrarAlerta(alerta, resultado.mensaje);
            return;
        }

        CerrarModal($("ModalTarea"));
        notificar(editando ? "Tarea actualizada correctamente." : "Tarea asignada correctamente.", "exito");
        await cargarTareas();
    }

    async function eliminarTarea(id) {
        const t = tareas.find(x => x.idTarea === id);
        if (!t) return;

        const acepto = await Confirmar({
            titulo: "Desactivar tarea",
            texto: `Se desactiva "${t.titulo}" y deja de verse para los encargados. Las entregas quedan guardadas. ¿Desea continuar?`,
            textoBoton: "Desactivar",
            tipo: "peligro"
        });

        if (!acepto) return;

        const resultado = await enviar(RUTAS.eliminarTarea, datosConToken({ id }));

        if (!resultado.ok) {
            notificar(resultado.mensaje, "error");
            return;
        }

        notificar(resultado.datos?.mensaje || "Tarea eliminada.", "exito");
        await cargarTareas();
    }

    // =====================================================
    // ENTREGAS DE UNA TAREA
    // =====================================================

    // Ids de TB_ESTADO_TAREA
    const ESTADO_ENTREGA = {
        1: { texto: "Sin entregar", clase: "EstadoBadge-Neutro" },
        2: { texto: "Por revisar", clase: "EstadoBadge-Pendiente" },
        3: { texto: "Revisada", clase: "EstadoBadge-Completada" },
        4: { texto: "Vencida", clase: "EstadoBadge-Cancelada" },
        5: { texto: "Por corregir", clase: "EstadoBadge-Programada" }
    };

    function formatoFechaHora(iso) {
        if (!iso) return "";
        const f = new Date(iso);
        if (isNaN(f)) return "";
        return f.toLocaleString("es-CR", {
            day: "numeric", month: "short", year: "numeric", hour: "numeric", minute: "2-digit"
        });
    }

    function insigniaEntrega(e) {
        // Sin entregar y con la fecha limite ya pasada: atrasada
        const atrasada = (e.idEstadoTarea === 1 || e.idEstadoTarea === 5)
            && tareaEntregas && soloFecha(tareaEntregas.fechaLimite) < fechaLocal();
        const estado = atrasada && e.idEstadoTarea === 1
            ? { texto: "Atrasada", clase: "EstadoBadge-Cancelada" }
            : (ESTADO_ENTREGA[e.idEstadoTarea] || { texto: e.estado, clase: "EstadoBadge-Neutro" });

        return `<span class="EstadoBadge ${estado.clase}">${escaparHtml(estado.texto)}</span>`;
    }

    function mostrarListaEntregas() {
        entregaActual = null;
        $("VistaRevisarEntrega").classList.add("d-none");
        $("VistaListaEntregas").classList.remove("d-none");
        ocultarAlerta($("AlertaEntregasTarea"));
    }

    function pintarEntregas() {
        const cuerpo = $("CuerpoEntregasTarea");
        const cuenta = estado => entregas.filter(e => e.idEstadoTarea === estado).length;

        const chips = [
            [cuenta(2), "por revisar", "badge-pago-pendiente"],
            [cuenta(3), "revisadas", "badge-pago-pagado"],
            [cuenta(5), "por corregir", "badge-estado-seguimiento"],
            [cuenta(1) + cuenta(4), "sin entregar", "badge-estado-inactivo"]
        ];

        $("ResumenEntregas").innerHTML = chips
            .map(([n, texto, clase]) => `<span class="badge ${clase}">${n} ${texto}</span>`)
            .join("");

        if (entregas.length === 0) {
            cuerpo.innerHTML = FilaVaciaTabla(5, "bi-inbox", "Sin estudiantes asignados",
                "Edite la tarea para asignarla a uno o más estudiantes.");
            return;
        }

        cuerpo.innerHTML = entregas.map(e => {
            const entregada = e.fechaEntrega
                ? `${escaparHtml(formatoFechaHora(e.fechaEntrega))}${e.entregaTardia ? ' <span class="badge badge-pago-vencido">Tarde</span>' : ""}`
                : '<span class="TextoSuave">—</span>';

            const comentario = e.comentarioEstudiante
                ? `<span title="${escaparHtml(e.comentarioEstudiante)}">${escaparHtml(recortar(e.comentarioEstudiante, 60))}</span>`
                : '<span class="TextoSuave">—</span>';

            const adjunto = e.tieneArchivo
                ? ' <i class="bi bi-paperclip TextoSuave" title="Tiene archivo adjunto" aria-label="Tiene archivo adjunto"></i>'
                : "";

            // Lo que ya se entrego alguna vez se puede abrir: para revisar o
            // para consultar lo que se reviso
            const accion = e.idEstadoTarea === 2
                ? `<button type="button" class="btn btn-sm btn-primary" data-revisar-entrega="${e.idTareaAsignacion}">Revisar</button>`
                : e.fechaEntrega
                    ? `<button type="button" class="btn btn-sm btn-outline-secondary" data-revisar-entrega="${e.idTareaAsignacion}">Ver</button>`
                    : "";

            return `<tr>
                <td class="fw-semibold">${escaparHtml(e.estudiante)}${adjunto}</td>
                <td>${insigniaEntrega(e)}</td>
                <td class="text-nowrap">${entregada}</td>
                <td class="small">${comentario}</td>
                <td class="text-end">${accion}</td>
            </tr>`;
        }).join("");
    }

    async function cargarEntregas() {
        const resultado = await pedir(`${RUTAS.entregasTarea}?id=${tareaEntregas.idTarea}`);

        if (!resultado.ok) {
            $("CuerpoEntregasTarea").innerHTML =
                '<tr><td colspan="5" class="TextoSuave text-center py-4">No se pudieron cargar las entregas.</td></tr>';
            mostrarAlerta($("AlertaEntregasTarea"), resultado.mensaje);
            return false;
        }

        entregas = resultado.datos || [];
        pintarEntregas();
        return true;
    }

    async function abrirEntregas(idTarea) {
        tareaEntregas = tareas.find(t => t.idTarea === idTarea) || null;
        if (!tareaEntregas) return;

        entregas = [];
        mostrarListaEntregas();
        $("SubtituloEntregasTarea").textContent =
            `${tareaEntregas.titulo} · fecha límite ${formatoFecha(tareaEntregas.fechaLimite)}`;
        $("ResumenEntregas").innerHTML = "";
        $("CuerpoEntregasTarea").innerHTML =
            '<tr><td colspan="5" class="TextoSuave text-center py-4">Cargando…</td></tr>';

        modal("ModalEntregasTarea").show();
        await cargarEntregas();
    }

    function pintarVisorEntrega(e) {
        const visor = $("VisorEntregaTarea");

        if (!e.tieneArchivo) {
            visor.innerHTML = `
                <div class="VisorComprobante-Vacio">
                    <i class="bi bi-journal-check" aria-hidden="true"></i>
                    <p class="mb-0">La familia marcó la tarea como entregada sin adjuntar archivo.</p>
                </div>`;
            return;
        }

        // El id cambia en cada entrega: con la fecha en la URL el navegador no
        // muestra una foto vieja guardada en cache
        const url = `${RUTAS.archivoEntrega}?id=${e.idTareaAsignacion}&v=${encodeURIComponent(e.fechaEntrega || "")}`;
        const abrir = `<a href="${url}" target="_blank" rel="noopener" class="btn btn-sm btn-outline-secondary mt-2">
                    <i class="bi bi-box-arrow-up-right me-1" aria-hidden="true"></i>Abrir en otra pestaña</a>`;

        visor.innerHTML = e.tipoArchivo === "application/pdf"
            ? `<iframe src="${url}" title="Tarea en PDF" class="VisorComprobante-Pdf"></iframe>${abrir}`
            : `<a href="${url}" target="_blank" rel="noopener" class="VisorComprobante-Imagen">
                   <img src="${url}" alt="Foto de la tarea de ${escaparHtml(e.estudiante)}">
               </a>${abrir}`;
    }

    function abrirRevision(idTareaAsignacion) {
        const e = entregas.find(x => x.idTareaAsignacion === idTareaAsignacion);
        if (!e) return;

        entregaActual = e;
        ocultarAlerta($("AlertaEntregasTarea"));
        pintarVisorEntrega(e);

        const fila = (titulo, valor) => valor ? `<dt>${titulo}</dt><dd>${valor}</dd>` : "";

        $("DatosEntregaTarea").innerHTML =
            fila("Estudiante", escaparHtml(e.estudiante))
            + fila("Estado", insigniaEntrega(e))
            + fila("Entregada", `${escaparHtml(formatoFechaHora(e.fechaEntrega))}${e.entregaTardia ? ' <span class="badge badge-pago-vencido">Tarde</span>' : ""}`)
            + fila("Comentario de la familia", e.comentarioEstudiante ? escaparHtml(e.comentarioEstudiante) : "")
            + fila("Archivo", e.nombreArchivo ? escaparHtml(e.nombreArchivo) : "")
            + fila("Su comentario", e.comentarioRevision ? escaparHtml(e.comentarioRevision) : "");

        const pendiente = e.idEstadoTarea === 2;
        const comentario = $("ComentarioRevisionEntrega");

        comentario.value = "";
        comentario.classList.remove("is-invalid");
        $("AccionesRevisarEntrega").classList.toggle("d-none", !pendiente);

        const nota = $("NotaEntregaRevisada");
        nota.classList.toggle("d-none", pendiente);
        nota.textContent = pendiente ? "" : (e.fechaRevision
            ? `Revisada el ${formatoFechaHora(e.fechaRevision)}${e.revisadoPor ? ` por ${e.revisadoPor}` : ""}.`
            : "");

        $("VistaListaEntregas").classList.add("d-none");
        $("VistaRevisarEntrega").classList.remove("d-none");
    }

    async function revisarEntrega(aprobada) {
        if (!entregaActual) return;

        const comentario = $("ComentarioRevisionEntrega");
        const texto = comentario.value.trim();

        if (!aprobada && !marcar(comentario, texto.length > 0, "Para devolverla, escriba qué debe corregir.")) {
            comentario.focus();
            return;
        }

        comentario.classList.remove("is-invalid");

        const boton = aprobada ? $("BotonAprobarEntrega") : $("BotonDevolverEntrega");
        const resultado = await conBoton(boton, "Guardando…", () => enviar(RUTAS.revisarEntrega, datosConToken({
            id: entregaActual.idTareaAsignacion,
            aprobada: aprobada ? "true" : "false",
            comentario: texto
        })));

        if (!resultado.ok) {
            mostrarAlerta($("AlertaEntregasTarea"), resultado.mensaje);
            return;
        }

        notificar(resultado.datos?.mensaje || "Revisión guardada.", "exito");
        mostrarListaEntregas();

        // La lista del modal y el contador de la tabla de tareas
        await Promise.all([cargarEntregas(), cargarTareas()]);
        tareaEntregas = tareas.find(t => t.idTarea === tareaEntregas?.idTarea) || tareaEntregas;
    }

    // =====================================================
    // ARRANQUE
    // =====================================================

    function inicializarPestanias() {
        const selector = $("SelectorMateriales");

        selector.querySelectorAll("[data-vista]").forEach(boton => {
            boton.addEventListener("click", function () {
                selector.querySelectorAll(".nav-link").forEach(b => b.classList.remove("active"));
                boton.classList.add("active");

                const elegida = boton.getAttribute("data-vista");

                VISTAS.forEach(nombre => $("Vista" + nombre).classList.toggle("d-none", nombre !== elegida));
            });
        });
    }

    document.addEventListener("DOMContentLoaded", function () {
        inicializarPestanias();
        llenarNiveles();

        $("FechaActividadEducativa").value = fechaLocal();

        $("FormularioSubirPdf").addEventListener("submit", subirPdf);
        $("BotonNuevaGuia").addEventListener("click", abrirNuevaGuia);
        $("FormularioNuevaGuia").addEventListener("submit", guardarGuia);
        $("FormularioEditarMaterial").addEventListener("submit", guardarEdicion);
        $("FormularioActividad").addEventListener("submit", registrarActividad);
        $("FormularioNuevoEnlace").addEventListener("submit", agregarEnlace);

        ["ListaMaterialesPdf", "ListaGuias", "ListaActividadesEducativas", "ListaEnlaces"]
            .forEach(id => $(id).addEventListener("click", alHacerClicEnLista));

        $("BotonNuevaTarea").addEventListener("click", abrirNuevaTarea);
        $("FormularioTarea").addEventListener("submit", guardarTarea);

        $("BotonTodosEstudiantesTarea").addEventListener("click", function () {
            const casillas = $("EstudiantesTarea").querySelectorAll("input[type=checkbox]");
            const todas = [...casillas].every(c => c.checked);
            casillas.forEach(c => { c.checked = !todas; });
        });

        $("CuerpoTablaTareas").addEventListener("click", function (evento) {
            const boton = evento.target.closest("[data-accion]");
            if (!boton) return;

            const id = Number(boton.getAttribute("data-id"));

            switch (boton.getAttribute("data-accion")) {
                case "entregas-tarea": abrirEntregas(id); break;
                case "editar-tarea": abrirEdicionTarea(id); break;
                case "eliminar-tarea": eliminarTarea(id); break;
            }
        });

        $("CuerpoEntregasTarea").addEventListener("click", function (evento) {
            const boton = evento.target.closest("[data-revisar-entrega]");
            if (boton) abrirRevision(Number(boton.getAttribute("data-revisar-entrega")));
        });

        $("BotonVolverEntregas").addEventListener("click", mostrarListaEntregas);
        $("BotonAprobarEntrega").addEventListener("click", () => revisarEntrega(true));
        $("BotonDevolverEntrega").addEventListener("click", () => revisarEntrega(false));

        // Las tareas necesitan la lista de materiales para el selector, asi que
        // se piden juntas pero sin esperarse entre si
        cargarMateriales();
        cargarTareas();
    });
})();
