using Microsoft.Identity.Web;
using System.Security.Claims;

namespace SpaWebApi.Extensions
{
    public static class UserExtensions
    {
        private const string EmailClaimKey = "emails";
        public static Guid GetUserObjectId(this ClaimsPrincipal claimsPrincipal)
        {
            var guidString = claimsPrincipal.FindFirstValue(ClaimConstants.ObjectId);
            if (Guid.TryParse(guidString, out Guid guid))
            {
                return guid;
            }

            return Guid.Empty;
        }

        public static string GetEmail(this ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal.FindFirstValue(EmailClaimKey);
        }
    }
}
