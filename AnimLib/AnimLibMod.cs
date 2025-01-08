using System.IO;
using AnimLib.Networking;
using JetBrains.Annotations;
using Terraria.ID;

namespace AnimLib;

/// <summary>
/// Interface for any mods using this mod to interact with.
/// </summary>
[PublicAPI]
public sealed partial class AnimLibMod : Mod {
  /// <summary>
  /// The active instance of <see cref="AnimLibMod"/>.
  /// </summary>
  public static AnimLibMod Instance => ContentInstance<AnimLibMod>.Instance;

  /// <summary>
  /// GitHub profile that the mod's repository is stored on.
  /// </summary>
  public static string GithubUserName => "Ilemni";

  /// <summary>
  /// Name of the GitHub repository this mod is stored on.
  /// </summary>
  public static string GithubProjectName => "AnimLib";

  public static bool DebugEnabled { get; set; }

  public override void HandlePacket(BinaryReader reader, int whoAmI) {
    if (Main.netMode == NetmodeID.MultiplayerClient) {
      // If packet is sent TO server, it is FROM player.
      // If packet is sent TO player, it is FROM server (This block) and fromWho is 255.
      // Server-written packet includes the fromWho, the player that created it.
      // Now in either case of this being server or player, the fromWho is the player.
      whoAmI = reader.ReadUInt16();
    }

    ModContent.GetInstance<ModNetHandler>().HandlePacket(reader, whoAmI);
  }

  public override void Unload() {
    UnloadAse();
  }

  internal static void ToggleDebugMode() {
    DebugEnabled ^= true;
  }
}
