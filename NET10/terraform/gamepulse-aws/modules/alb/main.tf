# Application Load Balancer 모듈
# ALB, 타겟 그룹, 리스너 규칙, SSL 인증서 구성

# ALB 생성
resource "aws_lb" "main" {
  name               = "${var.project_name}-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb.id]
  subnets            = var.public_subnet_ids

  enable_deletion_protection       = var.enable_deletion_protection
  enable_cross_zone_load_balancing = var.enable_cross_zone_load_balancing
  enable_http2                     = var.enable_http2
  idle_timeout                     = var.idle_timeout
  ip_address_type                  = var.ip_address_type
  drop_invalid_header_fields       = var.drop_invalid_header_fields

  dynamic "access_logs" {
    for_each = var.enable_access_logs ? [1] : []
    content {
      bucket  = var.access_logs_bucket
      prefix  = var.access_logs_prefix
      enabled = true
    }
  }

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-alb"
    Type = "Application Load Balancer"
  })
}

# ALB 보안 그룹
resource "aws_security_group" "alb" {
  name_prefix = "${var.project_name}-alb-"
  vpc_id      = var.vpc_id
  description = "Security group for Application Load Balancer"

  # HTTP 인바운드 (80)
  ingress {
    description = "HTTP"
    from_port   = 80
    to_port     = 80
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # HTTPS 인바운드 (443)
  ingress {
    description = "HTTPS"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  # ECS 컨테이너로의 아웃바운드 (8080)
  egress {
    description     = "To ECS containers"
    from_port       = 8080
    to_port         = 8080
    protocol        = "tcp"
    security_groups = [var.ecs_security_group_id]
  }

  # HTTPS 아웃바운드 (인터넷 접근용)
  egress {
    description = "HTTPS outbound"
    from_port   = 443
    to_port     = 443
    protocol    = "tcp"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-alb-sg"
    Type = "ALB Security Group"
  })

  lifecycle {
    create_before_destroy = true
  }
}

# SSL 인증서 (ACM)
resource "aws_acm_certificate" "main" {
  count = var.domain_name != "" ? 1 : 0

  domain_name       = var.domain_name
  validation_method = "DNS"

  subject_alternative_names = var.subject_alternative_names

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-ssl-cert"
    Type = "SSL Certificate"
  })

  lifecycle {
    create_before_destroy = true
  }
}

# 타겟 그룹 (GamePulse 애플리케이션용)
resource "aws_lb_target_group" "app" {
  name        = "${var.project_name}-app-tg"
  port        = 8080
  protocol    = "HTTP"
  vpc_id      = var.vpc_id
  target_type = "ip"

  health_check {
    enabled             = var.target_group_health_check.enabled
    healthy_threshold   = var.target_group_health_check.healthy_threshold
    interval            = var.target_group_health_check.interval
    matcher             = var.target_group_health_check.matcher
    path                = var.target_group_health_check.path
    port                = var.target_group_health_check.port
    protocol            = var.target_group_health_check.protocol
    timeout             = var.target_group_health_check.timeout
    unhealthy_threshold = var.target_group_health_check.unhealthy_threshold
  }

  dynamic "stickiness" {
    for_each = var.enable_stickiness ? [1] : []
    content {
      type            = "lb_cookie"
      cookie_duration = var.stickiness_duration
      enabled         = true
    }
  }

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-app-tg"
    Type = "Target Group"
  })
}

# HTTP 리스너 (HTTPS로 리다이렉트)
resource "aws_lb_listener" "http" {
  load_balancer_arn = aws_lb.main.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type = "redirect"

    redirect {
      port        = "443"
      protocol    = "HTTPS"
      status_code = "HTTP_301"
    }
  }

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-http-listener"
    Type = "HTTP Listener"
  })
}

# HTTPS 리스너
resource "aws_lb_listener" "https" {
  count = var.domain_name != "" ? 1 : 0

  load_balancer_arn = aws_lb.main.arn
  port              = "443"
  protocol          = "HTTPS"
  ssl_policy        = var.ssl_policy
  certificate_arn   = aws_acm_certificate.main[0].arn

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.app.arn
  }

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-https-listener"
    Type = "HTTPS Listener"
  })

  depends_on = [aws_acm_certificate.main]
}

# HTTP 리스너 (SSL 인증서가 없는 경우)
resource "aws_lb_listener" "http_only" {
  count = var.domain_name == "" ? 1 : 0

  load_balancer_arn = aws_lb.main.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.app.arn
  }

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-http-only-listener"
    Type = "HTTP Only Listener"
  })
}

# 추가 리스너 규칙 (동적 생성)
resource "aws_lb_listener_rule" "additional_rules" {
  count = length(var.listener_rules)

  listener_arn = var.domain_name != "" ? aws_lb_listener.https[0].arn : aws_lb_listener.http_only[0].arn
  priority     = var.listener_rules[count.index].priority

  dynamic "condition" {
    for_each = var.listener_rules[count.index].conditions
    content {
      dynamic "path_pattern" {
        for_each = condition.value.field == "path-pattern" ? [condition.value] : []
        content {
          values = condition.value.values
        }
      }

      dynamic "host_header" {
        for_each = condition.value.field == "host-header" ? [condition.value] : []
        content {
          values = condition.value.values
        }
      }

      dynamic "http_header" {
        for_each = condition.value.field == "http-header" ? [condition.value] : []
        content {
          http_header_name = condition.value.values[0]
          values           = slice(condition.value.values, 1, length(condition.value.values))
        }
      }
    }
  }

  dynamic "action" {
    for_each = var.listener_rules[count.index].actions
    content {
      type             = action.value.type
      target_group_arn = action.value.type == "forward" ? action.value.target_group_arn : null

      dynamic "redirect" {
        for_each = action.value.type == "redirect" && action.value.redirect_config != null ? [action.value.redirect_config] : []
        content {
          host        = redirect.value.host
          path        = redirect.value.path
          port        = redirect.value.port
          protocol    = redirect.value.protocol
          query       = redirect.value.query
          status_code = redirect.value.status_code
        }
      }
    }
  }

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-listener-rule-${count.index + 1}"
    Type = "Listener Rule"
  })
}

# CloudWatch 로그 그룹 (ALB 액세스 로그용)
resource "aws_cloudwatch_log_group" "alb_logs" {
  count = var.enable_access_logs ? 1 : 0

  name              = "/aws/alb/${var.project_name}"
  retention_in_days = 30

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-alb-logs"
    Type = "CloudWatch Log Group"
  })
}

# 모니터링용 타겟 그룹 (Grafana, Prometheus 등)
resource "aws_lb_target_group" "monitoring" {
  count = var.create_monitoring_target_groups ? 1 : 0

  name        = "${var.project_name}-monitoring-tg"
  port        = 3000
  protocol    = "HTTP"
  vpc_id      = var.vpc_id
  target_type = "ip"

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = "/api/health"
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 2
  }

  tags = merge(var.common_tags, {
    Name = "${var.project_name}-monitoring-tg"
    Type = "Monitoring Target Group"
  })
}

# WAF 연결 (선택적)
resource "aws_wafv2_web_acl_association" "main" {
  count = var.enable_waf && var.waf_acl_arn != "" ? 1 : 0

  resource_arn = aws_lb.main.arn
  web_acl_arn  = var.waf_acl_arn
}