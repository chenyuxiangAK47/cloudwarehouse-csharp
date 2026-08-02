# CloudWarehouse — 部署清单（Windows Server）

> **师傅电脑没有 dotnet 时：** 用 **自包含发布包**（见下文「零、自包含」），解压后双击 `启动云仓.bat` 即可，**无需安装 .NET**。  
> **若用 IIS：** 仍建议装 Hosting Bundle；或改用 exe 直接运行。

---

## 零、自包含发布（无 dotnet 必看）

| 对比 | 框架依赖（旧包 ~7MB） | **自包含（推荐给师傅）** |
|------|----------------------|-------------------------|
| 师傅电脑 | 必须装 .NET 9 Runtime 或 Hosting Bundle | **不用装 dotnet** |
| 体积 | 小 | 约 **120MB**（zip 约 50MB） |
| 启动 | IIS 或 `dotnet` 环境 | 双击 **`启动云仓.bat`** 或 `CloudWarehouse.Backend.exe` |

**本机打自包含包：**

```powershell
cd D:\tools\cloudwarehouse-csharp
.\scripts\publish-self-contained.ps1
```

**交给师傅：**

`publish\CloudWarehouse-win-x64-self-contained.zip` → 解压 → 改 `appsettings.json` → 双击 `启动云仓.bat` → 浏览器 `http://localhost:5001`

**局域网/远程访问（如 `http://10.28.10.x:5001`）：**

1. 启动脚本已设 `ASPNETCORE_URLS=http://0.0.0.0:5001`（监听所有网卡，不要只绑 localhost）
2. Windows 防火墙 → 入站规则 → 允许 **TCP 5001**
3. 其他电脑浏览器访问：`http://服务器IP:5001`（把 `10.28.10.x` 换成装程序那台机器的内网 IP）
4. `appsettings.json` 里 SQL 的 `Server=` 仍是**数据库地址**（SQL 在本机可仍写 `localhost`；SQL 在别的机器写 `10.28.10.y`）

---

## 一、先搞清：IIS 与直接运行 exe

| 方式 | 是否需要 dotnet | 说明 |
|------|-----------------|------|
| **自包含 + 双击 exe** | **否** | 最适合师傅未装 .NET |
| **publish + IIS** | 需 Hosting Bundle | 正式对外、多用户 |
| **MSI 安装包** | 视打包方式 | 可选 |

---

## 一（原）、部署方式对比

| 方式 | 是什么 | 难度 |
|------|--------|------|
| **自包含 exe** | zip 解压运行 | ⭐ 最简单 |
| **publish + IIS** | IIS 指向目录 | ⭐⭐ |
| **MSI** | 安装包 | ⭐⭐⭐⭐ |

---

## 二、需要准备什么

### 2.1 你本机（开发机）

| 项目 | 要求 |
|------|------|
| .NET 9 SDK | 已能 `dotnet build` / `dotnet run` |
| 项目源码 | 本仓库 `cloudwarehouse-csharp` |
| 远程方式 | 能登录师傅服务器：**远程桌面（RDP）** 或共享文件夹/U 盘拷文件 |

### 2.2 师傅的服务器（Windows Server 或 Win10/11 当服务器）

| 项目 | 说明 |
|------|------|
| **IIS** | 启用「Web 服务器 (IIS)」角色/功能 |
| **.NET 9 Hosting Bundle** | **仅 IIS 部署时需要**；自包含 exe 直接运行则不需要 |
| **SQL Server** | 2019+ 或 Express；库名建议 `CloudWarehouse` |
| **防火墙** | 放行网站端口（如 **80** 或师傅指定的端口） |
| **权限** | 有管理员权限安装 Hosting Bundle、建 IIS 站点 |

### 2.3 下载链接（给师傅或 IT）

- **.NET 9 Hosting Bundle（含 ASP.NET Core Module）**  
  https://dotnet.microsoft.com/download/dotnet/9.0  
  选 **Hosting Bundle**（不是只下 SDK）。

装完后在服务器 PowerShell 执行（确认模块）：

```powershell
dotnet --info
# 应能看到 .NET 9.x runtime

# 建议重启 IIS（装 Hosting Bundle 后官方也建议）
iisreset
```

