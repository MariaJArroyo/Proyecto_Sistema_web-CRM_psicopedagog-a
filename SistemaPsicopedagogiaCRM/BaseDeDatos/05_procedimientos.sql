-- ============================================================
-- Mente Activa CRM - 05 Procedimientos almacenados
-- Correr despues de 01_esquema.sql y 02_catalogos.sql
--
-- Un solo archivo con todos los procedimientos del sistema,
-- dividido por modulo. Cada procedimiento empieza con
-- DROP PROCEDURE IF EXISTS, asi que el script se puede correr
-- las veces que haga falta sin fallar.
--
-- Para agregar uno nuevo: buscar la seccion del modulo y
-- escribirlo ahi. Si el que ya existe no calza con lo que
-- necesita tu modulo, editalo. No crear uno paralelo.
--
-- Convencion de nombres: SP_Entidad_Accion.
-- Los seis que terminan en _CRM vienen del primer avance del
-- proyecto y se dejan con su nombre original porque el API ya
-- los invoca asi. Son la unica excepcion.
-- ============================================================

SET NAMES utf8mb4;
USE mente_activa;

-- ============================================================
-- 1. Seguridad y usuarios
-- ============================================================

-- Ids de catalogo que se usan aqui:
--   TB_ESTADO_USUARIO  1 Activo  2 Inactivo  3 Bloqueado  4 Pendiente de activacion
--   TB_ROL             1 Administrador  2 Psicopedagoga  3 Asistente  4 Encargado
--
-- El hash de la contrasena se calcula siempre en la aplicacion. Aqui solo se
-- guarda y se lee. Ningun procedimiento de esta seccion recibe ni devuelve la
-- contrasena en claro, y el hash nunca se escribe en la bitacora.


-- Login: devuelve la fila que la aplicacion necesita para comparar el hash.
-- No decide nada; quien decide es el servicio de autenticacion.
DROP PROCEDURE IF EXISTS SP_Usuario_ObtenerPorCorreo;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_ObtenerPorCorreo(
  IN p_Correo VARCHAR(150)
)
BEGIN
  SELECT
    u.IdUsuario,
    u.NombreCompleto,
    u.Correo,
    u.ContrasenaHash,
    u.IdEncargado,
    u.IdEstadoUsuario,
    eu.Nombre AS EstadoUsuario,
    u.IntentosFallidos,
    r.IdRol,
    r.Nombre AS Rol
  FROM TB_USUARIO u
  JOIN TB_ESTADO_USUARIO eu ON eu.IdEstadoUsuario = u.IdEstadoUsuario
  LEFT JOIN TB_USUARIO_ROL ur ON ur.IdUsuario = u.IdUsuario
  LEFT JOIN TB_ROL r ON r.IdRol = ur.IdRol
  WHERE u.Correo = p_Correo
  LIMIT 1;
END$$
DELIMITER ;


-- Ingreso correcto: sella la fecha y borra el conteo de fallos.
DROP PROCEDURE IF EXISTS SP_Usuario_RegistrarIngreso;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_RegistrarIngreso(
  IN p_IdUsuario INT
)
BEGIN
  UPDATE TB_USUARIO
  SET UltimoAcceso = NOW(),
      IntentosFallidos = 0
  WHERE IdUsuario = p_IdUsuario;
END$$
DELIMITER ;


-- Ingreso fallido: suma uno y bloquea al llegar a cinco.
-- Recibe el correo y no el id a proposito: la aplicacion no debe averiguar si
-- la cuenta existe antes de llamar. Si no existe, no pasa nada.
DROP PROCEDURE IF EXISTS SP_Usuario_RegistrarFallo;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_RegistrarFallo(
  IN p_Correo VARCHAR(150)
)
BEGIN
  DECLARE v_IdUsuario INT DEFAULT NULL;
  DECLARE v_Intentos INT DEFAULT 0;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdUsuario INTO v_IdUsuario
  FROM TB_USUARIO
  WHERE Correo = p_Correo
  LIMIT 1;

  IF v_IdUsuario IS NOT NULL THEN
    START TRANSACTION;

    UPDATE TB_USUARIO
    SET IntentosFallidos = IntentosFallidos + 1
    WHERE IdUsuario = v_IdUsuario;

    SELECT IntentosFallidos INTO v_Intentos
    FROM TB_USUARIO
    WHERE IdUsuario = v_IdUsuario;

    IF v_Intentos >= 5 THEN
      UPDATE TB_USUARIO
      SET IdEstadoUsuario = 3
      WHERE IdUsuario = v_IdUsuario
        AND IdEstadoUsuario <> 3;

      IF ROW_COUNT() > 0 THEN
        INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
        VALUES (v_IdUsuario, 'TB_USUARIO', v_IdUsuario, 'CambiarEstado',
                JSON_OBJECT('Motivo', 'Intentos fallidos'),
                JSON_OBJECT('IdEstadoUsuario', 3, 'IntentosFallidos', v_Intentos));
      END IF;
    END IF;

    COMMIT;
  END IF;
END$$
DELIMITER ;


-- Alta de usuario interno. Nace sin contrasena y en estado Pendiente de
-- activacion: la define la persona al abrir el enlace de invitacion.
-- ContrasenaHash es NOT NULL, asi que se guarda una marca que ningun hash real
-- puede igualar. Aun asi, el estado es lo que impide entrar.
DROP PROCEDURE IF EXISTS SP_Usuario_Crear;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_Crear(
  IN p_IdUsuarioAccion INT,
  IN p_NombreCompleto VARCHAR(150),
  IN p_Correo VARCHAR(150),
  IN p_IdRol INT
)
BEGIN
  DECLARE v_IdUsuario INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF EXISTS (SELECT 1 FROM TB_USUARIO WHERE Correo = p_Correo) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe un usuario con ese correo.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ROL WHERE IdRol = p_IdRol AND Activo = 1) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El rol indicado no existe o esta inactivo.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_USUARIO (IdEncargado, IdEstadoUsuario, NombreCompleto, Correo, ContrasenaHash)
  VALUES (NULL, 4, p_NombreCompleto, p_Correo, 'PENDIENTE_ACTIVACION');

  SET v_IdUsuario = LAST_INSERT_ID();

  INSERT INTO TB_USUARIO_ROL (IdUsuario, IdRol)
  VALUES (v_IdUsuario, p_IdRol);

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_USUARIO', v_IdUsuario, 'Crear',
          JSON_OBJECT('NombreCompleto', p_NombreCompleto,
                      'Correo', p_Correo,
                      'IdRol', p_IdRol,
                      'IdEstadoUsuario', 4));

  COMMIT;

  SELECT v_IdUsuario AS IdUsuario;
END$$
DELIMITER ;


-- Una sola cuenta por su Id. Lo usa el reenvio de invitacion: el correo y el
-- nombre del destinatario se leen aqui y no se aceptan del navegador.
DROP PROCEDURE IF EXISTS SP_Usuario_ObtenerPorId;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_ObtenerPorId(
  IN p_IdUsuario INT
)
BEGIN
  SELECT
    u.IdUsuario,
    u.NombreCompleto,
    u.Correo,
    u.IdEstadoUsuario,
    eu.Nombre AS Estado,
    r.IdRol,
    r.Nombre AS Rol,
    u.IdEncargado
  FROM TB_USUARIO u
  JOIN TB_ESTADO_USUARIO eu ON eu.IdEstadoUsuario = u.IdEstadoUsuario
  LEFT JOIN TB_USUARIO_ROL ur ON ur.IdUsuario = u.IdUsuario
  LEFT JOIN TB_ROL r ON r.IdRol = ur.IdRol
  WHERE u.IdUsuario = p_IdUsuario;
END$$
DELIMITER ;


-- Edita nombre, correo y rol en una sola transaccion.
--
-- Si le cambian el correo, los enlaces vivos quedan usados: se mandaron a la
-- direccion vieja y quien la tenga no debe poder definir la contrasena de esta
-- cuenta. Hay que mandar la invitacion de nuevo.
DROP PROCEDURE IF EXISTS SP_Usuario_Editar;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_Editar(
  IN p_IdUsuarioAccion INT,
  IN p_IdUsuario INT,
  IN p_NombreCompleto VARCHAR(150),
  IN p_Correo VARCHAR(150),
  IN p_IdRol INT
)
BEGIN
  DECLARE v_Anterior JSON DEFAULT NULL;
  DECLARE v_CorreoAnterior VARCHAR(150) DEFAULT NULL;
  DECLARE v_RolAnterior INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT Correo, JSON_OBJECT('NombreCompleto', NombreCompleto, 'Correo', Correo)
    INTO v_CorreoAnterior, v_Anterior
  FROM TB_USUARIO
  WHERE IdUsuario = p_IdUsuario;

  IF v_Anterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El usuario indicado no existe.';
  END IF;

  IF EXISTS (SELECT 1 FROM TB_USUARIO
             WHERE Correo = p_Correo AND IdUsuario <> p_IdUsuario) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe otro usuario con ese correo.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ROL WHERE IdRol = p_IdRol AND Activo = 1) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El rol indicado no existe o esta inactivo.';
  END IF;

  SELECT IdRol INTO v_RolAnterior
  FROM TB_USUARIO_ROL
  WHERE IdUsuario = p_IdUsuario
  LIMIT 1;

  -- Siempre debe quedar al menos un administrador activo
  IF v_RolAnterior = 1 AND p_IdRol <> 1
     AND FN_Usuario_OtrosAdministradoresActivos(p_IdUsuario) = 0 THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Debe quedar al menos un administrador activo. Asigne el rol de Administrador a otra persona antes de cambiar este.';
  END IF;

  START TRANSACTION;

  UPDATE TB_USUARIO
  SET NombreCompleto = p_NombreCompleto,
      Correo = p_Correo,
      FechaModificacion = NOW()
  WHERE IdUsuario = p_IdUsuario;

  IF v_CorreoAnterior <> p_Correo THEN
    UPDATE TB_TOKEN_RECUPERACION
    SET Usado = 1
    WHERE IdUsuario = p_IdUsuario AND Usado = 0;
  END IF;

  IF v_RolAnterior IS NULL OR v_RolAnterior <> p_IdRol THEN
    DELETE FROM TB_USUARIO_ROL WHERE IdUsuario = p_IdUsuario;
    INSERT INTO TB_USUARIO_ROL (IdUsuario, IdRol) VALUES (p_IdUsuario, p_IdRol);
  END IF;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_USUARIO', p_IdUsuario, 'Editar',
          JSON_SET(v_Anterior, '$.IdRol', v_RolAnterior),
          JSON_OBJECT('NombreCompleto', p_NombreCompleto,
                      'Correo', p_Correo,
                      'IdRol', p_IdRol));

  COMMIT;
END$$
DELIMITER ;


-- Listado de la pantalla de administracion. Busca por nombre o correo y filtra
-- por estado. p_Busqueda y p_IdEstadoUsuario en NULL traen todo.
DROP PROCEDURE IF EXISTS SP_Usuario_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_Listar(
  IN p_Busqueda VARCHAR(150),
  IN p_IdEstadoUsuario INT,
  IN p_SoloInternos BOOLEAN,
  IN p_Pagina INT,
  IN p_TamanoPagina INT
)
BEGIN
  DECLARE v_Desplazamiento INT;

  SET p_Pagina = IFNULL(NULLIF(p_Pagina, 0), 1);
  SET p_TamanoPagina = IFNULL(NULLIF(p_TamanoPagina, 0), 25);
  SET v_Desplazamiento = (p_Pagina - 1) * p_TamanoPagina;

  SELECT
    u.IdUsuario,
    u.NombreCompleto,
    u.Correo,
    eu.Nombre AS Estado,
    r.Nombre AS Rol,
    u.UltimoAcceso,
    u.FechaCreacion,
    u.IdEncargado
  FROM TB_USUARIO u
  JOIN TB_ESTADO_USUARIO eu ON eu.IdEstadoUsuario = u.IdEstadoUsuario
  LEFT JOIN TB_USUARIO_ROL ur ON ur.IdUsuario = u.IdUsuario
  LEFT JOIN TB_ROL r ON r.IdRol = ur.IdRol
  WHERE (p_Busqueda IS NULL OR p_Busqueda = ''
         OR u.NombreCompleto LIKE CONCAT('%', p_Busqueda, '%')
         OR u.Correo LIKE CONCAT('%', p_Busqueda, '%'))
    AND (p_IdEstadoUsuario IS NULL OR u.IdEstadoUsuario = p_IdEstadoUsuario)
    AND (p_SoloInternos IS NULL
         OR (p_SoloInternos = 1 AND u.IdEncargado IS NULL)
         OR (p_SoloInternos = 0 AND u.IdEncargado IS NOT NULL))
  ORDER BY u.NombreCompleto
  LIMIT p_TamanoPagina OFFSET v_Desplazamiento;
END$$
DELIMITER ;

-- Cuantos administradores activos hay sin contar a p_IdUsuario. Si da 0,
-- quitarle el rol o desactivar a ese usuario dejaria el sistema sin administrador.
DROP FUNCTION IF EXISTS FN_Usuario_OtrosAdministradoresActivos;
DELIMITER $$
CREATE FUNCTION FN_Usuario_OtrosAdministradoresActivos(p_IdUsuario INT)
RETURNS INT
READS SQL DATA
BEGIN
  RETURN (
    SELECT COUNT(*)
    FROM TB_USUARIO u
    JOIN TB_USUARIO_ROL ur ON ur.IdUsuario = u.IdUsuario
    WHERE ur.IdRol = 1                -- Administrador
      AND u.IdEstadoUsuario = 1       -- Activo
      AND u.IdUsuario <> p_IdUsuario);
END$$
DELIMITER ;

-- Activar, inactivar o bloquear. El borrado de usuarios no existe.
DROP PROCEDURE IF EXISTS SP_Usuario_CambiarEstado;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_CambiarEstado(
  IN p_IdUsuarioAccion INT,
  IN p_IdUsuario INT,
  IN p_IdEstadoUsuario INT
)
BEGIN
  DECLARE v_EstadoAnterior INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdEstadoUsuario INTO v_EstadoAnterior
  FROM TB_USUARIO
  WHERE IdUsuario = p_IdUsuario;

  IF v_EstadoAnterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El usuario indicado no existe.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTADO_USUARIO WHERE IdEstadoUsuario = p_IdEstadoUsuario) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estado indicado no existe.';
  END IF;

   -- Desactivar o bloquear al unico administrador activo dejaria el sistema sin acceso
  IF v_EstadoAnterior = 1 AND p_IdEstadoUsuario <> 1
     AND EXISTS (SELECT 1 FROM TB_USUARIO_ROL WHERE IdUsuario = p_IdUsuario AND IdRol = 1)
     AND FN_Usuario_OtrosAdministradoresActivos(p_IdUsuario) = 0 THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'No se puede desactivar al único administrador activo del sistema.';
  END IF;

  START TRANSACTION;

  UPDATE TB_USUARIO
  SET IdEstadoUsuario = p_IdEstadoUsuario,
      FechaModificacion = NOW(),
      IntentosFallidos = IF(p_IdEstadoUsuario = 1, 0, IntentosFallidos)
  WHERE IdUsuario = p_IdUsuario;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_USUARIO', p_IdUsuario, 'CambiarEstado',
          JSON_OBJECT('IdEstadoUsuario', v_EstadoAnterior),
          JSON_OBJECT('IdEstadoUsuario', p_IdEstadoUsuario));

  COMMIT;
END$$
DELIMITER ;


-- Un rol por usuario. La tabla admite varios a proposito, por si el equipo lo
-- necesita mas adelante; la regla de uno solo se aplica aqui borrando el
-- anterior antes de insertar.
DROP PROCEDURE IF EXISTS SP_Usuario_AsignarRol;
DELIMITER $$
CREATE PROCEDURE SP_Usuario_AsignarRol(
  IN p_IdUsuarioAccion INT,
  IN p_IdUsuario INT,
  IN p_IdRol INT
)
BEGIN
  DECLARE v_RolAnterior INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF NOT EXISTS (SELECT 1 FROM TB_USUARIO WHERE IdUsuario = p_IdUsuario) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El usuario indicado no existe.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ROL WHERE IdRol = p_IdRol AND Activo = 1) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El rol indicado no existe o esta inactivo.';
  END IF;

  SELECT IdRol INTO v_RolAnterior
  FROM TB_USUARIO_ROL
  WHERE IdUsuario = p_IdUsuario
  LIMIT 1;

  -- Siempre debe quedar al menos un administrador activo
  IF v_RolAnterior = 1 AND p_IdRol <> 1
     AND FN_Usuario_OtrosAdministradoresActivos(p_IdUsuario) = 0 THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Debe quedar al menos un administrador activo. Asigne el rol de Administrador a otra persona antes de cambiar este.';
  END IF;


  START TRANSACTION;

  DELETE FROM TB_USUARIO_ROL WHERE IdUsuario = p_IdUsuario;

  INSERT INTO TB_USUARIO_ROL (IdUsuario, IdRol)
  VALUES (p_IdUsuario, p_IdRol);

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_USUARIO', p_IdUsuario, 'Editar',
          JSON_OBJECT('IdRol', v_RolAnterior),
          JSON_OBJECT('IdRol', p_IdRol));

  COMMIT;
END$$
DELIMITER ;


-- Habilita el portal a un encargado que ya existe. Nombre y correo salen de
-- TB_ENCARGADO, no se vuelven a pedir.
DROP PROCEDURE IF EXISTS SP_UsuarioExterno_Crear;
DELIMITER $$
CREATE PROCEDURE SP_UsuarioExterno_Crear(
  IN p_IdUsuarioAccion INT,
  IN p_IdEncargado INT
)
BEGIN
  DECLARE v_IdUsuario INT;
  DECLARE v_Nombre VARCHAR(150) DEFAULT NULL;
  DECLARE v_Correo VARCHAR(150) DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT TRIM(CONCAT(Nombre, ' ', PrimerApellido, ' ', COALESCE(SegundoApellido, ''))), Correo
    INTO v_Nombre, v_Correo
  FROM TB_ENCARGADO
  WHERE IdEncargado = p_IdEncargado
    AND Activo = 1;

  IF v_Nombre IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado no existe o esta inactivo.';
  END IF;

  IF v_Correo IS NULL OR TRIM(v_Correo) = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado no tiene correo registrado.';
  END IF;

  IF EXISTS (SELECT 1 FROM TB_USUARIO WHERE IdEncargado = p_IdEncargado) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese encargado ya tiene acceso al portal.';
  END IF;

  IF EXISTS (SELECT 1 FROM TB_USUARIO WHERE Correo = v_Correo) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe un usuario con ese correo.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_USUARIO (IdEncargado, IdEstadoUsuario, NombreCompleto, Correo, ContrasenaHash)
  VALUES (p_IdEncargado, 4, v_Nombre, v_Correo, 'PENDIENTE_ACTIVACION');

  SET v_IdUsuario = LAST_INSERT_ID();

  INSERT INTO TB_USUARIO_ROL (IdUsuario, IdRol)
  VALUES (v_IdUsuario, 4);

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_USUARIO', v_IdUsuario, 'Crear',
          JSON_OBJECT('NombreCompleto', v_Nombre,
                      'Correo', v_Correo,
                      'IdEncargado', p_IdEncargado,
                      'IdRol', 4,
                      'IdEstadoUsuario', 4));

  COMMIT;

  SELECT v_IdUsuario AS IdUsuario;
END$$
DELIMITER ;


-- Quita el acceso al portal sin tocar al encargado, que sigue siendo cliente.
DROP PROCEDURE IF EXISTS SP_UsuarioExterno_SuspenderAcceso;
DELIMITER $$
CREATE PROCEDURE SP_UsuarioExterno_SuspenderAcceso(
  IN p_IdUsuarioAccion INT,
  IN p_IdUsuario INT
)
BEGIN
  DECLARE v_IdEncargado INT DEFAULT NULL;
  DECLARE v_EstadoAnterior INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdEncargado, IdEstadoUsuario INTO v_IdEncargado, v_EstadoAnterior
  FROM TB_USUARIO
  WHERE IdUsuario = p_IdUsuario;

  IF v_EstadoAnterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El usuario indicado no existe.';
  END IF;

  IF v_IdEncargado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese usuario no es del portal de encargados.';
  END IF;

  START TRANSACTION;

  UPDATE TB_USUARIO
  SET IdEstadoUsuario = 2,
      FechaModificacion = NOW()
  WHERE IdUsuario = p_IdUsuario;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_USUARIO', p_IdUsuario, 'CambiarEstado',
          JSON_OBJECT('IdEstadoUsuario', v_EstadoAnterior),
          JSON_OBJECT('IdEstadoUsuario', 2, 'Motivo', 'Acceso al portal suspendido'));

  COMMIT;
END$$
DELIMITER ;


-- Guarda el token de invitacion o de recuperacion. El texto del token lo genera
-- la aplicacion, no la base. Al generar uno nuevo, los anteriores que siguieran
-- vivos quedan usados, para que solo el ultimo enlace funcione.
DROP PROCEDURE IF EXISTS SP_Token_Generar;
DELIMITER $$
CREATE PROCEDURE SP_Token_Generar(
  IN p_IdUsuario INT,
  IN p_Token VARCHAR(255),
  IN p_HorasVigencia INT
)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF NOT EXISTS (SELECT 1 FROM TB_USUARIO WHERE IdUsuario = p_IdUsuario) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El usuario indicado no existe.';
  END IF;

  SET p_HorasVigencia = IFNULL(NULLIF(p_HorasVigencia, 0), 1);

  START TRANSACTION;

  UPDATE TB_TOKEN_RECUPERACION
  SET Usado = 1
  WHERE IdUsuario = p_IdUsuario
    AND Usado = 0;

  INSERT INTO TB_TOKEN_RECUPERACION (IdUsuario, Token, FechaExpiracion)
  VALUES (p_IdUsuario, p_Token, NOW() + INTERVAL p_HorasVigencia HOUR);

  COMMIT;

  SELECT LAST_INSERT_ID() AS IdToken;
END$$
DELIMITER ;


-- Consume el token y fija la contrasena. Es el unico camino por el que un
-- usuario bloqueado vuelve a quedar activo.
DROP PROCEDURE IF EXISTS SP_Token_Consumir;
DELIMITER $$
CREATE PROCEDURE SP_Token_Consumir(
  IN p_Token VARCHAR(255),
  IN p_ContrasenaHash VARCHAR(255)
)
BEGIN
  DECLARE v_IdToken INT DEFAULT NULL;
  DECLARE v_IdUsuario INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF p_ContrasenaHash IS NULL OR TRIM(p_ContrasenaHash) = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Falta la contrasena nueva.';
  END IF;

  SELECT IdToken, IdUsuario INTO v_IdToken, v_IdUsuario
  FROM TB_TOKEN_RECUPERACION
  WHERE Token = p_Token
    AND Usado = 0
    AND FechaExpiracion > NOW()
  LIMIT 1;

  IF v_IdToken IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El enlace no es valido, ya se uso o vencio.';
  END IF;

  START TRANSACTION;

  UPDATE TB_TOKEN_RECUPERACION
  SET Usado = 1
  WHERE IdToken = v_IdToken;

  UPDATE TB_USUARIO
  SET ContrasenaHash = p_ContrasenaHash,
      IdEstadoUsuario = 1,
      IntentosFallidos = 0,
      FechaModificacion = NOW()
  WHERE IdUsuario = v_IdUsuario;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (v_IdUsuario, 'TB_USUARIO', v_IdUsuario, 'Editar',
          JSON_OBJECT('Accion', 'Contrasena definida', 'IdEstadoUsuario', 1));

  COMMIT;

  SELECT v_IdUsuario AS IdUsuario;
END$$
DELIMITER ;


-- Encargados que pueden tener portal, con el estado de su invitacion.
-- Los que no tienen correo salen igual, para que se vea por que no se pueden
-- invitar todavia.
DROP PROCEDURE IF EXISTS SP_UsuarioExterno_Listar;
DELIMITER $$
CREATE PROCEDURE SP_UsuarioExterno_Listar(
  IN p_Busqueda VARCHAR(150)
)
BEGIN
  SELECT
    e.IdEncargado,
    TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido, ' ', COALESCE(e.SegundoApellido, ''))) AS Encargado,
    e.Correo,
    est.Estudiante,
    u.IdUsuario,
    CASE
      WHEN u.IdUsuario IS NULL AND (e.Correo IS NULL OR TRIM(e.Correo) = '') THEN 'Sin correo'
      WHEN u.IdUsuario IS NULL THEN 'Sin invitar'
      ELSE eu.Nombre
    END AS Estado,
    u.UltimoAcceso
  FROM TB_ENCARGADO e
  LEFT JOIN TB_USUARIO u ON u.IdEncargado = e.IdEncargado
  LEFT JOIN TB_ESTADO_USUARIO eu ON eu.IdEstadoUsuario = u.IdEstadoUsuario
  LEFT JOIN (
    SELECT ee.IdEncargado,
           TRIM(CONCAT(es.Nombre, ' ', es.PrimerApellido)) AS Estudiante
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_ESTUDIANTE es ON es.IdEstudiante = ee.IdEstudiante
    WHERE ee.EsPrincipal = 1
  ) est ON est.IdEncargado = e.IdEncargado
  WHERE e.Activo = 1
    AND (p_Busqueda IS NULL OR p_Busqueda = ''
         OR e.Nombre LIKE CONCAT('%', p_Busqueda, '%')
         OR e.PrimerApellido LIKE CONCAT('%', p_Busqueda, '%')
         OR e.Correo LIKE CONCAT('%', p_Busqueda, '%'))
  ORDER BY e.Nombre, e.PrimerApellido;
