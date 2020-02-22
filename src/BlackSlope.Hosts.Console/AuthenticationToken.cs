using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace BlackSlope.Hosts.ConsoleApp
{
    public static class AuthenticationToken
    {
        public static async Task GetAuthTokenAsync()
        {
            Console.WriteLine("Welcome to the BlackSlop.NET Console");
            Console.WriteLine("");

            Console.Write("ClientId: ");
            var clientId = Console.ReadLine().Trim();
            Console.WriteLine();
            Console.Write("ClientSecret: ");
            var key = Console.ReadLine().Trim();
            Console.WriteLine();
            Console.Write("TenantId: ");
            var tenantId = Console.ReadLine().Trim();
            Console.WriteLine();
            Console.Write("App URI: ");
            var appIdUri = Console.ReadLine().Trim();
            Console.WriteLine();
            var response = await GetTokenAsynch(clientId, key, tenantId, appIdUri);

            var token = response.AccessToken;
            Console.WriteLine($"Bearer {token}");

            Console.WriteLine("");
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }

        public static async Task<AuthenticationResult> GetTokenAsynch(string clientId, string key, string tenantId, string appIdUniformResourceIdentifier)
        {
            var aadInstanceUrl = "https://login.microsoftonline.com/{0}";
            var authority = string.Format(CultureInfo.InvariantCulture, aadInstanceUrl, tenantId);

            var authContext = new AuthenticationContext(authority);
            var clientCredential = new ClientCredential(clientId, key);

            var response = await authContext.AcquireTokenAsync(appIdUniformResourceIdentifier, clientCredential);

            return response;
        }
    }
}
