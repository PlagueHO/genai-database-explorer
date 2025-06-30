#!/bin/bash

# DevContainer Validation Script
echo "🔍 Validating DevContainer setup..."

# Check .NET installation
echo "Checking .NET installation..."
if command -v dotnet &> /dev/null; then
    echo "✅ .NET CLI: $(dotnet --version)"
else
    echo "❌ .NET CLI not found"
fi

# Check Azure CLI
echo "Checking Azure CLI..."
if command -v az &> /dev/null; then
    echo "✅ Azure CLI: $(az --version | head -n1)"
else
    echo "❌ Azure CLI not found"
fi

# Check PowerShell
echo "Checking PowerShell..."
if command -v pwsh &> /dev/null; then
    echo "✅ PowerShell: $(pwsh --version)"
else
    echo "❌ PowerShell not found"
fi

# Check Git
echo "Checking Git..."
if command -v git &> /dev/null; then
    echo "✅ Git: $(git --version)"
else
    echo "❌ Git not found"
fi

# Check GitHub CLI
echo "Checking GitHub CLI..."
if command -v gh &> /dev/null; then
    echo "✅ GitHub CLI: $(gh --version | head -n1)"
else
    echo "❌ GitHub CLI not found"
fi

# Check Node.js
echo "Checking Node.js..."
if command -v node &> /dev/null; then
    echo "✅ Node.js: $(node --version)"
else
    echo "❌ Node.js not found"
fi

# Check Docker
echo "Checking Docker..."
if command -v docker &> /dev/null; then
    echo "✅ Docker: $(docker --version)"
else
    echo "❌ Docker not found"
fi

# Check .NET global tools
echo "Checking .NET global tools..."
dotnet tool list -g | grep -E "(httprepl|dotnet-ef|libman|dotnet-outdated|dotnet-format)" || echo "⚠️  Some .NET global tools may not be installed"

echo "🎉 DevContainer validation completed!"
