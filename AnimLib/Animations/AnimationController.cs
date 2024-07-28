using AnimLib.Extensions;
using JetBrains.Annotations;

namespace AnimLib.Animations;

/// <summary>
/// This class plays various <see cref="Animation"/>s and manages advancement of frames.
/// Your <see cref="AnimationController"/> is automatically created by <see cref="AnimLibMod"/> when a player is initialized.
/// <para>For your mod, you must have exactly one class derived from <see cref="AnimationController"/>, else your player cannot be animated.</para>
/// <para>To get your <see cref="AnimationController"/> instance on the player, use extension method <see cref="ModPlayerExtensions.GetAnimCharacter"/>.</para>
/// </summary>
/// <remarks>
/// Alongside your <see cref="AnimSpriteSheet"/>s, that stores what animations are, such as their positions on spritesheets and duration,
/// your <see cref="AnimationController"/> determines which tag plays depending on whatever conditions you have, and how they are played.
/// </remarks>
[PublicAPI]
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract partial class AnimationController {
  // TODO: maybe automate playerlayers
  // There's a PR in tML to overhaul them to be more OoP, and that can go a long ways to automating them here.
  // Currently, PlayerLayers require IndexOf() for inserting in the list in ModifyDrawLayers to get desirable results.
  // That cannot be automated with desirable results without making this code clunky AF
  // Or making the assumption that all playerlayers should be inserted in the same point OriMod's do.
  // Let's not make that assumption.

  /// <summary>
  /// The current <see cref="Animation"/> to retrieve tag data from.
  /// <para>By default, this is the first <see cref="Animation"/> in <see cref="Animations"/>.</para>
  /// </summary>
  public Animation? MainAnimation { get; private set; }


  /// <summary>
  /// The name of the animation tag currently playing. This value cannot be set to a null or whitespace value.
  /// </summary>
  /// <exception cref="ArgumentException">A set operation cannot be performed with a null or whitespace value.</exception>
  public string CurrentTagName { get; private set; } = string.Empty;

  /// <summary>
  /// Current index of the <see cref="AnimTag"/> being played.
  /// </summary>
  public int FrameIndex { get; private set; }

  /// <summary>
  /// Current time of the <see cref="AnimFrame"/> being played.
  /// </summary>
  public float FrameTime { get; internal set; }

  /// <summary>
  /// The amount of times which this Animation has looped.
  /// </summary>
  public int TimesLooped { get; private set; }

  /// <summary>
  /// Current rotation the sprite is set to.
  /// </summary>
  public float SpriteRotation { get; private set; }

  /// <summary>
  /// Whether the animation is currently being played in reverse.
  /// </summary>
  public bool Reversed { get; private set; }

  /// <summary>
  /// <see cref="SpriteEffects"/> that will determine the flip directions of the sprite.
  /// </summary>
  public SpriteEffects Effects { get; private set; }

  /// <summary>
  /// All <see cref="Animation"/>s that belong to this mod.
  /// </summary>
  public List<Animation> Animations { get; } = [];

  /// <summary>
  /// The <see cref="Terraria.Player"/> that is being animated.
  /// </summary>
  public Player Player => Entity;

  /// <summary>
  /// Allows you to do things after this <see cref="AnimationController"/> is constructed.
  /// Useful for getting references to <see cref="Animation"/>s via <see cref="RegisterAnimation"/>.
  /// </summary>
  public virtual void Initialize() {
  }

  /// <summary>
  /// Determines whether the animation should update. Return <see langword="false"/> to stop the animation from updating.
  /// Consider using "base.PreUpdate() and" when overriding to ensure compatibility modules proper operation.
  /// Returns <see langword="true"/> by default.
  /// </summary>
  /// <returns><see langword="true"/> to update the animation, or <see langword="false"/> to stop it.</returns>
  public virtual bool PreUpdate() => AnimationUpdEnabledCompat;

  /// <summary>
  /// This is where you choose what animations are played, and how they are played.
  /// </summary>
  /// <inheritdoc cref="AnimationOptions"/>
  /// <example>
  /// Here is an example of updating the animation based on player movement.
  /// This code assumes your <see cref="MainAnimation"/> have tags named "Running", "Jumping", "Falling", and "Idle".
  /// <code>
  /// public override void Update() {
  ///   if (Math.Abs(player.velocity.X) &gt; 0.1f) {
  ///     return new AnimationOptions("Running");
  ///   }
  ///   if (player.velocity.Y != 0) {
  ///     return new AnimationOptions(player.velocity.Y * player.gravDir &lt; 0 ? "Jumping" : "Falling");
  ///   }
  ///   return new AnimationOptions("Idle");
  /// }
  /// </code>
  /// </example>
  public abstract AnimationOptions Update();


  /// <summary>
  /// Creates an <see cref="Animation"/> with the provided <see cref="AnimSpriteSheet"/>.
  /// If this is the first Animation added, <see cref="MainAnimation"/> is set to the returned result.
  /// </summary>
  /// <returns>The <see cref="Animation"/> with the matching <see cref="AnimSpriteSheet"/>.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="spriteSheet"/> cannot be null.</exception>
  public Animation RegisterAnimation(AnimSpriteSheet spriteSheet) {
    ArgumentNullException.ThrowIfNull(spriteSheet);
    if (spriteSheet.Tags.Length == 0) {
      throw new ArgumentException("The provided Sprite Sheet requires Animation Tags in the Aseprite file.",
        nameof(spriteSheet));
    }

    Animation animation = new(this, spriteSheet);
    if (Animations.Count == 0) {
      MainAnimation = animation;
      SetTag(spriteSheet.Tags[0].Name);
    }

    Animations.Add(animation);
    return animation;
  }

  /// <summary>
  /// Sets the main <see cref="Animation"/> of this player to the given <see cref="Animation"/>.
  /// This can be useful for things like player transformations that use multiple <see cref="AnimSpriteSheet"/>s.
  /// </summary>
  /// <param name="animation">Animation to set this player's <see cref="MainAnimation"/> to.</param>
  /// <param name="tagName">Name of an optional tag to set this to.</param>
  /// <exception cref="ArgumentNullException"><paramref name="animation"/> is null.</exception>
  public void SetMainAnimation(Animation animation, string? tagName) {
    ArgumentNullException.ThrowIfNull(animation);
    if (tagName is not null) {
      if (!animation.SpriteSheet.TagDictionary.ContainsKey(tagName)) {
        throw new ArgumentException($"Tag \"{tagName}\" does not match an Animation Tag in the provided Animation");
      }
    }

    MainAnimation = animation;
  }

  internal void UpdateAnimation(AnimationOptions options) {
    ArgumentException.ThrowIfNullOrWhiteSpace(options.TagName);

    if (MainAnimation is null) {
      return;
    }

    Animation anim = MainAnimation;

    if (!anim.TryGetTag(options.TagName, out AnimTag? tag)) {
      string message = $"\"{options.TagName}\" is not a valid key for the main Animation track.";
      throw new ArgumentException(message, nameof(options));
    }

    if (options.FrameIndex.HasValue) {
      int index = options.FrameIndex.Value;
      ArgumentOutOfRangeException.ThrowIfNegative(index);
      ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, tag.Frames.Length);
    }

    ArgumentOutOfRangeException.ThrowIfNegative(options.Speed);

    // Set member values
    SpriteRotation = options.Rotation;
    Effects = options.Effects ?? SpriteEffectsFromPlayer();

    if (options.TagName != CurrentTagName) {
      SetTag(options.TagName, options.IsReversed);
    }

    if (options.FrameIndex.HasValue) {
      FrameIndex = options.FrameIndex.Value;
      FrameTime = 0;
    }
    else {
      // Loop logic
      PostPlay(options);
    }

    if (AnimPlayer.Local?.DebugEnabled ?? false) {
      // TODO: replace Main.NewText spam with something better?
      // Main.NewText($"Frame called: Tile [{MainAnimation.CurrentFrame}], " +
      //   $"{CurrentTagName}{(Reversed ? " (Reversed)" : "")} " +
      //   $"Time: {FrameTime}, " +
      //   $"AnimIndex: {FrameIndex}/{MainAnimation.CurrentTag.Frames.Length}");
    }
  }

  private SpriteEffects SpriteEffectsFromPlayer() {
    SpriteEffects effects = SpriteEffects.None;
    if (Player.direction < 0) effects |= SpriteEffects.FlipHorizontally;
    if (Player.gravDir < 0) effects |= SpriteEffects.FlipVertically;
    return effects;
  }

  private void PostPlay(AnimationOptions options) {
    const float delta = 1f / 60f; // TODO: High FPS Support
    float duration = MainAnimation!.CurrentFrame.Duration;
    float newFrameTime = FrameTime + options.Speed * delta;

    // Do nothing if not enough time has passed to advance to the next frame
    if (newFrameTime < duration || duration <= 0) {
      FrameTime = newFrameTime;
      return;
    }

    // Calculate number of frames to advance
    AnimTag tag = MainAnimation.CurrentTag;
    var frames = tag.Frames;
    int loopCount = options.LoopCount ?? tag.LoopCount;
    bool isReversed = options.IsReversed ?? tag.IsReversed;
    bool isPingPong = options.IsPingPong ?? tag.IsPingPong;

    int lastFrame = frames.Length - 1;

    int newFrameIndex = FrameIndex;
    while (newFrameTime >= duration) {
      // Determine next frame
      bool isFirstFrame = newFrameIndex == 0;
      bool isLastFrame = newFrameIndex == lastFrame;
      bool endOfFrame = !isPingPong ? isLastFrame : isFirstFrame;
      if (endOfFrame) {
        if (loopCount > 0 && TimesLooped >= loopCount) {
          // Do not change frame
          break;
        }

        TimesLooped++;

        // Ping-pong: flip state, otherwise set to argument or default state
        Reversed = isPingPong ? !Reversed : isReversed;
        newFrameIndex = Reversed ? lastFrame : 0;
      }
      else {
        newFrameIndex += !Reversed ? 1 : -1;
      }

      newFrameTime -= duration;
      duration = tag.Frames[newFrameIndex].Duration;
    }

    FrameIndex = newFrameIndex;
    FrameTime = newFrameTime;
  }

  internal void SetTag(string newTrack, bool? isReversed = null) {
    CurrentTagName = newTrack;
    AnimTag track = MainAnimation!.SpriteSheet.TagDictionary[newTrack];
    FrameTime = 0;
    Reversed = isReversed ?? track.IsReversed;
    FrameIndex = Reversed ? track.Frames.Length - 1 : 0;
  }
}
