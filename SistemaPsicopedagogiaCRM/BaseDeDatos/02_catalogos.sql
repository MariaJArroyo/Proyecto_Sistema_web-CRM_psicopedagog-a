-- ============================================================
-- Mente Activa CRM - 02 Catalogos y datos minimos
-- Correr despues de 01_esquema.sql
--
-- No son datos de prueba: sin esto el sistema no arranca.
-- Los Ids son fijos porque el codigo y los procedimientos los usan
-- como constantes. No cambiarlos.
--
-- Se puede correr varias veces. Si la fila ya existe no se toca,
-- asi no se pisan los cambios hechos desde el panel. Por eso se usa
-- ON DUPLICATE KEY UPDATE con un update que no cambia nada, y no
-- INSERT IGNORE, que esconderia errores reales como un CHECK violado.
--
-- Los bloques marcados GENERICO son valores de relleno: hay que
-- reemplazarlos antes de crear la base de produccion.
-- ============================================================

SET NAMES utf8mb4;
USE mente_activa;

INSERT INTO TB_ROL (IdRol, Nombre, Descripcion) VALUES
  (1, 'Administrador', 'Acceso completo, incluida la administración de usuarios'),
  (2, 'Psicopedagoga', 'Profesional a cargo de la atención de los estudiantes'),
  (3, 'Asistente', 'Apoyo administrativo en agenda, clientes y pagos'),
  (4, 'Encargado', 'Acceso al portal de encargados')
ON DUPLICATE KEY UPDATE IdRol = IdRol;

INSERT INTO TB_ESTADO_USUARIO (IdEstadoUsuario, Nombre) VALUES
  (1, 'Activo'),
  (2, 'Inactivo'),
  (3, 'Bloqueado'),
  (4, 'Pendiente de activación')
ON DUPLICATE KEY UPDATE IdEstadoUsuario = IdEstadoUsuario;

-- Orden es la posicion en el embudo, no el Id
INSERT INTO TB_ESTADO_CLIENTE (IdEstadoCliente, Nombre, Orden, ClaseCss) VALUES
  (1, 'Nuevo', 1, 'estado-cliente-nuevo'),
  (2, 'Contactado', 2, 'estado-cliente-contactado'),
  (3, 'Cita agendada', 4, 'estado-cliente-cita-agendada'),
  (4, 'Cliente activo', 5, 'estado-cliente-activo'),
  (5, 'Inactivo', 6, 'estado-cliente-inactivo'),
  (6, 'En seguimiento', 3, 'estado-cliente-seguimiento')
ON DUPLICATE KEY UPDATE IdEstadoCliente = IdEstadoCliente;

INSERT INTO TB_ESTADO_CITA (IdEstadoCita, Nombre, ColorHex) VALUES
  (1, 'Programada', '#4F6FAE'),
  (2, 'Confirmada', '#7BC4A4'),
  (3, 'Completada', '#3E9B6E'),
  (4, 'Cancelada', '#D9534F'),
  (5, 'No asistió', '#E0A63C')
ON DUPLICATE KEY UPDATE IdEstadoCita = IdEstadoCita;

INSERT INTO TB_ESTADO_PAGO (IdEstadoPago, Nombre) VALUES
  (1, 'Pagado'),
  (2, 'Pendiente'),
  (3, 'Vencido'),
  (4, 'Parcial'),
  (5, 'Anulado')
ON DUPLICATE KEY UPDATE IdEstadoPago = IdEstadoPago;

INSERT INTO TB_ESTADO_PLAN (IdEstadoPlan, Nombre) VALUES
  (1, 'Borrador'),
  (2, 'Activo'),
  (3, 'Finalizado'),
  (4, 'Suspendido')
ON DUPLICATE KEY UPDATE IdEstadoPlan = IdEstadoPlan;

INSERT INTO TB_ESTADO_TAREA (IdEstadoTarea, Nombre) VALUES
  (1, 'Asignada'),
  (2, 'Entregada'),
  (3, 'Revisada'),
  (4, 'Vencida')
ON DUPLICATE KEY UPDATE IdEstadoTarea = IdEstadoTarea;

-- GENERICO: duraciones por defecto en minutos
INSERT INTO TB_TIPO_SESION (IdTipoSesion, Nombre, DuracionMinutos) VALUES
  (1, 'Tutoría', 60),
  (2, 'Sesión psicopedagógica', 60),
  (3, 'Evaluación', 90),
  (4, 'Reunión con padres', 45)
ON DUPLICATE KEY UPDATE IdTipoSesion = IdTipoSesion;

INSERT INTO TB_TIPO_ATENCION (IdTipoAtencion, Nombre) VALUES
  (1, 'Individual'),
  (2, 'Grupal'),
  (3, 'Familiar')
ON DUPLICATE KEY UPDATE IdTipoAtencion = IdTipoAtencion;

INSERT INTO TB_MODALIDAD (IdModalidad, Nombre) VALUES
  (1, 'Presencial'),
  (2, 'Virtual')
ON DUPLICATE KEY UPDATE IdModalidad = IdModalidad;

