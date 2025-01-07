using AnimLib.Networking;
using AnimLib.States;
using Terraria.ModLoader.IO;

namespace AnimLib;

public abstract partial class AnimCharacter {
  private const string ActiveKey = "active";
  private const string StyleKey = "style";

  /// <summary>
  /// Ensures that all <see cref="AbilityStates"/> levels are synced.
  /// </summary>
  internal override void NetSyncInternal(NetSyncer sync) {
    foreach (AbilityState abilityState in AbilityStates) {
      abilityState.SyncLevel(sync);
    }

    Style.NetSync(sync);

    base.NetSyncInternal(sync);
  }

  public override void SaveData(TagCompound tag) {
    tag[ActiveKey] = Active;
    if (Active) {
      Style.AssignFromPlayer(Player);
    }

    if (Style.Save(_defaultStyle, out TagCompound? styleTag)) {
      tag[StyleKey] = styleTag;
    }
  }

  public override void LoadData(TagCompound tag) {
    if (tag.TryGet(ActiveKey, out bool active) && active && Characters.ActiveCharacter is null) {
      Enable();
    }

    if (tag.TryGet(StyleKey, out TagCompound styleTag)) {
      Style.Load(styleTag, _defaultStyle);
    }

    if (Active) {
      Style.AssignToPlayer(Player);
    }
  }
}
