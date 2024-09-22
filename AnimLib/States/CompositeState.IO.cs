using System.IO;
using System.Linq;
using AnimLib.Networking;

namespace AnimLib.States;

public abstract partial class CompositeState {
  internal override void NetSyncInternal(ISync sync) {
    base.NetSyncInternal(sync);

    if (!HasChildren) {
      return;
    }

    var netUpdateChildren = NetSyncUpdateChildren(sync);
    foreach (State? netUpdateChild in netUpdateChildren) {
      netUpdateChild.NetSyncInternal(sync);
    }
  }

  private protected IEnumerable<State> NetSyncUpdateChildren(ISync sync) {
    return sync.SyncEnumerate(this,
      NetChildrenCount,
      NetUpdateChildren,
      NetWriteChildIdentifier,
      NetReadChildIdentifier);

    static int NetChildrenCount(CompositeState state) => state.GetNetUpdateChildren(true).Count();
    static IEnumerable<State> NetUpdateChildren(CompositeState state) => state.GetNetUpdateChildren(true);
    static void NetWriteChildIdentifier(BinaryWriter writer, State child) => writer.Write(child.NetId);
    static State NetReadChildIdentifier(CompositeState state, BinaryReader reader) => state.GetChild(reader.ReadInt16());
  }

  private protected IEnumerable<State> GetNetUpdateChildren(bool includeIndirect) =>
    includeIndirect
      ? _children.Where(state => state.NetUpdate || state.IndirectNetUpdate)
      : _children.Where(state => state.NetUpdate);

  private protected int GetNetChildrenCount(bool includeIndirect) => GetNetUpdateChildren(includeIndirect).Count();

  private protected int GetAllNetChildrenCount(bool includeIndirect = true) =>
    GetAllNetUpdateChildren(includeIndirect).Count();

  private protected IEnumerable<State> GetAllNetUpdateChildren(bool includeIndirect) =>
    includeIndirect
      ? AllChildren.Where(state => state.NetUpdate || state.IndirectNetUpdate)
      : AllChildren.Where(state => state.NetUpdate);
}
