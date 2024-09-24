using JetBrains.Annotations;

namespace AnimLib.Commands;

[UsedImplicitly]
internal class AnimDebugCommand : ModCommand {
  public override string Command => "animdebug";
  public override CommandType Type => CommandType.Chat;

#if DEBUG
  public static bool DebugEnabled { get; private set; } = true;
#else
  public static bool DebugEnabled { get; private set; };
#endif

  public override void Action(CommandCaller caller, string input, string[] args) {
    DebugEnabled ^= true;
    caller.Reply($"Set AnimLib debug mode to {DebugEnabled}");
  }
}
