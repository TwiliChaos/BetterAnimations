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
/// <param name="loopCount">
/// Number of times the animation will be played,
/// -or- <see langword="null"/> to use the value defined in the Aseprite file.
/// <br /> A value of 0 will play the animation indefinitely.
/// </param>
/// <param name="isReversed">
/// Whether to play the animation in reverse,
/// -or- <see langword="null"/>, to use the value defined in the Aseprite file.
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
public struct AnimationOptions(
  string tagName,
  int? frameIndex = null,
  float speed = 1,
  float rotation = 0,
  bool rotationOffset = false,
  int? loopCount = null,
  bool? isReversed = null,
  bool? isPingPong = null,
  SpriteEffects? effects = null) {
  /// <summary>
  /// The Animation Tag to play, as defined in the Aseprite file.
  /// </summary>
  public string TagName { get; set; } = tagName;
  /// <summary>
  /// The frame to play,
  /// -or- <see langword="null"/> to use the current playing frame <see cref="AnimFrame"/>.
  /// A non-<see langword="null"/> value prevents normal playback.
  /// </summary>
  public int? FrameIndex { get; set; } = frameIndex;
  /// <summary>
  /// The speed at which the animation plays.
  /// A value of 1 plays at normal speed, as can be previewed in the Aseprite file.
  /// </summary>
  public float Speed { get; set; } = speed;
  /// <summary>
  /// Rotation of the sprite, in radians.
  /// If degrees are necessary to work with, use <see cref="MathHelper.ToRadians(float)"/> for this parameter.
  /// </summary>
  public float Rotation { get; set; } = rotation;
  /// <summary>
  /// Whether the specified <see cref="Rotation"/> should be added to the sprite rotation (<see langword="true"/>),
  /// or set to the specified <see cref="Rotation"/> (<see langword="false"/>).
  /// This value is ignored if <see cref="TagName"/> is different from the previous frame.
  /// </summary>
  public bool RotationOffset { get; set; } = rotationOffset;
  /// <summary>
  /// Number of times the animation will be played,
  /// -or- <see langword="null"/> to use the value defined in the Aseprite file.
  /// <br /> A value of 0 will play the animation indefinitely.
  /// </summary>
  public int? LoopCount { get; set; } = loopCount;
  /// <summary>
  /// Whether to play the animation in reverse,
  /// -or- <see langword="null"/>, to use the value defined in the Aseprite file.
  /// </summary>
  public bool? IsReversed { get; set; } = isReversed;
  /// <summary>
  /// Whether the animation should play in reverse after reaching the last frame of its current playback,
  ///  -or- <see langword="null"/> to use the value defined in the Aseprite file.
  /// </summary>
  public bool? IsPingPong { get; set; } = isPingPong;
  /// <summary>
  /// <see cref="SpriteEffects"/> that will determine the flip directions of the sprite,
  /// -or- <see langword="null"/>, to use an effect based on player direction and gravity.
  /// </summary>
  public SpriteEffects? Effects { get; set; } = effects;
}
