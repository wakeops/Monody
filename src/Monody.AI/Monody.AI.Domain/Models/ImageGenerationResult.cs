using System;

namespace Monody.AI.Domain.Models;

public class ImageGenerationResult
{
    public Uri ImageUri { get; init; }

    public BinaryData ImageBytes { get; init; }
}
