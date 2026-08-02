-- CloudWarehouse SQL Server 建库脚本（库表结构 + 示例数据）
-- 在 SSMS 或 sqlcmd 中执行
--
-- 说明：本文件不含「应用登录用户名/密码」。
--       账号密码在 appsettings.json 连接串中；首次请再执行 setup-sql-authentication.sql 设置 sa。

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'CloudWarehouse')
    CREATE DATABASE CloudWarehouse;
GO

USE CloudWarehouse;
GO

-- 站点（如 C001 配送站）
IF OBJECT_ID(N'dbo.Sites', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Sites (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        SiteCode        NVARCHAR(50)  NOT NULL,
        SiteName        NVARCHAR(100) NOT NULL,
        SiteType        INT           NOT NULL DEFAULT 1,
        ExpressCompany  NVARCHAR(100) NULL,
        ContactPerson   NVARCHAR(50)  NULL,
        ContactPhone    NVARCHAR(30)  NULL,
        Address         NVARCHAR(200) NULL,
        Status          INT           NOT NULL DEFAULT 1,
        CreateTime      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        Remark          NVARCHAR(500) NULL,
        CONSTRAINT UQ_Sites_SiteCode UNIQUE (SiteCode)
    );
END
GO

-- 目的地/仓库（如 001、11 等编码）
IF OBJECT_ID(N'dbo.Destinations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Destinations (
        Id          BIGINT IDENTITY(1,1) PRIMARY KEY,
        DestCode    NVARCHAR(50)  NOT NULL,
        Province    NVARCHAR(50)  NOT NULL,
        City        NVARCHAR(50)  NULL,
        Area        NVARCHAR(50)  NULL,
        CreateTime  DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        CONSTRAINT UQ_Destinations_DestCode UNIQUE (DestCode)
    );
END
GO

-- 客户（客户编号 / 客户名称）
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

-- 价格规则（由 Excel 导入生成；区间≤5kg + 续重>5kg）
IF OBJECT_ID(N'dbo.PriceRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriceRules (
        Id              BIGINT IDENTITY(1,1) PRIMARY KEY,
        SiteId          BIGINT        NOT NULL,
        DestId          BIGINT        NOT NULL,
        BillingType     INT           NOT NULL,
        MinWeight       DECIMAL(10,2) NOT NULL,
        MaxWeight       DECIMAL(10,2) NOT NULL,
        UnitPrice       DECIMAL(10,2) NOT NULL,
        BaseFee         DECIMAL(10,2) NOT NULL DEFAULT 3.5,
        EffectiveDate   DATE          NOT NULL,
        ExpiryDate      DATE          NULL,
        Status          INT           NOT NULL DEFAULT 1,
        CreateTime      DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
        Remark          NVARCHAR(200) NULL,
        CONSTRAINT FK_PriceRules_Site FOREIGN KEY (SiteId) REFERENCES dbo.Sites(Id),
        CONSTRAINT FK_PriceRules_Dest FOREIGN KEY (DestId) REFERENCES dbo.Destinations(Id)
    );
    -- Non-unique only: many rules per lane (tiers + overweight share EffectiveDate).
    -- Do NOT add UNIQUE (SiteId, DestId, EffectiveDate) — import will fail.
    CREATE INDEX IX_PriceRules_Site_Dest ON dbo.PriceRules(SiteId, DestId);
END
GO

-- 示例数据（便于导入演示：站点 C001 + 目的地 11/12）
IF NOT EXISTS (SELECT 1 FROM dbo.Sites WHERE SiteCode = N'C001')
    INSERT INTO dbo.Sites (SiteCode, SiteName, SiteType, ExpressCompany, Status)
    VALUES (N'C001', N'示例配送站', 1, N'示例快递', 1);

IF NOT EXISTS (SELECT 1 FROM dbo.Destinations WHERE DestCode = N'11')
    INSERT INTO dbo.Destinations (DestCode, Province, City, Area)
    VALUES (N'11', N'安徽省', N'', N'');

IF NOT EXISTS (SELECT 1 FROM dbo.Destinations WHERE DestCode = N'12')
    INSERT INTO dbo.Destinations (DestCode, Province, City, Area)
    VALUES (N'12', N'福建省', N'', N'');

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE CustomerCode = N'A0001')
    INSERT INTO dbo.Customers (CustomerCode, CustomerName, Status) VALUES
    (N'A0001', N'小米粒服饰百货店铺', 1),
    (N'A0002', N'织布鸟家纺工厂店', 1),
    (N'A0003', N'长安区坦姐服装店', 1),
    (N'A0004', N'鑫诗雅魅力服饰箱包严选', 1),
    (N'A0005', N'小米粒服饰百货店铺', 1),
    (N'A0006', N'娜娜服饰中午12点开播', 1),
    (N'A0007', N'沫沫大姨女装网批', 1);
GO
