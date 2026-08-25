// Report §8.5 / §8.8 — Demo JWT scoring artefact (Auth:DemoJwt).
// POST /api/auth/token → Bearer access_token + Admin role claim.
// Enable: Auth:DemoJwt:Enabled=true (see appsettings.DemoJwt.json).
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace CloudWarehouse.Backend.Modules.Security;

public sealed class DemoLoginRequest
{
    public string Username { get; set; } = "demo";
    public string Password { get; set; } = "demo";
}

[ApiController]
[Route("api/auth")]
public sealed class DemoAuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public DemoAuthController(IConfiguration config) => _config = config;

    [HttpPost("token")]
    public ActionResult<object> IssueToken([FromBody] DemoLoginRequest? req)
    {
        var enabled = _config.GetValue("Auth:DemoJwt:Enabled", false);
        if (!enabled)
            return BadRequest(new { message = "Demo JWT is disabled. Set Auth:DemoJwt:Enabled=true to enable." });

        req ??= new DemoLoginRequest();
        var user = _config["Auth:DemoJwt:Username"] ?? "demo";
        var pass = _config["Auth:DemoJwt:Password"] ?? "demo";
        if (!string.Equals(req.Username, user, StringComparison.Ordinal) ||
            !string.Equals(req.Password, pass, StringComparison.Ordinal))
            return Unauthorized(new { message = "Invalid credentials" });

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _config["Auth:DemoJwt:SigningKey"] ?? "CloudWarehouse-Demo-Signing-Key-32chars!!"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "CloudWarehouse.Demo",
            audience: "CloudWarehouse.Demo",
            claims: new[]
            {
                new Claim(ClaimTypes.Name, req.Username),
                new Claim(ClaimTypes.Role, "Admin")
            },
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new
        {
            access_token = new JwtSecurityTokenHandler().WriteToken(token),
            token_type = "Bearer",
            expires_in = 28800,
            role = "Admin"
        });
    }
}
