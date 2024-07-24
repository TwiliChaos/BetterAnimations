using AnimLib.Animations;
using System.Linq;

namespace AnimLib.Compat;

public static class GlobalCompatConditions {
  /// <summary>
  /// The <see cref="Predicate{Player}"/> list to determine if layers' display
  /// should be disabled, contains conditions, if any return true,
  /// associated flag is turned to false, if none - to true
  /// </summary>
  private static readonly List<Predicate<Player>> DisableGraphicsOrPredicates = [];

  /// <summary>
  /// The <see cref="Predicate{Player}"/> list to determine if
  /// animations updates should be disabled
  /// </summary>
  private static readonly List<Predicate<Player>> DisableAnimationsUpdating = [];

  /// <summary>
  /// Evaluates conditions of <see cref="DisableGraphicsOrPredicates"/>
  /// </summary>
  internal static bool EvaluateDisableGraphics(Player player) =>
    DisableGraphicsOrPredicates.Any(p => p(player));

  /// <summary>
  /// Evaluates conditions of <see cref="DisableAnimationsUpdating"/>
  /// </summary>
  internal static bool EvaluateDisableAnimationUpdate(Player player) =>
    DisableAnimationsUpdating.Any(p => p(player));

  /// <summary>
  /// Adds <see cref="Predicate{Player}"/> to list
  /// of "should AnimLib mods' graphics be hidden" conditions,
  /// allows for evaluation of condition for specified player
  /// if any of predicates returns true during condition check phase
  /// <see cref="AnimationController.GraphicsEnabledCompat"/> becomes false until the
  /// next evaluation.
  /// Use this for compatibility, if you want to add trigger for
  /// disabling of PlayerDrawLayers' changes
  /// (hiding vanilla layers and displaying game character)
  /// (as example, morph ball from NetroidMod should hide players' character)
  /// </summary>
  public static void AddGraphicsDisableCondition(Predicate<Player> p) =>
    DisableGraphicsOrPredicates.Add(p);

  /// <summary>
  /// Adds <see cref="Predicate{Player}"/> to list
  /// of "should AnimLib mods' animation updates be disabled" conditions,
  /// allows for evaluation of condition for specified player
  /// if any of predicates returns true during condition check phase
  /// <see cref="AnimationController.AnimationUpdEnabledCompat"/> becomes false until the
  /// next evaluation.
  /// Use this for compatibility, if you want to add trigger for
  /// disabling of animation (if using AnimationController) updating
  /// </summary>
  public static void AddAnimationUpdateDisableCondition(Predicate<Player> p) =>
    DisableAnimationsUpdating.Add(p);

  internal static void Unload() {
    DisableGraphicsOrPredicates.Clear();
    DisableAnimationsUpdating.Clear();
  }
}
