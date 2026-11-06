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
-- Convencion de nombres: SP_Entidad_Accion, sin sufijo.
--
-- Hay 45 procedimientos que terminan en _CRM. Empezaron siendo seis, heredados
-- del primer avance, que se dejaron con su nombre porque el API ya los invocaba
-- asi. Hoy son 45 de 109: la excepcion se volvio costumbre y se sigue copiando
-- del vecino. Los que estan no se renombran de a uno, porque romperian
-- endpoints ajenos; se renombran todos juntos cuando el equipo lo acuerde (ver
-- decision pendiente 1 de PROCEDIMIENTOS.md).
--
-- Lo que si aplica desde ya: un procedimiento NUEVO no lleva _CRM.
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
-- Para la burbuja de "Acceso al portal": encargados activos, con correo, que
-- todavia no tienen cuenta de portal. La condicion es la misma rama
-- 'Sin invitar' de SP_UsuarioExterno_Listar, para que el numero de la burbuja y
-- la columna de la tabla no puedan decir cosas distintas.
DROP PROCEDURE IF EXISTS SP_UsuarioExterno_ContarSinInvitar;
DELIMITER $$
CREATE PROCEDURE SP_UsuarioExterno_ContarSinInvitar()
BEGIN
  SELECT COUNT(*) AS Total
  FROM TB_ENCARGADO e
  WHERE e.Activo = 1
    AND e.Correo IS NOT NULL
    AND TRIM(e.Correo) <> ''
    AND NOT EXISTS (SELECT 1 FROM TB_USUARIO u WHERE u.IdEncargado = e.IdEncargado);
END$$
DELIMITER ;


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
    IN p_IdUsuarioAccion INT,
    IN p_IdEncargado     INT
)
BEGIN
    DECLARE v_Activo TINYINT DEFAULT NULL;
    DECLARE v_Estado INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT Activo, IdEstadoCliente INTO v_Activo, v_Estado
    FROM TB_ENCARGADO
    WHERE IdEncargado = p_IdEncargado;

    IF v_Activo IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado indicado no existe.';
    END IF;

    IF v_Activo = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado ya esta inactivo.';
    END IF;

    START TRANSACTION;

    -- Aqui no se toca IdEstadoCliente a proposito. SP_ConsultarClientes_CRM y
    -- SP_ObtenerCliente_CRM ya muestran "Inactivo" cuando Activo = 0, asi que
    -- guardarlo era de mas, y encima borraba el estado comercial que la
    -- reactivacion necesita devolver.
    UPDATE TB_ENCARGADO
    SET Activo = 0,
        FechaModificacion = NOW()
    WHERE IdEncargado = p_IdEncargado;

    CALL SP_DesactivarEstudiantesEncargado_CRM(p_IdEncargado);

    -- 'CambiarEstado' y no 'Desactivar': CK_Bitacora_Accion solo admite
    -- Crear, Editar, Eliminar y CambiarEstado.
    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_ENCARGADO', p_IdEncargado, 'CambiarEstado',
            JSON_OBJECT('Activo', 1, 'IdEstadoCliente', v_Estado),
            JSON_OBJECT('Activo', 0, 'IdEstadoCliente', v_Estado));

    COMMIT;
END$$
DELIMITER ;


-- Devuelve al encargado a activo. Es la vuelta de SP_DesactivarCliente_CRM y
-- hace lo mismo que ya hacia SP_EditarCliente_CRM al cambiarle el estado, pero
-- sin pedir el formulario completo: desde una fila solo se manda el id.
DROP PROCEDURE IF EXISTS SP_Encargado_Reactivar;
DELIMITER $$
CREATE PROCEDURE SP_Encargado_Reactivar(
    IN p_IdUsuarioAccion INT,
    IN p_IdEncargado     INT
)
BEGIN
    DECLARE v_Activo TINYINT DEFAULT NULL;
    DECLARE v_Estado INT;
    DECLARE v_EstadoNuevo INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT Activo, IdEstadoCliente INTO v_Activo, v_Estado
    FROM TB_ENCARGADO
    WHERE IdEncargado = p_IdEncargado;

    IF v_Activo IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado indicado no existe.';
    END IF;

    IF v_Activo = 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El encargado ya esta activo.';
    END IF;

    -- Vuelve con el estado comercial que tenia. Los que se dieron de baja antes
    -- de que la desactivacion dejara de sobrescribirlo quedaron guardados con 5
    -- ("Inactivo"), y eso no puede quedar en un encargado activo: esos pasan a
    -- 4 ("Activo").
    SET v_EstadoNuevo = IF(v_Estado = 5, 4, v_Estado);

    START TRANSACTION;

    UPDATE TB_ENCARGADO
    SET Activo = 1,
        IdEstadoCliente = v_EstadoNuevo,
        FechaModificacion = NOW()
    WHERE IdEncargado = p_IdEncargado;

    -- La misma regla que aplica SP_EditarCliente_CRM cuando reactiva por su
    -- via, para que la misma accion no haga dos cosas distintas segun el boton:
    -- vuelven los estudiantes que cayeron con el.
    UPDATE TB_ESTUDIANTE
    SET Activo = 1,
        FechaModificacion = NOW()
    WHERE Activo = 0
      AND IdEstudiante IN (
          SELECT ee.IdEstudiante
          FROM TB_ESTUDIANTE_ENCARGADO ee
          WHERE ee.IdEncargado = p_IdEncargado);

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_ENCARGADO', p_IdEncargado, 'CambiarEstado',
            JSON_OBJECT('Activo', 0, 'IdEstadoCliente', v_Estado),
            JSON_OBJECT('Activo', 1, 'IdEstadoCliente', v_EstadoNuevo));

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


-- Ficha del estudiante. Devuelve tres resultados, en este orden:
-- 1) datos del estudiante, 2) sus areas de dificultad, 3) sus encargados.
-- ============================================================
-- REEMPLAZAR: Consultar ficha del estudiante
-- ============================================================

DROP PROCEDURE IF EXISTS SP_ConsultarFichaEstudiante_CRM;

DELIMITER $$

CREATE PROCEDURE SP_ConsultarFichaEstudiante_CRM(
    IN p_IdEstudiante INT
)
BEGIN
    -- Datos generales.
    SELECT
        e.IdEstudiante,
        e.Nombre,
        e.PrimerApellido,
        e.SegundoApellido,
        e.FechaNacimiento,
        e.IdNivelEducativo,
        e.IdInstitucion,
        ne.Nombre AS NivelEducativo,
        i.Nombre AS Institucion,
        (
            SELECT GROUP_CONCAT(
                ad.Nombre
                ORDER BY ad.Nombre
                SEPARATOR ', '
            )
            FROM TB_ESTUDIANTE_AREA ea
            INNER JOIN TB_AREA_DIFICULTAD ad
                ON ad.IdAreaDificultad = ea.IdAreaDificultad
            WHERE ea.IdEstudiante = e.IdEstudiante
              AND ea.Activo = 1
        ) AS AreasDificultad,
        e.Observaciones,
        e.NecesidadesApoyo
    FROM TB_ESTUDIANTE e
    LEFT JOIN TB_NIVEL_EDUCATIVO ne
        ON ne.IdNivelEducativo = e.IdNivelEducativo
    LEFT JOIN TB_INSTITUCION i
        ON i.IdInstitucion = e.IdInstitucion
    WHERE e.IdEstudiante = p_IdEstudiante
      AND e.Activo = 1;

    -- Áreas actuales.
    SELECT
        ea.IdEstudianteArea,
        ea.IdAreaDificultad,
        ad.Nombre,
        ad.Descripcion,
        ea.Observacion
    FROM TB_ESTUDIANTE_AREA ea
    INNER JOIN TB_AREA_DIFICULTAD ad
        ON ad.IdAreaDificultad = ea.IdAreaDificultad
    INNER JOIN TB_ESTUDIANTE e
        ON e.IdEstudiante = ea.IdEstudiante
    WHERE ea.IdEstudiante = p_IdEstudiante
      AND ea.Activo = 1
      AND e.Activo = 1
    ORDER BY ad.Nombre, ea.IdEstudianteArea;

    -- Personas encargadas.
    SELECT
        ee.IdEstudianteEncargado,
        enc.IdEncargado,
        enc.Nombre,
        enc.PrimerApellido,
        enc.SegundoApellido,
        CONCAT_WS(
            ' ',
            enc.Nombre,
            enc.PrimerApellido,
            NULLIF(enc.SegundoApellido, '')
        ) AS NombreCompleto,
        enc.Correo,
        ee.IdParentesco,
        p.Nombre AS Parentesco,
        ee.EsPrincipal
    FROM TB_ESTUDIANTE_ENCARGADO ee
    INNER JOIN TB_ENCARGADO enc
        ON enc.IdEncargado = ee.IdEncargado
    INNER JOIN TB_PARENTESCO p
        ON p.IdParentesco = ee.IdParentesco
    INNER JOIN TB_ESTUDIANTE e
        ON e.IdEstudiante = ee.IdEstudiante
    WHERE ee.IdEstudiante = p_IdEstudiante
      AND e.Activo = 1
      AND enc.Activo = 1
    ORDER BY ee.EsPrincipal DESC, enc.Nombre, enc.IdEncargado;
END$$

DELIMITER ;


-- ============================================================
-- REEMPLAZAR: Agregar área de dificultad
-- ============================================================

-- ============================================================
-- REEMPLAZAR: Editar área de dificultad
-- ============================================================

-- ============================================================
-- NUEVO: Listar áreas actuales e inactivas
-- ============================================================

-- ============================================================
-- NUEVO: Desactivar o reactivar área del estudiante
-- ============================================================

DROP PROCEDURE IF EXISTS SP_Estudiante_AgregarArea_CRM;

DELIMITER $$

