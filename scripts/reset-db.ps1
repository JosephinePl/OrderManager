# Drops and recreates the development database.
# Run this from the repository root before e2e tests when starting from a dirty state.
# Requires the dotnet-ef tool: dotnet tool install --global dotnet-ef

$project = "src/OrderManager.Data"
$startup = "src/OrderManager.Api"

dotnet ef database drop --force --project $project --startup-project $startup
dotnet ef database update --project $project --startup-project $startup