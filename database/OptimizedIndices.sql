-- =========================================================================
-- AUTHOR: Antigravity Architect
-- DESCRIPTION: Optimized Covering Indices for TallerSaaS Multi-Tenant SaaS
-- USAGE:  Run in SSMS against TallerSaaS database. Safe to re-run (idempotent).
-- =========================================================================

-- ═══════════════════════════════════════════════════════════════════════════
-- 1. ORDENES — Paginated grid: ORDER BY FechaEntrada DESC, filter by Estado
-- ═══════════════════════════════════════════════════════════════════════════
-- The paginated query (OFFSET-FETCH) always filters by TenantId (global filter),
-- optionally by Estado, and sorts by FechaEntrada DESC.
-- INCLUDE covers the SELECT columns to eliminate key lookups.
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_Ordenes_Tenant_FechaEntrada_DESC' 
    AND object_id = OBJECT_ID('Ordenes')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Ordenes_Tenant_FechaEntrada_DESC]
    ON [dbo].[Ordenes] ([TenantId], [FechaEntrada] DESC, [Estado])
    INCLUDE ([NumeroOrden], [VehiculoId], [Total], [Pagada], [Bloqueada], [FacturaId]);
    
    PRINT 'Created IX_Ordenes_Tenant_FechaEntrada_DESC';
END
ELSE
BEGIN
    PRINT 'Index IX_Ordenes_Tenant_FechaEntrada_DESC already exists.';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 2. ORDENES — Dashboard aggregation: SUM(Total) WHERE FechaEntrada >= X
-- ═══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_Ordenes_Tenant_Pagada_FechaEntrada' 
    AND object_id = OBJECT_ID('Ordenes')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Ordenes_Tenant_Pagada_FechaEntrada]
    ON [dbo].[Ordenes] ([TenantId], [Pagada], [FechaEntrada])
    INCLUDE ([Total], [Estado]);
    
    PRINT 'Created IX_Ordenes_Tenant_Pagada_FechaEntrada';
END
ELSE
BEGIN
    PRINT 'Index IX_Ordenes_Tenant_Pagada_FechaEntrada already exists.';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 3. VEHICULOS — Paginated grid: ORDER BY FechaRegistro DESC
-- ═══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_Vehiculos_Tenant_FechaRegistro_DESC' 
    AND object_id = OBJECT_ID('Vehiculos')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Vehiculos_Tenant_FechaRegistro_DESC]
    ON [dbo].[Vehiculos] ([TenantId], [FechaRegistro] DESC)
    INCLUDE ([ClienteId], [Marca], [Modelo], [Anio], [Placa], [VIN], [Color], [Kilometraje]);
    
    PRINT 'Created IX_Vehiculos_Tenant_FechaRegistro_DESC';
END
ELSE
BEGIN
    PRINT 'Index IX_Vehiculos_Tenant_FechaRegistro_DESC already exists.';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 4. CLIENTES — Quick top-N for dropdown population
-- ═══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_Clientes_Tenant_Activo' 
    AND object_id = OBJECT_ID('Clientes')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Clientes_Tenant_Activo]
    ON [dbo].[Clientes] ([TenantId], [Activo])
    INCLUDE ([NombreCompleto], [Telefono], [FechaRegistro]);
    
    PRINT 'Created IX_Clientes_Tenant_Activo';
END
ELSE
BEGIN
    PRINT 'Index IX_Clientes_Tenant_Activo already exists.';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 5. APPOINTMENTS — Schedule queries by Tenant + date range
-- ═══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_Appointments_Tenant_DateRange_Mechanic' 
    AND object_id = OBJECT_ID('Appointments')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Appointments_Tenant_DateRange_Mechanic]
    ON [dbo].[Appointments] ([TenantId], [StartDateTime], [EndDateTime])
    INCLUDE ([MechanicId], [Status]);
    
    PRINT 'Created IX_Appointments_Tenant_DateRange_Mechanic';
END
ELSE
BEGIN
    PRINT 'Index IX_Appointments_Tenant_DateRange_Mechanic already exists.';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 6. MECHANIC AVAILABILITY — Day-of-week lookups
-- ═══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_MechanicAvailabilities_Mechanic_Day_Active' 
    AND object_id = OBJECT_ID('MechanicAvailabilities')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_MechanicAvailabilities_Mechanic_Day_Active]
    ON [dbo].[MechanicAvailabilities] ([MechanicId], [DayOfWeek], [IsActive])
    INCLUDE ([StartTime], [EndTime], [TenantId]);
    
    PRINT 'Created IX_MechanicAvailabilities_Mechanic_Day_Active';
END
ELSE
BEGIN
    PRINT 'Index IX_MechanicAvailabilities_Mechanic_Day_Active already exists.';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 7. FACTURAS — Dashboard count of pending DIAN submissions
-- ═══════════════════════════════════════════════════════════════════════════
IF NOT EXISTS (
    SELECT * FROM sys.indexes 
    WHERE name = 'IX_Facturas_Tenant_TipoFacturacion_EstadoEnvio' 
    AND object_id = OBJECT_ID('Facturas')
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Facturas_Tenant_TipoFacturacion_EstadoEnvio]
    ON [dbo].[Facturas] ([TenantId], [TipoFacturacion], [EstadoEnvio]);
    
    PRINT 'Created IX_Facturas_Tenant_TipoFacturacion_EstadoEnvio';
END
ELSE
BEGIN
    PRINT 'Index IX_Facturas_Tenant_TipoFacturacion_EstadoEnvio already exists.';
END
GO