CREATE PROCEDURE SP_Estudiante_AgregarArea_CRM(
    IN p_IdEstudiante INT,
    IN p_NombreArea VARCHAR(100),
    IN p_Observacion VARCHAR(500)
)
BEGIN
    DECLARE v_IdAreaDificultad INT;
    DECLARE v_NombreArea VARCHAR(100);

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_NombreArea = TRIM(p_NombreArea);

    IF v_NombreArea IS NULL OR v_NombreArea = '' THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Debe ingresar el nombre del área de dificultad.';
    END IF;

    IF NOT EXISTS(
        SELECT 1
        FROM TB_ESTUDIANTE
        WHERE IdEstudiante = p_IdEstudiante
          AND Activo = 1
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estudiante no existe o está inactivo.';
    END IF;

    START TRANSACTION;

    INSERT INTO TB_AREA_DIFICULTAD (Nombre)
    VALUES (v_NombreArea)
    ON DUPLICATE KEY UPDATE
        IdAreaDificultad = LAST_INSERT_ID(IdAreaDificultad);

    SET v_IdAreaDificultad = LAST_INSERT_ID();

    IF EXISTS(
        SELECT 1
        FROM TB_ESTUDIANTE_AREA
        WHERE IdEstudiante = p_IdEstudiante
          AND IdAreaDificultad = v_IdAreaDificultad
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estudiante ya tiene registrada esta area. Si esta inactiva, reactívela desde Areas inactivas.';
    END IF;

    INSERT INTO TB_ESTUDIANTE_AREA (
        IdEstudiante,
        IdAreaDificultad,
        Observacion
    )
    VALUES (
        p_IdEstudiante,
        v_IdAreaDificultad,
        NULLIF(TRIM(p_Observacion), '')
    );

    COMMIT;
END$$

DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Estudiante_EditarArea_CRM;

DELIMITER $$

CREATE PROCEDURE SP_Estudiante_EditarArea_CRM(
    IN p_IdEstudiante INT,
    IN p_IdAreaDificultad INT,
    IN p_NombreArea VARCHAR(100),
    IN p_Observacion VARCHAR(500)
)
BEGIN
    DECLARE v_NuevoIdArea INT;
    DECLARE v_NombreArea VARCHAR(100);

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SET v_NombreArea = TRIM(p_NombreArea);

    IF v_NombreArea IS NULL OR v_NombreArea = '' THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Debe ingresar el nombre del área de dificultad.';
    END IF;

    IF NOT EXISTS(
        SELECT 1
        FROM TB_ESTUDIANTE_AREA
        WHERE IdEstudiante = p_IdEstudiante
          AND IdAreaDificultad = p_IdAreaDificultad
          AND Activo = 1
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El área indicada no está registrada para este estudiante.';
    END IF;

    START TRANSACTION;

    INSERT INTO TB_AREA_DIFICULTAD (Nombre)
    VALUES (v_NombreArea)
    ON DUPLICATE KEY UPDATE
        IdAreaDificultad = LAST_INSERT_ID(IdAreaDificultad);

    SET v_NuevoIdArea = LAST_INSERT_ID();

    IF v_NuevoIdArea <> p_IdAreaDificultad AND EXISTS(
        SELECT 1
        FROM TB_ESTUDIANTE_AREA
        WHERE IdEstudiante = p_IdEstudiante
          AND IdAreaDificultad = v_NuevoIdArea
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estudiante ya tiene registrada esta área de dificultad.';
    END IF;

    UPDATE TB_ESTUDIANTE_AREA
    SET
        IdAreaDificultad = v_NuevoIdArea,
        Observacion = NULLIF(TRIM(p_Observacion), '')
    WHERE IdEstudiante = p_IdEstudiante
      AND IdAreaDificultad = p_IdAreaDificultad
      AND Activo = 1;

    COMMIT;
END$$

DELIMITER ;

DROP PROCEDURE IF EXISTS SP_Estudiante_ListarAreasEstado;

DELIMITER $$

CREATE PROCEDURE SP_Estudiante_ListarAreasEstado(
    IN p_IdEstudiante INT
)
BEGIN
    IF NOT EXISTS(
        SELECT 1
        FROM TB_ESTUDIANTE
        WHERE IdEstudiante = p_IdEstudiante
          AND Activo = 1
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estudiante no esta disponible.';
    END IF;

    SELECT
        ea.IdAreaDificultad,
        a.Nombre,
        ea.Observacion,
        ea.Activo,
        ea.FechaDesactivacion
    FROM TB_ESTUDIANTE_AREA ea
    INNER JOIN TB_AREA_DIFICULTAD a
        ON a.IdAreaDificultad = ea.IdAreaDificultad
    WHERE ea.IdEstudiante = p_IdEstudiante
    ORDER BY a.Nombre, ea.IdAreaDificultad;
END$$

DELIMITER ;

DROP PROCEDURE IF EXISTS SP_Estudiante_CambiarEstadoArea;

DELIMITER $$

CREATE PROCEDURE SP_Estudiante_CambiarEstadoArea(
    IN p_IdEstudiante INT,
    IN p_IdAreaDificultad INT,
    IN p_Activo BOOLEAN
)
BEGIN
    DECLARE v_Actual BOOLEAN DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_Activo IS NULL OR p_Activo NOT IN (0, 1) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Indique un estado valido.';
    END IF;

    START TRANSACTION;

    IF NOT EXISTS(
        SELECT 1
        FROM TB_ESTUDIANTE
        WHERE IdEstudiante = p_IdEstudiante
          AND Activo = 1
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estudiante no esta disponible.';
    END IF;

    SELECT Activo
    INTO v_Actual
    FROM TB_ESTUDIANTE_AREA
    WHERE IdEstudiante = p_IdEstudiante
      AND IdAreaDificultad = p_IdAreaDificultad
    FOR UPDATE;

    IF v_Actual IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El area no pertenece a este estudiante.';
    END IF;

    IF v_Actual <> p_Activo THEN
        UPDATE TB_ESTUDIANTE_AREA
        SET
            Activo = p_Activo,
            FechaDesactivacion = IF(p_Activo = 0, NOW(), NULL)
        WHERE IdEstudiante = p_IdEstudiante
          AND IdAreaDificultad = p_IdAreaDificultad;
    END IF;

    COMMIT;
END$$

DELIMITER ;

DROP PROCEDURE IF EXISTS SP_BuscarEncargados;
DELIMITER $$
CREATE PROCEDURE SP_BuscarEncargados(
    IN p_Texto VARCHAR(100)
)
BEGIN
    DECLARE v_Texto VARCHAR(110);

    SET v_Texto = CONCAT('%', TRIM(IFNULL(p_Texto, '')), '%');

    SELECT
        en.IdEncargado,
        CONCAT_WS(' ', en.Nombre, en.PrimerApellido, en.SegundoApellido) AS NombreCompleto,
        en.Identificacion,
        en.Correo
    FROM TB_ENCARGADO en
    WHERE en.Activo = 1
      AND (
            CONCAT_WS(' ', en.Nombre, en.PrimerApellido, en.SegundoApellido) LIKE v_Texto
         OR en.Identificacion LIKE v_Texto
         OR en.Correo LIKE v_Texto
      )
    ORDER BY NombreCompleto
    LIMIT 20;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS SP_Estudiante_EditarFicha;
DELIMITER $$
CREATE PROCEDURE SP_Estudiante_EditarFicha(
    IN p_IdUsuarioAccion INT,
    IN p_IdEstudiante INT,
    IN p_Nombre VARCHAR(100),
    IN p_PrimerApellido VARCHAR(100),
    IN p_SegundoApellido VARCHAR(100),
    IN p_FechaNacimiento DATE,
    IN p_IdNivelEducativo INT,
    IN p_NombreInstitucion VARCHAR(100),
    IN p_Observaciones VARCHAR(1000),
    IN p_NecesidadesApoyo VARCHAR(1000)
)
BEGIN
    DECLARE v_IdInstitucion INT DEFAULT NULL;
    DECLARE v_NombreInstitucion VARCHAR(100);

    SET v_NombreInstitucion = NULLIF(TRIM(p_NombreInstitucion), '');

    IF NULLIF(TRIM(p_Nombre), '') IS NULL
       OR NULLIF(TRIM(p_PrimerApellido), '') IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El nombre y el primer apellido son obligatorios.';
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM TB_ESTUDIANTE
        WHERE IdEstudiante = p_IdEstudiante AND Activo = 1
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estudiante no existe o está inactivo.';
    END IF;

    -- Institución escrita a mano: se busca por nombre y, si no existe, se crea
    IF v_NombreInstitucion IS NOT NULL THEN

        SELECT IdInstitucion INTO v_IdInstitucion
        FROM TB_INSTITUCION
        WHERE Nombre = v_NombreInstitucion
        LIMIT 1;

        IF v_IdInstitucion IS NULL THEN
            INSERT INTO TB_INSTITUCION (Nombre) VALUES (v_NombreInstitucion);
            SET v_IdInstitucion = LAST_INSERT_ID();
        END IF;

    END IF;

    CALL SP_Estudiante_Editar(
        p_IdUsuarioAccion,
        p_IdEstudiante,
        TRIM(p_Nombre),
        TRIM(p_PrimerApellido),
        NULLIF(TRIM(p_SegundoApellido), ''),
        p_FechaNacimiento,
        p_IdNivelEducativo,
        v_IdInstitucion,
        NULLIF(TRIM(p_Observaciones), ''),
        NULLIF(TRIM(p_NecesidadesApoyo), ''),
        NULL
    );
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS SP_Estudiante_CrearConEncargado;
DELIMITER $$
CREATE PROCEDURE SP_Estudiante_CrearConEncargado(
    IN p_IdUsuarioAccion INT,
    IN p_Nombre VARCHAR(100),
    IN p_PrimerApellido VARCHAR(100),
    IN p_SegundoApellido VARCHAR(100),
    IN p_FechaNacimiento DATE,
    IN p_IdNivelEducativo INT,
    IN p_NombreInstitucion VARCHAR(100),
    IN p_Observaciones VARCHAR(1000),
    IN p_IdEncargado INT,
    IN p_IdParentesco INT
)
BEGIN
    DECLARE v_IdInstitucion INT DEFAULT NULL;
    DECLARE v_NombreInstitucion VARCHAR(100);

    SET v_NombreInstitucion = NULLIF(TRIM(p_NombreInstitucion), '');

    IF NULLIF(TRIM(p_Nombre), '') IS NULL
       OR NULLIF(TRIM(p_PrimerApellido), '') IS NULL THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El nombre y el primer apellido son obligatorios.';
    END IF;

    IF p_IdEncargado IS NULL OR p_IdEncargado <= 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Debe seleccionar la persona encargada.';
    END IF;

    IF p_IdParentesco IS NULL OR p_IdParentesco <= 0 THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Debe seleccionar el parentesco.';
    END IF;

    IF v_NombreInstitucion IS NOT NULL THEN

        SELECT IdInstitucion INTO v_IdInstitucion
        FROM TB_INSTITUCION
        WHERE Nombre = v_NombreInstitucion
        LIMIT 1;

        IF v_IdInstitucion IS NULL THEN
            INSERT INTO TB_INSTITUCION (Nombre) VALUES (v_NombreInstitucion);
            SET v_IdInstitucion = LAST_INSERT_ID();
        END IF;

    END IF;

    -- Devuelve el IdEstudiante (el SELECT final de SP_Estudiante_Crear)
    CALL SP_Estudiante_Crear(
        p_IdUsuarioAccion,
        TRIM(p_Nombre),
        TRIM(p_PrimerApellido),
        NULLIF(TRIM(p_SegundoApellido), ''),
        p_FechaNacimiento,
        p_IdNivelEducativo,
        v_IdInstitucion,
        NULLIF(TRIM(p_Observaciones), ''),
        NULL,
        p_IdEncargado,
        p_IdParentesco,
        NULL
    );
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

  -- HU-M10-3: confirmacion al encargado. Va aqui dentro y no en el API para
  -- que ningun modulo que agende una cita pueda saltarse el aviso. No puede
  -- fallar: inserta cero filas si el estudiante no tiene encargado con correo.
  CALL SP_Notificacion_AvisarCitas(v_IdCita, NULL, 'ConfirmacionCita', 1);

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

  -- HU-M10-3: la cita cambio de fecha, se avisa de nuevo.
  CALL SP_Notificacion_AvisarCitas(p_IdCita, NULL, 'ActualizacionCita', 1);

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

  -- HU-M10-3: aviso de cancelacion. Con 0 porque la cita que interesa es la
  -- que acaba de quedar cancelada, no las vigentes.
  CALL SP_Notificacion_AvisarCitas(p_IdCita, NULL, 'CancelacionCita', 0);

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


-- Pasa a Completada toda cita Programada o Confirmada cuya hora de fin ya paso,
-- y le crea su cobro pendiente. La llama el servicio en segundo plano del API.
-- No va a bitacora: no hay usuario que la ejecute (TB_BITACORA exige uno).
DROP PROCEDURE IF EXISTS SP_Cita_CompletarVencidas_CRM;
DELIMITER $$
CREATE PROCEDURE SP_Cita_CompletarVencidas_CRM(
    IN p_Ahora DATETIME
)
BEGIN
    DECLARE v_Completadas INT DEFAULT 0;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- Primero los cobros: en este momento las citas todavia estan en 1 o 2,
    -- asi se cobran exactamente las que se van a completar ahora
    CALL SP_Pago_GenerarCobrosCitas(NULL, p_Ahora);

    UPDATE TB_CITA
    SET IdEstadoCita = 3,
        FechaModificacion = p_Ahora
    WHERE IdCita > 0
      AND IdEstadoCita IN (1, 2)
      AND FechaHoraFin <= p_Ahora;

    SET v_Completadas = ROW_COUNT();

    COMMIT;

    SELECT v_Completadas AS CitasCompletadas;
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

    -- M5 agrego borrado logico (2026-10-07): una sesion inactivada ya no bloquea
    IF EXISTS (SELECT 1 FROM TB_SESION WHERE IdCita = p_IdCita AND Activo = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita tiene una sesión registrada: el estudiante sí asistió.';
    END IF;

    START TRANSACTION;

    UPDATE TB_CITA
    SET IdEstadoCita = 5,
        FechaModificacion = p_Ahora
    WHERE IdCita = p_IdCita;

    -- Si la cita ya genero su cobro y nadie ha pagado, se anula.
    -- Si ya tiene abonos se deja como esta: eso lo decide la profesional.
    UPDATE TB_PAGO p
    SET p.IdEstadoPago = 5,
        p.Observaciones = CONCAT_WS(' | ', p.Observaciones, 'Anulado: la cita se marcó como no asistió.'),
        p.FechaModificacion = p_Ahora
    WHERE p.IdCita = p_IdCita
      AND p.IdEstadoPago <> 5
      AND NOT EXISTS (SELECT 1 FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1);

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
    SELECT IdTipoSesion, Nombre, IFNULL(DuracionMinutos, 60) AS DuracionMinutos, Precio
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

    -- HU-M10-3: una confirmacion por cada estudiante del grupo.
    CALL SP_Notificacion_AvisarCitas(NULL, v_IdGrupo, 'ConfirmacionCita', 1);

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

    -- HU-M10-3: confirmacion al encargado del estudiante que se acaba de agregar.
    CALL SP_Notificacion_AvisarCitas(v_IdCita, NULL, 'ConfirmacionCita', 1);

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

    -- HU-M10-3: aviso a los que siguen vigentes. Los que ya tenian su cita
    -- cancelada quedan fuera, que es para lo que sirve el ultimo parametro.
    CALL SP_Notificacion_AvisarCitas(NULL, p_IdGrupoCita, 'ActualizacionCita', 1);

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

    -- HU-M10-3: aviso de cancelacion a todo el grupo.
    CALL SP_Notificacion_AvisarCitas(NULL, p_IdGrupoCita, 'CancelacionCita', 0);

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

-- Ids que se usan en esta seccion:
--   TB_ESTADO_CITA     1 Programada  2 Confirmada  3 Completada  4 Cancelada  5 No asistio
--   TB_TIPO_ATENCION   1 Individual  2 Grupal  3 Familiar
--
-- Una sesion es atencion YA realizada, por eso no se acepta fecha futura. La
-- hora de referencia la manda la aplicacion en p_Ahora: es la hora del
-- consultorio, no la del servidor de base de datos.
--
-- La sesion guarda DuracionMinutos y no hora de fin, al reves que TB_CITA.
--
-- Dos reglas que viven aqui y no en un indice, porque con borrado logico un
-- indice unico bloquearia registros legitimos:
--   1. Un estudiante no puede tener dos sesiones ACTIVAS que se traslapen.
--      Se mide con la duracion, no solo por hora de inicio igual. El traslape
--      se compara SOLO contra el mismo estudiante: en una sesion grupal varios
--      estudiantes comparten hora a proposito.
--   2. Una cita no puede tener dos sesiones ACTIVAS.
--
-- Las validaciones de rango y de texto vacio estan duplicadas a proposito: la
-- tabla las tiene como CHECK, pero un CHECK violado sale como error tecnico y el
-- API lo traduce a 500. Con SIGNAL '45000' el usuario recibe 400 y un mensaje
-- que se entiende.

-- Registrar la atencion ya realizada. Si nace de una cita, esa cita queda
-- marcada como Completada en la misma transaccion.
DROP PROCEDURE IF EXISTS SP_Sesion_Registrar;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_Registrar(
    IN p_IdUsuarioAccion         INT,
    IN p_IdEstudiante            INT,
    IN p_IdCita                  INT,
    IN p_IdTipoSesion            INT,
    IN p_IdTipoAtencion          INT,
    IN p_FechaHoraInicio         DATETIME,
    IN p_DuracionMinutos         SMALLINT,
    IN p_Objetivo                VARCHAR(500),
    IN p_TemaTrabajado           VARCHAR(500),
    IN p_ActividadesRealizadas   VARCHAR(1000),
    IN p_Acuerdos                VARCHAR(1000),
    IN p_ObjetivosAlcanzados     VARCHAR(1000),
    IN p_HabilidadesDesarrolladas VARCHAR(1000),
    IN p_Avances                 VARCHAR(1000),
    IN p_Recomendaciones         VARCHAR(1000),
    IN p_Observaciones           VARCHAR(1000),
    IN p_Ahora                   DATETIME
)
BEGIN
    DECLARE v_IdSesion        INT;
    DECLARE v_EstudianteCita  INT DEFAULT NULL;
    DECLARE v_EstadoCita      INT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE
                   WHERE IdEstudiante = p_IdEstudiante AND Activo = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante indicado no existe o está inactivo.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_TIPO_SESION WHERE IdTipoSesion = p_IdTipoSesion) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El tipo de sesión indicado no existe.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_TIPO_ATENCION WHERE IdTipoAtencion = p_IdTipoAtencion) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El tipo de atención indicado no existe.';
    END IF;

    IF p_FechaHoraInicio IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique la fecha y la hora de la sesión.';
    END IF;

    IF p_DuracionMinutos IS NULL OR p_DuracionMinutos NOT BETWEEN 15 AND 240 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La duración debe estar entre 15 y 240 minutos.';
    END IF;

    IF p_Objetivo IS NULL OR TRIM(p_Objetivo) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el objetivo de la sesión.';
    END IF;

    IF p_TemaTrabajado IS NULL OR TRIM(p_TemaTrabajado) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el tema trabajado.';
    END IF;

    IF p_FechaHoraInicio > p_Ahora THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No se puede registrar una sesión con fecha futura.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM TB_SESION s
        WHERE s.IdEstudiante = p_IdEstudiante
          AND s.Activo = 1
          AND p_FechaHoraInicio < s.FechaHoraInicio + INTERVAL s.DuracionMinutos MINUTE
          AND s.FechaHoraInicio < p_FechaHoraInicio + INTERVAL p_DuracionMinutos MINUTE) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante ya tiene una sesión registrada que se traslapa con ese horario.';
    END IF;

    IF p_IdCita IS NOT NULL THEN
        SELECT IdEstudiante, IdEstadoCita
          INTO v_EstudianteCita, v_EstadoCita
        FROM TB_CITA
        WHERE IdCita = p_IdCita;

        IF v_EstudianteCita IS NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita indicada no existe.';
        END IF;

        IF v_EstudianteCita <> p_IdEstudiante THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita es de otro estudiante.';
        END IF;

        IF v_EstadoCita = 4 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No se puede registrar una sesión de una cita cancelada.';
        END IF;

        IF v_EstadoCita = 5 THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Esa cita está marcada como no asistió. Corrija el estado de la cita antes de registrar la sesión.';
        END IF;

        IF EXISTS (SELECT 1 FROM TB_SESION
                   WHERE IdCita = p_IdCita AND Activo = 1) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Esa cita ya tiene una sesión registrada.';
        END IF;
    END IF;

    START TRANSACTION;

    INSERT INTO TB_SESION (IdEstudiante, IdCita, IdTipoSesion, IdTipoAtencion, IdUsuarioRegistro,
                           FechaHoraInicio, DuracionMinutos, Objetivo, TemaTrabajado,
                           ActividadesRealizadas, Acuerdos, ObjetivosAlcanzados,
                           HabilidadesDesarrolladas, Avances, Recomendaciones, Observaciones)
    VALUES (p_IdEstudiante, p_IdCita, p_IdTipoSesion, p_IdTipoAtencion, p_IdUsuarioAccion,
            p_FechaHoraInicio, p_DuracionMinutos, TRIM(p_Objetivo), TRIM(p_TemaTrabajado),
            p_ActividadesRealizadas, p_Acuerdos, p_ObjetivosAlcanzados,
            p_HabilidadesDesarrolladas, p_Avances, p_Recomendaciones, p_Observaciones);

    SET v_IdSesion = LAST_INSERT_ID();

    IF p_IdCita IS NOT NULL THEN
        UPDATE TB_CITA
        SET IdEstadoCita = 3,
            FechaModificacion = p_Ahora
        WHERE IdCita = p_IdCita AND IdEstadoCita NOT IN (3, 4);

        -- Sesion registrada = cita completada: se crea su cobro si aun no lo tiene
        CALL SP_Pago_GenerarCobrosCitas(p_IdCita, p_Ahora);
    END IF;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_SESION', v_IdSesion, 'Crear',
            JSON_OBJECT('IdEstudiante', p_IdEstudiante,
                        'IdCita', p_IdCita,
                        'FechaHoraInicio', p_FechaHoraInicio,
                        'DuracionMinutos', p_DuracionMinutos,
                        'TemaTrabajado', TRIM(p_TemaTrabajado)));

    COMMIT;

    SELECT v_IdSesion AS IdSesion;
END$$
DELIMITER ;

-- Edicion de una sesion ya registrada. No cambia el estudiante ni la cita de
-- origen: eso seria otra sesion. El traslape se revisa sin contar a la propia
-- sesion, o editar sin mover la hora fallaria contra si misma.
DROP PROCEDURE IF EXISTS SP_Sesion_Editar;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_Editar(
    IN p_IdUsuarioAccion         INT,
    IN p_IdSesion                INT,
    IN p_IdTipoSesion            INT,
    IN p_IdTipoAtencion          INT,
    IN p_FechaHoraInicio         DATETIME,
    IN p_DuracionMinutos         SMALLINT,
    IN p_Objetivo                VARCHAR(500),
    IN p_TemaTrabajado           VARCHAR(500),
    IN p_ActividadesRealizadas   VARCHAR(1000),
    IN p_Acuerdos                VARCHAR(1000),
    IN p_ObjetivosAlcanzados     VARCHAR(1000),
    IN p_HabilidadesDesarrolladas VARCHAR(1000),
    IN p_Avances                 VARCHAR(1000),
    IN p_Recomendaciones         VARCHAR(1000),
    IN p_Observaciones           VARCHAR(1000),
    IN p_Ahora                   DATETIME
)
BEGIN
    DECLARE v_IdEstudiante INT DEFAULT NULL;
    DECLARE v_Activo       TINYINT DEFAULT NULL;
    DECLARE v_Anterior     JSON DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT IdEstudiante, Activo,
           JSON_OBJECT('IdTipoSesion', IdTipoSesion,
                       'IdTipoAtencion', IdTipoAtencion,
                       'FechaHoraInicio', FechaHoraInicio,
                       'DuracionMinutos', DuracionMinutos,
                       'Objetivo', Objetivo,
                       'TemaTrabajado', TemaTrabajado)
      INTO v_IdEstudiante, v_Activo, v_Anterior
    FROM TB_SESION
    WHERE IdSesion = p_IdSesion;

    IF v_IdEstudiante IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sesión indicada no existe.';
    END IF;

    IF v_Activo = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sesión está inactiva y no se puede editar.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_TIPO_SESION WHERE IdTipoSesion = p_IdTipoSesion) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El tipo de sesión indicado no existe.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_TIPO_ATENCION WHERE IdTipoAtencion = p_IdTipoAtencion) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El tipo de atención indicado no existe.';
    END IF;

    IF p_FechaHoraInicio IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique la fecha y la hora de la sesión.';
    END IF;

    IF p_DuracionMinutos IS NULL OR p_DuracionMinutos NOT BETWEEN 15 AND 240 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La duración debe estar entre 15 y 240 minutos.';
    END IF;

    IF p_Objetivo IS NULL OR TRIM(p_Objetivo) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el objetivo de la sesión.';
    END IF;

    IF p_TemaTrabajado IS NULL OR TRIM(p_TemaTrabajado) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el tema trabajado.';
    END IF;

    IF p_FechaHoraInicio > p_Ahora THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No se puede registrar una sesión con fecha futura.';
    END IF;

    IF EXISTS (
        SELECT 1
        FROM TB_SESION s
        WHERE s.IdEstudiante = v_IdEstudiante
          AND s.IdSesion <> p_IdSesion
          AND s.Activo = 1
          AND p_FechaHoraInicio < s.FechaHoraInicio + INTERVAL s.DuracionMinutos MINUTE
          AND s.FechaHoraInicio < p_FechaHoraInicio + INTERVAL p_DuracionMinutos MINUTE) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante ya tiene una sesión registrada que se traslapa con ese horario.';
    END IF;

    START TRANSACTION;

    UPDATE TB_SESION
    SET IdTipoSesion             = p_IdTipoSesion,
        IdTipoAtencion           = p_IdTipoAtencion,
        FechaHoraInicio          = p_FechaHoraInicio,
        DuracionMinutos          = p_DuracionMinutos,
        Objetivo                 = TRIM(p_Objetivo),
        TemaTrabajado            = TRIM(p_TemaTrabajado),
        ActividadesRealizadas    = p_ActividadesRealizadas,
        Acuerdos                 = p_Acuerdos,
        ObjetivosAlcanzados      = p_ObjetivosAlcanzados,
        HabilidadesDesarrolladas = p_HabilidadesDesarrolladas,
        Avances                  = p_Avances,
        Recomendaciones          = p_Recomendaciones,
        Observaciones            = p_Observaciones
    WHERE IdSesion = p_IdSesion;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_SESION', p_IdSesion, 'Editar', v_Anterior,
            JSON_OBJECT('IdTipoSesion', p_IdTipoSesion,
                        'IdTipoAtencion', p_IdTipoAtencion,
                        'FechaHoraInicio', p_FechaHoraInicio,
                        'DuracionMinutos', p_DuracionMinutos,
                        'Objetivo', TRIM(p_Objetivo),
                        'TemaTrabajado', TRIM(p_TemaTrabajado)));

    COMMIT;
