using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Processors;

namespace AnimLib.Animations.Aseprite.Processors;

/// <summary>
/// Use <see cref="IAsepriteProcessor{T}"/>
/// </summary>
public interface IAsepriteProcessor;

/// <summary>
/// Defines a processor for processing an <see cref="AsepriteFile"/> into an instance of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Type which the processor processes the <see cref="AsepriteFile"/> into.</typeparam>
public interface IAsepriteProcessor<out T> : IAsepriteProcessor where T : class {
  /// <summary>
  /// Create an instance of <typeparamref name="T"/> from the provided <paramref name="file"/>.
  /// </summary>
  T Process(AsepriteFile file, ProcessorOptions? options = null);
}
