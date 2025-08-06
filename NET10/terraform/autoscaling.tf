# Auto Scaling Target
resource "aws_appautoscaling_target" "gamepulse_target" {
  max_capacity       = var.max_capacity
  min_capacity       = var.min_capacity
  resource_id        = "service/${aws_ecs_cluster.gamepulse_cluster.name}/${aws_ecs_service.gamepulse_service.name}"
  scalable_dimension = "ecs:service:DesiredCount"
  service_namespace  = "ecs"

  tags = {
    Name        = "${var.project_name}-autoscaling-target"
    Environment = var.environment
  }
}

# Auto Scaling Policy - CPU
resource "aws_appautoscaling_policy" "gamepulse_cpu_policy" {
  name               = "${var.project_name}-cpu-scaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.gamepulse_target.resource_id
  scalable_dimension = aws_appautoscaling_target.gamepulse_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.gamepulse_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageCPUUtilization"
    }
    target_value       = var.cpu_target_value
    scale_in_cooldown  = var.scale_in_cooldown
    scale_out_cooldown = var.scale_out_cooldown
  }
}

# Auto Scaling Policy - Memory
resource "aws_appautoscaling_policy" "gamepulse_memory_policy" {
  name               = "${var.project_name}-memory-scaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.gamepulse_target.resource_id
  scalable_dimension = aws_appautoscaling_target.gamepulse_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.gamepulse_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ECSServiceAverageMemoryUtilization"
    }
    target_value       = var.memory_target_value
    scale_in_cooldown  = var.scale_in_cooldown
    scale_out_cooldown = var.scale_out_cooldown
  }
}

# Auto Scaling Policy - ALB Request Count
resource "aws_appautoscaling_policy" "gamepulse_request_policy" {
  name               = "${var.project_name}-request-scaling"
  policy_type        = "TargetTrackingScaling"
  resource_id        = aws_appautoscaling_target.gamepulse_target.resource_id
  scalable_dimension = aws_appautoscaling_target.gamepulse_target.scalable_dimension
  service_namespace  = aws_appautoscaling_target.gamepulse_target.service_namespace

  target_tracking_scaling_policy_configuration {
    predefined_metric_specification {
      predefined_metric_type = "ALBRequestCountPerTarget"
      resource_label         = "${aws_lb.gamepulse_alb.arn_suffix}/${aws_lb_target_group.gamepulse_tg.arn_suffix}"
    }
    target_value       = var.request_count_target_value
    scale_in_cooldown  = var.scale_in_cooldown
    scale_out_cooldown = var.scale_out_cooldown
  }
}