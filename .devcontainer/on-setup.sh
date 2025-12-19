#!/bin/bash

echo "🔧 Starting DevContainer setup..."

# Configure HTTPS development certificates for .NET (optional, continue on failure)
echo "📜 Configuring HTTPS certificates..."
if sudo mkdir -p /usr/local/share/ca-certificates/aspnet; then
    sudo -E dotnet dev-certs https -ep /usr/local/share/ca-certificates/aspnet/https.crt --format PEM || echo "⚠️  HTTPS certificate configuration failed (optional)"
    sudo update-ca-certificates || echo "⚠️  Certificate update failed (optional)"
else
    echo "⚠️  Could not create certificate directory (optional)"
fi

# Install .NET workloads for Azure development
echo "📦 Installing .NET workloads..."
dotnet workload update || echo "⚠️  Workload update failed, continuing..."
dotnet workload install aspire || echo "⚠️  Aspire workload installation failed, continuing..."

# Install global .NET tools (continue on individual failures)
echo "🔨 Installing .NET global tools..."
# Add retries and better error handling for tool installations
install_dotnet_tool() {
    local tool_name=$1
    local package_name=$2
    local prerelease=${3:-false}
    local prerelease_flag=""
    
    if [ "$prerelease" = "true" ]; then
        prerelease_flag="--prerelease"
    fi
    
    echo "Installing $tool_name..."
    for i in {1..3}; do
        if dotnet tool install -g "$package_name" $prerelease_flag; then
            echo "✅ $tool_name installed successfully"
            return 0
        else
            echo "⚠️  Attempt $i failed for $tool_name, retrying in 5 seconds..."
            sleep 5
        fi
    done
    echo "❌ Failed to install $tool_name after 3 attempts"
    return 1
}

# Install tools with retries
install_dotnet_tool "HttpRepl" "Microsoft.dotnet-httprepl" || true
install_dotnet_tool "Entity Framework Core CLI" "dotnet-ef" || true
install_dotnet_tool "Library Manager CLI" "Microsoft.Web.LibraryManager.Cli" || true
install_dotnet_tool "Outdated Tool" "dotnet-outdated-tool" || true
install_dotnet_tool "Format Tool" "dotnet-format" || true
install_dotnet_tool "Aspire CLI" "aspire" true || true

# Update Azure CLI and install extensions
echo "☁️  Configuring Azure CLI extensions..."
az extension add --name azure-devops 2>/dev/null || echo "⚠️  azure-devops extension failed"
az extension add --name application-insights 2>/dev/null || echo "⚠️  application-insights extension failed"
az extension add --name resource-graph 2>/dev/null || echo "⚠️  resource-graph extension failed"

# Install Azure Developer CLI (azd) if not already present
echo "🌐 Checking Azure Developer CLI..."
if ! command -v azd &> /dev/null; then
    echo "Installing Azure Developer CLI..."
    curl -fsSL https://aka.ms/install-azd.sh | bash || echo "⚠️  azd installation failed"
else
    echo "Azure Developer CLI already installed"
fi

# Configure Git (if not already configured)
echo "🔧 Configuring Git..."
git config --global init.defaultBranch main || echo "⚠️  Git config failed"
git config --global pull.rebase false || echo "⚠️  Git config failed"

# Ensure PowerShell PSReadLine history directory exists and is writable for the 'vscode' user
echo "🧭 Ensuring PowerShell history directory exists and is writable..."
PSHISTORY_DIR="/home/vscode/.local/share/powershell/PSReadLine"
if sudo mkdir -p "$PSHISTORY_DIR"; then
    # Ensure correct ownership and permissions so PowerShell can write history
    sudo chown -R vscode:vscode "$(dirname "$PSHISTORY_DIR")"
    sudo chmod -R u+rwX "$(dirname "$PSHISTORY_DIR")"
    echo "✅ PowerShell history directory ensured: $PSHISTORY_DIR"
else
    echo "⚠️  Could not create PowerShell history directory: $PSHISTORY_DIR"
fi

echo "✅ DevContainer setup completed successfully!"
echo "🚀 Ready for .NET 10 + C# 14 development with Azure tooling"