END$$
DELIMITER ;


-- ============================================================
-- 2. Clientes (encargados)
-- ============================================================

-- Alta completa del encargado con su telefono principal.
DROP PROCEDURE IF EXISTS SP_Encargado_Crear;
DELIMITER $$
CREATE PROCEDURE SP_Encargado_Crear(
  IN p_IdUsuarioAccion INT,
  IN p_Nombre VARCHAR(100),
  IN p_PrimerApellido VARCHAR(100),
  IN p_SegundoApellido VARCHAR(100),
  IN p_Identificacion VARCHAR(30),
  IN p_Correo VARCHAR(150),
  IN p_Observaciones VARCHAR(1000),
  IN p_IdEstadoCliente INT,
  IN p_IdServicioInteres INT,
  IN p_Telefono VARCHAR(15),
  IN p_IdTipoTelefono INT
)
BEGIN
  DECLARE v_IdEncargado INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  START TRANSACTION;

  INSERT INTO TB_ENCARGADO (IdEstadoCliente, IdServicioInteres, Nombre, PrimerApellido,
                            SegundoApellido, Identificacion, Correo, Observaciones)
  VALUES (IFNULL(p_IdEstadoCliente, 1), p_IdServicioInteres, p_Nombre, p_PrimerApellido,
          p_SegundoApellido, p_Identificacion, p_Correo, p_Observaciones);

  SET v_IdEncargado = LAST_INSERT_ID();

  IF p_Telefono IS NOT NULL AND TRIM(p_Telefono) <> '' THEN
    INSERT INTO TB_TELEFONO (IdEncargado, IdTipoTelefono, Numero, EsPrincipal)
    VALUES (v_IdEncargado, IFNULL(p_IdTipoTelefono, 1), p_Telefono, 1);
  END IF;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ENCARGADO', v_IdEncargado, 'Crear',
          JSON_OBJECT('Nombre', p_Nombre, 'PrimerApellido', p_PrimerApellido,
                      'Correo', p_Correo, 'IdEstadoCliente', IFNULL(p_IdEstadoCliente, 1)));

  COMMIT;

  SELECT v_IdEncargado AS IdEncargado;
END$$
DELIMITER ;


-- Edicion de los datos generales. El telefono se maneja aparte.
DROP PROCEDURE IF EXISTS SP_Encargado_Editar;
DELIMITER $$
CREATE PROCEDURE SP_Encargado_Editar(
  IN p_IdUsuarioAccion INT,
  IN p_IdEncargado INT,
  IN p_Nombre VARCHAR(100),
  IN p_PrimerApellido VARCHAR(100),
  IN p_SegundoApellido VARCHAR(100),
  IN p_Identificacion VARCHAR(30),
  IN p_Correo VARCHAR(150),
  IN p_Observaciones VARCHAR(1000),
  IN p_IdServicioInteres INT
)
BEGIN
  DECLARE v_Anterior JSON DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT JSON_OBJECT('Nombre', Nombre, 'PrimerApellido', PrimerApellido,
                     'Correo', Correo, 'IdServicioInteres', IdServicioInteres)
    INTO v_Anterior
  FROM TB_ENCARGADO
  WHERE IdEncargado = p_IdEncargado;

  IF v_Anterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado indicado no existe.';
  END IF;

  START TRANSACTION;

  UPDATE TB_ENCARGADO
  SET Nombre = p_Nombre,
      PrimerApellido = p_PrimerApellido,
      SegundoApellido = p_SegundoApellido,
      Identificacion = p_Identificacion,
      Correo = p_Correo,
      Observaciones = p_Observaciones,
      IdServicioInteres = p_IdServicioInteres,
      FechaModificacion = NOW()
  WHERE IdEncargado = p_IdEncargado;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ENCARGADO', p_IdEncargado, 'Editar', v_Anterior,
          JSON_OBJECT('Nombre', p_Nombre, 'PrimerApellido', p_PrimerApellido,
                      'Correo', p_Correo, 'IdServicioInteres', p_IdServicioInteres));

  COMMIT;
END$$
DELIMITER ;


-- Provisional: se acepta cualquier movimiento en el embudo. Si el equipo define
-- que hay pasos que no se pueden saltar, la regla va aqui.
DROP PROCEDURE IF EXISTS SP_Encargado_CambiarEstado;
DELIMITER $$
CREATE PROCEDURE SP_Encargado_CambiarEstado(
  IN p_IdUsuarioAccion INT,
  IN p_IdEncargado INT,
  IN p_IdEstadoCliente INT
)
BEGIN
  DECLARE v_Anterior INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdEstadoCliente INTO v_Anterior FROM TB_ENCARGADO WHERE IdEncargado = p_IdEncargado;

  IF v_Anterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado indicado no existe.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTADO_CLIENTE WHERE IdEstadoCliente = p_IdEstadoCliente) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estado indicado no existe.';
  END IF;

  START TRANSACTION;

  UPDATE TB_ENCARGADO
  SET IdEstadoCliente = p_IdEstadoCliente,
      FechaModificacion = NOW()
  WHERE IdEncargado = p_IdEncargado;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ENCARGADO', p_IdEncargado, 'CambiarEstado',
          JSON_OBJECT('IdEstadoCliente', v_Anterior),
          JSON_OBJECT('IdEstadoCliente', p_IdEstadoCliente));

  COMMIT;
END$$
DELIMITER ;


-- Baja logica. El encargado no se borra nunca: queda el historial.
DROP PROCEDURE IF EXISTS SP_Encargado_Eliminar;
DELIMITER $$
CREATE PROCEDURE SP_Encargado_Eliminar(
  IN p_IdUsuarioAccion INT,
  IN p_IdEncargado INT
)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF NOT EXISTS (SELECT 1 FROM TB_ENCARGADO WHERE IdEncargado = p_IdEncargado) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado indicado no existe.';
  END IF;

  START TRANSACTION;

  UPDATE TB_ENCARGADO
  SET Activo = 0,
      FechaModificacion = NOW()
  WHERE IdEncargado = p_IdEncargado;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ENCARGADO', p_IdEncargado, 'Eliminar',
          JSON_OBJECT('Activo', 0));

  COMMIT;
END$$
DELIMITER ;


-- Devuelve tres resultados: datos del encargado, sus telefonos y sus estudiantes.
-- Desde Dapper se leen con QueryMultiple.
DROP PROCEDURE IF EXISTS SP_Encargado_ObtenerDetalle;
DELIMITER $$
CREATE PROCEDURE SP_Encargado_ObtenerDetalle(
  IN p_IdEncargado INT
)
BEGIN
  SELECT e.IdEncargado, e.Nombre, e.PrimerApellido, e.SegundoApellido, e.Identificacion,
         e.Correo, e.Observaciones, e.IdEstadoCliente, ec.Nombre AS EstadoCliente,
         e.IdServicioInteres, s.Nombre AS ServicioInteres, e.FechaRegistro, e.Activo
  FROM TB_ENCARGADO e
  JOIN TB_ESTADO_CLIENTE ec ON ec.IdEstadoCliente = e.IdEstadoCliente
  LEFT JOIN TB_SERVICIO s ON s.IdServicio = e.IdServicioInteres
  WHERE e.IdEncargado = p_IdEncargado;

  SELECT t.IdTelefono, t.Numero, t.CodigoPais, t.EsPrincipal, tt.Nombre AS TipoTelefono
  FROM TB_TELEFONO t
  JOIN TB_TIPO_TELEFONO tt ON tt.IdTipoTelefono = t.IdTipoTelefono
  WHERE t.IdEncargado = p_IdEncargado
  ORDER BY t.EsPrincipal DESC, t.IdTelefono;

  SELECT es.IdEstudiante,
         TRIM(CONCAT(es.Nombre, ' ', es.PrimerApellido)) AS Estudiante,
         p.Nombre AS Parentesco,
         ee.EsPrincipal
  FROM TB_ESTUDIANTE_ENCARGADO ee
  JOIN TB_ESTUDIANTE es ON es.IdEstudiante = ee.IdEstudiante
  JOIN TB_PARENTESCO p ON p.IdParentesco = ee.IdParentesco
  WHERE ee.IdEncargado = p_IdEncargado
  ORDER BY ee.EsPrincipal DESC, es.Nombre;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS SP_ConsultarClientes_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ConsultarClientes_CRM()
BEGIN
    SELECT
      e.IdEncargado AS Id,
      CONCAT(e.Nombre, ' ', e.PrimerApellido) AS Encargado,
      t.Numero AS Telefono,
      e.Correo AS Correo,
      s.Nombre AS Servicio,
      CONCAT(est.Nombre, ' ', est.PrimerApellido) AS Estudiante,
      (SELECT COUNT(*)
       FROM TB_ESTUDIANTE_ENCARGADO ec2
       WHERE ec2.IdEncargado = e.IdEncargado) AS CantidadEstudiantes,
      IF(e.Activo = 1, ec.Nombre, 'Inactivo') AS Estado,
      IF(e.Activo = 1, e.IdEstadoCliente, 5) AS IdEstadoCliente
    FROM TB_ENCARGADO e
    JOIN TB_ESTADO_CLIENTE ec ON ec.IdEstadoCliente = e.IdEstadoCliente
    LEFT JOIN TB_SERVICIO s ON s.IdServicio = e.IdServicioInteres
    LEFT JOIN TB_TELEFONO t ON t.IdEncargado = e.IdEncargado AND t.EsPrincipal = 1
    LEFT JOIN TB_ESTUDIANTE est ON est.IdEstudiante = (
        SELECT ee.IdEstudiante
        FROM TB_ESTUDIANTE_ENCARGADO ee
        WHERE ee.IdEncargado = e.IdEncargado
        ORDER BY ee.EsPrincipal DESC, ee.IdEstudiante
        LIMIT 1)
    -- Orden del flujo de atencion (TB_ESTADO_CLIENTE.Orden): Nuevo, Contactado,
    -- Cita agendada, Activo. Los encargados desactivados van siempre al final,
    -- conserven el estado que conserven. Dentro de cada estado, los mas recientes primero.
    ORDER BY
      e.Activo DESC,
      ec.Orden,
      e.FechaRegistro DESC;
END$$
DELIMITER ;


-- p_EstudiantesJson, ejemplo:
-- [{"Nombre":"Ana","Apellido":"Mora","IdParentesco":1,"FechaNacimiento":"2015-04-10","IdNivelEducativo":2},
--  {"Nombre":"Luis","Apellido":"Mora","IdParentesco":1,"FechaNacimiento":null,"IdNivelEducativo":null}]
DROP PROCEDURE IF EXISTS SP_RegistrarCliente_CRM;
DELIMITER $$
CREATE PROCEDURE SP_RegistrarCliente_CRM(
    IN p_IdUsuarioAccion   INT,
    IN p_NombreEncargado   VARCHAR(100),
    IN p_ApellidoEncargado VARCHAR(100),
    IN p_Telefono          VARCHAR(15),
    IN p_Correo            VARCHAR(150),
    IN p_IdServicioInteres INT,
    IN p_Observaciones     VARCHAR(1000),
    IN p_EstudiantesJson   JSON,
    IN p_IdSolicitud       INT          -- NULL = registro normal; con valor = conversion de solicitud
)
BEGIN
    DECLARE v_IdEncargado      INT;
    DECLARE v_IdEstudiante     INT;
    DECLARE v_Total            INT DEFAULT 0;
    DECLARE v_Fila             INT DEFAULT 1;
    DECLARE v_Nombre           VARCHAR(100);
    DECLARE v_Apellido         VARCHAR(100);
    DECLARE v_FechaNacimiento  DATE;
    DECLARE v_IdNivelEducativo INT;
    DECLARE v_IdParentesco     INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_Total = IFNULL(JSON_LENGTH(p_EstudiantesJson), 0);

    IF v_Total = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Debe agregar al menos un estudiante.';
    END IF;

    IF p_IdSolicitud IS NOT NULL AND NOT EXISTS (
        SELECT 1 FROM TB_SOLICITUD_CONTACTO
        WHERE IdSolicitud = p_IdSolicitud AND IdEstadoSolicitud <> 3) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La solicitud no existe o ya fue convertida.';
    END IF;

    START TRANSACTION;

    -- IdEstadoCliente = 1 ("Nuevo") siempre al registrar
    INSERT INTO TB_ENCARGADO (IdEstadoCliente, IdServicioInteres, Nombre, PrimerApellido, Correo, Observaciones)
    VALUES (1, p_IdServicioInteres, TRIM(p_NombreEncargado), TRIM(p_ApellidoEncargado),
            NULLIF(TRIM(p_Correo), ''), NULLIF(TRIM(p_Observaciones), ''));

    SET v_IdEncargado = LAST_INSERT_ID();

    INSERT INTO TB_TELEFONO (IdEncargado, IdTipoTelefono, Numero, EsPrincipal)
    VALUES (v_IdEncargado, 1, p_Telefono, 1);

    WHILE v_Fila <= v_Total DO

        SELECT j.Nombre, j.Apellido, j.FechaNacimiento, j.IdNivelEducativo, j.IdParentesco
          INTO v_Nombre, v_Apellido, v_FechaNacimiento, v_IdNivelEducativo, v_IdParentesco
        FROM JSON_TABLE(p_EstudiantesJson, '$[*]' COLUMNS (
            Fila             FOR ORDINALITY,
            Nombre           VARCHAR(100) PATH '$.Nombre',
            Apellido         VARCHAR(100) PATH '$.Apellido',
            FechaNacimiento  DATE         PATH '$.FechaNacimiento'  NULL ON EMPTY NULL ON ERROR,
            IdNivelEducativo INT          PATH '$.IdNivelEducativo' NULL ON EMPTY NULL ON ERROR,
            IdParentesco     INT          PATH '$.IdParentesco'     NULL ON EMPTY NULL ON ERROR
        )) AS j
        WHERE j.Fila = v_Fila;

        INSERT INTO TB_ESTUDIANTE (IdNivelEducativo, Nombre, PrimerApellido, FechaNacimiento)
        VALUES (v_IdNivelEducativo, TRIM(v_Nombre), TRIM(v_Apellido), v_FechaNacimiento);

        SET v_IdEstudiante = LAST_INSERT_ID();

        INSERT INTO TB_ESTUDIANTE_ENCARGADO (IdEstudiante, IdEncargado, IdParentesco, EsPrincipal)
        VALUES (v_IdEstudiante, v_IdEncargado, IFNULL(v_IdParentesco, 3), 1);

        SET v_Fila = v_Fila + 1;
    END WHILE;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_ENCARGADO', v_IdEncargado, 'Crear',
            JSON_OBJECT('Nombre', p_NombreEncargado, 'PrimerApellido', p_ApellidoEncargado,
                        'Correo', p_Correo, 'IdEstadoCliente', 1,
                        'CantidadEstudiantes', v_Total, 'IdSolicitud', p_IdSolicitud));

    -- Conversion: la solicitud queda enlazada al cliente creado
    IF p_IdSolicitud IS NOT NULL THEN

        UPDATE TB_SOLICITUD_CONTACTO
        SET IdEstadoSolicitud = 3,
            IdEncargado = v_IdEncargado,
            IdUsuarioAtiende = p_IdUsuarioAccion,
            FechaAtencion = NOW()
        WHERE IdSolicitud = p_IdSolicitud
          AND IdEstadoSolicitud <> 3;

        -- Si otra persona la convirtio al mismo tiempo, se revierte todo
        IF ROW_COUNT() = 0 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La solicitud ya fue convertida por otro usuario.';
        END IF;

        INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
        VALUES (p_IdUsuarioAccion, 'TB_SOLICITUD_CONTACTO', p_IdSolicitud, 'CambiarEstado',
                JSON_OBJECT('IdEstadoSolicitud', 3, 'IdEncargado', v_IdEncargado));
    END IF;

    COMMIT;

    SELECT v_IdEncargado AS IdEncargado;
END$$
DELIMITER ;

-- Desactiva los estudiantes del encargado que ya no tengan ningun otro
-- encargado activo. Se llama DESPUES de poner Activo = 0 al encargado.
DROP PROCEDURE IF EXISTS SP_DesactivarEstudiantesEncargado_CRM;
DELIMITER $$
CREATE PROCEDURE SP_DesactivarEstudiantesEncargado_CRM(
    IN p_IdEncargado INT
)
BEGIN
    UPDATE TB_ESTUDIANTE es
    SET es.Activo = 0,
        es.FechaModificacion = NOW()
    WHERE es.Activo = 1
      AND es.IdEstudiante IN (
          SELECT ee.IdEstudiante
          FROM TB_ESTUDIANTE_ENCARGADO ee
          WHERE ee.IdEncargado = p_IdEncargado)
      AND NOT EXISTS (
          SELECT 1
          FROM TB_ESTUDIANTE_ENCARGADO ee2
          JOIN TB_ENCARGADO e2 ON e2.IdEncargado = ee2.IdEncargado
          WHERE ee2.IdEstudiante = es.IdEstudiante
            AND e2.Activo = 1);
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_DesactivarCliente_CRM;
DELIMITER $$
CREATE PROCEDURE SP_DesactivarCliente_CRM(
    IN p_IdEncargado INT
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- 5 = "Inactivo" en TB_ESTADO_CLIENTE
    UPDATE TB_ENCARGADO
    SET Activo = 0,
        IdEstadoCliente = 5,
        FechaModificacion = NOW()
    WHERE IdEncargado = p_IdEncargado;

    CALL SP_DesactivarEstudiantesEncargado_CRM(p_IdEncargado);

    COMMIT;
END$$
DELIMITER ;

-- ============================================================
-- NUEVOS SPs PARA EDICION Y DETALLE DE CLIENTES
-- ============================================================

DROP PROCEDURE IF EXISTS SP_ObtenerCliente_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ObtenerCliente_CRM(
    IN p_IdEncargado INT
)
BEGIN
    -- 1) Datos del encargado
    SELECT
      e.IdEncargado AS Id,
      e.Nombre AS NombreEncargado,
      e.PrimerApellido AS ApellidoEncargado,
      t.Numero AS Telefono,
      e.Correo,
      e.Observaciones,
      e.IdServicioInteres,
      s.Nombre AS Servicio,
      IF(e.Activo = 1, e.IdEstadoCliente, 5) AS IdEstadoCliente,
      IF(e.Activo = 1, ec.Nombre, 'Inactivo') AS Estado,
      e.Activo,
      e.FechaRegistro
    FROM TB_ENCARGADO e
    JOIN TB_ESTADO_CLIENTE ec ON ec.IdEstadoCliente = e.IdEstadoCliente
    LEFT JOIN TB_SERVICIO s ON s.IdServicio = e.IdServicioInteres
    LEFT JOIN TB_TELEFONO t ON t.IdEncargado = e.IdEncargado AND t.EsPrincipal = 1
    WHERE e.IdEncargado = p_IdEncargado;

    -- 2) Estudiantes a cargo
    SELECT
      es.IdEstudiante,
      es.Nombre,
      es.PrimerApellido AS Apellido,
      es.FechaNacimiento,
      TIMESTAMPDIFF(YEAR, es.FechaNacimiento, CURDATE()) AS Edad,
      es.IdNivelEducativo,
      ne.Nombre AS NivelEducativo,
      ee.IdParentesco,
      p.Nombre AS Parentesco,
      es.Activo
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_ESTUDIANTE es ON es.IdEstudiante = ee.IdEstudiante
    JOIN TB_PARENTESCO p ON p.IdParentesco = ee.IdParentesco
    LEFT JOIN TB_NIVEL_EDUCATIVO ne ON ne.IdNivelEducativo = es.IdNivelEducativo
    WHERE ee.IdEncargado = p_IdEncargado
    ORDER BY es.Activo DESC, es.Nombre, es.PrimerApellido;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_EditarCliente_CRM;
DELIMITER $$
CREATE PROCEDURE SP_EditarCliente_CRM(
    IN p_IdUsuarioAccion   INT,
    IN p_IdEncargado       INT,
    IN p_NombreEncargado   VARCHAR(100),
    IN p_ApellidoEncargado VARCHAR(100),
    IN p_Telefono          VARCHAR(15),
    IN p_Correo            VARCHAR(150),
    IN p_IdServicioInteres INT,
    IN p_IdEstadoCliente   INT,
    IN p_Observaciones     VARCHAR(1000)
)
BEGIN
    DECLARE v_Anterior JSON DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT JSON_OBJECT('Nombre', Nombre, 'PrimerApellido', PrimerApellido, 'Correo', Correo,
                       'IdServicioInteres', IdServicioInteres, 'IdEstadoCliente', IdEstadoCliente,
                       'Activo', Activo)
      INTO v_Anterior
    FROM TB_ENCARGADO
    WHERE IdEncargado = p_IdEncargado;

    IF v_Anterior IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cliente indicado no existe.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_ESTADO_CLIENTE WHERE IdEstadoCliente = p_IdEstadoCliente) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estado indicado no existe.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_SERVICIO WHERE IdServicio = p_IdServicioInteres) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El servicio indicado no existe.';
    END IF;

    START TRANSACTION;

    -- 5 = "Inactivo": desactiva al cliente. Cualquier otro estado lo reactiva.
    UPDATE TB_ENCARGADO
    SET Nombre = TRIM(p_NombreEncargado),
        PrimerApellido = TRIM(p_ApellidoEncargado),
        Correo = NULLIF(TRIM(p_Correo), ''),
        Observaciones = NULLIF(TRIM(p_Observaciones), ''),
        IdServicioInteres = p_IdServicioInteres,
        IdEstadoCliente = p_IdEstadoCliente,
        Activo = IF(p_IdEstadoCliente = 5, 0, 1),
        FechaModificacion = NOW()
    WHERE IdEncargado = p_IdEncargado;

    -- Telefono principal: se actualiza, o se crea si el cliente no tenia
    IF EXISTS (SELECT 1 FROM TB_TELEFONO WHERE IdEncargado = p_IdEncargado AND EsPrincipal = 1) THEN
        UPDATE TB_TELEFONO
        SET Numero = p_Telefono
        WHERE IdEncargado = p_IdEncargado AND EsPrincipal = 1;
    ELSE
        INSERT INTO TB_TELEFONO (IdEncargado, IdTipoTelefono, Numero, EsPrincipal)
        VALUES (p_IdEncargado, 1, p_Telefono, 1);
    END IF;

    -- Estudiantes: se desactivan con el cliente (si no tienen otro encargado activo)
    -- y se reactivan si el cliente estaba inactivo.
    IF p_IdEstadoCliente = 5 THEN
        CALL SP_DesactivarEstudiantesEncargado_CRM(p_IdEncargado);
    ELSEIF JSON_EXTRACT(v_Anterior, '$.Activo') = 0 THEN
        UPDATE TB_ESTUDIANTE
        SET Activo = 1,
            FechaModificacion = NOW()
        WHERE Activo = 0
          AND IdEstudiante IN (
              SELECT ee.IdEstudiante
              FROM TB_ESTUDIANTE_ENCARGADO ee
              WHERE ee.IdEncargado = p_IdEncargado);
    END IF;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_ENCARGADO', p_IdEncargado, 'Editar', v_Anterior,
            JSON_OBJECT('Nombre', p_NombreEncargado, 'PrimerApellido', p_ApellidoEncargado,
                        'Correo', p_Correo, 'IdServicioInteres', p_IdServicioInteres,
                        'IdEstadoCliente', p_IdEstadoCliente,
                        'Activo', IF(p_IdEstadoCliente = 5, 0, 1)));

    COMMIT;
END$$
DELIMITER ;

-- Edita un estudiante desde el detalle del cliente. Solo toca los campos que
-- maneja ese formulario; el resto de la ficha (institucion, observaciones,
-- necesidades de apoyo) queda igual.
DROP PROCEDURE IF EXISTS SP_EditarEstudianteCliente_CRM;
DELIMITER $$
CREATE PROCEDURE SP_EditarEstudianteCliente_CRM(
    IN p_IdUsuarioAccion  INT,
    IN p_IdEncargado      INT,
    IN p_IdEstudiante     INT,
    IN p_Nombre           VARCHAR(100),
    IN p_Apellido         VARCHAR(100),
    IN p_FechaNacimiento  DATE,
    IN p_IdNivelEducativo INT,
    IN p_IdParentesco     INT
)
BEGIN
    DECLARE v_Anterior JSON DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO
                   WHERE IdEstudiante = p_IdEstudiante AND IdEncargado = p_IdEncargado) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante no pertenece a este cliente.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_PARENTESCO WHERE IdParentesco = p_IdParentesco) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El parentesco indicado no existe.';
    END IF;

    SELECT JSON_OBJECT('Nombre', es.Nombre, 'PrimerApellido', es.PrimerApellido,
                       'FechaNacimiento', es.FechaNacimiento,
                       'IdNivelEducativo', es.IdNivelEducativo, 'IdParentesco', ee.IdParentesco)
      INTO v_Anterior
    FROM TB_ESTUDIANTE es
    JOIN TB_ESTUDIANTE_ENCARGADO ee ON ee.IdEstudiante = es.IdEstudiante
    WHERE es.IdEstudiante = p_IdEstudiante AND ee.IdEncargado = p_IdEncargado;

    START TRANSACTION;

    UPDATE TB_ESTUDIANTE
    SET Nombre = TRIM(p_Nombre),
        PrimerApellido = TRIM(p_Apellido),
        FechaNacimiento = p_FechaNacimiento,
        IdNivelEducativo = p_IdNivelEducativo,
        FechaModificacion = NOW()
    WHERE IdEstudiante = p_IdEstudiante;

    UPDATE TB_ESTUDIANTE_ENCARGADO
    SET IdParentesco = p_IdParentesco
    WHERE IdEstudiante = p_IdEstudiante AND IdEncargado = p_IdEncargado;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_ESTUDIANTE', p_IdEstudiante, 'Editar', v_Anterior,
            JSON_OBJECT('Nombre', p_Nombre, 'PrimerApellido', p_Apellido,
                        'FechaNacimiento', p_FechaNacimiento,
                        'IdNivelEducativo', p_IdNivelEducativo, 'IdParentesco', p_IdParentesco));

    COMMIT;
