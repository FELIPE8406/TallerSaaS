IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AspNetRoles] (
    [Id] nvarchar(450) NOT NULL,
    [Name] nvarchar(256) NULL,
    [NormalizedName] nvarchar(256) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetUsers] (
    [Id] nvarchar(450) NOT NULL,
    [TenantId] uniqueidentifier NULL,
    [NombreCompleto] nvarchar(max) NULL,
    [EsSuperAdmin] bit NOT NULL,
    [FechaRegistro] datetime2 NOT NULL,
    [Activo] bit NOT NULL,
    [UserName] nvarchar(256) NULL,
    [NormalizedUserName] nvarchar(256) NULL,
    [Email] nvarchar(256) NULL,
    [NormalizedEmail] nvarchar(256) NULL,
    [EmailConfirmed] bit NOT NULL,
    [PasswordHash] nvarchar(max) NULL,
    [SecurityStamp] nvarchar(max) NULL,
    [ConcurrencyStamp] nvarchar(max) NULL,
    [PhoneNumber] nvarchar(max) NULL,
    [PhoneNumberConfirmed] bit NOT NULL,
    [TwoFactorEnabled] bit NOT NULL,
    [LockoutEnd] datetimeoffset NULL,
    [LockoutEnabled] bit NOT NULL,
    [AccessFailedCount] int NOT NULL,
    CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Clientes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [NombreCompleto] nvarchar(300) NOT NULL,
    [Email] nvarchar(max) NULL,
    [Telefono] nvarchar(max) NULL,
    [Direccion] nvarchar(max) NULL,
    [Cedula] nvarchar(max) NULL,
    [FechaRegistro] datetime2 NOT NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [Inventario] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Nombre] nvarchar(max) NOT NULL,
    [SKU] nvarchar(max) NULL,
    [Descripcion] nvarchar(max) NULL,
    [Categoria] nvarchar(max) NULL,
    [Stock] int NOT NULL,
    [StockMinimo] int NOT NULL,
    [PrecioCompra] decimal(12,2) NOT NULL,
    [PrecioVenta] decimal(12,2) NOT NULL,
    [Proveedor] nvarchar(max) NULL,
    [FechaActualizacion] datetime2 NOT NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_Inventario] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [PlanesSuscripcion] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    [LimiteUsuarios] int NOT NULL,
    [Precio] decimal(10,2) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    [Activo] bit NOT NULL,
    CONSTRAINT [PK_PlanesSuscripcion] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [AspNetRoleClaims] (
    [Id] int NOT NULL IDENTITY,
    [RoleId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserClaims] (
    [Id] int NOT NULL IDENTITY,
    [UserId] nvarchar(450) NOT NULL,
    [ClaimType] nvarchar(max) NULL,
    [ClaimValue] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserLogins] (
    [LoginProvider] nvarchar(450) NOT NULL,
    [ProviderKey] nvarchar(450) NOT NULL,
    [ProviderDisplayName] nvarchar(max) NULL,
    [UserId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey]),
    CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserRoles] (
    [UserId] nvarchar(450) NOT NULL,
    [RoleId] nvarchar(450) NOT NULL,
    CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [AspNetUserTokens] (
    [UserId] nvarchar(450) NOT NULL,
    [LoginProvider] nvarchar(450) NOT NULL,
    [Name] nvarchar(450) NOT NULL,
    [Value] nvarchar(max) NULL,
    CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
    CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Vehiculos] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ClienteId] uniqueidentifier NOT NULL,
    [Marca] nvarchar(max) NOT NULL,
    [Modelo] nvarchar(max) NOT NULL,
    [Anio] int NOT NULL,
    [Placa] nvarchar(max) NULL,
    [VIN] nvarchar(max) NULL,
    [Color] nvarchar(max) NULL,
    [Kilometraje] nvarchar(max) NULL,
    [FechaRegistro] datetime2 NOT NULL,
    CONSTRAINT [PK_Vehiculos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Vehiculos_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Tenants] (
    [Id] uniqueidentifier NOT NULL,
    [Nombre] nvarchar(200) NOT NULL,
    [RFC] nvarchar(max) NULL,
    [Logo] nvarchar(max) NULL,
    [Telefono] nvarchar(max) NULL,
    [Direccion] nvarchar(max) NULL,
    [Email] nvarchar(max) NULL,
    [FechaAlta] datetime2 NOT NULL,
    [Activo] bit NOT NULL,
    [PlanSuscripcionId] int NULL,
    CONSTRAINT [PK_Tenants] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Tenants_PlanesSuscripcion_PlanSuscripcionId] FOREIGN KEY ([PlanSuscripcionId]) REFERENCES [PlanesSuscripcion] ([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE [Ordenes] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [VehiculoId] uniqueidentifier NOT NULL,
    [NumeroOrden] nvarchar(max) NOT NULL,
    [Estado] int NOT NULL,
    [FechaEntrada] datetime2 NOT NULL,
    [FechaSalida] datetime2 NULL,
    [DiagnosticoInicial] nvarchar(max) NULL,
    [TrabajoRealizado] nvarchar(max) NULL,
    [Observaciones] nvarchar(max) NULL,
    [Subtotal] decimal(12,2) NOT NULL,
    [Descuento] decimal(12,2) NOT NULL,
    [IVA] decimal(12,2) NOT NULL,
    [Total] decimal(12,2) NOT NULL,
    [Pagada] bit NOT NULL,
    CONSTRAINT [PK_Ordenes] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Ordenes_Vehiculos_VehiculoId] FOREIGN KEY ([VehiculoId]) REFERENCES [Vehiculos] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [Pagos] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Monto] decimal(10,2) NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [Estado] nvarchar(max) NOT NULL,
    [Referencia] nvarchar(max) NULL,
    [Concepto] nvarchar(max) NULL,
    [PlanSuscripcionId] int NULL,
    CONSTRAINT [PK_Pagos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Pagos_PlanesSuscripcion_PlanSuscripcionId] FOREIGN KEY ([PlanSuscripcionId]) REFERENCES [PlanesSuscripcion] ([Id]),
    CONSTRAINT [FK_Pagos_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [ItemsOrden] (
    [Id] uniqueidentifier NOT NULL,
    [OrdenId] uniqueidentifier NOT NULL,
    [Descripcion] nvarchar(max) NOT NULL,
    [Tipo] nvarchar(max) NOT NULL,
    [Cantidad] decimal(10,2) NOT NULL,
    [PrecioUnitario] decimal(12,2) NOT NULL,
    [ProductoInventarioId] uniqueidentifier NULL,
    CONSTRAINT [PK_ItemsOrden] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_ItemsOrden_Inventario_ProductoInventarioId] FOREIGN KEY ([ProductoInventarioId]) REFERENCES [Inventario] ([Id]),
    CONSTRAINT [FK_ItemsOrden_Ordenes_OrdenId] FOREIGN KEY ([OrdenId]) REFERENCES [Ordenes] ([Id]) ON DELETE CASCADE
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Descripcion', N'LimiteUsuarios', N'Nombre', N'Precio') AND [object_id] = OBJECT_ID(N'[PlanesSuscripcion]'))
    SET IDENTITY_INSERT [PlanesSuscripcion] ON;
INSERT INTO [PlanesSuscripcion] ([Id], [Activo], [Descripcion], [LimiteUsuarios], [Nombre], [Precio])
VALUES (1, CAST(1 AS bit), N'Hasta 3 usuarios, módulos esenciales', 3, N'Básico', 299.0),
(2, CAST(1 AS bit), N'Hasta 10 usuarios, reportes PDF/Excel', 10, N'Profesional', 699.0),
(3, CAST(1 AS bit), N'Usuarios ilimitados, soporte prioritario', 50, N'Empresarial', 1499.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Activo', N'Descripcion', N'LimiteUsuarios', N'Nombre', N'Precio') AND [object_id] = OBJECT_ID(N'[PlanesSuscripcion]'))
    SET IDENTITY_INSERT [PlanesSuscripcion] OFF;
GO

CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
GO

CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL;
GO

CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
GO

CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
GO

CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
GO

CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL;
GO

CREATE INDEX [IX_Clientes_TenantId] ON [Clientes] ([TenantId]);
GO

CREATE INDEX [IX_Inventario_TenantId] ON [Inventario] ([TenantId]);
GO

CREATE INDEX [IX_ItemsOrden_OrdenId] ON [ItemsOrden] ([OrdenId]);
GO

CREATE INDEX [IX_ItemsOrden_ProductoInventarioId] ON [ItemsOrden] ([ProductoInventarioId]);
GO

CREATE INDEX [IX_Ordenes_TenantId] ON [Ordenes] ([TenantId]);
GO

CREATE INDEX [IX_Ordenes_VehiculoId] ON [Ordenes] ([VehiculoId]);
GO

CREATE INDEX [IX_Pagos_PlanSuscripcionId] ON [Pagos] ([PlanSuscripcionId]);
GO

CREATE INDEX [IX_Pagos_TenantId] ON [Pagos] ([TenantId]);
GO

CREATE INDEX [IX_Tenants_PlanSuscripcionId] ON [Tenants] ([PlanSuscripcionId]);
GO

CREATE INDEX [IX_Vehiculos_ClienteId] ON [Vehiculos] ([ClienteId]);
GO

CREATE INDEX [IX_Vehiculos_TenantId] ON [Vehiculos] ([TenantId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260220214217_ColombianRefactor', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260223131453_FinalSaaSStructure', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Ordenes] ADD [Bloqueada] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Ordenes] ADD [FacturaId] uniqueidentifier NULL;
GO

CREATE TABLE [EventosTrazabilidad] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [VehiculoId] uniqueidentifier NOT NULL,
    [Tipo] int NOT NULL,
    [Descripcion] nvarchar(500) NOT NULL,
    [ReferenciaId] uniqueidentifier NOT NULL,
    [FechaEvento] datetime2 NOT NULL,
    CONSTRAINT [PK_EventosTrazabilidad] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EventosTrazabilidad_Vehiculos_VehiculoId] FOREIGN KEY ([VehiculoId]) REFERENCES [Vehiculos] ([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE [Facturas] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [NumeroFactura] nvarchar(20) NOT NULL,
    [FechaEmision] datetime2 NOT NULL,
    [Subtotal] decimal(12,2) NOT NULL,
    [Descuento] decimal(12,2) NOT NULL,
    [IVA] decimal(12,2) NOT NULL,
    [Total] decimal(12,2) NOT NULL,
    [CodigoQR] nvarchar(max) NULL,
    [FirmadaDigitalmente] bit NOT NULL,
    [Observaciones] nvarchar(max) NULL,
    CONSTRAINT [PK_Facturas] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_Ordenes_FacturaId] ON [Ordenes] ([FacturaId]);
GO

CREATE INDEX [IX_EventosTrazabilidad_TenantId] ON [EventosTrazabilidad] ([TenantId]);
GO

CREATE INDEX [IX_EventosTrazabilidad_VehiculoId] ON [EventosTrazabilidad] ([VehiculoId]);
GO

CREATE INDEX [IX_Facturas_TenantId] ON [Facturas] ([TenantId]);
GO

ALTER TABLE [Ordenes] ADD CONSTRAINT [FK_Ordenes_Facturas_FacturaId] FOREIGN KEY ([FacturaId]) REFERENCES [Facturas] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260302231129_AddFacturasYTrazabilidad', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var0 sysname;
SELECT @var0 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Facturas]') AND [c].[name] = N'CodigoQR');
IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Facturas] DROP CONSTRAINT [' + @var0 + '];');
ALTER TABLE [Facturas] DROP COLUMN [CodigoQR];
GO

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Facturas]') AND [c].[name] = N'FirmadaDigitalmente');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Facturas] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [Facturas] DROP COLUMN [FirmadaDigitalmente];
GO

