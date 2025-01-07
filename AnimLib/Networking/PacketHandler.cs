using System.IO;
using AnimLib.States;

namespace AnimLib.Networking;

/// <summary>
/// Base class for sending and handling received <see cref="ModPacket"/>s.
/// </summary>
/// <param name="handlerType">
/// Identifies which <see cref="PacketHandler"/> created the <see cref="ModPacket"/>.
/// </param>
internal abstract class PacketHandler(byte handlerType) {
  private readonly ReadSyncer _readSyncer = new();
  private readonly WriteSyncer _writeSyncer = new();

  internal void HandlePacket(BinaryReader reader, int fromWho) {
    _readSyncer.SetReader(reader);
    HandlePacket(_readSyncer, fromWho);
    _readSyncer.ClearReader();
  }

  /// <summary>
  /// Handle the received <see cref="ModPacket"/> using <paramref name="reader"/>. Packet is from <paramref name="fromWho"/>.
  /// </summary>
  /// <param name="reader"><see cref="BinaryReader"/> for the <see cref="ModPacket"/>.</param>
  /// <param name="fromWho">Client this was from.</param>
  protected abstract void HandlePacket(ReadSyncer reader, int fromWho);

  internal void SendPacket(int toWho = -1, int fromWho = -1) {
    ModPacket packet = AnimLibMod.Instance.GetPacket();
    if (Main.dedServ) {
      packet.Write((ushort)fromWho);
    }

    packet.Write(handlerType);
    _writeSyncer.SetWriter(packet);
    OnSendPacket(_writeSyncer, fromWho);
    _writeSyncer.ClearWriter();
    packet.Send(toWho, fromWho);
  }

  protected abstract void OnSendPacket(WriteSyncer writer, int fromWho);

  protected static State[] GetStates(int fromWho) => Main.player[fromWho].GetStates();
}
