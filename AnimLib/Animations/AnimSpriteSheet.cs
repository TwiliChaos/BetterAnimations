using System.Collections.ObjectModel;
using JetBrains.Annotations;

namespace AnimLib.Animations;

[PublicAPI]
public record AnimSpriteSheet {
  private readonly AnimTag[] _tags;
  private readonly ReadOnlyDictionary<string, TextureAtlas> _atlases;
  private readonly ReadOnlyDictionary<string, AnimTag> _tagDictionary;
  private readonly ReadOnlyDictionary<string, Vector2[]> _points;

  public AnimSpriteSheet(Dictionary<string, TextureAtlas> atlases, AnimTag[] tags,
    Dictionary<string, Vector2[]> points) {
    _tags = tags;
    _atlases = atlases.AsReadOnly();
    _points = points.AsReadOnly();

    // Avoid .ToDictionary, seems to remain allocated after mod unload
    Dictionary<string, AnimTag> tagDict = [];
    foreach (AnimTag tag in tags) {
      tagDict.Add(tag.Name, tag);
    }

    _tagDictionary = tagDict.AsReadOnly();
  }

  /// <summary>
  /// Animation tags as they appear in the Aseprite file in the program.
  /// </summary>
  public ReadOnlySpan<AnimTag> Tags => (ReadOnlySpan<AnimTag>)_tags;

  /// <summary>
  /// Animation tags as they appear in the Aseprite file in the program, indexable by animation name.
  /// </summary>
  public IReadOnlyDictionary<string, AnimTag> TagDictionary => _tagDictionary;

  /// <summary>
  /// Represents pairs of imported layer names and the texture atlas generated from the layer.
  /// </summary>
  public IReadOnlyDictionary<string, TextureAtlas> Atlases => _atlases;

  /// <summary>
  /// Represents pairs of Yellow layer names, and the single pixel position on each frame.
  /// If a frame was missing a pixel, the value will be the center of the sprite.
  /// </summary>
  public IReadOnlyDictionary<string, Vector2[]> Points => _points;

