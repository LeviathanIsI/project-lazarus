using System.Threading;
using System.Threading.Tasks;

namespace Lazarus.Desktop.Services;

public interface IFileSystemBootstrapService
{
    Task EnsureLayoutAsync(CancellationToken cancellationToken = default);
}

