using AsepriteDotNet;

namespace AnimLib.Aseprite;

public record AnimTextureAtlas(TextureRegion[] _regions, Asset<Texture2D> Texture) {
  private readonly TextureRegion[] _regions = _regions;
  public ReadOnlySpan<TextureRegion> Regions => _regions;
}
