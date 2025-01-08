using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Processors;

namespace AnimLib.Aseprite.Processors;

/// <summary>
/// Processes an <see cref="AsepriteFile"/> to a <see cref="Dictionary{TKey,TValue}"/>,
/// where the key is the name of the layer,
/// and the value is the layer processed to a <see cref="Texture2D"/> Asset.
/// </summary>
public class TextureDictionaryProcessor : IAsepriteProcessor<Dictionary<string, Asset<Texture2D>>> {
  public Dictionary<string, Asset<Texture2D>> Process(AsepriteFile file, ProcessorOptions? options = null) {
    options ??= ProcessorOptions.Default with {
      OnlyVisibleLayers = false,
      IncludeBackgroundLayer = true
    };

    var textureAtlases = AnimTextureAtlasProcessor.Process(file, options);

    var result = new Dictionary<string, Asset<Texture2D>>();
    foreach ((string key, AnimTextureAtlas value) in textureAtlases) {
      result.Add(key, value.TextureAsset);
    }

    return result;
  }
}
