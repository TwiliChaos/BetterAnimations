using AnimLib.Networking;
using AnimLib.States;
using Terraria.ModLoader.IO;

namespace AnimLib;

public abstract partial class AnimCharacter {
  /// <summary>
  /// Serializes all <see cref="AbilityStates"/> data into a new <see cref="TagCompound"/> and returns it.
  /// You will want to call this in <see cref="ModPlayer.SaveData">ModPlayer.SaveData()</see>
  /// </summary>
  /// <returns>
  /// An instance of <see cref="TagCompound"/> containing <see cref="States.AbilityState"/> save data.
  /// </returns>
  public TagCompound Save() {
    TagCompound tag = [];
    foreach (AbilityState state in AbilityStates) {
      TagCompound stateTag = state.Save();
      if (stateTag.Count > 0) {
        tag[state.Name] = stateTag;
      }
    }

    SaveCustomData(tag);

    return tag;
  }

  /// <summary>
  /// Deserializes all <see cref="AbilityStates"/> data from the given <see cref="TagCompound"/>.
  /// You will want to call this in <see cref="ModPlayer.LoadData">ModPlayer.LoadData()</see>
  /// </summary>
  public void Load(TagCompound tag) {
    foreach (AbilityState state in AbilityStates) {
      if (tag.TryGet<TagCompound>(state.Name, out TagCompound? abilityTag)) {
        state.Load(abilityTag);
      }
    }

    LoadCustomData(tag);
  }

  /// <summary>
  /// Syncs <see cref="IsEnabled"/>.
  /// <para />
  /// Calls ConcurrentState:
  /// <para><inheritdoc cref="ConcurrentState.NetSyncInternal"/></para>
  /// End ConcurrentState.
  /// </summary>
  internal override void NetSyncInternal(ISync sync) {
    sync.Sync(ref _isEnabled);
    foreach (AbilityState abilityState in AbilityStates) {
      abilityState.SyncLevel(sync);
    }

    base.NetSyncInternal(sync);
  }
}
