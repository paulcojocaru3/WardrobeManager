using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using NSubstitute;
using WardrobeManager.API.Controllers;

namespace WardrobeManager.Tests.Unit.Security;

// guards the auth-cookie hardening: the cookie must be Secure in any non-dev deployment.
[Trait("Category", "Unit")]
public sealed class AuthCookieOptionsTests
{
    private static string LogoutSetCookieHeader(string environmentName, bool isHttps)
    {
        var env = Substitute.For<IWebHostEnvironment>();
        env.EnvironmentName.Returns(environmentName);

        var http = new DefaultHttpContext();
        http.Request.IsHttps = isHttps;

        var controller = new UsersController(Substitute.For<IMediator>(), env)
        {
            ControllerContext = new ControllerContext { HttpContext = http }
        };

        controller.Logout(); // deletes the cookie using BuildCookieOptions()
        return http.Response.Headers["Set-Cookie"].ToString();
    }

    [Fact]
    public void Production_CookieIsSecure_EvenWhenRequestIsNotHttps()
    {
        var header = LogoutSetCookieHeader("Production", isHttps: false);

        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=none", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Development_OverHttp_CookieIsNotSecure()
    {
        var header = LogoutSetCookieHeader("Development", isHttps: false);

        Assert.DoesNotContain("secure", header, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", header, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Development_OverHttps_CookieIsSecure()
    {
        var header = LogoutSetCookieHeader("Development", isHttps: true);

        Assert.Contains("secure", header, StringComparison.OrdinalIgnoreCase);
    }
}
