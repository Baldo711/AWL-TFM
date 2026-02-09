-- =====================================================
-- SCRIPT DE PRUEBA: Generar Datos para Disparar Alertas
-- Propósito: Crear eventos diseñados para activar el motor de detección
-- =====================================================

-- Usuario de prueba
DECLARE @TestUserId NVARCHAR(128) = 'test-user-001';
DECLARE @TestUserPrincipal NVARCHAR(256) = 'carlos.garcia@company.com';

-- Dispositivos habituales
DECLARE @NormalDevice NVARCHAR(256) = 'device-laptop-carlos-001';
DECLARE @AnomalousDevice NVARCHAR(256) = 'unknown-device-suspicious-999';

-- IPs habituales (España)
DECLARE @NormalIP1 NVARCHAR(64) = '83.45.123.100'; -- Madrid
DECLARE @NormalIP2 NVARCHAR(64) = '85.88.45.200';  -- Barcelona
DECLARE @AnomalousIP NVARCHAR(64) = '203.0.113.45'; -- China

-- Fechas
DECLARE @BaseDate DATETIME2 = DATEADD(DAY, -35, GETUTCDATE()); -- Hace 35 días
DECLARE @Today DATETIME2 = GETUTCDATE();

PRINT '=====================================================';
PRINT 'CREANDO DATOS DE PRUEBA PARA ALERTAS';
PRINT '=====================================================';
PRINT '';

-- =====================================================
-- PASO 1: CREAR PERFIL DE USUARIO NORMAL (30 días)
-- Comportamiento habitual para construir baseline
-- =====================================================
PRINT '1. Insertando eventos de BASELINE (comportamiento normal)...';

-- Semana 1: Madrid, horario laboral (9:00-18:00), mismo dispositivo
DECLARE @Counter INT = 0;
WHILE @Counter < 20
BEGIN
    INSERT INTO dbo.access_Events (
        Id, EventId, UserId, UserPrincipalName, TimestampUtc,
        IpAddress, Country, City, DeviceId, DeviceName,
        ClientApp, AuthMethod, Result, RiskLevel,
        CreatedAt, IsAnalyzed
    )
    VALUES (
        NEWID(),
        'baseline-event-' + CAST(@Counter AS NVARCHAR(10)),
        @TestUserId,
        @TestUserPrincipal,
        DATEADD(DAY, @Counter, DATEADD(HOUR, 9 + (@Counter % 8), @BaseDate)), -- 9:00-17:00
        @NormalIP1,
        'España',
        'Madrid',
        @NormalDevice,
        'Laptop Carlos',
        'Microsoft Office',
        'authenticatorApp',
        'CORRECTO',
        'none',
        DATEADD(DAY, @Counter, DATEADD(HOUR, 9 + (@Counter % 8), @BaseDate)),
        0 -- Pendiente de análisis
    );
    
    SET @Counter = @Counter + 1;
END

PRINT '   ? 20 eventos normales insertados (Madrid, 9:00-17:00, mismo dispositivo)';

-- Semana 2-3: Algunos desde Barcelona (comportamiento habitual)
SET @Counter = 0;
WHILE @Counter < 15
BEGIN
    INSERT INTO dbo.access_Events (
        Id, EventId, UserId, UserPrincipalName, TimestampUtc,
        IpAddress, Country, City, DeviceId, DeviceName,
        ClientApp, AuthMethod, Result, RiskLevel,
        CreatedAt, IsAnalyzed
    )
    VALUES (
        NEWID(),
        'baseline-bcn-' + CAST(@Counter AS NVARCHAR(10)),
        @TestUserId,
        @TestUserPrincipal,
        DATEADD(DAY, 20 + @Counter, DATEADD(HOUR, 10 + (@Counter % 7), @BaseDate)),
        @NormalIP2,
        'España',
        'Barcelona',
        @NormalDevice,
        'Laptop Carlos',
        'Microsoft Teams',
        'authenticatorApp',
        'CORRECTO',
        'none',
        DATEADD(DAY, 20 + @Counter, DATEADD(HOUR, 10 + (@Counter % 7), @BaseDate)),
        0
    );
    
    SET @Counter = @Counter + 1;
