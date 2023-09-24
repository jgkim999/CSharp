using Microsoft.AspNetCore.Mvc;

namespace RabbitMqDemo.Controllers;

[ApiController]
public class PubDemo : ControllerBase
{
  private readonly ILogger<PubDemo> logger_;
  
  public PubDemo(ILogger<PubDemo> logger)
    {
        logger_ = logger;
        }
  
}