IF DB_ID(N'BeOnLookout') IS NULL
BEGIN
    CREATE DATABASE [BeOnLookout];
END;
GO

USE [BeOnLookout];
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        CustomerId BIGINT IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_Customers PRIMARY KEY,
        CustomerReference NVARCHAR(64) NOT NULL,
        DisplayName NVARCHAR(128) NOT NULL,
        CreatedAtUtc DATETIMEOFFSET(7) NOT NULL
            CONSTRAINT DF_Customers_CreatedAtUtc
            DEFAULT (TODATETIMEOFFSET(SYSUTCDATETIME(), '+00:00')),
        CONSTRAINT CK_Customers_CustomerReference_NotBlank
            CHECK (LEN(LTRIM(RTRIM(CustomerReference))) > 0),
        CONSTRAINT CK_Customers_DisplayName_NotBlank
            CHECK (LEN(LTRIM(RTRIM(DisplayName))) > 0)
    );
END;
GO
