using AnimLib.States;
using JetBrains.Annotations;

namespace AnimLib.Projectiles;

/// <summary>
/// Base class for ability projectiles.
/// </summary>
[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class AbilityProjectile<T> : AbilityProjectile where T : AbilityState {
  /// <summary>
  /// The <see cref="AbilityState"/> that this <see cref="AbilityProjectile{T}"/> belongs to.
  /// It's recommended that you set this value on projectile creation, else it will be searched for.
  /// </summary>
  public override T Ability =>
    (T)(base.Ability ??= Player.GetModPlayer<AnimPlayer>().Characters.FindAbility<T>());
}

public abstract class AbilityProjectile : ModProjectile {
  /// <summary>
  /// The level of the <see cref="AbilityState"/> when this <see cref="AbilityProjectile{T}"/> was created.
  /// </summary>
  public int Level {
    get => (int)Projectile.ai[0];
    set => Projectile.ai[0] = value;
  }

  /// <summary>
  /// The <see cref="Player"/> that this <see cref="AbilityProjectile{T}"/> belongs to.
  /// </summary>
  protected Player Player => _player ??= Main.player[Projectile.owner];

  private Player? _player;


  /// <summary>
  /// The <see cref="AbilityState"/> that this <see cref="AbilityProjectile"/> belongs to.
  /// <para />
  /// This must be set before the getter is used, else it will return <see langword="null"/>.
  /// Inherit from <see cref="AbilityProjectile{T}"/> generic instead to allow lazy getter and auto cast.
  /// </summary>
  public virtual AbilityState? Ability { get; set; }
}
