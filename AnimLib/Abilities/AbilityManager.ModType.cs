using AnimLib.Internal;

namespace AnimLib.Abilities;

public partial class AbilityManager : ModType<Player, AbilityManager>, IIndexed {
  public override AbilityManager NewInstance(Player entity) {
    AbilityManager newInstance = base.NewInstance(entity);
    newInstance.Index = Index;
    return newInstance;
  }

  protected override void Register() {
    AnimLoader.Add(this);
    ModTypeLookup<AbilityManager>.Register(this);
  }

  protected override Player CreateTemplateEntity() => null!;

  public ushort Index { get; internal set; }
}
