using AnimLib.Animations;
using Terraria.DataStructures;

namespace AnimLib.States;

public abstract class AnimatedStateMachine : StateMachine {
  public abstract Asset<AnimSpriteSheet> SpriteSheetAsset { get; }

  /// <summary>
  /// Value of <see cref="SpriteSheetAsset"/>. Do not store this value.
  /// </summary>
  public AnimSpriteSheet SpriteSheet {
    get {
      if (!SpriteSheetAsset.IsLoaded) {
        SpriteSheetAsset.Wait();
      }

      return SpriteSheetAsset.Value;
    }
  }

  /// <summary>
  /// Current <see cref="AnimTag"/> being played.
  /// </summary>
  [field: AllowNull, MaybeNull]
  public AnimTag CurrentTag {
    get => field ??= SpriteSheet.Tags[0];
    private set;
  }

  public AnimFrame CurrentFrame => CurrentTag.Frames[FrameIndex];

  /// <summary>
  /// Current index of the <see cref="AnimTag"/> being played.
  /// </summary>
  public int FrameIndex { get; private set; }

  /// <summary>
  /// Current time of the <see cref="AnimFrame"/> being played, in seconds.
  /// </summary>
  public float FrameTime { get; private set; }

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
  /// Creates a <see cref="DrawData"/> for the current animation state.
  /// </summary>
  /// <param name="drawInfo">Parameter of <see cref="PlayerDrawLayer.Draw">PlayerDrawLayer.Draw</see></param>
  /// <param name="layer">The texture layer to draw.</param>
  /// <returns></returns>
  public DrawData GetDrawData(ref readonly PlayerDrawSet drawInfo, string layer) {
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    Rectangle sourceRect = GetRect(layer);

    return new DrawData {
      texture = GetTexture(layer),
      position = drawInfo.Position - Main.screenPosition + drawInfo.drawPlayer.Size / 2,
      sourceRect = sourceRect,
      color = Color.White,
      rotation = SpriteRotation,
      origin = sourceRect.Size() / 2,
      scale = Vector2.One,
      effect = Effects,
    };
  }

  /// <summary>
  /// Gets the texture that represents the specified <paramref name="layer"/>.
  /// </summary>
  /// <param name="layer">The layer, as named in the Aseprite file.</param>
  /// <returns>The <see cref="Texture2D"/> for the specified <paramref name="layer"/>.</returns>
  public Texture2D GetTexture(string layer) => SpriteSheet.Atlases[layer].GetTexture();

  /// <summary>
  /// Gets a <see cref="Rectangle"/> to use for <see cref="DrawData.sourceRect">DrawData.sourceRect</see>
  /// for the current animation state,
  /// for the specified <paramref name="layer"/>.
  /// </summary>
  /// <param name="layer">The layer, as named in the Aseprite file.</param>
  /// <returns></returns>
  /// <remarks>
  /// This returned value will likely differ between <paramref name="layer"/>s,
  /// even if the animation state is identical. Make sure you are also using the correct texture.
  /// </remarks>
  public Rectangle GetRect(string layer) => GetRect(layer, CurrentFrame.AtlasFrameIndex);

