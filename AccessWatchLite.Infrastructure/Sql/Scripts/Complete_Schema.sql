-- =====================================================
-- AccessWatchLite - Database Schema Complete
-- TFM: Sistema de detección y respuesta ante accesos anómalos
-- =====================================================

-- =====================================================
-- 1. TABLA DE EVENTOS SIMULADOR (YA EXISTE - REFERENCIA)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sim_Events')
BEGIN
    CREATE TABLE [dbo].[sim_Events](
        [Id] [uniqueidentifier] NOT NULL,
        [EventId] [nvarchar](128) NOT NULL,
        [UserId] [nvarchar](128) NULL,
        [UserPrincipalName] [nvarchar](256) NULL,
        [TimestampUtc] [datetime2](7) NOT NULL,
        [IpAddress] [nvarchar](64) NOT NULL,
        [Country] [nvarchar](128) NULL,
        [City] [nvarchar](128) NULL,
        [DeviceId] [nvarchar](256) NULL,
        [DeviceName] [nvarchar](256) NULL,
        [ClientApp] [nvarchar](128) NULL,
        [AuthMethod] [nvarchar](128) NULL,
        [Result] [nvarchar](64) NULL,
        [RiskLevel] [nvarchar](64) NULL,
        [RiskEventTypesJson] [nvarchar](MAX) NULL,
        [RawJson] [nvarchar](MAX) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_sim_Events] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_sim_Events_TimestampUtc] ON [dbo].[sim_Events]([TimestampUtc] DESC);
    CREATE INDEX [IX_sim_Events_UserId] ON [dbo].[sim_Events]([UserId]);
END
GO

-- =====================================================
-- 2. TABLA DE EVENTOS REALES (PRODUCCIÓN)
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'access_Events')
BEGIN
    CREATE TABLE [dbo].[access_Events](
        [Id] [uniqueidentifier] NOT NULL,
        [EventId] [nvarchar](128) NOT NULL,
        [UserId] [nvarchar](128) NULL,
        [UserPrincipalName] [nvarchar](256) NULL,
        [TimestampUtc] [datetime2](7) NOT NULL,
        [IpAddress] [nvarchar](64) NOT NULL,
        [Country] [nvarchar](128) NULL,
        [City] [nvarchar](128) NULL,
        [DeviceId] [nvarchar](256) NULL,
        [DeviceName] [nvarchar](256) NULL,
        [ClientApp] [nvarchar](128) NULL,
        [AuthMethod] [nvarchar](128) NULL,
        [Result] [nvarchar](64) NULL,
        [RiskLevel] [nvarchar](64) NULL,
        [RiskEventTypesJson] [nvarchar](MAX) NULL,
        [RawJson] [nvarchar](MAX) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        [IsAnalyzed] [bit] NOT NULL DEFAULT 0,
        CONSTRAINT [PK_access_Events] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_access_Events_TimestampUtc] ON [dbo].[access_Events]([TimestampUtc] DESC);
    CREATE INDEX [IX_access_Events_UserId] ON [dbo].[access_Events]([UserId]);
    CREATE INDEX [IX_access_Events_IsAnalyzed] ON [dbo].[access_Events]([IsAnalyzed]) WHERE [IsAnalyzed] = 0;
END
GO