END

PRINT '   ? 15 eventos normales insertados (Barcelona, horario laboral)';
PRINT '';

-- =====================================================
-- PASO 2: CREAR EVENTOS ANÓMALOS (HOY)
-- Diseñados para disparar MÚLTIPLES señales
-- =====================================================
PRINT '2. Insertando eventos ANÓMALOS (dispararán alertas)...';
PRINT '';

-- =====================================================
-- ALERTA 1: RIESGO ALTO (Score esperado: ~75)
-- Señales: UnusualLocation + UnknownDevice + AtypicalTime + IpChange
-- =====================================================
PRINT '   [ALERTA 1] Creando escenario de ALTO RIESGO...';

-- Acceso desde China, dispositivo desconocido, horario nocturno (3:00 AM)
INSERT INTO dbo.access_Events (
    Id, EventId, UserId, UserPrincipalName, TimestampUtc,
    IpAddress, Country, City, DeviceId, DeviceName,
    ClientApp, AuthMethod, Result, RiskLevel,
    CreatedAt, IsAnalyzed
)
VALUES (
    NEWID(),
    'anomaly-high-risk-001',
    @TestUserId,
    @TestUserPrincipal,
    DATEADD(HOUR, 3, @Today), -- 3:00 AM UTC
    @AnomalousIP,
    'China',
    'Beijing',
    @AnomalousDevice,
    NULL,
    'Outlook Web App',
    'password', -- Método débil
    'CORRECTO',
    'high',
    DATEADD(HOUR, 3, @Today),
    0 -- Pendiente de análisis
);

PRINT '      ? Evento High Risk: China + dispositivo desconocido + 3:00 AM + password';
PRINT '      ? Señales esperadas: UnusualLocation, UnknownDevice, AtypicalTime, IpChange, WeakAuth';
PRINT '      ? Risk Score esperado: ~72-80';
PRINT '';

-- =====================================================
-- ALERTA 2: RIESGO MEDIO (Score esperado: ~45)
-- Señales: UnknownDevice + AtypicalTime
-- =====================================================
PRINT '   [ALERTA 2] Creando escenario de RIESGO MEDIO...';

-- Acceso desde Madrid (habitual) pero dispositivo nuevo y horario nocturno
INSERT INTO dbo.access_Events (
    Id, EventId, UserId, UserPrincipalName, TimestampUtc,
    IpAddress, Country, City, DeviceId, DeviceName,
    ClientApp, AuthMethod, Result, RiskLevel,
    CreatedAt, IsAnalyzed
)
VALUES (
    NEWID(),
    'anomaly-medium-risk-001',
    @TestUserId,
    @TestUserPrincipal,
    DATEADD(HOUR, 22, @Today), -- 22:00 (10 PM)
    @NormalIP1,
    'España',
    'Madrid',
    'device-mobile-new-phone',
    'iPhone 15',
    'Microsoft Teams',
    'authenticatorApp',
    'CORRECTO',
    'medium',
    DATEADD(HOUR, 22, @Today),
    0
);

PRINT '      ? Evento Medium Risk: Madrid (OK) + dispositivo nuevo + 22:00';
PRINT '      ? Señales esperadas: UnknownDevice, AtypicalTime';
PRINT '      ? Risk Score esperado: ~40-50';
PRINT '';

-- =====================================================
-- ALERTA 3: RIESGO ALTO - INTENTOS FALLIDOS + ÉXITO
-- Señales: FailedAttempts + UnusualLocation + WeakAuth
-- =====================================================
PRINT '   [ALERTA 3] Creando escenario de BRUTE FORCE + ÉXITO...';

