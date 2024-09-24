using System.Linq;
using System.Runtime.CompilerServices;
using AnimLib.Commands;
using AnimLib.States;
using JetBrains.Annotations;
using ReLogic.Graphics;
using Terraria.GameContent;
using Terraria.UI;

namespace AnimLib.UI;

[Autoload(Side = ModSide.Client)]
[UsedImplicitly]
public sealed class DebugSystem : ModSystem {
  private UserInterface _uiInterface = null!; // Load()
  private DebugPlayerAnimationUI _ui = null!; // Load()

  public override void Load() {
    _ui = new DebugPlayerAnimationUI();
    _ui.Activate();
    _uiInterface = new UserInterface();
    _uiInterface.SetState(_ui);
  }

  public override void UpdateUI(GameTime gameTime) {
    if (AnimDebugCommand.DebugEnabled) {
      _uiInterface.Update(gameTime);
    }
  }

  public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) {
    int mouseTextIndex = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Mouse Text"));
    if (mouseTextIndex != -1) {
      layers.Insert(mouseTextIndex, new LegacyGameInterfaceLayer(
        "AnimLib: Debug Animation",
        delegate {
          _uiInterface.Draw(Main.spriteBatch, new GameTime());
          return true;
        },
        InterfaceScaleType.UI)
      );
    }
  }
}

// ReSharper disable once InconsistentNaming "UI"
public class DebugPlayerAnimationUI : UIState {
  public override void Draw(SpriteBatch spriteBatch) {
    const int xSize = 320;
    const int xOffset = 120;
    const int yOffset = 16;

    AnimPlayer animPlayer = Main.LocalPlayer.GetModPlayer<AnimPlayer>();
    AnimCharacter? character = animPlayer.Characters.ActiveCharacter;
    if (character is not { IsActive: true }) {
      return;
    }

    int count = character.ActiveChildren.OfType<AnimatedStateMachine>().Count();
    if (count == 0) {
      return;
    }

    Vector2 baseDrawPosition = Main.ScreenSize.ToVector2() * 0.5f;
    baseDrawPosition.X -= 100 + (count - 1) * xSize / 2f;
    baseDrawPosition.Y -= 160;

    Vector2 pos = baseDrawPosition;

    pos.Y -= yOffset;
    Color color = new(0, 1f, 0);
    DrawString("Character:", character.Name);
    color = Color.White;
    pos.Y += yOffset;


    foreach (AnimatedStateMachine state in character.ActiveChildren.OfType<AnimatedStateMachine>()) {
      DrawAppendString("State:", state.Name);
      DrawAppendString("AnimTag:", state.CurrentTag.Name);
      DrawAppendString("Frame:", $"{state.FrameIndex} / {state.CurrentTag.Frames.Length}");
      DrawAppendString("Frame (Atlas):", state.CurrentFrame.AtlasFrameIndex.ToString());

      pos.X += xSize;
      pos.Y = baseDrawPosition.Y;
    }

    return;

    void DrawString(string key, string value) {
      spriteBatch.DrawString(FontAssets.MouseText.Value, key, pos, color);
      pos.X += xOffset;
      spriteBatch.DrawString(FontAssets.MouseText.Value, value, pos, color);
      pos.X -= xOffset;
    }

    void DrawAppendString(string key, string value) {
      DrawString(key, value);
      pos.Y += yOffset;
    }
  }
}
