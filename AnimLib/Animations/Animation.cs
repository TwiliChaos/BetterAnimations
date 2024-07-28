using JetBrains.Annotations;
using Terraria.DataStructures;
using Terraria.ID;

namespace AnimLib.Animations;

/// <summary>
/// Animation for a single player.
/// This class uses runtime data from a <see cref="AnimationController"/> to retrieve values from an <see cref="AnimSpriteSheet"/>.
/// One of these will be created for each <see cref="AnimationController"/> you have in your mod, per player.
/// <para>To get an <see cref="Animation"/> instance from the player, use <see cref="AnimationController.RegisterAnimation"/>.</para>
/// </summary>
/// <remarks>
/// This class is essentially the glue between your <see cref="AnimationController"/> and all your <see cref="AnimSpriteSheet"/>.
/// </remarks>
[PublicAPI]
public sealed partial class Animation {
  /// <summary>
  /// <see cref="AnimationController"/> this <see cref="Animation"/> belongs to. This is used to get the current <see cref="AnimTag"/>s and
  /// <see cref="AnimFrame"/>s.
  /// </summary>
  public readonly AnimationController Controller;

  /// <summary>
  /// <see cref="AnimSpriteSheet"/> info used for this <see cref="Animation"/>.
  /// </summary>
  public readonly AnimSpriteSheet SpriteSheet;

  /// <summary>
  /// Creates a new instance of <see cref="Animation"/> for the given <see cref="AnimPlayer"/>, using the given <see cref="AnimSpriteSheet"/> and
  /// rendering with <see cref="PlayerDrawLayer"/>.
  /// </summary>
  /// <param name="controller"><see cref="AnimationController"/> instance this will belong to.</param>
  /// <param name="spriteSheet"><see cref="AnimSpriteSheet"/> to determine which sprite is drawn.</param>
  /// <exception cref="System.InvalidOperationException">Animation classes are not allowed to be constructed on a server.</exception>
  internal Animation(AnimationController controller, AnimSpriteSheet spriteSheet) {
    if (Main.netMode == NetmodeID.Server) {
      throw new InvalidOperationException("Animation classes are not allowed to be constructed on servers.");
    }

    Controller = controller;
    SpriteSheet = spriteSheet;
  }

  /// <summary>
  /// The index of the animation tag currently playing.
  /// </summary>
  public int CurrentTagIndex { get; internal set; }

  /// <summary>
  /// Current <see cref="AnimTag"/> that is being played.
  /// </summary>
  public AnimTag CurrentTag => SpriteSheet.Tags[CurrentTagIndex];

  /// <summary>
  /// Current <see cref="AnimFrame"/> that is being played.
  /// </summary>
  public AnimFrame CurrentFrame => CurrentTag.Frames[Controller.FrameIndex];

