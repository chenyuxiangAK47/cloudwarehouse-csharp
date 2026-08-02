-- 演示客户 93 小二小店 + 账户 AIR小店30（运单测试用）
USE CloudWarehouse;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'93')
    INSERT INTO dbo.Customers (CustomerCode, CustomerName, Status, CreateTime)
    VALUES (N'93', N'小二小店', 1, SYSDATETIME());
GO

IF OBJECT_ID(N'dbo.CustomerAccounts', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.CustomerAccounts WHERE AccountName = N'AIR小店30')
        INSERT INTO dbo.CustomerAccounts (CustomerId, AccountName, Status, CreateTime)
        SELECT Id, N'AIR小店30', 1, SYSDATETIME()
        FROM dbo.Customers WHERE CustomerCode = N'93';
END
GO

PRINT N'演示客户 93 / 账户 AIR小店30 已就绪。';
GO
