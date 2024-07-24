using JetBrains.Annotations;

namespace AnimLib.Animations;

/// <summary>
/// Data class that contains a <see cref="Texture2D"/>, and
/// <see cref="Rectangle"/>s representing the source rect of
/// each frame of animation on this <see cref="Texture"/>.
/// </summary>
[PublicAPI]
public record AnimTextureAtlas {
  public AnimTextureAtlas(Rectangle[] _regions, Asset<Texture2D> _textureAsset) {
    ArgumentNullException.ThrowIfNull(_regions);
    ArgumentNullException.ThrowIfNull(_textureAsset);

    this._regions = _regions;
    this._textureAsset = _textureAsset;

    _texture = _textureAsset.IsLoaded
      ? new Lazy<Texture2D>(_textureAsset.Value)
      : new Lazy<Texture2D>(() => {
        _textureAsset.Wait();
        return _textureAsset.Value;
      });
  }

  private readonly Rectangle[] _regions;
  private readonly Asset<Texture2D> _textureAsset;
  private readonly Lazy<Texture2D> _texture;

  /// <summary>
  /// A <see cref="ReadOnlySpan{T}"/> that contains all the <see cref="Rectangle"/>s of the animation.
  /// </summary>
  /// <remarks>
  /// In the case of duplicate or empty frames during import,
  /// some <see cref="Rectangle"/> may map to the same part
  /// of the <see cref="Texture"/>.
  /// </remarks>
  public ReadOnlySpan<Rectangle> Regions => _regions;

  /// <summary>
  /// The <see cref="Texture2D"/> of this Texture Atlas.
  /// </summary>
  public Texture2D Texture => _texture.Value;

  /// <summary>
  /// Gets a <see cref="Rectangle"/> at the frame of the provided <paramref name="index"/>.
  /// The result is the same regardless of the current animation being played.
  /// </summary>
  /// <param name="index">The frame index.</param>
  /// <returns>
  /// The source rect that represents the frame at the provided <paramref name="index"/>.
  /// </returns>
  /// <remarks>
  /// In the case of duplicate or empty frames during import,
  /// some <see cref="Rectangle"/> may map to the same part
  /// of the <see cref="Texture"/>.
  /// </remarks>
  public Rectangle GetRect(int index) {
    ArgumentOutOfRangeException.ThrowIfNegative(index);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, _regions.Length);

    return _regions[index];
  }

  /// <summary>
  /// Gets a span of <see cref="Rectangle"/>s representing all the frames this <see cref="AnimTag"/> represents.
  /// </summary>
  /// <param name="tag">The animation tag.</param>
  public ReadOnlySpan<Rectangle> GetAnimationRects(AnimTag tag) {
    ArgumentNullException.ThrowIfNull(tag);

    var frames = tag.Frames;
    int start = frames[0].AtlasFrameIndex;

    if (start < 0) {
      throw new ArgumentOutOfRangeException(nameof(tag), "The start of the tag is negative.");
    }
    if (start + frames.Length > _regions.Length) {
      throw new ArgumentOutOfRangeException(nameof(tag), "The end of the tag exceeded this Atlas's region count.");
    }

    return new ReadOnlySpan<Rectangle>(_regions, start, frames.Length);
  }
}