ALTER TABLE [Inventario] ADD [BodegaId] uniqueidentifier NULL;
GO

CREATE TABLE [Bodegas] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Nombre] nvarchar(200) NOT NULL,
    [Descripcion] nvarchar(max) NULL,
    [Ubicacion] nvarchar(max) NULL,
    [Activo] bit NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_Bodegas] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [MovimientosInventario] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ProductoId] uniqueidentifier NOT NULL,
    [BodegaOrigenId] uniqueidentifier NULL,
    [BodegaDestinoId] uniqueidentifier NULL,
    [Tipo] nvarchar(30) NOT NULL,
    [Cantidad] int NOT NULL,
    [Referencia] nvarchar(max) NULL,
    [Observaciones] nvarchar(max) NULL,
    [Fecha] datetime2 NOT NULL,
    [BodegaId] uniqueidentifier NULL,
    CONSTRAINT [PK_MovimientosInventario] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MovimientosInventario_Bodegas_BodegaDestinoId] FOREIGN KEY ([BodegaDestinoId]) REFERENCES [Bodegas] ([Id]),
    CONSTRAINT [FK_MovimientosInventario_Bodegas_BodegaId] FOREIGN KEY ([BodegaId]) REFERENCES [Bodegas] ([Id]),
    CONSTRAINT [FK_MovimientosInventario_Bodegas_BodegaOrigenId] FOREIGN KEY ([BodegaOrigenId]) REFERENCES [Bodegas] ([Id]),
    CONSTRAINT [FK_MovimientosInventario_Inventario_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Inventario] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_Inventario_BodegaId] ON [Inventario] ([BodegaId]);
