using System;
using System.Collections.Generic;
using System.Linq;
using AnimLib.Abilities;
using AnimLib.Animations;
using AnimLib.Compat;
using AnimLib.Extensions;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Core;
using Terraria.ModLoader.Exceptions;

namespace AnimLib.Internal;

/// <summary>
/// Manages the construction and distribution of all <see cref="AnimationSource"/>s and <see cref="AnimationController"/>s.
/// <para>
/// <strong>
/// <see cref="AnimationSource"/>
/// </strong>
/// </para>
/// <para>On <see cref="Mod.Load"/>, all <see cref="AnimationSource"/>s are constructed.</para>
/// <para>On <see cref="Mod.PostSetupContent"/>, all <see cref="AnimationSource"/>s have their Textures assigned.</para>
/// <para>
/// <strong>
/// <see cref="AnimationController"/>
/// </strong>
/// </para>
/// <para>On <see cref="Mod.Load"/>, all <see cref="Type"/>s of <see cref="AnimationController"/> are collected.</para>
/// <para>On <see cref="ModPlayer.Initialize"/>, all <see cref="AnimationController"/>s are constructed and added to the <see cref="AnimPlayer"/>.</para>
/// </summary>
internal static class AnimLoader {
  /// <summary>
  /// Collection of all <see cref="Type"/>s of <see cref="AnimationController"/>, collected during <see cref="Mod.Load"/> and constructed during
  /// <see cref="ModPlayer.Initialize"/>.
  /// </summary>
  internal static Dictionary<Mod, Type> modAnimationControllerTypeDictionary;

  internal static Dictionary<Mod, Type> modAbilityManagerTypeDictionary;
  internal static Dictionary<Mod, IEnumerable<Type>> modAbilityTypeDictionary;
  private static List<Mod> _loadedMods;

  /// <summary>
  /// Collection of all <see cref="AnimationSources"/>, constructed during <see cref="Mod.Load"/>.
  /// </summary>
  internal static Dictionary<Mod, AnimationSource[]> AnimationSources { get; private set; }

  /// <summary>
  /// Whether or not to use animations during this session. Returns <see langword="true"/> if this is not run on a server; otherwise,
  /// <see langword="false"/>.
  /// </summary>
  public static bool UseAnimations => Main.netMode != NetmodeID.Server;

  public static bool HasMods =>
    (modAnimationControllerTypeDictionary?.Count ?? 0) + (modAbilityTypeDictionary?.Count ?? 0) != 0;

  public static List<Mod> LoadedMods =>
    _loadedMods ??= modAnimationControllerTypeDictionary.Keys.Union(modAbilityManagerTypeDictionary.Keys).ToList();

  public static bool GetLoadedMods(out List<Mod> loadedMods) => (loadedMods = HasMods ? LoadedMods : null) != null;


  private static void Unload() {
    Log.Debug($"{nameof(AnimLoader)}.{nameof(Unload)} called.");
    AnimationSources = null;
    modAnimationControllerTypeDictionary = null;
    modAbilityManagerTypeDictionary = null;
    modAbilityTypeDictionary = null;
  }

  /// <summary>
  /// Searches all mods for any and all classes extending <see cref="AnimationSource"/> and <see cref="AnimationController"/>.
  /// <para>For <see cref="AnimationSource"/>s, they will be constructed, check for loading, log errors and skip if applicable, and added to the dict.</para>
  /// </summary>
  internal static void Load() {
    AnimLibMod.OnUnload += Unload;
    AnimLibMod.OnUnload += GlobalCompatConditions.Unload;

    AnimationSources = new Dictionary<Mod, AnimationSource[]>();
    modAnimationControllerTypeDictionary = new Dictionary<Mod, Type>();
    modAbilityManagerTypeDictionary = new Dictionary<Mod, Type>();
    modAbilityTypeDictionary = new Dictionary<Mod, IEnumerable<Type>>();

    foreach (Mod mod in ModLoader.Mods) {
      if (CanLoadMod(mod, out var types))
        LoadMod(mod, types);
    }

    int sourcesCount = AnimationSources.Count;
    int controllerCount = modAnimationControllerTypeDictionary.Count;
    if (sourcesCount == 0 && controllerCount == 0) {
      Log.Info($"AnimLibMod loaded with {sourcesCount} sources and {controllerCount} controllers.");
    }
  }

  private static bool CanLoadMod(Mod mod, out List<Type> types) {
    if (mod is AnimLibMod || mod.Code is null) {
      types = null;
      return false;
    }

    // Collect all instantiatable types.
    // Collect only animation types if the mod is not being run on a server
    types = (from t in AssemblyManager.GetLoadableTypes(mod.Code)
      where !t.IsAbstract && !t.IsGenericType &&
        (t.IsSubclassOf(typeof(AnimationSource)) && UseAnimations ||
          t.IsSubclassOf(typeof(AnimationController)) && UseAnimations ||
          t.IsSubclassOf(typeof(AbilityManager)) ||
          t.IsSubclassOf(typeof(Ability)))
      select t).ToList();
    return types.Count != 0;
  }

  private static void LoadMod(Mod mod, List<Type> types) {
    if (UseAnimations) AnimationLoader.Load(mod, types);

    AbilityLoader.Load(mod, types);
  }
}

