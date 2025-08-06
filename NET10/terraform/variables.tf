# Variables for GamePulse ECS Deployment

variable "aws_region" {
  description = "AWS region for deployment"
  type        = string
  default     = "ap-northeast-2"
}

variable "project_name" {
  description = "Name of the project"
  type        = string
  default     = "gamepulse"
}

variable "environment" {
  description = "Environment name"
  type        = string
  default     = "dev"
}

variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for public subnets"
  type        = list(string)
  default     = ["10.0.1.0/24", "10.0.2.0/24"]
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for private subnets"
  type        = list(string)
  default     = ["10.0.10.0/24", "10.0.20.0/24"]
}

variable "container_port" {
  description = "Port on which the container listens"
  type        = number
  default     = 8080
}

variable "health_check_path" {
  description = "Health check path for the application"
  type        = string
  default     = "/health"
}

variable "cpu" {
  description = "CPU units for the task (1024 = 1 vCPU)"
  type        = number
  default     = 512
}

variable "memory" {
  description = "Memory for the task in MB"
  type        = number
  default     = 1024
}

variable "desired_count" {
  description = "Desired number of tasks"
  type        = number
  default     = 2
}

variable "min_capacity" {
  description = "Minimum number of tasks"
  type        = number
  default     = 1
}

variable "max_capacity" {
  description = "Maximum number of tasks"
  type        = number
  default     = 10
}

variable "ecr_repository_name" {
  description = "Name of the ECR repository"
  type        = string
  default     = "gamepulse"
}

variable "log_retention_days" {
  description = "CloudWatch log retention in days"
  type        = number
  default     = 7
}

# Auto Scaling Variables
variable "cpu_target_value" {
  description = "Target CPU utilization percentage for auto scaling"
  type        = number
  default     = 70.0
}

variable "memory_target_value" {
  description = "Target memory utilization percentage for auto scaling"
  type        = number
  default     = 80.0
}

variable "request_count_target_value" {
  description = "Target request count per target for auto scaling"
  type        = number
  default     = 1000.0
}

variable "scale_in_cooldown" {
  description = "Scale in cooldown period in seconds"
  type        = number
  default     = 300
}

variable "scale_out_cooldown" {
  description = "Scale out cooldown period in seconds"
  type        = number
  default     = 300
}
