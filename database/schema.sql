-- CloudWarehouse SQL Server 建库脚本
-- 在 SSMS 或 sqlcmd 中执行

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
GO