internal static class AnimationLoader {
  public static void Load(Mod mod, List<Type> types) {
    if (!GetSourcesFromTypes(types, mod, out var sources))
      return;

    if (!GetControllerTypeFromTypes(types, mod, out Type controllerType)) {
      string classStr = sources.Count > 1 ? "classes" : "a class";
      Log.Warn(
        $"{mod.Name} error: " +
        $"{mod.Name} contains {classStr} extending {nameof(AnimationSource)}, " +
        $"but does not contain any classes extending {nameof(AnimationController)}s.");
      return;
    }

    AnimLoader.AnimationSources[mod] = sources.ToArray();
    AnimLoader.modAnimationControllerTypeDictionary[mod] = controllerType;
  }

  /// <summary>
  /// Searches all types from the given <see cref="Mod"/> for <see cref="AnimationSource"/>, and checks if they should be included.
  /// </summary>
  private static bool GetSourcesFromTypes(IEnumerable<Type> types, Mod mod, out List<AnimationSource> sources) {
    sources = new List<AnimationSource>();
    foreach (Type type in types) {
      if (!type.IsSubclassOf(typeof(AnimationSource))) continue;

      if (!TryConstructSource(type, mod, out AnimationSource source)) continue;
      sources.Add(source);
      Log.Info($"[{mod.Name}]: Collected {nameof(AnimationSource)} \"{type.UniqueTypeName()}\"");
    }

    return sources.Count != 0;
  }

  /// <summary>
  /// Searches for a single type of <see cref="AnimationController"/> from the given <see cref="Mod"/>, and rejects others if more than one if found.
  /// </summary>
  private static bool GetControllerTypeFromTypes(IEnumerable<Type> types, Mod mod, out Type result) {
    result = null;
    foreach (Type type in types) {
      if (!type.IsSubclassOf(typeof(AnimationController))) continue;
      if (result is not null) {
        throw new CustomModDataException(mod, $"Cannot have more than one {nameof(AnimationController)} per mod.",
          null);
      }

      Log.Info($"[{mod.Name}]: Collected {nameof(AnimationController)} \"{type.UniqueTypeName()}\"");
      result = type;
    }

    return result != null;
  }

  /// <summary>
  /// Attempts to construct the animation source, and rejects any that have bad inputs.
  /// </summary>
  private static bool TryConstructSource(Type type, Mod mod, out AnimationSource source) {
    source = (AnimationSource)Activator.CreateInstance(type, true);

    // ReSharper disable once PossibleNullReferenceException
    string fullName = source.GetType().FullName;
    if (fullName is null)
      throw new ArgumentException($"Invalid full type name from type {source.GetType()}", nameof(type));

    string texturePath = fullName.Replace('.', '/');
    if (!source.Load(ref texturePath)) {
      source = null;
      return false;
    }

    Asset<Texture2D> _t;
    if (AnimationSource.texture_assets.TryGetValue(texturePath, out var asset)) {
      _t = asset;
    }
    else {
      if (!ModContent.HasAsset(texturePath))
        throw new MissingResourceException(
          $"[{mod.Name}:{type.FullName}]: Error constructing {type.Name}: Invalid texture path \"{texturePath}\".");
    }

    if (source.tracks is null)
      throw new Exception(
        $"[{mod.Name}:{type.FullName}]: Error constructing {type.Name}: Tracks cannot be null.");

    if (source.spriteSize.x == 0 || source.spriteSize.y == 0)
      throw new Exception(
        $"[{mod.Name}:{type.FullName}]: Error constructing {type.Name}: Sprite Size cannot contain a value of 0.");

    source.mod = mod;
    source.texture = _t;
    return true;
  }
}

internal static class AbilityLoader {
  public static void Load(Mod mod, List<Type> types) {
    if (!GetAbilityTypesFromTypes(types, mod, out var abilityTypes)) return;
    AnimLoader.modAbilityTypeDictionary[mod] = abilityTypes;
    if (GetAbilityManagerTypeFromTypes(types, mod, out Type managerType))
      AnimLoader.modAbilityManagerTypeDictionary[mod] = managerType;
  }

  private static bool GetAbilityManagerTypeFromTypes(IEnumerable<Type> types, Mod mod, out Type type) {
    type = null;
    foreach (Type t in types) {
      if (!t.IsSubclassOf(typeof(AbilityManager))) continue;
      if (type is not null)
        throw new CustomModDataException(mod, $"Cannot have more than one {nameof(AbilityManager)} per mod.", null);

      Log.Info($"[{mod.Name}]: Collected {nameof(AbilityManager)} \"{t.UniqueTypeName()}\"");
      type = t;
    }

    return type != null;
  }

  private static bool GetAbilityTypesFromTypes(IEnumerable<Type> types, Mod mod, out List<Type> abilityTypes) {
    abilityTypes = new List<Type>();
    foreach (Type type in types) {
      if (!type.IsSubclassOf(typeof(Ability))) continue;
      abilityTypes.Add(type);
      Log.Info($"From mod {mod.Name} collected {nameof(AnimationSource)} \"{type.UniqueTypeName()}\"");
    }

    return abilityTypes.Count != 0;
  }
}
