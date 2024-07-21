using System.Linq;

namespace AnimLib.Animations;

[PublicAPI]
public record AnimSpriteSheet(Dictionary<string, AnimTextureAtlas> _atlases, AnimTag[] _tags) {
  private readonly AnimTag[] _tags = _tags;
  private readonly Dictionary<string, AnimTextureAtlas> _atlases = _atlases;
  private readonly Dictionary<string, AnimTag> _tagDictionary = _tags.ToDictionary(tag => tag.Name, tag => tag);

  /// <summary>
  /// Animation tags as they appear in the Aseprite file in the program.
  /// </summary>
  public ReadOnlySpan<AnimTag> Tags => (ReadOnlySpan<AnimTag>)_tags;

  /// <summary>
  /// Animation tags as they appear in the Aseprite file in the program, indexable by animation name.
  /// </summary>
  public IReadOnlyDictionary<string, AnimTag> TagDictionary => _tagDictionary;

  /// <summary>
  /// Represents pairs of root layer names and the texture atlas generated from the layer.
  /// </summary>
  public IReadOnlyDictionary<string, AnimTextureAtlas> Atlases => _atlases;

  public ReadOnlySpan<Rectangle> GetAnimationFrames(string animation, string layer) {
    if (!TagDictionary.TryGetValue(animation, out AnimTag tag)) {
      throw new ArgumentException($"Animation with name \"{animation}\" does not exist.", nameof(animation));
    }

    if (!Atlases.TryGetValue(layer, out AnimTextureAtlas atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    var frames = tag.Frames;
    int start = frames[0].AtlasFrameIndex;
    return atlas.Regions.Slice(start, frames.Length);
  }

  public Rectangle GetAnimationRect([NotNull] string animation, [NotNull] string layer, int index) {
    var frames = GetAnimationFrames(animation, layer);

    ArgumentOutOfRangeException.ThrowIfNegative(index);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, frames.Length);

    return frames[index];
  }

  public Rectangle GetAtlasRect([NotNull] string layer, int index) {
    ArgumentException.ThrowIfNullOrEmpty(layer);
    if (!Atlases.TryGetValue(layer, out AnimTextureAtlas atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    return atlas.GetRect(index);
  }

  public Rectangle GetRectFromTimer(string layer, string animation, int duration) =>
    GetRectFromTimer(layer, animation, duration / 60f);

  public Rectangle GetRectFromTimer(string layer, string animation, float durationSeconds) {
    if (!TagDictionary.TryGetValue(animation, out AnimTag tag)) {
      throw new ArgumentException($"Animation with name \"{animation}\" does not exist.", nameof(animation));
    }

    int frameIndex = 0;
    var frames = GetAnimationFrames(animation, layer);
    var tagFrames = tag.Frames;
    while (true) {
      float frameDuration = tagFrames[frameIndex].Duration;
      if (durationSeconds > frameDuration && frameIndex < tagFrames.Length) {
        durationSeconds -= frameDuration;
        frameIndex++;
      }
      else {
        return frames[frameIndex];
      }
    }
  }
}
