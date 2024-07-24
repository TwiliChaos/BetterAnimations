using JetBrains.Annotations;

namespace AnimLib.Commands;

[UsedImplicitly]
internal class AnimDebugCommand : ModCommand {
  public override string Command => "animdebug";
  public override CommandType Type => CommandType.Chat;

  public override void Action(CommandCaller caller, string input, string[] args) {
    AnimPlayer? localPlayer = AnimPlayer.Local;
    if (localPlayer is null) {
      return;
    }

    localPlayer.DebugEnabled ^= true;
    caller.Reply($"Set AnimLib debug mode to {localPlayer.DebugEnabled}");
  }
}
