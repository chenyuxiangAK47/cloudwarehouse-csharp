# Infrastructure as Code — CloudWarehouse

Report mapping: **§8.8.1 IaC Done**.

| Layer | Path | CI |
|-------|------|-----|
| Azure Bicep | `infra/bicep/main.bicep` | `.github/workflows/iac.yml` (`bicep build`) |
| Terraform | `infra/terraform/main.tf` | `terraform validate` |
| Compose | `docker-compose.yml` | `docker compose config` |
| Image | `Dockerfile` | used by Compose / Trivy-ready |
| DB as code | `database/*.sql` | in Git |
| DAST target | published Backend | `.github/workflows/dast-zap.yml` |

## Local Compose

```bash
docker compose up -d --build
# API http://localhost:8080  SQL localhost:1433 (sa / Your_password123)
```

## Azure (Bicep)

```bash
az group create -n rg-cloudwarehouse-demo -l southeastasia
az deployment group create -g rg-cloudwarehouse-demo \
  -f infra/bicep/main.bicep -p @infra/bicep/parameters.dev.json
```

## Azure (Terraform)

```bash
cd infra/terraform
terraform init
terraform apply -var="sql_admin_password=***"
```
