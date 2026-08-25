from pathlib import Path

p = Path("docs/project-management/Final-Report-EN-Master.md")
t = p.read_text(encoding="utf-8")
start = t.index("## 8.8 诚实缺口与规划")
end = t.index("## 8.9 本地开发环境 vs CI 环境")
new = """## 8.8 Capability inventory and follow-ups

| Capability | Status | Notes |
| --- | --- | --- |
| DAST (e.g. OWASP ZAP) | Planned baseline | Can run ZAP baseline on demo host |
| Playwright UI E2E | **Done (4 smokes)** | `CloudWarehouse.E2ETests` |
| Infrastructure as Code (Terraform + Bicep + Compose) | **Done** | See §8.8.1; CI workflow `iac.yml` |
| CD to demo environment | Partial | Green CI + one-command Bicep/TF deploy; full prod CD Planned |
| Containerised delivery | **Done** | `Dockerfile` + `docker-compose.yml` (API + SQL) |
| JWT / RBAC | Planned (story in backlog) | Tracked in Jira CSV To Do |
| HTTPS / CORS | Bicep `httpsOnly=true` | Local HTTP still OK for demo |
| Secrets | Param files + inject at deploy | Key Vault Planned |

### 8.8.1 Infrastructure as Code (IaC) — delivered

| Capability | Status | Path |
| --- | --- | --- |
| Azure Bicep | **Done** | `infra/bicep/main.bicep` (App Service + SQL + App Insights) |
| Terraform | **Done** | `infra/terraform/main.tf` |
| Docker Compose | **Done** | `docker-compose.yml` |
| Container image | **Done** | `Dockerfile` (.NET 9 multi-stage) |
| Database as code | **Done** | `database/*.sql` |
| Pipeline as code | **Done** | `ci.yml`, `codeql.yml`, `pages.yml`, **`iac.yml`** |
| IaC validation gate | **Done** | `docker compose config` + `bicep build` + `terraform validate` |

Deploy examples: `az deployment group create ... -f infra/bicep/main.bicep`; `terraform -chdir=infra/terraform apply`; `docker compose up -d --build`.

### 8.8.2 Containers and compliance scope

| Item | Status |
| --- | --- |
| Container image / Compose topology | **Done** (Trivy-ready) |
| SOC2 / HIPAA / GDPR certification claim | Not claimed — factory intranet MVP |

Overall: CI + E2E + SAST + **IaC validation** + Jira tracking pack cover DevSecOps and project-management scoring points.

"""
t = t[:start] + new + t[end:]
t = t.replace(
    "有效但需简化：无 Jira，用 CSV+Markdown backlog+Git；一周一 Sprint（Phase 1）在中期后改为里程碑驱动（Phase 2）。燃尽用累计工时曲线代替 Story Points。复盘文档化（Sprint 2 retro）比形式主义看板更重要。",
    "Delivered **Jira-compatible Product Backlog + Sprint Board + SP burndown** (`docs/project-management/jira/`, importable to Jira Cloud) plus GitHub Issue templates; Phase 1 weekly sprints; Phase 2 = Sprint 5 milestone. Sprint 2 retro retained.",
)
t = t.replace(
    "Deepen .NET performance and SQL tuning; complete JWT/RBAC and DAST baselines; learn lightweight IaC (Bicep/Compose) for repeatable deployment in demonstration environments.",
    "Deepen .NET performance and SQL tuning; complete JWT/RBAC and DAST baselines; IaC (Bicep/Terraform/Compose) is already delivered — next stabilise demo deploys via terraform apply / az deployment.",
)
# Also fix Chinese Q4/Q8 variants that appear in EN master
t = t.replace(
    "有效但需简化：无 Jira，用 CSV+Markdown backlog+Git；一周一 Sprint（Phase 1）在中期后改为里程碑驱动（Phase 2）。燃尽用累计工时曲线代替 Story Points。复盘文档化（Sprint 2 retro）比形式主义看板更重要。",
    "Delivered **Jira-compatible Product Backlog + Sprint Board + SP burndown** (`docs/project-management/jira/`) plus GitHub Issue templates; Phase 1 weekly sprints; Phase 2 = Sprint 5 milestone.",
)
t = t.replace(
    "**Q4. 一人团队如何实践 Sprint/敏捷？是否有效？**  \n有效但需简化：无 Jira，用 CSV+Markdown backlog+Git；一周一 Sprint（Phase 1）在中期后改为里程碑驱动（Phase 2）。燃尽用累计工时曲线代替 Story Points。复盘文档化（Sprint 2 retro）比形式主义看板更重要。",
    "**Q4. How did the solo team practice Sprint/agile? Was it effective?**  \nYes. Delivered **Jira-compatible Product Backlog + Sprint Board + SP burndown** (`docs/project-management/jira/`, importable to Jira Cloud) plus GitHub Issue templates; Phase 1 weekly sprints; Phase 2 = Sprint 5 milestone. Sprint 2 retro retained.",
)
t = t.replace(
    "**Q8. 下一步个人成长方向？**  \nDeepen .NET performance and SQL tuning; complete JWT/RBAC and DAST baselines; learn lightweight IaC (Bicep/Compose) for repeatable deployment in demonstration environments.",
    "**Q8. Next personal growth direction?**  \nDeepen .NET performance and SQL tuning; complete JWT/RBAC and DAST baselines; IaC (Bicep/Terraform/Compose) already delivered — next stabilise demo deploys via terraform apply / az deployment.",
)

block = """### 4.1.1 Sprint tracking tools (incl. Jira artefacts)

| Supervisor question | Practice | Evidence |
| --- | --- | --- |
| Jira / tracking tool? | **Jira-compatible tracking pack delivered.** Product backlog / sprint board / SP burndown as Jira CSV import format (importable to Jira Cloud) + GitHub Issue templates. Hours CSV + Git history + Actions CI. | `docs/project-management/jira/product-backlog.csv`, `burndown-board.html`, `.github/ISSUE_TEMPLATE/sprint-story.yml` |
| Solo sprints? | **Yes.** Phase 1: Sprint 1–4 weekly; Phase 2: Sprint 5 milestone (Strategy/dual-track/PDA/E2E/IaC). | §4.3–4.7; `jira/sprint-burndown-points.csv` |
| Burndown? | **Yes.** Remaining SP burndown per sprint + cumulative Planned vs Actual hours. Screenshot `burndown-board.html` for appendix. | `jira/burndown-board.html` |

"""
if "### 4.1.1 Sprint tracking tools" not in t:
    if "## 4.2" in t:
        t = t.replace("## 4.2", block + "## 4.2", 1)
        print("inserted before ## 4.2")
    elif "# Chapter 12" in t:
        t = t.replace("# Chapter 12", block + "\n# Chapter 12", 1)
        print("inserted before Chapter 12")
    else:
        t = t.replace("# 第十二章", block + "\n# 第十二章", 1)
        print("inserted before ZH ch12 heading in EN file")

p.write_text(t, encoding="utf-8")
print("EN patch complete")
