using System.Linq;
using AnimLib.UI.Debug;

namespace AnimLib.Menus.Debug;

/// <summary>
/// Debug UI that displays info about a character collection's list of <see cref="AnimCharacter"/>.
/// </summary>
public class CharacterListUI : DebugUIState<AnimCharacterCollection> {
  protected override int XOffset => 160;

  public override void OnInitialize() {
    base.OnInitialize();
    AddHeader("Character List");
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (Header!.IsMouseHovering) {
      Main.hoverItemName = "Displays a list of all available characters for the selected player.";
    }

    if (State is null) {
      DrawAppendLine("No character collection specified.", Color.LightGray);
      return;
    }

    int count = State.Characters.Count();
    DrawAppendLabelValue("Character Count", count, count > 0 ? Green : Color.LightGray);

    if (count == 0) {
      return;
    }

    foreach (AnimCharacter character in State.Characters) {
      Color = Color.White;
      DrawAppendLine(character.Name, character.IsActive ? Green : Color.LightGray);
    }
  }
}
