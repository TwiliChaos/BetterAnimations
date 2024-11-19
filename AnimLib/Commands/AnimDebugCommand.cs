using AnimLib.UI.Debug;
using JetBrains.Annotations;

namespace AnimLib.Commands;

[UsedImplicitly]
internal class AnimDebugCommand : ModCommand {
  public override string Command => "animdebug";
  public override CommandType Type => CommandType.Chat;

  public static bool DebugEnabled { get; private set; }
#if DEBUG
    = true;
#endif

  public override void Action(CommandCaller caller, string input, string[] args) {
    ToggleDebugMode();
    caller.Reply($"Set AnimLib debug mode to {DebugEnabled}");
  }

  internal static void ToggleDebugMode() {
    DebugEnabled ^= true;
    ModContent.GetInstance<DebugUISystem>().SetUIVisibility(DebugEnabled);
  }
}
