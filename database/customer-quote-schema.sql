-- Phase 2：客户报价表（应收价；成本/PriceRules 仍为应付）
USE CloudWarehouse;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'A0001')
    INSERT INTO dbo.Customers (CustomerCode, CustomerName, Status, CreateTime)
    VALUES (N'A0001', N'Integration Test Customer', 1, SYSDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'A0002')
    INSERT INTO dbo.Customers (CustomerCode, CustomerName, Status, CreateTime)
    VALUES (N'A0002', N'Sample Customer 2', 1, SYSDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'93')
    INSERT INTO dbo.Customers (CustomerCode, CustomerName, Status, CreateTime)
    VALUES (N'93', N'小二小店', 1, SYSDATETIME());
GO

IF OBJECT_ID(N'dbo.CustomerQuoteRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerQuoteRules (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        CustomerId      BIGINT        NOT NULL,
        Province        NVARCHAR(50)  NOT NULL,
        ExpressType     NVARCHAR(100) NULL,
        BillingType     INT           NOT NULL,
        MinWeight       DECIMAL(10,2) NOT NULL,
        MaxWeight       DECIMAL(10,2) NOT NULL,
        UnitPrice       DECIMAL(10,2) NOT NULL,
        BaseFee         DECIMAL(10,2) NOT NULL DEFAULT 0,
        EffectiveDate   DATE          NOT NULL,
        ExpiryDate      DATE          NULL,
        Status          INT           NOT NULL DEFAULT 1,
        CreateTime      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        Remark          NVARCHAR(200) NULL,
        CONSTRAINT FK_CustomerQuoteRules_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
    );
    CREATE INDEX IX_CustomerQuoteRules_Lookup ON dbo.CustomerQuoteRules(CustomerId, Province, ExpressType, EffectiveDate);
END
GO

-- 集成测试用账户（运单「集成测试账户」→ A0001）
IF OBJECT_ID(N'dbo.CustomerAccounts', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.CustomerAccounts WHERE AccountName = N'IntegrationTestAccount')
        INSERT INTO dbo.CustomerAccounts (CustomerId, AccountName, Status)
        SELECT Id, N'IntegrationTestAccount', 1 FROM dbo.Customers WHERE CustomerCode = N'A0001';
END
GO