GO

CREATE INDEX [IX_Bodegas_TenantId] ON [Bodegas] ([TenantId]);
GO

CREATE INDEX [IX_Movimientos_ProductoId] ON [MovimientosInventario] ([ProductoId]);
GO

CREATE INDEX [IX_Movimientos_TenantId] ON [MovimientosInventario] ([TenantId]);
GO

CREATE INDEX [IX_MovimientosInventario_BodegaDestinoId] ON [MovimientosInventario] ([BodegaDestinoId]);
GO

CREATE INDEX [IX_MovimientosInventario_BodegaId] ON [MovimientosInventario] ([BodegaId]);
GO

CREATE INDEX [IX_MovimientosInventario_BodegaOrigenId] ON [MovimientosInventario] ([BodegaOrigenId]);
GO

ALTER TABLE [Inventario] ADD CONSTRAINT [FK_Inventario_Bodegas_BodegaId] FOREIGN KEY ([BodegaId]) REFERENCES [Bodegas] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260306175401_RefactorizacionCompleta_v2', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Inventario] ADD [TipoItem] int NOT NULL DEFAULT 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260306225537_ConsolidacionFinal', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inventario]') AND [c].[name] = N'SKU');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Inventario] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [Inventario] ALTER COLUMN [SKU] nvarchar(100) NULL;
GO

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inventario]') AND [c].[name] = N'Proveedor');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Inventario] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [Inventario] ALTER COLUMN [Proveedor] nvarchar(300) NULL;
GO

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inventario]') AND [c].[name] = N'Nombre');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Inventario] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Inventario] ALTER COLUMN [Nombre] nvarchar(300) NOT NULL;
GO

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Inventario]') AND [c].[name] = N'Categoria');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Inventario] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Inventario] ALTER COLUMN [Categoria] nvarchar(100) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260309233101_Fix_Inventario_MaxLength', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Facturas] ADD [EstadoEnvio] int NOT NULL DEFAULT 0;
GO

