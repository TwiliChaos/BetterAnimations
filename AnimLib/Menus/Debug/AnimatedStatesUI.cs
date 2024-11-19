using System.Linq;
using AnimLib.States;
using AnimLib.UI.Debug;

namespace AnimLib.Menus.Debug;

/// <summary>
/// Debug UI that displays info about all <see cref="AnimatedStateMachine"/>s in an <see cref="AnimCharacter"/>.
/// </summary>
public class AnimatedStatesUI : DebugUIState<AnimCharacter> {
  protected override int XOffset => 140;

  public override void OnInitialize() {
    base.OnInitialize();
    AddHeader("Animated States");
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (Header!.IsMouseHovering) {
      Main.hoverItemName = "Displays info about all animated states on the selected character.";
    }

    if (State is null) {
      DrawAppendLine("No character specified.", Color.LightGray);
      return;
    }

    DrawAppendLabelValue("Character", State.Name, State.IsEnabled ? Green : Red);

    if (!State.ActiveChildren.OfType<AnimatedStateMachine>().Any()) {
      DrawAppendLine("No animated states.", Color.LightGray);
      return;
    }

    foreach (AnimatedStateMachine state in State.ActiveChildren.OfType<AnimatedStateMachine>()) {
      Color = Color.White;
      state.DebugHeader(this);
      state.DebugAnimationText(this);
      TextPosition.Y += YOffset;
    }
  }
}
