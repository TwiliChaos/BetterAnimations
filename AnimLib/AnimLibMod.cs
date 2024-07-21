using System.IO;
using AnimLib.Abilities;
using AnimLib.Animations;
using AnimLib.Compat;
using AnimLib.Extensions;
using AnimLib.Internal;
using AnimLib.Networking;
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
    Instance ??= this;
  }

  /// <summary>
  /// The active instance of <see cref="AnimLibMod"/>.
  /// </summary>
  public static AnimLibMod Instance { get; private set; }


  /// <summary>
  /// GitHub profile that the mod's repository is stored on.
  /// </summary>
  public static string GithubUserName => "Ilemni";

  /// <summary>
  /// Name of the GitHub repository this mod is stored on.
  /// </summary>
  public static string GithubProjectName => "AnimLib";

  /// <summary>
  /// Gets the <see cref="AnimationController"/> of the given type from the given <see cref="ModPlayer"/>.
  /// Use this if you want your code to use values such as the current track and frame.
  /// <para>This <strong>cannot</strong> be used during the <see cref="ModPlayer.Initialize"/> method.</para>
  /// </summary>
  /// <typeparam name="T">Type of <see cref="AnimationController"/> to get.</typeparam>
  /// <param name="modPlayer">The <see cref="ModPlayer"/>.</param>
  /// <returns>An <see cref="AnimationController"/> of type <typeparamref name="T"/>.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="modPlayer"/> cannot be null.</exception>
  /// <exception cref="ArgumentException">
  /// The <see cref="Mod"/> in <paramref name="modPlayer"/> does not have an <see cref="AnimationController"/> of type <typeparamref name="T"/>.
  /// </exception>
  [NotNull]
  public static T GetAnimationController<T>([NotNull] ModPlayer modPlayer) where T : AnimationController {
    ArgumentNullException.ThrowIfNull(modPlayer);
    AnimationController controller = modPlayer.GetAnimCharacter().AnimationController;
    return controller as T ?? throw ThrowHelper.BadType<T>(controller, modPlayer.Mod, nameof(T));
  }


  /// <summary>
  /// Gets the <see cref="AbilityManager"/> of the given type from the given <see cref="ModPlayer"/>.
  /// Use this if you want your code to access ability information.
  /// <para>This <strong>cannot</strong> be used during the <see cref="ModPlayer.Initialize"/> method.</para>
  /// </summary>
  /// <typeparam name="T">Type of <see cref="AbilityManager"/> to get.</typeparam>
  /// <param name="modPlayer">The <see cref="ModPlayer"/>.</param>
  /// <returns>An <see cref="AbilityManager"/> of type <typeparamref name="T"/>.</returns>
  /// <exception cref="ArgumentNullException"><paramref name="modPlayer"/> cannot be null.</exception>
  /// <exception cref="ArgumentException">
  /// The <see cref="Mod"/> in <paramref name="modPlayer"/> does not have an <see cref="AbilityManager"/> of type <typeparamref name="T"/>.
  /// </exception>
  [NotNull]
  public static T GetAbilityManager<T>([NotNull] ModPlayer modPlayer) where T : AbilityManager {
    ArgumentNullException.ThrowIfNull(modPlayer);
    AbilityManager manager = modPlayer.GetAnimCharacter().AbilityManager;
    return manager as T ?? throw ThrowHelper.BadType<T>(manager, modPlayer.Mod, nameof(T));
  }

  /// <summary>
  /// Use this to null static reference types on unload.
  /// </summary>
  internal static event Action OnUnload;

  /// <summary>
  /// Collects and constructs all <see cref="AnimSpriteSheet"/>s across all other <see cref="Mod"/>s.
  /// </summary>
  public override void PostSetupContent() {
    OnUnload += GlobalCompatConditions.Unload;
  }

  /// <inheritdoc/>
  public override void Unload() {
    OnUnload?.Invoke();
    OnUnload = null;
    Instance = null;
    AnimLoader.Unload();
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
