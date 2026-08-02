# 数据库脚本执行顺序

| 顺序 | 文件 | 作用 |
|------|------|------|
| 1 | **schema.sql** | 创建数据库 `CloudWarehouse`、四张表、示例数据 |
| 2 | **setup-sql-authentication.sql** | 设置 **sa** 密码（须先在 SSMS 启用「SQL 和 Windows 混合验证」并重启 SQL） |
| 3 | （仅旧库报错时）**fix-price-rules-index.sql** | 删除错误的 PriceRules 唯一索引 |

## 用户名和密码在哪？

- **不在 schema.sql 里** — 那是建表脚本，不是连接配置。
- **应用连接** — `CloudWarehouse.Backend/appsettings.json`：

```text
User Id=sa;Password=Cw@Wh2026#Sa9xK
```

部署到师傅服务器后请改成更强密码，并同步修改 SQL 与 appsettings。

## 给师傅的一句话

> 先跑 schema 建库，再跑 setup 设 sa 密码；网站 appsettings 里用同样的 sa 账号连接。