END$$
DELIMITER ;


-- Baja logica. El historial de la sesion se conserva: nunca se borra la fila.
-- No se devuelve la cita de origen a su estado anterior a proposito: la cita si
-- ocurrio, lo que se anula es el registro de la atencion.
DROP PROCEDURE IF EXISTS SP_Sesion_Inactivar;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_Inactivar(
    IN p_IdUsuarioAccion INT,
    IN p_IdSesion        INT,
    IN p_Ahora           DATETIME
)
BEGIN
    DECLARE v_Activo TINYINT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    SELECT Activo INTO v_Activo FROM TB_SESION WHERE IdSesion = p_IdSesion;

    IF v_Activo IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sesión indicada no existe.';
    END IF;

    IF v_Activo = 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sesión ya está inactiva.';
    END IF;

    START TRANSACTION;

    UPDATE TB_SESION
    SET Activo = 0,
        FechaModificacion = p_Ahora
    WHERE IdSesion = p_IdSesion;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_SESION', p_IdSesion, 'Eliminar',
            JSON_OBJECT('Activo', 1), JSON_OBJECT('Activo', 0));

    COMMIT;
END$$
DELIMITER ;

-- Historial de sesiones. Solo activas: una sesion inactivada no sale aqui.
-- Devuelve tambien los campos de avance, para que la vista de historial de
-- avances se arme con esta misma consulta y no se duplique.
-- p_IdEstudiante en NULL trae todas; p_Desde y p_Hasta son opcionales.
DROP PROCEDURE IF EXISTS SP_Sesion_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_Listar(
    IN p_IdEstudiante INT,
    IN p_Desde        DATE,
    IN p_Hasta        DATE
)
BEGIN
    SELECT s.IdSesion,
           s.IdEstudiante,
           TRIM(CONCAT_WS(' ', es.Nombre, es.PrimerApellido, es.SegundoApellido)) AS Estudiante,
           s.FechaHoraInicio,
           s.DuracionMinutos,
           s.IdTipoSesion,
           ts.Nombre AS TipoSesion,
           s.IdTipoAtencion,
           ta.Nombre AS TipoAtencion,
           s.Objetivo,
           s.TemaTrabajado,
           s.ObjetivosAlcanzados,
           s.HabilidadesDesarrolladas,
           s.Avances,
           s.Recomendaciones,
           s.Observaciones,
           s.IdCita,
           u.NombreCompleto AS RegistradaPor
    FROM TB_SESION s
    JOIN TB_ESTUDIANTE es    ON es.IdEstudiante = s.IdEstudiante
    JOIN TB_TIPO_SESION ts   ON ts.IdTipoSesion = s.IdTipoSesion
    JOIN TB_TIPO_ATENCION ta ON ta.IdTipoAtencion = s.IdTipoAtencion
    JOIN TB_USUARIO u        ON u.IdUsuario = s.IdUsuarioRegistro
    WHERE s.Activo = 1
      AND (p_IdEstudiante IS NULL OR s.IdEstudiante = p_IdEstudiante)
      AND (p_Desde IS NULL OR s.FechaHoraInicio >= p_Desde)
      AND (p_Hasta IS NULL OR s.FechaHoraInicio < p_Hasta + INTERVAL 1 DAY)
    ORDER BY s.FechaHoraInicio DESC, s.IdSesion DESC;
END$$
DELIMITER ;


-- Detalle completo de una sesion. Trae las inactivas tambien: el historial se
-- conserva y tiene que poder consultarse.
DROP PROCEDURE IF EXISTS SP_Sesion_ObtenerDetalle;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_ObtenerDetalle(
    IN p_IdSesion INT
)
BEGIN
    SELECT s.IdSesion,
           s.IdEstudiante,
           TRIM(CONCAT_WS(' ', es.Nombre, es.PrimerApellido, es.SegundoApellido)) AS Estudiante,
           s.FechaHoraInicio,
           s.DuracionMinutos,
           s.IdTipoSesion,
           ts.Nombre AS TipoSesion,
           s.IdTipoAtencion,
           ta.Nombre AS TipoAtencion,
           s.Objetivo,
           s.TemaTrabajado,
           s.ActividadesRealizadas,
           s.Acuerdos,
           s.ObjetivosAlcanzados,
           s.HabilidadesDesarrolladas,
           s.Avances,
           s.Recomendaciones,
           s.Observaciones,
           s.Activo,
           s.IdCita,
           c.FechaHoraInicio AS FechaHoraCita,
           u.NombreCompleto AS RegistradaPor,
           s.FechaCreacion,
           s.FechaModificacion
    FROM TB_SESION s
    JOIN TB_ESTUDIANTE es    ON es.IdEstudiante = s.IdEstudiante
    JOIN TB_TIPO_SESION ts   ON ts.IdTipoSesion = s.IdTipoSesion
    JOIN TB_TIPO_ATENCION ta ON ta.IdTipoAtencion = s.IdTipoAtencion
    JOIN TB_USUARIO u        ON u.IdUsuario = s.IdUsuarioRegistro
    LEFT JOIN TB_CITA c      ON c.IdCita = s.IdCita
    WHERE s.IdSesion = p_IdSesion;
END$$
DELIMITER ;


-- Citas del estudiante a las que todavia se les puede registrar la sesion: ya
-- empezaron, no estan canceladas y no tienen una sesion activa. Alimenta el
-- campo opcional de cita de origen del formulario.
-- Para la burbuja de "Bitacora de citas": citas cuya hora ya paso y que nadie
-- anoto como sesion. Es SP_Sesion_ListarCitasDisponibles sin el filtro por
-- estudiante y contando en vez de listar, asi que las dos aplican la misma
-- regla: fuera las canceladas y las de "no asistio", porque en esas no hubo
-- atencion que anotar.
DROP PROCEDURE IF EXISTS SP_Sesion_ContarCitasSinSesion;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_ContarCitasSinSesion(
    IN p_Ahora DATETIME
)
BEGIN
    DECLARE v_Ahora DATETIME;

    SET v_Ahora = IFNULL(p_Ahora, NOW());

    SELECT COUNT(*) AS Total
    FROM TB_CITA c
    WHERE c.FechaHoraInicio <= v_Ahora
      AND c.IdEstadoCita NOT IN (4, 5)
      AND NOT EXISTS (SELECT 1 FROM TB_SESION s
                      WHERE s.IdCita = c.IdCita AND s.Activo = 1);
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Sesion_ListarCitasDisponibles;
DELIMITER $$
CREATE PROCEDURE SP_Sesion_ListarCitasDisponibles(
    IN p_IdEstudiante INT,
    IN p_Ahora        DATETIME
)
BEGIN
    SELECT c.IdCita,
           c.FechaHoraInicio,
           c.FechaHoraFin,
           c.IdTipoSesion,
           ts.Nombre AS TipoSesion,
           TIMESTAMPDIFF(MINUTE, c.FechaHoraInicio, c.FechaHoraFin) AS DuracionMinutos,
           ec.Nombre AS EstadoCita
    FROM TB_CITA c
    JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = c.IdTipoSesion
    JOIN TB_ESTADO_CITA ec ON ec.IdEstadoCita = c.IdEstadoCita
    WHERE c.IdEstudiante = p_IdEstudiante
      AND c.FechaHoraInicio <= p_Ahora
      -- Fuera las canceladas y las de "no asistio": en esas no hubo atencion,
      -- asi que una sesion las contradice. SP_Cita_MarcarNoAsistio_CRM aplica la
      -- regla al reves, y niega el "no asistio" si la cita ya tiene sesion.
      AND c.IdEstadoCita NOT IN (4, 5)
      AND NOT EXISTS (SELECT 1 FROM TB_SESION s
                      WHERE s.IdCita = c.IdCita AND s.Activo = 1)
    ORDER BY c.FechaHoraInicio DESC;
END$$
DELIMITER ;


-- Catalogo para el select del formulario. El de tipos de sesion ya existe en la
-- seccion de agenda: SP_ListarTiposSesion_CRM.
DROP PROCEDURE IF EXISTS SP_TipoAtencion_Listar;
DELIMITER $$
CREATE PROCEDURE SP_TipoAtencion_Listar()
BEGIN
    SELECT IdTipoAtencion, Nombre
    FROM TB_TIPO_ATENCION
    ORDER BY Nombre;
END$$
DELIMITER ;

-- ============================================================
-- 6. Planes de intervencion
-- ============================================================

-- Ids de TB_ESTADO_PLAN: 1 Borrador, 2 Activo, 3 Finalizado, 4 Suspendido.


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
-- Para la burbuja de Pagos: cobros en estado "Vencido" (3). Cuenta el estado
-- guardado y no lo recalcula, para decir lo mismo que muestra la pantalla.
-- Ojo: quien pasa un cobro a vencido es SP_Pago_RecalcularVencidos, y hoy no lo
-- llama nadie, asi que un cobro pasado de fecha puede seguir figurando como
-- pendiente y no entrar aqui. Esta anotado.
DROP PROCEDURE IF EXISTS SP_Pago_ContarVencidos;
DELIMITER $$
CREATE PROCEDURE SP_Pago_ContarVencidos()
BEGIN
    SELECT COUNT(*) AS Total
    FROM TB_PAGO
    WHERE IdEstadoPago = 3;
END$$
DELIMITER ;


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



-- ============================================================
-- 8. Reportes
-- ============================================================

DROP PROCEDURE IF EXISTS SP_Reporte_Listar;

DELIMITER $$

CREATE PROCEDURE SP_Reporte_Listar(
    IN p_IdEstudiante INT,
    IN p_Desde DATE,
    IN p_Hasta DATE,
    IN p_Buscar VARCHAR(150),
    IN p_Estado VARCHAR(10),
    IN p_Orden VARCHAR(2)
)
BEGIN
    IF p_Estado IS NULL
       OR p_Estado NOT IN ('activos', 'inactivos', 'todos')
       OR p_Orden IS NULL
       OR p_Orden NOT IN ('az', 'za') THEN

        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estado o el orden del filtro no es valido.';
    END IF;

    SELECT
        r.IdReporte,
        r.IdEstudiante,
        TRIM(CONCAT_WS(
            ' ',
            e.Nombre,
            e.PrimerApellido,
            e.SegundoApellido
        )) AS Estudiante,
        r.Titulo,
        r.PeriodoInicio,
        r.PeriodoFin,
        r.VisibleEnPortal,
        r.FechaGeneracion,
        r.Activo,
        u.NombreCompleto AS GeneradoPor
    FROM TB_REPORTE r
    INNER JOIN TB_ESTUDIANTE e
        ON e.IdEstudiante = r.IdEstudiante
    INNER JOIN TB_USUARIO u
        ON u.IdUsuario = r.IdUsuarioGenero
    WHERE (p_IdEstudiante IS NULL OR r.IdEstudiante = p_IdEstudiante)
      AND (p_Desde IS NULL OR r.PeriodoInicio >= p_Desde)
      AND (p_Hasta IS NULL OR r.PeriodoFin <= p_Hasta)
      AND (
          p_Buscar IS NULL
          OR TRIM(p_Buscar) = ''
          OR LOCATE(
              TRIM(p_Buscar),
              TRIM(CONCAT_WS(
                  ' ',
                  e.Nombre,
                  e.PrimerApellido,
                  e.SegundoApellido
              ))
          ) > 0
      )
      AND (
          p_Estado = 'todos'
          OR (p_Estado = 'activos' AND r.Activo = 1)
          OR (p_Estado = 'inactivos' AND r.Activo = 0)
      )
    ORDER BY
        CASE WHEN p_Orden = 'az'
            THEN TRIM(CONCAT_WS(' ', e.Nombre, e.PrimerApellido, e.SegundoApellido))
        END ASC,
        CASE WHEN p_Orden = 'za'
            THEN TRIM(CONCAT_WS(' ', e.Nombre, e.PrimerApellido, e.SegundoApellido))
        END DESC,
        r.FechaGeneracion DESC,
        r.IdReporte DESC;
END$$

DELIMITER ;



DROP PROCEDURE IF EXISTS SP_Reporte_ObtenerDetalle;

DELIMITER $$

