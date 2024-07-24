using AnimLib.Abilities;
using AnimLib.Animations;
using Terraria.ID;

namespace AnimLib.Internal;

internal static class AnimLoader {
  private static readonly Dictionary<Mod, AnimSet> TemplateAnimSets = [];

  /// <summary>
  /// Called from <see cref="ModType{T,T}"/> <see cref="AnimationController.Register"/>
  /// </summary>
  internal static void Add(AnimationController controller) {
    AnimSet set = GetTemplateForModType(controller);
    if (set.Controller is not null) {
      throw new InvalidOperationException(
        $"Cannot have more than one {nameof(AnimationController)} on mod {controller.Mod}");
    }

    set.Controller = controller;
    controller.Index = set.Index;
  }

  /// <summary>
  /// Called from <see cref="ModType{T,T}"/> <see cref="AbilityManager.Register"/>
  /// </summary>
  internal static void Add(AbilityManager manager) {
    AnimSet set = GetTemplateForModType(manager);
    if (set.Manager is not null) {
      throw new InvalidOperationException(
        $"Cannot have more than one {nameof(AnimationController)} on mod {manager.Mod}");
    }

    set.Manager = manager;
    manager.Index = set.Index;
  }

  public static void Add(Ability ability) {
    AnimSet set = GetTemplateForModType(ability);
    set.Abilities.Add(ability);
  }

  private static AnimSet GetTemplateForModType<T>(T item) where T : ModType {
    if (TemplateAnimSets.TryGetValue(item.Mod, out AnimSet? template)) {
      return template;
    }

    template = new AnimSet((ushort)TemplateAnimSets.Count, item.Mod);
    TemplateAnimSets.Add(item.Mod, template);
    return template;
  }

  internal static void Unload() {
    TemplateAnimSets.Clear();
  }

  // Called from AnimPlayer.Initialize()
  internal static AnimCharacterCollection SetupCharacterCollection(AnimPlayer animPlayer) {
    AnimCharacterCollection characterCollection = new();

    foreach (AnimSet set in NewInstances(animPlayer.Player)) {
      AnimCharacter character = new(set.Mod, characterCollection, set.Manager, set.Controller);
      characterCollection.Dict[set.Mod] = character;
    }

    return characterCollection;
  }

  private static IEnumerable<AnimSet> NewInstances(Player player) {
    foreach (AnimSet template in TemplateAnimSets.Values) {
      AnimSet set = new(template.Index, template.Mod) {
        Controller = NewAnimationController(player, template.Controller),
        Manager = NewAbilityManager(player, template.Manager, template.Abilities)
      };
      yield return set;
    }
  }

  private static AnimationController? NewAnimationController(Player player, AnimationController? controller) {
    if (controller is null) {
      return null;
    }
    try {
      return controller.NewInstance(player);
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
}

/// <summary>
/// A given mod may have at least one of these.
/// </summary>
/// <param name="index"></param>
/// <param name="mod"></param>
internal class AnimSet(ushort index, Mod mod) {
  internal AnimationController? Controller;
  internal AbilityManager? Manager;
  internal readonly List<Ability> Abilities = [];
  internal readonly ushort Index = index;
  internal readonly Mod Mod = mod;
}