END$$
DELIMITER ;


-- Activa o desactiva un estudiante individual. No deja activar un estudiante
-- que no tenga ningun encargado activo.
DROP PROCEDURE IF EXISTS SP_CambiarEstadoEstudiante_CRM;
DELIMITER $$
CREATE PROCEDURE SP_CambiarEstadoEstudiante_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdEncargado     INT,
    IN p_IdEstudiante    INT,
    IN p_Activo          BOOLEAN
)
BEGIN
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO
                   WHERE IdEstudiante = p_IdEstudiante AND IdEncargado = p_IdEncargado) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante no pertenece a este cliente.';
    END IF;

    IF p_Activo = 1 AND NOT EXISTS (
        SELECT 1
        FROM TB_ESTUDIANTE_ENCARGADO ee
        JOIN TB_ENCARGADO e ON e.IdEncargado = ee.IdEncargado
        WHERE ee.IdEstudiante = p_IdEstudiante AND e.Activo = 1) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'No se puede activar el estudiante porque su encargado esta inactivo.';
    END IF;

    START TRANSACTION;

    UPDATE TB_ESTUDIANTE
    SET Activo = p_Activo,
        FechaModificacion = NOW()
    WHERE IdEstudiante = p_IdEstudiante;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_ESTUDIANTE', p_IdEstudiante, 'CambiarEstado',
            JSON_OBJECT('Activo', p_Activo));

    COMMIT;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_ListarParentescos_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ListarParentescos_CRM()
BEGIN
    SELECT IdParentesco, Nombre
    FROM TB_PARENTESCO
    ORDER BY IdParentesco;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_ListarNivelesEducativos_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ListarNivelesEducativos_CRM()
BEGIN
    SELECT IdNivelEducativo, Nombre
    FROM TB_NIVEL_EDUCATIVO
    ORDER BY Orden;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_ListarEstadosCliente_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ListarEstadosCliente_CRM()
BEGIN
    SELECT IdEstadoCliente, Nombre
    FROM TB_ESTADO_CLIENTE
    ORDER BY Orden;
END$$
DELIMITER ;

-- ============================================================
-- 3. Estudiantes
-- ============================================================

-- Alta del estudiante con su encargado principal y sus areas de dificultad.
-- Las areas llegan como texto JSON, por ejemplo '[1,3,5]'. Si viene NULL o
-- vacio, simplemente no se registra ninguna.
DROP PROCEDURE IF EXISTS SP_Estudiante_Crear;
DELIMITER $$
CREATE PROCEDURE SP_Estudiante_Crear(
  IN p_IdUsuarioAccion INT,
  IN p_Nombre VARCHAR(100),
  IN p_PrimerApellido VARCHAR(100),
  IN p_SegundoApellido VARCHAR(100),
  IN p_FechaNacimiento DATE,
  IN p_IdNivelEducativo INT,
  IN p_IdInstitucion INT,
  IN p_Observaciones VARCHAR(1000),
  IN p_NecesidadesApoyo VARCHAR(1000),
  IN p_IdEncargado INT,
  IN p_IdParentesco INT,
  IN p_AreasJson VARCHAR(255)
)
BEGIN
  DECLARE v_IdEstudiante INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF NOT EXISTS (SELECT 1 FROM TB_ENCARGADO WHERE IdEncargado = p_IdEncargado AND Activo = 1) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado indicado no existe o esta inactivo.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_ESTUDIANTE (IdNivelEducativo, IdInstitucion, Nombre, PrimerApellido,
                             SegundoApellido, FechaNacimiento, Observaciones, NecesidadesApoyo)
  VALUES (p_IdNivelEducativo, p_IdInstitucion, p_Nombre, p_PrimerApellido,
          p_SegundoApellido, p_FechaNacimiento, p_Observaciones, p_NecesidadesApoyo);

  SET v_IdEstudiante = LAST_INSERT_ID();

  INSERT INTO TB_ESTUDIANTE_ENCARGADO (IdEstudiante, IdEncargado, IdParentesco, EsPrincipal)
  VALUES (v_IdEstudiante, p_IdEncargado, IFNULL(p_IdParentesco, 3), 1);

  IF p_AreasJson IS NOT NULL AND JSON_LENGTH(p_AreasJson) > 0 THEN
    INSERT INTO TB_ESTUDIANTE_AREA (IdEstudiante, IdAreaDificultad)
    SELECT v_IdEstudiante, a.IdArea
    FROM JSON_TABLE(p_AreasJson, '$[*]' COLUMNS (IdArea INT PATH '$')) AS a;
  END IF;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ESTUDIANTE', v_IdEstudiante, 'Crear',
          JSON_OBJECT('Nombre', p_Nombre, 'PrimerApellido', p_PrimerApellido,
                      'IdEncargadoPrincipal', p_IdEncargado));

  COMMIT;

  SELECT v_IdEstudiante AS IdEstudiante;
END$$
DELIMITER ;


-- Edicion del estudiante. Si viene un encargado principal distinto, se cambia:
-- primero se baja el que estaba, porque el indice unico solo admite uno.
DROP PROCEDURE IF EXISTS SP_Estudiante_Editar;
DELIMITER $$
CREATE PROCEDURE SP_Estudiante_Editar(
  IN p_IdUsuarioAccion INT,
  IN p_IdEstudiante INT,
  IN p_Nombre VARCHAR(100),
  IN p_PrimerApellido VARCHAR(100),
  IN p_SegundoApellido VARCHAR(100),
  IN p_FechaNacimiento DATE,
  IN p_IdNivelEducativo INT,
  IN p_IdInstitucion INT,
  IN p_Observaciones VARCHAR(1000),
  IN p_NecesidadesApoyo VARCHAR(1000),
  IN p_IdEncargadoPrincipal INT
)
BEGIN
  DECLARE v_Anterior JSON DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT JSON_OBJECT('Nombre', Nombre, 'PrimerApellido', PrimerApellido,
                     'IdNivelEducativo', IdNivelEducativo, 'IdInstitucion', IdInstitucion)
    INTO v_Anterior
  FROM TB_ESTUDIANTE
  WHERE IdEstudiante = p_IdEstudiante;

  IF v_Anterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante indicado no existe.';
  END IF;

  IF p_IdEncargadoPrincipal IS NOT NULL
     AND NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO
                     WHERE IdEstudiante = p_IdEstudiante AND IdEncargado = p_IdEncargadoPrincipal) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese encargado no esta asociado al estudiante.';
  END IF;

  START TRANSACTION;

  UPDATE TB_ESTUDIANTE
  SET Nombre = p_Nombre,
      PrimerApellido = p_PrimerApellido,
      SegundoApellido = p_SegundoApellido,
      FechaNacimiento = p_FechaNacimiento,
      IdNivelEducativo = p_IdNivelEducativo,
      IdInstitucion = p_IdInstitucion,
      Observaciones = p_Observaciones,
      NecesidadesApoyo = p_NecesidadesApoyo,
      FechaModificacion = NOW()
  WHERE IdEstudiante = p_IdEstudiante;

  IF p_IdEncargadoPrincipal IS NOT NULL THEN
    UPDATE TB_ESTUDIANTE_ENCARGADO
    SET EsPrincipal = 0
    WHERE IdEstudiante = p_IdEstudiante AND EsPrincipal = 1;

    UPDATE TB_ESTUDIANTE_ENCARGADO
    SET EsPrincipal = 1
    WHERE IdEstudiante = p_IdEstudiante AND IdEncargado = p_IdEncargadoPrincipal;
  END IF;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ESTUDIANTE', p_IdEstudiante, 'Editar', v_Anterior,
          JSON_OBJECT('Nombre', p_Nombre, 'PrimerApellido', p_PrimerApellido,
                      'IdNivelEducativo', p_IdNivelEducativo, 'IdInstitucion', p_IdInstitucion));

  COMMIT;
END$$
DELIMITER ;


-- Baja logica del estudiante.
DROP PROCEDURE IF EXISTS SP_Estudiante_Eliminar;
DELIMITER $$
CREATE PROCEDURE SP_Estudiante_Eliminar(
  IN p_IdUsuarioAccion INT,
  IN p_IdEstudiante INT
)
BEGIN
  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE WHERE IdEstudiante = p_IdEstudiante) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante indicado no existe.';
  END IF;

  START TRANSACTION;

  UPDATE TB_ESTUDIANTE
  SET Activo = 0,
      FechaModificacion = NOW()
  WHERE IdEstudiante = p_IdEstudiante;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ESTUDIANTE', p_IdEstudiante, 'Eliminar',
          JSON_OBJECT('Activo', 0));

  COMMIT;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS SP_ConsultarEstudiantes_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ConsultarEstudiantes_CRM()
BEGIN

     SELECT
        e.IdEstudiante,

        CONCAT(
            e.Nombre,
            ' ',
            e.PrimerApellido,
            ' ',
            COALESCE(e.SegundoApellido, '')
        ) AS Estudiante,

        e.FechaNacimiento,

        ne.Nombre AS NivelEducativo,

        i.Nombre AS Institucion,

        CONCAT(
            en.Nombre,
            ' ',
            en.PrimerApellido
        ) AS Encargado

    FROM TB_ESTUDIANTE e

    LEFT JOIN TB_NIVEL_EDUCATIVO ne
        ON ne.IdNivelEducativo = e.IdNivelEducativo

    LEFT JOIN TB_INSTITUCION i
        ON i.IdInstitucion = e.IdInstitucion

    LEFT JOIN TB_ESTUDIANTE_ENCARGADO ee
        ON ee.IdEstudiante = e.IdEstudiante
        AND ee.EsPrincipal = 1

    LEFT JOIN TB_ENCARGADO en
        ON en.IdEncargado = ee.IdEncargado

    WHERE e.Activo = 1

    ORDER BY e.FechaRegistro DESC;



END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS SP_ConsultarFichaEstudiante_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ConsultarFichaEstudiante_CRM(
    IN p_IdEstudiante INT
)
BEGIN

    SELECT
        e.IdEstudiante,
        e.Nombre,
        e.PrimerApellido,
        e.SegundoApellido,
        e.FechaNacimiento,

        ne.Nombre AS NivelEducativo,

        i.Nombre AS Institucion,

        GROUP_CONCAT(
            DISTINCT ad.Nombre
            ORDER BY ad.Nombre
            SEPARATOR ', '
        ) AS AreasDificultad,

        e.Observaciones,
        e.NecesidadesApoyo

    FROM TB_ESTUDIANTE e

    LEFT JOIN TB_NIVEL_EDUCATIVO ne
        ON ne.IdNivelEducativo = e.IdNivelEducativo

    LEFT JOIN TB_INSTITUCION i
        ON i.IdInstitucion = e.IdInstitucion

    LEFT JOIN TB_ESTUDIANTE_AREA ea
        ON ea.IdEstudiante = e.IdEstudiante

    LEFT JOIN TB_AREA_DIFICULTAD ad
        ON ad.IdAreaDificultad = ea.IdAreaDificultad

    WHERE e.IdEstudiante = p_IdEstudiante
      AND e.Activo = 1

    GROUP BY
        e.IdEstudiante,
        e.Nombre,
        e.PrimerApellido,
        e.SegundoApellido,
        e.FechaNacimiento,
        ne.Nombre,
        i.Nombre,
        e.Observaciones,
        e.NecesidadesApoyo;

END$$
DELIMITER ;

-- ============================================================
-- 4. Agenda y citas
-- ============================================================

-- Ids de TB_ESTADO_CITA: 1 Programada, 2 Confirmada, 3 Completada,
-- 4 Cancelada, 5 No asistio. Las canceladas y las no asistidas no ocupan
-- espacio en la agenda.


-- Agendar. Valida traslape, horario de atencion y dia no laboral.
-- Provisional: se rechaza todo lo que caiga fuera del horario. Si la
-- psicopedagoga necesita agendar excepciones, hay que agregar un parametro
-- para saltarse esa validacion a proposito.
DROP PROCEDURE IF EXISTS SP_Cita_Crear;
DELIMITER $$
CREATE PROCEDURE SP_Cita_Crear(
  IN p_IdUsuarioAccion INT,
  IN p_IdEstudiante INT,
  IN p_IdTipoSesion INT,
  IN p_IdModalidad INT,
  IN p_FechaHoraInicio DATETIME,
  IN p_FechaHoraFin DATETIME,
  IN p_Observaciones VARCHAR(1000)
)
BEGIN
  DECLARE v_IdCita INT;
  DECLARE v_DiaSemana INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF p_FechaHoraFin <= p_FechaHoraInicio THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La hora de fin tiene que ser posterior a la de inicio.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE WHERE IdEstudiante = p_IdEstudiante AND Activo = 1) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante indicado no existe o esta inactivo.';
  END IF;

  IF EXISTS (SELECT 1 FROM TB_DIA_NO_LABORAL WHERE Fecha = DATE(p_FechaHoraInicio)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese dia esta marcado como no laboral.';
  END IF;

  -- WEEKDAY devuelve 0 para lunes; el catalogo usa 1 para lunes
  SET v_DiaSemana = WEEKDAY(p_FechaHoraInicio) + 1;

  IF NOT EXISTS (
    SELECT 1 FROM TB_HORARIO_ATENCION h
    WHERE h.DiaSemana = v_DiaSemana
      AND h.Activo = 1
      AND TIME(p_FechaHoraInicio) >= h.HoraInicio
      AND TIME(p_FechaHoraFin) <= h.HoraFin) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita queda fuera del horario de atencion.';
  END IF;

  IF EXISTS (
    SELECT 1 FROM TB_CITA c
    WHERE c.IdEstadoCita NOT IN (4, 5)
      AND c.FechaHoraInicio < p_FechaHoraFin
      AND p_FechaHoraInicio < c.FechaHoraFin) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita se traslapa con otra ya agendada.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_CITA (IdEstudiante, IdTipoSesion, IdModalidad, IdEstadoCita, IdUsuarioRegistro,
                       FechaHoraInicio, FechaHoraFin, Observaciones)
  VALUES (p_IdEstudiante, p_IdTipoSesion, p_IdModalidad, 1, p_IdUsuarioAccion,
          p_FechaHoraInicio, p_FechaHoraFin, p_Observaciones);

  SET v_IdCita = LAST_INSERT_ID();

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_CITA', v_IdCita, 'Crear',
          JSON_OBJECT('IdEstudiante', p_IdEstudiante,
                      'FechaHoraInicio', p_FechaHoraInicio,
                      'FechaHoraFin', p_FechaHoraFin));

  COMMIT;

  SELECT v_IdCita AS IdCita;
END$$
DELIMITER ;


-- Mover una cita de fecha u hora. Repite las mismas validaciones, sin contarse
-- a si misma en el traslape.
DROP PROCEDURE IF EXISTS SP_Cita_Reprogramar;
DELIMITER $$
CREATE PROCEDURE SP_Cita_Reprogramar(
  IN p_IdUsuarioAccion INT,
  IN p_IdCita INT,
  IN p_FechaHoraInicio DATETIME,
  IN p_FechaHoraFin DATETIME
)
BEGIN
  DECLARE v_Anterior JSON DEFAULT NULL;
  DECLARE v_Estado INT DEFAULT NULL;
  DECLARE v_DiaSemana INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdEstadoCita,
         JSON_OBJECT('FechaHoraInicio', FechaHoraInicio, 'FechaHoraFin', FechaHoraFin)
    INTO v_Estado, v_Anterior
  FROM TB_CITA
  WHERE IdCita = p_IdCita;

  IF v_Anterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita indicada no existe.';
  END IF;

  IF v_Estado IN (3, 4) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una cita completada o cancelada no se reprograma.';
  END IF;

  IF (SELECT IdGrupoCita FROM TB_CITA WHERE IdCita = p_IdCita) IS NOT NULL THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Esta cita es parte de un grupo: reprograme el grupo completo o cancele solo a este estudiante.';
  END IF;

  IF p_FechaHoraFin <= p_FechaHoraInicio THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La hora de fin tiene que ser posterior a la de inicio.';
  END IF;

  IF EXISTS (SELECT 1 FROM TB_DIA_NO_LABORAL WHERE Fecha = DATE(p_FechaHoraInicio)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese dia esta marcado como no laboral.';
  END IF;

  SET v_DiaSemana = WEEKDAY(p_FechaHoraInicio) + 1;

  IF NOT EXISTS (
    SELECT 1 FROM TB_HORARIO_ATENCION h
    WHERE h.DiaSemana = v_DiaSemana
      AND h.Activo = 1
      AND TIME(p_FechaHoraInicio) >= h.HoraInicio
      AND TIME(p_FechaHoraFin) <= h.HoraFin) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita queda fuera del horario de atencion.';
  END IF;

  IF EXISTS (
    SELECT 1 FROM TB_CITA c
    WHERE c.IdCita <> p_IdCita
      AND c.IdEstadoCita NOT IN (4, 5)
      AND c.FechaHoraInicio < p_FechaHoraFin
      AND p_FechaHoraInicio < c.FechaHoraFin) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita se traslapa con otra ya agendada.';
  END IF;

  START TRANSACTION;

  UPDATE TB_CITA
  SET FechaHoraInicio = p_FechaHoraInicio,
      FechaHoraFin = p_FechaHoraFin,
      FechaModificacion = NOW()
  WHERE IdCita = p_IdCita;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_CITA', p_IdCita, 'Editar', v_Anterior,
          JSON_OBJECT('FechaHoraInicio', p_FechaHoraInicio, 'FechaHoraFin', p_FechaHoraFin));

  COMMIT;
END$$
DELIMITER ;


-- Cancelar exige motivo: sin el, la agenda pierde la razon del hueco.
DROP PROCEDURE IF EXISTS SP_Cita_Cancelar;
DELIMITER $$
CREATE PROCEDURE SP_Cita_Cancelar(
  IN p_IdUsuarioAccion INT,
  IN p_IdCita INT,
  IN p_Motivo VARCHAR(500)
)
BEGIN
  DECLARE v_Estado INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF p_Motivo IS NULL OR TRIM(p_Motivo) = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el motivo de la cancelacion.';
  END IF;

  SELECT IdEstadoCita INTO v_Estado FROM TB_CITA WHERE IdCita = p_IdCita;

  IF v_Estado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita indicada no existe.';
  END IF;

  IF v_Estado = 3 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una cita completada no se puede cancelar.';
  END IF;

  START TRANSACTION;

  UPDATE TB_CITA
  SET IdEstadoCita = 4,
      MotivoCancelacion = p_Motivo,
      FechaModificacion = NOW()
  WHERE IdCita = p_IdCita;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_CITA', p_IdCita, 'CambiarEstado',
          JSON_OBJECT('IdEstadoCita', v_Estado),
          JSON_OBJECT('IdEstadoCita', 4, 'Motivo', p_Motivo));

  COMMIT;
END$$
DELIMITER ;


-- Confirmar, completar o marcar que no asistio.
-- Provisional: lo unico que se impide es sacar una cita de Cancelada. Si el
-- equipo define mas reglas de avance, van aqui.
DROP PROCEDURE IF EXISTS SP_Cita_CambiarEstado;
DELIMITER $$
CREATE PROCEDURE SP_Cita_CambiarEstado(
  IN p_IdUsuarioAccion INT,
  IN p_IdCita INT,
  IN p_IdEstadoCita INT
)
BEGIN
  DECLARE v_Estado INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdEstadoCita INTO v_Estado FROM TB_CITA WHERE IdCita = p_IdCita;

  IF v_Estado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita indicada no existe.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTADO_CITA WHERE IdEstadoCita = p_IdEstadoCita) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estado indicado no existe.';
  END IF;

  IF v_Estado = 4 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una cita cancelada ya no cambia de estado.';
  END IF;

  IF p_IdEstadoCita = 4 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Para cancelar use SP_Cita_Cancelar, que pide el motivo.';
  END IF;

  START TRANSACTION;

  UPDATE TB_CITA
  SET IdEstadoCita = p_IdEstadoCita,
      FechaModificacion = NOW()
  WHERE IdCita = p_IdCita;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_CITA', p_IdCita, 'CambiarEstado',
          JSON_OBJECT('IdEstadoCita', v_Estado),
          JSON_OBJECT('IdEstadoCita', p_IdEstadoCita));

  COMMIT;
END$$
DELIMITER ;


-- Calendario por rango de fechas. Con p_IdEstudiante en NULL trae todo.
DROP PROCEDURE IF EXISTS SP_Cita_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Cita_Listar(
  IN p_Desde DATETIME,
  IN p_Hasta DATETIME,
  IN p_IdEstudiante INT,
  IN p_IdEstadoCita INT
)
BEGIN
  SELECT c.IdCita,
         c.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         c.FechaHoraInicio,
         c.FechaHoraFin,
         ts.Nombre AS TipoSesion,
         m.Nombre AS Modalidad,
         ec.Nombre AS Estado,
         ec.ColorHex,
         c.Observaciones
  FROM TB_CITA c
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = c.IdEstudiante
  JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = c.IdTipoSesion
  JOIN TB_MODALIDAD m ON m.IdModalidad = c.IdModalidad
  JOIN TB_ESTADO_CITA ec ON ec.IdEstadoCita = c.IdEstadoCita
  WHERE (p_Desde IS NULL OR c.FechaHoraInicio >= p_Desde)
    AND (p_Hasta IS NULL OR c.FechaHoraInicio <= p_Hasta)
    AND (p_IdEstudiante IS NULL OR c.IdEstudiante = p_IdEstudiante)
    AND (p_IdEstadoCita IS NULL OR c.IdEstadoCita = p_IdEstadoCita)
  ORDER BY c.FechaHoraInicio;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Cita_ObtenerDetalle;
DELIMITER $$
CREATE PROCEDURE SP_Cita_ObtenerDetalle(
  IN p_IdCita INT
)
BEGIN
  SELECT c.IdCita,
         c.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         c.FechaHoraInicio,
         c.FechaHoraFin,
         c.IdTipoSesion, ts.Nombre AS TipoSesion,
         c.IdModalidad, m.Nombre AS Modalidad,
         c.IdEstadoCita, ec.Nombre AS Estado,
         c.Observaciones,
         c.MotivoCancelacion,
         s.IdSesion
  FROM TB_CITA c
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = c.IdEstudiante
  JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = c.IdTipoSesion
  JOIN TB_MODALIDAD m ON m.IdModalidad = c.IdModalidad
  JOIN TB_ESTADO_CITA ec ON ec.IdEstadoCita = c.IdEstadoCita
  LEFT JOIN TB_SESION s ON s.IdCita = c.IdCita
  WHERE c.IdCita = p_IdCita;
END$$
DELIMITER ;


-- Espacios libres de un dia: el horario de atencion partido en bloques del
-- largo que tenga el tipo de sesion, menos lo que ya esta ocupado.
DROP PROCEDURE IF EXISTS SP_Agenda_ConsultarDisponibilidad;
DELIMITER $$
CREATE PROCEDURE SP_Agenda_ConsultarDisponibilidad(
  IN p_Fecha DATE,
  IN p_IdTipoSesion INT
)
BEGIN
  DECLARE v_Duracion INT DEFAULT 60;
  DECLARE v_DiaSemana INT;

  SELECT IFNULL(DuracionMinutos, 60) INTO v_Duracion
  FROM TB_TIPO_SESION
  WHERE IdTipoSesion = p_IdTipoSesion;

  SET v_Duracion = IFNULL(v_Duracion, 60);
  SET v_DiaSemana = WEEKDAY(p_Fecha) + 1;

  IF EXISTS (SELECT 1 FROM TB_DIA_NO_LABORAL WHERE Fecha = p_Fecha) THEN
    SELECT CAST(NULL AS DATETIME) AS Inicio, CAST(NULL AS DATETIME) AS Fin FROM DUAL WHERE FALSE;
  ELSE
    WITH RECURSIVE bloque AS (
      SELECT TIMESTAMP(p_Fecha, h.HoraInicio) AS Inicio,
             TIMESTAMP(p_Fecha, h.HoraFin) AS Cierre
      FROM TB_HORARIO_ATENCION h
      WHERE h.DiaSemana = v_DiaSemana AND h.Activo = 1
      UNION ALL
      SELECT b.Inicio + INTERVAL v_Duracion MINUTE, b.Cierre
      FROM bloque b
      WHERE b.Inicio + INTERVAL (v_Duracion * 2) MINUTE <= b.Cierre
    )
    SELECT b.Inicio, b.Inicio + INTERVAL v_Duracion MINUTE AS Fin
    FROM bloque b
    WHERE b.Inicio + INTERVAL v_Duracion MINUTE <= b.Cierre
      AND NOT EXISTS (
        SELECT 1 FROM TB_CITA c
        WHERE c.IdEstadoCita NOT IN (4, 5)
          AND c.FechaHoraInicio < b.Inicio + INTERVAL v_Duracion MINUTE
          AND b.Inicio < c.FechaHoraFin)
    ORDER BY b.Inicio;
  END IF;
