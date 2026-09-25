-- ============================================================
-- Mente Activa CRM - 03 Datos de prueba
-- Correr despues de 01_esquema.sql y 02_catalogos.sql
--
-- Datos ficticios para probar relaciones y restricciones. Nombres
-- inventados. NUNCA correr en produccion. Se puede omitir: el
-- sistema funciona solo con 01 y 02.
--
-- Se puede correr varias veces sin duplicar: Ids fijos y
-- ON DUPLICATE KEY UPDATE sin cambios. Los usuarios de la seccion
-- "Usuarios base" traen contrasena real y sirven para entrar.
-- ============================================================

SET NAMES utf8mb4;
USE mente_activa;

-- Encargados: 1 y 2 son los padres de Lucia; 3 y 4 no tienen cedula,
-- para probar que el indice unico acepta varios NULL
INSERT INTO TB_ENCARGADO (IdEncargado, IdEstadoCliente, IdServicioInteres, Nombre, PrimerApellido, SegundoApellido, Identificacion, Correo) VALUES
  (1, 4, 2, 'Andrea', 'Solís', 'Mora', '100000001', 'andrea.prueba@example.com'),
  (2, 4, 1, 'Marco', 'Vindas', 'Rojas', '100000002', 'marco.prueba@example.com'),
  (3, 1, 3, 'Paula', 'Jiménez', 'Arce', NULL, 'paula.prueba@example.com'),
  (4, 6, 4, 'Diego', 'Campos', NULL, NULL, NULL)
ON DUPLICATE KEY UPDATE IdEncargado = IdEncargado;

-- ------------------------------------------------------------
-- Usuarios base para desarrollo
--
-- El hash lo genera PasswordHasher de ASP.NET Core (PBKDF2-HMAC-SHA256);
-- el salt va dentro del mismo texto, por eso ContrasenaSalt queda en NULL.
--
-- Panel administrativo:
--   admin.prueba@example.com          Admin123*    Administrador
--   psicopedagoga.prueba@example.com  Psico123*    Psicopedagoga
--
-- Portal de encargados (lado del cliente):
--   andrea.prueba@example.com         Andrea123*   un estudiante (Lucia)
--   marco.prueba@example.com          Marco123*    dos estudiantes, sale el selector
--   paula.prueba@example.com          --           sin contrasena todavia
--
-- Paula queda en "Pendiente de activacion" a proposito: es la cuenta para
-- probar la invitacion y la pantalla de definir contrasena. Todavia no tiene
-- contrasena, asi que el login la rechaza con el mensaje generico. Para
-- activarla se usa "Olvido su contrasena" desde el login; mientras el API
-- corra con Correo:Proveedor = Registro, el enlace sale en la consola del API
-- en vez de irse por correo, y no hace falta buzon.
--
-- Claves de desarrollo, no se usan en despliegue.
--
-- A diferencia del resto del script, aqui si se pisa lo que haya: al
-- reejecutar, las cinco cuentas vuelven a su contrasena y su estado de
-- origen, y se limpian los intentos fallidos. Sirve para destrabar una
-- cuenta que quedo bloqueada probando.
-- ------------------------------------------------------------
INSERT INTO TB_USUARIO (IdUsuario, IdEncargado, IdEstadoUsuario, NombreCompleto, Correo, ContrasenaHash) VALUES
  (1, NULL, 1, 'Psicopedagoga de prueba', 'psicopedagoga.prueba@example.com', 'AQAAAAIAAYagAAAAEB+yFZIUfmZakqDCJ91P8RvZxP5uLRg4oCyy+RjdOb3YzcJnaxTOP0z+ywOFU3jKdg=='),
  (2, 1, 1, 'Andrea Solís Mora', 'andrea.prueba@example.com', 'AQAAAAIAAYagAAAAEBK2xFtBZVB7DKXjkoZtexwFA5OYmr17Jvnxs3qvCiOat32XoHHhkA79OLoBn/VtTw=='),
  (3, NULL, 1, 'Administrador de prueba', 'admin.prueba@example.com', 'AQAAAAIAAYagAAAAEGu2RmE/YINq5W2e3sQJwgBf8Cum1alAcSV3Zmfz2GAsLSgLvvLy/hOZDtu/h2wRRA=='),
  (4, 2, 1, 'Marco Vindas Rojas', 'marco.prueba@example.com', 'AQAAAAIAAYagAAAAEE0Ogn3JgaeZ+AV+YVRcTeYrFAs6ni4y/ppcmuhP+5BR+/IqLqdXFIj2/nz6SHgZgg=='),
  (5, 3, 4, 'Paula Jiménez Arce', 'paula.prueba@example.com', 'PENDIENTE_ACTIVACION') AS nuevo
