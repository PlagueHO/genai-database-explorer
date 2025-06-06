#!/bin/bash

# Configure HTTPS development certificates for .NET
sudo -E dotnet dev-certs https -ep /usr/local/share/ca-certificates/aspnet/https.crt --format PEM
sudo update-ca-certificates

# Install .NET workloads for Azure development
dotnet workload update
dotnet workload install aspire

# Install global .NET tools
dotnet tool install -g Microsoft.dotnet-httprepl
dotnet tool install -g dotnet-ef
dotnet tool install -g Microsoft.Web.LibraryManager.Cli
dotnet tool install -g dotnet-outdated-tool
dotnet tool install -g dotnet-format

# Update Azure CLI and install extensions
az extension add --name azure-devops --upgrade
az extension add --name application-insights --upgrade
az extension add --name resource-graph --upgrade

# Install Azure Developer CLI (azd) if not already present
if ! command -v azd &> /dev/null; then
    curl -fsSL https://aka.ms/install-azd.sh | bash
fi

# Configure Git (if not already configured)
git config --global init.defaultBranch main
git config --global pull.rebase false

echo "✅ DevContainer setup completed successfully!"
echo "🚀 Ready for .NET 8 + C# 13 development with Azure tooling"
