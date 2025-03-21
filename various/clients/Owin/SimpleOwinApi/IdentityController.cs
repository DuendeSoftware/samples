using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Owin.Security;
using SimpleApi;

namespace SampleOwinApi
{
    [Authorize]
    [RequireScope("scope1")]
    public class IdentityController : ApiController
    {
        // this action simply echoes the claims back to the client
        [HttpGet]
        public async Task<IHttpActionResult> Get()
        {
            var claims = (User as ClaimsPrincipal)?.Claims?.Select(c => new { c.Type, c.Value });
            return Json(claims);
        }
    }
}
