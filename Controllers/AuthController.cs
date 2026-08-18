using LogisticsPlatform.API.DTOs;
using LogisticsPlatform.API.Models;
using LogisticsPlatform.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LogisticsPlatform.API.Controllers;

/// <summary>
/// Implements FR-01 (Authentication & Authorization) from the requirement document.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user. Only a SystemAdministrator may create another
    /// SystemAdministrator account; anonymous self-registration is allowed for
    /// operational roles so the demo/dataset can be seeded easily. Tighten this
    /// policy for production use.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterDto dto)
    {
        if (!AppRoles.All.Contains(dto.Role))
        {
            return BadRequest(new { message = $"Role must be one of: {string.Join(", ", AppRoles.All)}" });
        }

        if (dto.Role == AppRoles.SystemAdministrator && !User.IsInRole(AppRoles.SystemAdministrator))
        {
            return Forbid();
        }

        var existing = await _userManager.FindByEmailAsync(dto.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "A user with this email already exists." });
        }

        var user = new ApplicationUser
        {
            UserName = dto.Email,
            Email = dto.Email,
            FullName = dto.FullName
        };

        var result = await _userManager.CreateAsync(user, dto.Password);
        if (!result.Succeeded)
        {
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });
        }

        await _userManager.AddToRoleAsync(user, dto.Role);
        _logger.LogInformation("New user registered: {Email} with role {Role}", dto.Email, dto.Role);

        var (token, expiresAtUtc) = _tokenService.GenerateToken(user, new List<string> { dto.Role });

        return Ok(new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = dto.Role,
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed login attempt for {Email}", dto.Email);
            return Unauthorized(new { message = "Invalid credentials." });
        }

        user.LastLoginUtc = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiresAtUtc) = _tokenService.GenerateToken(user, roles);

        return Ok(new AuthResponseDto
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email!,
            Role = roles.FirstOrDefault() ?? string.Empty,
            Token = token,
            ExpiresAtUtc = expiresAtUtc
        });
    }

    /// <summary>Simple endpoint to verify a bearer token is valid and inspect its claims.</summary>
    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var claims = User.Claims.Select(c => new { c.Type, c.Value });
        return Ok(claims);
    }
}
