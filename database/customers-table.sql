-- Customers master data (run on existing CloudWarehouse DB)
-- Also merged into database/schema.sql for new installs

USE CloudWarehouse;
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        CustomerCode    NVARCHAR(50)  NOT NULL,
        CustomerName    NVARCHAR(200) NOT NULL,
        Status          INT           NOT NULL DEFAULT 1,
        CreateTime      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        Remark          NVARCHAR(500) NULL,
        CONSTRAINT UQ_Customers_CustomerCode UNIQUE (CustomerCode)
    );
    CREATE INDEX IX_Customers_CustomerName ON dbo.Customers(CustomerName);
END
GO

-- Seed from sample spreadsheet (客户编号 / 客户名称)
MERGE dbo.Customers AS t
USING (VALUES
    (N'A0001', N'小米粒服饰百货店铺'),
    (N'A0002', N'织布鸟家纺工厂店'),
    (N'A0003', N'长安区坦姐服装店'),
    (N'A0004', N'鑫诗雅魅力服饰箱包严选'),
    (N'A0005', N'小米粒服饰百货店铺'),
    (N'A0006', N'娜娜服饰中午12点开播'),
    (N'A0007', N'沫沫大姨女装网批')
) AS s(CustomerCode, CustomerName)
ON t.CustomerCode = s.CustomerCode
WHEN MATCHED THEN
    UPDATE SET CustomerName = s.CustomerName, Status = 1
WHEN NOT MATCHED THEN
    INSERT (CustomerCode, CustomerName, Status)
    VALUES (s.CustomerCode, s.CustomerName, 1);
GO

PRINT 'Customers table ready.';
GO
