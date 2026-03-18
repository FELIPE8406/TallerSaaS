USE master;
GO

-- Forzar el cierre de todas las conexiones activas a la base de datos
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'TallerSaaS')
BEGIN
    ALTER DATABASE [TallerSaaS] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    
    -- Eliminar la base de datos
    DROP DATABASE [TallerSaaS];
    
    PRINT 'La base de datos TallerSaaS ha sido eliminada correctamente.';
END
ELSE
BEGIN
    PRINT 'La base de datos TallerSaaS no existe.';
END
GO