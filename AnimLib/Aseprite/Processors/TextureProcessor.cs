using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Processors;
using TextureAtlas = AnimLib.Animations.TextureAtlas;

namespace AnimLib.Aseprite.Processors;

/// <summary>
/// Basic processor to process an <see cref="AsepriteFile"/> into a <see cref="Texture2D"/>.
/// </summary>
public class TextureProcessor : IAsepriteProcessor<Texture2D> {
  public Texture2D Process(AsepriteFile file, ProcessorOptions? options = null) {
    TextureAtlas tex = TextureAtlasProcessor.Process(file, options ?? ProcessorOptions.Default);
    return AnimLibMod.Instance.AseTextureToTexture2DAsset(tex.Texture, AssetRequestMode.ImmediateLoad).Value;
  }
}
