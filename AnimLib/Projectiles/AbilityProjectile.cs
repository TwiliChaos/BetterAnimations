using AnimLib.Abilities;

namespace AnimLib.Projectiles;

/// <summary>
/// Base class for ability projectiles.
/// </summary>
[PublicAPI]
public abstract class AbilityProjectile : ModProjectile {
  private Ability _ability;

  private AnimPlayer _aPlayer;

  /// <summary>
  /// Correlates to a <see cref="Ability.Id"/>.
  /// </summary>
  public abstract int Id { get; }

  /// <summary>
  /// The level of the <see cref="Abilities.Ability"/> when this <see cref="AbilityProjectile"/> was created.
  /// </summary>
  public int Level {
    get => (int)Projectile.ai[0];
    set => Projectile.ai[0] = value;
  }

  /// <summary>
  /// THe <see cref="AnimPlayer"/> that this <see cref="AbilityProjectile"/> belongs to.
  /// </summary>
  public AnimPlayer APlayer => _aPlayer ??= Main.player[Projectile.owner].GetModPlayer<AnimPlayer>();

  /// <summary>
  /// The <see cref="Abilities.Ability"/> that this <see cref="AbilityProjectile"/> belongs to.
  /// </summary>
  public Ability Ability {
    get => _ability ??= APlayer.Characters[Mod].AbilityManager?[Id];
    internal set => _ability = value;
  }
}
