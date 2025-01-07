using System.Linq;
using System.Text;
using AnimLib.Animations;
using AnimLib.States;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace AnimLib.UI.Debug;

public abstract class DebugUIElement : DraggablePanel {
  protected DebugUIElement() {
    SetPadding(0);
  }

  protected abstract string HeaderHoverText { get; }

  protected UIText HeaderText = null!;
  protected UIPanel HeaderContainer = null!;
  protected UIElement BodyContainer = null!;

  public static Color Green => new(0, 1f, 0);
  public static Color Red => new(1f, 0.2f, 0.2f);

  public override void OnInitialize() {
    base.OnInitialize();

    HeaderContainer = new UIPanel {
      Width = StyleDimension.Fill,
      Height = StyleDimension.FromPixels(40),
      IgnoresMouseInteraction = true
    };
    HeaderContainer.SetPadding(0);
    Append(HeaderContainer);

    HeaderText = new UIText(string.Empty, 1.2f) {
      HAlign = 0.5f,
      VAlign = 0.5f,
      IgnoresMouseInteraction = true
    };
    HeaderContainer.Append(HeaderText);

    BodyContainer = new UIElement {
      VAlign = 1,
      Width = StyleDimension.Fill,
      Height = StyleDimension.FromPixelsAndPercent(-40, 1),
    };
    BodyContainer.SetPadding(4);
    Append(BodyContainer);
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (IsMouseHovering && HeaderText.ContainsPoint(Main.MouseScreen)) {
      Main.instance.MouseText(HeaderHoverText);
    }
  }

  /// <summary>
  /// Set the text of the Header element.
  /// Only call this after <see cref="DebugUIElement"/> base class has been initialized.
  /// </summary>
  /// <param name="text"></param>
  protected void SetHeaderText(string text) => HeaderText.SetText(text);
}

public abstract class DebugUIElement<TState> : DebugUIElement where TState : State {
  protected TState? State { get; private set; }

  /// <summary>
  /// Determines whether the selected element may change in the UI if it becomes inactive.
  /// If <see langword="true"/>, the element will not be changed if it goes inactive.
  /// If <see langword="false"/>, the element will be replaced with the active sibling.
  /// </summary>
  private bool _locked;

  private UIImageButton _lockButton = null!;

  private Asset<Texture2D> _lockTexture = null!;
  private Asset<Texture2D> _unlockTexture = null!;

  private string _lockButtonHoverText = "Keep current state selected";

  public override void OnInitialize() {
    base.OnInitialize();

    HeaderContainer.Width.Pixels = -40;

    TextureDictionary lockButtonTextures = AnimLibMod.Instance.Assets
      .Request<TextureDictionary>("AnimLib/UI/Lock", AssetRequestMode.ImmediateLoad).Value;

    _unlockTexture = lockButtonTextures["unlock"];
    _lockTexture = lockButtonTextures["lock"];
    _unlockTexture.Wait();
    _lockTexture.Wait();

    _lockButton = new UIImageButton(_unlockTexture) {
      HAlign = 1f,
      VAlign = 0f,
      Top = StyleDimension.FromPixels(6),
      Left = StyleDimension.FromPixels(-6)
    };
    Append(_lockButton);
    _lockButton.OnLeftClick += LockButton_OnClick;
  }

  public override void Update(GameTime gameTime) {
    if (!_locked && State is { Active: false }) {
      State = State.Parent?.ActiveChildren.FirstOrDefault() as TState;
    }

    base.Update(gameTime);
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (_lockButton.IsMouseHovering) {
      Main.instance.MouseText(_lockButtonHoverText);
    }
  }

  public void SetState(TState? element) {
    if (_locked) {
      return;
    }

    State = element;
    UpdateLockButtonHoverText();
    OnSetState(element);
  }

  protected virtual void OnSetState(TState? element) {
  }

  private void LockButton_OnClick(UIMouseEvent evt, UIElement listeningElement) {
    _locked ^= true;
    _lockButton.SetImage(_locked ? _lockTexture : _unlockTexture);
    UpdateLockButtonHoverText();
  }

  private void UpdateLockButtonHoverText() {
    StringBuilder sb = new();
    sb.AppendLine(_locked ? "Unlock selected state" : "Keep current state selected");

    if (State is not null) {
      sb.AppendLine("Current Selection: " + ((State as AnimCharacter)?.DisplayName ?? State.Name));

      // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
      // Player may be null for the template state
      if (State.Player is not null) {
        sb.Append("Player: " + State.Player.name);
      }
    }
    else {
      sb.Append("No state selected");
    }

    _lockButtonHoverText = sb.ToString();
  }
}
