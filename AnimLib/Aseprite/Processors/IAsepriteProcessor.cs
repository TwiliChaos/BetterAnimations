using AsepriteDotNet.Aseprite;

namespace AnimLib.Aseprite.Processors;

/// <summary>
/// Use <see cref="IAsepriteProcessor{T}"/>
/// </summary>
public interface IAsepriteProcessor;

public interface IAsepriteProcessor<out T> : IAsepriteProcessor where T : class {
  /// <summary>
  /// Create an instance of <typeparamref name="T"/> from the provided <paramref name="file"/>.
  /// </summary>
  public T Process(AsepriteFile file, AnimProcessorOptions options);
}
