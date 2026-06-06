using HomeLabManager.API.Interfaces.Security;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace HomeLabManager.API.Services.Security
{
    // Resolves credential references from IConfiguration (suitable for dotnet user-secrets during development).
    public class UserSecretsCredentialResolver : ICredentialResolver
    {
        private readonly IConfiguration _configuration;

        public UserSecretsCredentialResolver(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Looks up configuration key `Credentials:{credentialReference}` and returns the value as-is.
        public Task<string?> ResolveCredentialAsync(string credentialReference)
        {
            if (string.IsNullOrWhiteSpace(credentialReference))
                return Task.FromResult<string?>(null);

            var value = _configuration[$"Credentials:{credentialReference}"];
            return Task.FromResult<string?>(value);
        }
    }
}
