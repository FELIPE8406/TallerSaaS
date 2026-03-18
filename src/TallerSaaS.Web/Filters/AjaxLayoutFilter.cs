using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TallerSaaS.Web.Filters;

public class AjaxLayoutFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Simple check for AJAX requests
        bool isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        
        if (isAjax)
        {
            // If it's an AJAX request, we tell the view not to use the layout
            // This assumes the views are prepared to handle Layout = null
            // or that the view start logic respects this change.
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                controller.ViewData["IsAjaxRequest"] = true;
            }
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }
}
