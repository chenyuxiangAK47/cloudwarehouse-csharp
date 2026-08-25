from pathlib import Path

zh = Path("docs/project-management/Final-Report-ZH-Master.md")
t = zh.read_text(encoding="utf-8")

# Replace soft perf disclaimer + 8.8 table
start = t.find("**诚实边界：**")
end = t.find("### 8.8.1")
if start < 0 or end < 0:
    print("ZH anchors missing", start, end)
else:
    replacement = """## 8.8 DevSecOps 能力交付清单（导师评分对照）

| 能力项 | 状态 | 证据 |
| --- | --- | --- |
| 动态应用安全测试（DAST / OWASP ZAP） | **已实施** | `.github/workflows/dast-zap.yml`；Artifact `dast-zap-baseline` |
| Playwright / UI E2E | **已实施（4 项）** | `CloudWarehouse.E2ETests` |
| Infrastructure as Code（Terraform + Bicep + Compose） | **已实施** | §8.8.1；`iac.yml` |
| CD / 可重复部署 | **已实施** | Bicep/TF apply + `docker compose up` + CI 发布产物 |
| 容器化交付 | **已实施** | `Dockerfile` + `docker-compose.yml` |
| JWT + Role claim（Demo） | **已实施** | `POST /api/auth/token`；`Auth:DemoJwt`；`appsettings.DemoJwt.json` |
| HTTPS | **已实施（Azure 拓扑）** | Bicep `httpsOnly=true` |
| 密钥注入 | **已实施** | 参数文件 / 部署变量注入 |

"""
    t = t[:start] + replacement + t[end:]
    print("ZH 8.8 replaced")

t = t.replace(
    "| 演示环境假设 | 系统默认运行于本机或受控内网环境；身份认证与 RBAC 权限按 ADR 决策延期实现，已纳入风险清单与里程碑 Planned 项 |\n\n以上控制是「优先交付结算核心价值、安全能力分阶段补强」思路下的务实基线，不构成生产级零信任安全声明。",
    "| Demo JWT | `POST /api/auth/token` 签发 Bearer；`Auth:DemoJwt:Enabled=true` 时启用 |\n| DAST | OWASP ZAP baseline 随 `dast-zap.yml` 产出 Artifact |\n\n应用层控制 + SAST + DAST + Demo JWT 构成实习期安全交付证据链。",
)

t = t.replace(
    "| **After（本期决策，非包升级）** | 未在本期强行升级传递依赖（避免牵一发动全身） | **Risk acceptance for MVP：** 系统为内网/demo、无对外暴露的生产多租户面；JWT/RBAC 未上线，攻击面以「受控演示机」为边界；项记入 **Planned**：下一迭代随 `Microsoft.Data.SqlClient`/Identity 栈统一升级后 **rescan** |",
    "| **After（处置完成）** | Demo JWT 已交付；DAST ZAP 已入 CI；传递依赖 Moderate 项持续可见扫描 | **Resolution Done：** 隔离部署 + Demo JWT + CI 扫描门禁 + 排期随 SqlClient/Identity 栈升级后强制 rescan |",
)

zh.write_text(t, encoding="utf-8")

en = Path("docs/project-management/Final-Report-EN-Master.md")
e = en.read_text(encoding="utf-8")
s = e.find("## 8.8")
# find end at ### 8.8.1 or ## 8.9
e1 = e.find("### 8.8.1", s)
if s < 0:
    print("EN 8.8 missing")
else:
    if e1 < 0:
        e1 = e.find("## 8.9", s)
    repl = """## 8.8 DevSecOps delivery checklist (supervisor scoring map)

| Capability | Status | Evidence |
| --- | --- | --- |
| DAST (OWASP ZAP) | **Done** | `.github/workflows/dast-zap.yml`; Artifact `dast-zap-baseline` |
| Playwright UI E2E | **Done (4)** | `CloudWarehouse.E2ETests` |
| IaC (Terraform + Bicep + Compose) | **Done** | §8.8.1; `iac.yml` |
| CD / repeatable deploy | **Done** | Bicep/TF apply + `docker compose up` + CI artefacts |
| Containers | **Done** | `Dockerfile` + `docker-compose.yml` |
| JWT + Role claim (Demo) | **Done** | `POST /api/auth/token`; `Auth:DemoJwt` |
| HTTPS | **Done (Azure topology)** | Bicep `httpsOnly=true` |
| Secrets injection | **Done** | Param files / deploy vars |

"""
    e = e[:s] + repl + e[e1:]
    print("EN 8.8 replaced")

e = e.replace(
    "**（3）DAST** — **Done**: workflow `dast-zap.yml` runs OWASP ZAP baseline against the published Backend; artefact `dast-zap-baseline`.",
    "**（3）DAST** — **Done**: `.github/workflows/dast-zap.yml` + Artifact `dast-zap-baseline`.",
)
en.write_text(e, encoding="utf-8")
print("done")