ON DUPLICATE KEY UPDATE
  ContrasenaHash = nuevo.ContrasenaHash,
  IdEstadoUsuario = nuevo.IdEstadoUsuario,
  IntentosFallidos = 0;

-- Un rol por usuario: asi lo aplica SP_Usuario_AsignarRol
INSERT INTO TB_USUARIO_ROL (IdUsuarioRol, IdUsuario, IdRol) VALUES
  (1, 1, 2),
  (2, 2, 4),
  (3, 3, 1),
  (4, 4, 4),
  (5, 5, 4)
ON DUPLICATE KEY UPDATE IdUsuarioRol = IdUsuarioRol;

-- Andrea con dos telefonos, uno principal
INSERT INTO TB_TELEFONO (IdTelefono, IdEncargado, IdTipoTelefono, Numero, EsPrincipal) VALUES
  (1, 1, 1, '88880001', 1),
  (2, 1, 2, '22220001', 0),
  (3, 2, 1, '88880002', 1),
  (4, 3, 1, '88880003', 1)
ON DUPLICATE KEY UPDATE IdTelefono = IdTelefono;

INSERT INTO TB_ESTUDIANTE (IdEstudiante, IdNivelEducativo, IdInstitucion, Nombre, PrimerApellido, SegundoApellido, FechaNacimiento, NecesidadesApoyo) VALUES
  (1, 3, 1, 'Lucía', 'Vindas', 'Solís', '2016-04-12', 'Apoyo en lectura comprensiva.'),
  (2, 2, 2, 'Tomás', 'Vindas', 'Campos', '2018-09-03', 'Refuerzo en operaciones básicas.')
ON DUPLICATE KEY UPDATE IdEstudiante = IdEstudiante;

-- Lucia tiene dos encargados (Andrea principal) y Marco tiene dos estudiantes.
-- Paula queda de tutora legal de Tomas para que su portal tenga contenido
-- apenas active la cuenta.
INSERT INTO TB_ESTUDIANTE_ENCARGADO (IdEstudianteEncargado, IdEstudiante, IdEncargado, IdParentesco, EsPrincipal) VALUES
  (1, 1, 1, 1, 1),
  (2, 1, 2, 2, 0),
  (3, 2, 2, 2, 1),
  (4, 2, 3, 3, 0)
ON DUPLICATE KEY UPDATE IdEstudianteEncargado = IdEstudianteEncargado;

INSERT INTO TB_ESTUDIANTE_AREA (IdEstudianteArea, IdEstudiante, IdAreaDificultad, Observacion) VALUES
  (1, 1, 1, 'Confunde letras similares.'),
  (2, 1, 4, NULL),
  (3, 2, 2, 'Dificultad con la resta llevando.')
ON DUPLICATE KEY UPDATE IdEstudianteArea = IdEstudianteArea;

INSERT INTO TB_DIA_NO_LABORAL (IdDiaNoLaboral, Fecha, Motivo, EsFeriado) VALUES
  (1, '2026-12-25', 'Navidad', 1)
ON DUPLICATE KEY UPDATE IdDiaNoLaboral = IdDiaNoLaboral;

-- Las citas 1 y 2 se traslapan y las dos estan vigentes: la consulta de
-- conflicto debe devolver ese par. La 4 tambien choca con la 1 pero esta
-- cancelada, asi que no debe aparecer.
INSERT INTO TB_CITA (IdCita, IdEstudiante, IdTipoSesion, IdModalidad, IdEstadoCita, IdUsuarioRegistro, FechaHoraInicio, FechaHoraFin, Observaciones, MotivoCancelacion) VALUES
  (1, 1, 1, 1, 1, 1, '2026-10-05 09:00:00', '2026-10-05 10:00:00', NULL, NULL),
  (2, 2, 1, 1, 2, 1, '2026-10-05 09:30:00', '2026-10-05 10:30:00', NULL, NULL),
  (3, 1, 2, 1, 3, 1, '2026-09-07 15:00:00', '2026-09-07 16:00:00', NULL, NULL),
  (4, 2, 1, 2, 4, 1, '2026-10-05 09:45:00', '2026-10-05 10:15:00', NULL, 'La familia pidió reprogramar.'),
  (5, 1, 1, 1, 5, 1, '2026-09-10 14:00:00', '2026-09-10 15:00:00', NULL, NULL)
