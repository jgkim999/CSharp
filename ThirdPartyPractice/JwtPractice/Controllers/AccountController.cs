using JwtPractice.Models;
using JwtPractice.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JwtPractice.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class AccountController : Controller
{
    private readonly JwtSettings jwtSettings;

    public AccountController(JwtSettings jwtSettings)
    {
        this.jwtSettings = jwtSettings;
    }

    private IEnumerable<Users> logins = new List<Users>()
    {
        new Users()
        {
            Id = Guid.NewGuid(),
            EmailId = "adminakp@gmail.com",
            UserName = "Admin",
            Password = "Admin",
        },
        new Users()
        {
            Id = Guid.NewGuid(),
            EmailId = "adminakp@gmail.com",
            UserName = "User1",
            Password = "Admin",
        }
    };

    [HttpPost]
    public IActionResult GetToken(UserLogins userLogins)
    {
        try
        {
            var Token = new UserTokens();
            var Valid = logins.Any(x => x.UserName.Equals(userLogins.UserName, StringComparison.OrdinalIgnoreCase));
            if (Valid)
            {
                var user = logins.FirstOrDefault(x =>
                    x.UserName.Equals(userLogins.UserName, StringComparison.OrdinalIgnoreCase));
                Token = JwtHelpers.GenTokenkey(new UserTokens()
                {
                    EmailId = user.EmailId,
                    GuidId = Guid.NewGuid(),
                    UserName = user.UserName,
                    Id = user.Id,
                }, jwtSettings);
            }
            else
            {
                return BadRequest($"wrong password");
            }

            return Ok(Token);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    /// <summary>
    /// Get List of UserAccounts
    /// </summary>
    /// <returns>List Of UserAccounts</returns>
    [HttpGet]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
    public IActionResult GetList()
    {
        return Ok(logins);
    }
}