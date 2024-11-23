using AnimLib.Commands;
using AnimLib.Menus.Debug;
using JetBrains.Annotations;
using Terraria.UI;

namespace AnimLib.UI.Debug;

[Autoload(Side = ModSide.Client)]
[UsedImplicitly]
public sealed class DebugUISystem : ModSystem {
  private UserInterface? _uiInterface;
  private CharacterCollectionUIState? _ui;
  private LegacyGameInterfaceLayer? _debugAnimationLayer;
  private GameTime? _gameTime;


  public void SetCharacters(AnimCharacterCollection collection) {
    _ui?.SetCharacters(collection);
  }

  /// <summary>
  /// Attempts to set the character displayed in the UI to the specified collection's active character.
  /// <para />
  /// This method does nothing if the UI is not currently displaying the specified collection.
  /// </summary>
  /// <param name="collection"></param>
  public void TrySetActiveCharacter(AnimCharacterCollection collection) {
    if (ReferenceEquals(_ui?.Characters, collection)) {
      _ui?.SetCharacter(collection.ActiveCharacter);
    }
  }

  public override void Load() {
    _ui = new CharacterCollectionUIState();
    _ui.Activate();
    _uiInterface = new UserInterface();
    _uiInterface.SetState(AnimLibMod.DebugEnabled ? _ui : null);
    _debugAnimationLayer = new LegacyGameInterfaceLayer(
      "AnimLib: Debug Animation",
      delegate {
        if (_gameTime is not null && _uiInterface?.CurrentState is not null) {
          _uiInterface.Draw(Main.spriteBatch, _gameTime);
        }

        return true;
      },
      InterfaceScaleType.UI);
  }

  public override void UpdateUI(GameTime gameTime) {
    _gameTime = gameTime;
    _uiInterface?.Update(gameTime);
  }

  public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
    int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
    if (mouseTextIndex != -1 && _debugAnimationLayer is not null) {
      layers.Insert(mouseTextIndex, _debugAnimationLayer);
    }
  }

  public void SetUIVisibility(bool visible) {
    _uiInterface?.SetState(visible ? _ui : null);
  }

  public void ToggleUI() => _uiInterface?.SetState(_uiInterface.CurrentState is not null ? null : _ui);
}
