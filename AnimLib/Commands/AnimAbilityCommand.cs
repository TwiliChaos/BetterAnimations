using System.Linq;
using AnimLib.Abilities;

namespace AnimLib.Commands;

[UsedImplicitly]
internal class AnimAbilityCommand : ModCommand {
  public override string Command => "animability";

  public override string Usage => "/animability <mod> <ability> [level]";
  public override string Description => "Get or set the ability level.";

  public override CommandType Type => CommandType.Chat;

  public override void Action(CommandCaller caller, string input, string[] args) {
    Message message = Action(caller, args);
    caller.Reply(message.Text, message.Color);
  }

  private Message Action(CommandCaller caller, IReadOnlyList<string> args) {
    int idx = 0;

    AnimPlayer player = caller.Player.GetModPlayer<AnimPlayer>();
    AnimCharacterCollection characters = player.characters;

    if (!player.DebugEnabled) {
      return Error("This command cannot be used outside of debug mode.");
    }

    if (characters.Count == 0) {
      return Error($"This command cannot be used when no mods are using {nameof(AnimLib)}.");
    }

    if (!HasNextArg(out string arg)) {
      return Error($"This command requires arguments. Usage: {Usage}");
    }

    if (!ModLoader.TryGetMod(arg, out Mod targetMod)) {
      // We'll allow not specifying mod only if exactly one mod is using AnimLib
      var charactersKeys = characters.Keys.ToArray();
      if (charactersKeys.Length > 1) {
        return Error($"Must specify mod when more than one mod is using {nameof(AnimLib)}.");
      }

      // Only one mod is loaded, command implicitly refers to that mod
      targetMod = charactersKeys.First();
      idx--;
    }

    if (!HasNextArg(out arg)) {
      return Error("This command requires at least 2 arguments.");
    }

    if (!characters.TryGetValue(targetMod, out AnimCharacter character)) {
      return Error($"Mod {targetMod} does not use AnimLib.");
    }

    AbilityManager manager = character.abilityManager;
    if (manager is null) {
      return Error($"Mod {targetMod} does not have abilities.");
    }

    Ability ability = null;
    if (int.TryParse(arg, out int id)) {
      if (!manager.TryGet(id, out ability)) {
        return Error("Specified ability ID is out of range.");
      }

      ability = manager[id];
    }
    else {
      foreach (Ability a in manager) {
        if (string.Equals(a.GetType().Name, arg, StringComparison.OrdinalIgnoreCase)) {
          ability = a;
          break;
        }
      }

      if (ability is null) {
        return Error($"\"{arg}\" is not a valid ability name.");
      }
    }

    ILevelable levelable = ability as ILevelable;
    if (!HasNextArg(out arg)) {
      // Command is only Reading
      return Success(levelable is null
        ? $"{ability.GetType().Name} is currently {(ability.Unlocked ? "Unlocked" : "Locked")} "
        : $"{ability.GetType().Name} is currently {(ability.Unlocked ? "Unlocked" : "Locked")} at level {levelable.Level}/{levelable.MaxLevel}");
    }

    if (!int.TryParse(arg, out int level)) {
      return Error("Argument must be a number.");
    }

    if (level < 0) {
      return Error("Argument must be a positive number.");
    }

    if (levelable is null) {
      return Error($"{ability} cannot be leveled.");
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
