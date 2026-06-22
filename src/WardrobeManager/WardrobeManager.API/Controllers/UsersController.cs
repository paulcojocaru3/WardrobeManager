using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WardrobeManager.API.Extensions;
using WardrobeManager.Application.Users.Commands;
using WardrobeManager.Application.Users.Queries;

namespace WardrobeManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(IMediator mediator, IWebHostEnvironment environment) : ControllerBase
{
    private const string AuthCookieName = "wardrobe_auth";

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserCommand command, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(command, ct);
            SetAuthCookie(result.Token);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        if (result == null) return Unauthorized("wrong credentials");
        SetAuthCookie(result.Token);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookieName, BuildCookieOptions());
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserCommand command, CancellationToken ct)
    {
        if (id != User.GetUserId()) return Forbid();
        try
        {
            // trust identity from the token.
            var updated = await mediator.Send(command with { UserId = User.GetUserId() }, ct);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    public sealed class UpdatePreferencesRequest
    {
        public List<string>? FavoriteColors { get; init; }
        public string? PreferredCity { get; init; }
        public string? ThemePreference { get; init; }
        public string? OuterwearMode { get; init; }
        public int? OuterwearTempThreshold { get; init; }
        public List<string>? AvoidColors { get; init; }
        public string? VarietyLevel { get; init; }
        public bool? BlockDuplicateUploads { get; init; }
        public bool? PreferLightOnHotDays { get; init; }
        public bool? UseGemmaStylistForOutfits { get; init; }

        private int? _defaultReuseAfterDays;
        public int? DefaultReuseAfterDays
        {
            get => _defaultReuseAfterDays;
            init
            {
                _defaultReuseAfterDays = value;
                HasDefaultReuseAfterDays = true;
            }
        }

        public bool HasDefaultReuseAfterDays { get; private init; }
    }

    [HttpPut("{id:guid}/preferences")]
    public async Task<IActionResult> UpdatePreferences(Guid id, [FromBody] UpdatePreferencesRequest request, CancellationToken ct)
    {
        if (id != User.GetUserId()) return Forbid();
        var updated = await mediator.Send(new UpdateUserPreferencesCommand(
            User.GetUserId(), request.FavoriteColors, request.PreferredCity, request.ThemePreference,
            request.OuterwearMode, request.OuterwearTempThreshold,
            request.AvoidColors, request.VarietyLevel,
            request.BlockDuplicateUploads, request.PreferLightOnHotDays, request.UseGemmaStylistForOutfits,
            request.DefaultReuseAfterDays, request.HasDefaultReuseAfterDays), ct);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        if (id != User.GetUserId()) return Forbid();
        await mediator.Send(new DeleteUserCommand(User.GetUserId()), ct);
        Response.Cookies.Delete(AuthCookieName, BuildCookieOptions());
        return NoContent();
    }

    private void SetAuthCookie(string token)
    {
        Response.Cookies.Append(AuthCookieName, token, BuildCookieOptions());
    }

    private CookieOptions BuildCookieOptions()
    {
        // Always mark the auth cookie Secure in real deployments; only relax it for local
        // HTTP development so the cookie can still round-trip there.
        var secure = !environment.IsDevelopment() || Request.IsHttps;

        return new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            // SameSite=None requires Secure; fall back to Lax only on the insecure dev cookie.
            SameSite = secure ? SameSiteMode.None : SameSiteMode.Lax,
            MaxAge = TimeSpan.FromHours(24),
            Path = "/"
        };
    }
}
