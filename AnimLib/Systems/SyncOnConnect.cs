using AnimLib.Networking;
using JetBrains.Annotations;
using Terraria.ID;
using Terraria.Localization;

namespace AnimLib.Systems;

[UsedImplicitly]
internal sealed class SyncOnConnect : ModSystem {
  public override bool HijackSendData(int whoAmI, int msgType, int remoteClient, int ignoreClient, NetworkText text,
    int number, float number2, float number3, float number4, int number5, int number6, int number7) {
    if (msgType == MessageID.FinishedConnectingToServer && Main.netMode == NetmodeID.Server) {
      SendSyncStates(remoteClient);
    }

    return false;
  }

  private static void SendSyncStates(int remoteClient) {
    ModNetHandler netHandler = ModContent.GetInstance<ModNetHandler>();
    FullSyncPacketHandler fullSyncHandler = netHandler.FullSyncHandler;

    foreach (Player pl in Main.ActivePlayers) {
      int fromPlayer = pl.whoAmI;
      if (remoteClient != fromPlayer) {
        // Let the newly-connected client know the full State data of other players.
        fullSyncHandler.SendPacket(remoteClient, fromPlayer);
      }
    }
  }
}
