-- ============================================================
-- Mente Activa CRM - 04 Pruebas de integridad
-- Correr despues de 01, 02 y 03. Es OPCIONAL: no crea ni cambia
-- nada permanente.
--
-- Cada prueba intenta a proposito una operacion que la base debe
-- rechazar, y muestra el error con el que la rechaza. Si una prueba
-- deja de dar su error esperado, algo se rompio en el esquema.
--
-- Los borrados van dentro de transacciones que se revierten, asi que
-- al terminar los datos quedan igual que antes.
--
-- Ejecutar asi, para que no se detenga en el primer error esperado:
--     mysql -u root -p --force < 04_pruebas_integridad.sql
-- En Workbench: ejecutar el script completo; los errores en rojo de
-- las pruebas 01 a 05 y 11 a 15 son el resultado correcto.
-- ============================================================

SET NAMES utf8mb4;
USE mente_activa;

SELECT '01 Llave foranea: cita de un estudiante que no existe. Esperado 1452' AS prueba;
INSERT INTO TB_CITA (IdEstudiante, IdTipoSesion, IdModalidad, IdEstadoCita, IdUsuarioRegistro, FechaHoraInicio, FechaHoraFin)
VALUES (999, 1, 1, 1, 1, '2026-11-02 09:00:00', '2026-11-02 10:00:00');

SELECT '02 Llave foranea: borrar un estado de cita en uso. Esperado 1451' AS prueba;
DELETE FROM TB_ESTADO_CITA WHERE IdEstadoCita = 1;

SELECT '03 Principal unico: segundo encargado principal del mismo estudiante. Esperado 1062' AS prueba;
INSERT INTO TB_ESTUDIANTE_ENCARGADO (IdEstudiante, IdEncargado, IdParentesco, EsPrincipal) VALUES (1, 3, 5, 1);

SELECT '04 Principal unico: segundo telefono principal del mismo encargado. Esperado 1062' AS prueba;
INSERT INTO TB_TELEFONO (IdEncargado, IdTipoTelefono, Numero, EsPrincipal) VALUES (1, 3, '88880009', 1);

SELECT '05 Identificacion repetida. Esperado 1062' AS prueba;
INSERT INTO TB_ENCARGADO (IdEstadoCliente, Nombre, PrimerApellido, Identificacion) VALUES (1, 'Prueba', 'Prueba', '100000001');

SELECT '06 Varios encargados sin identificacion si se aceptan. Esperado 2' AS prueba;
SELECT COUNT(*) AS sin_identificacion FROM TB_ENCARGADO WHERE Identificacion IS NULL;

SELECT '07 Cambiar de encargado principal quitando primero el anterior. Esperado que funcione' AS prueba;
START TRANSACTION;
UPDATE TB_ESTUDIANTE_ENCARGADO SET EsPrincipal = 0 WHERE IdEstudianteEncargado = 1;
UPDATE TB_ESTUDIANTE_ENCARGADO SET EsPrincipal = 1 WHERE IdEstudianteEncargado = 2;
SELECT IdEstudianteEncargado, IdEstudiante, IdEncargado, EsPrincipal, EstudiantePrincipal
  FROM TB_ESTUDIANTE_ENCARGADO WHERE IdEstudiante = 1;
ROLLBACK;

SELECT '08 Cascada: al borrar un plan se van sus estrategias y actividades. Esperado 0 y 0' AS prueba;
START TRANSACTION;
DELETE FROM TB_PLAN_INTERVENCION WHERE IdPlan = 1;
SELECT (SELECT COUNT(*) FROM TB_ESTRATEGIA) AS estrategias, (SELECT COUNT(*) FROM TB_ACTIVIDAD) AS actividades;
ROLLBACK;

SELECT '09 Cascada: al borrar una cita se van sus notificaciones. Esperado 0' AS prueba;
START TRANSACTION;
DELETE FROM TB_CITA WHERE IdCita = 1;
SELECT COUNT(*) AS notificaciones FROM TB_NOTIFICACION WHERE IdCita = 1;
ROLLBACK;

SELECT '10 Restriccion: no se puede borrar un estudiante con dependencias. Esperado 1451' AS prueba;
DELETE FROM TB_ESTUDIANTE WHERE IdEstudiante = 1;

SELECT '11 Regla de negocio: citas vigentes que se traslapan. Esperado solo el par 1-2' AS prueba;
SELECT a.IdCita AS cita_a, b.IdCita AS cita_b, a.FechaHoraInicio AS inicio_a, b.FechaHoraInicio AS inicio_b
  FROM TB_CITA a
  JOIN TB_CITA b ON a.IdCita < b.IdCita
   AND a.FechaHoraInicio < b.FechaHoraFin
   AND b.FechaHoraInicio < a.FechaHoraFin
 WHERE a.IdEstadoCita NOT IN (4, 5) AND b.IdEstadoCita NOT IN (4, 5);

SELECT '12 CHECK: cita que termina antes de empezar. Esperado 3819' AS prueba;
INSERT INTO TB_CITA (IdEstudiante, IdTipoSesion, IdModalidad, IdEstadoCita, IdUsuarioRegistro, FechaHoraInicio, FechaHoraFin)
VALUES (1, 1, 1, 1, 1, '2026-11-02 10:00:00', '2026-11-02 09:00:00');

SELECT '13 CHECK: telefono de Costa Rica con 7 digitos. Esperado 3819' AS prueba;
INSERT INTO TB_TELEFONO (IdEncargado, IdTipoTelefono, Numero) VALUES (3, 1, '8888000');

SELECT '14 CHECK: mensaje enviado a uno mismo. Esperado 3819' AS prueba;
INSERT INTO TB_MENSAJE (IdUsuarioRemitente, IdUsuarioDestinatario, Asunto, Cuerpo) VALUES (1, 1, 'Prueba', 'Prueba');

SELECT '15 CHECK: abono en cero. Esperado 3819' AS prueba;
INSERT INTO TB_ABONO (IdPago, IdEncargadoPagador, IdMetodoPago, IdUsuarioRegistro, Monto, FechaAbono)
VALUES (3, 1, 1, 1, 0, '2026-10-02');

SELECT '16 JSON invalido en la bitacora. Esperado 3140' AS prueba;
INSERT INTO TB_BITACORA (IdUsuario, Entidad, IdRegistro, Accion, ValorNuevo) VALUES (1, 'TB_CITA', 1, 'Editar', '{roto');

SELECT '17 Estado de cuenta: lo abonado contra lo cobrado' AS prueba;
SELECT p.IdPago, p.Concepto, p.Monto, COALESCE(SUM(a.Monto), 0) AS abonado, ep.Nombre AS estado
  FROM TB_PAGO p
  LEFT JOIN TB_ABONO a ON a.IdPago = p.IdPago AND a.Activo = 1
  JOIN TB_ESTADO_PAGO ep ON ep.IdEstadoPago = p.IdEstadoPago
 GROUP BY p.IdPago, p.Concepto, p.Monto, ep.Nombre
 ORDER BY p.IdPago;

SELECT '18 Los datos quedaron como estaban' AS prueba;
SELECT (SELECT COUNT(*) FROM TB_ENCARGADO) AS encargados, (SELECT COUNT(*) FROM TB_CITA) AS citas,
       (SELECT COUNT(*) FROM TB_ESTRATEGIA) AS estrategias, (SELECT COUNT(*) FROM TB_ABONO) AS abonos;
