-- ============================================================
-- Mente Activa CRM - 01 Esquema
-- MySQL 8.4 LTS
--
-- Crea la base y todas las tablas. Se puede correr las veces que
-- sea: solo crea lo que falta. Si se cambia la definicion de una
-- tabla que ya existe, hay que recrear la base (ver LEEME.txt).
-- ============================================================

SET NAMES utf8mb4;

CREATE DATABASE IF NOT EXISTS mente_activa
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

USE mente_activa;


-- ============================================================
-- 1. Catalogos
-- ============================================================

CREATE TABLE IF NOT EXISTS TB_ROL (
  IdRol INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  Descripcion VARCHAR(255) NULL,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  PRIMARY KEY (IdRol),
  UNIQUE KEY UX_Rol_Nombre (Nombre),
  CONSTRAINT CK_Rol_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTADO_USUARIO (
  IdEstadoUsuario INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdEstadoUsuario),
  UNIQUE KEY UX_EstadoUsuario_Nombre (Nombre),
  CONSTRAINT CK_EstadoUsuario_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTADO_CLIENTE (
  IdEstadoCliente INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  Orden INT NULL,
  ClaseCss VARCHAR(50) NULL,
  PRIMARY KEY (IdEstadoCliente),
  UNIQUE KEY UX_EstadoCliente_Nombre (Nombre),
  CONSTRAINT CK_EstadoCliente_NombreNoVacio CHECK (TRIM(Nombre) <> ''),
  CONSTRAINT CK_EstadoCliente_Orden CHECK (Orden IS NULL OR Orden > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTADO_SOLICITUD (
  IdEstadoSolicitud INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdEstadoSolicitud),
  UNIQUE KEY UX_EstadoSolicitud_Nombre (Nombre),
  CONSTRAINT CK_EstadoSolicitud_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTADO_CITA (
  IdEstadoCita INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  ColorHex VARCHAR(7) NULL,
  PRIMARY KEY (IdEstadoCita),
  UNIQUE KEY UX_EstadoCita_Nombre (Nombre),
  CONSTRAINT CK_EstadoCita_NombreNoVacio CHECK (TRIM(Nombre) <> ''),
  CONSTRAINT CK_EstadoCita_ColorHex CHECK (ColorHex IS NULL OR ColorHex REGEXP '^#[0-9A-Fa-f]{6}$')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTADO_PAGO (
  IdEstadoPago INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdEstadoPago),
  UNIQUE KEY UX_EstadoPago_Nombre (Nombre),
  CONSTRAINT CK_EstadoPago_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTADO_PLAN (
  IdEstadoPlan INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdEstadoPlan),
  UNIQUE KEY UX_EstadoPlan_Nombre (Nombre),
  CONSTRAINT CK_EstadoPlan_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTADO_TAREA (
  IdEstadoTarea INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdEstadoTarea),
  UNIQUE KEY UX_EstadoTarea_Nombre (Nombre),
  CONSTRAINT CK_EstadoTarea_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_TIPO_SESION (
  IdTipoSesion INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  DuracionMinutos INT NULL,
  PRIMARY KEY (IdTipoSesion),
  UNIQUE KEY UX_TipoSesion_Nombre (Nombre),
  CONSTRAINT CK_TipoSesion_NombreNoVacio CHECK (TRIM(Nombre) <> ''),
  CONSTRAINT CK_TipoSesion_Duracion CHECK (DuracionMinutos IS NULL OR DuracionMinutos > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_TIPO_ATENCION (
  IdTipoAtencion INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdTipoAtencion),
  UNIQUE KEY UX_TipoAtencion_Nombre (Nombre),
  CONSTRAINT CK_TipoAtencion_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_MODALIDAD (
  IdModalidad INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(30) NOT NULL,
  PRIMARY KEY (IdModalidad),
  UNIQUE KEY UX_Modalidad_Nombre (Nombre),
  CONSTRAINT CK_Modalidad_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_METODO_PAGO (
  IdMetodoPago INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdMetodoPago),
  UNIQUE KEY UX_MetodoPago_Nombre (Nombre),
  CONSTRAINT CK_MetodoPago_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_NIVEL_EDUCATIVO (
  IdNivelEducativo INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  Orden INT NULL,
  PRIMARY KEY (IdNivelEducativo),
  UNIQUE KEY UX_NivelEducativo_Nombre (Nombre),
  CONSTRAINT CK_NivelEducativo_NombreNoVacio CHECK (TRIM(Nombre) <> ''),
  CONSTRAINT CK_NivelEducativo_Orden CHECK (Orden IS NULL OR Orden > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Unico por nombre y canton: hay escuelas con el mismo nombre en cantones distintos
CREATE TABLE IF NOT EXISTS TB_INSTITUCION (
  IdInstitucion INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(150) NOT NULL,
  Canton VARCHAR(100) NULL,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  PRIMARY KEY (IdInstitucion),
  UNIQUE KEY UX_Institucion_NombreCanton (Nombre, Canton),
  CONSTRAINT CK_Institucion_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_SERVICIO (
  IdServicio INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(100) NOT NULL,
  Descripcion VARCHAR(500) NULL,
  PrecioReferencia DECIMAL(10,2) NULL,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  PRIMARY KEY (IdServicio),
  UNIQUE KEY UX_Servicio_Nombre (Nombre),
  CONSTRAINT CK_Servicio_NombreNoVacio CHECK (TRIM(Nombre) <> ''),
  CONSTRAINT CK_Servicio_Precio CHECK (PrecioReferencia IS NULL OR PrecioReferencia >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_AREA_DIFICULTAD (
  IdAreaDificultad INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(100) NOT NULL,
  Descripcion VARCHAR(255) NULL,
  PRIMARY KEY (IdAreaDificultad),
  UNIQUE KEY UX_AreaDificultad_Nombre (Nombre),
  CONSTRAINT CK_AreaDificultad_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_TIPO_TELEFONO (
  IdTipoTelefono INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(30) NOT NULL,
  PRIMARY KEY (IdTipoTelefono),
  UNIQUE KEY UX_TipoTelefono_Nombre (Nombre),
  CONSTRAINT CK_TipoTelefono_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_PARENTESCO (
  IdParentesco INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdParentesco),
  UNIQUE KEY UX_Parentesco_Nombre (Nombre),
  CONSTRAINT CK_Parentesco_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_TIPO_MATERIAL (
  IdTipoMaterial INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(50) NOT NULL,
  PRIMARY KEY (IdTipoMaterial),
  UNIQUE KEY UX_TipoMaterial_Nombre (Nombre),
  CONSTRAINT CK_TipoMaterial_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_CANAL_NOTIFICACION (
  IdCanalNotificacion INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(30) NOT NULL,
  PRIMARY KEY (IdCanalNotificacion),
  UNIQUE KEY UX_CanalNotificacion_Nombre (Nombre),
  CONSTRAINT CK_CanalNotificacion_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Parametros del negocio en clave-valor. Valor puede quedar vacio
-- mientras el parametro no se haya llenado.
CREATE TABLE IF NOT EXISTS TB_CONFIGURACION (
  IdConfiguracion INT NOT NULL AUTO_INCREMENT,
  Clave VARCHAR(100) NOT NULL,
  Valor VARCHAR(1000) NOT NULL,
  Descripcion VARCHAR(255) NULL,
  FechaModificacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (IdConfiguracion),
  UNIQUE KEY UX_Configuracion_Clave (Clave),
  CONSTRAINT CK_Configuracion_ClaveNoVacio CHECK (TRIM(Clave) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- 2. Clientes y seguridad
-- ============================================================

CREATE TABLE IF NOT EXISTS TB_ENCARGADO (
  IdEncargado INT NOT NULL AUTO_INCREMENT,
  IdEstadoCliente INT NOT NULL,
  IdServicioInteres INT NULL,
  Nombre VARCHAR(100) NOT NULL,
  PrimerApellido VARCHAR(100) NOT NULL,
  SegundoApellido VARCHAR(100) NULL,
  Identificacion VARCHAR(20) NULL,
  Correo VARCHAR(150) NULL,
  Observaciones VARCHAR(1000) NULL,
  FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaModificacion DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  PRIMARY KEY (IdEncargado),
  UNIQUE KEY UX_Encargado_Identificacion (Identificacion),
  KEY IX_Encargado_EstadoCliente (IdEstadoCliente),
  KEY IX_Encargado_ServicioInteres (IdServicioInteres),
  CONSTRAINT FK_Encargado_EstadoCliente FOREIGN KEY (IdEstadoCliente)
    REFERENCES TB_ESTADO_CLIENTE (IdEstadoCliente) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Encargado_ServicioInteres FOREIGN KEY (IdServicioInteres)
    REFERENCES TB_SERVICIO (IdServicio) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Encargado_NombreNoVacio CHECK (TRIM(Nombre) <> ''),
  CONSTRAINT CK_Encargado_PrimerApellidoNoVacio CHECK (TRIM(PrimerApellido) <> ''),
  -- una cedula en blanco contaria como valor y chocaria con el indice unico; sin cedula va NULL
  CONSTRAINT CK_Encargado_IdentificacionNoVacio CHECK (Identificacion IS NULL OR TRIM(Identificacion) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- FechaModificacion sin ON UPDATE a proposito: el login toca UltimoAcceso e
-- IntentosFallidos y eso no cuenta como modificacion del usuario
CREATE TABLE IF NOT EXISTS TB_USUARIO (
  IdUsuario INT NOT NULL AUTO_INCREMENT,
  IdEncargado INT NULL,
  IdEstadoUsuario INT NOT NULL,
  NombreCompleto VARCHAR(150) NOT NULL,
  Correo VARCHAR(150) NOT NULL,
  ContrasenaHash VARCHAR(255) NOT NULL,
  ContrasenaSalt VARCHAR(100) NULL,
  UltimoAcceso DATETIME NULL,
  IntentosFallidos INT NOT NULL DEFAULT 0,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaModificacion DATETIME NULL,
  PRIMARY KEY (IdUsuario),
  UNIQUE KEY UX_Usuario_Correo (Correo),
  UNIQUE KEY UX_Usuario_Encargado (IdEncargado),
  KEY IX_Usuario_EstadoUsuario (IdEstadoUsuario),
  CONSTRAINT FK_Usuario_Encargado FOREIGN KEY (IdEncargado)
    REFERENCES TB_ENCARGADO (IdEncargado) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Usuario_EstadoUsuario FOREIGN KEY (IdEstadoUsuario)
    REFERENCES TB_ESTADO_USUARIO (IdEstadoUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Usuario_NombreCompletoNoVacio CHECK (TRIM(NombreCompleto) <> ''),
  CONSTRAINT CK_Usuario_CorreoNoVacio CHECK (TRIM(Correo) <> ''),
  CONSTRAINT CK_Usuario_ContrasenaHashNoVacio CHECK (TRIM(ContrasenaHash) <> ''),
  CONSTRAINT CK_Usuario_IntentosFallidos CHECK (IntentosFallidos >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_USUARIO_ROL (
  IdUsuarioRol INT NOT NULL AUTO_INCREMENT,
  IdUsuario INT NOT NULL,
  IdRol INT NOT NULL,
  FechaAsignacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdUsuarioRol),
  UNIQUE KEY UX_UsuarioRol_Relacion (IdUsuario, IdRol),
  KEY IX_UsuarioRol_Rol (IdRol),
  CONSTRAINT FK_UsuarioRol_Usuario FOREIGN KEY (IdUsuario)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT FK_UsuarioRol_Rol FOREIGN KEY (IdRol)
    REFERENCES TB_ROL (IdRol) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_TOKEN_RECUPERACION (
  IdToken INT NOT NULL AUTO_INCREMENT,
  IdUsuario INT NOT NULL,
  Token VARCHAR(255) NOT NULL,
  FechaExpiracion DATETIME NOT NULL,
  Usado BOOLEAN NOT NULL DEFAULT 0,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdToken),
  UNIQUE KEY UX_TokenRecuperacion_Token (Token),
  KEY IX_TokenRecuperacion_Usuario (IdUsuario),
  CONSTRAINT FK_TokenRecuperacion_Usuario FOREIGN KEY (IdUsuario)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT CK_TokenRecuperacion_TokenNoVacio CHECK (TRIM(Token) <> ''),
  CONSTRAINT CK_TokenRecuperacion_Expiracion CHECK (FechaExpiracion > FechaCreacion)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- EncargadoPrincipal solo tiene valor en el telefono principal; como el indice
-- unico admite varios NULL, eso deja un unico principal por encargado.
-- La FK a encargado va RESTRICT porque MySQL no permite CASCADE sobre la
-- columna base de una columna generada STORED (error 1215).
CREATE TABLE IF NOT EXISTS TB_TELEFONO (
  IdTelefono INT NOT NULL AUTO_INCREMENT,
  IdEncargado INT NOT NULL,
  IdTipoTelefono INT NOT NULL,
  CodigoPais VARCHAR(5) NOT NULL DEFAULT '506',
  Numero VARCHAR(15) NOT NULL,
  EsPrincipal BOOLEAN NOT NULL DEFAULT 0,
  EncargadoPrincipal INT GENERATED ALWAYS AS (IF(EsPrincipal = 1, IdEncargado, NULL)) STORED,
  PRIMARY KEY (IdTelefono),
  UNIQUE KEY UX_Telefono_Principal (EncargadoPrincipal),
  KEY IX_Telefono_Encargado (IdEncargado),
  KEY IX_Telefono_TipoTelefono (IdTipoTelefono),
  CONSTRAINT FK_Telefono_Encargado FOREIGN KEY (IdEncargado)
    REFERENCES TB_ENCARGADO (IdEncargado) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Telefono_TipoTelefono FOREIGN KEY (IdTipoTelefono)
    REFERENCES TB_TIPO_TELEFONO (IdTipoTelefono) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Telefono_CodigoPais CHECK (CodigoPais REGEXP '^[0-9]{1,4}$'),
  CONSTRAINT CK_Telefono_NumeroDigitos CHECK (Numero REGEXP '^[0-9]+$'),
  CONSTRAINT CK_Telefono_LongitudCostaRica CHECK (CodigoPais <> '506' OR CHAR_LENGTH(Numero) = 8)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTUDIANTE (
  IdEstudiante INT NOT NULL AUTO_INCREMENT,
  IdNivelEducativo INT NULL,
  IdInstitucion INT NULL,
  Nombre VARCHAR(100) NOT NULL,
  PrimerApellido VARCHAR(100) NOT NULL,
  SegundoApellido VARCHAR(100) NULL,
  FechaNacimiento DATE NULL,
  Observaciones VARCHAR(1000) NULL,
  NecesidadesApoyo VARCHAR(1000) NULL,
  FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaModificacion DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  PRIMARY KEY (IdEstudiante),
  KEY IX_Estudiante_NivelEducativo (IdNivelEducativo),
  KEY IX_Estudiante_Institucion (IdInstitucion),
  CONSTRAINT FK_Estudiante_NivelEducativo FOREIGN KEY (IdNivelEducativo)
    REFERENCES TB_NIVEL_EDUCATIVO (IdNivelEducativo) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Estudiante_Institucion FOREIGN KEY (IdInstitucion)
    REFERENCES TB_INSTITUCION (IdInstitucion) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Estudiante_NombreNoVacio CHECK (TRIM(Nombre) <> ''),
  CONSTRAINT CK_Estudiante_PrimerApellidoNoVacio CHECK (TRIM(PrimerApellido) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Mismo truco que en TB_TELEFONO para tener un solo encargado principal por
-- estudiante, y por la misma razon la FK a estudiante va RESTRICT
CREATE TABLE IF NOT EXISTS TB_ESTUDIANTE_ENCARGADO (
  IdEstudianteEncargado INT NOT NULL AUTO_INCREMENT,
  IdEstudiante INT NOT NULL,
  IdEncargado INT NOT NULL,
  IdParentesco INT NOT NULL,
  EsPrincipal BOOLEAN NOT NULL DEFAULT 0,
  EstudiantePrincipal INT GENERATED ALWAYS AS (IF(EsPrincipal = 1, IdEstudiante, NULL)) STORED,
  PRIMARY KEY (IdEstudianteEncargado),
  UNIQUE KEY UX_EstudianteEncargado_Relacion (IdEstudiante, IdEncargado),
  UNIQUE KEY UX_EstudianteEncargado_Principal (EstudiantePrincipal),
  KEY IX_EstudianteEncargado_Encargado (IdEncargado),
  KEY IX_EstudianteEncargado_Parentesco (IdParentesco),
  CONSTRAINT FK_EstudianteEncargado_Estudiante FOREIGN KEY (IdEstudiante)
    REFERENCES TB_ESTUDIANTE (IdEstudiante) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_EstudianteEncargado_Encargado FOREIGN KEY (IdEncargado)
    REFERENCES TB_ENCARGADO (IdEncargado) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT FK_EstudianteEncargado_Parentesco FOREIGN KEY (IdParentesco)
    REFERENCES TB_PARENTESCO (IdParentesco) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Mensajes del formulario publico. No son clientes: el equipo los revisa y,
-- si procede, los convierte (IdEncargado queda enlazado al cliente creado).
CREATE TABLE IF NOT EXISTS TB_SOLICITUD_CONTACTO (
  IdSolicitud INT NOT NULL AUTO_INCREMENT,
  IdEstadoSolicitud INT NOT NULL DEFAULT 1,
  IdServicioInteres INT NULL,
  IdEncargado INT NULL,
  IdUsuarioAtiende INT NULL,
  Nombre VARCHAR(100) NOT NULL,
  Apellido VARCHAR(100) NOT NULL,
  Telefono VARCHAR(15) NOT NULL,
  Correo VARCHAR(150) NOT NULL,
  Mensaje VARCHAR(2000) NOT NULL,
  NotaInterna VARCHAR(1000) NULL,
  DireccionIp VARCHAR(45) NULL,
  FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaAtencion DATETIME NULL,
  PRIMARY KEY (IdSolicitud),
  KEY IX_Solicitud_Estado (IdEstadoSolicitud),
  KEY IX_Solicitud_Fecha (FechaRegistro),
  KEY IX_Solicitud_Ip (DireccionIp, FechaRegistro),
  CONSTRAINT FK_Solicitud_Estado FOREIGN KEY (IdEstadoSolicitud)
    REFERENCES TB_ESTADO_SOLICITUD (IdEstadoSolicitud) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Solicitud_Servicio FOREIGN KEY (IdServicioInteres)
    REFERENCES TB_SERVICIO (IdServicio) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Solicitud_Encargado FOREIGN KEY (IdEncargado)
    REFERENCES TB_ENCARGADO (IdEncargado) ON DELETE SET NULL ON UPDATE RESTRICT,
  CONSTRAINT FK_Solicitud_UsuarioAtiende FOREIGN KEY (IdUsuarioAtiende)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Solicitud_NombreNoVacio CHECK (TRIM(Nombre) <> ''),
  CONSTRAINT CK_Solicitud_ApellidoNoVacio CHECK (TRIM(Apellido) <> ''),
  CONSTRAINT CK_Solicitud_MensajeNoVacio CHECK (TRIM(Mensaje) <> ''),
  CONSTRAINT CK_Solicitud_Telefono CHECK (Telefono REGEXP '^[0-9]{8}$')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTUDIANTE_AREA (
  IdEstudianteArea INT NOT NULL AUTO_INCREMENT,
  IdEstudiante INT NOT NULL,
  IdAreaDificultad INT NOT NULL,
  Observacion VARCHAR(500) NULL,
  FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdEstudianteArea),
  UNIQUE KEY UX_EstudianteArea_Relacion (IdEstudiante, IdAreaDificultad),
  KEY IX_EstudianteArea_AreaDificultad (IdAreaDificultad),
  CONSTRAINT FK_EstudianteArea_Estudiante FOREIGN KEY (IdEstudiante)
    REFERENCES TB_ESTUDIANTE (IdEstudiante) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT FK_EstudianteArea_AreaDificultad FOREIGN KEY (IdAreaDificultad)
    REFERENCES TB_AREA_DIFICULTAD (IdAreaDificultad) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- 3. Agenda y sesiones
-- ============================================================

-- Horario del consultorio: no apunta a nadie porque hay una sola profesional
CREATE TABLE IF NOT EXISTS TB_HORARIO_ATENCION (
  IdHorarioAtencion INT NOT NULL AUTO_INCREMENT,
  DiaSemana TINYINT UNSIGNED NOT NULL,
  HoraInicio TIME NOT NULL,
  HoraFin TIME NOT NULL,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  PRIMARY KEY (IdHorarioAtencion),
  CONSTRAINT CK_HorarioAtencion_DiaSemana CHECK (DiaSemana BETWEEN 1 AND 7),
  CONSTRAINT CK_HorarioAtencion_Rango CHECK (HoraFin > HoraInicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_DIA_NO_LABORAL (
  IdDiaNoLaboral INT NOT NULL AUTO_INCREMENT,
  Fecha DATE NOT NULL,
  Motivo VARCHAR(150) NOT NULL,
  EsFeriado BOOLEAN NOT NULL DEFAULT 0,
  PRIMARY KEY (IdDiaNoLaboral),
  UNIQUE KEY UX_DiaNoLaboral_Fecha (Fecha),
  CONSTRAINT CK_DiaNoLaboral_MotivoNoVacio CHECK (TRIM(Motivo) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_CITA (
  IdCita INT NOT NULL AUTO_INCREMENT,
  IdEstudiante INT NOT NULL,
  IdTipoSesion INT NOT NULL,
  IdModalidad INT NOT NULL,
  IdEstadoCita INT NOT NULL,
  IdUsuarioRegistro INT NOT NULL,
  FechaHoraInicio DATETIME NOT NULL,
  FechaHoraFin DATETIME NOT NULL,
  Observaciones VARCHAR(500) NULL,
  MotivoCancelacion VARCHAR(500) NULL,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaModificacion DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (IdCita),
  KEY IX_Cita_FechaHoraInicio (FechaHoraInicio),
  KEY IX_Cita_Estudiante (IdEstudiante),
  KEY IX_Cita_TipoSesion (IdTipoSesion),
  KEY IX_Cita_Modalidad (IdModalidad),
  KEY IX_Cita_EstadoCita (IdEstadoCita),
  KEY IX_Cita_UsuarioRegistro (IdUsuarioRegistro),
  CONSTRAINT FK_Cita_Estudiante FOREIGN KEY (IdEstudiante)
    REFERENCES TB_ESTUDIANTE (IdEstudiante) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Cita_TipoSesion FOREIGN KEY (IdTipoSesion)
    REFERENCES TB_TIPO_SESION (IdTipoSesion) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Cita_Modalidad FOREIGN KEY (IdModalidad)
    REFERENCES TB_MODALIDAD (IdModalidad) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Cita_EstadoCita FOREIGN KEY (IdEstadoCita)
    REFERENCES TB_ESTADO_CITA (IdEstadoCita) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Cita_UsuarioRegistro FOREIGN KEY (IdUsuarioRegistro)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Cita_RangoFechas CHECK (FechaHoraFin > FechaHoraInicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_SESION (
  IdSesion INT NOT NULL AUTO_INCREMENT,
  IdEstudiante INT NOT NULL,
  IdCita INT NULL,
  IdTipoAtencion INT NOT NULL,
  IdUsuarioRegistro INT NOT NULL,
  Fecha DATE NOT NULL,
  TemaTrabajado VARCHAR(500) NOT NULL,
  Avances VARCHAR(1000) NULL,
  Recomendaciones VARCHAR(1000) NULL,
  Observaciones VARCHAR(1000) NULL,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaModificacion DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (IdSesion),
  UNIQUE KEY UX_Sesion_Cita (IdCita),
  KEY IX_Sesion_Estudiante (IdEstudiante),
  KEY IX_Sesion_TipoAtencion (IdTipoAtencion),
  KEY IX_Sesion_UsuarioRegistro (IdUsuarioRegistro),
  CONSTRAINT FK_Sesion_Estudiante FOREIGN KEY (IdEstudiante)
    REFERENCES TB_ESTUDIANTE (IdEstudiante) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Sesion_Cita FOREIGN KEY (IdCita)
    REFERENCES TB_CITA (IdCita) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Sesion_TipoAtencion FOREIGN KEY (IdTipoAtencion)
    REFERENCES TB_TIPO_ATENCION (IdTipoAtencion) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Sesion_UsuarioRegistro FOREIGN KEY (IdUsuarioRegistro)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Sesion_TemaTrabajadoNoVacio CHECK (TRIM(TemaTrabajado) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_NOTIFICACION (
  IdNotificacion INT NOT NULL AUTO_INCREMENT,
  IdCita INT NULL,
  IdUsuarioDestinatario INT NOT NULL,
  IdCanalNotificacion INT NOT NULL,
  Asunto VARCHAR(200) NULL,
  Cuerpo VARCHAR(1000) NOT NULL,
  FechaProgramada DATETIME NOT NULL,
  FechaEnvio DATETIME NULL,
  Enviada BOOLEAN NOT NULL DEFAULT 0,
  MensajeError VARCHAR(500) NULL,
  PRIMARY KEY (IdNotificacion),
  KEY IX_Notificacion_Cita (IdCita),
  KEY IX_Notificacion_UsuarioDestinatario (IdUsuarioDestinatario),
  KEY IX_Notificacion_CanalNotificacion (IdCanalNotificacion),
  CONSTRAINT FK_Notificacion_Cita FOREIGN KEY (IdCita)
    REFERENCES TB_CITA (IdCita) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT FK_Notificacion_UsuarioDestinatario FOREIGN KEY (IdUsuarioDestinatario)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Notificacion_CanalNotificacion FOREIGN KEY (IdCanalNotificacion)
    REFERENCES TB_CANAL_NOTIFICACION (IdCanalNotificacion) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Notificacion_CuerpoNoVacio CHECK (TRIM(Cuerpo) <> ''),
  CONSTRAINT CK_Notificacion_EnvioCoherente CHECK (
    (Enviada = 0 AND FechaEnvio IS NULL) OR (Enviada = 1 AND FechaEnvio IS NOT NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- 4. Planes de intervencion
-- ============================================================

CREATE TABLE IF NOT EXISTS TB_PLAN_INTERVENCION (
  IdPlan INT NOT NULL AUTO_INCREMENT,
  IdEstudiante INT NOT NULL,
  IdEstadoPlan INT NOT NULL,
  IdUsuarioRegistro INT NOT NULL,
  Titulo VARCHAR(150) NOT NULL,
  ObjetivoGeneral VARCHAR(1000) NOT NULL,
  FechaInicio DATE NOT NULL,
  FechaFin DATE NULL,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaModificacion DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (IdPlan),
  KEY IX_PlanIntervencion_Estudiante (IdEstudiante),
  KEY IX_PlanIntervencion_EstadoPlan (IdEstadoPlan),
  KEY IX_PlanIntervencion_UsuarioRegistro (IdUsuarioRegistro),
  CONSTRAINT FK_PlanIntervencion_Estudiante FOREIGN KEY (IdEstudiante)
    REFERENCES TB_ESTUDIANTE (IdEstudiante) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_PlanIntervencion_EstadoPlan FOREIGN KEY (IdEstadoPlan)
    REFERENCES TB_ESTADO_PLAN (IdEstadoPlan) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_PlanIntervencion_UsuarioRegistro FOREIGN KEY (IdUsuarioRegistro)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_PlanIntervencion_TituloNoVacio CHECK (TRIM(Titulo) <> ''),
  CONSTRAINT CK_PlanIntervencion_ObjetivoGeneralNoVacio CHECK (TRIM(ObjetivoGeneral) <> ''),
  CONSTRAINT CK_PlanIntervencion_Vigencia CHECK (FechaFin IS NULL OR FechaFin >= FechaInicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ESTRATEGIA (
  IdEstrategia INT NOT NULL AUTO_INCREMENT,
  IdPlan INT NOT NULL,
  Descripcion VARCHAR(500) NOT NULL,
  Orden INT NULL,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdEstrategia),
  KEY IX_Estrategia_Plan (IdPlan),
  CONSTRAINT FK_Estrategia_PlanIntervencion FOREIGN KEY (IdPlan)
    REFERENCES TB_PLAN_INTERVENCION (IdPlan) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT CK_Estrategia_DescripcionNoVacio CHECK (TRIM(Descripcion) <> ''),
  CONSTRAINT CK_Estrategia_Orden CHECK (Orden IS NULL OR Orden > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_ACTIVIDAD (
  IdActividad INT NOT NULL AUTO_INCREMENT,
  IdEstrategia INT NOT NULL,
  Nombre VARCHAR(150) NOT NULL,
  Descripcion VARCHAR(500) NULL,
  Completada BOOLEAN NOT NULL DEFAULT 0,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdActividad),
  KEY IX_Actividad_Estrategia (IdEstrategia),
  CONSTRAINT FK_Actividad_Estrategia FOREIGN KEY (IdEstrategia)
    REFERENCES TB_ESTRATEGIA (IdEstrategia) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT CK_Actividad_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- 5. Reportes, pagos y abonos
-- ============================================================

CREATE TABLE IF NOT EXISTS TB_REPORTE (
  IdReporte INT NOT NULL AUTO_INCREMENT,
  IdEstudiante INT NOT NULL,
  IdUsuarioGenero INT NOT NULL,
  Titulo VARCHAR(150) NOT NULL,
  PeriodoInicio DATE NOT NULL,
  PeriodoFin DATE NOT NULL,
  Contenido LONGTEXT NULL,
  RutaArchivo VARCHAR(500) NULL,
  VisibleEnPortal BOOLEAN NOT NULL DEFAULT 0,
  FechaGeneracion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdReporte),
  KEY IX_Reporte_Estudiante (IdEstudiante),
  KEY IX_Reporte_UsuarioGenero (IdUsuarioGenero),
  CONSTRAINT FK_Reporte_Estudiante FOREIGN KEY (IdEstudiante)
    REFERENCES TB_ESTUDIANTE (IdEstudiante) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Reporte_UsuarioGenero FOREIGN KEY (IdUsuarioGenero)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Reporte_TituloNoVacio CHECK (TRIM(Titulo) <> ''),
  CONSTRAINT CK_Reporte_Periodo CHECK (PeriodoFin >= PeriodoInicio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- El pago es el cobro: que se debe, cuanto y cuando vence. Quien pago, como y
-- cuando vive en TB_ABONO, porque un cobro se puede cancelar en partes y cada
-- parte la puede pagar un encargado distinto.
CREATE TABLE IF NOT EXISTS TB_PAGO (
  IdPago INT NOT NULL AUTO_INCREMENT,
  IdEstudiante INT NOT NULL,
  IdEstadoPago INT NOT NULL,
  IdUsuarioRegistro INT NOT NULL,
  Concepto VARCHAR(150) NOT NULL,
  Monto DECIMAL(10,2) NOT NULL,
  FechaEmision DATE NOT NULL,
  FechaVencimiento DATE NULL,
  Observaciones VARCHAR(500) NULL,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaModificacion DATETIME NULL DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (IdPago),
  KEY IX_Pago_Estudiante (IdEstudiante),
  KEY IX_Pago_EstadoPago (IdEstadoPago),
  KEY IX_Pago_UsuarioRegistro (IdUsuarioRegistro),
  CONSTRAINT FK_Pago_Estudiante FOREIGN KEY (IdEstudiante)
    REFERENCES TB_ESTUDIANTE (IdEstudiante) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Pago_EstadoPago FOREIGN KEY (IdEstadoPago)
    REFERENCES TB_ESTADO_PAGO (IdEstadoPago) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Pago_UsuarioRegistro FOREIGN KEY (IdUsuarioRegistro)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Pago_ConceptoNoVacio CHECK (TRIM(Concepto) <> ''),
  CONSTRAINT CK_Pago_MontoPositivo CHECK (Monto > 0),
  CONSTRAINT CK_Pago_Vencimiento CHECK (FechaVencimiento IS NULL OR FechaVencimiento >= FechaEmision)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Que la suma de abonos no pase del monto y que el pagador sea encargado del
-- estudiante no cabe en un CHECK: lo valida el procedimiento de registrar pago
CREATE TABLE IF NOT EXISTS TB_ABONO (
  IdAbono INT NOT NULL AUTO_INCREMENT,
  IdPago INT NOT NULL,
  IdEncargadoPagador INT NOT NULL,
  IdMetodoPago INT NOT NULL,
  IdUsuarioRegistro INT NOT NULL,
  Monto DECIMAL(10,2) NOT NULL,
  FechaAbono DATE NOT NULL,
  Referencia VARCHAR(100) NULL,
  Observaciones VARCHAR(500) NULL,
  Activo BOOLEAN NOT NULL DEFAULT 1,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdAbono),
  KEY IX_Abono_Pago (IdPago),
  KEY IX_Abono_EncargadoPagador (IdEncargadoPagador),
  KEY IX_Abono_MetodoPago (IdMetodoPago),
  KEY IX_Abono_UsuarioRegistro (IdUsuarioRegistro),
  CONSTRAINT FK_Abono_Pago FOREIGN KEY (IdPago)
    REFERENCES TB_PAGO (IdPago) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Abono_EncargadoPagador FOREIGN KEY (IdEncargadoPagador)
    REFERENCES TB_ENCARGADO (IdEncargado) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Abono_MetodoPago FOREIGN KEY (IdMetodoPago)
    REFERENCES TB_METODO_PAGO (IdMetodoPago) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Abono_UsuarioRegistro FOREIGN KEY (IdUsuarioRegistro)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Abono_MontoPositivo CHECK (Monto > 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- 6. Materiales y tareas
-- ============================================================

CREATE TABLE IF NOT EXISTS TB_MATERIAL (
  IdMaterial INT NOT NULL AUTO_INCREMENT,
  IdTipoMaterial INT NOT NULL,
  IdUsuarioRegistro INT NOT NULL,
  Titulo VARCHAR(150) NOT NULL,
  Descripcion VARCHAR(500) NULL,
  RutaArchivo VARCHAR(500) NULL,
  Url VARCHAR(500) NULL,
  Publicado BOOLEAN NOT NULL DEFAULT 0,
  FechaPublicacion DATETIME NULL,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdMaterial),
  KEY IX_Material_TipoMaterial (IdTipoMaterial),
  KEY IX_Material_UsuarioRegistro (IdUsuarioRegistro),
  CONSTRAINT FK_Material_TipoMaterial FOREIGN KEY (IdTipoMaterial)
    REFERENCES TB_TIPO_MATERIAL (IdTipoMaterial) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Material_UsuarioRegistro FOREIGN KEY (IdUsuarioRegistro)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Material_TituloNoVacio CHECK (TRIM(Titulo) <> ''),
  CONSTRAINT CK_Material_PublicacionCoherente CHECK (Publicado = 0 OR FechaPublicacion IS NOT NULL)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_TAREA (
  IdTarea INT NOT NULL AUTO_INCREMENT,
  IdMaterial INT NULL,
  IdUsuarioRegistro INT NOT NULL,
  Titulo VARCHAR(150) NOT NULL,
  Descripcion VARCHAR(1000) NULL,
  FechaLimite DATE NOT NULL,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdTarea),
  KEY IX_Tarea_Material (IdMaterial),
  KEY IX_Tarea_UsuarioRegistro (IdUsuarioRegistro),
  CONSTRAINT FK_Tarea_Material FOREIGN KEY (IdMaterial)
    REFERENCES TB_MATERIAL (IdMaterial) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Tarea_UsuarioRegistro FOREIGN KEY (IdUsuarioRegistro)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Tarea_TituloNoVacio CHECK (TRIM(Titulo) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_TAREA_ASIGNACION (
  IdTareaAsignacion INT NOT NULL AUTO_INCREMENT,
  IdTarea INT NOT NULL,
  IdEstudiante INT NOT NULL,
  IdEstadoTarea INT NOT NULL,
  FechaEntrega DATETIME NULL,
  ComentarioEstudiante VARCHAR(500) NULL,
  ComentarioRevision VARCHAR(500) NULL,
  PRIMARY KEY (IdTareaAsignacion),
  UNIQUE KEY UX_TareaAsignacion_Relacion (IdTarea, IdEstudiante),
  KEY IX_TareaAsignacion_Estudiante (IdEstudiante),
  KEY IX_TareaAsignacion_EstadoTarea (IdEstadoTarea),
  CONSTRAINT FK_TareaAsignacion_Tarea FOREIGN KEY (IdTarea)
    REFERENCES TB_TAREA (IdTarea) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT FK_TareaAsignacion_Estudiante FOREIGN KEY (IdEstudiante)
    REFERENCES TB_ESTUDIANTE (IdEstudiante) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT FK_TareaAsignacion_EstadoTarea FOREIGN KEY (IdEstadoTarea)
    REFERENCES TB_ESTADO_TAREA (IdEstadoTarea) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- 7. Comunicacion y contenido publico
-- ============================================================

CREATE TABLE IF NOT EXISTS TB_PLANTILLA_MENSAJE (
  IdPlantilla INT NOT NULL AUTO_INCREMENT,
  Nombre VARCHAR(100) NOT NULL,
  Asunto VARCHAR(200) NULL,
  Cuerpo LONGTEXT NOT NULL,
  Activa BOOLEAN NOT NULL DEFAULT 1,
  FechaCreacion DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdPlantilla),
  CONSTRAINT CK_PlantillaMensaje_NombreNoVacio CHECK (TRIM(Nombre) <> '')
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_MENSAJE (
  IdMensaje INT NOT NULL AUTO_INCREMENT,
  IdUsuarioRemitente INT NOT NULL,
  IdUsuarioDestinatario INT NOT NULL,
  IdMensajePadre INT NULL,
  Asunto VARCHAR(200) NOT NULL,
  Cuerpo LONGTEXT NOT NULL,
  Leido BOOLEAN NOT NULL DEFAULT 0,
  FechaEnvio DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  FechaLectura DATETIME NULL,
  PRIMARY KEY (IdMensaje),
  KEY IX_Mensaje_UsuarioRemitente (IdUsuarioRemitente),
  KEY IX_Mensaje_UsuarioDestinatario (IdUsuarioDestinatario),
  KEY IX_Mensaje_MensajePadre (IdMensajePadre),
  CONSTRAINT FK_Mensaje_UsuarioRemitente FOREIGN KEY (IdUsuarioRemitente)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Mensaje_UsuarioDestinatario FOREIGN KEY (IdUsuarioDestinatario)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT FK_Mensaje_MensajePadre FOREIGN KEY (IdMensajePadre)
    REFERENCES TB_MENSAJE (IdMensaje) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Mensaje_AsuntoNoVacio CHECK (TRIM(Asunto) <> ''),
  CONSTRAINT CK_Mensaje_RemitenteDistinto CHECK (IdUsuarioRemitente <> IdUsuarioDestinatario),
  CONSTRAINT CK_Mensaje_LecturaCoherente CHECK (
    (Leido = 0 AND FechaLectura IS NULL) OR (Leido = 1 AND FechaLectura IS NOT NULL)),
  CONSTRAINT CK_Mensaje_FechaLectura CHECK (FechaLectura IS NULL OR FechaLectura >= FechaEnvio)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_TESTIMONIO (
  IdTestimonio INT NOT NULL AUTO_INCREMENT,
  NombreCliente VARCHAR(100) NOT NULL,
  Comentario VARCHAR(1000) NOT NULL,
  Calificacion INT NULL,
  Publicado BOOLEAN NOT NULL DEFAULT 0,
  FechaRegistro DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdTestimonio),
  CONSTRAINT CK_Testimonio_NombreClienteNoVacio CHECK (TRIM(NombreCliente) <> ''),
  CONSTRAINT CK_Testimonio_ComentarioNoVacio CHECK (TRIM(Comentario) <> ''),
  CONSTRAINT CK_Testimonio_Calificacion CHECK (Calificacion IS NULL OR Calificacion BETWEEN 1 AND 5)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;


-- ============================================================
-- 8. Trazabilidad
-- ============================================================

-- Sin llaves foraneas a proposito: tiene que poder escribirse aunque el usuario
-- no exista o la transaccion del negocio se haya revertido
CREATE TABLE IF NOT EXISTS TB_LOG_ERROR (
  IdLogError BIGINT NOT NULL AUTO_INCREMENT,
  FechaHora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  Nivel VARCHAR(20) NOT NULL,
  Origen VARCHAR(200) NULL,
  Mensaje TEXT NOT NULL,
  TipoExcepcion VARCHAR(200) NULL,
  StackTrace LONGTEXT NULL,
  UrlSolicitada VARCHAR(500) NULL,
  MetodoHttp VARCHAR(10) NULL,
  IdUsuario INT NULL,
  DireccionIp VARCHAR(45) NULL,
  NavegadorAgente VARCHAR(500) NULL,
  Resuelto BOOLEAN NOT NULL DEFAULT 0,
  PRIMARY KEY (IdLogError),
  CONSTRAINT CK_LogError_Nivel CHECK (Nivel IN ('Error', 'Advertencia', 'Crítico'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS TB_BITACORA (
  IdBitacora BIGINT NOT NULL AUTO_INCREMENT,
  IdUsuario INT NOT NULL,
  Entidad VARCHAR(100) NOT NULL,
  IdRegistro INT NOT NULL,
  Accion VARCHAR(20) NOT NULL,
  ValorAnterior JSON NULL,
  ValorNuevo JSON NULL,
  FechaHora DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (IdBitacora),
  KEY IX_Bitacora_FechaHora (FechaHora),
  KEY IX_Bitacora_EntidadRegistro (Entidad, IdRegistro),
  KEY IX_Bitacora_Usuario (IdUsuario),
  CONSTRAINT FK_Bitacora_Usuario FOREIGN KEY (IdUsuario)
    REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT CK_Bitacora_EntidadNoVacio CHECK (TRIM(Entidad) <> ''),
  CONSTRAINT CK_Bitacora_Accion CHECK (Accion IN ('Crear', 'Editar', 'Eliminar', 'CambiarEstado'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;



