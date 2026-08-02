-- Phase 2：运单结算与客户账户（在 schema.sql 之后执行）
USE CloudWarehouse;
GO

IF OBJECT_ID(N'dbo.CustomerAccounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerAccounts (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        CustomerId      BIGINT        NOT NULL,
        AccountName     NVARCHAR(200) NOT NULL,
        Status          INT           NOT NULL DEFAULT 1,
        CreateTime      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        Remark          NVARCHAR(500) NULL,
        CONSTRAINT FK_CustomerAccounts_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id),
        CONSTRAINT UQ_CustomerAccounts_AccountName UNIQUE (AccountName)
    );
    CREATE INDEX IX_CustomerAccounts_CustomerId ON dbo.CustomerAccounts(CustomerId);
END
GO

IF OBJECT_ID(N'dbo.BillLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BillLines (
        Id                      BIGINT IDENTITY(1,1) PRIMARY KEY,
        WaybillNo               NVARCHAR(50)   NOT NULL,
        BillDate                DATE           NOT NULL,
        CustomerId              BIGINT         NULL,
        AccountName             NVARCHAR(200)  NULL,
        ExpressType             NVARCHAR(100)  NULL,
        Province                NVARCHAR(50)   NOT NULL,
        City                    NVARCHAR(50)   NULL,
        BillingType             NVARCHAR(20)   NOT NULL DEFAULT N'正向计费',
        ActualWeight            DECIMAL(10,3)  NOT NULL,
        RoundedWeight           DECIMAL(10,3)  NULL,
        -- 应收
        ReceivableTransitFee    DECIMAL(10,2)  NULL,
        ReceivableLabelFee      DECIMAL(10,2)  NULL,
        ReceivableSurcharge     DECIMAL(10,2)  NULL DEFAULT 0,
        ReceivableTotal         DECIMAL(10,2)  NULL,
        -- 应付
        PayableTransitFee       DECIMAL(10,2)  NULL,
        PayableLabelFee         DECIMAL(10,2)  NULL,
        PayableSurcharge        DECIMAL(10,2)  NULL DEFAULT 0,
        PayableTotal            DECIMAL(10,2)  NULL,
        Profit                  DECIMAL(10,2)  NULL,
        ImportBatchId           NVARCHAR(50)   NULL,
        CreateTime              DATETIME2      NOT NULL DEFAULT SYSDATETIME(),
        Remark                  NVARCHAR(500)  NULL,
        CONSTRAINT UQ_BillLines_WaybillNo UNIQUE (WaybillNo)
    );
    CREATE INDEX IX_BillLines_BillDate ON dbo.BillLines(BillDate);
    CREATE INDEX IX_BillLines_CustomerId ON dbo.BillLines(CustomerId);
    CREATE INDEX IX_BillLines_Province ON dbo.BillLines(Province);
END
GO
