IF DB_ID(N'BeOnLookout') IS NULL
BEGIN
    CREATE DATABASE [BeOnLookout];
END;
GO

USE [BeOnLookout];
GO

IF OBJECT_ID(N'dbo.CashDeposits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CashDeposits
    (
        CashDepositId BIGINT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_CashDeposits PRIMARY KEY,
        CustomerReference NVARCHAR(64) NOT NULL,
        Amount DECIMAL(11, 2) NOT NULL,
        DepositedAtUtc DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_CashDeposits_DepositedAtUtc
            DEFAULT (TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00')),
        CONSTRAINT CK_CashDeposits_CustomerReference_NotBlank
            CHECK (LEN(LTRIM(RTRIM(CustomerReference))) > 0),
        CONSTRAINT CK_CashDeposits_Amount_Positive
            CHECK (Amount > 0)
    );
END;
GO
