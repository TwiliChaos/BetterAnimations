using AnimLib.Animations;
using AsepriteDotNet.Aseprite;

namespace AnimLib.Aseprite.Processors;

/// <summary>
/// Processes an <see cref="AsepriteFile"/> to a <see cref="Dictionary{TKey,TValue}"/>,
/// where the key is the name of the layer,
/// and the value is the layer processed to a <see cref="Texture2D"/> Asset.
/// </summary>
public class TextureDictionaryProcessor : IAsepriteProcessor<TextureDictionary>, IAsepriteProcessor<Dictionary<string, Asset<Texture2D>>> {
  public TextureDictionary Process(AsepriteFile file, AnimProcessorOptions options) {
    var textureAtlases = AnimTextureAtlasProcessor.Process(file, options);

    TextureDictionary result = new();
    foreach ((string key, TextureAtlas value) in textureAtlases) {
      result.Add(key, value.TextureAsset);
    }

    return result;
  }

  Dictionary<string, Asset<Texture2D>> IAsepriteProcessor<Dictionary<string, Asset<Texture2D>>>.Process(AsepriteFile file, AnimProcessorOptions options) {
    return Process(file, options);
  }
}