-- =====================================================
-- 3. TABLA DE PERFILES DE USUARIO (Baseline)
-- Para análisis contextual: comportamiento habitual
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'user_Profiles')
BEGIN
    CREATE TABLE [dbo].[user_Profiles](
        [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [UserId] [nvarchar](128) NOT NULL,
        [UserPrincipalName] [nvarchar](256) NULL,
        
        -- Ubicaciones habituales (JSON array de países/ciudades)
        [UsualCountries] [nvarchar](500) NULL,
        [UsualCities] [nvarchar](1000) NULL,
        
        -- IPs habituales (JSON array)
        [UsualIpRanges] [nvarchar](1000) NULL,
        
        -- Dispositivos conocidos (JSON array de DeviceIds)
        [KnownDevices] [nvarchar](MAX) NULL,
        
        -- Horarios habituales (formato: "08:00-18:00,Mo-Fr")
        [UsualSchedule] [nvarchar](500) NULL,
        
        -- Métodos de autenticación habituales
        [UsualAuthMethods] [nvarchar](500) NULL,
        
        -- Aplicaciones cliente habituales
        [UsualClientApps] [nvarchar](1000) NULL,
        
        -- Estadísticas
        [TotalAccessCount] [int] NOT NULL DEFAULT 0,
        [FailedAccessCount] [int] NOT NULL DEFAULT 0,
        [LastAccessDate] [datetime2](7) NULL,
        
        -- Configuración
        [IsHighPrivilege] [bit] NOT NULL DEFAULT 0,
        [CustomRiskThreshold] [decimal](5,2) NULL,
        
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        [UpdatedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        
        CONSTRAINT [PK_user_Profiles] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_user_Profiles_UserId] UNIQUE ([UserId])
    );
    CREATE INDEX [IX_user_Profiles_UserId] ON [dbo].[user_Profiles]([UserId]);
END
GO

-- =====================================================
-- 4. TABLA DE REGLAS DE DETECCIÓN
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'detection_Rules')
BEGIN
    CREATE TABLE [dbo].[detection_Rules](
        [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [Name] [nvarchar](200) NOT NULL,
        [Description] [nvarchar](1000) NULL,
        [RuleType] [nvarchar](50) NOT NULL, -- 'Signal', 'Threshold', 'Composite'
        [SignalName] [nvarchar](100) NULL, -- Ej: 'UnusualLocation', 'UnknownDevice'
        [Weight] [decimal](5,2) NOT NULL DEFAULT 1.0,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [Priority] [int] NOT NULL DEFAULT 5,
        [ConfigJson] [nvarchar](MAX) NULL, -- Parámetros adicionales en JSON
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        [UpdatedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_detection_Rules] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_detection_Rules_IsEnabled] ON [dbo].[detection_Rules]([IsEnabled]) WHERE [IsEnabled] = 1;
END
GO

-- =====================================================
-- 5. TABLA DE ALERTAS/INCIDENTES
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'alerts')
BEGIN
    CREATE TABLE [dbo].[alerts](
        [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [EventId] [uniqueidentifier] NOT NULL,
        [UserId] [nvarchar](128) NULL,
        [UserPrincipalName] [nvarchar](256) NULL,
        
        -- Clasificación
        [Severity] [nvarchar](20) NOT NULL, -- 'Low', 'Medium', 'High'
        [RiskScore] [decimal](5,2) NOT NULL,
        [Status] [nvarchar](20) NOT NULL DEFAULT 'New', -- 'New', 'Investigating', 'Resolved', 'FalsePositive'
        
        -- Detalles
        [Title] [nvarchar](200) NOT NULL,
        [Description] [nvarchar](MAX) NULL,
        [DetectedSignals] [nvarchar](MAX) NULL, -- JSON array de señales detectadas
        
        -- Contexto del evento
        [EventTimestamp] [datetime2](7) NOT NULL,
        [IpAddress] [nvarchar](64) NULL,
        [Country] [nvarchar](128) NULL,
        [City] [nvarchar](128) NULL,
        [DeviceId] [nvarchar](256) NULL,
        
        -- Metadatos
        [DetectedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        [ResolvedAt] [datetime2](7) NULL,
        [ResolvedBy] [nvarchar](256) NULL,
        [Resolution] [nvarchar](MAX) NULL,
        
        -- Modo
        [IsSimulation] [bit] NOT NULL DEFAULT 0,
        
        CONSTRAINT [PK_alerts] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
    CREATE INDEX [IX_alerts_Status] ON [dbo].[alerts]([Status]);
    CREATE INDEX [IX_alerts_Severity] ON [dbo].[alerts]([Severity]);
    CREATE INDEX [IX_alerts_DetectedAt] ON [dbo].[alerts]([DetectedAt] DESC);
    CREATE INDEX [IX_alerts_UserId] ON [dbo].[alerts]([UserId]);
    CREATE INDEX [IX_alerts_IsSimulation] ON [dbo].[alerts]([IsSimulation]);
END
GO

-- =====================================================
-- 6. TABLA DE ACCIONES DE RESPUESTA
-- =====================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'response_Actions')
BEGIN
    CREATE TABLE [dbo].[response_Actions](
        [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [AlertId] [uniqueidentifier] NOT NULL,
        [ActionType] [nvarchar](50) NOT NULL, -- 'Notify', 'BlockUser', 'RevokeSession', 'Log'
        [ActionStatus] [nvarchar](20) NOT NULL, -- 'Pending', 'Executed', 'Failed'
        [ExecutedAt] [datetime2](7) NULL,
        [Result] [nvarchar](MAX) NULL,
        [ErrorMessage] [nvarchar](MAX) NULL,
        [IsSimulation] [bit] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        CONSTRAINT [PK_response_Actions] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_response_Actions_alerts] FOREIGN KEY ([AlertId]) REFERENCES [dbo].[alerts]([Id])
    );
    CREATE INDEX [IX_response_Actions_AlertId] ON [dbo].[response_Actions]([AlertId]);
    CREATE INDEX [IX_response_Actions_ActionStatus] ON [dbo].[response_Actions]([ActionStatus]);
END
GO

-- =====================================================
-- 7. INSERTAR REGLAS DE DETECCIÓN INICIALES
-- =====================================================
IF NOT EXISTS (SELECT * FROM [dbo].[detection_Rules])
BEGIN
    -- Regla 1: Ubicación inusual
    INSERT INTO [dbo].[detection_Rules] ([Id], [Name], [Description], [RuleType], [SignalName], [Weight], [IsEnabled], [Priority])
    VALUES (NEWID(), 'Ubicación Inusual', 'Acceso desde un país o ciudad no habitual para el usuario', 'Signal', 'UnusualLocation', 3.0, 1, 8);
    
    -- Regla 2: Dispositivo desconocido
    INSERT INTO [dbo].[detection_Rules] ([Id], [Name], [Description], [RuleType], [SignalName], [Weight], [IsEnabled], [Priority])
    VALUES (NEWID(), 'Dispositivo Desconocido', 'Acceso desde un dispositivo no registrado en el perfil del usuario', 'Signal', 'UnknownDevice', 2.5, 1, 7);
    
    -- Regla 3: Horario atípico
    INSERT INTO [dbo].[detection_Rules] ([Id], [Name], [Description], [RuleType], [SignalName], [Weight], [IsEnabled], [Priority])
    VALUES (NEWID(), 'Horario Atípico', 'Acceso fuera del horario habitual del usuario', 'Signal', 'UnusualTime', 1.5, 1, 5);
    
    -- Regla 4: Múltiples fallos previos
    INSERT INTO [dbo].[detection_Rules] ([Id], [Name], [Description], [RuleType], [SignalName], [Weight], [IsEnabled], [Priority])
    VALUES (NEWID(), 'Múltiples Fallos Previos', 'Varios intentos fallidos antes de un acceso exitoso', 'Signal', 'MultipleFailures', 4.0, 1, 9);
    
    -- Regla 5: Cambio brusco de IP
    INSERT INTO [dbo].[detection_Rules] ([Id], [Name], [Description], [RuleType], [SignalName], [Weight], [IsEnabled], [Priority])
    VALUES (NEWID(), 'Cambio Brusco de IP', 'Cambio significativo en la dirección IP en poco tiempo', 'Signal', 'IpChange', 2.0, 1, 6);
    
    -- Regla 6: Método de autenticación débil
    INSERT INTO [dbo].[detection_Rules] ([Id], [Name], [Description], [RuleType], [SignalName], [Weight], [IsEnabled], [Priority])
    VALUES (NEWID(), 'Autenticación Débil', 'Uso de método de autenticación menos seguro de lo habitual', 'Signal', 'WeakAuth', 1.0, 1, 4);
END
GO

-- =====================================================
-- 8. VISTA CONSOLIDADA DE EVENTOS (Para Detection)
-- =====================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'v_AllEvents')
    DROP VIEW [dbo].[v_AllEvents];
GO

CREATE VIEW [dbo].[v_AllEvents] AS
SELECT 
    Id, EventId, UserId, UserPrincipalName, TimestampUtc, 
    IpAddress, Country, City, DeviceId, DeviceName, 
    ClientApp, AuthMethod, Result, RiskLevel, RiskEventTypesJson, 
    RawJson, CreatedAt,
    CAST(1 AS bit) AS IsSimulation,
    CAST(1 AS bit) AS IsAnalyzed -- En sim siempre consideramos analizados
FROM [dbo].[sim_Events]

UNION ALL

SELECT 
    Id, EventId, UserId, UserPrincipalName, TimestampUtc, 
    IpAddress, Country, City, DeviceId, DeviceName, 
    ClientApp, AuthMethod, Result, RiskLevel, RiskEventTypesJson, 
    RawJson, CreatedAt,
    CAST(0 AS bit) AS IsSimulation,
    IsAnalyzed
FROM [dbo].[access_Events];
GO

PRINT 'Database schema created successfully!';
