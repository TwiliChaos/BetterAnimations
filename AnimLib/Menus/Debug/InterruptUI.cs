using AnimLib.States;
using AnimLib.UI.Debug;

namespace AnimLib.Menus.Debug;

/// <summary>
/// Debug UI that displays a list of interruptible states on currently active states.
/// </summary>
public class InterruptUI : DebugUIState<AnimCharacter> {
  protected override int XOffset => 150;

  public override void OnInitialize() {
    base.OnInitialize();
    AddHeader("Interruptible States");
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (IsMouseHovering) {
      if (Header!.IsMouseHovering) {
        Main.hoverItemName =
          "Displays a list of states which can interrupt the\ncurrent active state on the selected character.";
      }
    }

    if (State is null) {
      DrawAppendLine("No character specified.", Color.LightGray);
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

      List<State>? interrupt = null;
      bool hasInterrupts = state is StateMachine sm && sm.Interrupts.TryGetValue(child.GetType(), out interrupt);
      if (child is CompositeState || hasInterrupts) {
        child.DebugHeader(this);
      }

      if (hasInterrupts) {
        using (Indent()) {
          foreach (State interruptState in interrupt!) {
            DrawAppendLine(interruptState.Name, interruptState.CanEnter() ? Green : Color.LightGray);
          }
        }
      }

      if (child is CompositeState compositeState) {
        DrawNestedActiveChildren(compositeState);
      }
    }
  }
}
