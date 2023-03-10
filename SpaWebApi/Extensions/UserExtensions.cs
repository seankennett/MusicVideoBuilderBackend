using Microsoft.Identity.Web;
using System.Security.Claims;

namespace SpaWebApi.Extensions
{
    public static class UserExtensions
    {
        public static Guid GetUserObjectId(this ClaimsPrincipal claimsPrincipal)
        {
            var guidString = claimsPrincipal.FindFirstValue(ClaimConstants.ObjectId);
            if (Guid.TryParse(guidString, out Guid guid))
            {
                return guid;
            }

            return Guid.Empty;
        }
    }
}
