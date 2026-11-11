-- Conserva columnas, registros, identificadores, índices y claves foráneas.
-- Ejecutar con las instancias de FarmaGest cerradas y usar el mapeo ToTable("Cobro").
USE FarmaGestDB;
GO
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.Facturacion', N'U') IS NOT NULL
    BEGIN
        IF OBJECT_ID(N'dbo.Cobro') IS NOT NULL
            THROW 51000, 'Ya existe dbo.Cobro; no se puede renombrar Facturacion.', 1;
        EXEC sys.sp_rename @objname = N'dbo.Facturacion', @newname = N'Cobro', @objtype = N'OBJECT';
    END
    ELSE IF OBJECT_ID(N'dbo.Cobro', N'U') IS NULL
        THROW 51000, 'No existe dbo.Facturacion ni dbo.Cobro.', 1;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
