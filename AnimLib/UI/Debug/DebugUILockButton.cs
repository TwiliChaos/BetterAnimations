using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace AnimLib.UI.Debug;

public class DebugUILockButton : UIImageButton {
  private readonly DebugUIState _state;

  public DebugUILockButton(DebugUIState state) : base(AnimLibMod.Instance.Assets.Request<Texture2D>("AnimLib/UI/Unlock", AssetRequestMode.ImmediateLoad)) {
    _state = state;
    OnLeftClick += LockButton_OnClick;
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (IsMouseHovering) {
      Main.hoverItemName = _state.Locked ? "Unlock selected state" : "Keep current state selected";
    }
  }

  private void LockButton_OnClick(UIMouseEvent evt, UIElement listeningElement) {
    _state.Locked = !_state.Locked;
    SetImage(AnimLibMod.Instance.Assets.Request<Texture2D>(_state.Locked
      ? "AnimLib/UI/Lock"
      : "AnimLib/UI/Unlock", AssetRequestMode.ImmediateLoad));
  }
}
