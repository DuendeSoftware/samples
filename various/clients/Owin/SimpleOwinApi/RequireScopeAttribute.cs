using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace SimpleApi
{
    public class RequireScopeAttribute : AuthorizeAttribute
    {
        private readonly string scope;

        public RequireScopeAttribute(string scope)
        {
            this.scope = scope;
        }
        protected override bool IsAuthorized(HttpActionContext actionContext)
        {
            var user = actionContext.Request.GetOwinContext().Authentication.User;
            return user.Claims.Any(c => c.Type == "scope" && c.Value == scope);
        }

    }
}
