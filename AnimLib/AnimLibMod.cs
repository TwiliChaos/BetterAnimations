using System.IO;
using AnimLib.Compat;
using AnimLib.Internal;
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
  /// Creates a new instance of <see cref="AnimLibMod"/>.
  /// </summary>
  public AnimLibMod() {
    Instance = this;
  }

  /// <summary>
  /// The active instance of <see cref="AnimLibMod"/>.
  /// </summary>
#pragma warning disable CS8618 // This should only ever be null when the assembly is unloaded, i.e, none of this mod's code can run.
  public static AnimLibMod? Instance { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.


  /// <summary>
  /// GitHub profile that the mod's repository is stored on.
  /// </summary>
  public static string GithubUserName => "Ilemni";

  /// <summary>
  /// Name of the GitHub repository this mod is stored on.
  /// </summary>
  public static string GithubProjectName => "AnimLib";

  /// <inheritdoc/>
  public override void Unload() {
    GlobalCompatConditions.Unload();
    ModNetHandler.Unload();
    AnimLoader.Unload();

    UnloadAse();

    AnimPlayer.Local = null;
    Instance = null;
  }

  public override void HandlePacket(BinaryReader reader, int whoAmI) {
    if (Main.netMode == NetmodeID.MultiplayerClient) {
      // If packet is sent TO server, it is FROM player.
      // If packet is sent TO player, it is FROM server (This block) and fromWho is 255.
      // Server-written packet includes the fromWho, the player that created it.
      // Now in either case of this being server or player, the fromWho is the player.
      whoAmI = reader.ReadUInt16();
    }

    ModNetHandler.Instance.HandlePacket(reader, whoAmI);
  }
}
