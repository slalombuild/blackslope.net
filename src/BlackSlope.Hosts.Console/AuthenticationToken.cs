using System;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace BlackSlope.Hosts.ConsoleApp
{
    public static class AuthenticationToken
    {
        public static async Task GetAuthTokenAsync()
        {
            Console.WriteLine("Welcome to the BlackSlope.NET Console");
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
            var response = await GetTokenAsync(clientId, key, tenantId, appIdUri);

            var token = response.AccessToken;
            Console.WriteLine($"Bearer {token}");

            Console.WriteLine("");
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }

        public static async Task<AuthenticationResult> GetTokenAsync(string clientId, string key, string tenantId, string appIdUniformResourceIdentifier)
        {
            var authority = $"https://login.microsoftonline.com/{tenantId}";

            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(key)
                .WithAuthority(authority)
                .Build();

            var scopes = new[] { $"{appIdUniformResourceIdentifier}/.default" };
            var response = await app.AcquireTokenForClient(scopes).ExecuteAsync();

            return response;
        }
    }
}