ALTER TABLE [Facturas] ADD [TipoFacturacion] int NOT NULL DEFAULT 0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310013514_AddTipoFacturacionToFactura', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_Inventario_TenantId] ON [Inventario];
GO

CREATE INDEX [IX_Inventario_SKU] ON [Inventario] ([SKU]);
GO

CREATE INDEX [IX_Inventario_TenantId_Activo] ON [Inventario] ([TenantId], [Activo]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260310170156_IX_Inventario_Performance_v2', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [PlanesSuscripcion] ADD [Beneficios] nvarchar(max) NULL;
GO

ALTER TABLE [PlanesSuscripcion] ADD [ColorHex] nvarchar(max) NULL;
GO

UPDATE [PlanesSuscripcion] SET [Beneficios] = NULL, [ColorHex] = NULL
WHERE [Id] = 1;
SELECT @@ROWCOUNT;

GO

UPDATE [PlanesSuscripcion] SET [Beneficios] = NULL, [ColorHex] = NULL
WHERE [Id] = 2;
SELECT @@ROWCOUNT;

GO

UPDATE [PlanesSuscripcion] SET [Beneficios] = NULL, [ColorHex] = NULL
WHERE [Id] = 3;
SELECT @@ROWCOUNT;

GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260312021019_AddPlanBeneficiosAndColor', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [AsientosContables] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [Referencia] nvarchar(50) NULL,
    [Descripcion] nvarchar(max) NULL,
    [TipoEvento] nvarchar(max) NULL,
    CONSTRAINT [PK_AsientosContables] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [CuentasContables] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Codigo] nvarchar(20) NOT NULL,
    [Nombre] nvarchar(200) NOT NULL,
    [Clase] int NOT NULL,
    [EsActiva] bit NOT NULL,
    [PermiteMovimiento] bit NOT NULL,
    CONSTRAINT [PK_CuentasContables] PRIMARY KEY ([Id])
);
GO

