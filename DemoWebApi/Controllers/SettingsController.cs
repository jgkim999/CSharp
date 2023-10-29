using DemoApplication.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace DemoWebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class SettingsController : ControllerBase
    {
        private readonly ILogger<SettingsController> _logger;
        private readonly IOptions<FormatSettings> _formatSetting;
        private readonly IOptions<DatabaseSettings> _databasesSetting;
        
        public SettingsController(
            ILogger<SettingsController> logger,
            IOptions<FormatSettings> formatSetting,
            IOptions<DatabaseSettings> databasesSetting)
        {
            _logger = logger;
            _formatSetting = formatSetting;
            _databasesSetting = databasesSetting;
        }
        
        [HttpGet]
        public ActionResult<string> GetFormatSetting()
        {
            return Ok(JsonConvert.SerializeObject(_formatSetting.Value));
        }
        
        [HttpGet]
        public ActionResult<string> GetDatabaseSetting()
        {
            return Ok(JsonConvert.SerializeObject(_databasesSetting.Value));
        }
    }
}