END$$
DELIMITER ;

-- ============================================================
-- 4b. Agenda (modulo de citas del panel)
-- ============================================================

-- ¿El rango choca con alguna cita activa?
--   p_IdCitaExcluir:    la propia cita al reprogramar (NULL si no aplica)
--   p_IdGrupoPermitido: las citas de ESTE grupo no cuentan como choque (NULL = todas cuentan)
-- Es la regla de "una sola profesional": nada se traslapa, salvo los miembros de un mismo grupo.
DROP FUNCTION IF EXISTS FN_Cita_HayConflicto;
DELIMITER $$
CREATE FUNCTION FN_Cita_HayConflicto(
    p_Inicio           DATETIME,
    p_Fin              DATETIME,
    p_IdCitaExcluir    INT,
    p_IdGrupoPermitido INT
) RETURNS BOOLEAN
READS SQL DATA
BEGIN
    RETURN EXISTS (
        SELECT 1
        FROM TB_CITA c
        WHERE c.IdEstadoCita NOT IN (4, 5)
          AND c.FechaHoraInicio < p_Fin
          AND p_Inicio < c.FechaHoraFin
          AND (p_IdCitaExcluir IS NULL OR c.IdCita <> p_IdCitaExcluir)
          AND (p_IdGrupoPermitido IS NULL OR c.IdGrupoCita IS NULL OR c.IdGrupoCita <> p_IdGrupoPermitido));
END$$
DELIMITER ;


-- ¿El rango cae dentro del horario de atencion y no es dia no laboral?
DROP FUNCTION IF EXISTS FN_Agenda_DentroDeHorario;
DELIMITER $$
CREATE FUNCTION FN_Agenda_DentroDeHorario(
    p_Inicio DATETIME,
    p_Fin    DATETIME
) RETURNS BOOLEAN
READS SQL DATA
BEGIN
    RETURN DATE(p_Inicio) = DATE(p_Fin)
       AND NOT EXISTS (SELECT 1 FROM TB_DIA_NO_LABORAL d WHERE d.Fecha = DATE(p_Inicio))
       AND EXISTS (
           SELECT 1
           FROM TB_HORARIO_ATENCION h
           WHERE h.DiaSemana = WEEKDAY(p_Inicio) + 1
             AND h.Activo = 1
             AND TIME(p_Inicio) >= h.HoraInicio
             AND TIME(p_Fin) <= h.HoraFin);
END$$
DELIMITER ;

-- Buscador de estudiantes para "Nueva cita". Busca por nombre del estudiante o
-- de cualquiera de sus encargados. La collation _ai_ci ignora mayusculas y
-- tildes: "jose" encuentra "José".
DROP PROCEDURE IF EXISTS SP_Cita_BuscarEstudiantes_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Cita_BuscarEstudiantes_CRM(
    IN p_Texto VARCHAR(100)
)
BEGIN
    DECLARE v_Patron VARCHAR(310);

    -- % y _ del usuario se escapan para que se busquen como texto, no como comodin
    SET v_Patron = CONCAT('%',
        REPLACE(REPLACE(REPLACE(TRIM(p_Texto), '\\', '\\\\'), '%', '\\%'), '_', '\\_'),
        '%');

    SELECT
      es.IdEstudiante,
      TRIM(CONCAT_WS(' ', es.Nombre, es.PrimerApellido, es.SegundoApellido)) AS Estudiante,
      ne.Nombre AS NivelEducativo,
      TRIM(CONCAT(en.Nombre, ' ', en.PrimerApellido)) AS Encargado
    FROM TB_ESTUDIANTE es
    LEFT JOIN TB_NIVEL_EDUCATIVO ne ON ne.IdNivelEducativo = es.IdNivelEducativo
    LEFT JOIN TB_ESTUDIANTE_ENCARGADO ee ON ee.IdEstudiante = es.IdEstudiante AND ee.EsPrincipal = 1
    LEFT JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    WHERE es.Activo = 1
      AND (
        CONCAT_WS(' ', es.Nombre, es.PrimerApellido, es.SegundoApellido) LIKE v_Patron
        OR EXISTS (
            SELECT 1
            FROM TB_ESTUDIANTE_ENCARGADO ee2
            JOIN TB_ENCARGADO e2 ON e2.IdEncargado = ee2.IdEncargado
            WHERE ee2.IdEstudiante = es.IdEstudiante
              AND CONCAT_WS(' ', e2.Nombre, e2.PrimerApellido, e2.SegundoApellido) LIKE v_Patron)
      )
    -- Primero los que empiezan con lo escrito, despues el resto
    ORDER BY (es.Nombre LIKE CONCAT(TRIM(p_Texto), '%')) DESC, es.Nombre, es.PrimerApellido
    LIMIT 10;
END$$
DELIMITER ;


-- Citas de un rango (semana o mes visible en el calendario).
DROP PROCEDURE IF EXISTS SP_Cita_ConsultarAgenda_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Cita_ConsultarAgenda_CRM(
    IN p_Desde DATETIME,
    IN p_Hasta DATETIME
)
BEGIN
    SELECT
      c.IdCita,
      c.IdEstudiante,
      TRIM(CONCAT(es.Nombre, ' ', es.PrimerApellido)) AS Estudiante,
      TRIM(CONCAT(en.Nombre, ' ', en.PrimerApellido)) AS Encargado,
      c.FechaHoraInicio,
      c.FechaHoraFin,
      c.IdTipoSesion,
      ts.Nombre AS TipoSesion,
      c.IdModalidad,
      m.Nombre AS Modalidad,
      c.IdEstadoCita,
      ec.Nombre AS Estado,
      c.Observaciones,
      c.MotivoCancelacion,
      EXISTS (SELECT 1 FROM TB_SESION s WHERE s.IdCita = c.IdCita) AS TieneSesion,
      c.IdGrupoCita,
      g.Nombre AS NombreGrupo,
      g.CupoMaximo AS CupoGrupo
    FROM TB_CITA c
    JOIN TB_ESTUDIANTE es ON es.IdEstudiante = c.IdEstudiante
    JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = c.IdTipoSesion
    JOIN TB_MODALIDAD m ON m.IdModalidad = c.IdModalidad
    JOIN TB_ESTADO_CITA ec ON ec.IdEstadoCita = c.IdEstadoCita
    LEFT JOIN TB_GRUPO_CITA g ON g.IdGrupoCita = c.IdGrupoCita
    LEFT JOIN TB_ESTUDIANTE_ENCARGADO ee ON ee.IdEstudiante = es.IdEstudiante AND ee.EsPrincipal = 1
    LEFT JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    WHERE c.FechaHoraInicio >= p_Desde
      AND c.FechaHoraInicio < p_Hasta
    ORDER BY c.FechaHoraInicio, c.IdGrupoCita, es.Nombre;
END$$
DELIMITER ;


-- Espacios libres de un dia, cada 30 minutos, del largo del tipo de sesion.
--   p_IdCitaExcluir: al reprogramar, la propia cita no cuenta como ocupada (NULL al crear)
--   p_Ahora: hora actual de Costa Rica, la manda el API; no se ofrecen horas pasadas
DROP PROCEDURE IF EXISTS SP_Cita_ConsultarDisponibilidad_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Cita_ConsultarDisponibilidad_CRM(
    IN p_Fecha          DATE,
    IN p_IdTipoSesion   INT,
    IN p_IdCitaExcluir  INT,
    IN p_IdGrupoExcluir INT,
    IN p_Ahora          DATETIME
)
BEGIN
    DECLARE v_Duracion  INT DEFAULT NULL;
    DECLARE v_DiaSemana INT;

    SELECT DuracionMinutos INTO v_Duracion
    FROM TB_TIPO_SESION
    WHERE IdTipoSesion = p_IdTipoSesion;

    SET v_Duracion = IFNULL(v_Duracion, 60);
    SET v_DiaSemana = WEEKDAY(p_Fecha) + 1;

    WITH RECURSIVE bloque AS (
        SELECT TIMESTAMP(p_Fecha, h.HoraInicio) AS Inicio,
               TIMESTAMP(p_Fecha, h.HoraFin)    AS Cierre
        FROM TB_HORARIO_ATENCION h
        WHERE h.DiaSemana = v_DiaSemana
          AND h.Activo = 1
          AND NOT EXISTS (SELECT 1 FROM TB_DIA_NO_LABORAL d WHERE d.Fecha = p_Fecha)
        UNION ALL
        SELECT b.Inicio + INTERVAL 30 MINUTE, b.Cierre
        FROM bloque b
        WHERE b.Inicio + INTERVAL 30 MINUTE < b.Cierre
    )
    SELECT b.Inicio,
           b.Inicio + INTERVAL v_Duracion MINUTE AS Fin
    FROM bloque b
    WHERE b.Inicio + INTERVAL v_Duracion MINUTE <= b.Cierre
      AND b.Inicio > p_Ahora
      AND NOT FN_Cita_HayConflicto(b.Inicio, b.Inicio + INTERVAL v_Duracion MINUTE,
                                   p_IdCitaExcluir, p_IdGrupoExcluir)
    ORDER BY b.Inicio;
END$$
DELIMITER ;


-- Pasa a Completada toda cita Programada o Confirmada cuya hora de fin ya paso.
-- La llama el servicio en segundo plano del API y tambien la agenda al cargar.
-- No va a bitacora: no hay usuario que la ejecute (TB_BITACORA exige uno).
DROP PROCEDURE IF EXISTS SP_Cita_CompletarVencidas_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Cita_CompletarVencidas_CRM(
    IN p_Ahora DATETIME
)
BEGIN
    UPDATE TB_CITA
    SET IdEstadoCita = 3,
        FechaModificacion = p_Ahora
    WHERE IdEstadoCita IN (1, 2)
      AND FechaHoraFin <= p_Ahora;

    SELECT ROW_COUNT() AS CitasCompletadas;
END$$
DELIMITER ;


-- "No asistio": solo para citas que ya empezaron, que no esten canceladas y que
-- no tengan una sesion registrada (si hay sesion, el estudiante si vino).
DROP PROCEDURE IF EXISTS SP_Cita_MarcarNoAsistio_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Cita_MarcarNoAsistio_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdCita          INT,
    IN p_Ahora           DATETIME
)
BEGIN
    DECLARE v_Estado INT DEFAULT NULL;
    DECLARE v_Inicio DATETIME DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT IdEstadoCita, FechaHoraInicio INTO v_Estado, v_Inicio
    FROM TB_CITA
    WHERE IdCita = p_IdCita;

    IF v_Estado IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita indicada no existe.';
    END IF;

    IF v_Estado = 4 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Una cita cancelada no se puede marcar como no asistió.';
    END IF;

    IF v_Estado = 5 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita ya está marcada como no asistió.';
    END IF;

    IF v_Inicio > p_Ahora THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Solo se puede marcar cuando la hora de la cita ya pasó.';
    END IF;

    IF EXISTS (SELECT 1 FROM TB_SESION WHERE IdCita = p_IdCita) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita tiene una sesión registrada: el estudiante sí asistió.';
    END IF;

    START TRANSACTION;

    UPDATE TB_CITA
    SET IdEstadoCita = 5,
        FechaModificacion = p_Ahora
    WHERE IdCita = p_IdCita;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_CITA', p_IdCita, 'CambiarEstado',
            JSON_OBJECT('IdEstadoCita', v_Estado),
            JSON_OBJECT('IdEstadoCita', 5));

    COMMIT;
END$$
DELIMITER ;


-- Catalogos del formulario. La duracion se usa para calcular la hora de fin.
DROP PROCEDURE IF EXISTS SP_ListarTiposSesion_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ListarTiposSesion_CRM()
BEGIN
    SELECT IdTipoSesion, Nombre, IFNULL(DuracionMinutos, 60) AS DuracionMinutos
    FROM TB_TIPO_SESION
    ORDER BY Nombre;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_ListarModalidades_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ListarModalidades_CRM()
BEGIN
    SELECT IdModalidad, Nombre
    FROM TB_MODALIDAD
    ORDER BY IdModalidad;
END$$
DELIMITER ;

-- Crea el grupo y una cita por estudiante, en una sola transaccion.
-- p_EstudiantesJson: arreglo de ids, por ejemplo '[4, 7, 12]'
DROP PROCEDURE IF EXISTS SP_Grupo_Crear_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Grupo_Crear_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdTipoSesion    INT,
    IN p_IdModalidad     INT,
    IN p_Nombre          VARCHAR(100),
    IN p_Inicio          DATETIME,
    IN p_Fin             DATETIME,
    IN p_CupoMaximo      INT,
    IN p_EstudiantesJson JSON,
    IN p_Observaciones   VARCHAR(500)
)
BEGIN
    DECLARE v_IdGrupo   INT;
    DECLARE v_Total     INT DEFAULT 0;
    DECLARE v_Distintos INT DEFAULT 0;
    DECLARE v_Activos   INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_Total = IFNULL(JSON_LENGTH(p_EstudiantesJson), 0);

    IF v_Total = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Agregue al menos un estudiante al grupo.';
    END IF;

    IF p_CupoMaximo IS NULL OR p_CupoMaximo NOT BETWEEN 2 AND 30 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cupo del grupo debe estar entre 2 y 30.';
    END IF;

    IF v_Total > p_CupoMaximo THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Hay más estudiantes que cupos en el grupo.';
    END IF;

    SELECT COUNT(DISTINCT j.Id) INTO v_Distintos
    FROM JSON_TABLE(p_EstudiantesJson, '$[*]' COLUMNS (Id INT PATH '$')) AS j;

    IF v_Distintos <> v_Total THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Hay estudiantes repetidos en el grupo.';
    END IF;

    SELECT COUNT(*) INTO v_Activos
    FROM JSON_TABLE(p_EstudiantesJson, '$[*]' COLUMNS (Id INT PATH '$')) AS j
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = j.Id AND e.Activo = 1;

    IF v_Activos <> v_Total THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Uno de los estudiantes no existe o está inactivo.';
    END IF;

    IF NOT FN_Agenda_DentroDeHorario(p_Inicio, p_Fin) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El grupo queda fuera del horario de atención o en un día no laboral.';
    END IF;

    IF FN_Cita_HayConflicto(p_Inicio, p_Fin, NULL, NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El horario se traslapa con otra cita ya agendada.';
    END IF;

    START TRANSACTION;

    INSERT INTO TB_GRUPO_CITA (IdTipoSesion, IdModalidad, IdUsuarioRegistro, Nombre,
                               FechaHoraInicio, FechaHoraFin, CupoMaximo)
    VALUES (p_IdTipoSesion, p_IdModalidad, p_IdUsuarioAccion, NULLIF(TRIM(p_Nombre), ''),
            p_Inicio, p_Fin, p_CupoMaximo);

    SET v_IdGrupo = LAST_INSERT_ID();

    INSERT INTO TB_CITA (IdEstudiante, IdTipoSesion, IdModalidad, IdEstadoCita, IdUsuarioRegistro,
                         IdGrupoCita, FechaHoraInicio, FechaHoraFin, Observaciones)
    SELECT j.Id, p_IdTipoSesion, p_IdModalidad, 1, p_IdUsuarioAccion,
           v_IdGrupo, p_Inicio, p_Fin, NULLIF(TRIM(p_Observaciones), '')
    FROM JSON_TABLE(p_EstudiantesJson, '$[*]' COLUMNS (Id INT PATH '$')) AS j;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_GRUPO_CITA', v_IdGrupo, 'Crear',
            JSON_OBJECT('FechaHoraInicio', p_Inicio, 'CupoMaximo', p_CupoMaximo,
                        'Estudiantes', p_EstudiantesJson));

    COMMIT;

    SELECT v_IdGrupo AS IdGrupoCita;
END$$
DELIMITER ;


-- Suma un estudiante a un grupo existente. Bloquea la fila del grupo
-- (FOR UPDATE) para que dos personas no ocupen el ultimo cupo a la vez.
DROP PROCEDURE IF EXISTS SP_Grupo_AgregarEstudiante_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Grupo_AgregarEstudiante_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdGrupoCita     INT,
    IN p_IdEstudiante    INT,
    IN p_Ahora           DATETIME
)
BEGIN
    DECLARE v_IdTipoSesion INT DEFAULT NULL;
    DECLARE v_IdModalidad  INT;
    DECLARE v_Inicio       DATETIME;
    DECLARE v_Fin          DATETIME;
    DECLARE v_Cupo         INT;
    DECLARE v_Activo       BOOLEAN;
    DECLARE v_Inscritos    INT DEFAULT 0;
    DECLARE v_IdCita       INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    SELECT IdTipoSesion, IdModalidad, FechaHoraInicio, FechaHoraFin, CupoMaximo, Activo
      INTO v_IdTipoSesion, v_IdModalidad, v_Inicio, v_Fin, v_Cupo, v_Activo
    FROM TB_GRUPO_CITA
    WHERE IdGrupoCita = p_IdGrupoCita
    FOR UPDATE;

    IF v_IdTipoSesion IS NULL OR v_Activo = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El grupo no existe o fue cancelado.';
    END IF;

    IF v_Inicio <= p_Ahora THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El grupo ya empezó: no se pueden agregar estudiantes.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE WHERE IdEstudiante = p_IdEstudiante AND Activo = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante no existe o está inactivo.';
    END IF;

    IF EXISTS (SELECT 1 FROM TB_CITA
               WHERE IdGrupoCita = p_IdGrupoCita AND IdEstudiante = p_IdEstudiante
                 AND IdEstadoCita <> 4) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante ya está en este grupo.';
    END IF;

    SELECT COUNT(*) INTO v_Inscritos
    FROM TB_CITA
    WHERE IdGrupoCita = p_IdGrupoCita AND IdEstadoCita <> 4;

    IF v_Inscritos >= v_Cupo THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El grupo ya no tiene cupo disponible.';
    END IF;

    -- Solo los miembros del propio grupo pueden compartir el horario
    IF FN_Cita_HayConflicto(v_Inicio, v_Fin, NULL, p_IdGrupoCita) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El horario del grupo se traslapa con otra cita.';
    END IF;

    INSERT INTO TB_CITA (IdEstudiante, IdTipoSesion, IdModalidad, IdEstadoCita, IdUsuarioRegistro,
                         IdGrupoCita, FechaHoraInicio, FechaHoraFin)
    VALUES (p_IdEstudiante, v_IdTipoSesion, v_IdModalidad, 1, p_IdUsuarioAccion,
            p_IdGrupoCita, v_Inicio, v_Fin);

    SET v_IdCita = LAST_INSERT_ID();

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_CITA', v_IdCita, 'Crear',
            JSON_OBJECT('IdEstudiante', p_IdEstudiante, 'IdGrupoCita', p_IdGrupoCita));

    COMMIT;

    SELECT v_IdCita AS IdCita;
END$$
DELIMITER ;


-- Mueve el grupo completo. Solo se mueven las citas pendientes;
-- las canceladas quedan con su hora original en el historial.
DROP PROCEDURE IF EXISTS SP_Grupo_Reprogramar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Grupo_Reprogramar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdGrupoCita     INT,
    IN p_Inicio          DATETIME,
    IN p_Fin             DATETIME,
    IN p_Ahora           DATETIME
)
BEGIN
    DECLARE v_InicioActual DATETIME DEFAULT NULL;
    DECLARE v_Activo       BOOLEAN;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT FechaHoraInicio, Activo INTO v_InicioActual, v_Activo
    FROM TB_GRUPO_CITA
    WHERE IdGrupoCita = p_IdGrupoCita;

    IF v_InicioActual IS NULL OR v_Activo = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El grupo no existe o fue cancelado.';
    END IF;

    IF v_InicioActual <= p_Ahora THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Un grupo que ya empezó no se puede reprogramar.';
    END IF;

    IF p_Inicio <= p_Ahora THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La nueva hora tiene que ser posterior a la actual.';
    END IF;

    IF NOT FN_Agenda_DentroDeHorario(p_Inicio, p_Fin) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El nuevo horario queda fuera del horario de atención o en un día no laboral.';
    END IF;

    IF FN_Cita_HayConflicto(p_Inicio, p_Fin, NULL, p_IdGrupoCita) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El nuevo horario se traslapa con otra cita.';
    END IF;

    START TRANSACTION;

    UPDATE TB_GRUPO_CITA
    SET FechaHoraInicio = p_Inicio,
        FechaHoraFin = p_Fin,
        FechaModificacion = p_Ahora
    WHERE IdGrupoCita = p_IdGrupoCita;

    UPDATE TB_CITA
    SET FechaHoraInicio = p_Inicio,
        FechaHoraFin = p_Fin,
        FechaModificacion = p_Ahora
    WHERE IdGrupoCita = p_IdGrupoCita
      AND IdEstadoCita IN (1, 2);

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_GRUPO_CITA', p_IdGrupoCita, 'Editar',
            JSON_OBJECT('FechaHoraInicio', v_InicioActual),
            JSON_OBJECT('FechaHoraInicio', p_Inicio));

    COMMIT;
END$$
DELIMITER ;


-- Cancela el grupo completo: todas sus citas pendientes con el mismo motivo.
DROP PROCEDURE IF EXISTS SP_Grupo_Cancelar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Grupo_Cancelar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdGrupoCita     INT,
    IN p_Motivo          VARCHAR(500),
    IN p_Ahora           DATETIME
)
BEGIN
    DECLARE v_Inicio DATETIME DEFAULT NULL;
    DECLARE v_Activo BOOLEAN;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_Motivo IS NULL OR TRIM(p_Motivo) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el motivo de la cancelación.';
    END IF;

    SELECT FechaHoraInicio, Activo INTO v_Inicio, v_Activo
    FROM TB_GRUPO_CITA
    WHERE IdGrupoCita = p_IdGrupoCita;

    IF v_Inicio IS NULL OR v_Activo = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El grupo no existe o ya fue cancelado.';
    END IF;

    IF v_Inicio <= p_Ahora THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Un grupo que ya empezó no se puede cancelar.';
    END IF;

    START TRANSACTION;

    UPDATE TB_CITA
    SET IdEstadoCita = 4,
        MotivoCancelacion = TRIM(p_Motivo),
        FechaModificacion = p_Ahora
    WHERE IdGrupoCita = p_IdGrupoCita
      AND IdEstadoCita IN (1, 2);

    UPDATE TB_GRUPO_CITA
    SET Activo = 0,
        FechaModificacion = p_Ahora
    WHERE IdGrupoCita = p_IdGrupoCita;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_GRUPO_CITA', p_IdGrupoCita, 'CambiarEstado',
            JSON_OBJECT('Activo', 0, 'Motivo', TRIM(p_Motivo)));

    COMMIT;
END$$
DELIMITER ;


-- Cambia nombre y cupo. El cupo nunca queda por debajo de los inscritos.
DROP PROCEDURE IF EXISTS SP_Grupo_Editar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Grupo_Editar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdGrupoCita     INT,
    IN p_Nombre          VARCHAR(100),
    IN p_CupoMaximo      INT
)
BEGIN
    DECLARE v_Anterior  JSON DEFAULT NULL;
    DECLARE v_Inscritos INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT JSON_OBJECT('Nombre', Nombre, 'CupoMaximo', CupoMaximo) INTO v_Anterior
    FROM TB_GRUPO_CITA
    WHERE IdGrupoCita = p_IdGrupoCita AND Activo = 1;

    IF v_Anterior IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El grupo no existe o fue cancelado.';
    END IF;

    IF p_CupoMaximo IS NULL OR p_CupoMaximo NOT BETWEEN 2 AND 30 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cupo del grupo debe estar entre 2 y 30.';
    END IF;

    SELECT COUNT(*) INTO v_Inscritos
    FROM TB_CITA
    WHERE IdGrupoCita = p_IdGrupoCita AND IdEstadoCita <> 4;

    IF p_CupoMaximo < v_Inscritos THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El cupo no puede ser menor que la cantidad de estudiantes inscritos.';
    END IF;

    START TRANSACTION;

    UPDATE TB_GRUPO_CITA
    SET Nombre = NULLIF(TRIM(p_Nombre), ''),
        CupoMaximo = p_CupoMaximo
    WHERE IdGrupoCita = p_IdGrupoCita;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_GRUPO_CITA', p_IdGrupoCita, 'Editar', v_Anterior,
            JSON_OBJECT('Nombre', p_Nombre, 'CupoMaximo', p_CupoMaximo));

    COMMIT;
END$$
DELIMITER ;


