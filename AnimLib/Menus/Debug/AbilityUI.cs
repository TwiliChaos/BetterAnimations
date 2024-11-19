using AnimLib.States;
using AnimLib.UI.Debug;

namespace AnimLib.Menus.Debug;

/// <summary>
/// Debug UI that displays info about all <see cref="AbilityState"/>s in an <see cref="AnimCharacter"/>.
/// Info includes ability name, level, max level, cooldown time, and whether the ability is on cooldown.
/// </summary>
public class AbilityUI : DebugUIState<AnimCharacter> {
  protected override int XOffset => 150;

  public override void OnInitialize() {
    base.OnInitialize();
    AddHeader("Abilities");
  }

  protected override void DrawSelf(SpriteBatch spriteBatch) {
    base.DrawSelf(spriteBatch);
    if (IsMouseHovering) {
      if (Header!.IsMouseHovering) {
        Main.hoverItemName = "Displays info about all abilities on the selected character.";
      }
    }

    if (State is null) {
      DrawAppendLine("No character specified.", Color.LightGray);
      return;
    }

    if (!State.Entity.active) {
      return;
    }

    float baseX = TextPosition.X;
    DrawText("Ability");
    TextPosition.X += XOffset;
    DrawText("Level");
    TextPosition.X += 50;
    DrawText("(Max)");
    TextPosition.X += 80;
    DrawText("CD Time");
    TextPosition.X += 80;
    DrawText("On CD");
    TextPosition.X = baseX;
    TextPosition.Y += YOffset;

    foreach (AbilityState ability in State.AbilityStates) {
      Color levelColor = ability.Level > ability.MaxLevel ? Color.Yellow : ability.Level > 0 ? Green : Color.LightGray;
      Color = ability.Unlocked ? Green : Color.LightGray;
      DrawText(ability.Name);
      TextPosition.X += XOffset;
      DrawText(ability.Level, color: levelColor);
      TextPosition.X += 50;
      DrawText(ability.MaxLevel, color: levelColor);
      TextPosition.X += 80;
      if (ability is { Unlocked: true, SupportsCooldown: true }) {
        DrawText(ability.CooldownLeft / 60f, format: ['G', '3']);
        TextPosition.X += 80;
        DrawText(ability.IsOnCooldown ? "On CD" : "Ready", color: ability.IsOnCooldown ? Red : Green);
      }

      TextPosition.X = baseX;
      TextPosition.Y += YOffset;
    }
  }
}
