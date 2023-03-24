using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using NewVideoFunction.Interfaces;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NewVideoFunction
{
    public class UserService : IUserService
    {
        private readonly ChainedTokenCredential _chainedTokenCredential;
        public UserService(ChainedTokenCredential chainedTokenCredential)
        {
            _chainedTokenCredential = chainedTokenCredential;
        }

        public async Task<(string username, string email)> GetDetails(string userObjectId)
        {
            // Create the Graph service client with a ChainedTokenCredential which gets an access
            // token using the available Managed Identity or environment variables if running
            // in development.
            var token = _chainedTokenCredential.GetToken(
                new Azure.Core.TokenRequestContext(
                    new[] { "https://graph.microsoft.com/.default" }));

            var accessToken = token.Token;
            var graphServiceClient = new GraphServiceClient(
                new DelegateAuthenticationProvider((requestMessage) =>
                {
                    requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                    return Task.CompletedTask;
                }));

            var user = await graphServiceClient.Users[userObjectId].Request().Select(u => new { u.DisplayName, u.Identities }).GetAsync();
            return (user.DisplayName, user.Identities.First(i => i.SignInType == "emailAddress").IssuerAssignedId);
        }
    }
}
