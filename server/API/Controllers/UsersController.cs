using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Web.Http.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("[controller]")]
public class UsersController : ControllerBase 
{
    private readonly DatabaseContext _context;
    private readonly IConfiguration _configuration;
    public UsersController(IConfiguration configuration, DatabaseContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<ActionResult<string>> Register([FromBody]RegisterViewModel registerViewModel)
    {
        CreatePasswordHash(registerViewModel.Password, out byte[] passwordHash, out byte[] passwordSalt);

        var user = new User 
        {
            Email = registerViewModel.Email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        return Ok();
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login([FromBody]LoginViewModel loginViewModel)
    {
        var user = _context.Users.FirstOrDefault(x => x.Email == loginViewModel.Email);

        if (user == null)
        {
            return BadRequest("Kullanıcı Bulunamadı.");
        }

        if (!VerifyPassword(loginViewModel.Password, user.PasswordHash, user.PasswordSalt))
        {
            return BadRequest("Şifre Yanlış.");
        }

        var token = CreateToken(user);

        return Ok(token);
    }

    /// Token'ı decode etmek lazım.
    [HttpPost("verify")]
    public async Task<ActionResult<string>> Verify()
    {
        var token = HttpContext.Request.Headers["Authorization"];
        string[] tokenSplitted = token[0].Split(' ');
        var handler = new JwtSecurityTokenHandler();
        var jwtTokenDecoded = handler.ReadJwtToken(tokenSplitted[1]);

        var a = DateTime.Now.AddHours(-3);

        if (jwtTokenDecoded.Payload.ValidTo < DateTime.Now.AddHours(-3)) {
            return BadRequest("Token is expired");
        }

        // SymmetricSecurityKey symmetricSecurityKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
        //     _configuration.GetSection("AppSettings:Token").Value
        // ));
        
        // JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            
        // TokenValidationParameters tokenValidationParameters = new TokenValidationParameters()
        // {
        //     ValidateIssuerSigningKey = true,
        //     IssuerSigningKey = symmetricSecurityKey
        // };
        // try {
        //     tokenHandler.ValidateToken(tokenSplitted[1], tokenValidationParameters, out SecurityToken validatedToken);
        // } 
        // catch {
        //     var tokenExpiresAt = validatedToken.ValidTo;
        // }

        return Ok();
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt) {
        using (var hmac = new HMACSHA512()) {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }

    private bool VerifyPassword(string password, byte[] passwordHash, byte[] passwordSalt) {
        using (var hmac = new HMACSHA512(passwordSalt))
        {
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }
    }

    private string CreateToken(User user)
    {
        var claims = new [] 
        {
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("Id", user.Id.ToString())
        };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
            _configuration.GetSection("AppSettings:Token").Value
        ));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddMinutes(30),
            signingCredentials: creds
        );

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }
}