﻿using JetBrains.Annotations;

namespace AnimLib.Animations;

[PublicAPI]
public record AnimSpriteSheet {
  private readonly AnimTag[] _tags;
  private readonly Dictionary<string, AnimTextureAtlas> _atlases;
  private readonly Dictionary<string, AnimTag> _tagDictionary;
  private readonly Dictionary<string, Vector2[]> _points;

  public AnimSpriteSheet(Dictionary<string, AnimTextureAtlas> _atlases, AnimTag[] _tags, Dictionary<string, Vector2[]> _points) {
    this._tags = _tags;
    this._atlases = _atlases;
    this._points = _points;

    // Avoid .ToDictionary, seems to remain allocated after mod unload
    _tagDictionary = [];
    foreach (AnimTag tag in _tags) {
      _tagDictionary.Add(tag.Name, tag);
    }
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
  /// Represents pairs of root layer names and the texture atlas generated from the layer.
  /// </summary>
  public IReadOnlyDictionary<string, AnimTextureAtlas> Atlases => _atlases;

  /// <summary>
  /// Represents pairs of Yellow root layer names, and the single pixel position on each frame.
  /// If a frame was missing a pixel, the value will be the center of the sprite.
  /// </summary>
  public IReadOnlyDictionary<string, Vector2[]> Points => _points;

  public ReadOnlySpan<Rectangle> GetAnimationFrames(string animation, string layer) {
    ArgumentNullException.ThrowIfNull(animation);
    ArgumentNullException.ThrowIfNull(layer);

    if (!TagDictionary.TryGetValue(animation, out AnimTag? tag)) {
      throw new ArgumentException($"Animation with name \"{animation}\" does not exist.", nameof(animation));
    }

    if (!Atlases.TryGetValue(layer, out AnimTextureAtlas? atlas)) {
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

    if (!Atlases.TryGetValue(layer, out AnimTextureAtlas? atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    return atlas.GetRect(index);
  }

  public Rectangle GetRectFromTimer(string animation, string layer, int duration) {
    ArgumentException.ThrowIfNullOrWhiteSpace(animation);
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    return GetRectFromTimer(animation, layer, duration / 60f);
  }

  public Rectangle GetRectFromTimer(string animation, string layer, float durationSeconds) {
    ArgumentException.ThrowIfNullOrWhiteSpace(animation);
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    if (!TagDictionary.TryGetValue(animation, out AnimTag? tag)) {
      throw new ArgumentException($"Animation with name \"{animation}\" does not exist.", nameof(animation));
    }

    // TODO: Something not strictly linear
    // This works for basic vfx that uses this AnimSpriteSheet to load with.
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

  public void Deconstruct(out Dictionary<string, AnimTextureAtlas> _atlases, out AnimTag[] _tags, out Dictionary<string, Vector2[]> _points) {
    _atlases = this._atlases;
    _tags = this._tags;
    _points = this._points;
  }
}