CREATE TABLE [LineasAsientosContables] (
    [Id] uniqueidentifier NOT NULL,
    [AsientoContableId] uniqueidentifier NOT NULL,
    [CuentaContableId] uniqueidentifier NOT NULL,
    [Debito] decimal(14,2) NOT NULL,
    [Credito] decimal(14,2) NOT NULL,
    [TerceroId] uniqueidentifier NULL,
    [CentroCostoId] uniqueidentifier NULL,
    CONSTRAINT [PK_LineasAsientosContables] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LineasAsientosContables_AsientosContables_AsientoContableId] FOREIGN KEY ([AsientoContableId]) REFERENCES [AsientosContables] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_LineasAsientosContables_Clientes_TerceroId] FOREIGN KEY ([TerceroId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_LineasAsientosContables_CuentasContables_CuentaContableId] FOREIGN KEY ([CuentaContableId]) REFERENCES [CuentasContables] ([Id]) ON DELETE NO ACTION
);
GO

CREATE INDEX [IX_AsientosContables_TenantId] ON [AsientosContables] ([TenantId]);
GO

CREATE UNIQUE INDEX [IX_CuentasContables_TenantId_Codigo] ON [CuentasContables] ([TenantId], [Codigo]);
GO

CREATE INDEX [IX_LineasAsientosContables_AsientoContableId] ON [LineasAsientosContables] ([AsientoContableId]);
GO

CREATE INDEX [IX_LineasAsientosContables_CuentaContableId] ON [LineasAsientosContables] ([CuentaContableId]);
GO

CREATE INDEX [IX_LineasAsientosContables_TerceroId] ON [LineasAsientosContables] ([TerceroId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260316162701_AddAccountingModule', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Ordenes] ADD [AplicarRetencion] bit NOT NULL DEFAULT CAST(0 AS bit);
GO

ALTER TABLE [Ordenes] ADD [MontoRetencion] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

ALTER TABLE [Ordenes] ADD [PorcentajeRetencion] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260316165837_AddWithholdingToOrden', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Tenants] ADD [Ciudad] nvarchar(100) NULL;
GO

ALTER TABLE [Tenants] ADD [NIT] nvarchar(30) NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260316170943_AddNitAndCiudadToTenant', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Appointments] (
    [Id] uniqueidentifier NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [ClienteId] uniqueidentifier NOT NULL,
    [VehiculoId] uniqueidentifier NOT NULL,
    [MechanicId] nvarchar(max) NOT NULL,
    [StartDateTime] datetime2 NOT NULL,
    [EndDateTime] datetime2 NOT NULL,
    [EstimatedDuration] int NOT NULL,
    [ServiceType] nvarchar(200) NOT NULL,
    [Status] int NOT NULL,
    [WhatsappReminderSent] bit NOT NULL,
    [FechaRegistro] datetime2 NOT NULL,
    CONSTRAINT [PK_Appointments] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Appointments_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Appointments_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Appointments_Vehiculos_VehiculoId] FOREIGN KEY ([VehiculoId]) REFERENCES [Vehiculos] ([Id]) ON DELETE NO ACTION
);
GO

CREATE TABLE [MechanicAvailabilities] (
    [Id] uniqueidentifier NOT NULL,
    [MechanicId] nvarchar(max) NOT NULL,
    [DayOfWeek] int NOT NULL,
    [StartTime] time NOT NULL,
    [EndTime] time NOT NULL,
    [IsActive] bit NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    CONSTRAINT [PK_MechanicAvailabilities] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_MechanicAvailabilities_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_Appointments_ClienteId] ON [Appointments] ([ClienteId]);
GO

CREATE INDEX [IX_Appointments_TenantId] ON [Appointments] ([TenantId]);
GO

CREATE INDEX [IX_Appointments_VehiculoId] ON [Appointments] ([VehiculoId]);
GO

