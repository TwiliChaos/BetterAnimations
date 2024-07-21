using AnimLib.Abilities;
using AnimLib.Animations;

namespace AnimLib.Internal;

internal static class AnimLoader {
  internal static readonly Dictionary<Mod, AnimSet> AnimationClasses = [];

  /// <summary>
  /// Called from <see cref="ModType{T,T}"/> <see cref="AnimationController.Register"/>
  /// </summary>
  internal static void Add(AnimationController controller) {
    AnimSet set = GetAnimSetForModType(controller);
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
    AnimSet set = GetAnimSetForModType(manager);
    if (set.Manager is not null) {
      throw new InvalidOperationException(
        $"Cannot have more than one {nameof(AnimationController)} on mod {manager.Mod}");
    }

    set.Manager = manager;
    manager.Index = set.Index;
  }

  public static void Add(Ability ability) {
    AnimSet set = GetAnimSetForModType(ability);
    ability.Index = (ushort)set.Abilities.Count;
    set.Abilities.Add(ability);
  }

  private static AnimSet GetAnimSetForModType<T>(T item) where T : ModType {
    if (AnimationClasses.TryGetValue(item.Mod, out AnimSet set)) {
      return set;
    }

    set = new AnimSet((ushort)AnimationClasses.Count, item.Mod);
    AnimationClasses.Add(item.Mod, set);
    return set;
  }

  internal static void Unload() {
    AnimationClasses.Clear();
  }

  internal static AnimCharacterCollection SetupCharacterCollection(AnimPlayer animPlayer) {
    AnimCharacterCollection characterCollection = new();

    foreach (AnimSet pair in NewInstances(animPlayer.Player)) {
      AnimCharacter character = new(pair.Mod, characterCollection) {
        AnimationController = pair.Controller,
        AbilityManager = pair.Manager
        // TODO: Abilities
      };
      characterCollection.Dict[pair.Mod] = character;
    }

    return characterCollection;
  }

  private static IEnumerable<AnimSet> NewInstances(Player player) {
    foreach (AnimSet pair in AnimationClasses.Values) {
      AnimSet set = new(pair.Index, pair.Mod) {
        Controller = InstantiateAnimationController(player, pair),
        Manager = InstantiateAbilityManager(player, pair)
      };
      yield return set;
    }
  }

  private static AnimationController InstantiateAnimationController(Player player, AnimSet pair) {
    try {
      return pair.Controller.NewInstance(player);
    }
    catch (Exception ex) {
      Log.Error($"Exception thrown when constructing [{pair.Controller.FullName}]", ex);
      throw;
    }
  }

  private static AbilityManager InstantiateAbilityManager(Player player, AnimSet pair) {
    try {
      AbilityManager newManager = pair.Manager.NewInstance(player);
      if (pair.Abilities.Count > 0) {
        var list = new List<Ability>(pair.Abilities.Count);
        foreach (Ability a in pair.Abilities) {
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
      Log.Error($"Exception thrown when constructing [{pair.Manager.FullName}]", ex);
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
  internal AnimationController Controller;
  internal AbilityManager Manager;
  internal List<Ability> Abilities = [];
  internal readonly ushort Index = index;
  internal readonly Mod Mod = mod;
}