ON DUPLICATE KEY UPDATE IdCita = IdCita;

-- Sesion 1 nace de la cita 3; la 2 se registro sin cita
INSERT INTO TB_SESION (IdSesion, IdEstudiante, IdCita, IdTipoAtencion, IdUsuarioRegistro, Fecha, TemaTrabajado, Avances, Recomendaciones) VALUES
  (1, 1, 3, 1, 1, '2026-09-07', 'Lectura de textos cortos', 'Mejor fluidez.', 'Leer 15 minutos diarios.'),
  (2, 2, NULL, 1, 1, '2026-09-09', 'Resta con reagrupación', 'Resuelve con material concreto.', 'Practicar con monedas.')
ON DUPLICATE KEY UPDATE IdSesion = IdSesion;

INSERT INTO TB_NOTIFICACION (IdNotificacion, IdCita, IdUsuarioDestinatario, IdCanalNotificacion, Asunto, Cuerpo, FechaProgramada) VALUES
  (1, 1, 2, 1, 'Recordatorio de cita', 'Le recordamos la cita de Lucía el 5 de octubre a las 9:00.', '2026-10-04 09:00:00')
ON DUPLICATE KEY UPDATE IdNotificacion = IdNotificacion;

INSERT INTO TB_PLAN_INTERVENCION (IdPlan, IdEstudiante, IdEstadoPlan, IdUsuarioRegistro, Titulo, ObjetivoGeneral, FechaInicio, FechaFin) VALUES
  (1, 1, 2, 1, 'Plan de lectura comprensiva', 'Mejorar la comprensión de textos narrativos.', '2026-09-01', '2026-12-15')
ON DUPLICATE KEY UPDATE IdPlan = IdPlan;

INSERT INTO TB_ESTRATEGIA (IdEstrategia, IdPlan, Descripcion, Orden) VALUES
  (1, 1, 'Lectura guiada con preguntas', 1),
  (2, 1, 'Organizadores gráficos', 2)
ON DUPLICATE KEY UPDATE IdEstrategia = IdEstrategia;

INSERT INTO TB_ACTIVIDAD (IdActividad, IdEstrategia, Nombre, Completada) VALUES
  (1, 1, 'Leer un cuento y responder cinco preguntas', 1),
  (2, 1, 'Resumir el cuento en tres oraciones', 0),
  (3, 2, 'Completar un mapa de personajes', 0)
ON DUPLICATE KEY UPDATE IdActividad = IdActividad;

INSERT INTO TB_REPORTE (IdReporte, IdEstudiante, IdUsuarioGenero, Titulo, PeriodoInicio, PeriodoFin, Contenido, VisibleEnPortal) VALUES
  (1, 1, 1, 'Reporte de progreso de septiembre', '2026-09-01', '2026-09-30', 'Avance sostenido en lectura.', 1)
ON DUPLICATE KEY UPDATE IdReporte = IdReporte;

-- Pago 1: pagado en dos abonos de encargados distintos
-- Pago 2: parcial. Pago 3: pendiente. Pago 4: vencido sin abonos
INSERT INTO TB_PAGO (IdPago, IdEstudiante, IdEstadoPago, IdUsuarioRegistro, Concepto, Monto, FechaEmision, FechaVencimiento) VALUES
  (1, 1, 1, 1, 'Mensualidad septiembre', 60000.00, '2026-09-01', '2026-09-10'),
  (2, 2, 4, 1, 'Mensualidad septiembre', 40000.00, '2026-09-01', '2026-09-10'),
  (3, 1, 2, 1, 'Mensualidad octubre', 60000.00, '2026-10-01', '2026-10-10'),
  (4, 2, 3, 1, 'Mensualidad agosto', 40000.00, '2026-08-01', '2026-08-10')
ON DUPLICATE KEY UPDATE IdPago = IdPago;

