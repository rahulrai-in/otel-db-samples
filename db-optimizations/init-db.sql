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

TRUNCATE TABLE Payroll
TRUNCATE TABLE Timekeeping


INSERT INTO Payroll Values(1, 100)
INSERT INTO Payroll Values(2, 200)
INSERT INTO Payroll Values(3, 300)

INSERT INTO Timekeeping Values(1, 1111, GETDATE(), 10)
INSERT INTO Timekeeping Values(1, 2222, GETDATE(), 15)
INSERT INTO Timekeeping Values(2, 1111, GETDATE(), 15)
INSERT INTO Timekeeping Values(3, 2222, GETDATE(), 20)
GO