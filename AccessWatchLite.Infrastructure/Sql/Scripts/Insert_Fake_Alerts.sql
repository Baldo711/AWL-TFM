-- =====================================================
-- SCRIPT: Insertar Alertas Falsas para Diseñar Dashboard
-- Propósito: Crear alertas de ejemplo de todos los tipos
-- =====================================================

DECLARE @TestUserId NVARCHAR(128) = 'test-user-001';
DECLARE @TestUserPrincipal NVARCHAR(256) = 'carlos.garcia@company.com';

-- Obtener el primer evento de prueba para referenciar
DECLARE @EventId UNIQUEIDENTIFIER = (
    SELECT TOP 1 Id 
    FROM dbo.access_Events 
    WHERE UserId = @TestUserId 
    ORDER BY CreatedAt DESC
);

PRINT '=====================================================';
PRINT 'INSERTANDO ALERTAS FALSAS PARA DASHBOARD';
PRINT '=====================================================';
PRINT '';

-- =====================================================
-- ALERTA 1: HIGH - Acceso desde China, múltiples señales
-- =====================================================
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    @TestUserId,
    @TestUserPrincipal,
    0, -- REAL
    'High',
    85.50,
    'New',
    'Acceso de alto riesgo detectado - carlos.garcia@company.com',
    'Se detectaron 5 señal(es) de riesgo: Ubicación inusual (China, país no habitual para este usuario); Dispositivo desconocido (unknown-device-suspicious-999); Horario atípico (03:45 AM, fuera del rango habitual 09:00-18:00)',
    '[{"Signal":"Ubicación inusual (China, país no habitual)","Score":1.0},{"Signal":"Dispositivo desconocido (unknown-device-suspicious-999)","Score":1.0},{"Signal":"Horario atípico (03:45, fuera de horario laboral)","Score":0.8},{"Signal":"IP no reconocida (203.0.113.45)","Score":0.8},{"Signal":"Método de autenticación débil (password)","Score":0.75}]',
    DATEADD(HOUR, -3, GETDATE()),
    '203.0.113.45',
    'China',
    'Beijing',
    'unknown-device-suspicious-999',
    DATEADD(HOUR, -3, GETDATE())
);

PRINT '? Alerta 1 (HIGH): Acceso desde China - Score 85.50';

-- =====================================================
-- ALERTA 2: HIGH - Brute Force desde Rusia
-- =====================================================
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    @TestUserId,
    @TestUserPrincipal,
    0,
    'High',
    78.25,
    'Investigating',
    'Acceso de alto riesgo detectado - carlos.garcia@company.com',
    'Se detectaron 5 señal(es) de riesgo: Múltiples intentos fallidos previos (3 fallos en 10 minutos); Ubicación inusual (Rusia, país no habitual); Dispositivo desconocido',
    '[{"Signal":"Intentos de acceso fallidos previos (3 en 10 min)","Score":1.0},{"Signal":"Ubicación inusual (Rusia, país no habitual)","Score":1.0},{"Signal":"Dispositivo desconocido","Score":1.0},{"Signal":"Método de autenticación débil (password)","Score":0.75},{"Signal":"Horario atípico (05:10 AM)","Score":0.6}]',
    DATEADD(HOUR, -5, GETDATE()),
    '185.143.223.100',
    'Rusia',
    'Moscú',
    'unknown-device-hacker',
    DATEADD(HOUR, -5, GETDATE())
);

PRINT '? Alerta 2 (HIGH): Brute force desde Rusia - Score 78.25 - Estado: Investigating';

-- =====================================================
-- ALERTA 3: HIGH - Múltiples ubicaciones simultáneas
-- =====================================================
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    @TestUserId,
    @TestUserPrincipal,
    0,
    'High',
    72.00,
    'Resolved',
    'Acceso de alto riesgo detectado - carlos.garcia@company.com',
    'Se detectaron 3 señal(es) de riesgo: Viaje imposible detectado (Madrid ? Tokio en 30 minutos); Ubicación inusual (Japón)',
    '[{"Signal":"Ubicación inusual (Japón, país no habitual)","Score":1.0},{"Signal":"Cambio de IP sospechoso","Score":0.85},{"Signal":"Viaje geográficamente imposible (30 min entre ubicaciones)","Score":1.0}]',
    DATEADD(HOUR, -10, GETDATE()),
    '210.165.12.50',
    'Japón',
    'Tokio',
    'device-laptop-carlos-001',
    DATEADD(HOUR, -10, GETDATE())
);