-- 3 intentos fallidos desde Rusia
DECLARE @FailCounter INT = 0;
WHILE @FailCounter < 3
BEGIN
    INSERT INTO dbo.access_Events (
        Id, EventId, UserId, UserPrincipalName, TimestampUtc,
        IpAddress, Country, City, DeviceId, DeviceName,
        ClientApp, AuthMethod, Result, RiskLevel,
        CreatedAt, IsAnalyzed
    )
    VALUES (
        NEWID(),
        'anomaly-failed-' + CAST(@FailCounter AS NVARCHAR(10)),
        @TestUserId,
        @TestUserPrincipal,
        DATEADD(MINUTE, @FailCounter * 2, DATEADD(HOUR, 5, @Today)), -- Cada 2 minutos
        '185.143.223.100', -- IP Rusia
        'Rusia',
        'Moscú',
        @AnomalousDevice,
        NULL,
        'Outlook Web App',
        'password',
        'ERROR', -- Intento fallido
        'none',
        DATEADD(MINUTE, @FailCounter * 2, DATEADD(HOUR, 5, @Today)),
        0
    );
    
    SET @FailCounter = @FailCounter + 1;
END

-- Luego, éxito (indica posible compromiso)
INSERT INTO dbo.access_Events (
    Id, EventId, UserId, UserPrincipalName, TimestampUtc,
    IpAddress, Country, City, DeviceId, DeviceName,
    ClientApp, AuthMethod, Result, RiskLevel,
    CreatedAt, IsAnalyzed
)
VALUES (
    NEWID(),
    'anomaly-success-after-fails',
    @TestUserId,
    @TestUserPrincipal,
    DATEADD(MINUTE, 10, DATEADD(HOUR, 5, @Today)), -- 10 minutos después del último fallo
    '185.143.223.100',
    'Rusia',
    'Moscú',
    @AnomalousDevice,
    NULL,
    'Outlook Web App',
    'password',
    'CORRECTO', -- Éxito después de fallos
    'high',
    DATEADD(MINUTE, 10, DATEADD(HOUR, 5, @Today)),
    0
);

PRINT '      ? Evento High Risk: 3 fallos + éxito desde Rusia';
PRINT '      ? Señales esperadas: FailedAttempts, UnusualLocation, UnknownDevice, WeakAuth';
PRINT '      ? Risk Score esperado: ~70-85';
PRINT '';

-- =====================================================
-- ALERTA 4: RIESGO MEDIO - CAMBIO RÁPIDO DE UBICACIÓN
-- Señales: UnusualLocation + IpChange (viaje imposible)
-- =====================================================
PRINT '   [ALERTA 4] Creando escenario de VIAJE IMPOSIBLE...';

-- Acceso desde Madrid
INSERT INTO dbo.access_Events (
    Id, EventId, UserId, UserPrincipalName, TimestampUtc,
    IpAddress, Country, City, DeviceId, DeviceName,
    ClientApp, AuthMethod, Result, RiskLevel,
    CreatedAt, IsAnalyzed
)
VALUES (
    NEWID(),
    'anomaly-madrid-first',
    @TestUserId,
    @TestUserPrincipal,
    DATEADD(HOUR, 10, @Today), -- 10:00 AM
    @NormalIP1,
    'España',
    'Madrid',
    @NormalDevice,
    'Laptop Carlos',
    'Microsoft Teams',
    'authenticatorApp',
    'CORRECTO',
    'none',
    DATEADD(HOUR, 10, @Today),
    0
);

-- 30 minutos después: Acceso desde Japón (imposible físicamente)
INSERT INTO dbo.access_Events (
    Id, EventId, UserId, UserPrincipalName, TimestampUtc,
    IpAddress, Country, City, DeviceId, DeviceName,
    ClientApp, AuthMethod, Result, RiskLevel,
    CreatedAt, IsAnalyzed
)
VALUES (
    NEWID(),
    'anomaly-japan-impossible',
    @TestUserId,
    @TestUserPrincipal,
    DATEADD(MINUTE, 30, DATEADD(HOUR, 10, @Today)), -- 10:30 AM (30 min después)
    '210.165.12.50', -- IP Japón
    'Japón',
    'Tokio',
    @NormalDevice, -- Mismo dispositivo (aún más sospechoso)
    'Laptop Carlos',
    'Outlook Web App',
    'authenticatorApp',
    'CORRECTO',
    'medium',
    DATEADD(MINUTE, 30, DATEADD(HOUR, 10, @Today)),
    0
);

