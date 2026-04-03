using System.Threading;
using System.Threading.Tasks;

namespace Kralizek.Extensions.Configuration
{
    public interface ISecretValueRetriever
    {
        Task<string?> GetSecretValueAsync(string secretId, CancellationToken cancellationToken);
    }
}
