using AnimLib.Abilities;
using AnimLib.Animations;
using JetBrains.Annotations;

namespace AnimLib.Extensions;

/// <summary>
/// Contains extension methods for the <see cref="ModPlayer"/> class.
/// </summary>
[PublicAPI]
public static class ModPlayerExtensions {
  /// <summary>
  /// Gets the <see cref="AnimCharacter"/> instance that belongs to the <see cref="Mod"/> of <paramref name="modPlayer"/>.
  /// </summary>
  /// <param name="modPlayer">Your <see cref="ModPlayer"/> instance.</param>
  /// <returns>The <see cref="AnimCharacter"/> instance of <paramref name="modPlayer"/> for your <see cref="Mod"/></returns>
  public static AnimCharacter GetAnimCharacter(this ModPlayer modPlayer) {
    ArgumentNullException.ThrowIfNull(modPlayer);

    AnimPlayer animPlayer = modPlayer.Player.GetModPlayer<AnimPlayer>();
    Mod mod = modPlayer.Mod;

    if (!animPlayer.Characters.TryGetValue(mod, out AnimCharacter? c)) {
      throw new ArgumentException($"Mod {mod.Name} has no AnimLib types.");
    }

    return c;
  }

  /// <summary>
  /// Gets a wrapped <see cref="AnimCharacterWrapper{T, T}"/> instance that belongs to the <see cref="Mod"/> of <paramref name="modPlayer"/>.
  /// </summary>
  /// <param name="modPlayer">Your <see cref="ModPlayer"/> instance.</param>
  /// <returns>An <see cref="AnimCharacterWrapper{T, T}"/> instance of <paramref name="modPlayer"/> for your <see cref="Mod"/></returns>
  public static AnimCharacterWrapper<TAnimation, TAbility> GetAnimCharacter<TAnimation, TAbility>(this ModPlayer modPlayer)
    where TAnimation : AnimationController where TAbility : AbilityManager {
    ArgumentNullException.ThrowIfNull(modPlayer);
    
    AnimCharacter character = GetAnimCharacter(modPlayer);

    return character.GetWrapped<TAnimation, TAbility>();
  }
}
