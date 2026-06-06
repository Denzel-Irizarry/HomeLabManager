using HomeLabManager.API.Interfaces.Security;
using System.Threading.Tasks;

namespace HomeLabManager.API.Services.Security
{
    // Minimal credential resolver for local development and testing.
    public class StubCredentialResolver : ICredentialResolver
    {
        public Task<string?> ResolveCredentialAsync(string credentialReference)
        {
            // DO NOT use in production. Returns null to indicate no plaintext credential available.
            return Task.FromResult<string?>(null);
        }
    }
}
