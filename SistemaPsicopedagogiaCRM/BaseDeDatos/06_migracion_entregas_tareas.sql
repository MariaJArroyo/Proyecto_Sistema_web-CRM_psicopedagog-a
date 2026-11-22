-- ============================================================
-- Migracion: entregas de tareas desde el portal
-- ============================================================
-- Solo para una base que ya existia antes de este cambio. Una base nueva ya
-- trae todo desde 01_esquema.sql y 02_catalogos.sql.
--
-- Se puede correr varias veces: cada cambio revisa primero si ya esta hecho.
-- Despues de esto hay que correr 05_procedimientos.sql (o al menos los
-- procedimientos de tareas).

USE mente_activa;

DROP PROCEDURE IF EXISTS SP_Migracion_AgregarSiFalta;
DELIMITER $$
CREATE PROCEDURE SP_Migracion_AgregarSiFalta(
  IN p_Tabla VARCHAR(64),
  IN p_Objeto VARCHAR(64),
  IN p_Tipo VARCHAR(20),          -- COLUMNA, INDICE o RESTRICCION
  IN p_Sentencia TEXT
)
BEGIN
  DECLARE v_Existe INT DEFAULT 0;

  IF p_Tipo = 'COLUMNA' THEN
    SELECT COUNT(*) INTO v_Existe FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_Tabla AND COLUMN_NAME = p_Objeto;
  ELSEIF p_Tipo = 'INDICE' THEN
    SELECT COUNT(*) INTO v_Existe FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_Tabla AND INDEX_NAME = p_Objeto;
  ELSE
    SELECT COUNT(*) INTO v_Existe FROM information_schema.TABLE_CONSTRAINTS
    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = p_Tabla AND CONSTRAINT_NAME = p_Objeto;
  END IF;

  IF v_Existe = 0 THEN
    SET @sentencia = p_Sentencia;
    PREPARE consulta FROM @sentencia;
    EXECUTE consulta;
    DEALLOCATE PREPARE consulta;
  END IF;
END$$
DELIMITER ;

CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'ArchivoEntrega', 'COLUMNA',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD COLUMN ArchivoEntrega VARCHAR(100) NULL AFTER ComentarioRevision');
CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'TipoArchivoEntrega', 'COLUMNA',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD COLUMN TipoArchivoEntrega VARCHAR(50) NULL AFTER ArchivoEntrega');
CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'NombreArchivoEntrega', 'COLUMNA',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD COLUMN NombreArchivoEntrega VARCHAR(255) NULL AFTER TipoArchivoEntrega');
CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'IdUsuarioRevisa', 'COLUMNA',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD COLUMN IdUsuarioRevisa INT NULL AFTER NombreArchivoEntrega');
CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'FechaRevision', 'COLUMNA',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD COLUMN FechaRevision DATETIME NULL AFTER IdUsuarioRevisa');

CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'UX_TareaAsignacion_ArchivoEntrega', 'INDICE',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD UNIQUE KEY UX_TareaAsignacion_ArchivoEntrega (ArchivoEntrega)');
CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'IX_TareaAsignacion_UsuarioRevisa', 'INDICE',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD KEY IX_TareaAsignacion_UsuarioRevisa (IdUsuarioRevisa)');
CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'FK_TareaAsignacion_UsuarioRevisa', 'RESTRICCION',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD CONSTRAINT FK_TareaAsignacion_UsuarioRevisa FOREIGN KEY (IdUsuarioRevisa) REFERENCES TB_USUARIO (IdUsuario) ON DELETE RESTRICT ON UPDATE RESTRICT');
CALL SP_Migracion_AgregarSiFalta('TB_TAREA_ASIGNACION', 'CK_TareaAsignacion_ArchivoCompleto', 'RESTRICCION',
  'ALTER TABLE TB_TAREA_ASIGNACION ADD CONSTRAINT CK_TareaAsignacion_ArchivoCompleto CHECK ((ArchivoEntrega IS NULL AND TipoArchivoEntrega IS NULL) OR (ArchivoEntrega IS NOT NULL AND TipoArchivoEntrega IS NOT NULL))');

DROP PROCEDURE IF EXISTS SP_Migracion_AgregarSiFalta;

-- Estado nuevo: la profesional devuelve la tarea para que la corrijan
INSERT INTO TB_ESTADO_TAREA (IdEstadoTarea, Nombre) VALUES
  (5, 'Por corregir')
ON DUPLICATE KEY UPDATE IdEstadoTarea = IdEstadoTarea;

-- Revision: deberian salir las cinco columnas nuevas y el estado 5
SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'TB_TAREA_ASIGNACION'
ORDER BY ORDINAL_POSITION;

SELECT * FROM TB_ESTADO_TAREA ORDER BY IdEstadoTarea;
