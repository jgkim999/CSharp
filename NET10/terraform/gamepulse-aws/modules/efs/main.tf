# EFS 파일 시스템 생성
resource "aws_efs_file_system" "main" {
  creation_token = "${var.project_name}-${var.environment}-efs"
  
  performance_mode = var.performance_mode
  throughput_mode  = var.throughput_mode
  
  # 암호화 설정
  encrypted = true
  kms_key_id = var.kms_key_id
  
  # 라이프사이클 정책
  lifecycle_policy {
    transition_to_ia = var.transition_to_ia
  }
  
  lifecycle_policy {
    transition_to_primary_storage_class = var.transition_to_primary_storage_class
  }
  
  tags = merge(var.tags, {
    Name = "${var.project_name}-${var.environment}-efs"
    Type = "EFS"
  })
}

# EFS 마운트 타겟 생성 (각 프라이빗 서브넷에)
resource "aws_efs_mount_target" "main" {
  count = length(var.private_subnet_ids)
  
  file_system_id  = aws_efs_file_system.main.id
  subnet_id       = var.private_subnet_ids[count.index]
  security_groups = [aws_security_group.efs.id]
}

# EFS 보안 그룹
resource "aws_security_group" "efs" {
  name_prefix = "${var.project_name}-${var.environment}-efs-"
  vpc_id      = var.vpc_id
  
  ingress {
    description = "NFS from ECS"
    from_port   = 2049
    to_port     = 2049
    protocol    = "tcp"
    security_groups = var.allowed_security_groups
  }
  
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
  
  tags = merge(var.tags, {
    Name = "${var.project_name}-${var.environment}-efs-sg"
  })
}

# EFS 액세스 포인트 (Prometheus용)
resource "aws_efs_access_point" "prometheus" {
  file_system_id = aws_efs_file_system.main.id
  
  posix_user {
    gid = 65534
    uid = 65534
  }
  
  root_directory {
    path = "/prometheus"
    creation_info {
      owner_gid   = 65534
      owner_uid   = 65534
      permissions = "0755"
    }
  }
  
  tags = merge(var.tags, {
    Name = "${var.project_name}-${var.environment}-prometheus-ap"
    Service = "prometheus"
  })
}

# EFS 액세스 포인트 (Jaeger용)
resource "aws_efs_access_point" "jaeger" {
  file_system_id = aws_efs_file_system.main.id
  
  posix_user {
    gid = 10001
    uid = 10001
  }
  
  root_directory {
    path = "/jaeger"
    creation_info {
      owner_gid   = 10001
      owner_uid   = 10001
      permissions = "0755"
    }
  }
  
  tags = merge(var.tags, {
    Name = "${var.project_name}-${var.environment}-jaeger-ap"
    Service = "jaeger"
  })
}

# EFS 액세스 포인트 (Grafana용)
resource "aws_efs_access_point" "grafana" {
  file_system_id = aws_efs_file_system.main.id
  
  posix_user {
    gid = 472
    uid = 472
  }
  
  root_directory {
    path = "/grafana"
    creation_info {
      owner_gid   = 472
      owner_uid   = 472
      permissions = "0755"
    }
  }
  
  tags = merge(var.tags, {
    Name = "${var.project_name}-${var.environment}-grafana-ap"
    Service = "grafana"
  })
}