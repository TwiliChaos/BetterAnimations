using AnimLib.Internal;
using Terraria.ID;

namespace AnimLib.Animations;

public partial class AnimationController : ModType<Player, AnimationController> {
  /// <summary>
  /// <inheritdoc cref="ModType.IsLoadingEnabled"/>
  /// Returns <see langword="false"/> for <see cref="AnimationController"/> if this is running on a Server.
  /// </summary>
  public override bool IsLoadingEnabled(Mod mod) {
    return Main.netMode != NetmodeID.Server;
  }

  protected sealed override void Register() {
    AnimLoader.Add(this);
  }

  protected sealed override Player CreateTemplateEntity() => null!;
}
