using AnimLib.Networking;
using Terraria.ModLoader.IO;

namespace AnimLib.States;

public abstract partial class AbilityState {
  /// <summary>
  /// Save data that is specific to this <see cref="AbilityState"/>.
  /// By default, saves the ability's level.
  /// </summary>
  /// <returns>A <see cref="TagCompound"/> with data specific to this <see cref="AbilityState"/>.</returns>
  /// <seealso cref="Load"/>
  public virtual TagCompound Save() => new() {
    [nameof(Level)] = Level
  };

  /// <summary>
  /// Load data that is specific to this <see cref="AbilityState"/>.
  /// By default, loads the ability's level.
  /// </summary>
  /// <param name="tag">The tag to load ability data from.</param>
  /// <seealso cref="Save"/>
  public virtual void Load(TagCompound tag) =>
    Level = tag.GetInt(nameof(Level));

  /// <summary>
  /// Syncs the values of
  /// <see cref="Level"/>, <see cref="CooldownLeft"/>, and <see cref="IsOnCooldown"/>.
  /// <para />
  /// Calls State:
  /// <para><inheritdoc cref="State.NetSyncInternal"/></para>
  /// </summary>
  /// <param name="sync"></param>
  internal override void NetSyncInternal(ISync sync) {
    sync.Sync7BitEncodedInt(ref _level);
    sync.Sync7BitEncodedInt(ref _cooldownLeft);
    sync.Sync(ref _isOnCooldown);
    base.NetSyncInternal(sync);
  }

  internal void SyncLevel(ISync sync) {
    sync.Sync7BitEncodedInt(ref _level);
  }
}
