namespace DataBot
{
    using Microsoft.Azure.KeyVault;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using System;
    using System.Security;

    public class KeyVaultUtil
    {
        public const string KeyVaultUrl = "https://datapipelinekeyvault.vault.azure.net/";

        public const string SharedAccountName = "algtel";

        private const string AzurePowerShellClientId = "1950a258-227b-4e31-a9cf-717495945fc2";

        public static Lazy<KeyVaultClient> Client = new Lazy<KeyVaultClient>(() =>
                new KeyVaultClient(async (authority, resource, scope) =>
                    (await new AuthenticationContext(authority, TokenCache.DefaultShared).AcquireTokenAsync(resource,
                        AzurePowerShellClientId, new UserCredential())).AccessToken));

        public static string GetSecretInPlaintext(string secretName)
        {
            return Client.Value.GetSecretAsync(KeyVaultUrl, secretName).Result.Value;
        }
    }
}