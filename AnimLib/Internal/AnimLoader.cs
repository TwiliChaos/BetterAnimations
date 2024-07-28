using AnimLib.Abilities;
using AnimLib.Animations;

namespace AnimLib.Internal;

internal static class AnimLoader {
  private static readonly Dictionary<Mod, AnimTemplate> ModTemplates = [];

  /// <summary>
  /// Called from <see cref="ModType{T,T}"/> <see cref="AnimationController.Register"/>
  /// </summary>
  internal static void Add(AnimationController controller) {
    AnimTemplate template = GetOrCreateTemplate(controller);
    if (template.Controller is not null) {
      throw new InvalidOperationException(
        $"Cannot have more than one {nameof(AnimationController)} on mod {controller.Mod}");
    }

    template.Controller = controller;
  }

  /// <summary>
  /// Called from <see cref="ModType{T,T}"/> <see cref="AbilityManager.Register"/>
  /// </summary>
  internal static void Add(AbilityManager manager) {
    AnimTemplate template = GetOrCreateTemplate(manager);
    if (template.Manager is not null) {
      throw new InvalidOperationException(
        $"Cannot have more than one {nameof(AnimationController)} on mod {manager.Mod}");
    }

    template.Manager = manager;
  }

  public static void Add(Ability ability) {
    AnimTemplate template = GetOrCreateTemplate(ability);
    template.Abilities.Add(ability);
  }

  private static AnimTemplate GetOrCreateTemplate<T>(T item) where T : ModType {
    if (ModTemplates.TryGetValue(item.Mod, out AnimTemplate? template)) {
      return template;
    }

    template = new AnimTemplate(item.Mod);
    ModTemplates.Add(item.Mod, template);
    return template;
  }

  internal static void Unload() {
    ModTemplates.Clear();
  }

  // Called from AnimPlayer.Initialize()
  internal static AnimCharacterCollection SetupCharacterCollection(AnimPlayer animPlayer) {
    AnimCharacterCollection characterCollection = new();
    Player player = animPlayer.Player!;

    foreach (AnimTemplate template in ModTemplates.Values) {
      AnimationController? controller = NewAnimationController(player, template.Controller);
      AbilityManager? manager = NewAbilityManager(player, template.Manager, template.Abilities);
      AnimCharacter newCharacter = new(template.Mod, characterCollection, manager, controller);
      characterCollection.ModCharacterMap[template.Mod] = newCharacter;
    }

    return characterCollection;
  }

  private static AnimationController? NewAnimationController(Player player, AnimationController? controller) {
    if (controller is null) {
      return null;
    }

    try {
      AnimationController newController = controller.NewInstance(player);
      newController.Initialize();
      return newController;
    }
    catch (Exception ex) {
      Log.Error($"Exception thrown when constructing [{controller.FullName}]", ex);
      throw;
    }
  }

  private static AbilityManager? NewAbilityManager(Player player, AbilityManager? manager, List<Ability> abilities) {
    if (manager is null) {
      if (abilities.Count > 0) {
        string message = $"[{abilities[0].Mod}]: Missing AbilityManager but has at least one Ability loaded.";
        throw new InvalidOperationException(message);
      }

      return null;
    }

    try {
      AbilityManager newManager = manager.NewInstance(player);
      if (abilities.Count > 0) {
        var list = new List<Ability>(abilities.Count);
        foreach (Ability a in abilities) {
          Ability newAbility = a.NewInstance(player);
          newAbility.Abilities = newManager;
          list.Add(newAbility);
        }

        list.Sort((a1, a2) => a1.Id.CompareTo(a2.Id));
        newManager.AbilityArray = list.ToArray();
      }

      newManager.Initialize();
      foreach (Ability ability in newManager.AbilityArray) {
        ability.Initialize();
      }

      return newManager;
    }
    catch (Exception ex) {
      Log.Error($"Exception thrown when constructing [{manager.FullName}]", ex);
      throw;
    }
  }

  /// <summary>
  /// A given mod may have at least one of these.
  /// </summary>
  private class AnimTemplate(Mod mod) {
    internal AnimationController? Controller;
    internal AbilityManager? Manager;
    internal readonly List<Ability> Abilities = [];
    internal readonly Mod Mod = mod;
  }
}
