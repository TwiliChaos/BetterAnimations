using System.IO;

namespace AnimLib.Networking;

/// <summary>
/// Sends and receives <see cref="ModPacket"/>s that sync <see cref="AnimCharacterCollection"/> and all children.
/// </summary>
internal class StatePacketHandler(byte handlerType) : PacketHandler(handlerType) {
  internal override void HandlePacket(BinaryReader reader, int fromWho) {
    AnimCharacterCollection fromCharacters = GetAnimPlayer(fromWho).Characters;
    fromCharacters.NetSyncInternal(new NetReader(reader));

    if (Main.dedServ) {
      // Packet was sent by a client to the server,
      // Server sends same type of packet back
      SendPacket(fromWho: fromWho);
    }
  }

  protected override void OnSendPacket(ModPacket packet, int fromWho) {
    AnimCharacterCollection fromCharacters = GetAnimPlayer(fromWho).Characters;
    fromCharacters.NetSyncInternal(new NetWriter(packet));
    fromCharacters.NetUpdate = false;
  }
}

internal class FullSyncPacketHandler(byte handlerType) : PacketHandler(handlerType) {
  internal override void HandlePacket(BinaryReader reader, int fromWho) {
    AnimPlayer fromPlayer = GetAnimPlayer(fromWho);
    fromPlayer.Characters.NetSyncAll(new NetReader(reader));

    if (Main.dedServ) {
      // Packet was sent by a client to the server,
      // Server sends same type of packet back
      SendPacket(fromWho: fromWho);
    }
  }

  protected override void OnSendPacket(ModPacket packet, int fromWho) {
    AnimCharacterCollection fromCharacters = GetAnimPlayer(fromWho).Characters;
    fromCharacters.NetSyncAll(new NetWriter(packet));
    fromCharacters.NetUpdate = false;
  }
}