-- Grupos de un dia que todavia aceptan estudiantes (para "Unirse a un grupo").
DROP PROCEDURE IF EXISTS SP_Grupo_ConsultarDisponibles_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Grupo_ConsultarDisponibles_CRM(
    IN p_Fecha DATE,
    IN p_Ahora DATETIME
)
BEGIN
    SELECT
      g.IdGrupoCita,
      g.Nombre,
      g.FechaHoraInicio AS Inicio,
      g.FechaHoraFin AS Fin,
      ts.Nombre AS TipoSesion,
      m.Nombre AS Modalidad,
      g.CupoMaximo,
      (SELECT COUNT(*) FROM TB_CITA c
       WHERE c.IdGrupoCita = g.IdGrupoCita AND c.IdEstadoCita <> 4) AS Inscritos
    FROM TB_GRUPO_CITA g
    JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = g.IdTipoSesion
    JOIN TB_MODALIDAD m ON m.IdModalidad = g.IdModalidad
    WHERE g.Activo = 1
      AND g.FechaHoraInicio >= p_Fecha
      AND g.FechaHoraInicio < p_Fecha + INTERVAL 1 DAY
      AND g.FechaHoraInicio > p_Ahora
      AND NOT FN_Cita_HayConflicto(g.FechaHoraInicio, g.FechaHoraFin, NULL, g.IdGrupoCita)
    HAVING Inscritos < g.CupoMaximo
    ORDER BY g.FechaHoraInicio;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS SP_Grupo_Obtener_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Grupo_Obtener_CRM(
    IN p_IdGrupoCita INT
)
BEGIN
    SELECT IdGrupoCita, IdTipoSesion, FechaHoraInicio, Activo
    FROM TB_GRUPO_CITA
    WHERE IdGrupoCita = p_IdGrupoCita;
END$$
DELIMITER ;

-- ============================================================
-- 5. Sesiones
-- ============================================================

-- Registrar la atencion ya realizada. Si nace de una cita, esa cita queda
-- marcada como Completada en la misma transaccion.
DROP PROCEDURE IF EXISTS SP_Sesion_Registrar;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_Registrar(
  IN p_IdUsuarioAccion INT,
  IN p_IdEstudiante INT,
  IN p_IdCita INT,
  IN p_IdTipoAtencion INT,
  IN p_Fecha DATE,
  IN p_TemaTrabajado VARCHAR(500),
  IN p_Avances VARCHAR(1000),
  IN p_Recomendaciones VARCHAR(1000),
  IN p_Observaciones VARCHAR(1000)
)
BEGIN
  DECLARE v_IdSesion INT;
  DECLARE v_EstudianteCita INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE WHERE IdEstudiante = p_IdEstudiante AND Activo = 1) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante indicado no existe o esta inactivo.';
  END IF;

  IF p_IdCita IS NOT NULL THEN
    SELECT IdEstudiante INTO v_EstudianteCita FROM TB_CITA WHERE IdCita = p_IdCita;

    IF v_EstudianteCita IS NULL THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita indicada no existe.';
    END IF;

    IF v_EstudianteCita <> p_IdEstudiante THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita es de otro estudiante.';
    END IF;

    IF EXISTS (SELECT 1 FROM TB_SESION WHERE IdCita = p_IdCita) THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Esa cita ya tiene una sesion registrada.';
    END IF;
  END IF;

  START TRANSACTION;

  INSERT INTO TB_SESION (IdEstudiante, IdCita, IdTipoAtencion, IdUsuarioRegistro, Fecha,
                         TemaTrabajado, Avances, Recomendaciones, Observaciones)
  VALUES (p_IdEstudiante, p_IdCita, p_IdTipoAtencion, p_IdUsuarioAccion, p_Fecha,
          p_TemaTrabajado, p_Avances, p_Recomendaciones, p_Observaciones);

  SET v_IdSesion = LAST_INSERT_ID();

  IF p_IdCita IS NOT NULL THEN
    UPDATE TB_CITA
    SET IdEstadoCita = 3,
        FechaModificacion = NOW()
    WHERE IdCita = p_IdCita AND IdEstadoCita NOT IN (3, 4);
  END IF;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_SESION', v_IdSesion, 'Crear',
          JSON_OBJECT('IdEstudiante', p_IdEstudiante, 'IdCita', p_IdCita, 'Fecha', p_Fecha));

  COMMIT;

  SELECT v_IdSesion AS IdSesion;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Sesion_Editar;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_Editar(
  IN p_IdUsuarioAccion INT,
  IN p_IdSesion INT,
  IN p_IdTipoAtencion INT,
  IN p_Fecha DATE,
  IN p_TemaTrabajado VARCHAR(500),
  IN p_Avances VARCHAR(1000),
  IN p_Recomendaciones VARCHAR(1000),
  IN p_Observaciones VARCHAR(1000)
)
BEGIN
  DECLARE v_Anterior JSON DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT JSON_OBJECT('Fecha', Fecha, 'TemaTrabajado', TemaTrabajado)
    INTO v_Anterior
  FROM TB_SESION
  WHERE IdSesion = p_IdSesion;

  IF v_Anterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sesion indicada no existe.';
  END IF;

  START TRANSACTION;

  UPDATE TB_SESION
  SET IdTipoAtencion = p_IdTipoAtencion,
      Fecha = p_Fecha,
      TemaTrabajado = p_TemaTrabajado,
      Avances = p_Avances,
      Recomendaciones = p_Recomendaciones,
      Observaciones = p_Observaciones
  WHERE IdSesion = p_IdSesion;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_SESION', p_IdSesion, 'Editar', v_Anterior,
          JSON_OBJECT('Fecha', p_Fecha, 'TemaTrabajado', p_TemaTrabajado));

  COMMIT;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Sesion_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_Listar(
  IN p_IdEstudiante INT,
  IN p_Desde DATE,
  IN p_Hasta DATE
)
BEGIN
  SELECT s.IdSesion,
         s.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         s.Fecha,
         ta.Nombre AS TipoAtencion,
         s.TemaTrabajado,
         s.Avances,
         s.IdCita,
         u.NombreCompleto AS RegistradaPor
  FROM TB_SESION s
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = s.IdEstudiante
  JOIN TB_TIPO_ATENCION ta ON ta.IdTipoAtencion = s.IdTipoAtencion
  JOIN TB_USUARIO u ON u.IdUsuario = s.IdUsuarioRegistro
  WHERE (p_IdEstudiante IS NULL OR s.IdEstudiante = p_IdEstudiante)
    AND (p_Desde IS NULL OR s.Fecha >= p_Desde)
    AND (p_Hasta IS NULL OR s.Fecha <= p_Hasta)
  ORDER BY s.Fecha DESC, s.IdSesion DESC;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Sesion_ObtenerDetalle;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_ObtenerDetalle(
  IN p_IdSesion INT
)
BEGIN
  SELECT s.IdSesion,
         s.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         s.Fecha,
         s.IdTipoAtencion, ta.Nombre AS TipoAtencion,
         s.TemaTrabajado, s.Avances, s.Recomendaciones, s.Observaciones,
         s.IdCita, c.FechaHoraInicio AS FechaHoraCita,
         u.NombreCompleto AS RegistradaPor,
         s.FechaCreacion, s.FechaModificacion
  FROM TB_SESION s
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = s.IdEstudiante
  JOIN TB_TIPO_ATENCION ta ON ta.IdTipoAtencion = s.IdTipoAtencion
  JOIN TB_USUARIO u ON u.IdUsuario = s.IdUsuarioRegistro
  LEFT JOIN TB_CITA c ON c.IdCita = s.IdCita
  WHERE s.IdSesion = p_IdSesion;
END$$
DELIMITER ;


-- SP_Sesion_Eliminar: no se escribe. TB_SESION no tiene columna Activo y la
-- regla del equipo prohibe el borrado fisico. Falta decidir con el DBA.

-- ============================================================
-- 6. Planes de intervencion
-- ============================================================

-- Ids de TB_ESTADO_PLAN: 1 Borrador, 2 Activo, 3 Finalizado, 4 Suspendido.


-- Crea el plan con su arbol completo en una sola transaccion.
-- p_EstrategiasJson es un arreglo como:
--   [{"Descripcion":"...","Orden":1,"Actividades":[{"Nombre":"...","Descripcion":"..."}]}]
-- Puede venir NULL si el plan nace vacio y se le agregan estrategias despues.
DROP PROCEDURE IF EXISTS SP_Plan_Crear;
DELIMITER $$
CREATE PROCEDURE SP_Plan_Crear(
  IN p_IdUsuarioAccion INT,
  IN p_IdEstudiante INT,
  IN p_Titulo VARCHAR(150),
  IN p_ObjetivoGeneral VARCHAR(1000),
  IN p_FechaInicio DATE,
  IN p_FechaFin DATE,
  IN p_IdEstadoPlan INT,
  IN p_EstrategiasJson JSON
)
BEGIN
  DECLARE v_IdPlan INT;
  DECLARE v_IdEstrategia INT;
  DECLARE v_i INT DEFAULT 0;
  DECLARE v_n INT DEFAULT 0;
  DECLARE v_j INT DEFAULT 0;
  DECLARE v_m INT DEFAULT 0;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

IF NOT EXISTS (
    SELECT 1
    FROM TB_ESTUDIANTE
    WHERE IdEstudiante = p_IdEstudiante
      AND Activo = 1
) THEN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estudiante indicado no existe o esta inactivo.';
END IF;

IF EXISTS (
    SELECT 1
    FROM TB_PLAN_INTERVENCION
    WHERE IdEstudiante = p_IdEstudiante
      AND LOWER(TRIM(Titulo)) = LOWER(TRIM(p_Titulo))
) THEN
    SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT =
        'Ya existe un plan con ese nombre para el estudiante seleccionado.';
END IF;

START TRANSACTION;

  INSERT INTO TB_PLAN_INTERVENCION (IdEstudiante, IdEstadoPlan, IdUsuarioRegistro, Titulo,
                                    ObjetivoGeneral, FechaInicio, FechaFin)
  VALUES (p_IdEstudiante, IFNULL(p_IdEstadoPlan, 1), p_IdUsuarioAccion, p_Titulo,
          p_ObjetivoGeneral, p_FechaInicio, p_FechaFin);

  SET v_IdPlan = LAST_INSERT_ID();
  SET v_n = IFNULL(JSON_LENGTH(p_EstrategiasJson), 0);

  WHILE v_i < v_n DO
    INSERT INTO TB_ESTRATEGIA (IdPlan, Descripcion, Orden)
    VALUES (v_IdPlan,
            JSON_UNQUOTE(JSON_EXTRACT(p_EstrategiasJson, CONCAT('$[', v_i, '].Descripcion'))),
            CAST(JSON_EXTRACT(p_EstrategiasJson, CONCAT('$[', v_i, '].Orden')) AS UNSIGNED));

    SET v_IdEstrategia = LAST_INSERT_ID();
    SET v_m = IFNULL(JSON_LENGTH(JSON_EXTRACT(p_EstrategiasJson, CONCAT('$[', v_i, '].Actividades'))), 0);
    SET v_j = 0;

    WHILE v_j < v_m DO
      INSERT INTO TB_ACTIVIDAD (IdEstrategia, Nombre, Descripcion)
      VALUES (v_IdEstrategia,
              JSON_UNQUOTE(JSON_EXTRACT(p_EstrategiasJson, CONCAT('$[', v_i, '].Actividades[', v_j, '].Nombre'))),
              JSON_UNQUOTE(JSON_EXTRACT(p_EstrategiasJson, CONCAT('$[', v_i, '].Actividades[', v_j, '].Descripcion'))));
      SET v_j = v_j + 1;
    END WHILE;

    SET v_i = v_i + 1;
  END WHILE;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_PLAN_INTERVENCION', v_IdPlan, 'Crear',
          JSON_OBJECT('IdEstudiante', p_IdEstudiante, 'Titulo', p_Titulo, 'Estrategias', v_n));

  COMMIT;

  SELECT v_IdPlan AS IdPlan;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Plan_Editar;
DELIMITER $$
CREATE PROCEDURE SP_Plan_Editar(
  IN p_IdUsuarioAccion INT,
  IN p_IdPlan INT,
  IN p_Titulo VARCHAR(150),
  IN p_ObjetivoGeneral VARCHAR(1000),
  IN p_FechaInicio DATE,
  IN p_FechaFin DATE
)
BEGIN
  DECLARE v_Anterior JSON DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT JSON_OBJECT('Titulo', Titulo, 'FechaInicio', FechaInicio, 'FechaFin', FechaFin)
    INTO v_Anterior
  FROM TB_PLAN_INTERVENCION
  WHERE IdPlan = p_IdPlan;

  IF v_Anterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El plan indicado no existe.';
  END IF;

  START TRANSACTION;

  UPDATE TB_PLAN_INTERVENCION
  SET Titulo = p_Titulo,
      ObjetivoGeneral = p_ObjetivoGeneral,
      FechaInicio = p_FechaInicio,
      FechaFin = p_FechaFin
  WHERE IdPlan = p_IdPlan;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_PLAN_INTERVENCION', p_IdPlan, 'Editar', v_Anterior,
          JSON_OBJECT('Titulo', p_Titulo, 'FechaInicio', p_FechaInicio, 'FechaFin', p_FechaFin));

  COMMIT;
END$$
DELIMITER ;


-- Provisional: lo unico que se impide es reabrir un plan Finalizado. Falta
-- decidir si al finalizar hay que hacer algo con las actividades pendientes.
DROP PROCEDURE IF EXISTS SP_Plan_CambiarEstado;
DELIMITER $$
CREATE PROCEDURE SP_Plan_CambiarEstado(
  IN p_IdUsuarioAccion INT,
  IN p_IdPlan INT,
  IN p_IdEstadoPlan INT
)
BEGIN
  DECLARE v_Estado INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdEstadoPlan INTO v_Estado FROM TB_PLAN_INTERVENCION WHERE IdPlan = p_IdPlan;

  IF v_Estado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El plan indicado no existe.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTADO_PLAN WHERE IdEstadoPlan = p_IdEstadoPlan) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estado indicado no existe.';
  END IF;

  IF v_Estado = 3 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Un plan finalizado no se reabre.';
  END IF;

  START TRANSACTION;

  UPDATE TB_PLAN_INTERVENCION
  SET IdEstadoPlan = p_IdEstadoPlan
  WHERE IdPlan = p_IdPlan;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_PLAN_INTERVENCION', p_IdPlan, 'CambiarEstado',
          JSON_OBJECT('IdEstadoPlan', v_Estado),
          JSON_OBJECT('IdEstadoPlan', p_IdEstadoPlan));

  COMMIT;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Plan_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Plan_Listar(
  IN p_IdEstudiante INT,
  IN p_IdEstadoPlan INT
)
BEGIN
  SELECT p.IdPlan,
         p.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         p.Titulo,
         p.ObjetivoGeneral,
         p.FechaInicio,
         p.FechaFin,
         ep.Nombre AS Estado,
         (SELECT COUNT(*) FROM TB_ESTRATEGIA WHERE IdPlan = p.IdPlan) AS Estrategias,
         (SELECT COUNT(*) FROM TB_ACTIVIDAD a
          JOIN TB_ESTRATEGIA es ON es.IdEstrategia = a.IdEstrategia
          WHERE es.IdPlan = p.IdPlan) AS Actividades,
         (SELECT COUNT(*) FROM TB_ACTIVIDAD a
          JOIN TB_ESTRATEGIA es ON es.IdEstrategia = a.IdEstrategia
          WHERE es.IdPlan = p.IdPlan AND a.Completada = 1) AS ActividadesCompletadas
  FROM TB_PLAN_INTERVENCION p
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = p.IdEstudiante
  JOIN TB_ESTADO_PLAN ep ON ep.IdEstadoPlan = p.IdEstadoPlan
  WHERE (p_IdEstudiante IS NULL OR p.IdEstudiante = p_IdEstudiante)
    AND (p_IdEstadoPlan IS NULL OR p.IdEstadoPlan = p_IdEstadoPlan)
  ORDER BY p.FechaInicio DESC;
END$$
DELIMITER ;


-- Devuelve tres resultados: el plan, sus estrategias y sus actividades.
DROP PROCEDURE IF EXISTS SP_Plan_ObtenerDetalle;
DELIMITER $$
CREATE PROCEDURE SP_Plan_ObtenerDetalle(
  IN p_IdPlan INT
)
BEGIN
  SELECT p.IdPlan, p.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         p.Titulo, p.ObjetivoGeneral, p.FechaInicio, p.FechaFin,
         p.IdEstadoPlan, ep.Nombre AS Estado,
         u.NombreCompleto AS RegistradoPor
  FROM TB_PLAN_INTERVENCION p
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = p.IdEstudiante
  JOIN TB_ESTADO_PLAN ep ON ep.IdEstadoPlan = p.IdEstadoPlan
  JOIN TB_USUARIO u ON u.IdUsuario = p.IdUsuarioRegistro
  WHERE p.IdPlan = p_IdPlan;

  SELECT IdEstrategia, IdPlan, Descripcion, Orden
  FROM TB_ESTRATEGIA
  WHERE IdPlan = p_IdPlan
  ORDER BY IFNULL(Orden, 9999), IdEstrategia;

  SELECT a.IdActividad, a.IdEstrategia, a.Nombre, a.Descripcion, a.Completada
  FROM TB_ACTIVIDAD a
  JOIN TB_ESTRATEGIA es ON es.IdEstrategia = a.IdEstrategia
  WHERE es.IdPlan = p_IdPlan
  ORDER BY a.IdEstrategia, a.IdActividad;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Estrategia_Agregar;
DELIMITER $$
CREATE PROCEDURE SP_Estrategia_Agregar(
  IN p_IdUsuarioAccion INT,
  IN p_IdPlan INT,
  IN p_Descripcion VARCHAR(500),
  IN p_Orden INT
)
BEGIN
  DECLARE v_IdEstrategia INT;
  DECLARE v_Estado INT DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdEstadoPlan INTO v_Estado FROM TB_PLAN_INTERVENCION WHERE IdPlan = p_IdPlan;

  IF v_Estado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El plan indicado no existe.';
  END IF;

  IF v_Estado = 3 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No se agregan estrategias a un plan finalizado.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_ESTRATEGIA (IdPlan, Descripcion, Orden)
  VALUES (p_IdPlan, p_Descripcion, p_Orden);

  SET v_IdEstrategia = LAST_INSERT_ID();

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ESTRATEGIA', v_IdEstrategia, 'Crear',
          JSON_OBJECT('IdPlan', p_IdPlan, 'Descripcion', p_Descripcion));

  COMMIT;

  SELECT v_IdEstrategia AS IdEstrategia;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Actividad_Agregar;
DELIMITER $$
CREATE PROCEDURE SP_Actividad_Agregar(
  IN p_IdUsuarioAccion INT,
  IN p_IdEstrategia INT,
  IN p_Nombre VARCHAR(150),
  IN p_Descripcion VARCHAR(500)
)
BEGIN
  DECLARE v_IdActividad INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTRATEGIA WHERE IdEstrategia = p_IdEstrategia) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La estrategia indicada no existe.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_ACTIVIDAD (IdEstrategia, Nombre, Descripcion)
  VALUES (p_IdEstrategia, p_Nombre, p_Descripcion);

  SET v_IdActividad = LAST_INSERT_ID();

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_ACTIVIDAD', v_IdActividad, 'Crear',
          JSON_OBJECT('IdEstrategia', p_IdEstrategia, 'Nombre', p_Nombre));

  COMMIT;

  SELECT v_IdActividad AS IdActividad;
END$$
DELIMITER ;


-- SP_Plan_Eliminar: no se escribe. TB_PLAN_INTERVENCION no tiene columna Activo
-- y la regla del equipo prohibe el borrado fisico. Falta decidir con el DBA.

-- ============================================================
-- 7. Pagos y abonos
-- ============================================================

-- Ids de TB_ESTADO_PAGO: 1 Pagado, 2 Pendiente, 3 Vencido, 4 Parcial, 5 Anulado.
--
-- El estado del cobro no se escribe a mano desde ningun lado: lo recalcula
-- SP_Pago_RecalcularEstado cada vez que cambian los abonos.


-- Recalcula el estado de un cobro a partir de sus abonos activos.
-- Sin abonos queda Pendiente, o Vencido si ya paso la fecha; con abonos por
-- debajo del monto queda Parcial; completo queda Pagado. Un cobro anulado no
-- se toca.
DROP PROCEDURE IF EXISTS SP_Pago_RecalcularEstado;
DELIMITER $$
CREATE PROCEDURE SP_Pago_RecalcularEstado(
  IN p_IdPago INT
)
BEGIN
  DECLARE v_Monto DECIMAL(10,2) DEFAULT 0;
  DECLARE v_Abonado DECIMAL(10,2) DEFAULT 0;
  DECLARE v_Vencimiento DATE DEFAULT NULL;
  DECLARE v_Estado INT DEFAULT NULL;
  DECLARE v_Nuevo INT;

  SELECT Monto, FechaVencimiento, IdEstadoPago
    INTO v_Monto, v_Vencimiento, v_Estado
  FROM TB_PAGO
  WHERE IdPago = p_IdPago;

  IF v_Estado IS NOT NULL AND v_Estado <> 5 THEN
    SELECT IFNULL(SUM(Monto), 0) INTO v_Abonado
    FROM TB_ABONO
    WHERE IdPago = p_IdPago AND Activo = 1;

    IF v_Abonado >= v_Monto THEN
      SET v_Nuevo = 1;
    ELSEIF v_Abonado > 0 THEN
      SET v_Nuevo = 4;
    ELSEIF v_Vencimiento IS NOT NULL AND v_Vencimiento < CURDATE() THEN
      SET v_Nuevo = 3;
    ELSE
      SET v_Nuevo = 2;
    END IF;

    UPDATE TB_PAGO
    SET IdEstadoPago = v_Nuevo,
        FechaModificacion = NOW()
    WHERE IdPago = p_IdPago AND IdEstadoPago <> v_Nuevo;
  END IF;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Pago_Crear;
DELIMITER $$
CREATE PROCEDURE SP_Pago_Crear(
  IN p_IdUsuarioAccion INT,
  IN p_IdEstudiante INT,
  IN p_Concepto VARCHAR(200),
  IN p_Monto DECIMAL(10,2),
  IN p_FechaEmision DATE,
  IN p_FechaVencimiento DATE,
  IN p_Observaciones VARCHAR(1000)
)
BEGIN
  DECLARE v_IdPago INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF p_Monto IS NULL OR p_Monto <= 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El monto del cobro tiene que ser mayor que cero.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE WHERE IdEstudiante = p_IdEstudiante AND Activo = 1) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante indicado no existe o esta inactivo.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_PAGO (IdEstudiante, IdEstadoPago, IdUsuarioRegistro, Concepto, Monto,
                       FechaEmision, FechaVencimiento, Observaciones)
  VALUES (p_IdEstudiante, 2, p_IdUsuarioAccion, p_Concepto, p_Monto,
          IFNULL(p_FechaEmision, CURDATE()), p_FechaVencimiento, p_Observaciones);

  SET v_IdPago = LAST_INSERT_ID();

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_PAGO', v_IdPago, 'Crear',
          JSON_OBJECT('IdEstudiante', p_IdEstudiante, 'Concepto', p_Concepto, 'Monto', p_Monto));

  COMMIT;

  CALL SP_Pago_RecalcularEstado(v_IdPago);

  SELECT v_IdPago AS IdPago;
END$$
DELIMITER ;


-- Registrar un abono. Aqui viven las tres reglas que una restriccion CHECK no
-- puede validar: que la suma no pase del monto, que quien paga este asociado al
-- estudiante, y el estado resultante del cobro.
DROP PROCEDURE IF EXISTS SP_Pago_RegistrarAbono;
DELIMITER $$
CREATE PROCEDURE SP_Pago_RegistrarAbono(
  IN p_IdUsuarioAccion INT,
  IN p_IdPago INT,
  IN p_IdEncargadoPagador INT,
  IN p_IdMetodoPago INT,
  IN p_Monto DECIMAL(10,2),
  IN p_FechaAbono DATE,
  IN p_Referencia VARCHAR(100),
  IN p_Observaciones VARCHAR(1000)
)
BEGIN
  DECLARE v_IdAbono INT;
  DECLARE v_IdEstudiante INT DEFAULT NULL;
  DECLARE v_Estado INT DEFAULT NULL;
  DECLARE v_Monto DECIMAL(10,2) DEFAULT 0;
  DECLARE v_Abonado DECIMAL(10,2) DEFAULT 0;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF p_Monto IS NULL OR p_Monto <= 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El monto del abono tiene que ser mayor que cero.';
  END IF;

  SELECT IdEstudiante, IdEstadoPago, Monto
    INTO v_IdEstudiante, v_Estado, v_Monto
  FROM TB_PAGO
  WHERE IdPago = p_IdPago;

  IF v_IdEstudiante IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cobro indicado no existe.';
  END IF;

  IF v_Estado = 5 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No se abona sobre un cobro anulado.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO
                 WHERE IdEstudiante = v_IdEstudiante AND IdEncargado = p_IdEncargadoPagador) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese encargado no esta asociado al estudiante del cobro.';
  END IF;

  SELECT IFNULL(SUM(Monto), 0) INTO v_Abonado
  FROM TB_ABONO
  WHERE IdPago = p_IdPago AND Activo = 1;

  IF v_Abonado + p_Monto > v_Monto THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El abono supera el saldo pendiente del cobro.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_ABONO (IdPago, IdEncargadoPagador, IdMetodoPago, IdUsuarioRegistro,
                        Monto, FechaAbono, Referencia, Observaciones)
  VALUES (p_IdPago, p_IdEncargadoPagador, p_IdMetodoPago, p_IdUsuarioAccion,
          p_Monto, IFNULL(p_FechaAbono, CURDATE()), p_Referencia, p_Observaciones);

  SET v_IdAbono = LAST_INSERT_ID();

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_PAGO', p_IdPago, 'Editar',
          JSON_OBJECT('Abono', v_IdAbono, 'Monto', p_Monto, 'IdEncargadoPagador', p_IdEncargadoPagador));

  COMMIT;

  CALL SP_Pago_RecalcularEstado(p_IdPago);

  SELECT v_IdAbono AS IdAbono;
