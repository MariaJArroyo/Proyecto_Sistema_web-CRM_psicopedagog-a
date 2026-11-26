-- ============================================================
-- Mente Activa CRM - 06 Migracion de la mensajeria
-- MySQL 8.4 LTS
--
-- Para bases creadas antes de la bandeja del consultorio. Una base nueva
-- no la necesita: 01 ya crea TB_MENSAJE con el destinatario nulo.
--
-- Orden: 01, 02, este, 05. Se puede correr las veces que sea.
--
-- 1. TB_MENSAJE.IdUsuarioDestinatario pasa a aceptar nulo (nulo = el
--    mensaje va a la bandeja del consultorio).
-- 2. Los mensajes viejos de un encargado a una persona del consultorio
--    pasan al consultorio. Los del consultorio al encargado no cambian.
-- ============================================================

SET NAMES utf8mb4;

USE mente_activa;

-- El ALTER solo corre si la columna todavia es NOT NULL
SET @v_Nulo = (
  SELECT IS_NULLABLE
  FROM information_schema.columns
  WHERE table_schema = DATABASE()
    AND LOWER(table_name) = 'tb_mensaje'
    AND LOWER(column_name) = 'idusuariodestinatario');

SET @v_Sql = IF(@v_Nulo = 'NO',
  'ALTER TABLE TB_MENSAJE MODIFY IdUsuarioDestinatario INT NULL',
  'DO 0');

PREPARE v_Sentencia FROM @v_Sql;
EXECUTE v_Sentencia;
DEALLOCATE PREPARE v_Sentencia;

UPDATE TB_MENSAJE m
JOIN TB_USUARIO rem ON rem.IdUsuario = m.IdUsuarioRemitente
JOIN TB_USUARIO des ON des.IdUsuario = m.IdUsuarioDestinatario
SET m.IdUsuarioDestinatario = NULL
WHERE rem.IdEncargado IS NOT NULL
  AND des.IdEncargado IS NULL;
