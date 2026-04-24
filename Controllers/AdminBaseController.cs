using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace petcomm.Controllers
{
    public class AdminBaseController : Controller
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var role = HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(role) || role != "Admin")
            {
                var referer = Request.Headers["Referer"].ToString();

                if (!string.IsNullOrEmpty(referer))
                    context.Result = Redirect(referer);
                else
                    context.Result = RedirectToAction("Index", "Home");

                return;
            }

            base.OnActionExecuting(context);
        }
    }
}