END$$
DELIMITER ;


-- Anular un abono no lo borra: lo desactiva y recalcula el cobro.
DROP PROCEDURE IF EXISTS SP_Pago_AnularAbono;
DELIMITER $$
CREATE PROCEDURE SP_Pago_AnularAbono(
  IN p_IdUsuarioAccion INT,
  IN p_IdAbono INT,
  IN p_Motivo VARCHAR(500)
)
BEGIN
  DECLARE v_IdPago INT DEFAULT NULL;
  DECLARE v_Activo BOOLEAN DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT IdPago, Activo INTO v_IdPago, v_Activo FROM TB_ABONO WHERE IdAbono = p_IdAbono;

  IF v_IdPago IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El abono indicado no existe.';
  END IF;

  IF v_Activo = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese abono ya estaba anulado.';
  END IF;

  START TRANSACTION;

  UPDATE TB_ABONO
  SET Activo = 0,
      Observaciones = CONCAT_WS(' | ', Observaciones, CONCAT('Anulado: ', IFNULL(p_Motivo, 'sin motivo')))
  WHERE IdAbono = p_IdAbono;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_PAGO', v_IdPago, 'Editar',
          JSON_OBJECT('Abono', p_IdAbono, 'Activo', 1),
          JSON_OBJECT('Abono', p_IdAbono, 'Activo', 0, 'Motivo', p_Motivo));

  COMMIT;

  CALL SP_Pago_RecalcularEstado(v_IdPago);
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Pago_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Pago_Listar(
  IN p_IdEstudiante INT,
  IN p_IdEstadoPago INT,
  IN p_Desde DATE,
  IN p_Hasta DATE
)
BEGIN
  SELECT p.IdPago,
         p.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         p.Concepto,
         p.Monto,
         IFNULL((SELECT SUM(a.Monto) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1), 0) AS Abonado,
         p.Monto - IFNULL((SELECT SUM(a.Monto) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1), 0) AS Saldo,
         p.FechaEmision,
         p.FechaVencimiento,
         ep.Nombre AS Estado
  FROM TB_PAGO p
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = p.IdEstudiante
  JOIN TB_ESTADO_PAGO ep ON ep.IdEstadoPago = p.IdEstadoPago
  WHERE (p_IdEstudiante IS NULL OR p.IdEstudiante = p_IdEstudiante)
    AND (p_IdEstadoPago IS NULL OR p.IdEstadoPago = p_IdEstadoPago)
    AND (p_Desde IS NULL OR p.FechaEmision >= p_Desde)
    AND (p_Hasta IS NULL OR p.FechaEmision <= p_Hasta)
  ORDER BY p.FechaEmision DESC, p.IdPago DESC;
END$$
DELIMITER ;


-- Estado de cuenta de un estudiante: el resumen y el detalle de cada cobro.
DROP PROCEDURE IF EXISTS SP_Pago_ObtenerEstadoCuenta;
DELIMITER $$
CREATE PROCEDURE SP_Pago_ObtenerEstadoCuenta(
  IN p_IdEstudiante INT
)
BEGIN
  SELECT IFNULL(SUM(p.Monto), 0) AS TotalCobrado,
         IFNULL(SUM((SELECT IFNULL(SUM(a.Monto), 0) FROM TB_ABONO a
                     WHERE a.IdPago = p.IdPago AND a.Activo = 1)), 0) AS TotalAbonado,
         IFNULL(SUM(p.Monto), 0) - IFNULL(SUM((SELECT IFNULL(SUM(a.Monto), 0) FROM TB_ABONO a
                     WHERE a.IdPago = p.IdPago AND a.Activo = 1)), 0) AS SaldoTotal
  FROM TB_PAGO p
  WHERE p.IdEstudiante = p_IdEstudiante
    AND p.IdEstadoPago <> 5;

  SELECT p.IdPago, p.Concepto, p.Monto,
         IFNULL((SELECT SUM(a.Monto) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1), 0) AS Abonado,
         p.FechaEmision, p.FechaVencimiento,
         ep.Nombre AS Estado
  FROM TB_PAGO p
  JOIN TB_ESTADO_PAGO ep ON ep.IdEstadoPago = p.IdEstadoPago
  WHERE p.IdEstudiante = p_IdEstudiante
  ORDER BY p.FechaEmision DESC;
END$$
DELIMITER ;


-- Pasa a Vencido lo que ya vencio y sigue sin abonos. Pensado para correr una
-- vez al dia; tambien se puede llamar a mano.
DROP PROCEDURE IF EXISTS SP_Pago_RecalcularVencidos;
DELIMITER $$
CREATE PROCEDURE SP_Pago_RecalcularVencidos()
BEGIN
  UPDATE TB_PAGO p
  SET p.IdEstadoPago = 3,
      p.FechaModificacion = NOW()
  WHERE p.IdEstadoPago = 2
    AND p.FechaVencimiento IS NOT NULL
    AND p.FechaVencimiento < CURDATE()
    AND NOT EXISTS (SELECT 1 FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1);

  SELECT ROW_COUNT() AS CobrosActualizados;
END$$
DELIMITER ;


-- SP_Pago_Anular: no se escribe. Falta decidir que pasa con los abonos ya
-- registrados cuando se anula el cobro.

-- ============================================================
-- 8. Reportes
-- ============================================================

DROP PROCEDURE IF EXISTS SP_Reporte_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Reporte_Listar(
  IN p_IdEstudiante INT,
  IN p_Desde DATE,
  IN p_Hasta DATE
)
BEGIN
  SELECT r.IdReporte,
         r.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         r.Titulo,
         r.PeriodoInicio,
         r.PeriodoFin,
         r.VisibleEnPortal,
         r.FechaGeneracion,
         u.NombreCompleto AS GeneradoPor
  FROM TB_REPORTE r
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = r.IdEstudiante
  JOIN TB_USUARIO u ON u.IdUsuario = r.IdUsuarioGenero
  WHERE (p_IdEstudiante IS NULL OR r.IdEstudiante = p_IdEstudiante)
    AND (p_Desde IS NULL OR r.PeriodoInicio >= p_Desde)
    AND (p_Hasta IS NULL OR r.PeriodoFin <= p_Hasta)
  ORDER BY r.FechaGeneracion DESC;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Reporte_ObtenerDetalle;
DELIMITER $$
CREATE PROCEDURE SP_Reporte_ObtenerDetalle(
  IN p_IdReporte INT
)
BEGIN
  SELECT r.IdReporte,
         r.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         r.Titulo, r.PeriodoInicio, r.PeriodoFin,
         r.Contenido, r.RutaArchivo, r.VisibleEnPortal, r.FechaGeneracion,
         u.NombreCompleto AS GeneradoPor
  FROM TB_REPORTE r
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = r.IdEstudiante
  JOIN TB_USUARIO u ON u.IdUsuario = r.IdUsuarioGenero
  WHERE r.IdReporte = p_IdReporte;
END$$
DELIMITER ;


-- SP_Reporte_Generar: no se escribe. TB_REPORTE tiene Contenido y RutaArchivo,
-- las dos opcionales, y nadie definio que lleva el reporte ni si se guarda como
-- texto o como archivo. Falta esa decision.

-- ============================================================
-- 9. Materiales y tareas
-- ============================================================

-- Ids de TB_ESTADO_TAREA: 1 Asignada, 2 Entregada, 3 Revisada, 4 Vencida.


DROP PROCEDURE IF EXISTS SP_Material_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Material_Listar(
  IN p_IdTipoMaterial INT,
  IN p_SoloPublicados BOOLEAN,
  IN p_Busqueda VARCHAR(150)
)
BEGIN
  SELECT m.IdMaterial,
         m.Titulo,
         m.Descripcion,
         tm.Nombre AS TipoMaterial,
         m.Url,
         m.RutaArchivo,
         m.Publicado,
         m.FechaPublicacion,
         u.NombreCompleto AS RegistradoPor
  FROM TB_MATERIAL m
  JOIN TB_TIPO_MATERIAL tm ON tm.IdTipoMaterial = m.IdTipoMaterial
  JOIN TB_USUARIO u ON u.IdUsuario = m.IdUsuarioRegistro
  WHERE (p_IdTipoMaterial IS NULL OR m.IdTipoMaterial = p_IdTipoMaterial)
    AND (p_SoloPublicados IS NULL OR p_SoloPublicados = 0 OR m.Publicado = 1)
    AND (p_Busqueda IS NULL OR p_Busqueda = '' OR m.Titulo LIKE CONCAT('%', p_Busqueda, '%'))
  ORDER BY m.FechaCreacion DESC;
END$$
DELIMITER ;


-- Crea la tarea y la asigna a varios estudiantes de una vez.
-- p_EstudiantesJson es un arreglo de ids, por ejemplo '[1,2]'.
DROP PROCEDURE IF EXISTS SP_Tarea_Crear;
DELIMITER $$
CREATE PROCEDURE SP_Tarea_Crear(
  IN p_IdUsuarioAccion INT,
  IN p_IdMaterial INT,
  IN p_Titulo VARCHAR(150),
  IN p_Descripcion VARCHAR(1000),
  IN p_FechaLimite DATE,
  IN p_EstudiantesJson VARCHAR(500)
)
BEGIN
  DECLARE v_IdTarea INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF p_EstudiantesJson IS NULL OR JSON_LENGTH(p_EstudiantesJson) = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Asigne la tarea a por lo menos un estudiante.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_TAREA (IdMaterial, IdUsuarioRegistro, Titulo, Descripcion, FechaLimite)
  VALUES (p_IdMaterial, p_IdUsuarioAccion, p_Titulo, p_Descripcion, p_FechaLimite);

  SET v_IdTarea = LAST_INSERT_ID();

  INSERT INTO TB_TAREA_ASIGNACION (IdTarea, IdEstudiante, IdEstadoTarea)
  SELECT v_IdTarea, j.IdEstudiante, 1
  FROM JSON_TABLE(p_EstudiantesJson, '$[*]' COLUMNS (IdEstudiante INT PATH '$')) AS j
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = j.IdEstudiante AND e.Activo = 1;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_TAREA', v_IdTarea, 'Crear',
          JSON_OBJECT('Titulo', p_Titulo, 'FechaLimite', p_FechaLimite,
                      'Estudiantes', JSON_LENGTH(p_EstudiantesJson)));

  COMMIT;

  SELECT v_IdTarea AS IdTarea;
END$$
DELIMITER ;


-- Edita la tarea y, si vienen estudiantes, rehace la asignacion. Las entregas
-- que ya existan de estudiantes que siguen asignados no se pierden.
DROP PROCEDURE IF EXISTS SP_Tarea_Editar;
DELIMITER $$
CREATE PROCEDURE SP_Tarea_Editar(
  IN p_IdUsuarioAccion INT,
  IN p_IdTarea INT,
  IN p_Titulo VARCHAR(150),
  IN p_Descripcion VARCHAR(1000),
  IN p_FechaLimite DATE,
  IN p_EstudiantesJson VARCHAR(500)
)
BEGIN
  DECLARE v_Anterior JSON DEFAULT NULL;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT JSON_OBJECT('Titulo', Titulo, 'FechaLimite', FechaLimite)
    INTO v_Anterior
  FROM TB_TAREA
  WHERE IdTarea = p_IdTarea;

  IF v_Anterior IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La tarea indicada no existe.';
  END IF;

  START TRANSACTION;

  UPDATE TB_TAREA
  SET Titulo = p_Titulo,
      Descripcion = p_Descripcion,
      FechaLimite = p_FechaLimite
  WHERE IdTarea = p_IdTarea;

  IF p_EstudiantesJson IS NOT NULL AND JSON_LENGTH(p_EstudiantesJson) > 0 THEN
    DELETE FROM TB_TAREA_ASIGNACION
    WHERE IdTarea = p_IdTarea
      AND IdEstadoTarea = 1
      AND IdEstudiante NOT IN (
        SELECT j.IdEstudiante
        FROM JSON_TABLE(p_EstudiantesJson, '$[*]' COLUMNS (IdEstudiante INT PATH '$')) AS j);

    INSERT IGNORE INTO TB_TAREA_ASIGNACION (IdTarea, IdEstudiante, IdEstadoTarea)
    SELECT p_IdTarea, j.IdEstudiante, 1
    FROM JSON_TABLE(p_EstudiantesJson, '$[*]' COLUMNS (IdEstudiante INT PATH '$')) AS j
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = j.IdEstudiante AND e.Activo = 1;
  END IF;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_TAREA', p_IdTarea, 'Editar', v_Anterior,
          JSON_OBJECT('Titulo', p_Titulo, 'FechaLimite', p_FechaLimite));

  COMMIT;
END$$
DELIMITER ;


-- Entrega desde el portal. Recibe el usuario autenticado y comprueba que el
-- estudiante este a su cargo: si se confiara en el id que manda el navegador,
-- cualquier encargado podria entregar por otro.
DROP PROCEDURE IF EXISTS SP_TareaAsignacion_Entregar;
DELIMITER $$
CREATE PROCEDURE SP_TareaAsignacion_Entregar(
  IN p_IdUsuario INT,
  IN p_IdTareaAsignacion INT,
  IN p_Comentario VARCHAR(500)
)
BEGIN
  DECLARE v_IdEstudiante INT DEFAULT NULL;

  SELECT ta.IdEstudiante INTO v_IdEstudiante
  FROM TB_TAREA_ASIGNACION ta
  JOIN TB_ESTUDIANTE_ENCARGADO ee ON ee.IdEstudiante = ta.IdEstudiante
  JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
  WHERE ta.IdTareaAsignacion = p_IdTareaAsignacion
    AND u.IdUsuario = p_IdUsuario
  LIMIT 1;

  IF v_IdEstudiante IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La tarea no existe o no corresponde a un estudiante a su cargo.';
  END IF;

  UPDATE TB_TAREA_ASIGNACION
  SET IdEstadoTarea = 2,
      FechaEntrega = NOW(),
      ComentarioEstudiante = p_Comentario
  WHERE IdTareaAsignacion = p_IdTareaAsignacion;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_TareaAsignacion_Revisar;
DELIMITER $$
CREATE PROCEDURE SP_TareaAsignacion_Revisar(
  IN p_IdTareaAsignacion INT,
  IN p_Comentario VARCHAR(500)
)
BEGIN
  IF NOT EXISTS (SELECT 1 FROM TB_TAREA_ASIGNACION WHERE IdTareaAsignacion = p_IdTareaAsignacion) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La asignacion indicada no existe.';
  END IF;

  UPDATE TB_TAREA_ASIGNACION
  SET IdEstadoTarea = 3,
      ComentarioRevision = p_Comentario
  WHERE IdTareaAsignacion = p_IdTareaAsignacion;
END$$
DELIMITER ;

-- ============================================================
-- 10. Comunicacion
-- ============================================================

-- Bandeja de un usuario: un renglon por hilo, con el ultimo mensaje y cuantos
-- quedan sin leer. El hilo es el mensaje raiz; las respuestas cuelgan de el.
DROP PROCEDURE IF EXISTS SP_Comunicacion_ListarHilos;
DELIMITER $$
CREATE PROCEDURE SP_Comunicacion_ListarHilos(
  IN p_IdUsuario INT
)
BEGIN
  SELECT raiz.IdMensaje AS IdHilo,
         raiz.Asunto,
         otro.IdUsuario AS IdContraparte,
         otro.NombreCompleto AS Contraparte,
         ultimo.Cuerpo AS UltimoMensaje,
         ultimo.FechaEnvio AS UltimaFecha,
         (SELECT COUNT(*) FROM TB_MENSAJE m2
          WHERE (m2.IdMensaje = raiz.IdMensaje OR m2.IdMensajePadre = raiz.IdMensaje)
            AND m2.IdUsuarioDestinatario = p_IdUsuario
            AND m2.Leido = 0) AS SinLeer
  FROM TB_MENSAJE raiz
  JOIN TB_USUARIO otro
    ON otro.IdUsuario = IF(raiz.IdUsuarioRemitente = p_IdUsuario,
                           raiz.IdUsuarioDestinatario,
                           raiz.IdUsuarioRemitente)
  JOIN TB_MENSAJE ultimo
    ON ultimo.IdMensaje = (
         SELECT m3.IdMensaje FROM TB_MENSAJE m3
         WHERE m3.IdMensaje = raiz.IdMensaje OR m3.IdMensajePadre = raiz.IdMensaje
         ORDER BY m3.FechaEnvio DESC, m3.IdMensaje DESC
         LIMIT 1)
  WHERE raiz.IdMensajePadre IS NULL
    AND (raiz.IdUsuarioRemitente = p_IdUsuario OR raiz.IdUsuarioDestinatario = p_IdUsuario)
  ORDER BY ultimo.FechaEnvio DESC;
END$$
DELIMITER ;


-- Deja programados los recordatorios de las citas que se acercan.
-- Las horas de anticipacion salen de TB_CONFIGURACION, clave HorasRecordatorioCita.
-- Provisional: se manda por el primer canal del catalogo y solo al encargado
-- principal que tenga usuario. Falta confirmar canal y destinatarios.
DROP PROCEDURE IF EXISTS SP_Notificacion_ProgramarRecordatorios;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_ProgramarRecordatorios()
BEGIN
  DECLARE v_Horas INT DEFAULT 24;
  DECLARE v_IdCanal INT DEFAULT NULL;

  SELECT CAST(Valor AS UNSIGNED) INTO v_Horas
  FROM TB_CONFIGURACION
  WHERE Clave = 'HorasRecordatorioCita';

  SET v_Horas = IFNULL(v_Horas, 24);

  SELECT MIN(IdCanalNotificacion) INTO v_IdCanal FROM TB_CANAL_NOTIFICACION;

  IF v_IdCanal IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No hay canales de notificacion configurados.';
  END IF;

  INSERT INTO TB_NOTIFICACION (IdCita, IdUsuarioDestinatario, IdCanalNotificacion,
                               Asunto, Cuerpo, FechaProgramada)
  SELECT c.IdCita,
         u.IdUsuario,
         v_IdCanal,
         'Recordatorio de cita',
         CONCAT('Le recordamos la cita de ',
                TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)),
                ' el ', DATE_FORMAT(c.FechaHoraInicio, '%d/%m/%Y a las %H:%i'), '.'),
         c.FechaHoraInicio - INTERVAL v_Horas HOUR
  FROM TB_CITA c
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = c.IdEstudiante
  JOIN TB_ESTUDIANTE_ENCARGADO ee ON ee.IdEstudiante = c.IdEstudiante AND ee.EsPrincipal = 1
  JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado AND u.IdEstadoUsuario = 1
  WHERE c.IdEstadoCita IN (1, 2)
    AND c.FechaHoraInicio > NOW()
    AND c.FechaHoraInicio - INTERVAL v_Horas HOUR <= NOW() + INTERVAL 1 DAY
    AND NOT EXISTS (SELECT 1 FROM TB_NOTIFICACION n
                    WHERE n.IdCita = c.IdCita AND n.IdUsuarioDestinatario = u.IdUsuario);

  SELECT ROW_COUNT() AS RecordatoriosProgramados;
END$$
DELIMITER ;


-- Lo que el envio tiene que despachar: ya le llego la hora y sigue sin enviarse.
DROP PROCEDURE IF EXISTS SP_Notificacion_ObtenerPendientes;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_ObtenerPendientes()
BEGIN
  SELECT n.IdNotificacion,
         n.IdCita,
         n.IdUsuarioDestinatario,
         u.NombreCompleto AS Destinatario,
         u.Correo,
         cn.Nombre AS Canal,
         n.Asunto,
         n.Cuerpo,
         n.FechaProgramada
  FROM TB_NOTIFICACION n
  JOIN TB_USUARIO u ON u.IdUsuario = n.IdUsuarioDestinatario
  JOIN TB_CANAL_NOTIFICACION cn ON cn.IdCanalNotificacion = n.IdCanalNotificacion
  WHERE n.Enviada = 0
    AND n.FechaProgramada <= NOW()
  ORDER BY n.FechaProgramada;
END$$
DELIMITER ;

-- ============================================================
-- 11. Dashboard
-- ============================================================

-- Todo lo que muestra el tablero, en una sola llamada: contadores, proximas
-- citas y actividad reciente. Son tres resultados seguidos.
DROP PROCEDURE IF EXISTS SP_Dashboard_ObtenerResumen;
DELIMITER $$
CREATE PROCEDURE SP_Dashboard_ObtenerResumen()
BEGIN
  SELECT
    (SELECT COUNT(*) FROM TB_ESTUDIANTE WHERE Activo = 1) AS EstudiantesActivos,
    (SELECT COUNT(*) FROM TB_ENCARGADO WHERE Activo = 1) AS ClientesActivos,
    (SELECT COUNT(*) FROM TB_CITA
      WHERE IdEstadoCita IN (1, 2)
        AND FechaHoraInicio >= CURDATE()
        AND FechaHoraInicio < CURDATE() + INTERVAL 7 DAY) AS CitasProximaSemana,
    (SELECT COUNT(*) FROM TB_SESION WHERE Fecha >= CURDATE() - INTERVAL 30 DAY) AS SesionesUltimoMes,
    (SELECT COUNT(*) FROM TB_PLAN_INTERVENCION WHERE IdEstadoPlan = 2) AS PlanesActivos,
    (SELECT IFNULL(SUM(p.Monto), 0) - IFNULL(SUM(
       (SELECT IFNULL(SUM(a.Monto), 0) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1)
     ), 0) FROM TB_PAGO p WHERE p.IdEstadoPago IN (2, 3, 4)) AS SaldoPorCobrar,
    (SELECT COUNT(*) FROM TB_PAGO WHERE IdEstadoPago = 3) AS CobrosVencidos;

  SELECT c.IdCita,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         c.FechaHoraInicio,
         c.FechaHoraFin,
         ts.Nombre AS TipoSesion,
         ec.Nombre AS Estado,
         ec.ColorHex
  FROM TB_CITA c
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = c.IdEstudiante
  JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = c.IdTipoSesion
  JOIN TB_ESTADO_CITA ec ON ec.IdEstadoCita = c.IdEstadoCita
  WHERE c.IdEstadoCita IN (1, 2)
    AND c.FechaHoraInicio >= NOW()
  ORDER BY c.FechaHoraInicio
  LIMIT 10;

  SELECT b.IdBitacora,
         b.Entidad,
         b.IdRegistro,
         b.Accion,
         b.FechaHora,
         u.NombreCompleto AS Usuario
  FROM TB_BITACORA b
  JOIN TB_USUARIO u ON u.IdUsuario = b.IdUsuario
  ORDER BY b.FechaHora DESC, b.IdBitacora DESC
  LIMIT 10;
END$$
DELIMITER ;

-- ============================================================
-- 12. Portal de encargados
-- ============================================================

-- REGLA DE SEGURIDAD DE TODA ESTA SECCION
--
-- Los cinco reciben el identificador del usuario autenticado, nunca el del
-- estudiante que venga del navegador por si solo. Dentro se comprueba contra
-- TB_ESTUDIANTE_ENCARGADO que ese estudiante este realmente a cargo de ese
-- encargado. Si se confiara en el dato del cliente, cualquier encargado podria
-- ver los datos de otro cambiando un numero en la direccion.


