using System.Linq;
using Terraria.ID;

namespace AnimLib.Compat.Implementations;

/// <summary>
/// Hides character sprite when
/// mount which hides vanilla player
/// sprite is active
/// </summary>
public class PlayerHidingMount : AnimCompatSystem {
  public readonly List<int> MountIds = [MountID.Wolf];

  public override void PostSetupContent() {
    (string modName, string[] mounts)[] modMounts = [
      ("MountAndJourney", [
        "MAJ_SquirrelTransformation",
        "MAJ_ArcticFoxTransformation"
      ])
    ];

    foreach ((string modName, string[] mounts) in modMounts) {
      if (ModLoader.Mods.All(x => x.Name != modName))
        continue;
      foreach (string mount in mounts) {
        if (ModContent.TryFind(modName, mount, out ModMount m)) {
          MountIds.Add(m.Type);
        }
        else {
          Log.Warn($"Desired Player Hiding Mount " +
            $"({mount}) was not found, " +
            $"though mod {modName} is present, " +
            $"please notify developers of AnimLib");
        }
      }
    }

    GlobalCompatConditions.AddGraphicsDisableCondition(
      GetStandardPredicate(p => {
          if (p.mount is not null && p.mount.Active) {
            return MountIds.Contains(p.mount.Type);
          }

          return false;
        }
      ));
    Initialized = true;
  }
}
