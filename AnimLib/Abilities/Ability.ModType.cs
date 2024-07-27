﻿using AnimLib.Internal;

namespace AnimLib.Abilities;

public partial class Ability : ModType<Player, Ability> {
  public override Ability NewInstance(Player entity) {
    Ability newInstance = base.NewInstance(entity);
    return newInstance;
  }

  protected sealed override void Register() {
    AnimLoader.Add(this);
    ModTypeLookup<Ability>.Register(this);
  }

  protected override Player CreateTemplateEntity() => null!;
}