-- Resumen del estudiante seleccionado, mas la lista de estudiantes a cargo
-- para el selector de la barra superior.
DROP PROCEDURE IF EXISTS SP_Portal_ObtenerInicio;
DELIMITER $$
CREATE PROCEDURE SP_Portal_ObtenerInicio(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT
)
BEGIN
  DECLARE v_IdEstudiante INT DEFAULT NULL;

  -- Si no se pide uno en concreto, se toma el primero a cargo
  SELECT ee.IdEstudiante INTO v_IdEstudiante
  FROM TB_ESTUDIANTE_ENCARGADO ee
  JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante AND e.Activo = 1
  WHERE u.IdUsuario = p_IdUsuario
    AND (p_IdEstudiante IS NULL OR ee.IdEstudiante = p_IdEstudiante)
  ORDER BY ee.EsPrincipal DESC, ee.IdEstudiante
  LIMIT 1;

  IF v_IdEstudiante IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No tiene estudiantes a su cargo o ese estudiante no le corresponde.';
  END IF;

  SELECT ee.IdEstudiante,
         TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
         ee.EsPrincipal
  FROM TB_ESTUDIANTE_ENCARGADO ee
  JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante AND e.Activo = 1
  WHERE u.IdUsuario = p_IdUsuario
  ORDER BY ee.EsPrincipal DESC, e.Nombre;

  SELECT v_IdEstudiante AS IdEstudiante,
    (SELECT COUNT(*) FROM TB_CITA
      WHERE IdEstudiante = v_IdEstudiante AND IdEstadoCita IN (1, 2)
        AND FechaHoraInicio >= NOW()) AS CitasProximas,
    (SELECT COUNT(*) FROM TB_TAREA_ASIGNACION
      WHERE IdEstudiante = v_IdEstudiante AND IdEstadoTarea = 1) AS TareasPendientes,
    (SELECT COUNT(*) FROM TB_REPORTE
      WHERE IdEstudiante = v_IdEstudiante AND VisibleEnPortal = 1) AS ReportesDisponibles,
    (SELECT IFNULL(SUM(p.Monto), 0) - IFNULL(SUM(
       (SELECT IFNULL(SUM(a.Monto), 0) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1)
     ), 0) FROM TB_PAGO p
      WHERE p.IdEstudiante = v_IdEstudiante AND p.IdEstadoPago IN (2, 3, 4)) AS SaldoPendiente;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Portal_ListarCitas;
DELIMITER $$
CREATE PROCEDURE SP_Portal_ListarCitas(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT
)
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    WHERE u.IdUsuario = p_IdUsuario AND ee.IdEstudiante = p_IdEstudiante) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese estudiante no esta a su cargo.';
  END IF;

  SELECT c.IdCita,
         c.FechaHoraInicio,
         c.FechaHoraFin,
         ts.Nombre AS TipoSesion,
         m.Nombre AS Modalidad,
         ec.Nombre AS Estado,
         ec.ColorHex
  FROM TB_CITA c
  JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = c.IdTipoSesion
  JOIN TB_MODALIDAD m ON m.IdModalidad = c.IdModalidad
  JOIN TB_ESTADO_CITA ec ON ec.IdEstadoCita = c.IdEstadoCita
  WHERE c.IdEstudiante = p_IdEstudiante
    AND c.FechaHoraInicio >= CURDATE()
  ORDER BY c.FechaHoraInicio;
END$$
DELIMITER ;


-- Progreso: las sesiones ya realizadas y los reportes marcados como visibles.
-- Un reporte que no este publicado no sale, aunque exista.
DROP PROCEDURE IF EXISTS SP_Portal_ListarProgreso;
DELIMITER $$
CREATE PROCEDURE SP_Portal_ListarProgreso(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT
)
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    WHERE u.IdUsuario = p_IdUsuario AND ee.IdEstudiante = p_IdEstudiante) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese estudiante no esta a su cargo.';
  END IF;

  SELECT s.IdSesion, s.Fecha, ta.Nombre AS TipoAtencion,
         s.TemaTrabajado, s.Avances, s.Recomendaciones
  FROM TB_SESION s
  JOIN TB_TIPO_ATENCION ta ON ta.IdTipoAtencion = s.IdTipoAtencion
  WHERE s.IdEstudiante = p_IdEstudiante
  ORDER BY s.Fecha DESC;

  SELECT r.IdReporte, r.Titulo, r.PeriodoInicio, r.PeriodoFin, r.FechaGeneracion
  FROM TB_REPORTE r
  WHERE r.IdEstudiante = p_IdEstudiante
    AND r.VisibleEnPortal = 1
  ORDER BY r.FechaGeneracion DESC;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Portal_ListarTareas;
DELIMITER $$
CREATE PROCEDURE SP_Portal_ListarTareas(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT
)
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    WHERE u.IdUsuario = p_IdUsuario AND ee.IdEstudiante = p_IdEstudiante) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese estudiante no esta a su cargo.';
  END IF;

  SELECT ta.IdTareaAsignacion,
         t.IdTarea,
         t.Titulo,
         t.Descripcion,
         t.FechaLimite,
         et.Nombre AS Estado,
         ta.FechaEntrega,
         ta.ComentarioEstudiante,
         ta.ComentarioRevision,
         m.Titulo AS Material,
         m.Url AS MaterialUrl
  FROM TB_TAREA_ASIGNACION ta
  JOIN TB_TAREA t ON t.IdTarea = ta.IdTarea
  JOIN TB_ESTADO_TAREA et ON et.IdEstadoTarea = ta.IdEstadoTarea
  LEFT JOIN TB_MATERIAL m ON m.IdMaterial = t.IdMaterial
  WHERE ta.IdEstudiante = p_IdEstudiante
  ORDER BY t.FechaLimite;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Portal_ListarPagos;
DELIMITER $$
CREATE PROCEDURE SP_Portal_ListarPagos(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT
)
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    WHERE u.IdUsuario = p_IdUsuario AND ee.IdEstudiante = p_IdEstudiante) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese estudiante no esta a su cargo.';
  END IF;

  SELECT p.IdPago,
         p.Concepto,
         p.Monto,
         IFNULL((SELECT SUM(a.Monto) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1), 0) AS Abonado,
         p.Monto - IFNULL((SELECT SUM(a.Monto) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1), 0) AS Saldo,
         p.FechaEmision,
         p.FechaVencimiento,
         ep.Nombre AS Estado
  FROM TB_PAGO p
  JOIN TB_ESTADO_PAGO ep ON ep.IdEstadoPago = p.IdEstadoPago
  WHERE p.IdEstudiante = p_IdEstudiante
    AND p.IdEstadoPago <> 5
  ORDER BY p.FechaEmision DESC;
END$$
DELIMITER ;

-- ============================================================
-- 13. Sitio publico
-- ============================================================

DROP PROCEDURE IF EXISTS SP_ListarServiciosActivos_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ListarServiciosActivos_CRM()
BEGIN
    SELECT
        IdServicio,
        Nombre,
        Descripcion,
        PrecioReferencia,
        Activo
    FROM TB_SERVICIO
    WHERE Activo = 1
    ORDER BY Nombre;
END$$
DELIMITER ;

-- ============================================================
-- Solicitudes de contacto (formulario publico)
-- ============================================================

-- Alta desde el sitio publico. No hay usuario, asi que no va a bitacora.
-- Limite simple anti-abuso: maximo 5 solicitudes por IP por hora.
DROP PROCEDURE IF EXISTS SP_RegistrarSolicitudContacto_CRM;
DELIMITER $$
CREATE PROCEDURE SP_RegistrarSolicitudContacto_CRM(
    IN p_Nombre            VARCHAR(100),
    IN p_Apellido          VARCHAR(100),
    IN p_Telefono          VARCHAR(15),
    IN p_Correo            VARCHAR(150),
    IN p_IdServicioInteres INT,
    IN p_Mensaje           VARCHAR(2000),
    IN p_DireccionIp       VARCHAR(45)
)
BEGIN
    IF p_DireccionIp IS NOT NULL AND (
        SELECT COUNT(*) FROM TB_SOLICITUD_CONTACTO
        WHERE DireccionIp = p_DireccionIp
          AND FechaRegistro > NOW() - INTERVAL 1 HOUR) >= 5 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Se recibieron demasiadas solicitudes. Intente de nuevo más tarde.';
    END IF;

    -- Un servicio inexistente o inactivo no bloquea el mensaje: queda sin servicio
    INSERT INTO TB_SOLICITUD_CONTACTO
        (IdServicioInteres, Nombre, Apellido, Telefono, Correo, Mensaje, DireccionIp)
    VALUES (
        (SELECT IdServicio FROM TB_SERVICIO WHERE IdServicio = p_IdServicioInteres AND Activo = 1),
        TRIM(p_Nombre), TRIM(p_Apellido), p_Telefono, TRIM(p_Correo),
        TRIM(p_Mensaje), p_DireccionIp);

    SELECT LAST_INSERT_ID() AS IdSolicitud;
END$$
DELIMITER ;


-- Lista para la pestaña Solicitudes (se filtra por estado en pantalla)
DROP PROCEDURE IF EXISTS SP_ConsultarSolicitudes_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ConsultarSolicitudes_CRM()
BEGIN
    SELECT
      sc.IdSolicitud,
      CONCAT(sc.Nombre, ' ', sc.Apellido) AS NombreCompleto,
      sc.Telefono,
      sc.Correo,
      s.Nombre AS Servicio,
      sc.IdEstadoSolicitud,
      es.Nombre AS Estado,
      sc.IdEncargado,
      u.NombreCompleto AS AtendidaPor,
      sc.FechaRegistro,
      sc.FechaAtencion
    FROM TB_SOLICITUD_CONTACTO sc
    JOIN TB_ESTADO_SOLICITUD es ON es.IdEstadoSolicitud = sc.IdEstadoSolicitud
    LEFT JOIN TB_SERVICIO s ON s.IdServicio = sc.IdServicioInteres
    LEFT JOIN TB_USUARIO u ON u.IdUsuario = sc.IdUsuarioAtiende
    ORDER BY (sc.IdEstadoSolicitud = 1) DESC, sc.FechaRegistro DESC;
END$$
DELIMITER ;


-- Detalle de una solicitud. Dos resultados:
--   1) la solicitud
--   2) clientes existentes con el mismo telefono o correo (posibles duplicados)
DROP PROCEDURE IF EXISTS SP_ObtenerSolicitud_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ObtenerSolicitud_CRM(
    IN p_IdSolicitud INT
)
BEGIN
    DECLARE v_Telefono VARCHAR(15) DEFAULT NULL;
    DECLARE v_Correo   VARCHAR(150) DEFAULT NULL;

    SELECT Telefono, Correo INTO v_Telefono, v_Correo
    FROM TB_SOLICITUD_CONTACTO
    WHERE IdSolicitud = p_IdSolicitud;

    SELECT
      sc.IdSolicitud,
      sc.Nombre,
      sc.Apellido,
      sc.Telefono,
      sc.Correo,
      sc.IdServicioInteres,
      s.Nombre AS Servicio,
      sc.Mensaje,
      sc.NotaInterna,
      sc.IdEstadoSolicitud,
      es.Nombre AS Estado,
      sc.IdEncargado,
      CONCAT(e.Nombre, ' ', e.PrimerApellido) AS ClienteVinculado,
      u.NombreCompleto AS AtendidaPor,
      sc.FechaRegistro,
      sc.FechaAtencion
    FROM TB_SOLICITUD_CONTACTO sc
    JOIN TB_ESTADO_SOLICITUD es ON es.IdEstadoSolicitud = sc.IdEstadoSolicitud
    LEFT JOIN TB_SERVICIO s ON s.IdServicio = sc.IdServicioInteres
    LEFT JOIN TB_ENCARGADO e ON e.IdEncargado = sc.IdEncargado
    LEFT JOIN TB_USUARIO u ON u.IdUsuario = sc.IdUsuarioAtiende
    WHERE sc.IdSolicitud = p_IdSolicitud;

    SELECT
      e.IdEncargado AS Id,
      CONCAT(e.Nombre, ' ', e.PrimerApellido) AS Encargado,
      t.Numero AS Telefono,
      e.Correo,
      IF(e.Activo = 1, ec.Nombre, 'Inactivo') AS Estado
    FROM TB_ENCARGADO e
    JOIN TB_ESTADO_CLIENTE ec ON ec.IdEstadoCliente = e.IdEstadoCliente
    LEFT JOIN TB_TELEFONO t ON t.IdEncargado = e.IdEncargado AND t.EsPrincipal = 1
    WHERE EXISTS (SELECT 1 FROM TB_TELEFONO t2
                  WHERE t2.IdEncargado = e.IdEncargado AND t2.Numero = v_Telefono)
       OR (v_Correo IS NOT NULL AND e.Correo = v_Correo)
    ORDER BY e.Activo DESC, e.FechaRegistro DESC;
END$$
DELIMITER ;


-- Marca Pendiente / Contactada / Descartada y guarda la nota interna.
-- "Convertida" no se pone aqui: solo se llega convirtiendo o vinculando.
DROP PROCEDURE IF EXISTS SP_CambiarEstadoSolicitud_CRM;
DELIMITER $$
CREATE PROCEDURE SP_CambiarEstadoSolicitud_CRM(
    IN p_IdUsuarioAccion   INT,
    IN p_IdSolicitud       INT,
    IN p_IdEstadoSolicitud INT,
    IN p_NotaInterna       VARCHAR(1000)
)
BEGIN
    DECLARE v_Anterior INT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT IdEstadoSolicitud INTO v_Anterior
    FROM TB_SOLICITUD_CONTACTO
    WHERE IdSolicitud = p_IdSolicitud;

    IF v_Anterior IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La solicitud indicada no existe.';
    END IF;

    IF v_Anterior = 3 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La solicitud ya fue convertida en cliente.';
    END IF;

    IF p_IdEstadoSolicitud NOT IN (1, 2, 4) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Estado no permitido para esta acción.';
    END IF;

    START TRANSACTION;

    UPDATE TB_SOLICITUD_CONTACTO
    SET IdEstadoSolicitud = p_IdEstadoSolicitud,
        NotaInterna = NULLIF(TRIM(p_NotaInterna), ''),
        IdUsuarioAtiende = IF(p_IdEstadoSolicitud = 1, IdUsuarioAtiende, p_IdUsuarioAccion),
        FechaAtencion = IF(p_IdEstadoSolicitud = 1, FechaAtencion, NOW())
    WHERE IdSolicitud = p_IdSolicitud;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_SOLICITUD_CONTACTO', p_IdSolicitud, 'CambiarEstado',
            JSON_OBJECT('IdEstadoSolicitud', v_Anterior),
            JSON_OBJECT('IdEstadoSolicitud', p_IdEstadoSolicitud));

    COMMIT;
END$$
DELIMITER ;


-- Enlaza la solicitud a un cliente que ya existia (caso duplicado).
-- El mensaje se agrega a las observaciones del cliente para no perderlo.
DROP PROCEDURE IF EXISTS SP_VincularSolicitudCliente_CRM;
DELIMITER $$
CREATE PROCEDURE SP_VincularSolicitudCliente_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdSolicitud     INT,
    IN p_IdEncargado     INT
)
BEGIN
    DECLARE v_Mensaje VARCHAR(2000) DEFAULT NULL;
    DECLARE v_Fecha   DATETIME DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT Mensaje, FechaRegistro INTO v_Mensaje, v_Fecha
    FROM TB_SOLICITUD_CONTACTO
    WHERE IdSolicitud = p_IdSolicitud AND IdEstadoSolicitud <> 3;

    IF v_Mensaje IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La solicitud no existe o ya fue convertida.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_ENCARGADO WHERE IdEncargado = p_IdEncargado) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cliente indicado no existe.';
    END IF;

    START TRANSACTION;

    UPDATE TB_SOLICITUD_CONTACTO
    SET IdEstadoSolicitud = 3,
        IdEncargado = p_IdEncargado,
        IdUsuarioAtiende = p_IdUsuarioAccion,
        FechaAtencion = NOW()
    WHERE IdSolicitud = p_IdSolicitud;

    UPDATE TB_ENCARGADO
    SET Observaciones = LEFT(CONCAT_WS('\n\n', Observaciones,
            CONCAT('Solicitud web del ', DATE_FORMAT(v_Fecha, '%d/%m/%Y'), ': ', v_Mensaje)), 1000),
        FechaModificacion = NOW()
    WHERE IdEncargado = p_IdEncargado;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_SOLICITUD_CONTACTO', p_IdSolicitud, 'CambiarEstado',
            JSON_OBJECT('IdEstadoSolicitud', 3, 'IdEncargado', p_IdEncargado, 'Vinculada', TRUE));

    COMMIT;
END$$
DELIMITER ;


-- Para el contador del menu
DROP PROCEDURE IF EXISTS SP_ContarSolicitudesPendientes_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ContarSolicitudesPendientes_CRM()
BEGIN
    SELECT COUNT(*) AS Pendientes
    FROM TB_SOLICITUD_CONTACTO
    WHERE IdEstadoSolicitud = 1;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_ListarEstadosSolicitud_CRM;
DELIMITER $$
CREATE PROCEDURE SP_ListarEstadosSolicitud_CRM()
BEGIN
    SELECT IdEstadoSolicitud, Nombre
    FROM TB_ESTADO_SOLICITUD
    ORDER BY IdEstadoSolicitud;
END$$
DELIMITER ;

-- ============================================================
-- 14. Configuracion (solo Administrador)
-- ============================================================

-- ---------- Horario de atencion ----------

DROP PROCEDURE IF EXISTS SP_Horario_Listar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Horario_Listar_CRM()
BEGIN
    SELECT IdHorarioAtencion, DiaSemana, HoraInicio, HoraFin
    FROM TB_HORARIO_ATENCION
    WHERE Activo = 1
    ORDER BY DiaSemana, HoraInicio;
END$$
DELIMITER ;


-- Reemplaza el horario de toda la semana en una sola transaccion.
-- p_FranjasJson: [{"DiaSemana":1,"HoraInicio":"08:00","HoraFin":"12:00"}, ...]  (1 = lunes ... 7 = domingo)
-- Las citas existentes NO se tocan: al final devuelve las citas futuras que
-- quedaron fuera del nuevo horario, para avisarlas en pantalla.
DROP PROCEDURE IF EXISTS SP_Horario_Guardar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Horario_Guardar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_FranjasJson     JSON,
    IN p_Ahora           DATETIME
)
BEGIN
    DECLARE v_Anterior JSON DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF IFNULL(JSON_LENGTH(p_FranjasJson), 0) = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Defina al menos una franja de atención: sin horario no se pueden agendar citas.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM JSON_TABLE(p_FranjasJson, '$[*]' COLUMNS (
            DiaSemana  INT  PATH '$.DiaSemana',
            HoraInicio TIME PATH '$.HoraInicio',
            HoraFin    TIME PATH '$.HoraFin')) AS f
        WHERE f.DiaSemana IS NULL OR f.DiaSemana NOT BETWEEN 1 AND 7
           OR f.HoraInicio IS NULL OR f.HoraFin IS NULL
           OR f.HoraFin <= f.HoraInicio) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Hay franjas inválidas: la hora de cierre debe ser posterior a la de apertura.';
    END IF;

    -- Dos franjas del mismo dia no pueden traslaparse
    IF EXISTS (
        SELECT 1
        FROM JSON_TABLE(p_FranjasJson, '$[*]' COLUMNS (
                 Fila FOR ORDINALITY,
                 DiaSemana  INT  PATH '$.DiaSemana',
                 HoraInicio TIME PATH '$.HoraInicio',
                 HoraFin    TIME PATH '$.HoraFin')) AS a
        JOIN JSON_TABLE(p_FranjasJson, '$[*]' COLUMNS (
                 Fila FOR ORDINALITY,
                 DiaSemana  INT  PATH '$.DiaSemana',
                 HoraInicio TIME PATH '$.HoraInicio',
                 HoraFin    TIME PATH '$.HoraFin')) AS b
          ON a.DiaSemana = b.DiaSemana
         AND a.Fila < b.Fila
         AND a.HoraInicio < b.HoraFin
         AND b.HoraInicio < a.HoraFin) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Hay franjas que se traslapan en un mismo día.';
    END IF;

    SELECT JSON_ARRAYAGG(JSON_OBJECT('DiaSemana', DiaSemana,
                                     'HoraInicio', TIME_FORMAT(HoraInicio, '%H:%i'),
                                     'HoraFin', TIME_FORMAT(HoraFin, '%H:%i')))
      INTO v_Anterior
    FROM TB_HORARIO_ATENCION
    WHERE Activo = 1;

    START TRANSACTION;

    -- WHERE por llave para que funcione aunque Workbench tenga el modo seguro activo
    DELETE FROM TB_HORARIO_ATENCION WHERE IdHorarioAtencion > 0;

    INSERT INTO TB_HORARIO_ATENCION (DiaSemana, HoraInicio, HoraFin, Activo)
    SELECT f.DiaSemana, f.HoraInicio, f.HoraFin, 1
    FROM JSON_TABLE(p_FranjasJson, '$[*]' COLUMNS (
        DiaSemana  INT  PATH '$.DiaSemana',
        HoraInicio TIME PATH '$.HoraInicio',
        HoraFin    TIME PATH '$.HoraFin')) AS f
    ORDER BY f.DiaSemana, f.HoraInicio;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_HORARIO_ATENCION', 0, 'Editar', v_Anterior, p_FranjasJson);

    COMMIT;

    -- Citas pendientes que quedaron fuera del horario nuevo (o en un dia no laboral)
    SELECT
      c.IdCita,
      c.IdGrupoCita,
      TRIM(CONCAT(es.Nombre, ' ', es.PrimerApellido)) AS Estudiante,
      c.FechaHoraInicio,
      c.FechaHoraFin
    FROM TB_CITA c
    JOIN TB_ESTUDIANTE es ON es.IdEstudiante = c.IdEstudiante
    WHERE c.IdEstadoCita IN (1, 2)
      AND c.FechaHoraInicio > p_Ahora
      AND NOT FN_Agenda_DentroDeHorario(c.FechaHoraInicio, c.FechaHoraFin)
    ORDER BY c.FechaHoraInicio;
END$$
DELIMITER ;


-- ---------- Dias no laborales ----------

DROP PROCEDURE IF EXISTS SP_DiaNoLaboral_Listar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_DiaNoLaboral_Listar_CRM(
    IN p_Desde DATE
)
BEGIN
    SELECT IdDiaNoLaboral, Fecha, Motivo, EsFeriado
    FROM TB_DIA_NO_LABORAL
    WHERE Fecha >= p_Desde
    ORDER BY Fecha;
END$$
DELIMITER ;


-- No deja cerrar un dia que ya tiene citas activas: primero hay que moverlas.
DROP PROCEDURE IF EXISTS SP_DiaNoLaboral_Agregar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_DiaNoLaboral_Agregar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_Fecha           DATE,
    IN p_Motivo          VARCHAR(150),
    IN p_EsFeriado       BOOLEAN,
    IN p_Hoy             DATE
)
BEGIN
    DECLARE v_Citas   INT DEFAULT 0;
    DECLARE v_Mensaje VARCHAR(255);
    DECLARE v_Id      INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_Motivo IS NULL OR TRIM(p_Motivo) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el motivo.';
    END IF;

    IF p_Fecha < p_Hoy THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Solo se pueden marcar fechas de hoy en adelante.';
    END IF;

    IF EXISTS (SELECT 1 FROM TB_DIA_NO_LABORAL WHERE Fecha = p_Fecha) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Esa fecha ya está marcada como no laboral.';
    END IF;

    SELECT COUNT(*) INTO v_Citas
    FROM TB_CITA
    WHERE IdEstadoCita IN (1, 2)
      AND FechaHoraInicio >= p_Fecha
      AND FechaHoraInicio < p_Fecha + INTERVAL 1 DAY;

    IF v_Citas > 0 THEN
        SET v_Mensaje = CONCAT('Ese día tiene ', v_Citas,
            IF(v_Citas = 1, ' cita activa', ' citas activas'),
            '. Reprográmelas o cancélelas antes de marcarlo como no laboral.');
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_Mensaje;
    END IF;

    START TRANSACTION;

    INSERT INTO TB_DIA_NO_LABORAL (Fecha, Motivo, EsFeriado)
    VALUES (p_Fecha, TRIM(p_Motivo), IFNULL(p_EsFeriado, 0));

    SET v_Id = LAST_INSERT_ID();

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_DIA_NO_LABORAL', v_Id, 'Crear',
            JSON_OBJECT('Fecha', p_Fecha, 'Motivo', TRIM(p_Motivo), 'EsFeriado', IFNULL(p_EsFeriado, 0)));

    COMMIT;

    SELECT v_Id AS IdDiaNoLaboral;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_DiaNoLaboral_Eliminar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_DiaNoLaboral_Eliminar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdDiaNoLaboral  INT
)
BEGIN
    DECLARE v_Anterior JSON DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT JSON_OBJECT('Fecha', Fecha, 'Motivo', Motivo, 'EsFeriado', EsFeriado)
      INTO v_Anterior
    FROM TB_DIA_NO_LABORAL
    WHERE IdDiaNoLaboral = p_IdDiaNoLaboral;

    IF v_Anterior IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El día indicado no existe.';
    END IF;

    START TRANSACTION;

    DELETE FROM TB_DIA_NO_LABORAL WHERE IdDiaNoLaboral = p_IdDiaNoLaboral;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior)
    VALUES (p_IdUsuarioAccion, 'TB_DIA_NO_LABORAL', p_IdDiaNoLaboral, 'Eliminar', v_Anterior);

    COMMIT;
END$$
DELIMITER ;


-- ---------- Tipos de sesion ----------
-- Para listar se reutiliza SP_ListarTiposSesion_CRM (ya existe).

