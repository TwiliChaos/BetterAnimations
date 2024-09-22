using System.IO;

namespace AnimLib.Networking;

/// <summary>
/// Base class for sending and handling received <see cref="ModPacket"/>s.
/// </summary>
/// <param name="handlerType">
/// Identifies which <see cref="PacketHandler"/> created the <see cref="ModPacket"/>.
/// </param>
internal abstract class PacketHandler(byte handlerType) {
  /// <summary>
  /// Handle the received <see cref="ModPacket"/> using <paramref name="reader"/>. Packet is from <paramref name="fromWho"/>.
  /// </summary>
  /// <param name="reader"><see cref="BinaryReader"/> for the <see cref="ModPacket"/>.</param>
  /// <param name="fromWho">Client this was from.</param>
  internal abstract void HandlePacket(BinaryReader reader, int fromWho);

  internal void SendPacket(int toWho = -1, int fromWho = -1) {
    ModPacket packet = GetPacket(fromWho);
    OnSendPacket(packet, fromWho);
    packet.Send(toWho, fromWho);
  }

  protected abstract void OnSendPacket(ModPacket packet, int fromWho);

  /// <summary>
  /// Gets a <see cref="ModPacket"/> with <see cref="handlerType"/> and <paramref name="fromWho"/> written to it.
  /// </summary>
  /// <param name="fromWho">The whoAmI of the player whose data will be in this packet.</param>
  private ModPacket GetPacket(int fromWho) {
    ModPacket packet = AnimLibMod.Instance.GetPacket();
    if (Main.dedServ) {
      packet.Write((ushort)fromWho);
    }

    packet.Write(handlerType);
    return packet;
  }

  protected static AnimPlayer GetAnimPlayer(int fromWho) => Main.player[fromWho].GetModPlayer<AnimPlayer>();
}
