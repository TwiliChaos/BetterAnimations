using AnimLib.Internal;
using Terraria.ID;

namespace AnimLib.Animations;

public partial class AnimationController : ModType<Player, AnimationController>, IIndexed {
  public override AnimationController NewInstance(Player entity) {
    AnimationController newInstance = base.NewInstance(entity);
    newInstance.Index = Index;
    newInstance.Initialize();
    return newInstance;
  }

  public override bool IsLoadingEnabled(Mod mod) {
    return Main.netMode != NetmodeID.Server;
  }

  protected sealed override void Register() {
    AnimLoader.Add(this);
    ModTypeLookup<AnimationController>.Register(this);
  }

  protected override Player CreateTemplateEntity() => null;

  public ushort Index { get; internal set; }
}
