IF NOT EXISTS(SELECT * FROM sys.databases WHERE name = 'EMSDb')
BEGIN
    CREATE DATABASE EMSDb
END
GO

USE EMSDb

IF OBJECT_ID('[dbo].[Timekeeping]', 'U') IS NULL
BEGIN
    CREATE TABLE [Timekeeping] (
        [EmployeeId]      INT  NOT NULL,
        [ProjectId]       INT  NOT NULL,
        [WeekClosingDate] DATETIME NOT NULL,
        [HoursWorked]     INT  NOT NULL,
        CONSTRAINT [PK_Timekeeping] PRIMARY KEY CLUSTERED ([EmployeeId] ASC, [ProjectId] ASC,  [WeekClosingDate] ASC)
    )
END
GO

IF OBJECT_ID('[dbo].[Payroll]', 'U') IS NULL
BEGIN
    CREATE TABLE [Payroll] (
        [EmployeeId]   INT   NOT NULL,
        [PayRateInUSD] MONEY DEFAULT 0 NOT NULL,
        CONSTRAINT [PK_Payroll] PRIMARY KEY CLUSTERED ([EmployeeId] ASC)
    )
END
GO