  public Rectangle GetRect(string layer, int frameIndex) {
    if (!SpriteSheet.Atlases.TryGetValue(layer, out TextureAtlas? atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    return atlas.GetRect(frameIndex);
  }

  public Vector2 GetPoint(string layer) => GetPoint(layer, CurrentFrame.AtlasFrameIndex);

  public Vector2 GetPoint(string layer, int frameIndex) {
    if (!SpriteSheet.Points.TryGetValue(layer, out var points)) {
      throw new ArgumentException($"Layer \"{layer}\" is not a valid Points layer.");
    }

    return points[frameIndex];
  }

  public void SetLayer(ref DrawData data, string layer) {
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    if (!SpriteSheet.Atlases.TryGetValue(layer, out TextureAtlas? atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    SetLayer(ref data, atlas, CurrentFrame);
  }

  /// <summary>
  ///
  /// </summary>
  /// <param name="data">
  /// Existing <see cref="DrawData"/> to modify.
  /// </param>
  /// <param name="layer">
  /// The layer to get <see cref="DrawData.texture"/> and <see cref="DrawData.sourceRect"/> from.
  /// </param>
  /// <param name="tagName">
  /// Name of the <see cref="AnimTag"/> to get animation info from.
  /// </param>
  /// <param name="frameIndex">
  /// Frame in the <see cref="AnimTag"/> to get a source rect with.
  /// </param>
  /// <exception cref="ArgumentException">
  /// There is no <see cref="TextureAtlas"/> with name <paramref name="layer"/>.
  /// There is no <see cref="AnimTag"/> with name <paramref name="tagName"/>.
  /// </exception>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="layer"/> and <paramref name="tagName"/> cannot be <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentOutOfRangeException">
  /// <paramref name="frameIndex"/> is less than 0, or greater than number of frames in tag with name <paramref name="tagName"/>.
  /// </exception>
  public void SetLayerAndFrame(ref DrawData data, string layer, string tagName, int frameIndex) {
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);
    ArgumentException.ThrowIfNullOrWhiteSpace(tagName);

    if (!SpriteSheet.Atlases.TryGetValue(layer, out TextureAtlas? atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    if (!SpriteSheet.TryGetTag(tagName, out AnimTag? tag)) {
      throw new ArgumentException($"Animation Tag with name \"{tagName}\" does not exist.");
    }

    var tagFrames = tag.Frames;
    ArgumentOutOfRangeException.ThrowIfNegative(frameIndex);
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(frameIndex, tagFrames.Length);

    SetLayer(ref data, atlas, tagFrames[frameIndex]);
  }

  private static void SetLayer(ref DrawData data, TextureAtlas atlas, AnimFrame frame) {
    data.texture = atlas.GetTexture();
    data.sourceRect = atlas.GetRect(frame.AtlasFrameIndex);
  }

  public void UIAnimation(AnimationOptions options, AnimCharacter character, bool ignoreCategory = true) {
    int counter = ignoreCategory ? character.UIAnimationCounter : character.UICategoryAnimationCounter;
    options.FrameIndex ??= SpriteSheet.FrameFromTimer(options, counter);
    UpdateAnimation(options);
  }

  /// <summary>
  /// For use in UI, this will play an animation sequence based on the <paramref name="character"/>'s UI properties.
  /// The counter for all tags will be <see cref="AnimCharacter.UICategoryAnimationCounter"/>.
  /// </summary>
  /// <param name="tagOptions">
  /// The sequence of tags to play.
  /// </param>
  /// <param name="character">
  /// The character whose UI values will be used for the counter.
  /// </param>
  /// <param name="syncLastTag">
  /// If <see langword="true"/>, the last tag in <paramref name="tagOptions"/> will instead be played
  /// with the counter <see cref="AnimCharacter.UIAnimationCounter"/>.
  /// </param>
  public void UIAnimationSequence(ReadOnlySpan<AnimationOptions> tagOptions, AnimCharacter character,
    bool syncLastTag) {
    (AnimTag tag, int frame) =
      SpriteSheet.FrameFromTimerSequence(tagOptions, character.UICategoryAnimationCounter, loop: false);
    if (syncLastTag && tag.Name == tagOptions[^1].TagName) {
      (tag, frame) = SpriteSheet.FrameFromTimerSequence(tagOptions, character.UIAnimationCounter, loop: false);
    }

    UpdateAnimation(new AnimationOptions(tag.Name, frame));
  }

  /// <summary>
  /// For use in UI, this will play a looping animation sequence based on the <paramref name="character"/>'s UI properties.
  /// The counter for all tags will be <see cref="AnimCharacter.UICategoryAnimationCounter"/>.
  /// </summary>
  /// <param name="tagOptions">
  /// The sequence of tags to play.
  /// </param>
  /// <param name="character">
  /// The character whose UI values will be used for the counter.
  /// </param>
  /// <param name="ignoreCategory">
  /// Whether to sync this sequence with
  /// <see cref="AnimCharacter.UIAnimationCounter"/> (if <see langword="true"/>), or with
  /// <see cref="AnimCharacter.UICategoryAnimationCounter"/> (if <see langword="false"/>).
  /// </param>
  public void UILoopedAnimationSequence(ReadOnlySpan<AnimationOptions> tagOptions, AnimCharacter character, bool ignoreCategory) {
    int counter = ignoreCategory ? character.UIAnimationCounter : character.UICategoryAnimationCounter;
    (AnimTag tag, int frame) = SpriteSheet.FrameFromTimerSequence(tagOptions, counter);
    UpdateAnimation(new AnimationOptions(tag.Name, frame));
  }

  /// <summary>
  /// Changes <see cref="CurrentTag"/> if needed, and
  /// assigns various properties of <paramref name="options"/> to member values.
  /// <para />
  /// Calls <see cref="Play"/> when <see cref="AnimationOptions.FrameIndex">AnimationOptions.FrameIndex</see> is <see langword="null"/>
  /// </summary>
  /// <param name="options"></param>
  /// <param name="delta"></param>
  /// <exception cref="ArgumentException"></exception>
  internal void UpdateAnimation(AnimationOptions options, float delta = 1 / 60f) {
    ArgumentException.ThrowIfNullOrWhiteSpace(options.TagName);

    if (!SpriteSheet.TryGetTag(options.TagName, out AnimTag? tag)) {
      throw new ArgumentException($"\"{options.TagName}\" is not a valid key for the Animation tag.", nameof(options));
    }

    if (options.FrameIndex.HasValue) {
      int index = options.FrameIndex.Value;
      ArgumentOutOfRangeException.ThrowIfNegative(index);
      ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, tag.Frames.Length);
    }

    ArgumentOutOfRangeException.ThrowIfNegative(options.Speed);

    // Set member values
    bool isNewTag = options.TagName != CurrentTag.Name;

    SpriteRotation = options.Rotation + (options.RotationOffset && !isNewTag ? SpriteRotation : 0);
    Effects = options.Effects ?? SpriteEffectsFromPlayer(Player);

    if (isNewTag) {
      SetTag(options.TagName, options.IsReversed);
    }

    if (options.FrameIndex.HasValue) {
      FrameIndex = options.FrameIndex.Value;
      FrameTime = 0;
    }
    else {
      // Loop logic
      Play(options, delta);
    }
  }

  /// <summary>
  /// Advances <see cref="FrameTime"/>.
  /// <para />
  /// Advances <see cref="FrameIndex"/> if time exceeds <see cref="AnimFrame.Duration"/>.
  /// <para />
  /// Advances <see cref="TimesLooped"/> if index exceeds end of frame.
  /// <para />
  /// Flips <see cref="Reversed"/> if <see cref="AnimationOptions.IsPingPong">AnimationOptions.IsPingPong</see> /
  /// <see cref="AnimTag.IsPingPong">AnimTag.IsPingPong</see> is <see langword="true"/>.
  /// </summary>
  /// <param name="options"></param>
  /// <param name="delta"></param>
  private void Play(AnimationOptions options, float delta) {
    float duration = CurrentFrame.Duration;
    float newFrameTime = FrameTime + options.Speed * delta;

    // Do nothing if not enough time has passed to advance to the next frame
    if (newFrameTime < duration || duration <= 0) {
      FrameTime = newFrameTime;
      return;
    }

    AnimTag currentTag = CurrentTag;
    var frames = currentTag.Frames;
    int loopCount = options.LoopCount ?? currentTag.LoopCount;
    bool isReversed = options.IsReversed ?? currentTag.IsReversed;
    bool isPingPong = options.IsPingPong ?? currentTag.IsPingPong;

    int lastFrameIndex = Reversed ? 0 : frames.Length - 1;

    int newFrameIndex = FrameIndex;
    while (newFrameTime >= duration) {
      // Determine next frame
      bool endOfFrame = newFrameIndex == lastFrameIndex;
      if (endOfFrame) {
        if (loopCount > 0 && TimesLooped + 1 >= loopCount || frames.Length == 1) {
          // Do not change frame
          break;
        }

        TimesLooped++;

        // Ping-pong: flip state, otherwise set to argument or default state
        Reversed = isPingPong ? !Reversed : isReversed;
        newFrameIndex = Reversed ? frames.Length - 1 : 0;
        if (isPingPong) {
          // Skip first ping-pong frame, as it's same as last frame
          newFrameIndex += Reversed ? -1 : 1;
        }

        lastFrameIndex = Reversed ? 0 : frames.Length - 1;
      }
      else {
        newFrameIndex += !Reversed ? 1 : -1;
      }

      newFrameTime -= duration;
      duration = currentTag.Frames[newFrameIndex].Duration;
    }

    FrameIndex = newFrameIndex;
    FrameTime = newFrameTime;
  }

  private static SpriteEffects SpriteEffectsFromPlayer(Player player) {
    SpriteEffects effects = SpriteEffects.None;
    if (player.direction < 0) effects |= SpriteEffects.FlipHorizontally;
    if (player.gravDir < 0) effects |= SpriteEffects.FlipVertically;
    return effects;
  }

  private void SetTag(string newTagName, bool? isReversed = null) {
    if (!SpriteSheet.TryGetTag(newTagName, out AnimTag? tag)) {
      throw new ArgumentException($"{Name} does not have any AnimTags with name \"{newTagName}\"");
    }

    SetTag(tag, isReversed);
  }

  private void SetTag(AnimTag tag, bool? isReversed = null) {
    CurrentTag = tag;
    FrameTime = 0;
    Reversed = isReversed ?? tag.IsReversed;
    FrameIndex = Reversed ? tag.Frames.Length - 1 : 0;
    TimesLooped = 0;
  }
}
