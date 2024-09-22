﻿using JetBrains.Annotations;

namespace AnimLib.Animations;

/// <param name="tagName">
/// The Animation Tag to play, as defined in the Aseprite file.
/// </param>
/// <param name="frameIndex">
/// The frame to play,
/// -or- <see langword="null"/> to use the current playing frame <see cref="AnimFrame"/>.
/// A non-<see langword="null"/> value prevents normal playback.
/// </param>
/// <param name="speed">
/// The speed at which the animation plays.
/// A value of 1 plays at normal speed, as can be previewed in the Aseprite file.
/// </param>
/// <param name="rotation">
/// Rotation of the sprite, in radians.
/// If degrees are necessary to work with, use <see cref="MathHelper.ToRadians(float)"/> for this parameter.
/// </param>
/// <param name="rotationOffset">
/// Whether the specified <paramref name="rotation"/> should be added to the sprite rotation (<see langword="true"/>),
/// or set to the specified <paramref name="rotation"/> (<see langword="false"/>).
/// This value is ignored if <paramref name="tagName"/> is different from the previous frame.
/// </param>
/// <param name="isReversed">
/// Whether to play the animation in reverse,
/// -or- <see langword="null"/>, to use the value defined in the Aseprite file.
/// </param>
/// <param name="loopCount">
/// Number of times the animation will be played,
/// -or- <see langword="null"/> to use the value defined in the Aseprite file.
/// </param>
/// <param name="isPingPong">
/// Whether the animation should play in reverse after reaching the last frame of its current playback,
///  -or- <see langword="null"/> to use the value defined in the Aseprite file.
/// </param>
/// <param name="effects">
/// <see cref="SpriteEffects"/> that will determine the flip directions of the sprite,
/// -or- <see langword="null"/>, to use an effect based on player direction and gravity.
/// </param>
[PublicAPI]
public readonly struct AnimationOptions(
  string tagName,
  int? frameIndex = null,
  float speed = 1,
  float rotation = 0,
  bool rotationOffset = false,
  int? loopCount = null,
  bool? isReversed = null,
  bool? isPingPong = null,
  SpriteEffects? effects = null) {
  public string TagName { get; init; } = tagName;
  public int? FrameIndex { get; init; } = frameIndex;
  public float Speed { get; init; } = speed;
  public float Rotation { get; init; } = rotation;
  public bool RotationOffset { get; init; } = rotationOffset;
  public int? LoopCount { get; init; } = loopCount;
  public bool? IsReversed { get; init; } = isReversed;
  public bool? IsPingPong { get; init; } = isPingPong;
  public SpriteEffects? Effects { get; init; } = effects;
}
