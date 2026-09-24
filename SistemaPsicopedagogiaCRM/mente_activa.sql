-- MySQL dump 10.13  Distrib 8.0.46, for Win64 (x86_64)
--
-- Host: localhost    Database: mente_activa
-- ------------------------------------------------------
-- Server version	8.4.11

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `tb_abono`
--

DROP TABLE IF EXISTS `tb_abono`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_abono` (
  `IdAbono` int NOT NULL AUTO_INCREMENT,
  `IdPago` int NOT NULL,
  `IdEncargadoPagador` int NOT NULL,
  `IdMetodoPago` int NOT NULL,
  `IdUsuarioRegistro` int NOT NULL,
  `Monto` decimal(10,2) NOT NULL,
  `FechaAbono` date NOT NULL,
  `Referencia` varchar(100) DEFAULT NULL,
  `Observaciones` varchar(500) DEFAULT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdAbono`),
  KEY `IX_Abono_Pago` (`IdPago`),
  KEY `IX_Abono_EncargadoPagador` (`IdEncargadoPagador`),
  KEY `IX_Abono_MetodoPago` (`IdMetodoPago`),
  KEY `IX_Abono_UsuarioRegistro` (`IdUsuarioRegistro`),
  CONSTRAINT `FK_Abono_EncargadoPagador` FOREIGN KEY (`IdEncargadoPagador`) REFERENCES `tb_encargado` (`IdEncargado`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Abono_MetodoPago` FOREIGN KEY (`IdMetodoPago`) REFERENCES `tb_metodo_pago` (`IdMetodoPago`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Abono_Pago` FOREIGN KEY (`IdPago`) REFERENCES `tb_pago` (`IdPago`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Abono_UsuarioRegistro` FOREIGN KEY (`IdUsuarioRegistro`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Abono_MontoPositivo` CHECK ((`Monto` > 0))
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_abono`
--

LOCK TABLES `tb_abono` WRITE;
/*!40000 ALTER TABLE `tb_abono` DISABLE KEYS */;
INSERT INTO `tb_abono` VALUES (1,1,1,2,1,30000.00,'2026-09-03','SINPE-0001',NULL,1,'2026-09-15 16:45:37'),(2,1,2,1,1,30000.00,'2026-09-05',NULL,NULL,1,'2026-09-15 16:45:37'),(3,2,2,3,1,15000.00,'2026-09-06','TRF-0001',NULL,1,'2026-09-15 16:45:37');
/*!40000 ALTER TABLE `tb_abono` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_actividad`
--

DROP TABLE IF EXISTS `tb_actividad`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_actividad` (
  `IdActividad` int NOT NULL AUTO_INCREMENT,
  `IdEstrategia` int NOT NULL,
  `Nombre` varchar(150) NOT NULL,
  `Descripcion` varchar(500) DEFAULT NULL,
  `Completada` tinyint(1) NOT NULL DEFAULT '0',
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdActividad`),
  KEY `IX_Actividad_Estrategia` (`IdEstrategia`),
  CONSTRAINT `FK_Actividad_Estrategia` FOREIGN KEY (`IdEstrategia`) REFERENCES `tb_estrategia` (`IdEstrategia`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `CK_Actividad_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_actividad`
--

LOCK TABLES `tb_actividad` WRITE;
/*!40000 ALTER TABLE `tb_actividad` DISABLE KEYS */;
INSERT INTO `tb_actividad` VALUES (1,1,'Leer un cuento y responder cinco preguntas',NULL,1,'2026-09-15 16:45:37'),(2,1,'Resumir el cuento en tres oraciones',NULL,0,'2026-09-15 16:45:37'),(3,2,'Completar un mapa de personajes',NULL,0,'2026-09-15 16:45:37');
/*!40000 ALTER TABLE `tb_actividad` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_area_dificultad`
--

DROP TABLE IF EXISTS `tb_area_dificultad`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_area_dificultad` (
  `IdAreaDificultad` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(100) NOT NULL,
  `Descripcion` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`IdAreaDificultad`),
  UNIQUE KEY `UX_AreaDificultad_Nombre` (`Nombre`),
  CONSTRAINT `CK_AreaDificultad_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_area_dificultad`
--

LOCK TABLES `tb_area_dificultad` WRITE;
/*!40000 ALTER TABLE `tb_area_dificultad` DISABLE KEYS */;
INSERT INTO `tb_area_dificultad` VALUES (1,'Lectoescritura',NULL),(2,'Cálculo',NULL),(3,'Atención',NULL),(4,'Comprensión lectora',NULL),(5,'Memoria',NULL),(6,'Funciones ejecutivas',NULL),(7,'Lenguaje oral',NULL),(8,'Motricidad fina',NULL),(9,'Hábitos de estudio',NULL);
/*!40000 ALTER TABLE `tb_area_dificultad` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_bitacora`
--

DROP TABLE IF EXISTS `tb_bitacora`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_bitacora` (
  `IdBitacora` bigint NOT NULL AUTO_INCREMENT,
  `IdUsuario` int NOT NULL,
  `Entidad` varchar(100) NOT NULL,
  `IdRegistro` int NOT NULL,
  `Accion` varchar(20) NOT NULL,
  `ValorAnterior` json DEFAULT NULL,
  `ValorNuevo` json DEFAULT NULL,
  `FechaHora` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdBitacora`),
  KEY `IX_Bitacora_FechaHora` (`FechaHora`),
  KEY `IX_Bitacora_EntidadRegistro` (`Entidad`,`IdRegistro`),
  KEY `IX_Bitacora_Usuario` (`IdUsuario`),
  CONSTRAINT `FK_Bitacora_Usuario` FOREIGN KEY (`IdUsuario`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Bitacora_Accion` CHECK ((`Accion` in (_utf8mb4'Crear',_utf8mb4'Editar',_utf8mb4'Eliminar',_utf8mb4'CambiarEstado'))),
  CONSTRAINT `CK_Bitacora_EntidadNoVacio` CHECK ((trim(`Entidad`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_bitacora`
--

LOCK TABLES `tb_bitacora` WRITE;
/*!40000 ALTER TABLE `tb_bitacora` DISABLE KEYS */;
INSERT INTO `tb_bitacora` VALUES (1,1,'TB_ENCARGADO',1,'Crear',NULL,'{\"Nombre\": \"Andrea\", \"PrimerApellido\": \"Solís\", \"IdEstadoCliente\": 1}','2026-08-20 10:00:00'),(2,1,'TB_ENCARGADO',1,'CambiarEstado','{\"IdEstadoCliente\": 1}','{\"IdEstadoCliente\": 4}','2026-08-25 11:00:00'),(3,1,'TB_CITA',4,'CambiarEstado','{\"IdEstadoCita\": 1}','{\"IdEstadoCita\": 4, \"MotivoCancelacion\": \"La familia pidió reprogramar.\"}','2026-09-12 08:30:00');
/*!40000 ALTER TABLE `tb_bitacora` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_canal_notificacion`
--

DROP TABLE IF EXISTS `tb_canal_notificacion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_canal_notificacion` (
  `IdCanalNotificacion` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(30) NOT NULL,
  PRIMARY KEY (`IdCanalNotificacion`),
  UNIQUE KEY `UX_CanalNotificacion_Nombre` (`Nombre`),
  CONSTRAINT `CK_CanalNotificacion_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_canal_notificacion`
--

LOCK TABLES `tb_canal_notificacion` WRITE;
/*!40000 ALTER TABLE `tb_canal_notificacion` DISABLE KEYS */;
INSERT INTO `tb_canal_notificacion` VALUES (1,'Correo'),(3,'Portal'),(2,'WhatsApp');
/*!40000 ALTER TABLE `tb_canal_notificacion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_cita`
--

DROP TABLE IF EXISTS `tb_cita`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_cita` (
  `IdCita` int NOT NULL AUTO_INCREMENT,
  `IdEstudiante` int NOT NULL,
  `IdTipoSesion` int NOT NULL,
  `IdModalidad` int NOT NULL,
  `IdEstadoCita` int NOT NULL,
  `IdUsuarioRegistro` int NOT NULL,
  `FechaHoraInicio` datetime NOT NULL,
  `FechaHoraFin` datetime NOT NULL,
  `Observaciones` varchar(500) DEFAULT NULL,
  `MotivoCancelacion` varchar(500) DEFAULT NULL,
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdCita`),
  KEY `IX_Cita_FechaHoraInicio` (`FechaHoraInicio`),
  KEY `IX_Cita_Estudiante` (`IdEstudiante`),
  KEY `IX_Cita_TipoSesion` (`IdTipoSesion`),
  KEY `IX_Cita_Modalidad` (`IdModalidad`),
  KEY `IX_Cita_EstadoCita` (`IdEstadoCita`),
  KEY `IX_Cita_UsuarioRegistro` (`IdUsuarioRegistro`),
  CONSTRAINT `FK_Cita_EstadoCita` FOREIGN KEY (`IdEstadoCita`) REFERENCES `tb_estado_cita` (`IdEstadoCita`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Cita_Estudiante` FOREIGN KEY (`IdEstudiante`) REFERENCES `tb_estudiante` (`IdEstudiante`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Cita_Modalidad` FOREIGN KEY (`IdModalidad`) REFERENCES `tb_modalidad` (`IdModalidad`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Cita_TipoSesion` FOREIGN KEY (`IdTipoSesion`) REFERENCES `tb_tipo_sesion` (`IdTipoSesion`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Cita_UsuarioRegistro` FOREIGN KEY (`IdUsuarioRegistro`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Cita_RangoFechas` CHECK ((`FechaHoraFin` > `FechaHoraInicio`))
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_cita`
--

LOCK TABLES `tb_cita` WRITE;
/*!40000 ALTER TABLE `tb_cita` DISABLE KEYS */;
INSERT INTO `tb_cita` VALUES (1,1,1,1,1,1,'2026-10-05 09:00:00','2026-10-05 10:00:00',NULL,NULL,'2026-09-15 16:45:36',NULL),(2,2,1,1,2,1,'2026-10-05 09:30:00','2026-10-05 10:30:00',NULL,NULL,'2026-09-15 16:45:36',NULL),(3,1,2,1,3,1,'2026-09-07 15:00:00','2026-09-07 16:00:00',NULL,NULL,'2026-09-15 16:45:36',NULL),(4,2,1,2,4,1,'2026-10-05 09:45:00','2026-10-05 10:15:00',NULL,'La familia pidió reprogramar.','2026-09-15 16:45:36',NULL),(5,1,1,1,5,1,'2026-09-10 14:00:00','2026-09-10 15:00:00',NULL,NULL,'2026-09-15 16:45:36',NULL);
/*!40000 ALTER TABLE `tb_cita` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_configuracion`
--

DROP TABLE IF EXISTS `tb_configuracion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_configuracion` (
  `IdConfiguracion` int NOT NULL AUTO_INCREMENT,
  `Clave` varchar(100) NOT NULL,
  `Valor` varchar(1000) NOT NULL,
  `Descripcion` varchar(255) DEFAULT NULL,
  `FechaModificacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdConfiguracion`),
  UNIQUE KEY `UX_Configuracion_Clave` (`Clave`),
  CONSTRAINT `CK_Configuracion_ClaveNoVacio` CHECK ((trim(`Clave`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_configuracion`
--

LOCK TABLES `tb_configuracion` WRITE;
/*!40000 ALTER TABLE `tb_configuracion` DISABLE KEYS */;
INSERT INTO `tb_configuracion` VALUES (1,'NombreConsultorio','Mente Activa Psicopedagogía','Nombre que se muestra en el sitio y en los correos','2026-09-15 16:45:24'),(2,'CorreoContacto','contacto@example.com','Correo de contacto del consultorio','2026-09-15 16:45:24'),(3,'TelefonoContacto','00000000','Teléfono de contacto, solo dígitos','2026-09-15 16:45:24'),(4,'Moneda','CRC','Código ISO de la moneda de los montos','2026-09-15 16:45:24'),(5,'HorasRecordatorioCita','24','Horas de anticipación para el recordatorio de cita','2026-09-15 16:45:24');
/*!40000 ALTER TABLE `tb_configuracion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_dia_no_laboral`
--

DROP TABLE IF EXISTS `tb_dia_no_laboral`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_dia_no_laboral` (
  `IdDiaNoLaboral` int NOT NULL AUTO_INCREMENT,
  `Fecha` date NOT NULL,
  `Motivo` varchar(150) NOT NULL,
  `EsFeriado` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`IdDiaNoLaboral`),
  UNIQUE KEY `UX_DiaNoLaboral_Fecha` (`Fecha`),
  CONSTRAINT `CK_DiaNoLaboral_MotivoNoVacio` CHECK ((trim(`Motivo`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_dia_no_laboral`
--

LOCK TABLES `tb_dia_no_laboral` WRITE;
/*!40000 ALTER TABLE `tb_dia_no_laboral` DISABLE KEYS */;
INSERT INTO `tb_dia_no_laboral` VALUES (1,'2026-12-25','Navidad',1);
/*!40000 ALTER TABLE `tb_dia_no_laboral` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_encargado`
--

DROP TABLE IF EXISTS `tb_encargado`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_encargado` (
  `IdEncargado` int NOT NULL AUTO_INCREMENT,
  `IdEstadoCliente` int NOT NULL,
  `IdServicioInteres` int DEFAULT NULL,
  `Nombre` varchar(100) NOT NULL,
  `PrimerApellido` varchar(100) NOT NULL,
  `SegundoApellido` varchar(100) DEFAULT NULL,
  `Identificacion` varchar(20) DEFAULT NULL,
  `Correo` varchar(150) DEFAULT NULL,
  `Observaciones` varchar(1000) DEFAULT NULL,
  `FechaRegistro` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`IdEncargado`),
  UNIQUE KEY `UX_Encargado_Identificacion` (`Identificacion`),
  KEY `IX_Encargado_EstadoCliente` (`IdEstadoCliente`),
  KEY `IX_Encargado_ServicioInteres` (`IdServicioInteres`),
  CONSTRAINT `FK_Encargado_EstadoCliente` FOREIGN KEY (`IdEstadoCliente`) REFERENCES `tb_estado_cliente` (`IdEstadoCliente`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Encargado_ServicioInteres` FOREIGN KEY (`IdServicioInteres`) REFERENCES `tb_servicio` (`IdServicio`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Encargado_IdentificacionNoVacio` CHECK (((`Identificacion` is null) or (trim(`Identificacion`) <> _utf8mb4''))),
  CONSTRAINT `CK_Encargado_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4'')),
  CONSTRAINT `CK_Encargado_PrimerApellidoNoVacio` CHECK ((trim(`PrimerApellido`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_encargado`
--

LOCK TABLES `tb_encargado` WRITE;
/*!40000 ALTER TABLE `tb_encargado` DISABLE KEYS */;
INSERT INTO `tb_encargado` VALUES (1,4,2,'Andrea','Solís','Mora','100000001','andrea.prueba@example.com',NULL,'2026-09-15 16:45:35',NULL,1),(2,4,1,'Marco','Vindas','Rojas','100000002','marco.prueba@example.com',NULL,'2026-09-15 16:45:35',NULL,1),(3,1,3,'Paula','Jiménez','Arce',NULL,'paula.prueba@example.com',NULL,'2026-09-15 16:45:35',NULL,1),(4,6,4,'Diego','Campos',NULL,NULL,NULL,NULL,'2026-09-15 16:45:35','2026-09-24 10:18:36',0),(5,1,1,'Maria','Rodriguez',NULL,NULL,'maria@correo.com','Primera consulta','2026-09-23 19:16:41',NULL,1),(6,1,1,'Maria','Arroyo',NULL,NULL,'maria@test.com','Prueba','2026-09-24 08:40:01',NULL,1),(7,1,2,'Maria','Jose',NULL,NULL,'mjarroyo0339@ufide.ac.cr','Necesita psicologo\r\n','2026-09-24 09:09:41',NULL,1),(8,1,4,'Maria','Jose',NULL,NULL,'mjarroyo0339@ufide.ac.cr',NULL,'2026-09-24 09:12:06',NULL,1),(9,1,3,'prueba','aa',NULL,NULL,'aa@gmail.com','aaaaa','2026-09-24 09:15:02','2026-09-24 10:31:00',0);
/*!40000 ALTER TABLE `tb_encargado` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estado_cita`
--

DROP TABLE IF EXISTS `tb_estado_cita`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estado_cita` (
  `IdEstadoCita` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  `ColorHex` varchar(7) DEFAULT NULL,
  PRIMARY KEY (`IdEstadoCita`),
  UNIQUE KEY `UX_EstadoCita_Nombre` (`Nombre`),
  CONSTRAINT `CK_EstadoCita_ColorHex` CHECK (((`ColorHex` is null) or regexp_like(`ColorHex`,_utf8mb4'^#[0-9A-Fa-f]{6}$'))),
  CONSTRAINT `CK_EstadoCita_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estado_cita`
--

LOCK TABLES `tb_estado_cita` WRITE;
/*!40000 ALTER TABLE `tb_estado_cita` DISABLE KEYS */;
INSERT INTO `tb_estado_cita` VALUES (1,'Programada','#4F6FAE'),(2,'Confirmada','#7BC4A4'),(3,'Completada','#3E9B6E'),(4,'Cancelada','#D9534F'),(5,'No asistió','#E0A63C');
/*!40000 ALTER TABLE `tb_estado_cita` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estado_cliente`
--

DROP TABLE IF EXISTS `tb_estado_cliente`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estado_cliente` (
  `IdEstadoCliente` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  `Orden` int DEFAULT NULL,
  `ClaseCss` varchar(50) DEFAULT NULL,
  PRIMARY KEY (`IdEstadoCliente`),
  UNIQUE KEY `UX_EstadoCliente_Nombre` (`Nombre`),
  CONSTRAINT `CK_EstadoCliente_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4'')),
  CONSTRAINT `CK_EstadoCliente_Orden` CHECK (((`Orden` is null) or (`Orden` > 0)))
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estado_cliente`
--

LOCK TABLES `tb_estado_cliente` WRITE;
/*!40000 ALTER TABLE `tb_estado_cliente` DISABLE KEYS */;
INSERT INTO `tb_estado_cliente` VALUES (1,'Nuevo',1,'estado-cliente-nuevo'),(2,'Contactado',2,'estado-cliente-contactado'),(3,'Cita agendada',4,'estado-cliente-cita-agendada'),(4,'Cliente activo',5,'estado-cliente-activo'),(5,'Inactivo',6,'estado-cliente-inactivo'),(6,'En seguimiento',3,'estado-cliente-seguimiento');
/*!40000 ALTER TABLE `tb_estado_cliente` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estado_pago`
--

DROP TABLE IF EXISTS `tb_estado_pago`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estado_pago` (
  `IdEstadoPago` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`IdEstadoPago`),
  UNIQUE KEY `UX_EstadoPago_Nombre` (`Nombre`),
  CONSTRAINT `CK_EstadoPago_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estado_pago`
--

LOCK TABLES `tb_estado_pago` WRITE;
/*!40000 ALTER TABLE `tb_estado_pago` DISABLE KEYS */;
INSERT INTO `tb_estado_pago` VALUES (5,'Anulado'),(1,'Pagado'),(4,'Parcial'),(2,'Pendiente'),(3,'Vencido');
/*!40000 ALTER TABLE `tb_estado_pago` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estado_plan`
--

DROP TABLE IF EXISTS `tb_estado_plan`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estado_plan` (
  `IdEstadoPlan` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`IdEstadoPlan`),
  UNIQUE KEY `UX_EstadoPlan_Nombre` (`Nombre`),
  CONSTRAINT `CK_EstadoPlan_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estado_plan`
--

LOCK TABLES `tb_estado_plan` WRITE;
/*!40000 ALTER TABLE `tb_estado_plan` DISABLE KEYS */;
INSERT INTO `tb_estado_plan` VALUES (2,'Activo'),(1,'Borrador'),(3,'Finalizado'),(4,'Suspendido');
/*!40000 ALTER TABLE `tb_estado_plan` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estado_tarea`
--

DROP TABLE IF EXISTS `tb_estado_tarea`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estado_tarea` (
  `IdEstadoTarea` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`IdEstadoTarea`),
  UNIQUE KEY `UX_EstadoTarea_Nombre` (`Nombre`),
  CONSTRAINT `CK_EstadoTarea_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estado_tarea`
--

LOCK TABLES `tb_estado_tarea` WRITE;
/*!40000 ALTER TABLE `tb_estado_tarea` DISABLE KEYS */;
INSERT INTO `tb_estado_tarea` VALUES (1,'Asignada'),(2,'Entregada'),(3,'Revisada'),(4,'Vencida');
/*!40000 ALTER TABLE `tb_estado_tarea` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estado_usuario`
--

DROP TABLE IF EXISTS `tb_estado_usuario`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estado_usuario` (
  `IdEstadoUsuario` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`IdEstadoUsuario`),
  UNIQUE KEY `UX_EstadoUsuario_Nombre` (`Nombre`),
  CONSTRAINT `CK_EstadoUsuario_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estado_usuario`
--

LOCK TABLES `tb_estado_usuario` WRITE;
/*!40000 ALTER TABLE `tb_estado_usuario` DISABLE KEYS */;
INSERT INTO `tb_estado_usuario` VALUES (1,'Activo'),(3,'Bloqueado'),(2,'Inactivo'),(4,'Pendiente de activación');
/*!40000 ALTER TABLE `tb_estado_usuario` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estrategia`
--

DROP TABLE IF EXISTS `tb_estrategia`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estrategia` (
  `IdEstrategia` int NOT NULL AUTO_INCREMENT,
  `IdPlan` int NOT NULL,
  `Descripcion` varchar(500) NOT NULL,
  `Orden` int DEFAULT NULL,
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdEstrategia`),
  KEY `IX_Estrategia_Plan` (`IdPlan`),
  CONSTRAINT `FK_Estrategia_PlanIntervencion` FOREIGN KEY (`IdPlan`) REFERENCES `tb_plan_intervencion` (`IdPlan`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `CK_Estrategia_DescripcionNoVacio` CHECK ((trim(`Descripcion`) <> _utf8mb4'')),
  CONSTRAINT `CK_Estrategia_Orden` CHECK (((`Orden` is null) or (`Orden` > 0)))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estrategia`
--

LOCK TABLES `tb_estrategia` WRITE;
/*!40000 ALTER TABLE `tb_estrategia` DISABLE KEYS */;
INSERT INTO `tb_estrategia` VALUES (1,1,'Lectura guiada con preguntas',1,'2026-09-15 16:45:37'),(2,1,'Organizadores gráficos',2,'2026-09-15 16:45:37');
/*!40000 ALTER TABLE `tb_estrategia` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estudiante`
--

DROP TABLE IF EXISTS `tb_estudiante`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estudiante` (
  `IdEstudiante` int NOT NULL AUTO_INCREMENT,
  `IdNivelEducativo` int DEFAULT NULL,
  `IdInstitucion` int DEFAULT NULL,
  `Nombre` varchar(100) NOT NULL,
  `PrimerApellido` varchar(100) NOT NULL,
  `SegundoApellido` varchar(100) DEFAULT NULL,
  `FechaNacimiento` date DEFAULT NULL,
  `Observaciones` varchar(1000) DEFAULT NULL,
  `NecesidadesApoyo` varchar(1000) DEFAULT NULL,
  `FechaRegistro` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`IdEstudiante`),
  KEY `IX_Estudiante_NivelEducativo` (`IdNivelEducativo`),
  KEY `IX_Estudiante_Institucion` (`IdInstitucion`),
  CONSTRAINT `FK_Estudiante_Institucion` FOREIGN KEY (`IdInstitucion`) REFERENCES `tb_institucion` (`IdInstitucion`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Estudiante_NivelEducativo` FOREIGN KEY (`IdNivelEducativo`) REFERENCES `tb_nivel_educativo` (`IdNivelEducativo`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Estudiante_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4'')),
  CONSTRAINT `CK_Estudiante_PrimerApellidoNoVacio` CHECK ((trim(`PrimerApellido`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estudiante`
--

LOCK TABLES `tb_estudiante` WRITE;
/*!40000 ALTER TABLE `tb_estudiante` DISABLE KEYS */;
INSERT INTO `tb_estudiante` VALUES (1,3,1,'Lucía','Vindas','Solís','2016-04-12',NULL,'Apoyo en lectura comprensiva.','2026-09-15 16:45:35',NULL,1),(2,2,2,'Tomás','Vindas','Campos','2018-09-03',NULL,'Refuerzo en operaciones básicas.','2026-09-15 16:45:35',NULL,1),(3,NULL,NULL,'Juan','Rodriguez',NULL,NULL,NULL,NULL,'2026-09-23 19:16:41',NULL,1),(4,NULL,NULL,'Juan','Arroyo',NULL,NULL,NULL,NULL,'2026-09-24 08:40:01',NULL,1),(5,NULL,NULL,'Lafuente','Romero',NULL,NULL,NULL,NULL,'2026-09-24 09:09:41',NULL,1),(6,NULL,NULL,'llll','Romero',NULL,NULL,NULL,NULL,'2026-09-24 09:12:06',NULL,1),(7,NULL,NULL,'Maria','Jose',NULL,NULL,NULL,NULL,'2026-09-24 09:15:02',NULL,1);
/*!40000 ALTER TABLE `tb_estudiante` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estudiante_area`
--

DROP TABLE IF EXISTS `tb_estudiante_area`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estudiante_area` (
  `IdEstudianteArea` int NOT NULL AUTO_INCREMENT,
  `IdEstudiante` int NOT NULL,
  `IdAreaDificultad` int NOT NULL,
  `Observacion` varchar(500) DEFAULT NULL,
  `FechaRegistro` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdEstudianteArea`),
  UNIQUE KEY `UX_EstudianteArea_Relacion` (`IdEstudiante`,`IdAreaDificultad`),
  KEY `IX_EstudianteArea_AreaDificultad` (`IdAreaDificultad`),
  CONSTRAINT `FK_EstudianteArea_AreaDificultad` FOREIGN KEY (`IdAreaDificultad`) REFERENCES `tb_area_dificultad` (`IdAreaDificultad`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_EstudianteArea_Estudiante` FOREIGN KEY (`IdEstudiante`) REFERENCES `tb_estudiante` (`IdEstudiante`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estudiante_area`
--

LOCK TABLES `tb_estudiante_area` WRITE;
/*!40000 ALTER TABLE `tb_estudiante_area` DISABLE KEYS */;
INSERT INTO `tb_estudiante_area` VALUES (1,1,1,'Confunde letras similares.','2026-09-15 16:45:36'),(2,1,4,NULL,'2026-09-15 16:45:36'),(3,2,2,'Dificultad con la resta llevando.','2026-09-15 16:45:36');
/*!40000 ALTER TABLE `tb_estudiante_area` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_estudiante_encargado`
--

DROP TABLE IF EXISTS `tb_estudiante_encargado`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_estudiante_encargado` (
  `IdEstudianteEncargado` int NOT NULL AUTO_INCREMENT,
  `IdEstudiante` int NOT NULL,
  `IdEncargado` int NOT NULL,
  `IdParentesco` int NOT NULL,
  `EsPrincipal` tinyint(1) NOT NULL DEFAULT '0',
  `EstudiantePrincipal` int GENERATED ALWAYS AS (if((`EsPrincipal` = 1),`IdEstudiante`,NULL)) STORED,
  PRIMARY KEY (`IdEstudianteEncargado`),
  UNIQUE KEY `UX_EstudianteEncargado_Relacion` (`IdEstudiante`,`IdEncargado`),
  UNIQUE KEY `UX_EstudianteEncargado_Principal` (`EstudiantePrincipal`),
  KEY `IX_EstudianteEncargado_Encargado` (`IdEncargado`),
  KEY `IX_EstudianteEncargado_Parentesco` (`IdParentesco`),
  CONSTRAINT `FK_EstudianteEncargado_Encargado` FOREIGN KEY (`IdEncargado`) REFERENCES `tb_encargado` (`IdEncargado`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `FK_EstudianteEncargado_Estudiante` FOREIGN KEY (`IdEstudiante`) REFERENCES `tb_estudiante` (`IdEstudiante`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_EstudianteEncargado_Parentesco` FOREIGN KEY (`IdParentesco`) REFERENCES `tb_parentesco` (`IdParentesco`) ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_estudiante_encargado`
--

LOCK TABLES `tb_estudiante_encargado` WRITE;
/*!40000 ALTER TABLE `tb_estudiante_encargado` DISABLE KEYS */;
INSERT INTO `tb_estudiante_encargado` (`IdEstudianteEncargado`, `IdEstudiante`, `IdEncargado`, `IdParentesco`, `EsPrincipal`) VALUES (1,1,1,1,1),(2,1,2,2,0),(3,2,2,2,1),(4,3,5,3,1),(5,4,6,3,1),(6,5,7,3,1),(7,6,8,3,1),(8,7,9,3,1);
/*!40000 ALTER TABLE `tb_estudiante_encargado` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_horario_atencion`
--

DROP TABLE IF EXISTS `tb_horario_atencion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_horario_atencion` (
  `IdHorarioAtencion` int NOT NULL AUTO_INCREMENT,
  `DiaSemana` tinyint unsigned NOT NULL,
  `HoraInicio` time NOT NULL,
  `HoraFin` time NOT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`IdHorarioAtencion`),
  CONSTRAINT `CK_HorarioAtencion_DiaSemana` CHECK ((`DiaSemana` between 1 and 7)),
  CONSTRAINT `CK_HorarioAtencion_Rango` CHECK ((`HoraFin` > `HoraInicio`))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_horario_atencion`
--

LOCK TABLES `tb_horario_atencion` WRITE;
/*!40000 ALTER TABLE `tb_horario_atencion` DISABLE KEYS */;
INSERT INTO `tb_horario_atencion` VALUES (1,1,'08:00:00','17:00:00',1),(2,2,'08:00:00','17:00:00',1),(3,3,'08:00:00','17:00:00',1),(4,4,'08:00:00','17:00:00',1),(5,5,'08:00:00','17:00:00',1);
/*!40000 ALTER TABLE `tb_horario_atencion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_institucion`
--

DROP TABLE IF EXISTS `tb_institucion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_institucion` (
  `IdInstitucion` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(150) NOT NULL,
  `Canton` varchar(100) DEFAULT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`IdInstitucion`),
  UNIQUE KEY `UX_Institucion_NombreCanton` (`Nombre`,`Canton`),
  CONSTRAINT `CK_Institucion_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_institucion`
--

LOCK TABLES `tb_institucion` WRITE;
/*!40000 ALTER TABLE `tb_institucion` DISABLE KEYS */;
INSERT INTO `tb_institucion` VALUES (1,'Institución de ejemplo A','San José',1),(2,'Institución de ejemplo B','Heredia',1);
/*!40000 ALTER TABLE `tb_institucion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_log_error`
--

DROP TABLE IF EXISTS `tb_log_error`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_log_error` (
  `IdLogError` bigint NOT NULL AUTO_INCREMENT,
  `FechaHora` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `Nivel` varchar(20) NOT NULL,
  `Origen` varchar(200) DEFAULT NULL,
  `Mensaje` text NOT NULL,
  `TipoExcepcion` varchar(200) DEFAULT NULL,
  `StackTrace` longtext,
  `UrlSolicitada` varchar(500) DEFAULT NULL,
  `MetodoHttp` varchar(10) DEFAULT NULL,
  `IdUsuario` int DEFAULT NULL,
  `DireccionIp` varchar(45) DEFAULT NULL,
  `NavegadorAgente` varchar(500) DEFAULT NULL,
  `Resuelto` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`IdLogError`),
  CONSTRAINT `CK_LogError_Nivel` CHECK ((`Nivel` in (_utf8mb4'Error',_utf8mb4'Advertencia',_utf8mb4'Crítico')))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_log_error`
--

LOCK TABLES `tb_log_error` WRITE;
/*!40000 ALTER TABLE `tb_log_error` DISABLE KEYS */;
INSERT INTO `tb_log_error` VALUES (1,'2026-09-12 09:15:00','Error','CitasController/Crear','Error de prueba: tiempo de espera agotado.','TimeoutException',NULL,'/Citas/Crear','POST',1,NULL,NULL,0);
/*!40000 ALTER TABLE `tb_log_error` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_material`
--

DROP TABLE IF EXISTS `tb_material`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_material` (
  `IdMaterial` int NOT NULL AUTO_INCREMENT,
  `IdTipoMaterial` int NOT NULL,
  `IdUsuarioRegistro` int NOT NULL,
  `Titulo` varchar(150) NOT NULL,
  `Descripcion` varchar(500) DEFAULT NULL,
  `RutaArchivo` varchar(500) DEFAULT NULL,
  `Url` varchar(500) DEFAULT NULL,
  `Publicado` tinyint(1) NOT NULL DEFAULT '0',
  `FechaPublicacion` datetime DEFAULT NULL,
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdMaterial`),
  KEY `IX_Material_TipoMaterial` (`IdTipoMaterial`),
  KEY `IX_Material_UsuarioRegistro` (`IdUsuarioRegistro`),
  CONSTRAINT `FK_Material_TipoMaterial` FOREIGN KEY (`IdTipoMaterial`) REFERENCES `tb_tipo_material` (`IdTipoMaterial`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Material_UsuarioRegistro` FOREIGN KEY (`IdUsuarioRegistro`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Material_PublicacionCoherente` CHECK (((`Publicado` = 0) or (`FechaPublicacion` is not null))),
  CONSTRAINT `CK_Material_TituloNoVacio` CHECK ((trim(`Titulo`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_material`
--

LOCK TABLES `tb_material` WRITE;
/*!40000 ALTER TABLE `tb_material` DISABLE KEYS */;
INSERT INTO `tb_material` VALUES (1,3,1,'Cuentos cortos para practicar','Selección de lecturas breves.',NULL,'https://example.com/cuentos',1,'2026-09-02 10:00:00','2026-09-15 16:45:37');
/*!40000 ALTER TABLE `tb_material` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_mensaje`
--

DROP TABLE IF EXISTS `tb_mensaje`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_mensaje` (
  `IdMensaje` int NOT NULL AUTO_INCREMENT,
  `IdUsuarioRemitente` int NOT NULL,
  `IdUsuarioDestinatario` int NOT NULL,
  `IdMensajePadre` int DEFAULT NULL,
  `Asunto` varchar(200) NOT NULL,
  `Cuerpo` longtext NOT NULL,
  `Leido` tinyint(1) NOT NULL DEFAULT '0',
  `FechaEnvio` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaLectura` datetime DEFAULT NULL,
  PRIMARY KEY (`IdMensaje`),
  KEY `IX_Mensaje_UsuarioRemitente` (`IdUsuarioRemitente`),
  KEY `IX_Mensaje_UsuarioDestinatario` (`IdUsuarioDestinatario`),
  KEY `IX_Mensaje_MensajePadre` (`IdMensajePadre`),
  CONSTRAINT `FK_Mensaje_MensajePadre` FOREIGN KEY (`IdMensajePadre`) REFERENCES `tb_mensaje` (`IdMensaje`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Mensaje_UsuarioDestinatario` FOREIGN KEY (`IdUsuarioDestinatario`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Mensaje_UsuarioRemitente` FOREIGN KEY (`IdUsuarioRemitente`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Mensaje_AsuntoNoVacio` CHECK ((trim(`Asunto`) <> _utf8mb4'')),
  CONSTRAINT `CK_Mensaje_FechaLectura` CHECK (((`FechaLectura` is null) or (`FechaLectura` >= `FechaEnvio`))),
  CONSTRAINT `CK_Mensaje_LecturaCoherente` CHECK ((((`Leido` = 0) and (`FechaLectura` is null)) or ((`Leido` = 1) and (`FechaLectura` is not null)))),
  CONSTRAINT `CK_Mensaje_RemitenteDistinto` CHECK ((`IdUsuarioRemitente` <> `IdUsuarioDestinatario`))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_mensaje`
--

LOCK TABLES `tb_mensaje` WRITE;
/*!40000 ALTER TABLE `tb_mensaje` DISABLE KEYS */;
INSERT INTO `tb_mensaje` VALUES (1,2,1,NULL,'Consulta sobre la tarea','¿La lectura se entrega impresa?',1,'2026-09-15 18:00:00','2026-09-15 19:00:00'),(2,1,2,1,'Re: Consulta sobre la tarea','No hace falta, basta con comentarla en la sesión.',0,'2026-09-15 19:05:00',NULL);
/*!40000 ALTER TABLE `tb_mensaje` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_metodo_pago`
--

DROP TABLE IF EXISTS `tb_metodo_pago`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_metodo_pago` (
  `IdMetodoPago` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`IdMetodoPago`),
  UNIQUE KEY `UX_MetodoPago_Nombre` (`Nombre`),
  CONSTRAINT `CK_MetodoPago_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_metodo_pago`
--

LOCK TABLES `tb_metodo_pago` WRITE;
/*!40000 ALTER TABLE `tb_metodo_pago` DISABLE KEYS */;
INSERT INTO `tb_metodo_pago` VALUES (1,'Efectivo'),(2,'SINPE Móvil'),(4,'Tarjeta'),(3,'Transferencia');
/*!40000 ALTER TABLE `tb_metodo_pago` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_modalidad`
--

DROP TABLE IF EXISTS `tb_modalidad`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_modalidad` (
  `IdModalidad` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(30) NOT NULL,
  PRIMARY KEY (`IdModalidad`),
  UNIQUE KEY `UX_Modalidad_Nombre` (`Nombre`),
  CONSTRAINT `CK_Modalidad_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_modalidad`
--

LOCK TABLES `tb_modalidad` WRITE;
/*!40000 ALTER TABLE `tb_modalidad` DISABLE KEYS */;
INSERT INTO `tb_modalidad` VALUES (1,'Presencial'),(2,'Virtual');
/*!40000 ALTER TABLE `tb_modalidad` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_nivel_educativo`
--

DROP TABLE IF EXISTS `tb_nivel_educativo`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_nivel_educativo` (
  `IdNivelEducativo` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  `Orden` int DEFAULT NULL,
  PRIMARY KEY (`IdNivelEducativo`),
  UNIQUE KEY `UX_NivelEducativo_Nombre` (`Nombre`),
  CONSTRAINT `CK_NivelEducativo_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4'')),
  CONSTRAINT `CK_NivelEducativo_Orden` CHECK (((`Orden` is null) or (`Orden` > 0)))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_nivel_educativo`
--

LOCK TABLES `tb_nivel_educativo` WRITE;
/*!40000 ALTER TABLE `tb_nivel_educativo` DISABLE KEYS */;
INSERT INTO `tb_nivel_educativo` VALUES (1,'Preescolar',1),(2,'I Ciclo',2),(3,'II Ciclo',3),(4,'III Ciclo',4),(5,'Educación diversificada',5);
/*!40000 ALTER TABLE `tb_nivel_educativo` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_notificacion`
--

DROP TABLE IF EXISTS `tb_notificacion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_notificacion` (
  `IdNotificacion` int NOT NULL AUTO_INCREMENT,
  `IdCita` int DEFAULT NULL,
  `IdUsuarioDestinatario` int NOT NULL,
  `IdCanalNotificacion` int NOT NULL,
  `Asunto` varchar(200) DEFAULT NULL,
  `Cuerpo` varchar(1000) NOT NULL,
  `FechaProgramada` datetime NOT NULL,
  `FechaEnvio` datetime DEFAULT NULL,
  `Enviada` tinyint(1) NOT NULL DEFAULT '0',
  `MensajeError` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`IdNotificacion`),
  KEY `IX_Notificacion_Cita` (`IdCita`),
  KEY `IX_Notificacion_UsuarioDestinatario` (`IdUsuarioDestinatario`),
  KEY `IX_Notificacion_CanalNotificacion` (`IdCanalNotificacion`),
  CONSTRAINT `FK_Notificacion_CanalNotificacion` FOREIGN KEY (`IdCanalNotificacion`) REFERENCES `tb_canal_notificacion` (`IdCanalNotificacion`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Notificacion_Cita` FOREIGN KEY (`IdCita`) REFERENCES `tb_cita` (`IdCita`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `FK_Notificacion_UsuarioDestinatario` FOREIGN KEY (`IdUsuarioDestinatario`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Notificacion_CuerpoNoVacio` CHECK ((trim(`Cuerpo`) <> _utf8mb4'')),
  CONSTRAINT `CK_Notificacion_EnvioCoherente` CHECK ((((`Enviada` = 0) and (`FechaEnvio` is null)) or ((`Enviada` = 1) and (`FechaEnvio` is not null))))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_notificacion`
--

LOCK TABLES `tb_notificacion` WRITE;
/*!40000 ALTER TABLE `tb_notificacion` DISABLE KEYS */;
INSERT INTO `tb_notificacion` VALUES (1,1,2,1,'Recordatorio de cita','Le recordamos la cita de Lucía el 5 de octubre a las 9:00.','2026-10-04 09:00:00',NULL,0,NULL);
/*!40000 ALTER TABLE `tb_notificacion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_pago`
--

DROP TABLE IF EXISTS `tb_pago`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_pago` (
  `IdPago` int NOT NULL AUTO_INCREMENT,
  `IdEstudiante` int NOT NULL,
  `IdEstadoPago` int NOT NULL,
  `IdUsuarioRegistro` int NOT NULL,
  `Concepto` varchar(150) NOT NULL,
  `Monto` decimal(10,2) NOT NULL,
  `FechaEmision` date NOT NULL,
  `FechaVencimiento` date DEFAULT NULL,
  `Observaciones` varchar(500) DEFAULT NULL,
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdPago`),
  KEY `IX_Pago_Estudiante` (`IdEstudiante`),
  KEY `IX_Pago_EstadoPago` (`IdEstadoPago`),
  KEY `IX_Pago_UsuarioRegistro` (`IdUsuarioRegistro`),
  CONSTRAINT `FK_Pago_EstadoPago` FOREIGN KEY (`IdEstadoPago`) REFERENCES `tb_estado_pago` (`IdEstadoPago`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Pago_Estudiante` FOREIGN KEY (`IdEstudiante`) REFERENCES `tb_estudiante` (`IdEstudiante`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Pago_UsuarioRegistro` FOREIGN KEY (`IdUsuarioRegistro`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Pago_ConceptoNoVacio` CHECK ((trim(`Concepto`) <> _utf8mb4'')),
  CONSTRAINT `CK_Pago_MontoPositivo` CHECK ((`Monto` > 0)),
  CONSTRAINT `CK_Pago_Vencimiento` CHECK (((`FechaVencimiento` is null) or (`FechaVencimiento` >= `FechaEmision`)))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_pago`
--

LOCK TABLES `tb_pago` WRITE;
/*!40000 ALTER TABLE `tb_pago` DISABLE KEYS */;
INSERT INTO `tb_pago` VALUES (1,1,1,1,'Mensualidad septiembre',60000.00,'2026-09-01','2026-09-10',NULL,'2026-09-15 16:45:37',NULL),(2,2,4,1,'Mensualidad septiembre',40000.00,'2026-09-01','2026-09-10',NULL,'2026-09-15 16:45:37',NULL),(3,1,2,1,'Mensualidad octubre',60000.00,'2026-10-01','2026-10-10',NULL,'2026-09-15 16:45:37',NULL),(4,2,3,1,'Mensualidad agosto',40000.00,'2026-08-01','2026-08-10',NULL,'2026-09-15 16:45:37',NULL);
/*!40000 ALTER TABLE `tb_pago` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_parentesco`
--

DROP TABLE IF EXISTS `tb_parentesco`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_parentesco` (
  `IdParentesco` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`IdParentesco`),
  UNIQUE KEY `UX_Parentesco_Nombre` (`Nombre`),
  CONSTRAINT `CK_Parentesco_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_parentesco`
--

LOCK TABLES `tb_parentesco` WRITE;
/*!40000 ALTER TABLE `tb_parentesco` DISABLE KEYS */;
INSERT INTO `tb_parentesco` VALUES (4,'Abuelo/a'),(1,'Madre'),(5,'Otro'),(2,'Padre'),(3,'Tutor legal');
/*!40000 ALTER TABLE `tb_parentesco` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_plan_intervencion`
--

DROP TABLE IF EXISTS `tb_plan_intervencion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_plan_intervencion` (
  `IdPlan` int NOT NULL AUTO_INCREMENT,
  `IdEstudiante` int NOT NULL,
  `IdEstadoPlan` int NOT NULL,
  `IdUsuarioRegistro` int NOT NULL,
  `Titulo` varchar(150) NOT NULL,
  `ObjetivoGeneral` varchar(1000) NOT NULL,
  `FechaInicio` date NOT NULL,
  `FechaFin` date DEFAULT NULL,
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdPlan`),
  KEY `IX_PlanIntervencion_Estudiante` (`IdEstudiante`),
  KEY `IX_PlanIntervencion_EstadoPlan` (`IdEstadoPlan`),
  KEY `IX_PlanIntervencion_UsuarioRegistro` (`IdUsuarioRegistro`),
  CONSTRAINT `FK_PlanIntervencion_EstadoPlan` FOREIGN KEY (`IdEstadoPlan`) REFERENCES `tb_estado_plan` (`IdEstadoPlan`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_PlanIntervencion_Estudiante` FOREIGN KEY (`IdEstudiante`) REFERENCES `tb_estudiante` (`IdEstudiante`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_PlanIntervencion_UsuarioRegistro` FOREIGN KEY (`IdUsuarioRegistro`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_PlanIntervencion_ObjetivoGeneralNoVacio` CHECK ((trim(`ObjetivoGeneral`) <> _utf8mb4'')),
  CONSTRAINT `CK_PlanIntervencion_TituloNoVacio` CHECK ((trim(`Titulo`) <> _utf8mb4'')),
  CONSTRAINT `CK_PlanIntervencion_Vigencia` CHECK (((`FechaFin` is null) or (`FechaFin` >= `FechaInicio`)))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_plan_intervencion`
--

LOCK TABLES `tb_plan_intervencion` WRITE;
/*!40000 ALTER TABLE `tb_plan_intervencion` DISABLE KEYS */;
INSERT INTO `tb_plan_intervencion` VALUES (1,1,2,1,'Plan de lectura comprensiva','Mejorar la comprensión de textos narrativos.','2026-09-01','2026-12-15','2026-09-15 16:45:36',NULL);
/*!40000 ALTER TABLE `tb_plan_intervencion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_plantilla_mensaje`
--

DROP TABLE IF EXISTS `tb_plantilla_mensaje`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_plantilla_mensaje` (
  `IdPlantilla` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(100) NOT NULL,
  `Asunto` varchar(200) DEFAULT NULL,
  `Cuerpo` longtext NOT NULL,
  `Activa` tinyint(1) NOT NULL DEFAULT '1',
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdPlantilla`),
  CONSTRAINT `CK_PlantillaMensaje_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_plantilla_mensaje`
--

LOCK TABLES `tb_plantilla_mensaje` WRITE;
/*!40000 ALTER TABLE `tb_plantilla_mensaje` DISABLE KEYS */;
/*!40000 ALTER TABLE `tb_plantilla_mensaje` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_reporte`
--

DROP TABLE IF EXISTS `tb_reporte`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_reporte` (
  `IdReporte` int NOT NULL AUTO_INCREMENT,
  `IdEstudiante` int NOT NULL,
  `IdUsuarioGenero` int NOT NULL,
  `Titulo` varchar(150) NOT NULL,
  `PeriodoInicio` date NOT NULL,
  `PeriodoFin` date NOT NULL,
  `Contenido` longtext,
  `RutaArchivo` varchar(500) DEFAULT NULL,
  `VisibleEnPortal` tinyint(1) NOT NULL DEFAULT '0',
  `FechaGeneracion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdReporte`),
  KEY `IX_Reporte_Estudiante` (`IdEstudiante`),
  KEY `IX_Reporte_UsuarioGenero` (`IdUsuarioGenero`),
  CONSTRAINT `FK_Reporte_Estudiante` FOREIGN KEY (`IdEstudiante`) REFERENCES `tb_estudiante` (`IdEstudiante`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Reporte_UsuarioGenero` FOREIGN KEY (`IdUsuarioGenero`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Reporte_Periodo` CHECK ((`PeriodoFin` >= `PeriodoInicio`)),
  CONSTRAINT `CK_Reporte_TituloNoVacio` CHECK ((trim(`Titulo`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_reporte`
--

LOCK TABLES `tb_reporte` WRITE;
/*!40000 ALTER TABLE `tb_reporte` DISABLE KEYS */;
INSERT INTO `tb_reporte` VALUES (1,1,1,'Reporte de progreso de septiembre','2026-09-01','2026-09-30','Avance sostenido en lectura.',NULL,1,'2026-09-15 16:45:37');
/*!40000 ALTER TABLE `tb_reporte` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_rol`
--

DROP TABLE IF EXISTS `tb_rol`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_rol` (
  `IdRol` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  `Descripcion` varchar(255) DEFAULT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`IdRol`),
  UNIQUE KEY `UX_Rol_Nombre` (`Nombre`),
  CONSTRAINT `CK_Rol_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_rol`
--

LOCK TABLES `tb_rol` WRITE;
/*!40000 ALTER TABLE `tb_rol` DISABLE KEYS */;
INSERT INTO `tb_rol` VALUES (1,'Administrador','Acceso completo, incluida la administración de usuarios',1),(2,'Psicopedagoga','Profesional a cargo de la atención de los estudiantes',1),(3,'Asistente','Apoyo administrativo en agenda, clientes y pagos',1),(4,'Encargado','Acceso al portal de encargados',1);
/*!40000 ALTER TABLE `tb_rol` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_servicio`
--

DROP TABLE IF EXISTS `tb_servicio`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_servicio` (
  `IdServicio` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(100) NOT NULL,
  `Descripcion` varchar(500) DEFAULT NULL,
  `PrecioReferencia` decimal(10,2) DEFAULT NULL,
  `Activo` tinyint(1) NOT NULL DEFAULT '1',
  PRIMARY KEY (`IdServicio`),
  UNIQUE KEY `UX_Servicio_Nombre` (`Nombre`),
  CONSTRAINT `CK_Servicio_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4'')),
  CONSTRAINT `CK_Servicio_Precio` CHECK (((`PrecioReferencia` is null) or (`PrecioReferencia` >= 0)))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_servicio`
--

LOCK TABLES `tb_servicio` WRITE;
/*!40000 ALTER TABLE `tb_servicio` DISABLE KEYS */;
INSERT INTO `tb_servicio` VALUES (1,'Tutoría académica','Acompañamiento en materias escolares y hábitos de estudio.',15000.00,1),(2,'Apoyo psicopedagógico','Intervención en dificultades de aprendizaje.',20000.00,1),(3,'Evaluaciones','Valoración psicopedagógica con informe de resultados.',45000.00,1),(4,'Orientación a padres','Sesiones de guía para las familias.',18000.00,1);
/*!40000 ALTER TABLE `tb_servicio` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_sesion`
--

DROP TABLE IF EXISTS `tb_sesion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_sesion` (
  `IdSesion` int NOT NULL AUTO_INCREMENT,
  `IdEstudiante` int NOT NULL,
  `IdCita` int DEFAULT NULL,
  `IdTipoAtencion` int NOT NULL,
  `IdUsuarioRegistro` int NOT NULL,
  `Fecha` date NOT NULL,
  `TemaTrabajado` varchar(500) NOT NULL,
  `Avances` varchar(1000) DEFAULT NULL,
  `Recomendaciones` varchar(1000) DEFAULT NULL,
  `Observaciones` varchar(1000) DEFAULT NULL,
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdSesion`),
  UNIQUE KEY `UX_Sesion_Cita` (`IdCita`),
  KEY `IX_Sesion_Estudiante` (`IdEstudiante`),
  KEY `IX_Sesion_TipoAtencion` (`IdTipoAtencion`),
  KEY `IX_Sesion_UsuarioRegistro` (`IdUsuarioRegistro`),
  CONSTRAINT `FK_Sesion_Cita` FOREIGN KEY (`IdCita`) REFERENCES `tb_cita` (`IdCita`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Sesion_Estudiante` FOREIGN KEY (`IdEstudiante`) REFERENCES `tb_estudiante` (`IdEstudiante`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Sesion_TipoAtencion` FOREIGN KEY (`IdTipoAtencion`) REFERENCES `tb_tipo_atencion` (`IdTipoAtencion`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Sesion_UsuarioRegistro` FOREIGN KEY (`IdUsuarioRegistro`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Sesion_TemaTrabajadoNoVacio` CHECK ((trim(`TemaTrabajado`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_sesion`
--

LOCK TABLES `tb_sesion` WRITE;
/*!40000 ALTER TABLE `tb_sesion` DISABLE KEYS */;
INSERT INTO `tb_sesion` VALUES (1,1,3,1,1,'2026-09-07','Lectura de textos cortos','Mejor fluidez.','Leer 15 minutos diarios.',NULL,'2026-09-15 16:45:36',NULL),(2,2,NULL,1,1,'2026-09-09','Resta con reagrupación','Resuelve con material concreto.','Practicar con monedas.',NULL,'2026-09-15 16:45:36',NULL);
/*!40000 ALTER TABLE `tb_sesion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_tarea`
--

DROP TABLE IF EXISTS `tb_tarea`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_tarea` (
  `IdTarea` int NOT NULL AUTO_INCREMENT,
  `IdMaterial` int DEFAULT NULL,
  `IdUsuarioRegistro` int NOT NULL,
  `Titulo` varchar(150) NOT NULL,
  `Descripcion` varchar(1000) DEFAULT NULL,
  `FechaLimite` date NOT NULL,
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdTarea`),
  KEY `IX_Tarea_Material` (`IdMaterial`),
  KEY `IX_Tarea_UsuarioRegistro` (`IdUsuarioRegistro`),
  CONSTRAINT `FK_Tarea_Material` FOREIGN KEY (`IdMaterial`) REFERENCES `tb_material` (`IdMaterial`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Tarea_UsuarioRegistro` FOREIGN KEY (`IdUsuarioRegistro`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Tarea_TituloNoVacio` CHECK ((trim(`Titulo`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_tarea`
--

LOCK TABLES `tb_tarea` WRITE;
/*!40000 ALTER TABLE `tb_tarea` DISABLE KEYS */;
INSERT INTO `tb_tarea` VALUES (1,1,1,'Lectura de la semana','Leer un cuento del material y comentarlo.','2026-09-20','2026-09-15 16:45:37');
/*!40000 ALTER TABLE `tb_tarea` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_tarea_asignacion`
--

DROP TABLE IF EXISTS `tb_tarea_asignacion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_tarea_asignacion` (
  `IdTareaAsignacion` int NOT NULL AUTO_INCREMENT,
  `IdTarea` int NOT NULL,
  `IdEstudiante` int NOT NULL,
  `IdEstadoTarea` int NOT NULL,
  `FechaEntrega` datetime DEFAULT NULL,
  `ComentarioEstudiante` varchar(500) DEFAULT NULL,
  `ComentarioRevision` varchar(500) DEFAULT NULL,
  PRIMARY KEY (`IdTareaAsignacion`),
  UNIQUE KEY `UX_TareaAsignacion_Relacion` (`IdTarea`,`IdEstudiante`),
  KEY `IX_TareaAsignacion_Estudiante` (`IdEstudiante`),
  KEY `IX_TareaAsignacion_EstadoTarea` (`IdEstadoTarea`),
  CONSTRAINT `FK_TareaAsignacion_EstadoTarea` FOREIGN KEY (`IdEstadoTarea`) REFERENCES `tb_estado_tarea` (`IdEstadoTarea`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_TareaAsignacion_Estudiante` FOREIGN KEY (`IdEstudiante`) REFERENCES `tb_estudiante` (`IdEstudiante`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `FK_TareaAsignacion_Tarea` FOREIGN KEY (`IdTarea`) REFERENCES `tb_tarea` (`IdTarea`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_tarea_asignacion`
--

LOCK TABLES `tb_tarea_asignacion` WRITE;
/*!40000 ALTER TABLE `tb_tarea_asignacion` DISABLE KEYS */;
INSERT INTO `tb_tarea_asignacion` VALUES (1,1,1,1,NULL,NULL,NULL),(2,1,2,2,'2026-09-18 16:30:00','Me gustó el cuento del perro.',NULL);
/*!40000 ALTER TABLE `tb_tarea_asignacion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_telefono`
--

DROP TABLE IF EXISTS `tb_telefono`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_telefono` (
  `IdTelefono` int NOT NULL AUTO_INCREMENT,
  `IdEncargado` int NOT NULL,
  `IdTipoTelefono` int NOT NULL,
  `CodigoPais` varchar(5) NOT NULL DEFAULT '506',
  `Numero` varchar(15) NOT NULL,
  `EsPrincipal` tinyint(1) NOT NULL DEFAULT '0',
  `EncargadoPrincipal` int GENERATED ALWAYS AS (if((`EsPrincipal` = 1),`IdEncargado`,NULL)) STORED,
  PRIMARY KEY (`IdTelefono`),
  UNIQUE KEY `UX_Telefono_Principal` (`EncargadoPrincipal`),
  KEY `IX_Telefono_Encargado` (`IdEncargado`),
  KEY `IX_Telefono_TipoTelefono` (`IdTipoTelefono`),
  CONSTRAINT `FK_Telefono_Encargado` FOREIGN KEY (`IdEncargado`) REFERENCES `tb_encargado` (`IdEncargado`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Telefono_TipoTelefono` FOREIGN KEY (`IdTipoTelefono`) REFERENCES `tb_tipo_telefono` (`IdTipoTelefono`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Telefono_CodigoPais` CHECK (regexp_like(`CodigoPais`,_utf8mb4'^[0-9]{1,4}$')),
  CONSTRAINT `CK_Telefono_LongitudCostaRica` CHECK (((`CodigoPais` <> _utf8mb4'506') or (char_length(`Numero`) = 8))),
  CONSTRAINT `CK_Telefono_NumeroDigitos` CHECK (regexp_like(`Numero`,_utf8mb4'^[0-9]+$'))
) ENGINE=InnoDB AUTO_INCREMENT=9 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_telefono`
--

LOCK TABLES `tb_telefono` WRITE;
/*!40000 ALTER TABLE `tb_telefono` DISABLE KEYS */;
INSERT INTO `tb_telefono` (`IdTelefono`, `IdEncargado`, `IdTipoTelefono`, `CodigoPais`, `Numero`, `EsPrincipal`) VALUES (1,1,1,'506','88880001',1),(2,1,2,'506','22220001',0),(3,2,1,'506','88880002',1),(4,5,1,'506','88887777',1),(5,6,1,'506','88888888',1),(6,7,1,'506','81345423',1),(7,8,1,'506','81345423',1),(8,9,1,'506','12345678',1);
/*!40000 ALTER TABLE `tb_telefono` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_testimonio`
--

DROP TABLE IF EXISTS `tb_testimonio`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_testimonio` (
  `IdTestimonio` int NOT NULL AUTO_INCREMENT,
  `NombreCliente` varchar(100) NOT NULL,
  `Comentario` varchar(1000) NOT NULL,
  `Calificacion` int DEFAULT NULL,
  `Publicado` tinyint(1) NOT NULL DEFAULT '0',
  `FechaRegistro` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdTestimonio`),
  CONSTRAINT `CK_Testimonio_Calificacion` CHECK (((`Calificacion` is null) or (`Calificacion` between 1 and 5))),
  CONSTRAINT `CK_Testimonio_ComentarioNoVacio` CHECK ((trim(`Comentario`) <> _utf8mb4'')),
  CONSTRAINT `CK_Testimonio_NombreClienteNoVacio` CHECK ((trim(`NombreCliente`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_testimonio`
--

LOCK TABLES `tb_testimonio` WRITE;
/*!40000 ALTER TABLE `tb_testimonio` DISABLE KEYS */;
INSERT INTO `tb_testimonio` VALUES (1,'Familia de prueba','Muy buena atención y seguimiento.',5,1,'2026-09-15 16:45:38');
/*!40000 ALTER TABLE `tb_testimonio` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_tipo_atencion`
--

DROP TABLE IF EXISTS `tb_tipo_atencion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_tipo_atencion` (
  `IdTipoAtencion` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`IdTipoAtencion`),
  UNIQUE KEY `UX_TipoAtencion_Nombre` (`Nombre`),
  CONSTRAINT `CK_TipoAtencion_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_tipo_atencion`
--

LOCK TABLES `tb_tipo_atencion` WRITE;
/*!40000 ALTER TABLE `tb_tipo_atencion` DISABLE KEYS */;
INSERT INTO `tb_tipo_atencion` VALUES (3,'Familiar'),(2,'Grupal'),(1,'Individual');
/*!40000 ALTER TABLE `tb_tipo_atencion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_tipo_material`
--

DROP TABLE IF EXISTS `tb_tipo_material`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_tipo_material` (
  `IdTipoMaterial` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  PRIMARY KEY (`IdTipoMaterial`),
  UNIQUE KEY `UX_TipoMaterial_Nombre` (`Nombre`),
  CONSTRAINT `CK_TipoMaterial_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_tipo_material`
--

LOCK TABLES `tb_tipo_material` WRITE;
/*!40000 ALTER TABLE `tb_tipo_material` DISABLE KEYS */;
INSERT INTO `tb_tipo_material` VALUES (4,'Actividad'),(2,'Documento PDF'),(3,'Enlace útil'),(1,'Guía educativa');
/*!40000 ALTER TABLE `tb_tipo_material` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_tipo_sesion`
--

DROP TABLE IF EXISTS `tb_tipo_sesion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_tipo_sesion` (
  `IdTipoSesion` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(50) NOT NULL,
  `DuracionMinutos` int DEFAULT NULL,
  PRIMARY KEY (`IdTipoSesion`),
  UNIQUE KEY `UX_TipoSesion_Nombre` (`Nombre`),
  CONSTRAINT `CK_TipoSesion_Duracion` CHECK (((`DuracionMinutos` is null) or (`DuracionMinutos` > 0))),
  CONSTRAINT `CK_TipoSesion_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_tipo_sesion`
--

LOCK TABLES `tb_tipo_sesion` WRITE;
/*!40000 ALTER TABLE `tb_tipo_sesion` DISABLE KEYS */;
INSERT INTO `tb_tipo_sesion` VALUES (1,'Tutoría',60),(2,'Sesión psicopedagógica',60),(3,'Evaluación',90),(4,'Reunión con padres',45);
/*!40000 ALTER TABLE `tb_tipo_sesion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_tipo_telefono`
--

DROP TABLE IF EXISTS `tb_tipo_telefono`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_tipo_telefono` (
  `IdTipoTelefono` int NOT NULL AUTO_INCREMENT,
  `Nombre` varchar(30) NOT NULL,
  PRIMARY KEY (`IdTipoTelefono`),
  UNIQUE KEY `UX_TipoTelefono_Nombre` (`Nombre`),
  CONSTRAINT `CK_TipoTelefono_NombreNoVacio` CHECK ((trim(`Nombre`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_tipo_telefono`
--

LOCK TABLES `tb_tipo_telefono` WRITE;
/*!40000 ALTER TABLE `tb_tipo_telefono` DISABLE KEYS */;
INSERT INTO `tb_tipo_telefono` VALUES (2,'Casa'),(1,'Móvil'),(3,'Trabajo');
/*!40000 ALTER TABLE `tb_tipo_telefono` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_token_recuperacion`
--

DROP TABLE IF EXISTS `tb_token_recuperacion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_token_recuperacion` (
  `IdToken` int NOT NULL AUTO_INCREMENT,
  `IdUsuario` int NOT NULL,
  `Token` varchar(255) NOT NULL,
  `FechaExpiracion` datetime NOT NULL,
  `Usado` tinyint(1) NOT NULL DEFAULT '0',
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdToken`),
  UNIQUE KEY `UX_TokenRecuperacion_Token` (`Token`),
  KEY `IX_TokenRecuperacion_Usuario` (`IdUsuario`),
  CONSTRAINT `FK_TokenRecuperacion_Usuario` FOREIGN KEY (`IdUsuario`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE CASCADE ON UPDATE RESTRICT,
  CONSTRAINT `CK_TokenRecuperacion_Expiracion` CHECK ((`FechaExpiracion` > `FechaCreacion`)),
  CONSTRAINT `CK_TokenRecuperacion_TokenNoVacio` CHECK ((trim(`Token`) <> _utf8mb4''))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_token_recuperacion`
--

LOCK TABLES `tb_token_recuperacion` WRITE;
/*!40000 ALTER TABLE `tb_token_recuperacion` DISABLE KEYS */;
/*!40000 ALTER TABLE `tb_token_recuperacion` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_usuario`
--

DROP TABLE IF EXISTS `tb_usuario`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_usuario` (
  `IdUsuario` int NOT NULL AUTO_INCREMENT,
  `IdEncargado` int DEFAULT NULL,
  `IdEstadoUsuario` int NOT NULL,
  `NombreCompleto` varchar(150) NOT NULL,
  `Correo` varchar(150) NOT NULL,
  `ContrasenaHash` varchar(255) NOT NULL,
  `ContrasenaSalt` varchar(100) DEFAULT NULL,
  `UltimoAcceso` datetime DEFAULT NULL,
  `IntentosFallidos` int NOT NULL DEFAULT '0',
  `FechaCreacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  `FechaModificacion` datetime DEFAULT NULL,
  PRIMARY KEY (`IdUsuario`),
  UNIQUE KEY `UX_Usuario_Correo` (`Correo`),
  UNIQUE KEY `UX_Usuario_Encargado` (`IdEncargado`),
  KEY `IX_Usuario_EstadoUsuario` (`IdEstadoUsuario`),
  CONSTRAINT `FK_Usuario_Encargado` FOREIGN KEY (`IdEncargado`) REFERENCES `tb_encargado` (`IdEncargado`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_Usuario_EstadoUsuario` FOREIGN KEY (`IdEstadoUsuario`) REFERENCES `tb_estado_usuario` (`IdEstadoUsuario`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `CK_Usuario_ContrasenaHashNoVacio` CHECK ((trim(`ContrasenaHash`) <> _utf8mb4'')),
  CONSTRAINT `CK_Usuario_CorreoNoVacio` CHECK ((trim(`Correo`) <> _utf8mb4'')),
  CONSTRAINT `CK_Usuario_IntentosFallidos` CHECK ((`IntentosFallidos` >= 0)),
  CONSTRAINT `CK_Usuario_NombreCompletoNoVacio` CHECK ((trim(`NombreCompleto`) <> _utf8mb4''))
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_usuario`
--

LOCK TABLES `tb_usuario` WRITE;
/*!40000 ALTER TABLE `tb_usuario` DISABLE KEYS */;
INSERT INTO `tb_usuario` VALUES (1,NULL,1,'Psicopedagoga de prueba','psicopedagoga.prueba@example.com','hash-de-relleno',NULL,NULL,0,'2026-09-15 16:45:35',NULL),(2,1,1,'Andrea Solís Mora','andrea.prueba@example.com','hash-de-relleno',NULL,NULL,0,'2026-09-15 16:45:35',NULL);
/*!40000 ALTER TABLE `tb_usuario` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `tb_usuario_rol`
--

DROP TABLE IF EXISTS `tb_usuario_rol`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tb_usuario_rol` (
  `IdUsuarioRol` int NOT NULL AUTO_INCREMENT,
  `IdUsuario` int NOT NULL,
  `IdRol` int NOT NULL,
  `FechaAsignacion` datetime NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`IdUsuarioRol`),
  UNIQUE KEY `UX_UsuarioRol_Relacion` (`IdUsuario`,`IdRol`),
  KEY `IX_UsuarioRol_Rol` (`IdRol`),
  CONSTRAINT `FK_UsuarioRol_Rol` FOREIGN KEY (`IdRol`) REFERENCES `tb_rol` (`IdRol`) ON DELETE RESTRICT ON UPDATE RESTRICT,
  CONSTRAINT `FK_UsuarioRol_Usuario` FOREIGN KEY (`IdUsuario`) REFERENCES `tb_usuario` (`IdUsuario`) ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=3 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `tb_usuario_rol`
--

LOCK TABLES `tb_usuario_rol` WRITE;
/*!40000 ALTER TABLE `tb_usuario_rol` DISABLE KEYS */;
INSERT INTO `tb_usuario_rol` VALUES (1,1,2,'2026-09-15 16:45:35'),(2,2,4,'2026-09-15 16:45:35');
/*!40000 ALTER TABLE `tb_usuario_rol` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping routines for database 'mente_activa'
--
/*!50003 DROP PROCEDURE IF EXISTS `SP_ConsultarClientes_CRM` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_ConsultarClientes_CRM`()
BEGIN
    SELECT 
      e.IdEncargado AS Id,
      CONCAT(e.Nombre, ' ', e.PrimerApellido) AS Encargado,
      t.Numero AS Telefono,
      e.Correo AS Correo,
      s.Nombre AS Servicio,
      CONCAT(est.Nombre, ' ', est.PrimerApellido) AS Estudiante,
      ec.Nombre AS Estado,
      e.IdEstadoCliente AS IdEstadoCliente
    FROM TB_ENCARGADO e
    JOIN TB_ESTADO_CLIENTE ec ON ec.IdEstadoCliente = e.IdEstadoCliente
    LEFT JOIN TB_SERVICIO s ON s.IdServicio = e.IdServicioInteres
    LEFT JOIN TB_TELEFONO t ON t.IdEncargado = e.IdEncargado AND t.EsPrincipal = 1
    LEFT JOIN TB_ESTUDIANTE_ENCARGADO ee ON ee.IdEncargado = e.IdEncargado AND ee.EsPrincipal = 1
    LEFT JOIN TB_ESTUDIANTE est ON est.IdEstudiante = ee.IdEstudiante
    WHERE e.Activo = 1
    ORDER BY e.FechaRegistro DESC;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `SP_ConsultarEstudiantes_CRM` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_ConsultarEstudiantes_CRM`()
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



END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `SP_ConsultarFichaEstudiante_CRM` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_ConsultarFichaEstudiante_CRM`(
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

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `SP_DesactivarCliente_CRM` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_DesactivarCliente_CRM`(
    IN p_IdEncargado INT
)
BEGIN

    UPDATE TB_ENCARGADO
    SET Activo = 0
    WHERE IdEncargado = p_IdEncargado;

END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `SP_ListarServiciosActivos_CRM` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_ListarServiciosActivos_CRM`()
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
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!50003 DROP PROCEDURE IF EXISTS `SP_RegistrarCliente_CRM` */;
/*!50003 SET @saved_cs_client      = @@character_set_client */ ;
/*!50003 SET @saved_cs_results     = @@character_set_results */ ;
/*!50003 SET @saved_col_connection = @@collation_connection */ ;
/*!50003 SET character_set_client  = utf8mb4 */ ;
/*!50003 SET character_set_results = utf8mb4 */ ;
/*!50003 SET collation_connection  = utf8mb4_0900_ai_ci */ ;
/*!50003 SET @saved_sql_mode       = @@sql_mode */ ;
/*!50003 SET sql_mode              = 'ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION' */ ;
DELIMITER ;;
CREATE DEFINER=`root`@`localhost` PROCEDURE `SP_RegistrarCliente_CRM`(
    IN p_NombreEncargado      VARCHAR(100),
    IN p_ApellidoEncargado    VARCHAR(100),
    IN p_Telefono             VARCHAR(15),
    IN p_Correo               VARCHAR(150),
    IN p_IdServicioInteres    INT,
    IN p_NombreEstudiante     VARCHAR(100),
    IN p_ApellidoEstudiante   VARCHAR(100),
    IN p_Observaciones        VARCHAR(1000)
)
BEGIN
    DECLARE v_IdEncargado  INT;
    DECLARE v_IdEstudiante INT;

    -- si algo falla a mitad de camino, se revierte todo (nada de cliente a medias)
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    -- IdEstadoCliente = 1 ("Nuevo") siempre al registrar
    INSERT INTO TB_ENCARGADO (IdEstadoCliente, IdServicioInteres, Nombre, PrimerApellido, Correo, Observaciones)
    VALUES (1, p_IdServicioInteres, p_NombreEncargado, p_ApellidoEncargado, p_Correo, p_Observaciones);

    SET v_IdEncargado = LAST_INSERT_ID();

    -- IdTipoTelefono = 1 ("Movil"); CodigoPais usa el default '506' (8 digitos exactos)
    INSERT INTO TB_TELEFONO (IdEncargado, IdTipoTelefono, Numero, EsPrincipal)
    VALUES (v_IdEncargado, 1, p_Telefono, 1);

    INSERT INTO TB_ESTUDIANTE (Nombre, PrimerApellido)
    VALUES (p_NombreEstudiante, p_ApellidoEstudiante);

    SET v_IdEstudiante = LAST_INSERT_ID();

    -- TODO: IdParentesco = 3 ("Tutor legal") es un valor temporal.
    -- Ni HU-M2-1 ni HU-M3-1 piden este dato en el formulario; confirmar con
    -- el equipo si se agrega el campo o se deja fijo a proposito.
    INSERT INTO TB_ESTUDIANTE_ENCARGADO (IdEstudiante, IdEncargado, IdParentesco, EsPrincipal)
    VALUES (v_IdEstudiante, v_IdEncargado, 3, 1);

    COMMIT;

    SELECT v_IdEncargado AS IdEncargado, v_IdEstudiante AS IdEstudiante;
END ;;
DELIMITER ;
/*!50003 SET sql_mode              = @saved_sql_mode */ ;
/*!50003 SET character_set_client  = @saved_cs_client */ ;
/*!50003 SET character_set_results = @saved_cs_results */ ;
/*!50003 SET collation_connection  = @saved_col_connection */ ;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-09-24 11:27:31
