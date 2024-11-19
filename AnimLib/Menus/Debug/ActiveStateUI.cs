using AnimLib.States;
using AnimLib.UI.Debug;

namespace AnimLib.Menus.Debug;

/// <summary>
/// Debug UI that displays info about a specified <see cref="State"/>.
/// </summary>
public class ActiveStateUI : DebugUIState<AnimCharacter> {
  protected override int XOffset => 200;

  public override void OnInitialize() {
    base.OnInitialize();
    AddHeader("Active State Info");
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (Header?.IsMouseHovering ?? false) {
      Main.hoverItemName = "Displays info about all active states on the selected character.";
    }

    if (State is null) {
      DrawAppendLine("No state specified.", Color.LightGray);
      return;
    }

    if (!State.Entity.active) {
      return;
    }

    State.DebugHeader(this, redOnInactive: true);
    DrawNestedActiveChildren(State);
  }

  private void DrawNestedActiveChildren(CompositeState state) {
    foreach (State child in state.ActiveChildren) {
      Color = Color.White;
      child.DebugHeader(this);
      using (Indent()) {
        child.DebugText(this);
        if (child is CompositeState compositeState) {
          DrawNestedActiveChildren(compositeState);
        }
      }
    }
  }
}
