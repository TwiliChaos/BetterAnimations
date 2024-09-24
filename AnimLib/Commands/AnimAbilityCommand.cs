using System.Diagnostics;
using System.Linq;
using AnimLib.States;
using JetBrains.Annotations;

namespace AnimLib.Commands;

[UsedImplicitly]
internal class AnimAbilityCommand : ModCommand {
  public override string Command => "animability";

  public override string Usage => "/animability <character> [ability] [level]";

  public override string Description =>
    "/animability -> When exactly one character is registered, acts as the below command.\n" +
    "/animability <character> -> View the specified character's abilities.\n" +
    "/animability <character> [ability] -> View the specified ability's information.\n" +
    "/animability <character> [ability] [level] -> Sets the specified ability's level.";

  public override CommandType Type => CommandType.Chat;

  public override void Action(CommandCaller caller, string input, string[] args) {
    Message message = Action(caller, args);
    if (message.Text != string.Empty) {
      caller.Reply(message.Text, message.Color);
    }
  }

  private static Message Action(CommandCaller caller, IReadOnlyList<string> args) {
    if (!AnimDebugCommand.DebugEnabled) {
      return Error("This command cannot be used outside of debug mode.");
    }

    int idx = 0; // Index of args, incremented in HasNextArg
    string arg;

    AnimPlayer player = caller.Player.GetModPlayer<AnimPlayer>();
    AnimCharacterCollection characters = player.Characters;
    AnimCharacter character;

    switch (characters.Children.Count) {
      case 0:
        return Warn($"This command cannot be used when no characters are registered to {nameof(AnimLib)}.");
      case 1:
        character = characters.Characters.First();
        break;
      case > 1:
        if (!HasNextArg(out arg)) {
          caller.Reply($"Must specify character when more than one is registered {nameof(AnimLib)}.", Color.Yellow);
          foreach (AnimCharacter c in characters.Characters) {
            caller.Reply("  " + c.Name);
          }

          return None();
        }

        if (!characters.ChildrenByType.TryGetValue(arg, out State? state)) {
          return Error($"{arg} is not a valid character");
        }

        character = (AnimCharacter)state;
        break;
      default: throw new UnreachableException("Count is less than 0");
    }

    var abilityStateMachines = character.AbilityStates;

    if (!HasNextArg(out arg)) {
      // Command reading list of abilities and their stats
      foreach (AbilityState state in abilityStateMachines) {
        caller.Reply(state.DebugText());
      }

      return None();
    }

    AbilityState? asm = null;

    foreach (AbilityState a in abilityStateMachines) {
      if (!string.Equals(a.Name, arg, StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      asm = a;
      break;
    }

    if (asm is null) {
      return Error($"\"{arg}\" is not a valid ability name.");
    }

    int level = asm.Level;
    int maxLevel = asm.MaxLevel;
    if (!HasNextArg(out arg)) {
      // Command is reading specific ability
      string msg = $"{asm.GetType().Name} is {(asm.Unlocked ? "Unlocked" : "Locked")} ";
      msg += $" at level {level}/{maxLevel}";

      return Success(msg);
    }

    if (!int.TryParse(arg, out level)) {
      return Error($"Argument {arg} must be a number.");
    }

    if (level < 0) {
      return Error($"Argument {arg} must be a positive number.");
    }

    asm.Level = level;

    string name = asm.Name;
    return level > maxLevel
      ? SuccessWarn($"{name} level set to {level}/{maxLevel}. This is above max level, and is not supported.")
      : Success($"{name} level set to {level}/{maxLevel}.");

    bool HasNextArg(out string argument) {
      if (args.Count <= idx) {
        argument = string.Empty;
        return false;
      }

      argument = args[idx++];
      return true;
    }
  }

  private static Message Error(string message) => new(message, Color.Red);
  private static Message SuccessWarn(string message) => new(message, Color.GreenYellow);
  private static Message Warn(string message) => new(message, Color.Yellow);
  private static Message Success(string message) => new(message, Color.LightGreen);
  private static Message None() => new(string.Empty, Color.White);

  private readonly ref struct Message(string text, Color color) {
    public readonly Color Color = color;
    public readonly string Text = text;
  }
}
