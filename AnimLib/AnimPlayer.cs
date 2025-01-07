using System.Linq;
using AnimLib.Networking;
using AnimLib.States;
using AnimLib.Systems;
using JetBrains.Annotations;
using Terraria.ModLoader.IO;

namespace AnimLib;

/// <summary>
/// Main <see cref="ModPlayer"/> class for <see cref="AnimLibMod"/>, contains and updates <see cref="AnimLib.States.State"/>.
/// </summary>
[UsedImplicitly]
public sealed class AnimPlayer : ModPlayer {
  private const string StateDataKey = "stateData";

  internal State[] States = null!; // NewInstance() -> StateLoader.NewInstance

  public T GetState<T>() where T : State => (T)GetState(ModContent.GetInstance<T>().Index);

  public T GetState<T>(int index) where T : State {
    State result = GetState(index);
    if (result is T t) {
      return t;
    }

    throw new ArgumentException("Specified index does not refer to a State of type " + typeof(T).Name);
  }

  public State GetState(State templateState) => GetState(templateState.Index);

  public State GetState(int index) {
    ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
    ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, States.Length, nameof(index));
    return States[index];
  }

  public override void OnEnterWorld() {
    ModContent.GetInstance<DebugUISystem>().SetCharacters(GetState<AnimCharacterCollection>());
  }

  public override ModPlayer NewInstance(Player entity) {
    AnimPlayer? newInstance = (AnimPlayer)base.NewInstance(entity);
    StateLoader.NewInstance(newInstance); // Creates and populates States array
    return newInstance;
  }

  /// <inheritdoc/>
  public override void SendClientChanges(ModPlayer clientPlayer) {
    if (States.Any(s => s.NetUpdate)) {
      ModContent.GetInstance<ModNetHandler>().StatePacketHandler.SendPacket(255, Player.whoAmI);
    }
  }

  // ReSharper disable once RedundantOverriddenMember
  public override void CopyClientState(ModPlayer targetCopy) => base.CopyClientState(targetCopy);

  public override void SaveData(TagCompound tag) {
    tag[StateDataKey] = StateIO.SaveStateData(Player);
  }

  public override void LoadData(TagCompound tag) {
    if (tag.TryGet(StateDataKey, out IList<TagCompound> stateData)) {
      StateIO.LoadStateData(Player, stateData);
    }
  }
}