---

## 三、本机：发布（publish）

在 **PowerShell** 中执行（路径按你机器改）：

```powershell
cd D:\tools\cloudwarehouse-csharp

# 1. 先确认能编译通过
dotnet build CloudWarehouse.sln -c Release

# 2a. 自包含（师傅无 dotnet — 推荐）
.\scripts\publish-self-contained.ps1

# 2b. 框架依赖（体积小，服务器需装 .NET / Hosting Bundle）
dotnet publish CloudWarehouse.Backend\CloudWarehouse.Backend.csproj `
  -c Release `
  -o D:\tools\cloudwarehouse-csharp\publish\CloudWarehouse

# 3. 看输出里应有 CloudWarehouse.Backend.exe、wwwroot、appsettings.json 等
dir D:\tools\cloudwarehouse-csharp\publish\CloudWarehouse
```

### 3.1 发布前检查 `appsettings.json`

本机 `CloudWarehouse.Backend\appsettings.json` 里是：

```json
"DefaultConnection": "Server=localhost;Database=CloudWarehouse;Trusted_Connection=True;TrustServerCertificate=True"
```

**拷到服务器后必须改成服务器 SQL 地址**（见第五节），不要直接用 localhost（除非 SQL 就在同一台服务器上）。

### 3.2 打包给师傅拷走

把整个文件夹打成 zip 即可：

```powershell
Compress-Archive -Path D:\tools\cloudwarehouse-csharp\publish\CloudWarehouse `
  -DestinationPath D:\tools\cloudwarehouse-csharp\publish\CloudWarehouse-release.zip -Force
```

把 **`CloudWarehouse-release.zip`** 拷到服务器，例如解压到：

`C:\inetpub\CloudWarehouse`

---

## 四、服务器：数据库（若还没建库）

在 **SQL Server** 上（SSMS 或 sqlcmd）：

1. 执行仓库脚本：`database\schema.sql`  
2. 若曾改过价格规则索引，再执行：`database\fix-price-rules-index.sql`  
3. 确认有示例站点 `C001`、目的地等（脚本里可能有 seed）

连接串示例（**SQL 在本机、Windows 身份验证**）：

```
Server=localhost;Database=CloudWarehouse;Trusted_Connection=True;TrustServerCertificate=True
```

**SQL 账号密码登录**时（示例，按师傅实际改）：

```
Server=服务器名或IP;Database=CloudWarehouse;User Id=cloudwh;Password=***;TrustServerCertificate=True
```

---

## 五、服务器：改配置

编辑 **`C:\inetpub\CloudWarehouse\appsettings.json`**（发布目录里的那份）：

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "这里改成服务器能连上的 SQL 连接串"
  },
  "AllowedHosts": "*"
}
```

保存后不要在本机 `dotnet run`，由 IIS 启动。

> **Logging** 等其它节点保持默认即可。

---

## 六、服务器：IIS 配置（逐步）

### 6.1 启用 IIS（若未启用）

**Windows Server：**

- 服务器管理器 → 添加角色和功能 → **Web 服务器 (IIS)** → 至少勾选：
  - 静态内容
  - 默认文档
  - ASP.NET（若列表里有，可勾；Core 主要靠 Hosting Bundle）

**Windows 10/11 当服务器：**

- 控制面板 → 程序 → 启用或关闭 Windows 功能 → **Internet Information Services**

### 6.2 安装 Hosting Bundle

- 运行 **.NET 9 Hosting Bundle** 安装程序  
- 装完执行：`iisreset`

### 6.3 建应用程序池

1. 打开 **IIS 管理器**（`inetmgr`）  
2. **应用程序池** → 添加应用程序池  
   - 名称：`CloudWarehouse`  
   - **.NET CLR 版本：`无托管代码`**（重要）  
   - 托管管道：集成  

### 6.4 建网站

1. **网站** → 添加网站  
   - 网站名：`CloudWarehouse`  
   - 应用程序池：选上面的 `CloudWarehouse`  
   - **物理路径：** `C:\inetpub\CloudWarehouse`（publish 解压目录，内含 `wwwroot` 和 exe）  
   - **绑定：**  
     - 类型 http  
     - 端口 **80**（或师傅指定，如 5001）  
     - 主机名可留空（用 IP 访问）  

