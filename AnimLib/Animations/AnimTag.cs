using System.Diagnostics;
using System.Linq;

namespace AnimLib.Animations;

// AnimLib copy of AsepriteDotNet class so that mods dependent on AnimLib don't need to access AsepriteDotNet assembly
/// <summary>
/// Represents on animation tag as defined in the Aseprite file.
/// </summary>
[DebuggerDisplay("Name = {Name}, FrameCount = {Frames.Length}")]
public record AnimTag {
  public ReadOnlySpan<AnimFrame> Frames => _frames;
  private readonly AnimFrame[] _frames;

  /// <summary>
  /// The name of the animation, as defined in the Aseprite file.
  /// </summary>
  public readonly string Name;

  /// <summary>
  /// Number of times the animation will play before stopping, as defined in the Aseprite file.
  /// </summary>
  public readonly int LoopCount;

  /// <summary>
  /// Whether the animation will play in reverse, as defined in the Aseprite file.
  /// </summary>
  public readonly bool IsReversed;

  /// <summary>
  /// Whether the animation should ping-pong once reaching the last frame, as defined in the Aseprite file.
  /// </summary>
  public readonly bool IsPingPong;

  /// <summary>
  /// The total duration of the animation, in seconds.
  /// </summary>
  public readonly float TotalDuration;

  private AnimTag(AnimFrame[] frames, string name, int loopCount, bool isReversed, bool isPingPong) {
    _frames = frames;
    Name = name;
    LoopCount = loopCount;
    IsReversed = isReversed;
    IsPingPong = isPingPong;
    TotalDuration = frames.Sum(frame => frame.Duration);
  }

  internal static AnimTag FromAse(AsepriteDotNet.AnimationTag aseTag) {
    var aseFrames = aseTag.Frames;
    int frameCount = aseFrames.Length;

    var frames = new AnimFrame[frameCount];
    for (int i = 0; i < frameCount; i++) {
      frames[i] = AnimFrame.FromAse(aseFrames[i]);
    }

    return new AnimTag(frames, aseTag.Name, aseTag.LoopCount, aseTag.IsReversed, aseTag.IsPingPong);
  }
}
