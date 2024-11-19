using AnimLib.Networking;
using JetBrains.Annotations;
using Terraria.ID;
using Terraria.Localization;

namespace AnimLib;

[UsedImplicitly]
internal sealed class SyncOnConnect : ModSystem {
  public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text,
    int number, float number2, float number3, float number4, int number5, int number6, int number7) {
    SendSyncStates(msgType, remoteClient);
    return false;
  }

  private static void SendSyncStates(int msgType, int remoteClient) {
    if (msgType != MessageID.FinishedConnectingToServer || Main.netMode != NetmodeID.Server) {
      return;
    }

    ModNetHandler netHandler = ModContent.GetInstance<ModNetHandler>();

    netHandler.StateIDsHandler.SendPacket(remoteClient, Main.myPlayer);

    foreach (Player pl in Main.ActivePlayers) {
      int fromPlayer = pl.whoAmI;
      if (remoteClient != fromPlayer) {
        // A player just connected to the world, let them know the State data of other players.
        netHandler.FullSyncHandler.SendPacket(remoteClient, fromPlayer);
      }
    }
  }
}
