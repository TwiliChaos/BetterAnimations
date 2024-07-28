using AnimLib.Internal;

namespace AnimLib.Abilities;

public partial class Ability : ModType<Player, Ability> {
  protected sealed override void Register() {
    AnimLoader.Add(this);
    ModTypeLookup<Ability>.Register(this);
  }

  protected override Player CreateTemplateEntity() => null!;
}
