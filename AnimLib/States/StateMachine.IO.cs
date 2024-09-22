using System.IO;
using AnimLib.Networking;

namespace AnimLib.States;

public abstract partial class StateMachine {
  internal override void NetSyncInternal(ISync sync) {
    NetSyncActiveChild(sync);
    base.NetSyncInternal(sync);
  }

  private protected void NetSyncActiveChild(ISync sync) {
    State? child = ActiveChild;
    sync.SyncNullable(this, ref child,
      WriteId,
      ReadId,
      ReadNull);
  }

  private static void WriteId(BinaryWriter writer, State state) {
    switch (StatesNet.Count) {
      case <= 0xFF:
        writer.Write((byte)state.NetId);
        break;
      case <= 0x4000:
        writer.Write7BitEncodedInt(state.NetId);
        break;
      default:
        writer.Write(state.NetId);
        break;
    }
  }

  private static void ReadId(BinaryReader reader, StateMachine me) {
    int id = StatesNet.Count switch {
      <= 0xFF => reader.ReadByte(),
      <= 0x4000 => reader.Read7BitEncodedInt(),
      _ => reader.ReadInt16()
    };
    State child = me.GetChild(id);
    me.TrySetActiveChild(child, checkTransition: false);
  }

  private static void ReadNull(StateMachine me) => me.ClearActiveChild(silent: false);
}
