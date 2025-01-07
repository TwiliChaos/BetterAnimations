using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace AnimLib.UI;

/// <summary>
/// Class from ExampleMod, used to create a <see cref="UIPanel"/> which supports drag behavior.
/// <see href="https://github.com/tModLoader/tModLoader/blob/1.4.4/ExampleMod/Common/UI/ExampleCoinsUI/ExampleDraggableUIPanel.cs">
/// ExampleMod ExampleDraggableUIPanel source.
/// </see>
/// </summary>
public class DraggablePanel : UIPanel {
  // Stores the offset from the top left of the UIPanel while dragging
  private Vector2 _offset;

  // A flag that checks if the panel is currently being dragged
  private bool _dragging;

  public override void LeftMouseDown(UIMouseEvent evt) {
    // When you override UIElement methods, don't forget call the base method
    // This helps to keep the basic behavior of the UIElement
    base.LeftMouseDown(evt);

    // When the mouse button is down on this element, then we start dragging
    if (evt.Target == this) {
      DragStart(evt);
    }
  }

  public override void LeftMouseUp(UIMouseEvent evt) {
    base.LeftMouseUp(evt);

    // When the mouse button is up, then we stop dragging
    if (evt.Target == this) {
      DragEnd(evt);
    }
  }

  private void DragStart(UIMouseEvent evt) {
    // The offset variable helps to remember the position of the panel relative to the mouse position
    // So no matter where you start dragging the panel, it will move smoothly
    _offset = new Vector2(evt.MousePosition.X - Left.Pixels, evt.MousePosition.Y - Top.Pixels);
    _dragging = true;
  }

  private void DragEnd(UIMouseEvent evt) {
    Vector2 endMousePosition = evt.MousePosition;
    _dragging = false;

    Left.Set(endMousePosition.X - _offset.X, 0f);
    Top.Set(endMousePosition.Y - _offset.Y, 0f);

    Recalculate();
  }

  public override void Update(GameTime gameTime) {
    base.Update(gameTime);

    // Checking ContainsPoint and then setting mouseInterface to true is very common
    // This causes clicks on this UIElement to not cause the player to use current items
    if (ContainsPoint(Main.MouseScreen)) {
      Main.LocalPlayer.mouseInterface = true;
    }

    if (_dragging) {
      Left.Set(Main.mouseX - _offset.X, 0f); // Main.MouseScreen.X and Main.mouseX are the same
      Top.Set(Main.mouseY - _offset.Y, 0f);
      Recalculate();
    }

    // Here we check if the DraggableUIPanel is outside the Parent UIElement rectangle
    // By doing this and some simple math, we can snap the panel back on screen if the user resizes his window or otherwise changes resolution
    // AL: Modified from example to allow no more than 1/4 of the panel to be outside the parent
    Rectangle parentSpace = Parent.GetDimensions().ToRectangle();
    int maxW = (int)Math.Min(Width.Pixels / 4, 100);
    int maxH = (int)Math.Min(Height.Pixels / 4, 100);
    parentSpace.Inflate(-maxW, -maxH);
    if (GetDimensions().ToRectangle().Intersects(parentSpace)) {
      return;
    }

    Left.Pixels = Utils.Clamp(Left.Pixels, 0, parentSpace.Right - Width.Pixels);
    Top.Pixels = Utils.Clamp(Top.Pixels, 0, parentSpace.Bottom - Height.Pixels);

    // Recalculate forces the UI system to do the positioning math again.
    Recalculate();
  }
}
