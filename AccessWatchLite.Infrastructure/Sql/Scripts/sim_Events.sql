GO
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
    [CreatedAt] [datetime2](7) NOT NULL,
 CONSTRAINT [PK_sim_Events] PRIMARY KEY CLUSTERED
(
    [Id] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[sim_Events] ADD  DEFAULT (sysutcdatetime()) FOR [CreatedAt]
GO
