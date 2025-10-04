# MCP Server Setup Guide for Amazon Q

## Overview
Model Context Protocol (MCP) extends Amazon Q's capabilities by connecting to external tools and services. This guide helps you set up MCP servers for enhanced .NET development.

## Quick Start

### 1. Install MCP Servers
```bash
cd /Volumes/d/github/CSharp/.kiro/settings
./setup-mcp.sh
```

### 2. Configure Amazon Q
Copy the enhanced configuration to your Amazon Q settings:
- Location: `~/.config/amazonq/mcp.json` (Linux/Mac) or `%APPDATA%\amazonq\mcp.json` (Windows)
- Use the `mcp-enhanced.json` file as your template

### 3. Restart Amazon Q
Restart Amazon Q to load the new MCP servers.

## Available MCP Servers

### 🏗️ Infrastructure & Cloud
- **aws-diagram**: Generate AWS architecture diagrams
- **aws-eks**: Manage EKS clusters and Kubernetes resources
- **aws-ecs**: Manage ECS services and containers
- **terraform**: Infrastructure as Code management

### 🛠️ Development Tools
- **filesystem**: Enhanced file operations
- **git**: Advanced Git operations
- **docker**: Container management
- **database**: SQLite database operations

### 🌐 External Services (Optional)
- **fetch**: HTTP requests and API calls
- **web-search**: Brave Search integration
- **github**: GitHub repository management

## Configuration Details

### Core Servers (Always Enabled)
```json
{
  "aws-diagram": {
    "autoApprove": ["generate_diagram"]
  },
  "filesystem": {
    "env": {
      "ALLOWED_DIRECTORIES": "/Volumes/d/github/CSharp"
    }
  }
}
```

### Optional Servers (Require API Keys)
```json
{
  "web-search": {
    "env": {
      "BRAVE_API_KEY": "your-api-key"
    },
    "disabled": true
  },
  "github": {
    "env": {
      "GITHUB_PERSONAL_ACCESS_TOKEN": "your-token"
    },
    "disabled": true
  }
}
```

## API Keys Setup

### Brave Search API
1. Visit: https://api.search.brave.com/
2. Create account and get API key
3. Add to configuration: `"BRAVE_API_KEY": "your-key"`
4. Set `"disabled": false`

### GitHub Token
1. Visit: https://github.com/settings/tokens
2. Generate new token with repo permissions
3. Add to configuration: `"GITHUB_PERSONAL_ACCESS_TOKEN": "your-token"`
4. Set `"disabled": false`

## Troubleshooting

### Common Issues
1. **uvx not found**: Install uv first: `curl -LsSf https://astral.sh/uv/install.sh | sh`
2. **Docker not available**: Install Docker for Terraform MCP server
3. **Permission denied**: Run `chmod +x setup-mcp.sh`

### Verification
Test MCP servers are working:
```bash
# Test AWS diagram server
uvx awslabs.aws-diagram-mcp-server@latest --version

# Test filesystem server
uvx mcp-server-filesystem --version
```

## Usage Examples

### Generate AWS Architecture Diagram
```
@aws-diagram Create a diagram showing ECS service with load balancer
```

### Kubernetes Operations
```
@aws-eks List pods in my EKS cluster
```

### File Operations
```
@filesystem Search for all .csproj files in the project
```

### Git Operations
```
@git Show recent commits with detailed information
```

## Security Considerations

- MCP servers run with limited permissions
- File system access is restricted to allowed directories
- API keys should be stored securely
- Review auto-approve settings carefully

## Next Steps

1. Run the setup script
2. Configure API keys for optional services
3. Test MCP servers with Amazon Q
4. Explore advanced MCP server configurations
5. Consider creating custom MCP servers for specific needs

## Resources

- [MCP Specification](https://modelcontextprotocol.io/)
- [AWS MCP Servers](https://github.com/awslabs/)
- [Community MCP Servers](https://github.com/modelcontextprotocol/servers)