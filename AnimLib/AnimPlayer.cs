using AnimLib.Abilities;
using AnimLib.Animations;
using AnimLib.Internal;
using AnimLib.Networking;
using JetBrains.Annotations;

namespace AnimLib;

/// <summary>
/// Main <see cref="ModPlayer"/> class for <see cref="AnimLibMod"/>, contains and updates <see cref="AnimationController"/>.
/// </summary>
[UsedImplicitly]
public sealed class AnimPlayer : ModPlayer {
  private static AnimPlayer? _local;

  private bool _abilityNetUpdate;

  internal AnimCharacterCollection Characters =>
    _characters ??= AnimLoader.SetupCharacterCollection(this);
  private AnimCharacterCollection? _characters;

  internal static AnimPlayer? Local {
    get {
      if (_local is null) {
        if (Main.gameMenu) return null;
        _local = Main.LocalPlayer?.GetModPlayer<AnimPlayer>();
      }

      return _local;
    }
    set => _local = value;
  }

  /// <summary>
  /// The current active <see cref="AnimCharacter"/>.
  /// </summary>
  private AnimCharacter? ActiveCharacter => Characters.ActiveCharacter;

  /// <summary>
  /// Whether any <see cref="AnimCharacter"/>s need to be net-synced.<br />
  /// When this property is set to <b><see langword="false"/></b>, all
  /// <see cref="AbilityManager.NetUpdate">AbilityManager.netUpdate</see> on this player
  /// will also be set to <b><see langword="false"/></b>.<br />
  /// When any <see cref="AbilityManager.NetUpdate">AbilityManager.netUpdate</see> on this player
  /// is set to <b><see langword="true"/></b>, this property will also be set to <b><see langword="true"/></b>.
  /// </summary>
  internal bool AbilityNetUpdate {
    get => _abilityNetUpdate;
    set {
      _abilityNetUpdate = value;
      if (value) return;
      // Propagate false netUpdate downstream
      foreach (AnimCharacter character in Characters.Values) {
        if (character.AbilityManager != null)
          character.AbilityManager.NetUpdate = false;
      }
    }
  }

  internal bool DebugEnabled { get; set; }

  /// <inheritdoc/>
  public override void SendClientChanges(ModPlayer clientPlayer) {
    if (AbilityNetUpdate) {
      SendAbilityChanges();
      AbilityNetUpdate = false;
    }
  }

  // ReSharper disable once RedundantOverriddenMember
  public override void CopyClientState(ModPlayer targetCopy) => base.CopyClientState(targetCopy);

  private void SendAbilityChanges() => ModNetHandler.Instance.AbilityPacketHandler.SendPacket(255, Player.whoAmI);

  /// <summary>
  /// Updates the <see cref="AnimCharacterCollection.ActiveCharacter"/>.
  /// </summary>
  public override void PostUpdateRunSpeeds() => ActiveCharacter?.Update();

  /// <summary>
  /// Updates all <see cref="AnimationController"/>s on this <see cref="Player"/>.
  /// </summary>
  public override void PostUpdate() => ActiveCharacter?.PostUpdate();
}
