using System.Linq;
using System.Text;
using AnimLib.Abilities;
using JetBrains.Annotations;

namespace AnimLib.Commands;

[UsedImplicitly]
internal class AnimAbilityCommand : ModCommand {
  public override string Command => "animability";

  public override string Usage => "/animability <mod> [ability] [level]";
  public override string Description => "Get or set the ability level.";

  public override CommandType Type => CommandType.Chat;

  public override void Action(CommandCaller caller, string input, string[] args) {
    Message message = Action(caller, args);
    caller.Reply(message.Text, message.Color);
  }

  private static Message Action(CommandCaller caller, IReadOnlyList<string> args) {
    int idx = 0;

    AnimPlayer player = caller.Player.GetModPlayer<AnimPlayer>();
    AnimCharacterCollection characters = player.Characters;

#if !DEBUG
    if (!player.DebugEnabled) {
      return Error("This command cannot be used outside of debug mode.");
    }
#endif

    if (characters.Count == 0) {
      return Error($"This command cannot be used when no mods are using {nameof(AnimLib)}.");
    }

    if (!HasNextArg(out string arg) && characters.Count > 1) {
      // Attempt to view abilities but more than one mod loaded
      return Error($"Must specify mod when more than one mod is using {nameof(AnimLib)}.");
    }

    if (!ModLoader.TryGetMod(arg, out Mod targetMod)) {
      // We'll allow not specifying mod only if exactly one mod is using AnimLib
      if (characters.Count > 1) {
        // Attempt to write value to an ability but more than one loaded
        return Error($"Must specify mod when more than one mod is using {nameof(AnimLib)}.");
      }

      // Only one mod is loaded, command implicitly refers to that mod
      targetMod = characters.Keys.First();
      idx--;
    }

    if (!characters.TryGetValue(targetMod, out AnimCharacter? character)) {
      return Error($"Mod {targetMod} does not use AnimLib.");
    }

    AbilityManager? manager = character.AbilityManager;
    if (manager is null) {
      return Error($"Mod {targetMod} does not have abilities.");
    }

    if (!HasNextArg(out arg)) {
      // Command reading list of abilities and their stats
      StringBuilder sb = new();
      foreach (Ability a in manager) {
        sb.AppendLine(a.ToString());
      }

      return Success(sb.ToString());
    }

    Ability? ability;
    if (int.TryParse(arg, out int id)) {
      if (!manager.TryGet(id, out ability)) {
        return Error("Specified ability ID is out of range.");
      }
    }
    else {
      ability = manager.FirstOrDefault(a => string.Equals(a.Name, arg, StringComparison.OrdinalIgnoreCase));
      if (ability is null) {
        return Error($"\"{arg}\" is not a valid ability name.");
      }
    }

    ILevelable? levelable = ability as ILevelable;
    if (!HasNextArg(out arg)) {
      // Command is reading specific ability
      string msg = $"{ability.GetType().Name} is currently {(ability.Unlocked ? "Unlocked" : "Locked")} ";
      if (levelable is not null) {
        msg += $" at level {levelable.Level}/{levelable.MaxLevel}";
      }

      return Success(msg);
    }

    if (!int.TryParse(arg, out int level)) {
      return Error($"Argument {arg} must be a number.");
    }

    if (level < 0) {
      return Error($"Argument {arg} must be a positive number.");
    }

    if (levelable is null) {
      return Error($"{ability.Name} cannot be leveled.");
    }

    levelable.Level = level;
    return level > levelable.MaxLevel
      ? SuccessWarn(
        $"{ability.GetType().Name} level set to {levelable.Level}/{levelable.MaxLevel}. This level is above max level, and is not supported.")
      : Success($"{ability.GetType().Name} level set to {levelable.Level}/{levelable.MaxLevel}.");

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
  private static Message Success(string message) => new(message, Color.LightGreen);

  private readonly ref struct Message(string text, Color color) {
    public readonly Color Color = color;
    public readonly string Text = text;
  }
}
