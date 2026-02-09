-- =====================================================
-- MIGRACI?N: Optimizaci?n para patr?n IsSimulation
-- Fecha: 2024 - Motor de Detecci?n TFM
-- =====================================================

-- =====================================================
-- 1. VERIFICAR COLUMNA IsSimulation EN ALERTS
-- (Ya existe seg?n Complete_Schema.sql)
-- =====================================================
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.alerts') 
    AND name = 'IsSimulation'
)
BEGIN
    ALTER TABLE [dbo].[alerts]
    ADD [IsSimulation] [bit] NOT NULL DEFAULT 0;
    
    PRINT 'Columna IsSimulation agregada a dbo.alerts';
END
ELSE
BEGIN
    PRINT 'Columna IsSimulation ya existe en dbo.alerts';
END
GO

-- =====================================================
-- 2. VERIFICAR COLUMNA IsAnalyzed EN sim_Events
-- (Para detector cuando soporte simulador)
-- =====================================================
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'dbo.sim_Events') 
    AND name = 'IsAnalyzed'
)
BEGIN
    ALTER TABLE [dbo].[sim_Events]
    ADD [IsAnalyzed] [bit] NOT NULL DEFAULT 0;
    
    PRINT 'Columna IsAnalyzed agregada a dbo.sim_Events';
END
ELSE
BEGIN
    PRINT 'Columna IsAnalyzed ya existe en dbo.sim_Events';
END
GO

-- =====================================================
-- 3. ?NDICES OPTIMIZADOS PARA PATR?N IsSimulation
-- =====================================================

-- ?ndice compuesto: Alerts por IsSimulation y DetectedAt (para filtrado r?pido)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_alerts_IsSimulation_DetectedAt' 
    AND object_id = OBJECT_ID(N'dbo.alerts')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_alerts_IsSimulation_DetectedAt]
    ON [dbo].[alerts]([IsSimulation] ASC, [DetectedAt] DESC)
    INCLUDE ([Severity], [Status], [RiskScore], [UserPrincipalName]);
    
    PRINT '?ndice IX_alerts_IsSimulation_DetectedAt creado';
END
ELSE
BEGIN
    PRINT '?ndice IX_alerts_IsSimulation_DetectedAt ya existe';
END
GO

-- ?ndice compuesto: Alerts por IsSimulation, Status y Severity (para dashboard)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_alerts_IsSimulation_Status_Severity' 
    AND object_id = OBJECT_ID(N'dbo.alerts')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_alerts_IsSimulation_Status_Severity]
    ON [dbo].[alerts]([IsSimulation] ASC, [Status] ASC, [Severity] ASC);
    
    PRINT '?ndice IX_alerts_IsSimulation_Status_Severity creado';
END
ELSE
BEGIN
    PRINT '?ndice IX_alerts_IsSimulation_Status_Severity ya existe';
END
GO

-- ?ndice para eventos REALES no analizados (optimiza DetectionFunction)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_access_Events_IsAnalyzed_CreatedAt' 
    AND object_id = OBJECT_ID(N'dbo.access_Events')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_access_Events_IsAnalyzed_CreatedAt]
    ON [dbo].[access_Events]([IsAnalyzed] ASC, [CreatedAt] DESC)
    WHERE [IsAnalyzed] = 0;
    
    PRINT '?ndice IX_access_Events_IsAnalyzed_CreatedAt creado';
END
ELSE
BEGIN
    PRINT '?ndice IX_access_Events_IsAnalyzed_CreatedAt ya existe';
END
GO

-- ?ndice para eventos SIMULADOS no analizados (para futuro)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_sim_Events_IsAnalyzed_CreatedAt' 
    AND object_id = OBJECT_ID(N'dbo.sim_Events')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_sim_Events_IsAnalyzed_CreatedAt]
    ON [dbo].[sim_Events]([IsAnalyzed] ASC, [CreatedAt] DESC)
    WHERE [IsAnalyzed] = 0;
    
    PRINT '?ndice IX_sim_Events_IsAnalyzed_CreatedAt creado';
END
ELSE
BEGIN
    PRINT '?ndice IX_sim_Events_IsAnalyzed_CreatedAt ya existe';
END
GO

-- ?ndice para construcci?n de perfiles REALES (UserBehaviorProfile)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_access_Events_UserId_CreatedAt' 
    AND object_id = OBJECT_ID(N'dbo.access_Events')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_access_Events_UserId_CreatedAt]
    ON [dbo].[access_Events]([UserId] ASC, [CreatedAt] DESC)
    INCLUDE ([Country], [City], [IpAddress], [DeviceId], [ClientApp], [AuthMethod], [Result]);
    
    PRINT '?ndice IX_access_Events_UserId_CreatedAt creado';
END
ELSE
BEGIN
    PRINT '?ndice IX_access_Events_UserId_CreatedAt ya existe';
END
GO

-- ?ndice para construcci?n de perfiles SIMULADOS (para futuro)
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_sim_Events_UserId_CreatedAt' 
    AND object_id = OBJECT_ID(N'dbo.sim_Events')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_sim_Events_UserId_CreatedAt]
    ON [dbo].[sim_Events]([UserId] ASC, [CreatedAt] DESC)
    INCLUDE ([Country], [City], [IpAddress], [DeviceId], [ClientApp], [AuthMethod], [Result]);
    
    PRINT '?ndice IX_sim_Events_UserId_CreatedAt creado';
END
ELSE
BEGIN
    PRINT '?ndice IX_sim_Events_UserId_CreatedAt ya existe';
END
GO

-- =====================================================
-- 4. ESTAD?STICAS ACTUALIZADAS
-- =====================================================
UPDATE STATISTICS [dbo].[alerts] WITH FULLSCAN;
UPDATE STATISTICS [dbo].[access_Events] WITH FULLSCAN;
UPDATE STATISTICS [dbo].[sim_Events] WITH FULLSCAN;
GO

PRINT '';
PRINT '=====================================================';
PRINT 'MIGRACI?N COMPLETADA - Patr?n IsSimulation optimizado';
PRINT '=====================================================';
PRINT 'Cambios aplicados:';
PRINT '  - Columna IsSimulation verificada en alerts';
PRINT '  - Columna IsAnalyzed agregada a sim_Events';
PRINT '  - 6 ?ndices compuestos creados/verificados';
PRINT '  - Estad?sticas actualizadas';
PRINT '';
PRINT 'Beneficios:';
PRINT '  - Filtrado r?pido por IsSimulation en alertas';
PRINT '  - Detecci?n eficiente de eventos pendientes';
PRINT '  - Construcci?n optimizada de perfiles de usuario';
PRINT '  - Soporte futuro para motor con datos simulados';
PRINT '=====================================================';
GO