-- Crea (p_IdTipoSesion NULL) o edita un tipo de sesion. La duracion nueva solo
-- aplica a citas futuras: las ya agendadas conservan su hora de fin.
DROP PROCEDURE IF EXISTS SP_TipoSesion_Guardar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_TipoSesion_Guardar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdTipoSesion    INT,
    IN p_Nombre          VARCHAR(50),
    IN p_DuracionMinutos INT
)
BEGIN
    DECLARE v_Anterior JSON DEFAULT NULL;
    DECLARE v_Id       INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_Nombre IS NULL OR TRIM(p_Nombre) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el nombre del tipo de sesión.';
    END IF;

    IF p_DuracionMinutos IS NULL OR p_DuracionMinutos NOT BETWEEN 15 AND 240 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La duración debe estar entre 15 y 240 minutos.';
    END IF;

    IF EXISTS (SELECT 1 FROM TB_TIPO_SESION
               WHERE Nombre = TRIM(p_Nombre)
                 AND (p_IdTipoSesion IS NULL OR IdTipoSesion <> p_IdTipoSesion)) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe un tipo de sesión con ese nombre.';
    END IF;

    IF p_IdTipoSesion IS NOT NULL THEN
        SELECT JSON_OBJECT('Nombre', Nombre, 'DuracionMinutos', DuracionMinutos)
          INTO v_Anterior
        FROM TB_TIPO_SESION
        WHERE IdTipoSesion = p_IdTipoSesion;

        IF v_Anterior IS NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El tipo de sesión indicado no existe.';
        END IF;
    END IF;

    START TRANSACTION;

    IF p_IdTipoSesion IS NULL THEN
        INSERT INTO TB_TIPO_SESION (Nombre, DuracionMinutos)
        VALUES (TRIM(p_Nombre), p_DuracionMinutos);

        SET v_Id = LAST_INSERT_ID();
    ELSE
        UPDATE TB_TIPO_SESION
        SET Nombre = TRIM(p_Nombre),
            DuracionMinutos = p_DuracionMinutos
        WHERE IdTipoSesion = p_IdTipoSesion;

        SET v_Id = p_IdTipoSesion;
    END IF;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_TIPO_SESION', v_Id,
            IF(p_IdTipoSesion IS NULL, 'Crear', 'Editar'), v_Anterior,
            JSON_OBJECT('Nombre', TRIM(p_Nombre), 'DuracionMinutos', p_DuracionMinutos));

    COMMIT;

    SELECT v_Id AS IdTipoSesion;
END$$
DELIMITER ;


-- ---------- Datos del consultorio (TB_CONFIGURACION) ----------

DROP PROCEDURE IF EXISTS SP_Configuracion_Listar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Configuracion_Listar_CRM()
BEGIN
    SELECT Clave, Valor, Descripcion
    FROM TB_CONFIGURACION
    ORDER BY IdConfiguracion;
END$$
DELIMITER ;


-- Actualiza varias claves de una vez. Solo modifica claves que ya existen:
-- las claves nuevas se agregan por script, no desde la pantalla.
-- p_ValoresJson: [{"Clave":"CorreoContacto","Valor":"info@..."}, ...]
-- El formato de cada valor (correo, telefono, numeros) lo valida el API.
DROP PROCEDURE IF EXISTS SP_Configuracion_Guardar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Configuracion_Guardar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_ValoresJson     JSON
)
BEGIN
    DECLARE v_Anterior JSON DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF IFNULL(JSON_LENGTH(p_ValoresJson), 0) = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No hay cambios para guardar.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM JSON_TABLE(p_ValoresJson, '$[*]' COLUMNS (Clave VARCHAR(100) PATH '$.Clave')) AS j
        LEFT JOIN TB_CONFIGURACION c ON c.Clave = j.Clave
        WHERE c.IdConfiguracion IS NULL) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Hay claves de configuración que no existen.';
    END IF;

    SELECT JSON_OBJECTAGG(c.Clave, c.Valor) INTO v_Anterior
    FROM TB_CONFIGURACION c
    JOIN JSON_TABLE(p_ValoresJson, '$[*]' COLUMNS (Clave VARCHAR(100) PATH '$.Clave')) AS j
      ON j.Clave = c.Clave;

    START TRANSACTION;

    UPDATE TB_CONFIGURACION c
    JOIN JSON_TABLE(p_ValoresJson, '$[*]' COLUMNS (
             Clave VARCHAR(100)  PATH '$.Clave',
             Valor VARCHAR(1000) PATH '$.Valor')) AS j
      ON j.Clave = c.Clave
    SET c.Valor = TRIM(IFNULL(j.Valor, ''));

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_CONFIGURACION', 0, 'Editar', v_Anterior, p_ValoresJson);

    COMMIT;
END$$
DELIMITER ;

-- =========================================================
-- CONSULTA DEL HISTORIAL DE UN PLAN
-- Recupera las acciones registradas para el plan indicado,
-- incluyendo el usuario que realizó cada acción y los valores
-- anteriores y nuevos. Los resultados se muestran del más
-- reciente al más antiguo.
-- =========================================================
DROP PROCEDURE IF EXISTS SP_Plan_Historial;

DELIMITER $$

CREATE PROCEDURE SP_Plan_Historial(
    IN p_IdPlan INT
)
BEGIN

    SELECT
        b.IdBitacora,
        b.IdUsuario,
        u.NombreCompleto AS Usuario,
        b.FechaHora,
        b.Accion,
        b.ValorAnterior,
        b.ValorNuevo
    FROM TB_BITACORA b
    JOIN TB_USUARIO u
        ON u.IdUsuario = b.IdUsuario
    WHERE b.Entidad = 'TB_PLAN_INTERVENCION'
      AND b.IdRegistro = p_IdPlan
    ORDER BY b.FechaHora DESC, b.IdBitacora DESC;

END$$

DELIMITER ;

-- =========================================================
-- LISTADO DE ESTRATEGIAS DE UN PLAN
-- Devuelve las estrategias asociadas al plan recibido y las
-- ordena por el campo Orden. Las que no tienen orden aparecen
-- al final.
-- =========================================================
DROP PROCEDURE IF EXISTS SP_Estrategia_Listar;

DELIMITER $$

CREATE PROCEDURE SP_Estrategia_Listar(
    IN p_IdPlan INT
)
BEGIN
    SELECT
        e.IdEstrategia,
        e.IdPlan,
        e.Descripcion,
        e.Orden,
        e.FechaCreacion
    FROM TB_ESTRATEGIA e
    WHERE e.IdPlan = p_IdPlan
    ORDER BY
        IFNULL(e.Orden, 9999),
        e.IdEstrategia;
END$$

DELIMITER ;




-- =========================================================
-- ELIMINACIÓN DE UNA ESTRATEGIA
-- Comprueba que la estrategia exista, registra la eliminación
-- en la bitácora y luego elimina la estrategia. Por la relación
-- ON DELETE CASCADE, sus actividades asociadas también se borran.
-- =========================================================
DROP PROCEDURE IF EXISTS SP_Estrategia_Eliminar;

DELIMITER $$

CREATE PROCEDURE SP_Estrategia_Eliminar(
    IN p_IdUsuarioAccion INT,
    IN p_IdEstrategia INT
)
BEGIN
    DECLARE v_IdPlan INT DEFAULT NULL;
    DECLARE v_Descripcion VARCHAR(500) DEFAULT NULL;

    -- Ante cualquier error SQL, revierte la transacción y devuelve el error.
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT IdPlan, Descripcion
    INTO v_IdPlan, v_Descripcion
    FROM TB_ESTRATEGIA
    WHERE IdEstrategia = p_IdEstrategia;

    IF v_IdPlan IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'La estrategia indicada no existe.';
    END IF;

    -- Inicia una transacción para agrupar los cambios como una sola operación.
    START TRANSACTION;

    -- Guarda en la bitácora la acción realizada para mantener trazabilidad.
    INSERT INTO TB_BITACORA (
        IdUsuario,
        Entidad,
        IdRegistro,
        Accion,
        ValorAnterior
    )
    VALUES (
        p_IdUsuarioAccion,
        'TB_ESTRATEGIA',
        p_IdEstrategia,
        'Eliminar',
        JSON_OBJECT(
            'IdPlan', v_IdPlan,
            'Descripcion', v_Descripcion
        )
    );

    DELETE FROM TB_ESTRATEGIA
    WHERE IdEstrategia = p_IdEstrategia;

    COMMIT;
END$$

DELIMITER ;



-- =========================================================
-- LISTADO DE ACTIVIDADES DE UNA ESTRATEGIA
-- Devuelve las actividades de la estrategia indicada y las
-- ordena por fecha de creación y por identificador.
-- =========================================================
DROP PROCEDURE IF EXISTS SP_Actividad_Listar;

DELIMITER $$

CREATE PROCEDURE SP_Actividad_Listar(
    IN p_IdEstrategia INT
)
BEGIN
    SELECT
        a.IdActividad,
        a.IdEstrategia,
        a.Nombre,
        a.Descripcion,
        a.Completada,
        a.FechaCreacion
    FROM TB_ACTIVIDAD a
    WHERE a.IdEstrategia = p_IdEstrategia
    ORDER BY a.FechaCreacion, a.IdActividad;
END$$

DELIMITER ;


-- =========================================================
-- AGREGAR OBSERVACIONES AL PLAN
-- Añade el campo Observaciones a TB_PLAN_INTERVENCION.
-- Se permite NULL porque este dato es opcional.
-- =========================================================
USE mente_activa;

ALTER TABLE TB_PLAN_INTERVENCION
ADD COLUMN Observaciones VARCHAR(1000) NULL
AFTER ObjetivoGeneral;



USE mente_activa;

-- =========================================================
-- LISTADO Y FILTRO DE PLANES DE INTERVENCIÓN
-- Permite filtrar por estudiante y/o estado. También calcula
-- la cantidad de estrategias, actividades y actividades
-- completadas asociadas a cada plan.
-- =========================================================
DROP PROCEDURE IF EXISTS SP_Plan_Listar;

DELIMITER $$

CREATE PROCEDURE SP_Plan_Listar(
    IN p_IdEstudiante INT,
    IN p_IdEstadoPlan INT
)
BEGIN
    SELECT
        p.IdPlan,
        p.IdEstudiante,
        TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
        p.Titulo,
        p.ObjetivoGeneral,
        p.Observaciones,
        p.FechaInicio,
        p.FechaFin,
        ep.Nombre AS Estado,
        (
            SELECT COUNT(*)
            FROM TB_ESTRATEGIA
            WHERE IdPlan = p.IdPlan
        ) AS Estrategias,
        (
            SELECT COUNT(*)
            FROM TB_ACTIVIDAD a
            JOIN TB_ESTRATEGIA es
                ON es.IdEstrategia = a.IdEstrategia
            WHERE es.IdPlan = p.IdPlan
        ) AS Actividades,
        (
            SELECT COUNT(*)
            FROM TB_ACTIVIDAD a
            JOIN TB_ESTRATEGIA es
                ON es.IdEstrategia = a.IdEstrategia
            WHERE es.IdPlan = p.IdPlan
              AND a.Completada = 1
        ) AS ActividadesCompletadas
    FROM TB_PLAN_INTERVENCION p
    JOIN TB_ESTUDIANTE e
        ON e.IdEstudiante = p.IdEstudiante
    JOIN TB_ESTADO_PLAN ep
        ON ep.IdEstadoPlan = p.IdEstadoPlan
    WHERE
        (p_IdEstudiante IS NULL
            OR p.IdEstudiante = p_IdEstudiante)
        AND
        (p_IdEstadoPlan IS NULL
            OR p.IdEstadoPlan = p_IdEstadoPlan)
    ORDER BY p.FechaInicio DESC;
END$$

DELIMITER ;




USE mente_activa;

-- =========================================================
-- CREACIÓN DE UN PLAN DE INTERVENCIÓN
-- Valida el estudiante y evita duplicar el título para ese
-- estudiante. Crea el plan, sus estrategias y actividades a
-- partir del JSON recibido, registra la acción en la bitácora
-- y confirma la operación en una transacción.
-- =========================================================
DROP PROCEDURE IF EXISTS SP_Plan_Crear;

DELIMITER $$

CREATE PROCEDURE SP_Plan_Crear(
    IN p_IdUsuarioAccion INT,
    IN p_IdEstudiante INT,
    IN p_Titulo VARCHAR(150),
    IN p_ObjetivoGeneral VARCHAR(1000),
    IN p_Observaciones VARCHAR(1000),
    IN p_FechaInicio DATE,
    IN p_FechaFin DATE,
    IN p_IdEstadoPlan INT,
    IN p_EstrategiasJson JSON
)
BEGIN
    DECLARE v_IdPlan INT;
    DECLARE v_IdEstrategia INT;
    DECLARE v_i INT DEFAULT 0;
    DECLARE v_n INT DEFAULT 0;
    DECLARE v_j INT DEFAULT 0;
    DECLARE v_m INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF NOT EXISTS (
        SELECT 1
        FROM TB_ESTUDIANTE
        WHERE IdEstudiante = p_IdEstudiante
          AND Activo = 1
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El estudiante indicado no existe o esta inactivo.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM TB_PLAN_INTERVENCION
        WHERE IdEstudiante = p_IdEstudiante
          AND LOWER(TRIM(Titulo)) = LOWER(TRIM(p_Titulo))
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT =
            'Ya existe un plan con ese nombre para el estudiante seleccionado.';
    END IF;

    START TRANSACTION;

    INSERT INTO TB_PLAN_INTERVENCION (
        IdEstudiante,
        IdEstadoPlan,
        IdUsuarioRegistro,
        Titulo,
        ObjetivoGeneral,
        Observaciones,
        FechaInicio,
        FechaFin
    )
    VALUES (
        p_IdEstudiante,
        IFNULL(p_IdEstadoPlan, 1),
        p_IdUsuarioAccion,
        p_Titulo,
        p_ObjetivoGeneral,
        p_Observaciones,
        p_FechaInicio,
        p_FechaFin
    );

    SET v_IdPlan = LAST_INSERT_ID();
    SET v_n = IFNULL(JSON_LENGTH(p_EstrategiasJson), 0);

    WHILE v_i < v_n DO
        INSERT INTO TB_ESTRATEGIA (IdPlan, Descripcion, Orden)
        VALUES (
            v_IdPlan,
            JSON_UNQUOTE(JSON_EXTRACT(
                p_EstrategiasJson,
                CONCAT('$[', v_i, '].Descripcion')
            )),
            CAST(JSON_EXTRACT(
                p_EstrategiasJson,
                CONCAT('$[', v_i, '].Orden')
            ) AS UNSIGNED)
        );

        SET v_IdEstrategia = LAST_INSERT_ID();

        SET v_m = IFNULL(JSON_LENGTH(JSON_EXTRACT(
            p_EstrategiasJson,
            CONCAT('$[', v_i, '].Actividades')
        )), 0);

        SET v_j = 0;

        WHILE v_j < v_m DO
            INSERT INTO TB_ACTIVIDAD (IdEstrategia, Nombre, Descripcion)
            VALUES (
                v_IdEstrategia,
                JSON_UNQUOTE(JSON_EXTRACT(
                    p_EstrategiasJson,
                    CONCAT('$[', v_i, '].Actividades[', v_j, '].Nombre')
                )),
                JSON_UNQUOTE(JSON_EXTRACT(
                    p_EstrategiasJson,
                    CONCAT('$[', v_i, '].Actividades[', v_j, '].Descripcion')
                ))
            );

            SET v_j = v_j + 1;
        END WHILE;

        SET v_i = v_i + 1;
    END WHILE;

    INSERT INTO TB_BITACORA (
        IdUsuario,
        Entidad,
        IdRegistro,
        Accion,
        ValorNuevo
    )
    VALUES (
        p_IdUsuarioAccion,
        'TB_PLAN_INTERVENCION',
        v_IdPlan,
        'Crear',
        JSON_OBJECT(
            'IdEstudiante', p_IdEstudiante,
            'Titulo', p_Titulo,
            'Observaciones', p_Observaciones,
            'Estrategias', v_n
        )
    );

    COMMIT;

    SELECT v_IdPlan AS IdPlan;
END$$

DELIMITER ;





USE mente_activa;

-- =========================================================
-- EDICIÓN DE UN PLAN DE INTERVENCIÓN
-- Guarda los valores anteriores, actualiza los datos del plan
-- y registra en la bitácora los valores previos y los nuevos.
-- =========================================================
DROP PROCEDURE IF EXISTS SP_Plan_Editar;

DELIMITER $$

CREATE PROCEDURE SP_Plan_Editar(
    IN p_IdUsuarioAccion INT,
    IN p_IdPlan INT,
    IN p_Titulo VARCHAR(150),
    IN p_ObjetivoGeneral VARCHAR(1000),
    IN p_Observaciones VARCHAR(1000),
    IN p_FechaInicio DATE,
    IN p_FechaFin DATE
)
BEGIN
    DECLARE v_Anterior JSON DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT JSON_OBJECT(
        'Titulo', Titulo,
        'ObjetivoGeneral', ObjetivoGeneral,
        'Observaciones', Observaciones,
        'FechaInicio', FechaInicio,
        'FechaFin', FechaFin
    )
    INTO v_Anterior
    FROM TB_PLAN_INTERVENCION
    WHERE IdPlan = p_IdPlan;

    IF v_Anterior IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El plan indicado no existe.';
    END IF;

    START TRANSACTION;

    UPDATE TB_PLAN_INTERVENCION
    SET Titulo = p_Titulo,
        ObjetivoGeneral = p_ObjetivoGeneral,
        Observaciones = p_Observaciones,
        FechaInicio = p_FechaInicio,
        FechaFin = p_FechaFin
    WHERE IdPlan = p_IdPlan;

    INSERT INTO TB_BITACORA (
        IdUsuario,
        Entidad,
        IdRegistro,
        Accion,
        ValorAnterior,
        ValorNuevo
    )
    VALUES (
        p_IdUsuarioAccion,
        'TB_PLAN_INTERVENCION',
        p_IdPlan,
        'Editar',
        v_Anterior,
        JSON_OBJECT(
            'Titulo', p_Titulo,
            'ObjetivoGeneral', p_ObjetivoGeneral,
            'Observaciones', p_Observaciones,
            'FechaInicio', p_FechaInicio,
            'FechaFin', p_FechaFin
        )
    );

    COMMIT;
END$$

DELIMITER ;

  
  USE mente_activa;

-- =========================================================
-- REGISTRO DEL ESTADO INACTIVO
-- Agrega el estado Inactivo solamente si todavía no existe,
-- evitando insertar duplicados.
-- =========================================================
INSERT INTO TB_ESTADO_PLAN (Nombre)
SELECT 'Inactivo'
WHERE NOT EXISTS (
    SELECT 1
    FROM TB_ESTADO_PLAN
    WHERE Nombre = 'Inactivo'
);





USE mente_activa;

-- =========================================================
-- CAMBIO DE ESTADO DE UN PLAN
-- Valida el plan y el estado solicitado. Si la operación es
-- válida, actualiza el estado y registra el cambio en la
-- bitácora dentro de una transacción.
-- =========================================================
DROP PROCEDURE IF EXISTS SP_Plan_CambiarEstado;

DELIMITER $$

CREATE PROCEDURE SP_Plan_CambiarEstado(
    IN p_IdUsuarioAccion INT,
    IN p_IdPlan INT,
    IN p_IdEstadoPlan INT
)
BEGIN
    DECLARE v_Estado INT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT IdEstadoPlan
    INTO v_Estado
    FROM TB_PLAN_INTERVENCION
    WHERE IdPlan = p_IdPlan;

    IF v_Estado IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El plan indicado no existe.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM TB_ESTADO_PLAN
        WHERE IdEstadoPlan = p_IdEstadoPlan
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El estado indicado no existe.';
    END IF;

    IF v_Estado = 3 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Un plan finalizado no se reabre.';
    END IF;

    START TRANSACTION;

    UPDATE TB_PLAN_INTERVENCION
    SET IdEstadoPlan = p_IdEstadoPlan
    WHERE IdPlan = p_IdPlan;

    INSERT INTO TB_BITACORA (
        IdUsuario,
        Entidad,
        IdRegistro,
        Accion,
        ValorAnterior,
        ValorNuevo
    )
    VALUES (
        p_IdUsuarioAccion,
        'TB_PLAN_INTERVENCION',
        p_IdPlan,
        'CambiarEstado',
        JSON_OBJECT('IdEstadoPlan', v_Estado),
        JSON_OBJECT('IdEstadoPlan', p_IdEstadoPlan)
    );

    COMMIT;
END$$

DELIMITER ;



USE mente_activa;

DROP PROCEDURE IF EXISTS SP_Plan_CambiarEstado;

DELIMITER $$

CREATE PROCEDURE SP_Plan_CambiarEstado(
    IN p_IdUsuarioAccion INT,
    IN p_IdPlan INT,
    IN p_IdEstadoPlan INT
)
BEGIN
    DECLARE v_Estado INT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT IdEstadoPlan
    INTO v_Estado
    FROM TB_PLAN_INTERVENCION
    WHERE IdPlan = p_IdPlan;

    IF v_Estado IS NULL THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El plan indicado no existe.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM TB_ESTADO_PLAN
        WHERE IdEstadoPlan = p_IdEstadoPlan
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El estado indicado no existe.';
    END IF;

    IF v_Estado = 3 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Un plan finalizado no puede cambiar de estado.';
    END IF;

    IF p_IdEstadoPlan NOT IN (2, 5) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Solo se permite activar o inactivar planes.';
    END IF;

    IF v_Estado = p_IdEstadoPlan THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El plan ya tiene ese estado.';
    END IF;

    START TRANSACTION;

    UPDATE TB_PLAN_INTERVENCION
    SET IdEstadoPlan = p_IdEstadoPlan
    WHERE IdPlan = p_IdPlan;

    INSERT INTO TB_BITACORA (
        IdUsuario,
        Entidad,
        IdRegistro,
        Accion,
        ValorAnterior,
        ValorNuevo
    )
    VALUES (
        p_IdUsuarioAccion,
        'TB_PLAN_INTERVENCION',
        p_IdPlan,
        'CambiarEstado',
        JSON_OBJECT('IdEstadoPlan', v_Estado),
        JSON_OBJECT('IdEstadoPlan', p_IdEstadoPlan)
    );

    COMMIT;
END$$

DELIMITER ;


-- =========================================================
-- ELIMINACIÓN CONTROLADA DE UN PLAN (VERSIÓN ANTERIOR)
-- Esta versión bloquea la eliminación si existen estrategias
-- o historial asociado. Se conserva aquí porque forma parte
-- del script original; la versión posterior reemplaza este SP.
-- =========================================================
USE mente_activa;

DROP PROCEDURE IF EXISTS SP_Plan_Eliminar;

DELIMITER $$

CREATE PROCEDURE SP_Plan_Eliminar(
    IN p_IdPlan INT
)
BEGIN
    DECLARE v_Existe INT DEFAULT 0;
    DECLARE v_TotalEstrategias INT DEFAULT 0;
    DECLARE v_TotalHistorial INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_IdPlan IS NULL OR p_IdPlan <= 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El identificador del plan no es válido.';
    END IF;

    START TRANSACTION;

    SELECT COUNT(*)
    INTO v_Existe
    FROM TB_PLAN_INTERVENCION
    WHERE IdPlan = p_IdPlan
    FOR UPDATE;

    IF v_Existe = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El plan indicado no existe.';
    END IF;

    SELECT COUNT(*)
    INTO v_TotalEstrategias
    FROM TB_ESTRATEGIA
    WHERE IdPlan = p_IdPlan;

    IF v_TotalEstrategias > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT =
                'No se puede eliminar el plan porque tiene estrategias o actividades asociadas.';
    END IF;

    SELECT COUNT(*)
    INTO v_TotalHistorial
    FROM TB_BITACORA
    WHERE Entidad = 'TB_PLAN_INTERVENCION'
      AND IdRegistro = p_IdPlan;

    IF v_TotalHistorial > 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT =
                'No se puede eliminar el plan porque tiene historial registrado.';
    END IF;

    DELETE FROM TB_PLAN_INTERVENCION
    WHERE IdPlan = p_IdPlan;

    COMMIT;
END$$

DELIMITER ;





-- =========================================================
-- ELIMINACIÓN DEFINITIVA DE UN PLAN
-- Valida el identificador y comprueba que el plan exista.
-- Elimina la bitácora asociada directamente al plan y luego
-- elimina el plan. Las estrategias y actividades se eliminan
-- automáticamente por las claves foráneas ON DELETE CASCADE.
-- Si ocurre un error, la transacción se revierte.
-- =========================================================
USE mente_activa;

DROP PROCEDURE IF EXISTS SP_Plan_Eliminar;

DELIMITER $$

CREATE PROCEDURE SP_Plan_Eliminar(IN p_IdPlan INT)
BEGIN
    DECLARE v_Existe INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_IdPlan IS NULL OR p_IdPlan <= 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El identificador del plan no es válido.';
    END IF;

    START TRANSACTION;

    SELECT COUNT(*)
    INTO v_Existe
    FROM TB_PLAN_INTERVENCION
    WHERE IdPlan = p_IdPlan;

    IF v_Existe = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El plan indicado no existe.';
    END IF;

    -- Eliminar el historial asociado directamente al plan.
    DELETE FROM TB_BITACORA
    WHERE Entidad = 'TB_PLAN_INTERVENCION'
      AND IdRegistro = p_IdPlan;

    -- Eliminar el plan.
    -- MySQL eliminará automáticamente sus estrategias
    -- y las actividades asociadas mediante ON DELETE CASCADE.
    DELETE FROM TB_PLAN_INTERVENCION
    WHERE IdPlan = p_IdPlan;

    COMMIT;
END$$

DELIMITER ;

 