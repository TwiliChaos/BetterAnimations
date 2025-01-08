using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.UI;

namespace AnimLib.UI.Elements;

public sealed class UIAnimCharacterButton : UIElement {
  /// <summary> Colors used by the null character (vanilla character/human)</summary>
  private static readonly AnimCharacterStyle DefaultStyle = new() {
    HairColor = new Color(215, 90, 55),
    SkinColor = new Color(255, 125, 90),
    EyeColor = new Color(105, 90, 75),
    ShirtColor = new Color(175, 165, 140),
    UnderShirtColor = new Color(160, 180, 215),
    PantsColor = new Color(255, 230, 175),
    ShoeColor = new Color(160, 105, 60)
  };

  public readonly Player Player;

  /// <summary> Character which this Button would assign. </summary>
  public readonly AnimCharacter? Character;

  private readonly Asset<Texture2D> _basePanel;
  private readonly Asset<Texture2D> _border;
  private readonly Asset<Texture2D> _hoveredBorder;
  private readonly UICharacter _char;
  private bool _hovered;
  private bool _soundedHover;

  /// <summary> Character which is currently selected by the user. </summary>
  private AnimCharacter? _realCharacter;

  private int _realSkinVariant;
  private int _realHair;
  private readonly AnimCharacterStyle _realStyle = new();

  public UIAnimCharacterButton(Player player, AnimCharacter? character) {
    Player = player;
    Character = character is not null ? (AnimCharacter)player.GetState(character) : null;
    Width = StyleDimension.FromPixels(44f);
    Height = StyleDimension.FromPixels(80f);

    _basePanel =
      Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanel", AssetRequestMode.ImmediateLoad);
    _border =
      Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelHighlight", AssetRequestMode.ImmediateLoad);
    _hoveredBorder =
      Main.Assets.Request<Texture2D>("Images/UI/CharCreation/CategoryPanelBorder", AssetRequestMode.ImmediateLoad);

    _char = new UICharacter(Player, hasBackPanel: false) {
      HAlign = 0.5f,
      VAlign = 0.5f
    };
    Append(_char);
  }

  public override void Draw(SpriteBatch spriteBatch) {
    AnimCharacterCollection characters = Player.GetState<AnimCharacterCollection>();
    GetRealValues();
    if (Character is not null) {
      characters.TrySetActiveChild(Character);
    }
    else {
      characters.ClearActiveChild();
    }

    SetCharacterValues();
    base.Draw(spriteBatch);
    if (_realCharacter is null) {
      characters.ClearActiveChild();
    }
    else {
      characters.TrySetActiveChild(_realCharacter);
    }

    SetRealValues();
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (_hovered && !_soundedHover) {
      SoundEngine.PlaySound(in SoundID.MenuTick);
    }

    _soundedHover = _hovered;

    CalculatedStyle dimensions = GetDimensions();
    int w = (int)dimensions.Width;
    int h = (int)dimensions.Height;
    int x = (int)dimensions.X;
    int y = (int)dimensions.Y;
    Utils.DrawSplicedPanel(spriteBatch, _basePanel.Value, x, y, w, h, 10, 10, 10, 10, Color.White * 0.5f);
    if (CharactersEqual(_realCharacter, Character)) {
      Utils.DrawSplicedPanel(spriteBatch, _border.Value, x + 3, y + 3, w - 6, h - 6, 10, 10, 10, 10, Color.White);
    }

    if (_hovered) {
      Utils.DrawSplicedPanel(spriteBatch, _hoveredBorder.Value, x, y, w, h, 10, 10, 10, 10, Color.White);
    }

    return;

    static bool CharactersEqual(AnimCharacter? a, AnimCharacter? b) {
      if (a is null) {
        return b is null;
      }

      if (b is null) {
        return false;
      }

      return a.Index == b.Index;
    }
  }

  public override void LeftMouseDown(UIMouseEvent evt) {
    AnimCharacterCollection characters = Player.GetState<AnimCharacterCollection>();
    if (Character is not null) {
      characters.TrySetActiveChild(Character);
    }
    else {
      characters.ClearActiveChild();
    }

    SetCharacterValues();
    SoundEngine.PlaySound(in SoundID.MenuTick);
    base.LeftMouseDown(evt);
  }

  public override void MouseOver(UIMouseEvent evt) {
    base.MouseOver(evt);
    _hovered = true;
    _char.SetAnimated(true);
  }

  public override void MouseOut(UIMouseEvent evt) {
    base.MouseOut(evt);
    _hovered = false;
    _char.SetAnimated(false);
  }

  /// <summary>
  /// Assign fields from the current active <see cref="AnimCharacter"/> to this UI element's fields.
  /// </summary>
  private void GetRealValues() {
    _realCharacter = Player.GetActiveCharacter();
    _realSkinVariant = Player.skinVariant;
    _realHair = Player.hair;
    _realStyle.AssignFromPlayer(Player);
  }

  /// <summary>
  /// Set the player's appearance to match the active <see cref="AnimCharacter"/>.
  /// </summary>
  private void SetRealValues() {
    Player.skinVariant = _realSkinVariant;
    Player.armor[11] = Player.armor[12] = new Item();

    // if (character.StarterShirt)
    //   _player.armor[11] = new Item(ItemID.FamiliarShirt);
    // if (character.StarterPants)
    //   _player.armor[12] = new Item(ItemID.FamiliarPants);
    Player.hair = _realHair;
    _realStyle.AssignToPlayer(Player);
  }

  /// <summary>
  /// Set the player's appearance to match this button's <see cref="AnimCharacter"/>.
  /// </summary>
  private void SetCharacterValues() {
    AnimCharacter? character = Player.GetActiveCharacter();
    AnimCharacterStyle style = character?.Style ?? DefaultStyle;
    style.AssignToPlayer(Player);
    if (character is null) {
      return;
    }

    ReadOnlySpan<int> variants = Player.Male
      ? [0, 2, 1, 3, 8]
      : [4, 6, 5, 7, 9];

    // _player.skinVariant = variants[character.ClothStyle - 1];

    // int shirtType = character.StarterShirt ? ItemID.FamiliarShirt : ItemID.None;
    // int pantsType = character.StarterPants ? ItemID.FamiliarPants : ItemID.None;
    //
    // Item shirt = Player.armor[11];
    // if (shirt.type != shirtType) {
    //   shirt.SetDefaults(shirtType);
    // }
    //
    // Item pants = Player.armor[12];
    // if (pants.type != pantsType) {
    //   pants.SetDefaults(pantsType);
    // }

    // _player.hair = character.HairStyle;
    Player.hairColor = style.HairColor;
    Player.skinColor = style.SkinColor;

    // animPlayer.DetailColor = colors.DetailColor;
    Player.eyeColor = style.EyeColor;
    Player.shirtColor = style.ShirtColor;
    Player.underShirtColor = style.UnderShirtColor;
    Player.pantsColor = style.PantsColor;
    Player.shoeColor = style.ShoeColor;
  }
}