  public ReadOnlySpan<Rectangle> GetAnimationFrames(string animation, string layer) {
    ArgumentNullException.ThrowIfNull(animation);
    ArgumentNullException.ThrowIfNull(layer);

    if (!TagDictionary.TryGetValue(animation, out AnimTag? tag)) {
      throw new ArgumentException($"Animation with name \"{animation}\" does not exist.", nameof(animation));
    }

    if (!Atlases.TryGetValue(layer, out TextureAtlas? atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    var frames = tag.Frames;
    int start = frames[0].AtlasFrameIndex;
    return atlas.Regions.Slice(start, frames.Length);
  }

  public Rectangle GetAnimationRect(string animation, string layer, int index) {
    ArgumentException.ThrowIfNullOrWhiteSpace(animation);
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    var frames = GetAnimationFrames(animation, layer);

    ArgumentOutOfRangeException.ThrowIfNegative(index);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, frames.Length);

    return frames[index];
  }

  public Rectangle GetAtlasRect(string layer, int index) {
    ArgumentException.ThrowIfNullOrEmpty(layer);

    if (!Atlases.TryGetValue(layer, out TextureAtlas? atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    return atlas.GetRect(index);
  }

  public Rectangle GetRectFromTimer(AnimationOptions animation, string layer, int ticks, bool loop = true) {
    ArgumentException.ThrowIfNullOrWhiteSpace(animation.TagName);
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    int frame = FrameFromTimer(animation, ticks / 60f, loop);
    return GetAnimationRect(animation.TagName, layer, frame);
  }

  public Rectangle GetRectFromTimer(AnimationOptions animation, string layer, float seconds, bool loop = true) {
    ArgumentException.ThrowIfNullOrWhiteSpace(animation.TagName);
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    int frame = FrameFromTimer(animation, seconds, loop);
    return GetAnimationRect(animation.TagName, layer, frame);
  }

  public int FrameFromTimer(AnimationOptions animation, int ticks, bool loop = true) {
    return FrameFromTimer(animation, ticks / 60f, loop);
  }

  public int FrameFromTimer(AnimationOptions animation, float seconds, bool loop = true) {
    ArgumentException.ThrowIfNullOrWhiteSpace(animation.TagName);

    if (!TagDictionary.TryGetValue(animation.TagName, out AnimTag? tag)) {
      throw new ArgumentException($"Animation with name \"{animation}\" does not exist.", nameof(animation));
    }

    var frames = tag.Frames;
    float totalSeconds = tag.TotalDuration;

    if (!loop && seconds >= totalSeconds) {
      return frames.Length - 1;
    }

    float duration = seconds % totalSeconds;

    int frameIndex;
    if (animation.IsReversed ?? tag.IsReversed) {
      frameIndex = frames.Length - 1;
      while (true) {
        duration -= frames[frameIndex].Duration;
        if (duration <= 0) {
          return frameIndex;
        }

        frameIndex--;
      }
    }

    frameIndex = 0;
    while (true) {
      duration -= frames[frameIndex].Duration;
      if (duration <= 0) {
        return frameIndex;
      }

      frameIndex++;
    }
  }

  public Rectangle GetRectFromSequence(ReadOnlySpan<AnimationOptions> animations, string layer, int ticks,
    bool loop = true) {
    return GetRectFromSequence(animations, layer, ticks / 60f, loop);
  }

  public Rectangle GetRectFromSequence(ReadOnlySpan<AnimationOptions> animations, string layer, float seconds,
    bool loop = true) {
    (AnimTag tag, int frame) = FrameFromTimerSequence(animations, seconds, loop);
    return GetAnimationRect(tag.Name, layer, frame);
  }

  public (AnimTag, int) FrameFromTimerSequence(ReadOnlySpan<AnimationOptions> animations, int ticks, bool loop = true) {
    return FrameFromTimerSequence(animations, ticks / 60f, loop);
  }

  public (AnimTag, int) FrameFromTimerSequence(ReadOnlySpan<AnimationOptions> animations, float seconds,
    bool loop = true) {
    float totalSequenceDuration = 0;
    AnimTag? tag;
    bool hasInfLoop = false;
    foreach (AnimationOptions animation in animations) {
      if (!TagDictionary.TryGetValue(animation.TagName, out tag)) {
        throw new ArgumentException($"Animation with name \"{animation}\" does not exist.", nameof(animations));
      }

      int loopCount = animation.LoopCount ?? tag.LoopCount;
      float tagDuration = animation.FrameIndex switch {
        >= 0 => tag.Frames[animation.FrameIndex.Value].Duration,
        _ => tag.TotalDuration
      };
      totalSequenceDuration += tagDuration / animation.Speed * loopCount;
      if (loopCount == 0) {
        hasInfLoop = true;
        loop = false; // Inner infinite animation loop prevents sequence loop
      }
    }

    if (!loop && !hasInfLoop && seconds >= totalSequenceDuration) {
      tag = TagDictionary[animations[^1].TagName];
      return (tag, tag.Frames.Length - 1);
    }

    float duration = hasInfLoop ? seconds : seconds % totalSequenceDuration;

    foreach (AnimationOptions animation in animations) {
      if (!TagDictionary.TryGetValue(animation.TagName, out tag)) {
        throw new ArgumentException($"Animation with name \"{animation}\" does not exist.", nameof(animations));
      }


      int loopCount = animation.LoopCount ?? tag.LoopCount;
      float tagDuration = tag.TotalDuration / animation.Speed * loopCount;
      if (loopCount == 0 || duration <= tagDuration) {
        if (animation.FrameIndex is { } frameIndex) {
          return (tag, frameIndex);
        }
        return (tag, FrameFromTimer(animation, duration * animation.Speed));
      }

      duration -= tagDuration;
    }

    tag = TagDictionary[animations[^1].TagName];
    return (tag, tag.Frames.Length - 1);
  }

  public Vector2 GetPoint(string layer, int index) {
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    if (!Points.TryGetValue(layer, out var points)) {
      throw new ArgumentException($"Layer with name \"{layer}\" does not exist.");
    }

    return points[index];
  }


  /// <summary>
  /// Gets the <see cref="AnimTag"/> with the specified <see cref="AnimTag.Name"/>.
  /// </summary>
  /// <param name="tagName">The name of the <see cref="AnimTag"/> to retrieve.</param>
  /// <param name="tag">The resulting tag.</param>
  /// <returns>
  /// <see langword="true"/> if a tag with the specified name exists; otherwise, <see langword="false"/>.
  /// </returns>
  public bool TryGetTag(string tagName, [NotNullWhen(true)] out AnimTag? tag) {
    ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
    return TagDictionary.TryGetValue(tagName, out tag);
  }

  public void Deconstruct(
    out ReadOnlyDictionary<string, TextureAtlas> atlases,
    out ReadOnlySpan<AnimTag> tags,
    out ReadOnlyDictionary<string, Vector2[]> points) {
    atlases = _atlases;
    tags = (ReadOnlySpan<AnimTag>)_tags;
    points = _points;
  }
}