PRINT '? Alerta 3 (HIGH): Viaje imposible - Score 72.00 - Estado: Resolved';

-- =====================================================
-- ALERTA 4: MEDIUM - Nuevo dispositivo + horario nocturno
-- =====================================================
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    @TestUserId,
    @TestUserPrincipal,
    0,
    'Medium',
    48.50,
    'New',
    'Acceso sospechoso detectado - carlos.garcia@company.com',
    'Se detectaron 2 señal(es) de riesgo: Dispositivo desconocido (device-mobile-new-phone); Horario atípico (22:00, fuera del horario habitual)',
    '[{"Signal":"Dispositivo desconocido (device-mobile-new-phone)","Score":1.0},{"Signal":"Horario atípico (22:00, fuera de 09:00-18:00)","Score":0.55}]',
    DATEADD(HOUR, -22, GETDATE()),
    '83.45.123.100',
    'España',
    'Madrid',
    'device-mobile-new-phone',
    DATEADD(HOUR, -22, GETDATE())
);

PRINT '? Alerta 4 (MEDIUM): Nuevo dispositivo nocturno - Score 48.50';

-- =====================================================
-- ALERTA 5: MEDIUM - Acceso desde ubicación inusual
-- =====================================================
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    @TestUserId,
    @TestUserPrincipal,
    0,
    'Medium',
    42.75,
    'FalsePositive',
    'Acceso sospechoso detectado - carlos.garcia@company.com',
    'Se detectaron 2 señal(es) de riesgo: Ubicación inusual (Portugal, país no habitual); IP no reconocida',
    '[{"Signal":"Ubicación inusual (Portugal, país fronterizo)","Score":0.7},{"Signal":"IP no reconocida (cambio de red)","Score":0.65}]',
    DATEADD(HOUR, -15, GETDATE()),
    '94.60.12.45',
    'Portugal',
    'Lisboa',
    'device-laptop-carlos-001',
    DATEADD(HOUR, -15, GETDATE())
);

PRINT '? Alerta 5 (MEDIUM): Acceso desde Portugal - Score 42.75 - Estado: FalsePositive';

-- =====================================================
-- ALERTA 6: MEDIUM - Actividad inusual fin de semana
-- =====================================================
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    @TestUserId,
    @TestUserPrincipal,
    0,
    'Medium',
    40.00,
    'Investigating',
    'Acceso sospechoso detectado - carlos.garcia@company.com',
    'Se detectaron 2 señal(es) de riesgo: Horario atípico (domingo 02:30 AM); Aplicación cliente no habitual',
    '[{"Signal":"Horario atípico (domingo 02:30 AM)","Score":0.8},{"Signal":"Aplicación no usual (aplicación rara)","Score":0.5}]',
    DATEADD(HOUR, -48, GETDATE()),
    '83.45.123.100',
    'España',
    'Madrid',
    'device-laptop-carlos-001',
    DATEADD(HOUR, -48, GETDATE())
);

PRINT '? Alerta 6 (MEDIUM): Actividad fin de semana - Score 40.00 - Estado: Investigating';

-- =====================================================
-- ALERTA 7: LOW - Horario ligeramente atípico
-- =====================================================
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    @TestUserId,
    @TestUserPrincipal,
    0,
    'Low',
    35.50,
    'New',
    'Acceso con anomalías detectado - carlos.garcia@company.com',
    'Se detectaron 1 señal(es) de riesgo: Horario ligeramente atípico (06:15 AM, antes del horario habitual)',
    '[{"Signal":"Horario atípico (06:15 AM, inicio temprano)","Score":0.45}]',
    DATEADD(HOUR, -6, GETDATE()),
    '83.45.123.100',
    'España',
    'Madrid',
    'device-laptop-carlos-001',
    DATEADD(HOUR, -6, GETDATE())
);

PRINT '? Alerta 7 (LOW): Horario temprano - Score 35.50';

-- =====================================================
-- ALERTA 8: LOW - Cambio menor de IP (misma red)
-- =====================================================
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    @TestUserId,
    @TestUserPrincipal,
    0,
    'Low',
    32.00,
    'Resolved',
    'Acceso con anomalías detectado - carlos.garcia@company.com',
    'Se detectaron 1 señal(es) de riesgo: IP no reconocida (cambio menor dentro de misma red corporativa)',
    '[{"Signal":"IP no reconocida (83.45.123.105, variación en subnet)","Score":0.35}]',
    DATEADD(HOUR, -12, GETDATE()),
    '83.45.123.105',
    'España',
    'Madrid',
    'device-laptop-carlos-001',
    DATEADD(HOUR, -12, GETDATE())
);

