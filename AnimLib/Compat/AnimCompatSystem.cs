using JetBrains.Annotations;

namespace AnimLib.Compat;

[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public abstract class AnimCompatSystem : ModSystem {
  /// <summary>
  /// Set this flag to true, if system activation
  /// succeed and predicates were registered
  /// </summary>
  protected bool Initialized;

  /// <summary>
  /// Set this flag to true, if system activation
  /// failed and predicates were not registered
  /// </summary>
  protected bool Fault;

  /// <summary>
  /// Used for determining, if this compat system
  /// should be active for that player in current
  /// situation
  /// </summary>
  public virtual bool IsAllowed(Player player) =>
    Initialized && !Fault && !IsBlockListed(player);

  /// <summary>
  /// Checks that this compat system is not
  /// blacklisted in player's character controllers
  /// </summary>
  public bool IsBlockListed(Player player) =>
    player.GetActiveCharacter()?.AnimCompatSystemBlocklist.Contains(Name) ?? false;

  /// <summary>
  /// Returns wrapped predicate
  /// for safe operation
  /// (prevents running when is not allowed and throwing exceptions outside)
  /// </summary>
  protected Func<Player, bool> GetStandardPredicate(Func<Player, bool> predicate) => player => {
    if (!IsAllowed(player)) return false;
    try {
      return predicate(player);
    }
    catch (Exception ex) {
      Fault = true;
      Log.Error($"Something went wrong in {Name} compat module, it was disabled...", ex);
      return false;
    }
  };

  public override void Unload() {
    Initialized = false;
    Fault = false;
  }
}