CREATE INDEX [IX_MechanicAvailabilities_TenantId] ON [MechanicAvailabilities] ([TenantId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260316174438_AddAgendaModule', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Ordenes] ADD [AppointmentId] uniqueidentifier NULL;
GO

CREATE INDEX [IX_Ordenes_AppointmentId] ON [Ordenes] ([AppointmentId]);
GO

ALTER TABLE [Ordenes] ADD CONSTRAINT [FK_Ordenes_Appointments_AppointmentId] FOREIGN KEY ([AppointmentId]) REFERENCES [Appointments] ([Id]) ON DELETE SET NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260316195736_AddAppointmentIdToOrden', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_MechanicAvailabilities_TenantId] ON [MechanicAvailabilities];
GO

DROP INDEX [IX_Appointments_TenantId] ON [Appointments];
GO

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MechanicAvailabilities]') AND [c].[name] = N'MechanicId');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [MechanicAvailabilities] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [MechanicAvailabilities] ALTER COLUMN [MechanicId] nvarchar(450) NOT NULL;
GO

CREATE INDEX [IX_MechanicAvailabilities_Mechanic_Day_Active] ON [MechanicAvailabilities] ([TenantId], [MechanicId], [DayOfWeek]);
GO

CREATE INDEX [IX_Appointments_Tenant_DateRange_Mechanic] ON [Appointments] ([TenantId], [StartDateTime], [EndDateTime]) INCLUDE ([MechanicId], [Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260318124405_AddAgendaIndices', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [NominaRegistros] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(max) NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [Periodo] nvarchar(7) NOT NULL,
    [SalarioBase] decimal(12,2) NOT NULL,
    [Comisiones] decimal(12,2) NOT NULL,
    [Deducciones] decimal(12,2) NOT NULL,
    [Estado] int NOT NULL,
    [FechaCreacion] datetime2 NOT NULL,
    CONSTRAINT [PK_NominaRegistros] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_NominaRegistros_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_NominaRegistros_TenantId] ON [NominaRegistros] ([TenantId]);
GO

CREATE INDEX [IX_NominaRegistros_TenantId_Periodo] ON [NominaRegistros] ([TenantId], [Periodo]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260318174819_AddNominaModule', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [NominaRegistros] ADD [IngresosGenerados] decimal(18,2) NOT NULL DEFAULT 0.0;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260318182124_UpdateNominaRentabilidad', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [EmpleadoContratos] (
    [Id] uniqueidentifier NOT NULL,
    [UserId] nvarchar(450) NOT NULL,
    [TenantId] uniqueidentifier NOT NULL,
    [SalarioBase] decimal(12,2) NOT NULL,
    [PorcentajeComision] decimal(5,2) NOT NULL,
    [FechaIngreso] datetime2 NOT NULL,
    [Activo] bit NOT NULL,
    [TipoEmpleado] nvarchar(max) NOT NULL,
    [URLContratoPDF] nvarchar(max) NULL,
    CONSTRAINT [PK_EmpleadoContratos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_EmpleadoContratos_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_EmpleadoContratos_TenantId] ON [EmpleadoContratos] ([TenantId]);
GO

CREATE UNIQUE INDEX [IX_EmpleadoContratos_UserId] ON [EmpleadoContratos] ([UserId]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260318195223_AddEmpleadoContrato', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_NominaRegistros_TenantId] ON [NominaRegistros];
GO

DROP INDEX [IX_NominaRegistros_TenantId_Periodo] ON [NominaRegistros];
GO

DROP INDEX [IX_Appointments_Tenant_DateRange_Mechanic] ON [Appointments];
GO

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[NominaRegistros]') AND [c].[name] = N'UserId');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [NominaRegistros] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [NominaRegistros] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
GO

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Appointments]') AND [c].[name] = N'MechanicId');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [Appointments] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [Appointments] ALTER COLUMN [MechanicId] nvarchar(450) NOT NULL;
GO

CREATE INDEX [IX_NominaRegistros_Tenant_Period_Status_User] ON [NominaRegistros] ([TenantId], [Periodo], [Estado], [UserId]);
GO

CREATE INDEX [IX_Appointments_Tenant_Mechanic_Dates] ON [Appointments] ([TenantId], [MechanicId], [StartDateTime], [EndDateTime]) INCLUDE ([Status]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260318210516_AddOptimizationIndexes', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

DROP INDEX [IX_Vehiculos_TenantId] ON [Vehiculos];
GO

DROP INDEX [IX_Ordenes_TenantId] ON [Ordenes];
GO

DROP INDEX [IX_Clientes_TenantId] ON [Clientes];
GO

CREATE INDEX [IX_Vehiculos_Tenant_Date] ON [Vehiculos] ([TenantId], [FechaRegistro]);
GO

CREATE INDEX [IX_Ordenes_Tenant_Date_State] ON [Ordenes] ([TenantId], [FechaEntrada], [Estado]);
GO

CREATE INDEX [IX_Clientes_Tenant_Date] ON [Clientes] ([TenantId], [FechaRegistro]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260318213458_AddPerformanceIndices', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

CREATE INDEX [IX_AspNetUsers_TenantId] ON [AspNetUsers] ([TenantId]);
GO

ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Tenants_TenantId] FOREIGN KEY ([TenantId]) REFERENCES [Tenants] ([Id]) ON DELETE NO ACTION;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260322004709_AddUserTenantRelation', N'8.0.0');
GO

COMMIT;
GO

