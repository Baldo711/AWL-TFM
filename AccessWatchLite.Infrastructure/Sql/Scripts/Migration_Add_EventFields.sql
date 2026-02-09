-- Script de migración: Agregar columnas nuevas a sim_Events y access_Events
-- Ejecutar en Azure SQL Database

USE [AccessWatchLite];
GO

PRINT 'Iniciando migración: Nuevas columnas para sim_Events y access_Events';
GO

-- ==========================================
-- TABLA: sim_Events
-- ==========================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[sim_Events]
    ADD [Status] NVARCHAR(50) NULL;
    PRINT '? Columna [Status] agregada a sim_Events';
END
ELSE
    PRINT '? Columna [Status] ya existe en sim_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') AND name = 'ConditionalAccess')
BEGIN
    ALTER TABLE [dbo].[sim_Events]
    ADD [ConditionalAccess] NVARCHAR(50) NULL;
    PRINT '? Columna [ConditionalAccess] agregada a sim_Events';
END
ELSE
    PRINT '? Columna [ConditionalAccess] ya existe en sim_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') AND name = 'Error')
BEGIN
    ALTER TABLE [dbo].[sim_Events]
    ADD [Error] NVARCHAR(500) NULL;
    PRINT '? Columna [Error] agregada a sim_Events';
END
ELSE
    PRINT '? Columna [Error] ya existe en sim_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') AND name = 'ClientResource')
BEGIN
    ALTER TABLE [dbo].[sim_Events]
    ADD [ClientResource] NVARCHAR(255) NULL;
    PRINT '? Columna [ClientResource] agregada a sim_Events';
END
ELSE
    PRINT '? Columna [ClientResource] ya existe en sim_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') AND name = 'IsIgnored')
BEGIN
    ALTER TABLE [dbo].[sim_Events]
    ADD [IsIgnored] BIT NOT NULL DEFAULT 0;
    PRINT '? Columna [IsIgnored] agregada a sim_Events';
END
ELSE
    PRINT '? Columna [IsIgnored] ya existe en sim_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[sim_Events]
    ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
    PRINT '? Columna [CreatedAt] agregada a sim_Events';
END
ELSE
    PRINT '? Columna [CreatedAt] ya existe en sim_Events';

-- ==========================================
-- TABLA: access_Events
-- ==========================================

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') AND name = 'Status')
BEGIN
    ALTER TABLE [dbo].[access_Events]
    ADD [Status] NVARCHAR(50) NULL;
    PRINT '? Columna [Status] agregada a access_Events';
END
ELSE
    PRINT '? Columna [Status] ya existe en access_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') AND name = 'ConditionalAccess')
BEGIN
    ALTER TABLE [dbo].[access_Events]
    ADD [ConditionalAccess] NVARCHAR(50) NULL;
    PRINT '? Columna [ConditionalAccess] agregada a access_Events';
END
ELSE
    PRINT '? Columna [ConditionalAccess] ya existe en access_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') AND name = 'Error')
BEGIN
    ALTER TABLE [dbo].[access_Events]
    ADD [Error] NVARCHAR(500) NULL;
    PRINT '? Columna [Error] agregada a access_Events';
END
ELSE
    PRINT '? Columna [Error] ya existe en access_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') AND name = 'ClientResource')
BEGIN
    ALTER TABLE [dbo].[access_Events]
    ADD [ClientResource] NVARCHAR(255) NULL;
    PRINT '? Columna [ClientResource] agregada a access_Events';
END
ELSE
    PRINT '? Columna [ClientResource] ya existe en access_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') AND name = 'IsIgnored')
BEGIN
    ALTER TABLE [dbo].[access_Events]
    ADD [IsIgnored] BIT NOT NULL DEFAULT 0;
    PRINT '? Columna [IsIgnored] agregada a access_Events';
END
ELSE
    PRINT '? Columna [IsIgnored] ya existe en access_Events';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE [dbo].[access_Events]
    ADD [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE();
    PRINT '? Columna [CreatedAt] agregada a access_Events';
END
ELSE
    PRINT '? Columna [CreatedAt] ya existe en access_Events';

GO

PRINT '';
PRINT '========================================';
PRINT 'Migración completada exitosamente';
PRINT '========================================';
PRINT 'Nuevas columnas agregadas:';
PRINT '  • Status (Estado del evento)';
PRINT '  • ConditionalAccess (Acceso condicional)';
PRINT '  • Error (Motivo del error)';
PRINT '  • ClientResource (Recurso accedido)';
PRINT '  • IsIgnored (Marcar registros erróneos)';
PRINT '  • CreatedAt (Timestamp de inserción)';
PRINT '';
PRINT '??  IMPORTANTE: Actualizar SimLoaderFunction para mapear estas columnas';
GO