PRINT '? Alerta 8 (LOW): Cambio de IP menor - Score 32.00 - Estado: Resolved';

-- =====================================================
-- ALERTAS ADICIONALES: Otros usuarios (variedad)
-- =====================================================

-- ALERTA 9: HIGH - Usuario diferente
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    'user-002',
    'maria.lopez@company.com',
    0,
    'High',
    81.00,
    'New',
    'Acceso de alto riesgo detectado - maria.lopez@company.com',
    'Se detectaron 4 señal(es) de riesgo: Ubicación inusual (Nigeria); Dispositivo desconocido; Método débil; Horario atípico',
    '[{"Signal":"Ubicación inusual (Nigeria, país no habitual)","Score":1.0},{"Signal":"Dispositivo desconocido","Score":1.0},{"Signal":"Método débil (password)","Score":0.8},{"Signal":"Horario atípico (04:00 AM)","Score":0.7}]',
    DATEADD(HOUR, -8, GETDATE()),
    '197.210.55.30',
    'Nigeria',
    'Lagos',
    'unknown-device-xyz',
    DATEADD(HOUR, -8, GETDATE())
);

PRINT '? Alerta 9 (HIGH): Usuario maria.lopez - Nigeria - Score 81.00';

-- ALERTA 10: MEDIUM - Usuario diferente
INSERT INTO dbo.alerts (
    Id,
    EventId,
    UserId,
    UserPrincipalName,
    IsSimulation,
    Severity,
    RiskScore,
    Status,
    Title,
    Description,
    DetectedSignals,
    EventTimestamp,
    IpAddress,
    Country,
    City,
    DeviceId,
    DetectedAt
)
VALUES (
    NEWID(),
    @EventId,
    'user-003',
    'juan.perez@company.com',
    0,
    'Medium',
    45.00,
    'New',
    'Acceso sospechoso detectado - juan.perez@company.com',
    'Se detectaron 2 señal(es) de riesgo: Ciudad inusual (Valencia, usuario suele estar en Sevilla); Horario atípico',
    '[{"Signal":"Ciudad inusual (Valencia, cambio de ubicación)","Score":0.6},{"Signal":"Horario atípico (21:00)","Score":0.4}]',
    DATEADD(HOUR, -18, GETDATE()),
    '88.26.45.200',
    'España',
    'Valencia',
    'device-laptop-juan',
    DATEADD(HOUR, -18, GETDATE())
);

PRINT '? Alerta 10 (MEDIUM): Usuario juan.perez - Valencia - Score 45.00';

PRINT '';
PRINT '=====================================================';
PRINT 'ALERTAS FALSAS INSERTADAS CORRECTAMENTE';
PRINT '=====================================================';
PRINT '';
PRINT 'Total de alertas creadas: 10';
PRINT '';
PRINT 'Distribución por severidad:';
PRINT '  ?? HIGH:   4 alertas (scores: 85.50, 81.00, 78.25, 72.00)';
PRINT '  ?? MEDIUM: 4 alertas (scores: 48.50, 45.00, 42.75, 40.00)';
PRINT '  ?? LOW:    2 alertas (scores: 35.50, 32.00)';
PRINT '';
PRINT 'Distribución por estado:';
PRINT '  ?? New:           5 alertas';
PRINT '  ?? Investigating: 3 alertas';
PRINT '  ? Resolved:      2 alertas';
PRINT '  ? FalsePositive: 1 alerta';
PRINT '';
PRINT 'Usuarios afectados:';
PRINT '  - carlos.garcia@company.com (8 alertas)';
PRINT '  - maria.lopez@company.com (1 alerta)';
PRINT '  - juan.perez@company.com (1 alerta)';
PRINT '';
PRINT 'Países de origen:';
PRINT '  - China, Rusia, Japón, Nigeria (HIGH risk)';
PRINT '  - España, Portugal (MEDIUM/LOW risk)';
PRINT '';
PRINT 'Próximos pasos:';
PRINT '  1. Consultar: SELECT * FROM dbo.alerts ORDER BY DetectedAt DESC;';
PRINT '  2. Abrir Dashboard UI y verificar visualización';
PRINT '  3. Diseñar página de gestión de alertas (/alerts)';
PRINT '  4. Implementar filtros por severidad, estado, fecha';
PRINT '  5. Agregar acciones: Investigar, Resolver, Marcar como FP';
PRINT '';
PRINT '=====================================================';
GO
