using LinkUpPro.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers;

/// <summary>Applies authentication, private-page cache protection, safe errors, and CSRF validation.</summary>
[Authorize]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(SocialExceptionFilter))]
[ServiceFilter(typeof(ActiveAccountFilter))]
public abstract class SocialController : Controller;
