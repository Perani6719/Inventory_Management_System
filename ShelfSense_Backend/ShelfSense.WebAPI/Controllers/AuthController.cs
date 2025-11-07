using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using ShelfSense.Application.DTOs;
using ShelfSense.Application.DTOs.Auth;
using ShelfSense.Application.Interfaces;
using ShelfSense.Application.Services.Auth;
using ShelfSense.Domain.Identity;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.EntityFrameworkCore; // Needed for SingleOrDefaultAsync on UserManager.Users

namespace ShelfSense.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly JwtTokenService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IBackgroundJobClient _jobClient;
        private readonly ILogger<AuthController> _logger;

        // ---------------- CONSTRUCTOR ----------------
        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            JwtTokenService jwtService,
            IEmailService emailService,
            IBackgroundJobClient jobClient,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService = jwtService;
            _emailService = emailService;
            _jobClient = jobClient;
            _logger = logger;
        }
 
        // ---------------- LOGIN ----------------

        [AllowAnonymous]
        [HttpPost("login")]
       
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

            if (User.Identity.IsAuthenticated)
            {
                return BadRequest(new { message = "You are already logged in." });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login attempt failed due to invalid ModelState for email: {Email}", dto.Email);
                return BadRequest(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(dto.Email);

            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for email: {Email}", dto.Email);
                return Unauthorized(new
                {
                    message = "Invalid credentials",
                    status = 401
                });
            }


            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Login failed: Account locked out for user: {Email}", dto.Email);
                return Unauthorized(new
                {
                    message = "Account temporarily locked due to multiple failed login attempts. Please try again later."
                });
            }

            if (!await _userManager.CheckPasswordAsync(user, dto.Password))
            {
                await _userManager.AccessFailedAsync(user);

                var maxAttempts = 5; // Assuming a max attempt count is configured
                var currentAttempts = await _userManager.GetAccessFailedCountAsync(user);

                string attemptsMessage = string.Empty;
                if (currentAttempts < maxAttempts && await _userManager.GetLockoutEnabledAsync(user))
                {
                    attemptsMessage = $" {maxAttempts - currentAttempts} attempt(s) remaining before potential lockout.";
                }

                _logger.LogWarning("Login failed: Invalid password for user {Email}. Attempts: {Attempts}/{MaxAttempts}", dto.Email, currentAttempts, maxAttempts);
                return Unauthorized(new
                {
                    message = $"Invalid credentials.{attemptsMessage}"
                });
            }

            await _userManager.ResetAccessFailedCountAsync(user);
            _logger.LogInformation("User logged in successfully: {Email}", dto.Email);

            var roles = await _userManager.GetRolesAsync(user);
            var token = _jwtService.GenerateToken(user, roles);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Store the initial refresh token
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); // Set an initial, long expiration
            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                message = $"Welcome back, {user.UserName}!",
                token,
                refreshToken
            });
        }


        // ---------------- REFRESH TOKEN (Non-Rotating) ----------------
        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRefreshDto dto)
        {
            // We only log the start of the token for security/privacy
            _logger.LogInformation("Token refresh attempted with refresh token starting with: {RefreshTokenStart}...",
                dto.RefreshToken.Length > 10 ? dto.RefreshToken.Substring(0, 10) : "short token");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Token refresh failed: Invalid ModelState.");
                return BadRequest(ModelState);
            }

            // 1. Find the user by the refresh token
            // We query the database (via UserManager.Users) for the user whose stored refresh token matches the provided one.
            var user = await _userManager.Users.SingleOrDefaultAsync(
                u => u.RefreshToken == dto.RefreshToken
            );

            // 2. Validate the user and refresh token
            if (user == null)
            {
                _logger.LogWarning("Token refresh failed: Refresh token not found in database (invalid token).");
                return Unauthorized(new { error = "Invalid refresh token." });
            }

            if (user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Token refresh failed: Refresh token expired for user {Email}.", user.Email);

                // Recommended security: Clear the expired refresh token from the database
                user.RefreshToken = null;
                //user.RefreshTokenExpiryTime = null;
                await _userManager.UpdateAsync(user);

                return Unauthorized(new { error = "Expired refresh token. Please log in again." });
            }

            // 3. Generate ONLY the new Access Token
            var roles = await _userManager.GetRolesAsync(user);
            var newAccessToken = _jwtService.GenerateToken(user, roles);

            // 4. *** Non-Rotating Strategy: Do NOT update the RefreshToken or its ExpiryTime ***
            // The client continues to use the same refresh token until the original expiry.

            _logger.LogInformation("Token refresh successful (Non-rotating) for user {Email}. Access token issued.", user.Email);

            // 5. Return the new access token and the EXISTING refresh token
            return Ok(new
            {
                token = newAccessToken,
                // Return the existing refresh token
                refreshToken = user.RefreshToken
            });
        }

        // ---------------- WHOAMI ----------------
        [Authorize]
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            var username = User.Identity?.Name ?? "Unknown";
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var name = User.FindFirst("name")?.Value;
            var storeId = User.FindFirst("store_id")?.Value;

            return Ok(new
            {
                message = $"Welcome {name ?? username}",
                username,
                name,
                roles,
                storeId
            });
        }
    }
}