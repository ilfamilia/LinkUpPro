using LinkUpPro.Core.Application.Enums;
using LinkUpPro.Core.Application.Results;
using LinkUpPro.Filters;
using LinkUpPro.ViewModels.Social;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LinkUpPro.Controllers;

/// <summary>Applies authentication, private-page cache protection, safe errors, and CSRF validation.</summary>
[Authorize]
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[AutoValidateAntiforgeryToken]
[ServiceFilter(typeof(SocialExceptionFilter))]
[ServiceFilter(typeof(ActiveAccountFilter))]
public abstract class SocialController : Controller
{
    /// <summary>Shows a clear, encoded message page with the given status code instead of a blank browser error.</summary>
    protected IActionResult ErrorPage(string message, int statusCode = StatusCodes.Status400BadRequest)
    {
        Response.StatusCode = statusCode;
        return View("~/Views/Shared/SocialError.cshtml", new SocialErrorViewModel { Message = message });
    }

    /// <summary>Translates a failed service result into the matching status code and a user-facing message.</summary>
    protected IActionResult FailurePage(ServiceResult result, string fallbackMessage)
    {
        string? serviceMessage = result.Errors.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e));

        return result.Status switch
        {
            OperationStatus.Unauthorized => Challenge(),
            OperationStatus.NotFound => ErrorPage("El contenido solicitado no existe o ya no está disponible.",
                StatusCodes.Status404NotFound),
            OperationStatus.Forbidden => ErrorPage("No posee permisos para realizar esta acción.",
                StatusCodes.Status403Forbidden),
            OperationStatus.ValidationError => ErrorPage(serviceMessage ?? fallbackMessage),
            OperationStatus.Conflict => ErrorPage(serviceMessage ?? fallbackMessage, StatusCodes.Status409Conflict),
            _ => ErrorPage(fallbackMessage, StatusCodes.Status500InternalServerError)
        };
    }
}
