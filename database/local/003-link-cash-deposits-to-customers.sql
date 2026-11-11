USE [BeOnLookout];
GO

IF COL_LENGTH(N'dbo.CashDeposits', N'CustomerId') IS NULL
BEGIN
    ALTER TABLE dbo.CashDeposits
        ADD CustomerId BIGINT NULL;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_CashDeposits_Customers'
      AND parent_object_id = OBJECT_ID(N'dbo.CashDeposits')
)
BEGIN
    ALTER TABLE dbo.CashDeposits
        ADD CONSTRAINT FK_CashDeposits_Customers
        FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(CustomerId);
END;
GO
