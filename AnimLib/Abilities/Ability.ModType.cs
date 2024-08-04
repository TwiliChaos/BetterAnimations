using AnimLib.Internal;

namespace AnimLib.Abilities;

public partial class Ability : ModType<Player, Ability> {
  protected sealed override void Register() {
    AnimLoader.Add(this);
  }

  protected sealed override Player CreateTemplateEntity() => null!;
}
