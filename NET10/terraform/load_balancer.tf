# Application Load Balancer
resource "aws_lb" "gamepulse_alb" {
  name               = "${var.project_name}-alb"
  internal           = false
  load_balancer_type = "application"
  security_groups    = [aws_security_group.alb_sg.id]
  subnets            = aws_subnet.public_subnets[*].id

  enable_deletion_protection = false

  tags = {
    Name        = "${var.project_name}-alb"
    Environment = var.environment
  }
}

# Target Group
resource "aws_lb_target_group" "gamepulse_tg" {
  name        = "${var.project_name}-tg"
  port        = var.container_port
  protocol    = "HTTP"
  vpc_id      = aws_vpc.gamepulse_vpc.id
  target_type = "ip"

  health_check {
    enabled             = true
    healthy_threshold   = 2
    interval            = 30
    matcher             = "200"
    path                = var.health_check_path
    port                = "traffic-port"
    protocol            = "HTTP"
    timeout             = 5
    unhealthy_threshold = 2
  }

  tags = {
    Name        = "${var.project_name}-tg"
    Environment = var.environment
  }
}

# ALB Listener
resource "aws_lb_listener" "gamepulse_listener" {
  load_balancer_arn = aws_lb.gamepulse_alb.arn
  port              = "80"
  protocol          = "HTTP"

  default_action {
    type             = "forward"
    target_group_arn = aws_lb_target_group.gamepulse_tg.arn
  }

  tags = {
    Name        = "${var.project_name}-listener"
    Environment = var.environment
  }
}