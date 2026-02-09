-- =====================================================
-- TABLA: name_Mappings
-- Propósito: Mapeo consistente de nombres reales a pseudónimos
-- =====================================================

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'name_Mappings')
BEGIN
    CREATE TABLE [dbo].[name_Mappings](
        [Id] [uniqueidentifier] NOT NULL DEFAULT NEWID(),
        [OriginalHash] [nvarchar](128) NOT NULL,  -- SHA256 del nombre original
        [PseudonymFirstName] [nvarchar](100) NOT NULL,
        [PseudonymLastName] [nvarchar](100) NOT NULL,
        [PseudonymFullName] [nvarchar](200) NOT NULL,
        [PseudonymEmail] [nvarchar](256) NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (sysutcdatetime()),
        
        CONSTRAINT [PK_name_Mappings] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [UQ_name_Mappings_OriginalHash] UNIQUE ([OriginalHash])
    );
    
    CREATE INDEX [IX_name_Mappings_OriginalHash] ON [dbo].[name_Mappings]([OriginalHash]);
    
    PRINT 'Tabla name_Mappings creada exitosamente.';
END
ELSE
BEGIN
    PRINT 'La tabla name_Mappings ya existe.';
END
GO