INSERT INTO TB_METODO_PAGO (IdMetodoPago, Nombre) VALUES
  (1, 'Efectivo'),
  (2, 'SINPE Móvil'),
  (3, 'Transferencia'),
  (4, 'Tarjeta')
ON DUPLICATE KEY UPDATE IdMetodoPago = IdMetodoPago;

INSERT INTO TB_NIVEL_EDUCATIVO (IdNivelEducativo, Nombre, Orden) VALUES
  (1, 'Preescolar', 1),
  (2, 'I Ciclo', 2),
  (3, 'II Ciclo', 3),
  (4, 'III Ciclo', 4),
  (5, 'Educación diversificada', 5)
ON DUPLICATE KEY UPDATE IdNivelEducativo = IdNivelEducativo;

-- GENERICO
INSERT INTO TB_INSTITUCION (IdInstitucion, Nombre, Canton) VALUES
  (1, 'Institución de ejemplo A', 'San José'),
  (2, 'Institución de ejemplo B', 'Heredia')
ON DUPLICATE KEY UPDATE IdInstitucion = IdInstitucion;

-- GENERICO: descripciones y precios en colones
INSERT INTO TB_SERVICIO (IdServicio, Nombre, Descripcion, PrecioReferencia) VALUES
  (1, 'Tutoría académica', 'Acompañamiento en materias escolares y hábitos de estudio.', 15000.00),
  (2, 'Apoyo psicopedagógico', 'Intervención en dificultades de aprendizaje.', 20000.00),
  (3, 'Evaluaciones', 'Valoración psicopedagógica con informe de resultados.', 45000.00),
  (4, 'Orientación a padres', 'Sesiones de guía para las familias.', 18000.00)
ON DUPLICATE KEY UPDATE IdServicio = IdServicio;

-- Del 1 al 4 vienen del modelo; del 5 en adelante son GENERICOS
INSERT INTO TB_AREA_DIFICULTAD (IdAreaDificultad, Nombre) VALUES
  (1, 'Lectoescritura'),
  (2, 'Cálculo'),
  (3, 'Atención'),
  (4, 'Comprensión lectora'),
  (5, 'Memoria'),
  (6, 'Funciones ejecutivas'),
  (7, 'Lenguaje oral'),
  (8, 'Motricidad fina'),
  (9, 'Hábitos de estudio')
ON DUPLICATE KEY UPDATE IdAreaDificultad = IdAreaDificultad;

INSERT INTO TB_TIPO_TELEFONO (IdTipoTelefono, Nombre) VALUES
  (1, 'Móvil'),
  (2, 'Casa'),
  (3, 'Trabajo')
ON DUPLICATE KEY UPDATE IdTipoTelefono = IdTipoTelefono;

INSERT INTO TB_PARENTESCO (IdParentesco, Nombre) VALUES
  (1, 'Madre'),
  (2, 'Padre'),
  (3, 'Tutor legal'),
  (4, 'Abuelo/a'),
  (5, 'Otro')
ON DUPLICATE KEY UPDATE IdParentesco = IdParentesco;

INSERT INTO TB_TIPO_MATERIAL (IdTipoMaterial, Nombre) VALUES
  (1, 'Guía educativa'),
  (2, 'Documento PDF'),
  (3, 'Enlace útil'),
  (4, 'Actividad')
ON DUPLICATE KEY UPDATE IdTipoMaterial = IdTipoMaterial;

INSERT INTO TB_CANAL_NOTIFICACION (IdCanalNotificacion, Nombre) VALUES
  (1, 'Correo'),
  (2, 'WhatsApp'),
  (3, 'Portal')
ON DUPLICATE KEY UPDATE IdCanalNotificacion = IdCanalNotificacion;

-- GENERICO: datos de contacto de relleno
INSERT INTO TB_CONFIGURACION (IdConfiguracion, Clave, Valor, Descripcion) VALUES
  (1, 'NombreConsultorio', 'Mente Activa Psicopedagogía', 'Nombre que se muestra en el sitio y en los correos'),
  (2, 'CorreoContacto', 'contacto@example.com', 'Correo de contacto del consultorio'),
  (3, 'TelefonoContacto', '00000000', 'Teléfono de contacto, solo dígitos'),
  (4, 'Moneda', 'CRC', 'Código ISO de la moneda de los montos'),
  (5, 'HorasRecordatorioCita', '24', 'Horas de anticipación para el recordatorio de cita')
ON DUPLICATE KEY UPDATE IdConfiguracion = IdConfiguracion;

-- GENERICO: sin al menos una franja no se puede agendar ninguna cita.
-- DiaSemana: 1 = lunes ... 7 = domingo
INSERT INTO TB_HORARIO_ATENCION (IdHorarioAtencion, DiaSemana, HoraInicio, HoraFin) VALUES
  (1, 1, '08:00:00', '17:00:00'),
  (2, 2, '08:00:00', '17:00:00'),
  (3, 3, '08:00:00', '17:00:00'),
  (4, 4, '08:00:00', '17:00:00'),
  (5, 5, '08:00:00', '17:00:00')
ON DUPLICATE KEY UPDATE IdHorarioAtencion = IdHorarioAtencion;
