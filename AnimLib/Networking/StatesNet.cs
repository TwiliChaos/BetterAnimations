using System.IO;
using System.Linq;
using AnimLib.States;
using JetBrains.Annotations;

namespace AnimLib.Networking;

[UsedImplicitly]
internal sealed class StatesNet : ModSystem {
  public override void Unload() {
    NetStates = null;
  }

  internal static string[]? NetStates { get; private set; }

  internal static int Count => NetStates?.Length ?? 0;

  /// <summary>
  /// Creates and populates the <see cref="NetStates"/> array, on the server.
  /// </summary>
  /// <param name="characters"></param>
  internal static void CreateNetIDs(AnimCharacterCollection characters) {
    if (NetStates is not null || !Main.dedServ) {
      return;
    }

    NetStates = new string[characters.AllChildrenCount];

    int i = 0;
    foreach (State state in characters.AllChildren) {
      NetStates[i] = state.GetType().Name;
      state.NetId = (short)i;
      i++;
    }
  }

  /// <summary>
  /// Creates and populates the <see cref="NetStates"/> array, on the client with values provided by the server.
  /// </summary>
  /// <param name="reader">Packet to read from.</param>
  internal static void ReadNetIDs(BinaryReader reader) {
    int count = reader.ReadInt32();
    NetStates = new string[count];
    for (int i = 0; i < count; i++) {
      string name = reader.ReadString();
      NetStates[i] = name;
    }
  }

  /// <summary>
  /// Assigns <paramref name="characters"/>'s children <see cref="State.NetId"/> to <see cref="NetStates"/> indices.
  /// <para />
  /// Requires either <see cref="CreateNetIDs"/> (server) or <see cref="ReadNetIDs"/> (client) be called first.
  /// </summary>
  /// <param name="characters"></param>
  internal static void AssignNetIDs(AnimCharacterCollection characters) {
    if (NetStates is null) {
      return;
    }

    for (int i = 0; i < NetStates.Length; i++) {
      string name = NetStates[i];
      State? state = characters.AllChildren.SingleOrDefault(c => c.Name == name);
      if (state is not null) {
        state.NetId = (short)i;
      }
    }
  }

  internal static short GetIdFromState(State state) {
    string[] array = NetStates ??
      throw new InvalidOperationException($"{nameof(NetStates)} has not been initialized.");

    string name = state.GetType().Name;
    return (short)Array.IndexOf(array, name);
  }
}