PRINT '      ? Evento Medium Risk: Madrid ? Tokio en 30 minutos';
PRINT '      ? Señales esperadas: UnusualLocation, IpChange';
PRINT '      ? Risk Score esperado: ~35-45';
PRINT '';

-- =====================================================
-- ALERTA 5: RIESGO BAJO (Score esperado: ~32)
-- Señales: AtypicalTime (solo una señal leve)
-- =====================================================
PRINT '   [ALERTA 5] Creando escenario de RIESGO BAJO...';

-- Acceso desde ubicación habitual pero horario domingo muy temprano
INSERT INTO dbo.access_Events (
    Id, EventId, UserId, UserPrincipalName, TimestampUtc,
    IpAddress, Country, City, DeviceId, DeviceName,
    ClientApp, AuthMethod, Result, RiskLevel,
    CreatedAt, IsAnalyzed
)
VALUES (
    NEWID(),
    'anomaly-low-risk-001',
    @TestUserId,
    @TestUserPrincipal,
    DATEADD(HOUR, 6, @Today), -- 6:00 AM domingo
    @NormalIP1,
    'España',
    'Madrid',
    @NormalDevice,
    'Laptop Carlos',
    'Microsoft Office',
    'authenticatorApp',
    'CORRECTO',
    'low',
    DATEADD(HOUR, 6, @Today),
    0
);

PRINT '      ? Evento Low Risk: Madrid (OK) + 6:00 AM domingo';
PRINT '      ? Señales esperadas: AtypicalTime (leve)';
PRINT '      ? Risk Score esperado: ~30-35';
PRINT '';

-- =====================================================
-- RESUMEN
-- =====================================================
PRINT '=====================================================';
PRINT 'DATOS DE PRUEBA INSERTADOS CORRECTAMENTE';
PRINT '=====================================================';
PRINT '';
PRINT 'Eventos creados:';
PRINT '  - Baseline (normal): 35 eventos';
PRINT '  - Anómalos (alertas): 9 eventos';
PRINT '  - Total: 44 eventos';
PRINT '';
PRINT 'Alertas esperadas (cuando DetectionFunction ejecute):';
PRINT '  ?? HIGH (?70):   3 alertas';
PRINT '  ?? MEDIUM (40-69): 2 alertas';
PRINT '  ?? LOW (30-39):   1 alerta';
PRINT '  ? Total:         6 alertas';
PRINT '';
PRINT 'Próximos pasos:';
PRINT '  1. Esperar 1-2 minutos (DetectionFunction ejecuta cada minuto)';
PRINT '  2. Consultar: SELECT * FROM dbo.alerts WHERE IsSimulation = 0';
PRINT '  3. Verificar logs en Application Insights';
PRINT '  4. Abrir Dashboard UI para ver alertas';
PRINT '';
PRINT 'Consultas útiles:';
PRINT '  -- Ver eventos insertados:';
PRINT '  SELECT * FROM access_Events WHERE UserId = ''test-user-001'' ORDER BY CreatedAt DESC;';
PRINT '';
PRINT '  -- Ver alertas generadas:';
PRINT '  SELECT Severity, RiskScore, Title, Country, DetectedAt';
PRINT '  FROM alerts WHERE UserPrincipalName = ''carlos.garcia@company.com'' ORDER BY DetectedAt DESC;';
PRINT '';
PRINT '  -- Ver señales disparadas:';
PRINT '  SELECT DetectedSignals FROM alerts WHERE UserPrincipalName = ''carlos.garcia@company.com'';';
PRINT '=====================================================';
GO
