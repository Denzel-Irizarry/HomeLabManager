using System.Threading.Tasks;

namespace HomeLabManager.API.Interfaces.Security
{
    // Resolves a credential reference into runtime credentials.
    public interface ICredentialResolver
    {
        // For Phase 2 the resolver returns an opaque string (not used by stub provider).
        Task<string?> ResolveCredentialAsync(string credentialReference);
    }
}
