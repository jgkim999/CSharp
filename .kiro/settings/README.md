# MCP

## Windows

```json
{
  "mcpServers": {
    "aws-diagram": {
      "command": "python",
      "args": [
        "-m",
        "awslabs.aws_diagram_mcp_server.server"
      ],
      "env": {
        "FASTMCP_LOG_LEVEL": "ERROR"
      },
      "disabled": false,
      "autoApprove": [
        "generate_diagram"
      ]
    },
    "aws-eks": {
      "command": "python",
      "args": [
        "-m",
        "awslabs.eks_mcp_server.server"
      ],
      "env": {
        "FASTMCP_LOG_LEVEL": "ERROR"
      },
      "disabled": false,
      "autoApprove": []
    },
    "terraform": {
      "command": "docker",
      "args": [
        "run",
        "-i",
        "--rm",
        "hashicorp/terraform-mcp-server"
      ],
      "disabled": false,
      "autoApprove": []
    },
    "fetch": {
      "command": "python",
      "args": [
        "-m",
        "mcp_server_fetch"
      ],
      "env": {
        "FASTMCP_LOG_LEVEL": "ERROR"
      },
      "disabled": false,
      "autoApprove": []
    },
    "git": {
      "command": "python",
      "args": [
        "-m",
        "mcp_server_git"
      ]
    }
  }
}
```
