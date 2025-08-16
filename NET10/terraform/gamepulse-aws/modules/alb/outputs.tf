# ALB 모듈 출력값 정의

output "alb_arn" {
  description = "Application Load Balancer ARN"
  value       = aws_lb.main.arn
}

output "alb_dns_name" {
  description = "Application Load Balancer DNS 이름"
  value       = aws_lb.main.dns_name
}

output "alb_zone_id" {
  description = "Application Load Balancer Zone ID"
  value       = aws_lb.main.zone_id
}

output "alb_security_group_id" {
  description = "ALB 보안 그룹 ID"
  value       = aws_security_group.alb.id
}

output "target_group_arn" {
  description = "애플리케이션 타겟 그룹 ARN"
  value       = aws_lb_target_group.app.arn
}

output "target_group_name" {
  description = "애플리케이션 타겟 그룹 이름"
  value       = aws_lb_target_group.app.name
}

output "http_listener_arn" {
  description = "HTTP 리스너 ARN"
  value       = aws_lb_listener.http.arn
}

output "https_listener_arn" {
  description = "HTTPS 리스너 ARN (SSL 인증서가 있는 경우)"
  value       = var.domain_name != "" ? aws_lb_listener.https[0].arn : null
}

output "http_only_listener_arn" {
  description = "HTTP Only 리스너 ARN (SSL 인증서가 없는 경우)"
  value       = var.domain_name == "" ? aws_lb_listener.http_only[0].arn : null
}

output "ssl_certificate_arn" {
  description = "SSL 인증서 ARN (도메인이 설정된 경우)"
  value       = var.domain_name != "" ? aws_acm_certificate.main[0].arn : null
}

output "ssl_certificate_domain_validation_options" {
  description = "SSL 인증서 도메인 검증 옵션"
  value       = var.domain_name != "" ? aws_acm_certificate.main[0].domain_validation_options : null
}

# 모니터링 및 로깅 관련 출력
output "alb_canonical_hosted_zone_id" {
  description = "ALB의 정규 호스팅 영역 ID (Route 53 레코드 생성용)"
  value       = aws_lb.main.zone_id
}

output "alb_vpc_id" {
  description = "ALB가 배치된 VPC ID"
  value       = aws_lb.main.vpc_id
}

output "alb_subnets" {
  description = "ALB가 배치된 서브넷 목록"
  value       = aws_lb.main.subnets
}

# 보안 관련 출력
output "alb_security_group_rules" {
  description = "ALB 보안 그룹 규칙 정보"
  value = {
    ingress_http  = "Port 80 from 0.0.0.0/0"
    ingress_https = "Port 443 from 0.0.0.0/0"
    egress_ecs    = "Port 8080 to ECS security group"
    egress_https  = "Port 443 to 0.0.0.0/0"
  }
}

# 타겟 그룹 상세 정보
output "target_group_details" {
  description = "타겟 그룹 상세 정보"
  value = {
    arn                   = aws_lb_target_group.app.arn
    name                  = aws_lb_target_group.app.name
    port                  = aws_lb_target_group.app.port
    protocol              = aws_lb_target_group.app.protocol
    target_type           = aws_lb_target_group.app.target_type
    health_check_path     = aws_lb_target_group.app.health_check[0].path
    health_check_port     = aws_lb_target_group.app.health_check[0].port
    health_check_protocol = aws_lb_target_group.app.health_check[0].protocol
  }
}