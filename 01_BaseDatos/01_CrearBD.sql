-- ============================================================================
-- PROYECTO: VisorDatosSIG 2026
-- ARCHIVO: 01_BaseDatos/01_CrearBD.sql
-- DESCRIPCIÓN: Creación de la Base de Datos Espacial 'VisorDatosSIG_BD'
-- MOTOR: Microsoft SQL Server 2022
-- SISTEMA DE REFERENCIA ESPACIAL: WGS 84 (SRID 4326)
-- ============================================================================

USE master;
GO

-- 1. Cerrar conexiones activas si la base de datos ya existe y eliminarla limpiamente
IF EXISTS (SELECT name FROM sys.databases WHERE name = N'VisorDatosSIG_BD')
BEGIN
    PRINT '>> Cerrando conexiones y eliminando base de datos existente VisorDatosSIG_BD...';
    ALTER DATABASE VisorDatosSIG_BD SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE VisorDatosSIG_BD;
    PRINT '>> Base de datos anterior eliminada correctamente.';
END
GO

-- 2. Crear la base de datos con collation moderna compatible con español (insensible a mayúsculas y acentos según conveniencia estándar CI_AS)
PRINT '>> Creando base de datos VisorDatosSIG_BD...';
CREATE DATABASE VisorDatosSIG_BD
COLLATE Modern_Spanish_CI_AS;
GO

-- 3. Configuración de parámetros de base de datos
USE VisorDatosSIG_BD;
GO

-- Habilitar aislamiento de transacciones de lectura confirmada para alta concurrencia
ALTER DATABASE VisorDatosSIG_BD SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE VisorDatosSIG_BD SET ALLOW_SNAPSHOT_ISOLATION ON;
GO

-- Habilitar crecimiento automático y compatibilidad SQL Server 2022 (nivel 160)
ALTER DATABASE VisorDatosSIG_BD SET COMPATIBILITY_LEVEL = 160;
GO

PRINT '>> Base de datos VisorDatosSIG_BD creada y configurada exitosamente.';
GO
