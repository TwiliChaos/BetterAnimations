using AsepriteDotNet;

namespace AnimLib.Aseprite;

public record AnimTextureAtlas(TextureRegion[] _regions, Asset<Texture2D> _texture) {
  private readonly TextureRegion[] _regions = _regions;

  private readonly Asset<Texture2D> _texture = _texture;

  // TODO: Either use this for proper draw data, or find or create another type
  public ReadOnlySpan<TextureRegion> Regions => _regions;

  // TODO: Consider just using Texture here
  public Asset<Texture2D> Texture { get; } = _texture;
}
