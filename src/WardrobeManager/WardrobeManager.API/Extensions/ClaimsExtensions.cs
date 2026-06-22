using System.Security.Claims;

namespace WardrobeManager.API.Extensions;

// reads the user id from JWT claims — controllers trust this, never a userId from the route/body
public static class ClaimsExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        // try the standard claim first, then fall back to "sub".
        var value = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(value))
        {
            value = principal.FindFirstValue("sub");
        }

        if (!Guid.TryParse(value, out var id))
        {
            throw new UnauthorizedAccessException("Token does not contain a valid user id.");
        }

        return id;
    }
}
