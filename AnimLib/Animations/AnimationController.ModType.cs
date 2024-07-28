using AnimLib.Internal;
using Terraria.ID;

namespace AnimLib.Animations;

public partial class AnimationController : ModType<Player, AnimationController> {
  /// <summary>
  /// Returns <see langword="false"/> if this is running on a Server.
  /// </summary>
  public override bool IsLoadingEnabled(Mod mod) {
    return Main.netMode != NetmodeID.Server;
  }

  protected sealed override void Register() {
    AnimLoader.Add(this);
    ModTypeLookup<AnimationController>.Register(this);
  }

  protected override Player CreateTemplateEntity() => null!;
}
