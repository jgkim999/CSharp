#!/bin/bash

# MCP Server Setup Script for Amazon Q
# This script installs and configures MCP servers for enhanced development experience

echo "🚀 Setting up MCP servers for Amazon Q..."

# Check if uvx is installed
if ! command -v uvx &> /dev/null; then
    echo "❌ uvx not found. Installing uv first..."
    curl -LsSf https://astral.sh/uv/install.sh | sh
    source ~/.bashrc
fi

# Install MCP servers
echo "📦 Installing MCP servers..."

# Core AWS servers
uvx --from awslabs.aws-diagram-mcp-server aws-diagram-mcp-server --version
uvx --from awslabs.eks-mcp-server eks-mcp-server --version
uvx --from awslabs-ecs-mcp-server ecs-mcp-server --version

# Development tools
uvx mcp-server-fetch --version
uvx mcp-server-git --version
uvx mcp-server-filesystem --version
uvx mcp-server-docker --version
uvx mcp-server-sqlite --version

# Optional servers (require API keys)
echo "⚠️  Optional servers (require API keys):"
echo "   - mcp-server-brave-search (requires Brave API key)"
echo "   - mcp-server-github (requires GitHub token)"

# Check Docker
if ! command -v docker &> /dev/null; then
    echo "⚠️  Docker not found. Terraform MCP server requires Docker."
else
    echo "✅ Docker found - Terraform MCP server ready"
fi

echo ""
echo "✅ MCP servers installation complete!"
echo ""
echo "📋 Next steps:"
echo "1. Copy mcp-enhanced.json to your Amazon Q configuration"
echo "2. Add API keys for optional services:"
echo "   - Brave Search API: https://api.search.brave.com/"
echo "   - GitHub Token: https://github.com/settings/tokens"
echo "3. Restart Amazon Q to load the new configuration"
echo ""
echo "🔧 Configuration file: .kiro/settings/mcp-enhanced.json"