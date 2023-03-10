using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using NewVideoFunction.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace NewVideoFunction
{
    public class UserService : IUserService
    {
        private readonly IOptions<Connections> _options;
        private GraphServiceClient _graphServiceClient;

        public UserService(IOptions<Connections> options)
        {
            _options = options;
        }

        public async Task<(string username, string email)> GetDetails(string userObjectId)
        {
            // The client credentials flow requires that you request the
            // /.default scope, and preconfigure your permissions on the
            // app registration in Azure. An administrator must grant consent
            // to those permissions beforehand.
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = _options.Value.TenantId;

            // Values from app registration
            var clientId = _options.Value.VideoNotifyFunctionClientId;
            var clientSecret = _options.Value.VideoNotifyFunctionClientSecret;

            // using Azure.Identity;
            var tokenOptions = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, tokenOptions);

            var graphServiceClient = new GraphServiceClient(clientSecretCredential, scopes);

            var user = await graphServiceClient.Users[userObjectId].Request().Select(u => new { u.DisplayName, u.Identities }).GetAsync();
            return (user.DisplayName, user.Identities.First(i => i.SignInType == "emailAddress").IssuerAssignedId);
        }
    }
}
