-- Tabla de metadatos para sim_Events (optimización de performance)
-- Almacena información agregada para evitar escanear toda la tabla

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'sim_Metadata' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    CREATE TABLE dbo.sim_Metadata
    (
        Id INT PRIMARY KEY IDENTITY(1,1),
        HasData BIT NOT NULL DEFAULT 0,
        MinDate DATETIME2 NULL,
        MaxDate DATETIME2 NULL,
        TotalEvents INT NOT NULL DEFAULT 0,
        UnanalyzedEvents INT NOT NULL DEFAULT 0,
        LastUpdatedUtc DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedBy NVARCHAR(100) NOT NULL DEFAULT 'System'
    );

    -- Insertar registro inicial
    INSERT INTO dbo.sim_Metadata (HasData, MinDate, MaxDate, TotalEvents, UnanalyzedEvents, LastUpdatedUtc, UpdatedBy)
    VALUES (0, NULL, NULL, 0, 0, GETUTCDATE(), 'InitialSetup');

    PRINT 'Tabla sim_Metadata creada exitosamente';
END
ELSE
BEGIN
    PRINT 'Tabla sim_Metadata ya existe';
END
GO
