# Infrastructure as Code — CloudWarehouse
#
# Layers delivered:
# 1. docker-compose.yml          — local/demo SQL + API topology
# 2. infra/bicep/main.bicep      — Azure App Service + SQL + App Insights
# 3. infra/terraform/main.tf     — same Azure topology (Terraform alternative)
# 4. database/*.sql              — schema as code
# 5. .github/workflows/*         — pipeline as code
#
# Validate (CI also runs these):
#   az bicep build --file infra/bicep/main.bicep
#   terraform -chdir=infra/terraform init -backend=false && terraform -chdir=infra/terraform validate
#   docker compose config
#
# Deploy Azure (requires subscription + login):
#   az group create -n rg-cloudwarehouse-demo -l southeastasia
#   az deployment group create -g rg-cloudwarehouse-demo -f infra/bicep/main.bicep -p @infra/bicep/parameters.dev.json
