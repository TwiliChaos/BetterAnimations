using AnimLib.UI.Debug;
using Terraria.UI;

namespace AnimLib.Menus.Debug;

/// <summary>
/// Debug UI state that displays info about a player's <see cref="AnimCharacterCollection"/>.
/// <para />
/// This menu does not draw anything, but contains all the UI panels for the character collection.
/// </summary>
public class CharacterCollectionUIState : UIState {
  public AnimCharacterCollection? Characters { get; private set; }
  private CharacterListUI? _characterList;
  private AnimatedStatesUI? _animatedStateUI;
  private ActiveStateUI? _activeStateUI;
  private AbilityUI? _abilityUI;
  private InterruptUI? _interruptUI;

  public void SetCharacters(AnimCharacterCollection? collection) {
    Characters = collection;
    _characterList?.SetState(collection);
    SetCharacter(collection?.ActiveCharacter);
  }

  public void SetCharacter(AnimCharacter? character) {
    _animatedStateUI?.SetState(character);
    _activeStateUI?.SetState(character);
    _abilityUI?.SetState(character);
    _interruptUI?.SetState(character);
  }

  public override void OnInitialize() {
    Width = StyleDimension.Fill;
    Height = StyleDimension.Fill;

    _activeStateUI = new ActiveStateUI();
    _activeStateUI.Width.Set(400, 0);
    _activeStateUI.Height.Set(300, 0);
    _activeStateUI.Top.Set(100, 0);
    _activeStateUI.Left.Set(100, 0);
    AddLockButton(_activeStateUI);
    Append(_activeStateUI);

    _abilityUI = new AbilityUI();
    _abilityUI.Width.Set(450, 0);
    _abilityUI.Height.Set(300, 0);
    _abilityUI.Top = _activeStateUI.Top;
    _abilityUI.Left.Set(100 + _activeStateUI.Width.Pixels + 10, 0);
    AddLockButton(_abilityUI);
    Append(_abilityUI);

    _animatedStateUI = new AnimatedStatesUI();
    _animatedStateUI.Width.Set(300, 0);
    _animatedStateUI.Height.Set(250, 0);
    _animatedStateUI.Top.Set(100 + _activeStateUI.Height.Pixels + 10, 0);
    _animatedStateUI.Left.Set(100, 0);
    AddLockButton(_animatedStateUI);
    Append(_animatedStateUI);
    _animatedStateUI.Recalculate();

    _characterList = new CharacterListUI();
    _characterList.Width.Set(240, 0);
    _characterList.Height.Set(180, 0);
    _characterList.Top = _animatedStateUI.Top;
    _characterList.Left.Set(_animatedStateUI.GetDimensions().ToRectangle().Right + 10, 0);
    AddLockButton(_characterList);
    Append(_characterList);
    _characterList.Recalculate();

    // InterruptUI
    _interruptUI = new InterruptUI();
    _interruptUI.Width.Set(300, 0);
    _interruptUI.Height.Set(300, 0);
    _interruptUI.Top.Set(100, 0);
    _interruptUI.Left.Set(_abilityUI.Left.Pixels + _abilityUI.Width.Pixels + 10, 0);
    AddLockButton(_interruptUI);
    Append(_interruptUI);
  }

  private static void AddLockButton(DebugUIState ui) {
    DebugUILockButton button = new(ui) {
      HAlign = 1f,
      VAlign = 0f
    };
    button.Width.Set(32, 0);
    button.Height.Set(32, 0);
    ui.Append(button);
  }
}
