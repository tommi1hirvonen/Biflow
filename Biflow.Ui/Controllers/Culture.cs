using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Biflow.Ui.Controllers;

[Microsoft.AspNetCore.Mvc.Route("[controller]/[action]")]
public class CultureController : Controller
{
    public IActionResult Set(string? culture, string? redirectUri)
    {
        if (culture is null)
        {
            return redirectUri is not null
                ? LocalRedirect(redirectUri)
                : new OkResult();
        }
        
        var requestCulture = new RequestCulture(culture, culture);
        var cultureCookieValue = CookieRequestCultureProvider.MakeCookieValue(requestCulture);
        HttpContext.Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            cultureCookieValue);

        return redirectUri is not null
            ? LocalRedirect(redirectUri)
            : new OkResult();
    }
}