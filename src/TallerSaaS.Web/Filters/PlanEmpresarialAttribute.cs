using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TallerSaaS.Domain.Interfaces;

namespace TallerSaaS.Web.Filters;

public class PlanEmpresarialAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var tenantService = context.HttpContext.RequestServices.GetService<ICurrentTenantService>();
        
        // PlanId 3 is "Empresarial"
        if (tenantService == null || tenantService.PlanId != 3)
        {
            // Redirect to Upgrade page
            context.Result = new RedirectToActionResult("Upgrade", "Nomina", null);
        }
        
        base.OnActionExecuting(context);
    }
}
