namespace AnimLib.Animations;

public record AnimTextureAtlas(Rectangle[] _regions, Asset<Texture2D> _textureAsset) {
  private readonly Rectangle[] _regions = _regions;

  private readonly Asset<Texture2D> _textureAsset = _textureAsset;

  public ReadOnlySpan<Rectangle> Regions => _regions;

  // TODO: Consider just using Texture here
  public Texture2D Texture {
    get {
      if (_texture is not null) {
        return _texture;
      }

      _textureAsset.Wait();
      _texture = _textureAsset.Value;
      return _texture;
    }
  }

  private Texture2D _texture;


  public Rectangle GetRect(int index) {
    ArgumentOutOfRangeException.ThrowIfNegative(index);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _regions.Length);

    return _regions[index];
  }

  public ReadOnlySpan<Rectangle> GetAnimationRects(AnimTag tag) {
    var frames = tag.Frames;
    int start = frames[0].AtlasFrameIndex;
    return new ReadOnlySpan<Rectangle>(_regions, start, frames.Length);
  }
}
