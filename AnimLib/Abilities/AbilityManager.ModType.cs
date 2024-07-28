using AnimLib.Internal;

namespace AnimLib.Abilities;

public partial class AbilityManager : ModType<Player, AbilityManager> {
  protected sealed override void Register() {
    AnimLoader.Add(this);
    ModTypeLookup<AbilityManager>.Register(this);
  }

  protected override Player CreateTemplateEntity() => null!;
}
