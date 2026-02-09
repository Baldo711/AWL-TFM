-- =====================================================
-- MIGRATION SCRIPT: Agregar columnas IsAnalyzed y AnalyzedAt
-- =====================================================
-- Fecha: 2026-02-08
-- Propósito: Habilitar el motor de detección en sim_Events
-- =====================================================

-- Verificar y agregar IsAnalyzed a sim_Events
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') 
    AND name = 'IsAnalyzed'
)
BEGIN
    PRINT 'Agregando columna IsAnalyzed a sim_Events...';
    ALTER TABLE [dbo].[sim_Events]
    ADD [IsAnalyzed] [bit] NOT NULL DEFAULT 0;
    
    -- Crear índice para optimizar consultas WHERE IsAnalyzed = 0
    CREATE INDEX [IX_sim_Events_IsAnalyzed] 
    ON [dbo].[sim_Events]([IsAnalyzed]) 
    WHERE [IsAnalyzed] = 0;
    
    PRINT 'Columna IsAnalyzed agregada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La columna IsAnalyzed ya existe en sim_Events.';
END
GO

-- Verificar y agregar AnalyzedAt a sim_Events
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') 
    AND name = 'AnalyzedAt'
)
BEGIN
    PRINT 'Agregando columna AnalyzedAt a sim_Events...';
    ALTER TABLE [dbo].[sim_Events]
    ADD [AnalyzedAt] [datetime2](7) NULL;
    
    PRINT 'Columna AnalyzedAt agregada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La columna AnalyzedAt ya existe en sim_Events.';
END
GO

-- Verificar y agregar IsAnalyzed a access_Events (por si acaso)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') 
    AND name = 'IsAnalyzed'
)
BEGIN
    PRINT 'Agregando columna IsAnalyzed a access_Events...';
    ALTER TABLE [dbo].[access_Events]
    ADD [IsAnalyzed] [bit] NOT NULL DEFAULT 0;
    
    CREATE INDEX [IX_access_Events_IsAnalyzed] 
    ON [dbo].[access_Events]([IsAnalyzed]) 
    WHERE [IsAnalyzed] = 0;
    
    PRINT 'Columna IsAnalyzed agregada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La columna IsAnalyzed ya existe en access_Events.';
END
GO

-- Verificar y agregar AnalyzedAt a access_Events (por si acaso)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') 
    AND name = 'AnalyzedAt'
)
BEGIN
    PRINT 'Agregando columna AnalyzedAt a access_Events...';
    ALTER TABLE [dbo].[access_Events]
    ADD [AnalyzedAt] [datetime2](7) NULL;
    
    PRINT 'Columna AnalyzedAt agregada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La columna AnalyzedAt ya existe en access_Events.';
END
GO

-- Verificar resultados
PRINT '';
PRINT '========================================';
PRINT 'VERIFICACIÓN DE COLUMNAS AGREGADAS';
PRINT '========================================';

SELECT 
    'sim_Events' AS TableName,
    COUNT(*) AS ColumnCount
FROM sys.columns 
WHERE object_id = OBJECT_ID(N'[dbo].[sim_Events]') 
  AND name IN ('IsAnalyzed', 'AnalyzedAt')
UNION ALL
SELECT 
    'access_Events' AS TableName,
    COUNT(*) AS ColumnCount
FROM sys.columns 
WHERE object_id = OBJECT_ID(N'[dbo].[access_Events]') 
  AND name IN ('IsAnalyzed', 'AnalyzedAt');

PRINT 'Migración completada exitosamente.';
GO
