using AnimLib.Abilities;
using AnimLib.Animations;

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
  [NotNull]
  public static AnimCharacter GetAnimCharacter(this ModPlayer modPlayer) {
    AnimPlayer animPlayer = modPlayer.Player.GetModPlayer<AnimPlayer>();
    return animPlayer.characters.TryGetValue(modPlayer.Mod, out AnimCharacter c) ? c : throw ThrowHelper.NoType(modPlayer.Mod);
  }

  /// <summary>
  /// Gets a wrapped <see cref="AnimCharacterWrapper{T, T}"/> instance that belongs to the <see cref="Mod"/> of <paramref name="modPlayer"/>.
  /// </summary>
  /// <param name="modPlayer">Your <see cref="ModPlayer"/> instance.</param>
  /// <returns>An <see cref="AnimCharacterWrapper{T, T}"/> instance of <paramref name="modPlayer"/> for your <see cref="Mod"/></returns>
  public static AnimCharacterWrapper<TAnimation, TAbility> GetAnimCharacter<TAnimation, TAbility>(this ModPlayer modPlayer)
    where TAnimation : AnimationController where TAbility : AbilityManager {
    AnimCharacter character = GetAnimCharacter(modPlayer);

    return character.GetWrapped<TAnimation, TAbility>();
  }
}
