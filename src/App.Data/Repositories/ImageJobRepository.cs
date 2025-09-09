using Lazarus.Data.Entities;
using Microsoft.Extensions.Logging;

namespace Lazarus.Data.Repositories;

public class ImageJobRepository : Repository<ImageJob>, IImageJobRepository
{
    public ImageJobRepository(LazarusDbContext context, ILogger<ImageJobRepository>? logger = null)
        : base(context, logger)
    {
    }
}

