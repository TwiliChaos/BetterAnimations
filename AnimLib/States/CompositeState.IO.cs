using System.IO;
using System.Linq;
using AnimLib.Networking;

namespace AnimLib.States;

public abstract partial class CompositeState {
  /// <summary>
  /// Calls State:
  /// <para><inheritdoc cref="State.NetSyncInternal"/></para>
  /// End State.
  /// <para />
  /// If any children have <see cref="State.NetUpdate"/> or <see cref="State.IndirectNetUpdate"/>,
  /// they will have their <see cref="State.NetSyncInternal"/> called.
  /// </summary>
  /// <param name="sync"></param>
  internal override void NetSyncInternal(ISync sync) {
    base.NetSyncInternal(sync);

    if (!HasChildren) {
      return;
    }

    var netChildren = sync.SyncEnumerate(this, GetNetChildrenCount, GetNetUpdateChildren, WriteChildId, ReadChildId);

    foreach (State? netUpdateChild in netChildren) {
      netUpdateChild.NetSyncInternal(sync);
    }

    return;

    static int GetNetChildrenCount(CompositeState me) => GetNetUpdateChildren(me).Count();
    static void WriteChildId(BinaryWriter writer, State child) => writer.Write(child.NetId);
    static State ReadChildId(CompositeState me, BinaryReader reader) => me.GetChild(reader.ReadInt16());

    static IEnumerable<State> GetNetUpdateChildren(CompositeState me) =>
      me._children.Where(state => state.NetUpdate || state.IndirectNetUpdate);
  }

  private protected IEnumerable<State> GetAllNetChildren(bool includeIndirect) =>
    includeIndirect
      ? AllChildren.Where(state => state.NetUpdate || state.IndirectNetUpdate)
      : AllChildren.Where(state => state.NetUpdate);
}
