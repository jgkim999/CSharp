#!/bin/bash

# MCP Server Test Script
echo "🧪 Testing MCP servers..."

# Test core servers
echo "Testing AWS Diagram server..."
if uvx awslabs.aws-diagram-mcp-server@latest --help > /dev/null 2>&1; then
    echo "✅ AWS Diagram server: OK"
else
    echo "❌ AWS Diagram server: FAILED"
fi

echo "Testing EKS server..."
if uvx awslabs.eks-mcp-server@latest --help > /dev/null 2>&1; then
    echo "✅ EKS server: OK"
else
    echo "❌ EKS server: FAILED"
fi

echo "Testing Git server..."
if uvx mcp-server-git --help > /dev/null 2>&1; then
    echo "✅ Git server: OK"
else
    echo "❌ Git server: FAILED"
fi

echo "Testing Filesystem server..."
if uvx mcp-server-filesystem --help > /dev/null 2>&1; then
    echo "✅ Filesystem server: OK"
else
    echo "❌ Filesystem server: FAILED"
fi

echo "Testing Fetch server..."
if uvx mcp-server-fetch --help > /dev/null 2>&1; then
    echo "✅ Fetch server: OK"
else
    echo "❌ Fetch server: FAILED"
fi

# Test Docker
echo "Testing Docker..."
if command -v docker &> /dev/null; then
    echo "✅ Docker: Available"
else
    echo "⚠️  Docker: Not available (needed for Terraform server)"
fi

echo ""
echo "🎉 MCP server test complete!"