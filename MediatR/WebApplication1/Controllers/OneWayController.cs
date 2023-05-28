using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Handler;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OneWayController : ControllerBase
    {
        private readonly ILogger<OneWayController> _logger;
        private readonly IMediator _mediator;
    
        public OneWayController(IMediator mediator, ILogger<OneWayController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }
    
        [HttpGet(Name = "OneWay")]
        public async Task<OneWayRes> Get()
        {
            await _mediator.Send(new OneWay());
            _logger.LogInformation("OneWay"); // "Pong"
            var res = new OneWayRes
            {
                Msg = "oneWay"
            };
            return res;
        }
    }
}