  /// <summary>
  /// Gets the <see cref="Rectangle"/> that represents the current sprite position and size based on the current
  /// <see cref="AnimFrame"/> on the provided <see cref="AnimTextureAtlas"/>.
  /// </summary>
  /// <param name="layer">
  /// The name of the <see cref="AnimTextureAtlas"/> to get the <see cref="Rectangle"/> from.
  /// </param>
  public Rectangle GetRect(string layer) {
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);
    return SpriteSheet.GetAtlasRect(layer, CurrentFrame.AtlasFrameIndex);
  }

  /// <summary>
  /// Texture of the <see cref="AnimTextureAtlas"/> whose name matches <param name="layer"/>
  /// </summary>
  /// <param name="layer">
  /// The name of the <see cref="AnimTextureAtlas"/> to get the <see cref="Texture2D"/> from.
  /// </param>
  public Texture2D GetTexture(string layer) {
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    var textureAtlasMap = SpriteSheet.Atlases;
    if (!textureAtlasMap.TryGetValue(layer, out AnimTextureAtlas? atlas)) {
      throw new ArgumentException($"Atlas with name \"{layer}\" does not exist.");
    }

    return atlas.GetTexture();
  }


  /// <summary>
  /// Gets a <see cref="DrawData"/> that is based on this <see cref="Animation"/>.
  /// <list type="bullet">
  /// <item><see cref="DrawData.texture"/> is the return value of <see cref="GetTexture"/> (recommended)</item>
  /// <item><see cref="DrawData.position"/> is the center of the <see cref="PlayerDrawSet.drawPlayer"/>, in screen-space. (recommended)</item>
  /// <item><see cref="DrawData.sourceRect"/> is the return value of <see cref="GetRect"/> (recommended)</item>
  /// <item><see cref="DrawData.rotation"/> is <see cref="Entity.direction"/> <see langword="*"/> <see cref="AnimationController.SpriteRotation"/> (recommended)</item>
  /// <item><see cref="DrawData.origin"/> is half of <see cref="DrawData.sourceRect"/>'s size</item>
  /// <item><see cref="DrawData.effect"/> is based on <see cref="Entity.direction"/> and <see cref="Player.gravDir"/>. (recommended)</item>
  /// </list>
  /// </summary>
  /// <remarks>
  /// If your sprites are asymmetrical and cannot be flipped (i.e. Samus from Metroid),
  /// you should modify <see cref="DrawData.effect"/> and <see cref="DrawData.rotation"/> to get your desired effect.<br />
  /// If your sprites are not correctly positioned in the world, you may need to tweak <see cref="DrawData.origin"/>.
  /// </remarks>
  /// <param name="drawInfo">Parameter of <see cref="PlayerDrawLayer.Draw(ref PlayerDrawSet)">PlayerDrawLayer.Draw(ref PlayerDrawSet)</see>.</param>
  /// <param name="layer">Name of the Atlas that the <paramref name="drawInfo"/> will be based on.</param>
  /// <returns>A <see cref="DrawData"/> based on this <see cref="Animation"/>.</returns>
  public DrawData GetDrawData(PlayerDrawSet drawInfo, string layer) {
    ArgumentException.ThrowIfNullOrWhiteSpace(layer);

    Player player = drawInfo.drawPlayer;
    Texture2D texture = GetTexture(layer);
    Vector2 position = drawInfo.Position - Main.screenPosition + player.Size / 2;
    Rectangle rect = GetRect(layer);
    SpriteEffects effect = Controller.Effects;
    Vector2 origin = new(rect.Width / 2f, rect.Height / 2f);

    return new DrawData(texture, position, rect, Color.White, Controller.SpriteRotation, origin, 1, effect);
  }

  /// <summary>
  /// Determines whether the <see cref="SpriteSheet"/> contains an <see cref="AnimTag"/> with the specified <paramref name="tagName"/>
  /// </summary>
  /// <param name="tagName">The name of the <see cref="AnimTag"/> to check.</param>
  /// <returns></returns>
  public bool ContainsTag(string tagName) {
    ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
    return SpriteSheet.TagDictionary.ContainsKey(tagName);
  }

  /// <summary>
  /// Gets the <see cref="AnimTag"/> with the specified <see cref="AnimTag.Name"/>.
  /// </summary>
  /// <param name="tagName">The name of the <see cref="AnimTag"/> to retrieve.</param>
  /// <param name="tag"></param>
  /// <returns></returns>
  public bool TryGetTag(string tagName, [NotNullWhen(true)] out AnimTag? tag) {
    ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
    return SpriteSheet.TagDictionary.TryGetValue(tagName, out tag);
  }

  /// <summary>
  /// Returns the index of the <see cref="AnimTag"/> with the specified name,
  /// -or- -1 if no such <see cref="AnimTag"/> exists.
  /// </summary>
  /// <param name="tagName"></param>
  /// <returns></returns>
  public int IndexOfTag(string tagName) {
    var tags = SpriteSheet.Tags;

    for (int i = 0; i < tags.Length; i++) {
      if (tags[i].Name == tagName) {
        return i;
      }
    }

    return -1;
  }
}
