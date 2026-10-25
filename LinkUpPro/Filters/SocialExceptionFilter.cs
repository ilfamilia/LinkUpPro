using LinkUpPro.Core.Domain.Exceptions;
using LinkUpPro.ViewModels.Social;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace LinkUpPro.Filters;

/// <summary>Converts expected business errors to safe HTML responses without exposing exception details.</summary>
public sealed class SocialExceptionFilter(IModelMetadataProvider metadata) : IExceptionFilter
{
    /// <summary>Handles domain failures while allowing unexpected errors to reach the global error handler.</summary>
    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not DomainRuleException exception) return;
        context.Result = new ViewResult
        {
            ViewName = "~/Views/Shared/SocialError.cshtml",
            StatusCode = exception.Message.Contains("permisos", StringComparison.OrdinalIgnoreCase) ? 403 : 400,
            ViewData = new ViewDataDictionary<SocialErrorViewModel>(metadata, context.ModelState)
                { Model = new() { Message = exception.Message } }
        };
        context.ExceptionHandled = true;
    }
}
