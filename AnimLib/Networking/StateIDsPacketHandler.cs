using System.IO;

namespace AnimLib.Networking;

/// <summary>
/// Sends and receives from server a packet which assigns netIds to all player states
/// </summary>
internal class StateIDsPacketHandler(byte handlerType) : PacketHandler(handlerType) {
  /// <summary>
  /// Received on the client a packet which was sent by the server.
  /// Packet contains IDs for <see cref="StatesNet"/>.
  /// </summary>
  /// <param name="reader"></param>
  /// <param name="fromWho"></param>
  internal override void HandlePacket(BinaryReader reader, int fromWho) {
    StatesNet.ReadNetIDs(reader);

    foreach (Player player in Main.player) { // Assign to all Player objects, don't use Main.ActivePlayers
      if (player.TryGetModPlayer<AnimPlayer>(out AnimPlayer? animPlayer)) {
        StatesNet.AssignNetIDs(animPlayer.Characters);
      }
    }

    // We just connected to the world, let all other players know our State data
    ModContent.GetInstance<ModNetHandler>().FullSyncHandler.SendPacket(255, Main.myPlayer);
  }

  /// <summary>
  /// Sends on the server a packet which will be sent to any connected clients.
  /// Packet contains IDs for <see cref="StatesNet"/>.
  /// </summary>
  /// <param name="packet"></param>
  /// <param name="fromWho"></param>
  /// <exception cref="Exception">This method was not called on the server.</exception>
  protected override void OnSendPacket(ModPacket packet, int fromWho) {
    if (!Main.dedServ) {
      throw new Exception($"SyncStatesPacket may only be sent by a server.");
    }

    string[] stateNames = StatesNet.NetStates!;

    packet.Write(stateNames.Length);
    foreach (string name in stateNames) {
      packet.Write(name);
    }
  }
}
