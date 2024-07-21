namespace AnimLib.Animations;

// AnimLib copy of AsepriteDotNet class so that mods dependent on AnimLib don't need to access AsepriteDotNet assembly
/// <summary>
/// Represents on animation tag as defined in the Aseprite file.
/// </summary>
/// <param name="_frames"></param>
public record AnimTag(AnimFrame[] _frames, string Name, int LoopCount, bool IsReversed, bool IsPingPong) {
  public ReadOnlySpan<AnimFrame> Frames => _frames;
  private readonly AnimFrame[] _frames = _frames;

  /// <summary>
  /// The name of the animation, as defined in the Aseprite file.
  /// </summary>
  public string Name { get; init; } = Name;

  /// <summary>
  /// Number of times the animation will play before stopping, as defined in the Aseprite file.
  /// </summary>
  public readonly int LoopCount = LoopCount;

  /// <summary>
  /// Whether the animation will play in reverse, as defined in the Aseprite file.
  /// </summary>
  public readonly bool IsReversed = IsReversed;

  /// <summary>
  /// Whether the animation should ping-pong once reaching the last frame, as defined in the Aseprite file.
  /// </summary>
  public readonly bool IsPingPong = IsPingPong;

  public static AnimTag FromAse(AsepriteDotNet.AnimationTag aseTag) {
    var aseFrames = aseTag.Frames;
    int frameCount = aseFrames.Length;

    var frames = new AnimFrame[frameCount];
    for (int i = 0; i < frameCount; i++) {
      frames[i] = AnimFrame.FromAse(aseFrames[i]);
    }

    return new AnimTag(frames, aseTag.Name, aseTag.LoopCount, aseTag.IsReversed, aseTag.IsPingPong);
  }
}

// AnimLib copy of AsepriteDotNet class so that mods dependent on AnimLib don't need to access AsepriteDotNet assembly
