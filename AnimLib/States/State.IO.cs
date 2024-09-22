using System.IO;
using AnimLib.Networking;

namespace AnimLib.States;

public abstract partial class State {
  public short NetId { get; internal set; } = -1;

  /// <summary>
  /// Whether this <see cref="State"/> needs to be synced with other clients.
  /// This value is always <see langword="false"/> on non-local <see cref="Player"/> instances,
  /// and setting to the value is ignored.
  /// </summary>
  public bool NetUpdate {
    get => _netUpdate;
    protected internal set {
      // Ignore NetUpdate for non-local player
      if (!IsLocal && !Main.dedServ) {
        return;
      }

      _netUpdate = value;
      if (value) {
        OnNetUpdateNeeded();

        // True value propagates to the root
        if (Parent is not null) {
          Parent.IndirectNetUpdate = true;
        }
      }
      else if (this is CompositeState sm) {
        IndirectNetUpdate = false;

        // False value propagates to all children
        foreach (State? child in sm.Children) {
          child.NetUpdate = false;
        }
      }
    }
  }

  /// <summary>
  /// Whether this <see cref="State"/> should NetUpdate
  /// as a consequence of a child <see cref="NetUpdate"/> set to <see langword="true"/>.
  /// </summary>
  internal bool IndirectNetUpdate {
    get => _indirectNetUpdate;
    private set {
      // Ignore NetUpdate for non-local player
      if (!IsLocal && !Main.dedServ) {
        return;
      }

      _indirectNetUpdate = value;
      if (value && Parent is not null) {
        Parent.IndirectNetUpdate = true;
      }
    }
  }

  private bool _netUpdate;
  private bool _indirectNetUpdate;

  internal virtual void NetSyncInternal(ISync sync) {
    IReadSync? read = sync as IReadSync;
    if (read is not null && !Main.dedServ) {
      Main.NewText($"{Main.time} Read [{Name}] Before Pos: {read.Reader.BaseStream.Position}");
    }

    sync.Sync7BitEncodedInt(ref _activeTime);
    NetSync(sync);

    if (read is not null && !Main.dedServ) {
      Main.NewText($"{Main.time} Read [{Name}] After Pos: {read.Reader.BaseStream.Position}");
    }

    if (Main.dedServ) {
      // Client sent this State to server,
      // We want to ensure we send this state to other clients.
      NetUpdate = true;
    }
  }

  /// <summary>
  /// Sync additional data with other clients.
  /// </summary>
  /// <param name="sync">
  /// An instance which implements either <see cref="IReadSync"/> or <see cref="IWriteSync"/>.
  /// If you need to access the underlying <see cref="BinaryReader"/> or <see cref="ModPacket"/>,
  /// cast as <see cref="IReadSync"/> or <see cref="IWriteSync"/> to access their respective
  /// <see cref="IReadSync.Reader"/> or <see cref="IWriteSync.Writer"/>.
  /// </param>
  protected virtual void NetSync(ISync sync) {
  }

  /// <summary>
  /// Called when <see cref="NetUpdate"/> is set to <see langword="true"/>.
  /// </summary>
  protected virtual void OnNetUpdateNeeded() {
  }
}
