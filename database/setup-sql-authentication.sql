-- CloudWarehouse — SQL Server 登录（用户名/密码）
-- schema.sql 只负责建库建表，不包含账号密码；应用在 appsettings.json 里连接。
--
-- 本脚本：启用混合身份验证 + 设置 sa 密码（与 appsettings.json 中一致）
-- 在 SSMS 中【以管理员身份】对本机 SQL 实例执行。

-- ========== 第一步：在 SSMS 图形界面（必做，无法只用 SQL 完成）==========
-- 右键服务器 → 属性 → 安全性
--   · 服务器身份验证：选择【SQL Server 和 Windows 身份验证模式】
--   · 确定后【重启 SQL Server 服务】（配置管理器或 services.msc）
--
-- ========== 第二步：设置 sa 密码（与 appsettings 一致）==========
-- 下面密码需与 CloudWarehouse.Backend\appsettings.json 中 DefaultConnection 相同。
-- 部署到服务器后请改为更强密码，并同步改 appsettings.json。

ALTER LOGIN sa WITH PASSWORD = N'Cw@Wh2026#Sa9xK';
ALTER LOGIN sa ENABLE;
GO

-- 确认 sa 能访问本库（建库后执行 schema.sql 再跑本段可选）
USE CloudWarehouse;
GO
-- sa 默认是 sysadmin，已有权访问；若改用专用账号见文末注释。

PRINT N'sa 密码已更新。请用 SQL 身份验证测试连接后启动网站。';
GO

/*
========== 可选：不用 sa，建专用应用账号（更安全）==========
CREATE LOGIN cloudwarehouse_app WITH PASSWORD = N'你的复杂密码';
USE CloudWarehouse;
CREATE USER cloudwarehouse_app FOR LOGIN cloudwarehouse_app;
ALTER ROLE db_datareader ADD MEMBER cloudwarehouse_app;
ALTER ROLE db_datawriter ADD MEMBER cloudwarehouse_app;
-- 连接串改为：User Id=cloudwarehouse_app;Password=...

appsettings.json 示例：
Server=localhost;Database=CloudWarehouse;User Id=sa;Password=Cw@Wh2026#Sa9xK;TrustServerCertificate=True;Encrypt=False
*/
