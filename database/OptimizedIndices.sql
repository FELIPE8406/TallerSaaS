-- =========================================================================
-- AUTHOR: Antigravity Architect
-- DESCRIPTION: Optimized Indices for TallerSaaS Appointments & Availability
-- =========================================================================

-- 1. Appointments Index
-- Rationale: The query `GetAppointmentsAsync` frequently searches by TenantId and a date range.
-- Including MechanicId helps with overlapping checks `HasOverlapAsync`
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

-- 2. MechanicAvailability Index
-- Rationale: `IsMechanicAvailableAsync` checks by MechanicId, DayOfWeek, and filters by IsActive.
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
