-- =========================================================================
-- AUTHOR: Antigravity Architect
-- DESCRIPTION: Stored Procedure to atomically validate and create/update an appointment.
-- This prevents race conditions (double booking) when two users save an appointment simultaneously,
-- and avoids N+1 trips to the base by doing overlap checks server-side.
-- =========================================================================

CREATE OR ALTER PROCEDURE [dbo].[SP_UpsertAppointment]
    @Id UNIQUEIDENTIFIER,          -- Pass NULL or empty GUID for new appointment
    @TenantId UNIQUEIDENTIFIER,
    @ClienteId UNIQUEIDENTIFIER,
    @VehiculoId UNIQUEIDENTIFIER,
    @MechanicId NVARCHAR(450),
    @StartDateTime DATETIME2,      -- Must be UTC
    @EndDateTime DATETIME2,        -- Must be UTC
    @EstimatedDuration INT,
    @ServiceType NVARCHAR(200),
    @Status INT,
    
    @NewId UNIQUEIDENTIFIER OUTPUT, -- Returns the ID of the created/updated appointment
    @ErrorMessage NVARCHAR(4000) OUTPUT -- Returns error if validation fails
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    
    BEGIN TRY
        BEGIN TRANSACTION;

        SET @NewId = NULL;
        SET @ErrorMessage = NULL;

        -- 1. Validate Overlap
        -- Check if there's any appointment for this mechanic that overlaps with the requested time.
        -- Overlap logic: Existing Start < New End AND Existing End > New Start
        IF EXISTS (
            SELECT 1 
            FROM [dbo].[Appointments] WITH (UPDLOCK, HOLDLOCK) -- Lock to prevent race conditions
            WHERE TenantId = @TenantId 
              AND MechanicId = @MechanicId 
              AND Status != 5 -- Cancelled
              AND StartDateTime < @EndDateTime 
              AND EndDateTime > @StartDateTime
              AND (@Id IS NULL OR Id != @Id)
        )
        BEGIN
            SET @ErrorMessage = 'El mecánico ya tiene una cita programada en este horario.';
            ROLLBACK TRANSACTION;
            RETURN;
        END

        -- If @Id is empty, it's an insert, generate new GUID
        IF @Id IS NULL OR @Id = '00000000-0000-0000-0000-000000000000'
        BEGIN
            SET @NewId = NEWID();
            
            INSERT INTO [dbo].[Appointments] (
                [Id], [TenantId], [ClienteId], [VehiculoId], [MechanicId], 
                [StartDateTime], [EndDateTime], [EstimatedDuration], [ServiceType], 
                [Status], [WhatsappReminderSent], [FechaRegistro]
            )
            VALUES (
                @NewId, @TenantId, @ClienteId, @VehiculoId, @MechanicId,
                @StartDateTime, @EndDateTime, @EstimatedDuration, @ServiceType,
                @Status, 0, GETUTCDATE()
            );
        END
        ELSE
        BEGIN
            SET @NewId = @Id;

            UPDATE [dbo].[Appointments]
            SET 
                [ClienteId] = @ClienteId,
                [VehiculoId] = @VehiculoId,
                [MechanicId] = @MechanicId,
                [StartDateTime] = @StartDateTime,
                [EndDateTime] = @EndDateTime,
                [EstimatedDuration] = @EstimatedDuration,
                [ServiceType] = @ServiceType,
                [Status] = @Status
            WHERE [Id] = @Id AND [TenantId] = @TenantId;

            IF @@ROWCOUNT = 0
            BEGIN
                SET @ErrorMessage = 'Cita no encontrada o no pertenece al Tenant actual.';
                ROLLBACK TRANSACTION;
                RETURN;
            END
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        SET @ErrorMessage = ERROR_MESSAGE();
    END CATCH
END
GO