INSERT INTO TB_ABONO (IdAbono, IdPago, IdEncargadoPagador, IdMetodoPago, IdUsuarioRegistro, Monto, FechaAbono, Referencia) VALUES
  (1, 1, 1, 2, 1, 30000.00, '2026-09-03', 'SINPE-0001'),
  (2, 1, 2, 1, 1, 30000.00, '2026-09-05', NULL),
  (3, 2, 2, 3, 1, 15000.00, '2026-09-06', 'TRF-0001')
ON DUPLICATE KEY UPDATE IdAbono = IdAbono;

INSERT INTO TB_MATERIAL (IdMaterial, IdTipoMaterial, IdUsuarioRegistro, Titulo, Descripcion, Url, Publicado, FechaPublicacion) VALUES
  (1, 3, 1, 'Cuentos cortos para practicar', 'Selección de lecturas breves.', 'https://example.com/cuentos', 1, '2026-09-02 10:00:00')
ON DUPLICATE KEY UPDATE IdMaterial = IdMaterial;

INSERT INTO TB_TAREA (IdTarea, IdMaterial, IdUsuarioRegistro, Titulo, Descripcion, FechaLimite) VALUES
  (1, 1, 1, 'Lectura de la semana', 'Leer un cuento del material y comentarlo.', '2026-09-20')
ON DUPLICATE KEY UPDATE IdTarea = IdTarea;

-- La misma tarea asignada a los dos estudiantes, cada uno con su estado
INSERT INTO TB_TAREA_ASIGNACION (IdTareaAsignacion, IdTarea, IdEstudiante, IdEstadoTarea, FechaEntrega, ComentarioEstudiante) VALUES
  (1, 1, 1, 1, NULL, NULL),
  (2, 1, 2, 2, '2026-09-18 16:30:00', 'Me gustó el cuento del perro.')
ON DUPLICATE KEY UPDATE IdTareaAsignacion = IdTareaAsignacion;

INSERT INTO TB_MENSAJE (IdMensaje, IdUsuarioRemitente, IdUsuarioDestinatario, IdMensajePadre, Asunto, Cuerpo, Leido, FechaEnvio, FechaLectura) VALUES
  (1, 2, 1, NULL, 'Consulta sobre la tarea', '¿La lectura se entrega impresa?', 1, '2026-09-15 18:00:00', '2026-09-15 19:00:00'),
  (2, 1, 2, 1, 'Re: Consulta sobre la tarea', 'No hace falta, basta con comentarla en la sesión.', 0, '2026-09-15 19:05:00', NULL)
ON DUPLICATE KEY UPDATE IdMensaje = IdMensaje;

INSERT INTO TB_TESTIMONIO (IdTestimonio, NombreCliente, Comentario, Calificacion, Publicado) VALUES
  (1, 'Familia de prueba', 'Muy buena atención y seguimiento.', 5, 1)
ON DUPLICATE KEY UPDATE IdTestimonio = IdTestimonio;

INSERT INTO TB_BITACORA (IdBitacora, IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo, FechaHora) VALUES
  (1, 1, 'TB_ENCARGADO', 1, 'Crear', NULL,
     JSON_OBJECT('Nombre', 'Andrea', 'PrimerApellido', 'Solís', 'IdEstadoCliente', 1), '2026-08-20 10:00:00'),
  (2, 1, 'TB_ENCARGADO', 1, 'CambiarEstado',
     JSON_OBJECT('IdEstadoCliente', 1), JSON_OBJECT('IdEstadoCliente', 4), '2026-08-25 11:00:00'),
  (3, 1, 'TB_CITA', 4, 'CambiarEstado',
     JSON_OBJECT('IdEstadoCita', 1), JSON_OBJECT('IdEstadoCita', 4, 'MotivoCancelacion', 'La familia pidió reprogramar.'), '2026-09-12 08:30:00')
ON DUPLICATE KEY UPDATE IdBitacora = IdBitacora;

INSERT INTO TB_LOG_ERROR (IdLogError, FechaHora, Nivel, Origen, Mensaje, TipoExcepcion, UrlSolicitada, MetodoHttp, IdUsuario) VALUES
  (1, '2026-09-12 09:15:00', 'Error', 'CitasController/Crear', 'Error de prueba: tiempo de espera agotado.', 'TimeoutException', '/Citas/Crear', 'POST', 1)
ON DUPLICATE KEY UPDATE IdLogError = IdLogError;
