using Ogasela.Shared;

namespace Ogasela.Application.Ai.Interfaces;

public interface IImageEnhancer
{
    /// <returns>The storage key of the new, enhanced image - the original at <paramref name="imageRef"/> is left untouched.</returns>
    Task<Result<string>> EnhanceAsync(string imageRef, CancellationToken cancellationToken);
}
