using AnimLib.Abilities;
using JetBrains.Annotations;

namespace AnimLib.Projectiles;

/// <summary>
/// Base class for ability projectiles.
/// </summary>
[PublicAPI]
public abstract class AbilityProjectile : ModProjectile {
  protected AbilityProjectile() {
   _aPlayer = new Lazy<AnimPlayer>(() => Main.player[Projectile.owner].GetModPlayer<AnimPlayer>());
   _ability = new Lazy<Ability>(() => _aPlayer.Value.Characters[Mod].AbilityManager![Id]);
  }

  private Lazy<AnimPlayer> _aPlayer;

  private Lazy<Ability> _ability;

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
  public AnimPlayer APlayer => _aPlayer.Value;

  /// <summary>
  /// The <see cref="Abilities.Ability"/> that this <see cref="AbilityProjectile"/> belongs to.
  /// </summary>
  public Ability Ability {
    get => _ability.Value;
    internal set => _ability = new Lazy<Ability>(value);
  }
}
