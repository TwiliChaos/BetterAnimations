namespace AnimLib.Aseprite;

public record AnimTextureAtlas(Rectangle[] _regions, Asset<Texture2D> _texture) {
  private readonly Rectangle[] _regions = _regions;

  private readonly Asset<Texture2D> _texture = _texture;

  public ReadOnlySpan<Rectangle> Regions => _regions;

  // TODO: Consider just using Texture here
  public Asset<Texture2D> Texture { get; } = _texture;
}