CREATE PROCEDURE SP_Reporte_ObtenerDetalle(
    IN p_IdReporte INT
)
BEGIN
    IF NOT EXISTS(
        SELECT 1
        FROM TB_REPORTE
        WHERE IdReporte = p_IdReporte
          AND Activo = 1
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El reporte solicitado no esta disponible.';
    END IF;

    SELECT
        r.IdReporte,
        r.IdEstudiante,
        TRIM(CONCAT_WS(
            ' ',
            e.Nombre,
            e.PrimerApellido,
            e.SegundoApellido
        )) AS Estudiante,
        r.Titulo,
        r.PeriodoInicio,
        r.PeriodoFin,
        r.Contenido,
        r.RutaArchivo,
        r.VisibleEnPortal,
        r.FechaGeneracion,
        u.NombreCompleto AS GeneradoPor
    FROM TB_REPORTE r
    INNER JOIN TB_ESTUDIANTE e
        ON e.IdEstudiante = r.IdEstudiante
    INNER JOIN TB_USUARIO u
        ON u.IdUsuario = r.IdUsuarioGenero
    WHERE r.IdReporte = p_IdReporte;
END$$

DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Reporte_CambiarEstado;

DELIMITER $$

CREATE PROCEDURE SP_Reporte_CambiarEstado(
    IN p_IdReporte INT,
    IN p_Activo BOOLEAN
)
BEGIN
    IF p_IdReporte IS NULL
       OR p_IdReporte <= 0
       OR p_Activo IS NULL
       OR p_Activo NOT IN (0, 1) THEN

        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Indique un reporte y estado validos.';
    END IF;

    UPDATE TB_REPORTE
    SET Activo = p_Activo
    WHERE IdReporte = p_IdReporte;

    IF ROW_COUNT() = 0 AND NOT EXISTS(
        SELECT 1
        FROM TB_REPORTE
        WHERE IdReporte = p_IdReporte
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El reporte solicitado no existe.';
    END IF;
END$$

DELIMITER ;
DROP PROCEDURE IF EXISTS SP_Reporte_Generar;

DELIMITER $$

CREATE PROCEDURE SP_Reporte_Generar(
    IN p_IdUsuario INT,
    IN p_IdEstudiante INT,
    IN p_Titulo VARCHAR(150),
    IN p_Desde DATE,
    IN p_Hasta DATE,
    IN p_IncluirDatosGenerales BOOLEAN,
    IN p_IncluirSesiones BOOLEAN,
    IN p_IncluirPlanes BOOLEAN
)
generar_reporte: BEGIN
    DECLARE v_Contenido JSON;
    DECLARE v_IdReporte INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION WITH CONSISTENT SNAPSHOT;

    IF p_Titulo IS NULL
       OR TRIM(p_Titulo) = ''
       OR p_Desde IS NULL
       OR p_Hasta IS NULL
       OR p_Hasta < p_Desde THEN

        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Indique titulo y un periodo valido.';
    END IF;

    IF NOT EXISTS(
        SELECT 1
        FROM TB_ESTUDIANTE
        WHERE IdEstudiante = p_IdEstudiante
          AND Activo = 1
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'El estudiante no esta disponible.';
    END IF;

    IF NOT (
        COALESCE(p_IncluirDatosGenerales, 0)
        OR COALESCE(p_IncluirSesiones, 0)
        OR COALESCE(p_IncluirPlanes, 0)
    ) THEN
        SIGNAL SQLSTATE '45000'
        SET MESSAGE_TEXT = 'Seleccione al menos una seccion del reporte.';
    END IF;

    -- Validar sesiones si esa sección fue seleccionada.
    IF COALESCE(p_IncluirSesiones, 0) = 1 AND NOT EXISTS(
        SELECT 1
        FROM TB_SESION
        WHERE IdEstudiante = p_IdEstudiante
          AND Activo = 1
          AND DATE(FechaHoraInicio) BETWEEN p_Desde AND p_Hasta
    ) THEN
        COMMIT;
        SELECT 0 AS IdReporte;
        LEAVE generar_reporte;
    END IF;

    -- Validar planes si esa sección fue seleccionada.
    IF COALESCE(p_IncluirPlanes, 0) = 1 AND NOT EXISTS(
        SELECT 1
        FROM TB_PLAN_INTERVENCION
        WHERE IdEstudiante = p_IdEstudiante
          AND Activo = 1
          AND FechaInicio <= p_Hasta
          AND (FechaFin IS NULL OR FechaFin >= p_Desde)
    ) THEN
        COMMIT;
        SELECT 0 AS IdReporte;
        LEAVE generar_reporte;
    END IF;

    SELECT JSON_OBJECT(
        'estudiante', JSON_OBJECT(
            'nombre', CONCAT_WS(
                ' ',
                e.Nombre,
                e.PrimerApellido,
                e.SegundoApellido
            ),
            'nivel', n.Nombre,
            'institucion', i.Nombre,

            'areasDificultad', (
                SELECT GROUP_CONCAT(
                    a.Nombre
                    ORDER BY a.Nombre
                    SEPARATOR ', '
                )
                FROM TB_ESTUDIANTE_AREA ea
                INNER JOIN TB_AREA_DIFICULTAD a
                    ON a.IdAreaDificultad = ea.IdAreaDificultad
                WHERE ea.IdEstudiante = e.IdEstudiante
                  AND ea.Activo = 1
            ),

            'areasTrabajadas', IFNULL((
                SELECT JSON_ARRAYAGG(
                    JSON_OBJECT(
                        'nombre', a.Nombre,
                        'observacion', ea.Observacion,
                        'fechaDesactivacion', ea.FechaDesactivacion
                    )
                )
                FROM TB_ESTUDIANTE_AREA ea
                INNER JOIN TB_AREA_DIFICULTAD a
                    ON a.IdAreaDificultad = ea.IdAreaDificultad
                WHERE ea.IdEstudiante = e.IdEstudiante
                  AND ea.Activo = 0
            ), JSON_ARRAY())
        ),

        'sesiones', IFNULL((
            SELECT JSON_ARRAYAGG(
                JSON_OBJECT(
                    'fecha', DATE(s.FechaHoraInicio),
                    'tipoAtencion', ta.Nombre,
                    'objetivo', s.Objetivo,
                    'duracionMinutos', s.DuracionMinutos,
                    'tema', s.TemaTrabajado,
                    'actividadesRealizadas', s.ActividadesRealizadas,
                    'acuerdos', s.Acuerdos,
                    'objetivosAlcanzados', s.ObjetivosAlcanzados,
                    'habilidadesDesarrolladas', s.HabilidadesDesarrolladas,
                    'observaciones', s.Observaciones,
                    'avances', s.Avances,
                    'recomendaciones', s.Recomendaciones
                )
            )
            FROM TB_SESION s
            INNER JOIN TB_TIPO_ATENCION ta
                ON ta.IdTipoAtencion = s.IdTipoAtencion
            WHERE s.IdEstudiante = e.IdEstudiante
              AND s.Activo = 1
              AND DATE(s.FechaHoraInicio) BETWEEN p_Desde AND p_Hasta
        ), JSON_ARRAY()),

        'planes', IFNULL((
            SELECT JSON_ARRAYAGG(
                JSON_OBJECT(
                    'titulo', pl.Titulo,
                    'estado', ep.Nombre,
                    'objetivoGeneral', pl.ObjetivoGeneral,
                    'fechaInicio', pl.FechaInicio,
                    'fechaFin', pl.FechaFin,
                    'observaciones', pl.Observaciones,

                    'estrategias', IFNULL((
                        SELECT JSON_ARRAYAGG(
                            JSON_OBJECT(
                                'descripcion', es.Descripcion,
                                'orden', es.Orden,

                                'actividades', IFNULL((
                                    SELECT JSON_ARRAYAGG(
                                        JSON_OBJECT(
                                            'nombre', ac.Nombre,
                                            'descripcion', ac.Descripcion,
                                            'completada', IF(ac.Completada = 1, 1, 0)
                                        )
                                    )
                                    FROM TB_ACTIVIDAD ac
                                    WHERE ac.IdEstrategia = es.IdEstrategia
                                ), JSON_ARRAY())
                            )
                        )
                        FROM TB_ESTRATEGIA es
                        WHERE es.IdPlan = pl.IdPlan
                    ), JSON_ARRAY()),

                    'actividades', (
                        SELECT COUNT(*)
                        FROM TB_ACTIVIDAD ac
                        INNER JOIN TB_ESTRATEGIA es
                            ON es.IdEstrategia = ac.IdEstrategia
                        WHERE es.IdPlan = pl.IdPlan
                    ),

                    'actividadesCompletadas', (
                        SELECT COUNT(*)
                        FROM TB_ACTIVIDAD ac
                        INNER JOIN TB_ESTRATEGIA es
                            ON es.IdEstrategia = ac.IdEstrategia
                        WHERE es.IdPlan = pl.IdPlan
                          AND ac.Completada = 1
                    )
                )
            )
            FROM TB_PLAN_INTERVENCION pl
            INNER JOIN TB_ESTADO_PLAN ep
                ON ep.IdEstadoPlan = pl.IdEstadoPlan
            WHERE pl.IdEstudiante = e.IdEstudiante
              AND pl.Activo = 1
              AND pl.FechaInicio <= p_Hasta
              AND (pl.FechaFin IS NULL OR pl.FechaFin >= p_Desde)
        ), JSON_ARRAY())

    ) INTO v_Contenido
    FROM TB_ESTUDIANTE e
    LEFT JOIN TB_NIVEL_EDUCATIVO n
        ON n.IdNivelEducativo = e.IdNivelEducativo
    LEFT JOIN TB_INSTITUCION i
        ON i.IdInstitucion = e.IdInstitucion
    WHERE e.IdEstudiante = p_IdEstudiante;

    IF NOT COALESCE(p_IncluirDatosGenerales, 0) THEN
        SET v_Contenido = JSON_REMOVE(v_Contenido, '$.estudiante');
    END IF;

    IF NOT COALESCE(p_IncluirSesiones, 0) THEN
        SET v_Contenido = JSON_REMOVE(v_Contenido, '$.sesiones');
    END IF;

    IF NOT COALESCE(p_IncluirPlanes, 0) THEN
        SET v_Contenido = JSON_REMOVE(v_Contenido, '$.planes');
    END IF;

    INSERT INTO TB_REPORTE (
        IdEstudiante,
        IdUsuarioGenero,
        Titulo,
        PeriodoInicio,
        PeriodoFin,
        Contenido,
        VisibleEnPortal,
        Activo
    )
    VALUES (
        p_IdEstudiante,
        p_IdUsuario,
        TRIM(p_Titulo),
        p_Desde,
        p_Hasta,
        v_Contenido,
        0,
        1
    );

    SET v_IdReporte = LAST_INSERT_ID();

    COMMIT;

    SELECT v_IdReporte AS IdReporte;
END$$

DELIMITER ;

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
-- Programa los recordatorios de las citas que se acercan (HU-M10-2).
--
-- Dos cosas cambiaron respecto de la primera version:
--
--   El texto sale de la plantilla RecordatorioCita y ya no esta incrustado aqui.
--   La semilla de 02_catalogos.sql repite palabra por palabra el texto que tenia
--   antes, para que sacarlo de aqui no cambiara lo que recibe la familia.
--
--   El destinatario es el ENCARGADO y no su usuario de portal. Antes se exigia
--   usuario activo, asi que a un encargado que nunca activo la cuenta no se le
--   recordaba nada. Ahora alcanza con que tenga correo.
--
-- La regla antiduplicado mira la clave: sin eso, la confirmacion que ya se le
-- mando por esa misma cita contaria como recordatorio y el recordatorio no se
-- programaria nunca.
DROP PROCEDURE IF EXISTS SP_Notificacion_ProgramarRecordatorios;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_ProgramarRecordatorios()
BEGIN
  DECLARE v_Horas       INT DEFAULT 24;
  DECLARE v_IdCanal     INT DEFAULT NULL;
  DECLARE v_Asunto      VARCHAR(200) DEFAULT NULL;
  DECLARE v_Cuerpo      VARCHAR(1000) DEFAULT NULL;
  DECLARE v_Consultorio VARCHAR(200) DEFAULT NULL;

  SELECT CAST(Valor AS UNSIGNED) INTO v_Horas
  FROM TB_CONFIGURACION
  WHERE Clave = 'HorasRecordatorioCita';

  SET v_Horas = IFNULL(v_Horas, 24);

  SELECT IdCanalNotificacion INTO v_IdCanal
  FROM TB_CANAL_NOTIFICACION
  WHERE Nombre = 'Correo';

  IF v_IdCanal IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No esta configurado el canal de correo.';
  END IF;

  SELECT Asunto, LEFT(Cuerpo, 1000) INTO v_Asunto, v_Cuerpo
  FROM TB_PLANTILLA_MENSAJE
  WHERE Clave = 'RecordatorioCita';

  IF v_Cuerpo IS NULL THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Falta la plantilla RecordatorioCita.';
  END IF;

  SELECT LEFT(Valor, 200) INTO v_Consultorio
  FROM TB_CONFIGURACION
  WHERE Clave = 'NombreConsultorio';

  INSERT INTO TB_NOTIFICACION (IdCita, IdEncargado, IdCanalNotificacion, ClavePlantilla,
                               CorreoDestino, Asunto, Cuerpo, FechaProgramada)
  SELECT c.IdCita,
         e.IdEncargado,
         v_IdCanal,
         'RecordatorioCita',
         e.Correo,
         LEFT(FN_Plantilla_Sustituir(v_Asunto,
              TRIM(CONCAT_WS(' ', e.Nombre, e.PrimerApellido)),
              TRIM(CONCAT_WS(' ', es.Nombre, es.PrimerApellido)),
              DATE_FORMAT(c.FechaHoraInicio, '%d/%m/%Y'),
              DATE_FORMAT(c.FechaHoraInicio, '%H:%i'),
              v_Consultorio, NULL), 200),
         LEFT(FN_Plantilla_Sustituir(v_Cuerpo,
              TRIM(CONCAT_WS(' ', e.Nombre, e.PrimerApellido)),
              TRIM(CONCAT_WS(' ', es.Nombre, es.PrimerApellido)),
              DATE_FORMAT(c.FechaHoraInicio, '%d/%m/%Y'),
              DATE_FORMAT(c.FechaHoraInicio, '%H:%i'),
              v_Consultorio, NULL), 1000),
         c.FechaHoraInicio - INTERVAL v_Horas HOUR
  FROM TB_CITA c
  JOIN TB_ESTUDIANTE es ON es.IdEstudiante = c.IdEstudiante
  JOIN TB_ESTUDIANTE_ENCARGADO ee
    ON ee.IdEstudiante = c.IdEstudiante AND ee.EsPrincipal = 1
  JOIN TB_ENCARGADO e
    ON e.IdEncargado = ee.IdEncargado
   AND e.Activo = 1
   AND e.Correo IS NOT NULL
   AND TRIM(e.Correo) <> ''
  WHERE c.IdEstadoCita IN (1, 2)
    AND c.FechaHoraInicio > NOW()
    -- Se adelanta un dia a la hora de envio, para que el worker siempre
    -- encuentre la fila ya creada cuando le toque mandarla
    AND c.FechaHoraInicio - INTERVAL v_Horas HOUR <= NOW() + INTERVAL 1 DAY
    AND NOT EXISTS (SELECT 1 FROM TB_NOTIFICACION n
                    WHERE n.IdCita = c.IdCita
                      AND n.IdEncargado = e.IdEncargado
                      AND n.ClavePlantilla = 'RecordatorioCita');

  SELECT ROW_COUNT() AS RecordatoriosProgramados;
END$$
DELIMITER ;


-- Lo que el worker tiene que despachar en esta vuelta.
--
-- Se reescribio en la fase 3 por dos razones. La primera es que hacia
-- JOIN TB_USUARIO sobre IdUsuarioDestinatario, y desde que el destinatario
-- puede ser un encargado sin portal esa fila tiene el usuario en nulo: el JOIN
-- se las comia todas y el worker no habria mandado ni una. Ahora son LEFT JOIN y
-- el nombre sale del que venga lleno.
--
-- La segunda es el tope de intentos: una notificacion que ya fallo p_MaxIntentos
-- veces deja de aparecer, para que un correo imposible no se reintente para
-- siempre en cada vuelta. Sigue visible en la pantalla, marcada como fallida.
--
-- Solo el canal Correo: Portal se muestra dentro del sistema y no se despacha.
DROP PROCEDURE IF EXISTS SP_Notificacion_ObtenerPendientes;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_ObtenerPendientes(
  IN p_MaxIntentos INT,
  IN p_Limite      INT
)
BEGIN
  DECLARE v_Max    INT DEFAULT 3;
  DECLARE v_Limite INT DEFAULT 50;

  IF p_MaxIntentos IS NOT NULL AND p_MaxIntentos BETWEEN 1 AND 20 THEN
    SET v_Max = p_MaxIntentos;
  END IF;

  IF p_Limite IS NOT NULL AND p_Limite BETWEEN 1 AND 200 THEN
    SET v_Limite = p_Limite;
  END IF;

  SELECT n.IdNotificacion,
         n.IdCita,
         n.ClavePlantilla,
         COALESCE(NULLIF(TRIM(CONCAT_WS(' ', e.Nombre, e.PrimerApellido, e.SegundoApellido)), ''),
                  ud.NombreCompleto) AS Destinatario,
         COALESCE(NULLIF(TRIM(n.CorreoDestino), ''), ud.Correo) AS Correo,
         cn.Nombre AS Canal,
         n.Asunto,
         n.Cuerpo,
         n.FechaProgramada,
         n.Intentos
  FROM TB_NOTIFICACION n
  JOIN TB_CANAL_NOTIFICACION cn ON cn.IdCanalNotificacion = n.IdCanalNotificacion
  LEFT JOIN TB_ENCARGADO e ON e.IdEncargado = n.IdEncargado
  LEFT JOIN TB_USUARIO ud ON ud.IdUsuario = n.IdUsuarioDestinatario
  WHERE n.Enviada = 0
    AND n.FechaProgramada <= NOW()
    AND n.Intentos < v_Max
    AND cn.Nombre = 'Correo'
    -- Sin direccion no hay nada que mandar; la fila se queda pendiente y se ve
    AND COALESCE(NULLIF(TRIM(n.CorreoDestino), ''), ud.Correo) IS NOT NULL
  ORDER BY n.FechaProgramada, n.IdNotificacion
  LIMIT v_Limite;
END$$
DELIMITER ;

-- ------------------------------------------------------------
-- Mensajeria interna (HU-M10-4)
--
-- Solo existe entre un encargado y una persona del consultorio, en los dos
-- sentidos. La historia dice "padres de familia y estudiantes", y es un error de
-- redaccion del documento: el glosario del equipo es explicito en que los
-- estudiantes son menores, no se autentican y no hay rol de estudiante.
--
-- Quien es quien se deduce de TB_USUARIO.IdEncargado: lleno significa encargado,
-- nulo significa gente del consultorio. No hace falta mirar los roles.
--
-- Un hilo es el mensaje raiz (IdMensajePadre nulo) y las respuestas cuelgan
-- directo de el, sin anidar mas. Alcanza para una conversacion y evita el arbol
-- que nadie va a usar.
--
-- Estos procedimientos NO escriben bitacora, al contrario del correo manual: el
-- mensaje es el registro. Una fila de auditoria diciendo "se mando un mensaje"
-- al lado del mensaje mismo es el mismo hecho contado dos veces.
-- ------------------------------------------------------------

-- A quien le puede escribir el usuario que pregunta. Si es del consultorio, ve
-- los encargados con portal; si es encargado, ve la gente del consultorio.
-- Solo cuentas activas: a una cuenta bloqueada o pendiente no se le escribe
-- porque no va a poder contestar.
DROP PROCEDURE IF EXISTS SP_Mensaje_ListarDestinatarios;
DELIMITER $$
CREATE PROCEDURE SP_Mensaje_ListarDestinatarios(
  IN p_IdUsuario INT
)
BEGIN
  DECLARE v_EsEncargado BOOLEAN DEFAULT NULL;

  SELECT IdEncargado IS NOT NULL INTO v_EsEncargado
  FROM TB_USUARIO
  WHERE IdUsuario = p_IdUsuario;

  IF v_EsEncargado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El usuario indicado no existe.';
  END IF;

  SELECT u.IdUsuario,
         u.NombreCompleto,
         IFNULL(GROUP_CONCAT(DISTINCT r.Nombre ORDER BY r.Nombre SEPARATOR ', '), '') AS Roles
  FROM TB_USUARIO u
  LEFT JOIN TB_USUARIO_ROL ur ON ur.IdUsuario = u.IdUsuario
  LEFT JOIN TB_ROL r ON r.IdRol = ur.IdRol
  WHERE u.IdUsuario <> p_IdUsuario
    AND u.IdEstadoUsuario = 1
    AND ((v_EsEncargado = 1 AND u.IdEncargado IS NULL)
      OR (v_EsEncargado = 0 AND u.IdEncargado IS NOT NULL))
  GROUP BY u.IdUsuario, u.NombreCompleto
  ORDER BY u.NombreCompleto;
END$$
DELIMITER ;

-- Abre un hilo: marca como leidos los mensajes que iban para quien pregunta y
-- devuelve la conversacion completa en orden.
--
-- Hace las dos cosas en la misma llamada porque abrir un hilo ES leerlo. Si
-- fueran dos llamadas, la pantalla podria mostrar la conversacion y dejarla
-- marcada sin leer si la segunda falla.
--
-- Valida que quien pregunta sea parte del hilo: sin eso, cambiando el id en la
-- URL un encargado leeria la conversacion de otra familia.
DROP PROCEDURE IF EXISTS SP_Mensaje_AbrirHilo;
DELIMITER $$
CREATE PROCEDURE SP_Mensaje_AbrirHilo(
  IN p_IdUsuario INT,
  IN p_IdHilo    INT
)
BEGIN
  DECLARE v_Participa INT DEFAULT 0;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT COUNT(*) INTO v_Participa
  FROM TB_MENSAJE
  WHERE IdMensaje = p_IdHilo
    AND IdMensajePadre IS NULL
    AND (IdUsuarioRemitente = p_IdUsuario OR IdUsuarioDestinatario = p_IdUsuario);

  IF v_Participa = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La conversacion indicada no existe.';
  END IF;

  START TRANSACTION;

  UPDATE TB_MENSAJE
  SET Leido = 1,
      FechaLectura = NOW()
  WHERE (IdMensaje = p_IdHilo OR IdMensajePadre = p_IdHilo)
    AND IdUsuarioDestinatario = p_IdUsuario
    AND Leido = 0;

  COMMIT;

  SELECT m.IdMensaje,
         m.IdUsuarioRemitente,
         u.NombreCompleto AS Remitente,
         u.IdEncargado IS NOT NULL AS RemitenteEsEncargado,
         m.Asunto,
         m.Cuerpo,
         m.Leido,
         m.FechaEnvio,
         m.FechaLectura,
         m.IdUsuarioRemitente = p_IdUsuario AS Mio
  FROM TB_MENSAJE m
  JOIN TB_USUARIO u ON u.IdUsuario = m.IdUsuarioRemitente
  WHERE m.IdMensaje = p_IdHilo OR m.IdMensajePadre = p_IdHilo
  ORDER BY m.FechaEnvio, m.IdMensaje;
END$$
DELIMITER ;

-- Manda un mensaje. Con p_IdMensajePadre en nulo abre un hilo nuevo; con valor
-- responde en ese hilo.
--
-- Al responder, el destinatario NO se recibe como parametro: se saca del hilo,
-- para que no se pueda colar una respuesta a un tercero dentro de una
-- conversacion ajena. Y el asunto se hereda con "Re: " adelante.
DROP PROCEDURE IF EXISTS SP_Mensaje_Enviar;
DELIMITER $$
CREATE PROCEDURE SP_Mensaje_Enviar(
  IN p_IdUsuarioRemitente   INT,
  IN p_IdUsuarioDestinatario INT,
  IN p_IdMensajePadre       INT,
  IN p_Asunto               VARCHAR(200),
  IN p_Cuerpo               LONGTEXT
)
BEGIN
  DECLARE v_SoyEncargado   BOOLEAN DEFAULT NULL;
  DECLARE v_EsEncargado    BOOLEAN DEFAULT NULL;
  DECLARE v_Destino        INT DEFAULT NULL;
  DECLARE v_AsuntoHilo     VARCHAR(200) DEFAULT NULL;
  DECLARE v_Id             INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF p_Cuerpo IS NULL OR TRIM(p_Cuerpo) = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Escriba el mensaje.';
  END IF;

  SELECT IdEncargado IS NOT NULL INTO v_SoyEncargado
  FROM TB_USUARIO
  WHERE IdUsuario = p_IdUsuarioRemitente AND IdEstadoUsuario = 1;

  IF v_SoyEncargado IS NULL THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Su cuenta no esta activa.';
  END IF;

  IF p_IdMensajePadre IS NOT NULL THEN
    -- Responder: el destinatario y el asunto salen del hilo
    SELECT IF(IdUsuarioRemitente = p_IdUsuarioRemitente, IdUsuarioDestinatario, IdUsuarioRemitente),
           Asunto
      INTO v_Destino, v_AsuntoHilo
    FROM TB_MENSAJE
    WHERE IdMensaje = p_IdMensajePadre
      AND IdMensajePadre IS NULL
      AND (IdUsuarioRemitente = p_IdUsuarioRemitente
        OR IdUsuarioDestinatario = p_IdUsuarioRemitente);

    IF v_Destino IS NULL THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La conversacion indicada no existe.';
    END IF;

    SET p_Asunto = LEFT(IF(v_AsuntoHilo LIKE 'Re: %', v_AsuntoHilo, CONCAT('Re: ', v_AsuntoHilo)), 200);
  ELSE
    -- Hilo nuevo: hace falta asunto y destinatario
    IF p_Asunto IS NULL OR TRIM(p_Asunto) = '' THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el asunto del mensaje.';
    END IF;

    SET v_Destino = p_IdUsuarioDestinatario;
    SET p_Asunto = TRIM(p_Asunto);
  END IF;

  IF v_Destino = p_IdUsuarioRemitente THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No se puede escribir a uno mismo.';
  END IF;

  SELECT IdEncargado IS NOT NULL INTO v_EsEncargado
  FROM TB_USUARIO
  WHERE IdUsuario = v_Destino AND IdEstadoUsuario = 1;

  IF v_EsEncargado IS NULL THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'El destinatario indicado no existe o su cuenta no esta activa.';
  END IF;

  -- La regla de D-E: encargado con consultorio, nunca encargado con encargado ni
  -- consultorio con consultorio
  IF v_SoyEncargado = v_EsEncargado THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'La mensajeria es solo entre un encargado y el consultorio.';
  END IF;

  START TRANSACTION;

  INSERT INTO TB_MENSAJE (IdUsuarioRemitente, IdUsuarioDestinatario, IdMensajePadre, Asunto, Cuerpo)
  VALUES (p_IdUsuarioRemitente, v_Destino, p_IdMensajePadre, p_Asunto, TRIM(p_Cuerpo));

  SET v_Id = LAST_INSERT_ID();

  COMMIT;

  SELECT v_Id AS IdMensaje, IFNULL(p_IdMensajePadre, v_Id) AS IdHilo;
END$$
DELIMITER ;

-- Cuantos mensajes sin leer tiene quien pregunta. Para el contador del menu.
DROP PROCEDURE IF EXISTS SP_Mensaje_ContarSinLeer;
DELIMITER $$
CREATE PROCEDURE SP_Mensaje_ContarSinLeer(
  IN p_IdUsuario INT
)
BEGIN
  SELECT COUNT(*) AS SinLeer
  FROM TB_MENSAJE
  WHERE IdUsuarioDestinatario = p_IdUsuario
    AND Leido = 0;
END$$
DELIMITER ;

-- ------------------------------------------------------------
-- Avisos automaticos de cita (HU-M10-2 y HU-M10-3)
--
-- El aviso se inserta DENTRO de los procedimientos de la agenda, no desde el
-- API: asi ningun modulo que agende una cita en el futuro puede saltarse el
-- aviso por olvido. Son siete los procedimientos que crean o mueven citas,
-- porque las de grupo tienen su propio camino.
--
-- Los siete llaman a SP_Notificacion_AvisarCitas, que es una sola linea en cada
-- uno. Esta escrito para que NO PUEDA FALLAR, porque los siete tienen
-- EXIT HANDLER con ROLLBACK y RESIGNAL: cualquier error de aqui se llevaria la
-- cita por delante. De ahi las tres reglas que cumple:
--
--   1. inserta con INSERT ... SELECT, nunca con VALUES y subconsulta, para que
--      cuando no haya a quien avisarle inserte cero filas en vez de violar un
--      NOT NULL
--   2. no tiene un solo SIGNAL
--   3. si falta la plantilla o el canal, no avisa y se va en silencio: la cita
--      es lo importante y no se puede caer por esto
-- ------------------------------------------------------------

-- Sustituye los marcadores de una plantilla. Es el gemelo en SQL de
-- MarcadoresPlantilla del API, y existe porque el aviso de cita se arma dentro
-- de la transaccion de la agenda, donde no hay forma de llamar al C#.
--
-- La lista de marcadores queda entonces en dos lugares, uno por lenguaje, y cada
-- uno en un solo archivo. Si se agrega un marcador hay que tocar los dos.
--
-- Lo que viene NULL se deja con su marcador puesto, igual que en el API: un
-- texto que diga "la cita de  el  a las " es peor que uno que se note incompleto.
DROP FUNCTION IF EXISTS FN_Plantilla_Sustituir;
DELIMITER $$
CREATE FUNCTION FN_Plantilla_Sustituir(
  p_Texto       VARCHAR(4000),
  p_Encargado   VARCHAR(300),
  p_Estudiante  VARCHAR(300),
  p_Fecha       VARCHAR(20),
  p_Hora        VARCHAR(10),
  p_Consultorio VARCHAR(200),
  p_Motivo      VARCHAR(500)
)
RETURNS VARCHAR(4000)
DETERMINISTIC
NO SQL
BEGIN
  IF p_Texto IS NULL THEN
    RETURN NULL;
  END IF;

  RETURN REPLACE(
           REPLACE(
             REPLACE(
               REPLACE(
                 REPLACE(
                   REPLACE(p_Texto, '[Encargado]', IFNULL(p_Encargado, '[Encargado]')),
                 '[Estudiante]', IFNULL(p_Estudiante, '[Estudiante]')),
               '[Fecha]', IFNULL(p_Fecha, '[Fecha]')),
             '[Hora]', IFNULL(p_Hora, '[Hora]')),
           '[Consultorio]', IFNULL(p_Consultorio, '[Consultorio]')),
         '[Motivo]', IFNULL(p_Motivo, '[Motivo]'));
END$$
DELIMITER ;

-- Deja listo el aviso de una cita, o de todas las de un grupo, con el texto de
-- la plantilla que diga p_Clave y los marcadores ya sustituidos.
--
-- p_SoloVigentes separa los dos casos que existen: 1 para agendar, agregar al
-- grupo y reprogramar, que miran las citas programadas o confirmadas; 0 para
-- cancelar, que mira las que acaban de quedar canceladas. Sin esto, reprogramar
-- un grupo le avisaria tambien a quien ya tenia su cita cancelada.
--
-- Se le avisa al encargado principal del estudiante, y solo si esta activo y
-- tiene correo. Un estudiante sin encargado principal con correo no genera
-- aviso, y eso NO detiene la cita: por eso el INSERT ... SELECT.
DROP PROCEDURE IF EXISTS SP_Notificacion_AvisarCitas;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_AvisarCitas(
  IN p_IdCita       INT,
  IN p_IdGrupoCita  INT,
  IN p_Clave        VARCHAR(50),
  IN p_SoloVigentes BOOLEAN
)
BEGIN
  DECLARE v_Asunto      VARCHAR(200) DEFAULT NULL;
  DECLARE v_Cuerpo      VARCHAR(1000) DEFAULT NULL;
  DECLARE v_IdCanal     INT DEFAULT NULL;
  DECLARE v_Consultorio VARCHAR(200) DEFAULT NULL;

  SELECT Asunto, LEFT(Cuerpo, 1000) INTO v_Asunto, v_Cuerpo
  FROM TB_PLANTILLA_MENSAJE
  WHERE Clave = p_Clave;

  SELECT IdCanalNotificacion INTO v_IdCanal
  FROM TB_CANAL_NOTIFICACION
  WHERE Nombre = 'Correo';

  SELECT LEFT(Valor, 200) INTO v_Consultorio
  FROM TB_CONFIGURACION
  WHERE Clave = 'NombreConsultorio';

  -- Si la cita se movio o se cancelo, el recordatorio que estaba esperando quedo
  -- con la hora vieja. Se borra, y en el caso de reprogramar el worker lo vuelve
  -- a programar con la hora nueva en su siguiente vuelta.
  --
  -- Solo se tocan los que NO salieron todavia: un recordatorio ya enviado es
  -- historia y no se borra. Esto se encontro probando la fase 5: sin esto la
  -- familia recibia el aviso de que la cita se movio y despues un recordatorio
  -- con la hora anterior.
  IF p_Clave IN ('ActualizacionCita', 'CancelacionCita') THEN
    DELETE n FROM TB_NOTIFICACION n
    JOIN TB_CITA c ON c.IdCita = n.IdCita
    WHERE n.ClavePlantilla = 'RecordatorioCita'
      AND n.Enviada = 0
      AND ((p_IdCita IS NOT NULL AND c.IdCita = p_IdCita)
        OR (p_IdGrupoCita IS NOT NULL AND c.IdGrupoCita = p_IdGrupoCita));
  END IF;

  IF v_Cuerpo IS NOT NULL AND v_IdCanal IS NOT NULL THEN
    INSERT INTO TB_NOTIFICACION (IdCita, IdEncargado, IdCanalNotificacion, ClavePlantilla,
                                 CorreoDestino, Asunto, Cuerpo, FechaProgramada)
    SELECT c.IdCita,
           e.IdEncargado,
           v_IdCanal,
           p_Clave,
           e.Correo,
           LEFT(FN_Plantilla_Sustituir(v_Asunto, d.Encargado, d.Estudiante,
                                       d.Fecha, d.Hora, v_Consultorio, c.MotivoCancelacion), 200),
           LEFT(FN_Plantilla_Sustituir(v_Cuerpo, d.Encargado, d.Estudiante,
                                       d.Fecha, d.Hora, v_Consultorio, c.MotivoCancelacion), 1000),
           NOW()
    FROM TB_CITA c
    JOIN TB_ESTUDIANTE_ENCARGADO ee
      ON ee.IdEstudiante = c.IdEstudiante AND ee.EsPrincipal = 1
    JOIN TB_ENCARGADO e
      ON e.IdEncargado = ee.IdEncargado
     AND e.Activo = 1
     AND e.Correo IS NOT NULL
     AND TRIM(e.Correo) <> ''
    JOIN TB_ESTUDIANTE es
      ON es.IdEstudiante = c.IdEstudiante
    JOIN (SELECT c2.IdCita,
                 TRIM(CONCAT_WS(' ', e2.Nombre, e2.PrimerApellido)) AS Encargado,
                 TRIM(CONCAT_WS(' ', es2.Nombre, es2.PrimerApellido)) AS Estudiante,
                 DATE_FORMAT(c2.FechaHoraInicio, '%d/%m/%Y') AS Fecha,
                 DATE_FORMAT(c2.FechaHoraInicio, '%H:%i') AS Hora
          FROM TB_CITA c2
          JOIN TB_ESTUDIANTE_ENCARGADO ee2
            ON ee2.IdEstudiante = c2.IdEstudiante AND ee2.EsPrincipal = 1
          JOIN TB_ENCARGADO e2 ON e2.IdEncargado = ee2.IdEncargado
          JOIN TB_ESTUDIANTE es2 ON es2.IdEstudiante = c2.IdEstudiante) AS d
      ON d.IdCita = c.IdCita
    WHERE ((p_IdCita IS NOT NULL AND c.IdCita = p_IdCita)
        OR (p_IdGrupoCita IS NOT NULL AND c.IdGrupoCita = p_IdGrupoCita))
      AND ((IFNULL(p_SoloVigentes, 1) = 1 AND c.IdEstadoCita IN (1, 2))
        OR (IFNULL(p_SoloVigentes, 1) = 0 AND c.IdEstadoCita = 4));
  END IF;
END$$
DELIMITER ;

-- ------------------------------------------------------------
-- Correo manual y estado de las notificaciones (HU-M10-1)
--
-- El envio se hace en tres pasos y no en uno: primero se registra la
-- notificacion como pendiente, despues el API intenta mandarla, y al final se
-- marca enviada o fallida. Asi, si el proceso se cae a mitad de camino, queda
-- una fila pendiente que se puede ver, en vez de un correo que salio sin rastro
-- o un rastro de un correo que nunca salio.
--
-- Los mismos dos procedimientos de marcar los usa el worker de recordatorios de
-- la fase 3: el camino de salida es uno solo, venga de una persona o del reloj.
-- ------------------------------------------------------------

-- Los destinatarios que existen de verdad: encargado activo y con correo. Un
-- encargado sin correo no sale en la lista, que es la mitad de lo que pide el
-- escenario 2 de la historia; la otra mitad la valida el registro.
--
-- Devuelve dos resultados: los encargados y, aparte, sus estudiantes activos.
-- Los estudiantes van en su propia lista porque un encargado puede tener varios
-- y repetir sus datos en cada fila obliga a la pantalla a desarmar el cruce.
DROP PROCEDURE IF EXISTS SP_Correo_ListarDestinatarios;
DELIMITER $$
CREATE PROCEDURE SP_Correo_ListarDestinatarios()
BEGIN
  SELECT e.IdEncargado,
         TRIM(CONCAT_WS(' ', e.Nombre, e.PrimerApellido, e.SegundoApellido)) AS Encargado,
         e.Correo
  FROM TB_ENCARGADO e
  WHERE e.Activo = 1
    AND e.Correo IS NOT NULL
    AND TRIM(e.Correo) <> ''
  ORDER BY e.Nombre, e.PrimerApellido;

  SELECT ee.IdEncargado,
         es.IdEstudiante,
         TRIM(CONCAT_WS(' ', es.Nombre, es.PrimerApellido, es.SegundoApellido)) AS Estudiante
  FROM TB_ESTUDIANTE_ENCARGADO ee
  JOIN TB_ESTUDIANTE es ON es.IdEstudiante = ee.IdEstudiante AND es.Activo = 1
  JOIN TB_ENCARGADO e ON e.IdEncargado = ee.IdEncargado
  WHERE e.Activo = 1
    AND e.Correo IS NOT NULL
    AND TRIM(e.Correo) <> ''
  ORDER BY ee.IdEncargado, es.Nombre, es.PrimerApellido;
END$$
DELIMITER ;

-- Las citas del estudiante que se pueden mencionar en un correo escrito a mano.
-- Es lo que le da de comer al selector de cita del formulario, y es lo que
-- permite que [Fecha], [Hora] y [Motivo] se resuelvan tambien aqui y no solo en
-- los avisos automaticos.
--
-- No reusa SP_Cita_Listar aunque se parezca: esa es de la agenda, ordena de la
-- mas vieja a la mas nueva y no devuelve MotivoCancelacion, que es justo el dato
-- que [Motivo] necesita. Agregarle la columna le cambiaria la salida a un modulo
-- ajeno para servir a este.
--
-- El orden es el que sirve al redactar: primero las que vienen, de la mas
-- proxima a la mas lejana, y despues las que ya pasaron, de la mas reciente
-- hacia atras. Los 90 dias hacia atras evitan que el selector crezca sin fin
-- con citas viejas de las que ya nadie escribe.
DROP PROCEDURE IF EXISTS SP_Correo_ListarCitasEstudiante;
DELIMITER $$
CREATE PROCEDURE SP_Correo_ListarCitasEstudiante(
  IN p_IdEstudiante INT
)
BEGIN
  SELECT c.IdCita,
         c.IdEstudiante,
         c.FechaHoraInicio,
         ts.Nombre AS TipoSesion,
         ec.Nombre AS Estado,
         c.MotivoCancelacion
  FROM TB_CITA c
  JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = c.IdTipoSesion
  JOIN TB_ESTADO_CITA ec ON ec.IdEstadoCita = c.IdEstadoCita
  WHERE c.IdEstudiante = p_IdEstudiante
    AND c.FechaHoraInicio >= NOW() - INTERVAL 90 DAY
  ORDER BY (c.FechaHoraInicio < NOW()) ASC,
           IF(c.FechaHoraInicio >= NOW(), c.FechaHoraInicio, NULL) ASC,
           c.FechaHoraInicio DESC
  LIMIT 50;
END$$
DELIMITER ;

-- Registra el correo que una persona acaba de redactar, todavia sin enviar.
-- El canal lo resuelve por nombre y no lo recibe como parametro: un correo
-- manual siempre sale por correo, y pasarle el id abriria la puerta a guardarlo
-- con el canal equivocado.
--
-- El texto que recibe ya viene con los marcadores sustituidos: eso lo hace el
-- API, que es el unico que sabe los datos del estudiante y del consultorio.
DROP PROCEDURE IF EXISTS SP_Correo_Registrar;
DELIMITER $$
CREATE PROCEDURE SP_Correo_Registrar(
  IN p_IdUsuarioRegistro INT,
  IN p_IdEncargado       INT,
  IN p_IdCita            INT,
  IN p_CorreoDestino     VARCHAR(150),
  IN p_Asunto            VARCHAR(200),
  IN p_Cuerpo            VARCHAR(1000)
)
BEGIN
  DECLARE v_IdCanal INT DEFAULT NULL;
  DECLARE v_Id      INT;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  IF p_Asunto IS NULL OR TRIM(p_Asunto) = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el asunto del correo.';
  END IF;

  IF p_Cuerpo IS NULL OR TRIM(p_Cuerpo) = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el mensaje del correo.';
  END IF;

  IF p_CorreoDestino IS NULL OR TRIM(p_CorreoDestino) = '' THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'El destinatario no tiene correo registrado.';
  END IF;

  IF NOT EXISTS (SELECT 1 FROM TB_ENCARGADO
                 WHERE IdEncargado = p_IdEncargado AND Activo = 1) THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'El destinatario indicado no existe o esta inactivo.';
  END IF;

  -- La cita es opcional y solo viene cuando el texto hablaba de una. Se valida
  -- aqui aunque el API ya lo haya hecho, porque la llave foranea fallaria con un
  -- error de MySQL que sale como 500 y no dice nada.
  IF p_IdCita IS NOT NULL
     AND NOT EXISTS (SELECT 1 FROM TB_CITA WHERE IdCita = p_IdCita) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La cita indicada no existe.';
  END IF;

  SELECT IdCanalNotificacion INTO v_IdCanal
  FROM TB_CANAL_NOTIFICACION
  WHERE Nombre = 'Correo';

  IF v_IdCanal IS NULL THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'No esta configurado el canal de correo.';
  END IF;

  START TRANSACTION;

  -- IdCita queda guardado cuando el correo hablaba de una cita. No choca con el
  -- aviso automatico de esa misma cita: la regla antiduplicado de
  -- SP_Notificacion_ProgramarRecordatorios y el borrado de
  -- SP_Notificacion_AvisarCitas filtran por ClavePlantilla, y en el correo
  -- manual esa columna va en nulo.
  INSERT INTO TB_NOTIFICACION (IdEncargado, IdCita, IdUsuarioRegistro, IdCanalNotificacion,
                               CorreoDestino, Asunto, Cuerpo, FechaProgramada)
  VALUES (p_IdEncargado, p_IdCita, p_IdUsuarioRegistro, v_IdCanal,
          TRIM(p_CorreoDestino), TRIM(p_Asunto), TRIM(p_Cuerpo), NOW());

  SET v_Id = LAST_INSERT_ID();

  -- Un correo a una familia se audita: deja dicho quien lo mando y a donde
  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
  VALUES (p_IdUsuarioRegistro, 'TB_NOTIFICACION', v_Id, 'Crear',
          JSON_OBJECT('IdEncargado', p_IdEncargado,
                      'IdCita', p_IdCita,
                      'CorreoDestino', TRIM(p_CorreoDestino),
                      'Asunto', TRIM(p_Asunto)));

  COMMIT;

  SELECT v_Id AS IdNotificacion;
END$$
DELIMITER ;

-- Solo toca las pendientes: volver a marcar una que ya salio no la duplica ni
-- le mueve la fecha de envio.
DROP PROCEDURE IF EXISTS SP_Notificacion_MarcarEnviada;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_MarcarEnviada(
  IN p_IdNotificacion INT
)
BEGIN
  UPDATE TB_NOTIFICACION
  SET Enviada = 1,
      FechaEnvio = NOW(),
      Intentos = Intentos + 1,
      MensajeError = NULL
  WHERE IdNotificacion = p_IdNotificacion
    AND Enviada = 0;

  SELECT ROW_COUNT() AS Marcadas;
END$$
DELIMITER ;

-- Suma el intento y guarda el motivo. No lanza error: se la llama justo cuando
-- algo ya fallo, y que ademas reventara aqui taparia el error de verdad.
-- Para la burbuja de Comunicacion: avisos que agotaron sus intentos y no van a
-- volver a salir. El tope entra por parametro y no clavado, para que cuente lo
-- mismo que deja de reintentar SP_Notificacion_ObtenerPendientes, que lo recibe
-- igual desde la configuracion del worker.
DROP PROCEDURE IF EXISTS SP_Notificacion_ContarFallidas;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_ContarFallidas(
  IN p_MaxIntentos INT
)
BEGIN
  DECLARE v_Max INT DEFAULT 3;

  IF p_MaxIntentos IS NOT NULL AND p_MaxIntentos BETWEEN 1 AND 20 THEN
    SET v_Max = p_MaxIntentos;
  END IF;

  SELECT COUNT(*) AS Total
  FROM TB_NOTIFICACION
  WHERE Enviada = 0
    AND Intentos >= v_Max;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Notificacion_MarcarFallida;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_MarcarFallida(
  IN p_IdNotificacion INT,
  IN p_MensajeError   VARCHAR(500)
)
BEGIN
  UPDATE TB_NOTIFICACION
  SET Intentos = Intentos + 1,
      MensajeError = LEFT(IFNULL(NULLIF(TRIM(p_MensajeError), ''), 'Error no especificado.'), 500)
  WHERE IdNotificacion = p_IdNotificacion
    AND Enviada = 0;

  SELECT ROW_COUNT() AS Marcadas;
END$$
DELIMITER ;

-- Historial. p_SoloManuales separa lo que escribio una persona de lo que genero
-- el sistema, y lo decide IdUsuarioRegistro: con valor es manual, en nulo es
-- automatico. Es el dato, no una adivinanza por si la fila trae cita.
--
-- p_Limite acota la consulta; en nulo trae 50. El destinatario puede ser un
-- encargado o un usuario interno, asi que el nombre sale del que venga lleno.
DROP PROCEDURE IF EXISTS SP_Notificacion_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Notificacion_Listar(
  IN p_SoloManuales BOOLEAN,
  IN p_Limite       INT
)
BEGIN
  DECLARE v_Limite INT DEFAULT 50;

  IF p_Limite IS NOT NULL AND p_Limite BETWEEN 1 AND 500 THEN
    SET v_Limite = p_Limite;
  END IF;

  SELECT n.IdNotificacion,
         n.IdCita,
         n.IdEncargado,
         n.IdUsuarioRegistro,
         n.ClavePlantilla,
         -- NULLIF porque CONCAT_WS sobre puros NULL devuelve cadena vacia, no
         -- NULL, y sin esto COALESCE se queda con el vacio y nunca mira al usuario
         COALESCE(NULLIF(TRIM(CONCAT_WS(' ', e.Nombre, e.PrimerApellido, e.SegundoApellido)), ''),
                  ud.NombreCompleto) AS Destinatario,
         n.CorreoDestino,
         cn.Nombre AS Canal,
         ur.NombreCompleto AS Remitente,
         n.Asunto,
         n.Cuerpo,
         n.FechaProgramada,
         n.FechaEnvio,
         n.Enviada,
         n.Intentos,
         n.MensajeError
  FROM TB_NOTIFICACION n
  JOIN TB_CANAL_NOTIFICACION cn ON cn.IdCanalNotificacion = n.IdCanalNotificacion
  LEFT JOIN TB_ENCARGADO e ON e.IdEncargado = n.IdEncargado
  LEFT JOIN TB_USUARIO ud ON ud.IdUsuario = n.IdUsuarioDestinatario
  LEFT JOIN TB_USUARIO ur ON ur.IdUsuario = n.IdUsuarioRegistro
  WHERE (IFNULL(p_SoloManuales, 0) = 0 OR n.IdUsuarioRegistro IS NOT NULL)
  ORDER BY n.FechaProgramada DESC, n.IdNotificacion DESC
  LIMIT v_Limite;
END$$
DELIMITER ;

-- ------------------------------------------------------------
-- Plantillas de mensajes (HU-M10-5)
--
-- Van por procedimiento y no por consulta suelta, aunque sea una tabla plana:
-- tienen una regla que nadie debe poder saltarse, la de que una plantilla del
-- sistema no se desactiva. Molde: SP_TipoSesion_Guardar_CRM.
--
-- Clave llena = plantilla que usa el sistema solo (el recordatorio la busca por
-- ahi). Clave nula = plantilla de la usuaria. Nombre, asunto y cuerpo se editan
-- en las dos; lo unico que cambia es que la del sistema no se puede apagar,
-- porque el recordatorio automatico se quedaria sin texto.
--
-- Que los marcadores entre corchetes sean de la lista cerrada lo valida el
-- servicio, no esto: en MySQL habria que sacar los corchetes con REGEXP uno por
-- uno, y el guardia que de verdad importa esta en el envio, que se niega a
-- mandar un texto con un marcador sin resolver.
-- ------------------------------------------------------------

DROP PROCEDURE IF EXISTS SP_Plantilla_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Plantilla_Listar(
  IN p_SoloActivas BOOLEAN
)
BEGIN
  SELECT IdPlantilla,
         Clave,
         Nombre,
         Asunto,
         Cuerpo,
         Activa,
         FechaCreacion,
         FechaModificacion
  FROM TB_PLANTILLA_MENSAJE
  WHERE (IFNULL(p_SoloActivas, 0) = 0 OR Activa = 1)
  -- Las del sistema primero: son las que la usuaria busca mas seguido
  ORDER BY IF(Clave IS NULL, 1, 0), Nombre;
END$$
DELIMITER ;

DROP PROCEDURE IF EXISTS SP_Plantilla_ObtenerDetalle;
DELIMITER $$
CREATE PROCEDURE SP_Plantilla_ObtenerDetalle(
  IN p_IdPlantilla INT
)
BEGIN
  SELECT IdPlantilla,
         Clave,
         Nombre,
         Asunto,
         Cuerpo,
         Activa,
         FechaCreacion,
         FechaModificacion
  FROM TB_PLANTILLA_MENSAJE
  WHERE IdPlantilla = p_IdPlantilla;
END$$
DELIMITER ;

-- Asi la busca el recordatorio automatico y el envio de correo: por la clave,
-- que es estable, y no por id ni por nombre, que la usuaria puede cambiar.
-- Devuelve la plantilla aunque este inactiva: desactivar una del sistema no se
-- permite, y si alguien lo logra por la base es mejor que el aviso salga que
-- que se pierda en silencio.
DROP PROCEDURE IF EXISTS SP_Plantilla_ObtenerPorClave;
DELIMITER $$
CREATE PROCEDURE SP_Plantilla_ObtenerPorClave(
  IN p_Clave VARCHAR(50)
)
BEGIN
  SELECT IdPlantilla,
         Clave,
         Nombre,
         Asunto,
         Cuerpo,
         Activa,
         FechaCreacion,
         FechaModificacion
  FROM TB_PLANTILLA_MENSAJE
  WHERE Clave = p_Clave;
END$$
DELIMITER ;

-- Crea si p_IdPlantilla viene nulo y edita si trae valor, igual que
-- SP_TipoSesion_Guardar_CRM. Nunca asigna Clave: las del sistema nacen de
-- 02_catalogos.sql y no se crean desde la pantalla.
DROP PROCEDURE IF EXISTS SP_Plantilla_Guardar;
DELIMITER $$
CREATE PROCEDURE SP_Plantilla_Guardar(
  IN p_IdUsuarioAccion INT,
  IN p_IdPlantilla     INT,
  IN p_Nombre          VARCHAR(100),
  IN p_Asunto          VARCHAR(200),
  IN p_Cuerpo          LONGTEXT
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
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el nombre de la plantilla.';
  END IF;

  IF p_Cuerpo IS NULL OR TRIM(p_Cuerpo) = '' THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el contenido de la plantilla.';
  END IF;

  -- Se adelanta al indice unico para dar un mensaje y no un 1062 crudo
  IF EXISTS (SELECT 1 FROM TB_PLANTILLA_MENSAJE
             WHERE Nombre = TRIM(p_Nombre)
               AND (p_IdPlantilla IS NULL OR IdPlantilla <> p_IdPlantilla)) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe una plantilla con ese nombre.';
  END IF;

  IF p_IdPlantilla IS NOT NULL THEN
    SELECT JSON_OBJECT('Nombre', Nombre, 'Asunto', Asunto, 'Cuerpo', Cuerpo)
      INTO v_Anterior
    FROM TB_PLANTILLA_MENSAJE
    WHERE IdPlantilla = p_IdPlantilla;

    IF v_Anterior IS NULL THEN
      SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La plantilla indicada no existe.';
    END IF;
  END IF;

  START TRANSACTION;

  IF p_IdPlantilla IS NULL THEN
    INSERT INTO TB_PLANTILLA_MENSAJE (Clave, Nombre, Asunto, Cuerpo)
    VALUES (NULL, TRIM(p_Nombre), NULLIF(TRIM(IFNULL(p_Asunto, '')), ''), TRIM(p_Cuerpo));

    SET v_Id = LAST_INSERT_ID();
  ELSE
    UPDATE TB_PLANTILLA_MENSAJE
    SET Nombre = TRIM(p_Nombre),
        Asunto = NULLIF(TRIM(IFNULL(p_Asunto, '')), ''),
        Cuerpo = TRIM(p_Cuerpo)
    WHERE IdPlantilla = p_IdPlantilla;

    SET v_Id = p_IdPlantilla;
  END IF;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_PLANTILLA_MENSAJE', v_Id,
          IF(p_IdPlantilla IS NULL, 'Crear', 'Editar'), v_Anterior,
          JSON_OBJECT('Nombre', TRIM(p_Nombre), 'Asunto', p_Asunto, 'Cuerpo', TRIM(p_Cuerpo)));

  COMMIT;

  SELECT v_Id AS IdPlantilla;
END$$
DELIMITER ;

-- Baja logica. Apagar una plantilla del sistema se rechaza: el recordatorio
-- automatico la lee por Clave y se quedaria sin texto que mandar.
DROP PROCEDURE IF EXISTS SP_Plantilla_CambiarEstado;
DELIMITER $$
CREATE PROCEDURE SP_Plantilla_CambiarEstado(
  IN p_IdUsuarioAccion INT,
  IN p_IdPlantilla     INT,
  IN p_Activa          BOOLEAN
)
BEGIN
  DECLARE v_Activa  TINYINT DEFAULT NULL;
  DECLARE v_Clave   VARCHAR(50) DEFAULT NULL;
  DECLARE v_Existe  INT DEFAULT 0;

  DECLARE EXIT HANDLER FOR SQLEXCEPTION
  BEGIN
    ROLLBACK;
    RESIGNAL;
  END;

  SELECT COUNT(*), MAX(Activa), MAX(Clave)
    INTO v_Existe, v_Activa, v_Clave
  FROM TB_PLANTILLA_MENSAJE
  WHERE IdPlantilla = p_IdPlantilla;

  IF v_Existe = 0 THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La plantilla indicada no existe.';
  END IF;

  IF IFNULL(p_Activa, 0) = 0 AND v_Clave IS NOT NULL THEN
    SIGNAL SQLSTATE '45000'
      SET MESSAGE_TEXT = 'Esta plantilla la usa el sistema y no se puede desactivar.';
  END IF;

  IF v_Activa = IFNULL(p_Activa, 0) THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La plantilla ya esta en ese estado.';
  END IF;

  START TRANSACTION;

  UPDATE TB_PLANTILLA_MENSAJE
  SET Activa = IFNULL(p_Activa, 0)
  WHERE IdPlantilla = p_IdPlantilla;

  INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
  VALUES (p_IdUsuarioAccion, 'TB_PLANTILLA_MENSAJE', p_IdPlantilla, 'CambiarEstado',
          JSON_OBJECT('Activa', v_Activa),
          JSON_OBJECT('Activa', IFNULL(p_Activa, 0)));

  COMMIT;
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
    -- M5 renombro Fecha a FechaHoraInicio y agrego borrado logico (2026-10-07)
    (SELECT COUNT(*) FROM TB_SESION
      WHERE Activo = 1
        AND FechaHoraInicio >= CURDATE() - INTERVAL 30 DAY) AS SesionesUltimoMes,
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

-- =====================================================
-- INICIO DEL PORTAL
-- =====================================================

DROP PROCEDURE IF EXISTS SP_Portal_ObtenerInicio;
DELIMITER $$

CREATE PROCEDURE SP_Portal_ObtenerInicio(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT
)
BEGIN
  DECLARE v_IdEstudiante INT DEFAULT NULL;

  SELECT ee.IdEstudiante INTO v_IdEstudiante
  FROM TB_ESTUDIANTE_ENCARGADO ee
  JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
  JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
  WHERE u.IdUsuario = p_IdUsuario
    AND en.Activo = 1
    AND e.Activo = 1
    AND (p_IdEstudiante IS NULL OR ee.IdEstudiante = p_IdEstudiante)
  ORDER BY ee.EsPrincipal DESC, ee.IdEstudiante
  LIMIT 1;

  IF v_IdEstudiante IS NULL THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'No tiene estudiantes activos a su cargo o el estudiante no le corresponde.';
  END IF;

  -- Primer resultado: estudiantes vinculados al encargado.
  SELECT ee.IdEstudiante,
         TRIM(CONCAT_WS(' ', e.Nombre, e.PrimerApellido,
              NULLIF(e.SegundoApellido, ''))) AS Estudiante,
         ee.EsPrincipal
  FROM TB_ESTUDIANTE_ENCARGADO ee
  JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
  JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
  JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
  WHERE u.IdUsuario = p_IdUsuario
    AND en.Activo = 1
    AND e.Activo = 1
  ORDER BY ee.EsPrincipal DESC, e.Nombre, e.PrimerApellido, e.IdEstudiante;

  -- Segundo resultado: resumen del estudiante seleccionado.
  SELECT v_IdEstudiante AS IdEstudiante,

         (SELECT COUNT(*)
          FROM TB_CITA
          WHERE IdEstudiante = v_IdEstudiante
            AND IdEstadoCita IN (1, 2)
            AND FechaHoraInicio >= NOW()) AS CitasProximas,

         (SELECT COUNT(*)
          FROM TB_TAREA_ASIGNACION
          WHERE IdEstudiante = v_IdEstudiante
            AND IdEstadoTarea IN (1, 4)) AS TareasPendientes,

         (SELECT COUNT(*)
          FROM TB_REPORTE
          WHERE IdEstudiante = v_IdEstudiante
            AND VisibleEnPortal = 1
            AND Activo = 1) AS ReportesDisponibles,

         (SELECT IFNULL(SUM(
            GREATEST(
              p.Monto - IFNULL(
                (SELECT SUM(a.Monto)
                 FROM TB_ABONO a
                 WHERE a.IdPago = p.IdPago
                   AND a.Activo = 1),
                0
              ),
              0
            )
          ), 0)
          FROM TB_PAGO p
          WHERE p.IdEstudiante = v_IdEstudiante
            AND p.IdEstadoPago IN (2, 3, 4)) AS SaldoPendiente;
END$$

DELIMITER ;


-- =====================================================
-- CITAS DEL ESTUDIANTE
-- Filtros: proximas, futuras, pasadas,
--          canceladas, completadas, todas.
-- =====================================================

DROP PROCEDURE IF EXISTS SP_Portal_ListarCitas;
DELIMITER $$

CREATE PROCEDURE SP_Portal_ListarCitas(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT,
  IN p_Filtro VARCHAR(20)
)
BEGIN
  DECLARE v_Filtro VARCHAR(20);
  DECLARE v_Ahora DATETIME;

  SET v_Filtro = LOWER(
    COALESCE(NULLIF(TRIM(p_Filtro), ''), 'proximas')
  );

  SET v_Ahora = NOW();

  IF v_Filtro NOT IN (
    'proximas', 'futuras', 'pasadas',
    'canceladas', 'completadas', 'todas'
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El filtro de citas no es válido.';
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
    WHERE u.IdUsuario = p_IdUsuario
      AND ee.IdEstudiante = p_IdEstudiante
      AND en.Activo = 1
      AND e.Activo = 1
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El estudiante no está disponible o no está a su cargo.';
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
    AND (
      v_Filtro = 'todas'

      OR (v_Filtro = 'proximas'
          AND c.FechaHoraInicio >= v_Ahora
          AND c.IdEstadoCita IN (1, 2))

      OR (v_Filtro = 'futuras'
          AND c.FechaHoraInicio >= v_Ahora)

      OR (v_Filtro = 'pasadas'
          AND c.FechaHoraInicio < v_Ahora)

      OR (v_Filtro = 'canceladas'
          AND c.IdEstadoCita = 4)

      OR (v_Filtro = 'completadas'
          AND c.IdEstadoCita = 3)
    )
  ORDER BY
    CASE
      WHEN v_Filtro IN ('proximas', 'futuras')
      THEN c.FechaHoraInicio
    END ASC,

    CASE
      WHEN v_Filtro NOT IN ('proximas', 'futuras')
      THEN c.FechaHoraInicio
    END DESC,

    c.IdCita DESC;
END$$

DELIMITER ;


-- =====================================================
-- PROGRESO Y RECOMENDACIONES
-- Filtros de reportes: nuevos, viejos, todos.
-- Nuevos: generados en los últimos 30 días.
-- =====================================================

DROP PROCEDURE IF EXISTS SP_Portal_ListarProgreso;
DELIMITER $$

CREATE PROCEDURE SP_Portal_ListarProgreso(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT,
  IN p_FiltroReportes VARCHAR(20)
)
BEGIN
  DECLARE v_Filtro VARCHAR(20);
  DECLARE v_FechaLimite DATETIME;

  SET v_Filtro = LOWER(
    COALESCE(NULLIF(TRIM(p_FiltroReportes), ''), 'todos')
  );

  SET v_FechaLimite = DATE_SUB(NOW(), INTERVAL 30 DAY);

  IF v_Filtro NOT IN ('nuevos', 'viejos', 'todos') THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El filtro de reportes no es válido.';
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
    WHERE u.IdUsuario = p_IdUsuario
      AND ee.IdEstudiante = p_IdEstudiante
      AND en.Activo = 1
      AND e.Activo = 1
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El estudiante no está disponible o no está a su cargo.';
  END IF;

  -- Primer resultado: sesiones activas, avances y recomendaciones.
  SELECT s.IdSesion,
         DATE(s.FechaHoraInicio) AS Fecha,
         ta.Nombre AS TipoAtencion,
         s.TemaTrabajado,
         s.Avances,
         s.Recomendaciones
  FROM TB_SESION s
  JOIN TB_TIPO_ATENCION ta ON ta.IdTipoAtencion = s.IdTipoAtencion
  WHERE s.IdEstudiante = p_IdEstudiante
    AND s.Activo = 1
  ORDER BY s.FechaHoraInicio DESC, s.IdSesion DESC;

  -- Segundo resultado: reportes activos y visibles en el portal.
  SELECT r.IdReporte,
         r.Titulo,
         r.PeriodoInicio,
         r.PeriodoFin,
         r.FechaGeneracion
  FROM TB_REPORTE r
  WHERE r.IdEstudiante = p_IdEstudiante
    AND r.VisibleEnPortal = 1
    AND r.Activo = 1
    AND (
      v_Filtro = 'todos'

      OR (v_Filtro = 'nuevos'
          AND r.FechaGeneracion >= v_FechaLimite)

      OR (v_Filtro = 'viejos'
          AND r.FechaGeneracion < v_FechaLimite)
    )
  ORDER BY r.FechaGeneracion DESC, r.IdReporte DESC;
END$$

DELIMITER ;


-- =====================================================
-- DETALLE DEL REPORTE
-- Reutiliza SP_Reporte_ObtenerDetalle.
-- =====================================================

DROP PROCEDURE IF EXISTS SP_Portal_ObtenerReporte;
DELIMITER $$

CREATE PROCEDURE SP_Portal_ObtenerReporte(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT,
  IN p_IdReporte INT
)
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
    WHERE u.IdUsuario = p_IdUsuario
      AND ee.IdEstudiante = p_IdEstudiante
      AND en.Activo = 1
      AND e.Activo = 1
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El estudiante no está disponible o no está a su cargo.';
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM TB_REPORTE r
    WHERE r.IdReporte = p_IdReporte
      AND r.IdEstudiante = p_IdEstudiante
      AND r.Activo = 1
      AND r.VisibleEnPortal = 1
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El reporte solicitado no está disponible en el portal.';
  END IF;

  CALL SP_Reporte_ObtenerDetalle(p_IdReporte);
END$$

DELIMITER ;


-- =====================================================
-- TAREAS DEL ESTUDIANTE
-- Filtros: pendientes, asignadas, entregadas, todas.
-- Pendientes: Asignada (1) y Vencida (4).
-- =====================================================

DROP PROCEDURE IF EXISTS SP_Portal_ListarTareas;
DELIMITER $$

CREATE PROCEDURE SP_Portal_ListarTareas(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT,
  IN p_Filtro VARCHAR(20)
)
BEGIN
  DECLARE v_Filtro VARCHAR(20);

  SET v_Filtro = LOWER(
    COALESCE(NULLIF(TRIM(p_Filtro), ''), 'pendientes')
  );

  IF v_Filtro NOT IN (
    'pendientes', 'asignadas', 'entregadas', 'todas'
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El filtro de tareas no es válido.';
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
    WHERE u.IdUsuario = p_IdUsuario
      AND ee.IdEstudiante = p_IdEstudiante
      AND en.Activo = 1
      AND e.Activo = 1
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El estudiante no está disponible o no está a su cargo.';
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
         m.Url AS MaterialUrl,
         CASE
           WHEN NULLIF(TRIM(m.RutaArchivo), '') IS NOT NULL THEN 1
           ELSE 0
         END AS TieneArchivo
  FROM TB_TAREA_ASIGNACION ta
  JOIN TB_TAREA t ON t.IdTarea = ta.IdTarea
  JOIN TB_ESTADO_TAREA et ON et.IdEstadoTarea = ta.IdEstadoTarea
  LEFT JOIN TB_MATERIAL m ON m.IdMaterial = t.IdMaterial
                        AND m.Publicado = 1
  WHERE ta.IdEstudiante = p_IdEstudiante
    AND (
      v_Filtro = 'todas'

      OR (v_Filtro = 'pendientes'
          AND ta.IdEstadoTarea IN (1, 4))

      OR (v_Filtro = 'asignadas'
          AND ta.IdEstadoTarea = 1)

      OR (v_Filtro = 'entregadas'
          AND ta.IdEstadoTarea = 2)
    )
  ORDER BY
    CASE
      WHEN ta.IdEstadoTarea IN (1, 4) THEN 0
      ELSE 1
    END ASC,
    t.FechaLimite ASC,
    ta.IdTareaAsignacion ASC;
END$$

DELIMITER ;


-- =====================================================
-- ARCHIVO ADJUNTO DE UNA TAREA
-- Consulta los datos del archivo; la API realiza la descarga.
-- =====================================================

DROP PROCEDURE IF EXISTS SP_Portal_ObtenerArchivoTarea;
DELIMITER $$

CREATE PROCEDURE SP_Portal_ObtenerArchivoTarea(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT,
  IN p_IdTareaAsignacion INT
)
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
    WHERE u.IdUsuario = p_IdUsuario
      AND ee.IdEstudiante = p_IdEstudiante
      AND en.Activo = 1
      AND e.Activo = 1
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El estudiante no está disponible o no está a su cargo.';
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM TB_TAREA_ASIGNACION ta
    WHERE ta.IdTareaAsignacion = p_IdTareaAsignacion
      AND ta.IdEstudiante = p_IdEstudiante
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'La tarea no está asignada al estudiante indicado.';
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM TB_TAREA_ASIGNACION ta
    JOIN TB_TAREA t ON t.IdTarea = ta.IdTarea
    JOIN TB_MATERIAL m ON m.IdMaterial = t.IdMaterial
    WHERE ta.IdTareaAsignacion = p_IdTareaAsignacion
      AND ta.IdEstudiante = p_IdEstudiante
      AND m.Publicado = 1
      AND NULLIF(TRIM(m.RutaArchivo), '') IS NOT NULL
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'La tarea no tiene un archivo disponible para descargar.';
  END IF;

  SELECT ta.IdTareaAsignacion,
         t.IdTarea,
         t.Titulo AS Tarea,
         m.IdMaterial,
         m.Titulo AS Material,
         m.RutaArchivo
  FROM TB_TAREA_ASIGNACION ta
  JOIN TB_TAREA t ON t.IdTarea = ta.IdTarea
  JOIN TB_MATERIAL m ON m.IdMaterial = t.IdMaterial
  WHERE ta.IdTareaAsignacion = p_IdTareaAsignacion
    AND ta.IdEstudiante = p_IdEstudiante
    AND m.Publicado = 1
    AND NULLIF(TRIM(m.RutaArchivo), '') IS NOT NULL;
END$$

DELIMITER ;


-- =====================================================
-- ESTADO DE PAGOS
-- Filtros: pendientes, pagados, todos.
-- Solo consulta; no registra pagos ni actualiza estados.
-- =====================================================

DROP PROCEDURE IF EXISTS SP_Portal_ListarPagos;
DELIMITER $$

CREATE PROCEDURE SP_Portal_ListarPagos(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT,
  IN p_Filtro VARCHAR(20)
)
BEGIN
  DECLARE v_Filtro VARCHAR(20);

  SET v_Filtro = LOWER(
    COALESCE(NULLIF(TRIM(p_Filtro), ''), 'pendientes')
  );

  IF v_Filtro NOT IN ('pendientes', 'pagados', 'todos') THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El filtro de pagos no es válido.';
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
    WHERE u.IdUsuario = p_IdUsuario
      AND ee.IdEstudiante = p_IdEstudiante
      AND en.Activo = 1
      AND e.Activo = 1
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El estudiante no está disponible o no está a su cargo.';
  END IF;

  SELECT p.IdPago,
         p.Concepto,
         p.Monto,
         IFNULL(ab.Abonado, 0) AS Abonado,
         CASE
           WHEN p.IdEstadoPago = 5 THEN 0
           ELSE GREATEST(p.Monto - IFNULL(ab.Abonado, 0), 0)
         END AS Saldo,
         p.FechaEmision,
         p.FechaVencimiento,
         p.IdEstadoPago,
         ep.Nombre AS Estado
  FROM TB_PAGO p
  JOIN TB_ESTADO_PAGO ep ON ep.IdEstadoPago = p.IdEstadoPago
  LEFT JOIN (
    SELECT a.IdPago, SUM(a.Monto) AS Abonado
    FROM TB_ABONO a
    WHERE a.Activo = 1
    GROUP BY a.IdPago
  ) ab ON ab.IdPago = p.IdPago
  WHERE p.IdEstudiante = p_IdEstudiante
    AND (
      v_Filtro = 'todos'

      OR (v_Filtro = 'pendientes'
          AND p.IdEstadoPago IN (2, 3, 4))

      OR (v_Filtro = 'pagados'
          AND p.IdEstadoPago = 1)
    )
  ORDER BY p.FechaEmision DESC, p.IdPago DESC;
END$$

DELIMITER ;


-- =====================================================
-- DETALLE DE UN PAGO Y SUS ABONOS
-- =====================================================

DROP PROCEDURE IF EXISTS SP_Portal_ObtenerPago;
DELIMITER $$

CREATE PROCEDURE SP_Portal_ObtenerPago(
  IN p_IdUsuario INT,
  IN p_IdEstudiante INT,
  IN p_IdPago INT
)
BEGIN
  IF NOT EXISTS (
    SELECT 1
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_USUARIO u ON u.IdEncargado = ee.IdEncargado
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = ee.IdEstudiante
    WHERE u.IdUsuario = p_IdUsuario
      AND ee.IdEstudiante = p_IdEstudiante
      AND en.Activo = 1
      AND e.Activo = 1
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El estudiante no está disponible o no está a su cargo.';
  END IF;

  IF NOT EXISTS (
    SELECT 1
    FROM TB_PAGO p
    WHERE p.IdPago = p_IdPago
      AND p.IdEstudiante = p_IdEstudiante
  ) THEN
    SIGNAL SQLSTATE '45000'
    SET MESSAGE_TEXT = 'El cargo no existe o no corresponde al estudiante.';
  END IF;

  -- Primer resultado: encabezado del cargo.
  SELECT p.IdPago,
         p.IdEstudiante,
         p.Concepto,
         p.Monto,
         IFNULL(ab.Abonado, 0) AS Abonado,
         CASE
           WHEN p.IdEstadoPago = 5 THEN 0
           ELSE GREATEST(p.Monto - IFNULL(ab.Abonado, 0), 0)
         END AS Saldo,
         p.FechaEmision,
         p.FechaVencimiento,
         p.IdEstadoPago,
         ep.Nombre AS Estado
  FROM TB_PAGO p
  JOIN TB_ESTADO_PAGO ep ON ep.IdEstadoPago = p.IdEstadoPago
  LEFT JOIN (
    SELECT a.IdPago, SUM(a.Monto) AS Abonado
    FROM TB_ABONO a
    WHERE a.Activo = 1
    GROUP BY a.IdPago
  ) ab ON ab.IdPago = p.IdPago
  WHERE p.IdPago = p_IdPago
    AND p.IdEstudiante = p_IdEstudiante;

  -- Segundo resultado: abonos activos.
  SELECT a.IdAbono,
         a.IdPago,
         a.FechaAbono,
         a.Monto,
         mp.Nombre AS MetodoPago,
         a.Referencia
  FROM TB_ABONO a
  JOIN TB_METODO_PAGO mp ON mp.IdMetodoPago = a.IdMetodoPago
  JOIN TB_PAGO p ON p.IdPago = a.IdPago
  WHERE a.IdPago = p_IdPago
    AND p.IdEstudiante = p_IdEstudiante
    AND a.Activo = 1
  ORDER BY a.FechaAbono DESC, a.IdAbono DESC;
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
    DECLARE v_IdSolicitud INT;

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

    SET v_IdSolicitud = LAST_INSERT_ID();

    -- La primera columna sigue siendo el id, como antes. Las otras dos son para
    -- los dos correos que salen al registrar: el nombre del servicio lo resuelve
    -- la base, porque el INSERT de arriba descarta uno inactivo, y la fecha es la
    -- que quedo guardada y no la del reloj de quien pregunta.
    SELECT sc.IdSolicitud, s.Nombre AS Servicio, sc.FechaRegistro
    FROM TB_SOLICITUD_CONTACTO sc
    LEFT JOIN TB_SERVICIO s ON s.IdServicio = sc.IdServicioInteres
    WHERE sc.IdSolicitud = v_IdSolicitud;
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
-- Crea (p_IdTipoSesion NULL) o edita un tipo de sesion. La duracion y el precio
-- nuevos solo aplican a citas futuras: las ya agendadas y sus cobros no cambian.
DROP PROCEDURE IF EXISTS SP_TipoSesion_Guardar_CRM;
DELIMITER $$
CREATE PROCEDURE SP_TipoSesion_Guardar_CRM(
    IN p_IdUsuarioAccion INT,
    IN p_IdTipoSesion    INT,
    IN p_Nombre          VARCHAR(50),
    IN p_DuracionMinutos INT,
    IN p_Precio          DECIMAL(10,2)
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

    IF p_Precio IS NOT NULL AND p_Precio < 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El precio no puede ser negativo.';
    END IF;

    IF EXISTS (SELECT 1 FROM TB_TIPO_SESION
               WHERE Nombre = TRIM(p_Nombre)
                 AND (p_IdTipoSesion IS NULL OR IdTipoSesion <> p_IdTipoSesion)) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ya existe un tipo de sesión con ese nombre.';
    END IF;

    IF p_IdTipoSesion IS NOT NULL THEN
        SELECT JSON_OBJECT('Nombre', Nombre, 'DuracionMinutos', DuracionMinutos, 'Precio', Precio)
          INTO v_Anterior
        FROM TB_TIPO_SESION
        WHERE IdTipoSesion = p_IdTipoSesion;

        IF v_Anterior IS NULL THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El tipo de sesión indicado no existe.';
        END IF;
    END IF;

    START TRANSACTION;

    IF p_IdTipoSesion IS NULL THEN
        INSERT INTO TB_TIPO_SESION (Nombre, DuracionMinutos, Precio)
        VALUES (TRIM(p_Nombre), p_DuracionMinutos, p_Precio);

        SET v_Id = LAST_INSERT_ID();
    ELSE
        UPDATE TB_TIPO_SESION
        SET Nombre = TRIM(p_Nombre),
            DuracionMinutos = p_DuracionMinutos,
            Precio = p_Precio
        WHERE IdTipoSesion = p_IdTipoSesion;

        SET v_Id = p_IdTipoSesion;
    END IF;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_TIPO_SESION', v_Id,
            IF(p_IdTipoSesion IS NULL, 'Crear', 'Editar'), v_Anterior,
            JSON_OBJECT('Nombre', TRIM(p_Nombre), 'DuracionMinutos', p_DuracionMinutos, 'Precio', p_Precio));

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

-- ============================================================
-- 15. Pagos del CRM
-- ============================================================
-- Ids de TB_ESTADO_PAGO: 1 Pagado, 2 Pendiente, 3 Vencido, 4 Parcial, 5 Anulado.
-- El estado nunca se escribe a mano: lo calcula SP_Pago_RecalcularEstado
-- (seccion 7) segun los abonos. Aqui se reutilizan tambien
-- SP_Pago_RegistrarAbono y SP_Pago_AnularAbono.


-- Dias de plazo de un cobro (TB_CONFIGURACION.DiasVencimientoPago). 0 = no vence.
DROP FUNCTION IF EXISTS FN_Pago_DiasVencimiento;
DELIMITER $$
CREATE FUNCTION FN_Pago_DiasVencimiento()
RETURNS INT
READS SQL DATA
BEGIN
    DECLARE v_Dias INT DEFAULT NULL;

    SELECT CAST(Valor AS UNSIGNED) INTO v_Dias
    FROM TB_CONFIGURACION
    WHERE Clave = 'DiasVencimientoPago';

    RETURN IFNULL(v_Dias, 8);
END$$
DELIMITER ;


-- Lo que se le cobraria a cada cita: precio de su tipo de sesion. Solo citas
-- cuyo tipo tiene precio y que todavia no tienen cobro.
CREATE OR REPLACE VIEW VW_Cita_CobroSugerido AS
SELECT c.IdCita,
       c.IdEstudiante,
       c.IdUsuarioRegistro,
       c.IdEstadoCita,
       c.FechaHoraFin,
       CONCAT(ts.Nombre, ' - ', DATE_FORMAT(c.FechaHoraInicio, '%d/%m/%Y')) AS Concepto,
       ts.Precio AS Monto
FROM TB_CITA c
JOIN TB_TIPO_SESION ts ON ts.IdTipoSesion = c.IdTipoSesion
WHERE ts.Precio > 0
  AND NOT EXISTS (SELECT 1 FROM TB_PAGO p WHERE p.IdCita = c.IdCita);


-- Crea los cobros pendientes de citas que se completan. Uso interno: no abre
-- transaccion propia porque la llaman SPs que ya tienen una.
--   p_IdCita con valor -> solo esa cita, ya completada (al registrar su sesion)
--   p_IdCita NULL      -> las citas que estan por completarse (antes del UPDATE
--                         de SP_Cita_CompletarVencidas_CRM)
DROP PROCEDURE IF EXISTS SP_Pago_GenerarCobrosCitas;
DELIMITER $$
CREATE PROCEDURE SP_Pago_GenerarCobrosCitas(
    IN p_IdCita INT,
    IN p_Ahora  DATETIME
)
BEGIN
    DECLARE v_Dias INT DEFAULT FN_Pago_DiasVencimiento();

    INSERT INTO TB_PAGO (IdEstudiante, IdEstadoPago, IdUsuarioRegistro, IdCita, Concepto, Monto,
                         FechaEmision, FechaVencimiento, Observaciones)
    SELECT v.IdEstudiante,
           2,
           v.IdUsuarioRegistro,
           v.IdCita,
           v.Concepto,
           v.Monto,
           DATE(v.FechaHoraFin),
           IF(v_Dias > 0, DATE_ADD(DATE(v.FechaHoraFin), INTERVAL v_Dias DAY), NULL),
           'Cobro generado al completarse la cita.'
    FROM VW_Cita_CobroSugerido v
    WHERE (p_IdCita IS NOT NULL AND v.IdCita = p_IdCita AND v.IdEstadoCita = 3)
       OR (p_IdCita IS NULL AND v.IdEstadoCita IN (1, 2) AND v.FechaHoraFin <= p_Ahora);
END$$
DELIMITER ;


-- Listado de la pantalla de Pagos. Antes de listar pasa a Vencido lo que ya
-- vencio sin abonos, para que la pantalla siempre muestre el estado real.
DROP PROCEDURE IF EXISTS SP_Pago_Listar;
DELIMITER $$
CREATE PROCEDURE SP_Pago_Listar(
    IN p_Hoy DATE
)
BEGIN
    UPDATE TB_PAGO p
    SET p.IdEstadoPago = 3,
        p.FechaModificacion = NOW()
    WHERE p.IdPago > 0
      AND p.IdEstadoPago = 2
      AND p.FechaVencimiento IS NOT NULL
      AND p.FechaVencimiento < p_Hoy
      AND NOT EXISTS (SELECT 1 FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1);

    SELECT p.IdPago,
           p.IdEstudiante,
           TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
           (SELECT TRIM(CONCAT(en.Nombre, ' ', en.PrimerApellido))
            FROM TB_ESTUDIANTE_ENCARGADO ee
            JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
            WHERE ee.IdEstudiante = p.IdEstudiante
            ORDER BY ee.EsPrincipal DESC, ee.IdEncargado
            LIMIT 1) AS Encargado,
           p.Concepto,
           p.Monto,
           IFNULL(ab.Abonado, 0) AS Abonado,
           p.Monto - IFNULL(ab.Abonado, 0) AS Saldo,
           p.FechaEmision,
           p.FechaVencimiento,
           p.IdEstadoPago,
           ep.Nombre AS Estado,
           p.IdCita,
           (SELECT mp.Nombre
            FROM TB_ABONO a
            JOIN TB_METODO_PAGO mp ON mp.IdMetodoPago = a.IdMetodoPago
            WHERE a.IdPago = p.IdPago AND a.Activo = 1
            ORDER BY a.FechaAbono DESC, a.IdAbono DESC
            LIMIT 1) AS MetodoPago
    FROM TB_PAGO p
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = p.IdEstudiante
    JOIN TB_ESTADO_PAGO ep ON ep.IdEstadoPago = p.IdEstadoPago
    LEFT JOIN (SELECT IdPago, SUM(Monto) AS Abonado
               FROM TB_ABONO
               WHERE Activo = 1
               GROUP BY IdPago) ab ON ab.IdPago = p.IdPago
    ORDER BY p.FechaEmision DESC, p.IdPago DESC;
END$$
DELIMITER ;


-- Detalle de un cobro en 3 resultados:
--   1) el cobro, 2) sus abonos (tambien los anulados), 3) los encargados del
--   estudiante, para elegir quien paga al registrar un abono
DROP PROCEDURE IF EXISTS SP_Pago_Obtener;
DELIMITER $$
CREATE PROCEDURE SP_Pago_Obtener(
    IN p_IdPago INT
)
BEGIN
    DECLARE v_IdEstudiante INT DEFAULT NULL;

    SELECT IdEstudiante INTO v_IdEstudiante FROM TB_PAGO WHERE IdPago = p_IdPago;

    IF v_IdEstudiante IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cobro indicado no existe.';
    END IF;

    SELECT p.IdPago,
           p.IdEstudiante,
           TRIM(CONCAT(e.Nombre, ' ', e.PrimerApellido)) AS Estudiante,
           p.Concepto,
           p.Monto,
           IFNULL((SELECT SUM(a.Monto) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1), 0) AS Abonado,
           p.Monto - IFNULL((SELECT SUM(a.Monto) FROM TB_ABONO a WHERE a.IdPago = p.IdPago AND a.Activo = 1), 0) AS Saldo,
           p.FechaEmision,
           p.FechaVencimiento,
           p.IdEstadoPago,
           ep.Nombre AS Estado,
           p.IdCita,
           c.FechaHoraInicio AS FechaCita,
           p.Observaciones
    FROM TB_PAGO p
    JOIN TB_ESTUDIANTE e ON e.IdEstudiante = p.IdEstudiante
    JOIN TB_ESTADO_PAGO ep ON ep.IdEstadoPago = p.IdEstadoPago
    LEFT JOIN TB_CITA c ON c.IdCita = p.IdCita
    WHERE p.IdPago = p_IdPago;

    SELECT a.IdAbono,
           a.FechaAbono,
           a.Monto,
           mp.Nombre AS MetodoPago,
           TRIM(CONCAT(en.Nombre, ' ', en.PrimerApellido)) AS Pagador,
           a.Referencia,
           a.Observaciones,
           a.Activo
    FROM TB_ABONO a
    JOIN TB_METODO_PAGO mp ON mp.IdMetodoPago = a.IdMetodoPago
    JOIN TB_ENCARGADO en ON en.IdEncargado = a.IdEncargadoPagador
    WHERE a.IdPago = p_IdPago
    ORDER BY a.FechaAbono DESC, a.IdAbono DESC;

    SELECT en.IdEncargado,
           TRIM(CONCAT(en.Nombre, ' ', en.PrimerApellido)) AS Nombre,
           ee.EsPrincipal
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    WHERE ee.IdEstudiante = v_IdEstudiante
    ORDER BY ee.EsPrincipal DESC, en.Nombre;
END$$
DELIMITER ;


-- Encargados de un estudiante: para el modal de Nuevo pago cuando ya se cobro
DROP PROCEDURE IF EXISTS SP_Pago_EncargadosEstudiante;
DELIMITER $$
CREATE PROCEDURE SP_Pago_EncargadosEstudiante(
    IN p_IdEstudiante INT
)
BEGIN
    SELECT en.IdEncargado,
           TRIM(CONCAT(en.Nombre, ' ', en.PrimerApellido)) AS Nombre,
           ee.EsPrincipal
    FROM TB_ESTUDIANTE_ENCARGADO ee
    JOIN TB_ENCARGADO en ON en.IdEncargado = ee.IdEncargado
    WHERE ee.IdEstudiante = p_IdEstudiante
    ORDER BY ee.EsPrincipal DESC, en.Nombre;
END$$
DELIMITER ;


DROP PROCEDURE IF EXISTS SP_Pago_ListarMetodos;
DELIMITER $$
CREATE PROCEDURE SP_Pago_ListarMetodos()
BEGIN
    SELECT IdMetodoPago, Nombre
    FROM TB_METODO_PAGO
    ORDER BY IdMetodoPago;
END$$
DELIMITER ;


-- "Nuevo pago" manual. Crea el cobro y, si p_YaPagado = 1, registra en la misma
-- transaccion el abono por el monto completo: o se guarda todo o nada.
-- Sin fecha de vencimiento se usa el plazo de la configuracion.
DROP PROCEDURE IF EXISTS SP_Pago_Registrar;
DELIMITER $$
CREATE PROCEDURE SP_Pago_Registrar(
    IN p_IdUsuarioAccion    INT,
    IN p_IdEstudiante       INT,
    IN p_Concepto           VARCHAR(150),
    IN p_Monto              DECIMAL(10,2),
    IN p_FechaEmision       DATE,
    IN p_FechaVencimiento   DATE,
    IN p_Observaciones      VARCHAR(500),
    IN p_YaPagado           BOOLEAN,
    IN p_IdEncargadoPagador INT,
    IN p_IdMetodoPago       INT,
    IN p_Referencia         VARCHAR(100),
    IN p_Hoy                DATE
)
BEGIN
    DECLARE v_IdPago   INT;
    DECLARE v_Emision  DATE;
    DECLARE v_Vence    DATE;
    DECLARE v_Dias     INT DEFAULT FN_Pago_DiasVencimiento();

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_Concepto IS NULL OR TRIM(p_Concepto) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el concepto del cobro.';
    END IF;

    IF p_Monto IS NULL OR p_Monto <= 0 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El monto tiene que ser mayor que cero.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE WHERE IdEstudiante = p_IdEstudiante AND Activo = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El estudiante indicado no existe o está inactivo.';
    END IF;

    SET v_Emision = IFNULL(p_FechaEmision, p_Hoy);

    IF v_Emision > p_Hoy THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La fecha del cobro no puede ser futura.';
    END IF;

    SET v_Vence = IFNULL(p_FechaVencimiento,
                         IF(v_Dias > 0, DATE_ADD(v_Emision, INTERVAL v_Dias DAY), NULL));

    IF v_Vence IS NOT NULL AND v_Vence < v_Emision THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El vencimiento no puede ser anterior a la fecha del cobro.';
    END IF;

    IF IFNULL(p_YaPagado, 0) = 1 THEN
        IF NOT EXISTS (SELECT 1 FROM TB_METODO_PAGO WHERE IdMetodoPago = p_IdMetodoPago) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Seleccione el método de pago.';
        END IF;

        IF NOT EXISTS (SELECT 1 FROM TB_ESTUDIANTE_ENCARGADO
                       WHERE IdEstudiante = p_IdEstudiante AND IdEncargado = p_IdEncargadoPagador) THEN
            SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Seleccione quién pagó: debe ser un encargado del estudiante.';
        END IF;
    END IF;

    START TRANSACTION;

    INSERT INTO TB_PAGO (IdEstudiante, IdEstadoPago, IdUsuarioRegistro, Concepto, Monto,
                         FechaEmision, FechaVencimiento, Observaciones)
    VALUES (p_IdEstudiante, 2, p_IdUsuarioAccion, TRIM(p_Concepto), p_Monto,
            v_Emision, v_Vence, NULLIF(TRIM(p_Observaciones), ''));

    SET v_IdPago = LAST_INSERT_ID();

    IF IFNULL(p_YaPagado, 0) = 1 THEN
        INSERT INTO TB_ABONO (IdPago, IdEncargadoPagador, IdMetodoPago, IdUsuarioRegistro,
                              Monto, FechaAbono, Referencia)
        VALUES (v_IdPago, p_IdEncargadoPagador, p_IdMetodoPago, p_IdUsuarioAccion,
                p_Monto, v_Emision, NULLIF(TRIM(p_Referencia), ''));
    END IF;

    CALL SP_Pago_RecalcularEstado(v_IdPago);

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_PAGO', v_IdPago, 'Crear',
            JSON_OBJECT('IdEstudiante', p_IdEstudiante, 'Concepto', TRIM(p_Concepto),
                        'Monto', p_Monto, 'YaPagado', IFNULL(p_YaPagado, 0)));

    COMMIT;

    SELECT v_IdPago AS IdPago;
END$$
DELIMITER ;


-- Anula un cobro. Si tiene abonos activos no se deja: primero hay que anularlos
-- (asi el dinero recibido nunca desaparece sin rastro).
DROP PROCEDURE IF EXISTS SP_Pago_Anular;
DELIMITER $$
CREATE PROCEDURE SP_Pago_Anular(
    IN p_IdUsuarioAccion INT,
    IN p_IdPago          INT,
    IN p_Motivo          VARCHAR(300)
)
BEGIN
    DECLARE v_Estado INT DEFAULT NULL;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF p_Motivo IS NULL OR TRIM(p_Motivo) = '' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Indique el motivo de la anulación.';
    END IF;

    SELECT IdEstadoPago INTO v_Estado FROM TB_PAGO WHERE IdPago = p_IdPago;

    IF v_Estado IS NULL THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cobro indicado no existe.';
    END IF;

    IF v_Estado = 5 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Ese cobro ya está anulado.';
    END IF;

    IF EXISTS (SELECT 1 FROM TB_ABONO WHERE IdPago = p_IdPago AND Activo = 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El cobro tiene pagos registrados. Anúlelos primero.';
    END IF;

    START TRANSACTION;

    UPDATE TB_PAGO
    SET IdEstadoPago = 5,
        Observaciones = CONCAT_WS(' | ', Observaciones, CONCAT('Anulado: ', TRIM(p_Motivo))),
        FechaModificacion = NOW()
    WHERE IdPago = p_IdPago;

    INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorAnterior, ValorNuevo)
    VALUES (p_IdUsuarioAccion, 'TB_PAGO', p_IdPago, 'CambiarEstado',
            JSON_OBJECT('IdEstadoPago', v_Estado),
            JSON_OBJECT('IdEstadoPago', 5, 'Motivo', TRIM(p_Motivo)));

    COMMIT;
END$$
DELIMITER ;