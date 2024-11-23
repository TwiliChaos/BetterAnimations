using JetBrains.Annotations;
using Terraria.ModLoader.Config;

namespace AnimLib.Configs;

[UsedImplicitly]
public sealed class AnimLibConfig : ModConfig {
  public override ConfigScope Mode => ConfigScope.ClientSide;

  public bool DebugModeOnStart { get; set; } = false;

  public bool DebugModeEnabled {
    get => AnimLibMod.DebugEnabled;
    set => AnimLibMod.DebugEnabled = value;
  }

  public override void OnLoaded() {
    AnimLibMod.DebugEnabled = DebugModeOnStart;
  }
}
