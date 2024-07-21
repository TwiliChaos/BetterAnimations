using AnimLib.Internal;

namespace AnimLib.Abilities;

public partial class Ability : ModType<Player, Ability>, IIndexed {
  public override Ability NewInstance(Player entity) {
    Ability newInstance = base.NewInstance(entity);
    newInstance.Index = Index;
    return newInstance;
  }

  protected override void Register() {
    AnimLoader.Add(this);
  }

  protected override Player CreateTemplateEntity() => null;

  public ushort Index { get; internal set; }
}