2. 确认站点已**启动**（绿色播放图标）。

### 6.5 文件夹权限

应用程序池默认身份多为 `IIS AppPool\CloudWarehouse`：

- 给 `C:\inetpub\CloudWarehouse` 文件夹：**读取 + 执行**  
- 若写日志到该目录，再加**修改**权限  

（右键文件夹 → 属性 → 安全 → 编辑 → 添加 `IIS AppPool\CloudWarehouse`）

### 6.6 验证

在服务器浏览器或你电脑浏览器访问：

```
http://服务器IP/
http://服务器IP:端口/
```

应看到 **云仓管理系统** 首页。  
若 500.30 / 500.31 错误，多半是 **没装 Hosting Bundle** 或应用程序池不是「无托管代码」。

---

## 七、常见问题

| 现象 | 处理 |
|------|------|
| HTTP 500.30 | 安装/重装 .NET 9 **Hosting Bundle**，`iisreset` |
| HTTP 500.31 | 应用程序池 → **无托管代码** |
| 页面能开，导入/试算报错 | 检查 `appsettings.json` 连接串、SQL 是否允许远程/本机连接 |
| 只能本机访问 | 服务器防火墙入站规则放行 80（或你的端口） |
| 静态页有，API 404 | 确认站点根路径就是 publish 根目录（里面有 `wwwroot`），不要只指到 `wwwroot` 子文件夹 |

### 查看 IIS 日志

- IIS 日志：`C:\inetpub\logs\LogFiles\`  
- 应用 stdout 日志（可选）：在 `web.config`（publish 会自动生成）里配置 `stdoutLogEnabled="true"`

---

## 八、和本机开发的对比（给师傅讲）

| | 本机 `dotnet run` | IIS 部署 |
|--|-------------------|----------|
| 命令 | `dotnet run` | IIS 托管已发布的 exe |
| 地址 | http://localhost:5001 | http://服务器IP:80 |
| 用途 | 开发、截图 | 给别人长期访问 |
| SQL | 本机 SQL | 服务器 SQL |

---

## 九、可选：打成 MSI 吗？

**可以，但不是必须；工作量明显更大。**

| 你需要 | 说明 |
|--------|------|
| WiX Toolset 或 Advanced Installer 等 | 把 `publish` 目录封进 MSI，并写安装脚本 |
| 自定义动作 | 安装后仍要在服务器装 **Hosting Bundle + IIS 站点**（MSI 通常只拷文件，不自动配 IIS） |
| 证书/签名 | 企业环境可能要求签名，实习项目常省略 |

**更务实的「像安装包」做法：**

- 交付 **`CloudWarehouse-release.zip` + 本清单 PDF/MD**  
- 或写一个 **`install.ps1`**（解压 zip、改连接串提示、打开 IIS 管理器链接）  

若师傅坚持要 `.msi`，再说一下，可单独加 `docs/project-management/deploy-msi-notes.md`（WiX 最小示例）。

---

## 十、一键命令速查（复制用）

**本机发布：**

```powershell
cd D:\tools\cloudwarehouse-csharp
dotnet publish CloudWarehouse.Backend\CloudWarehouse.Backend.csproj -c Release -o .\publish\CloudWarehouse
Compress-Archive -Path .\publish\CloudWarehouse -DestinationPath .\publish\CloudWarehouse-release.zip -Force
```

**服务器（装完 Hosting Bundle 后）：**

```powershell
# 解压 zip 到 C:\inetpub\CloudWarehouse
# 编辑 appsettings.json 连接串
iisreset
```

---

## 十一、报告/答辩可写一句（英文）

> Production deployment on Windows Server uses framework-dependent `dotnet publish` output hosted in IIS with the .NET 9 Hosting Bundle, connecting to a centralized SQL Server instance. This replaces development-time `dotnet run` on localhost:5001.

---

*文档版本：CloudWarehouse Phase 1 · ASP.NET Core 9 · 框架依赖发布*
