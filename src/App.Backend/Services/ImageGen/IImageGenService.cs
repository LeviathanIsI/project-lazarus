using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Lazarus.Shared.Images;

namespace Lazarus.Backend.Services.ImageGen;

public interface IImageGenService
{
    IAsyncEnumerable<ImageGenEvent> GenerateAsync(ImageGenRequest request, CancellationToken ct);
    Task<bool> PingAsync(string baseUrl, CancellationToken ct);
}

