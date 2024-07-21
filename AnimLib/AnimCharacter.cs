using AnimLib.Abilities;
using AnimLib.Animations;
using AnimLib.Extensions;

namespace AnimLib;

/// <summary>
/// Generic wrapper for non-generic type <see cref="AnimCharacter"/>.
/// </summary>
/// <typeparam name="TAnimation">Your type of <see cref="AnimationController"/></typeparam>
/// <typeparam name="TAbility">Your type of <see cref="AbilityManager"/></typeparam>
[PublicAPI]
public class AnimCharacterWrapper<TAnimation, TAbility>
  where TAnimation : AnimationController where TAbility : AbilityManager {
  internal AnimCharacterWrapper(AnimCharacter character) {
    Character = character;
  }

  public readonly AnimCharacter Character;

  public TAnimation AnimationController => Character.animationController as TAnimation;
  public TAbility AbilityManager => Character.abilityManager as TAbility;
}

public class AnimCharacter {
  /// <summary>
  /// Enum representing the priority of the active character, for determining replacing the active state of a character..
  /// Used to determine if <see cref="AnimCharacter"/> can disable by other <see cref="AnimCharacter">AnimCharacters</see>.
  /// </summary>
  public enum Priority {
    /// <summary>
    /// Low priority. This character can only be enabled if no other characters are in use,
    /// and can be deactivated by any other character.
    /// </summary>
    Lowest = 1,

    /// <summary>
    /// The standard priority. This priority is typically for when the character is enabled by toggle (i.e. right-click item or tile, temporary buff),
    /// and can be disabled by other characters of <see cref="Priority.High">Priority.High</see> or higher priority.
    /// </summary>
    Default = 2,

    /// <summary>
    /// The character should be enabled by the player wearing equipment,
    /// and cannot be disabled by other characters (except by <see cref="Priority.Highest">Priority.Highest</see>.
    /// <para/>
    /// This character can only be disabled by this mod, ideally by the player unequipping the items that enabled it.
    /// </summary>
    High = 3,

    /// <summary>
    /// This character should be enabled no matter what. Consider only using this if you need to force your character state onto a player (i.e. debuff).
    /// This character cannot replace already-enabled characters of the same priority, and cannot be disabled by any other character.
    /// </summary>
    Highest = 4
  }

  internal AnimCharacter(Mod mod, AnimCharacterCollection characters) {
    this.mod = mod;
    this.characters = characters;
  }

  /// <summary>
  /// The <see cref="Mod"/> that this <see cref="AnimCharacter"/> instance belongs to.
  /// </summary>
  [NotNull]
  public Mod mod { get; }

  /// <summary>
  /// The <see cref="AnimationController"/> of this character.
  /// <para/>
  /// This value is your type of <see cref="AnimationController"/> if your mod has a type inheriting <see cref="AnimationController"/>;
  /// otherwise, it is <see langword="null"/>.
  /// </summary>
  [CanBeNull]
  public AnimationController animationController { get; internal init; }

  /// <summary>
  /// The <see cref="AbilityManager"/> of this character.
  /// This value is <see langword="null"/> if your mod does not have any types inheriting types in the <see cref="AnimLib.Abilities"/> namespace.
  /// This value is your type of <see cref="AbilityManager"/> if your mod has a type inheriting <see cref="AbilityManager"/>;
  /// otherwise, it is <see cref="AnimLib"/>'s type <see cref="AbilityManager"/>.
  /// </summary>
  [CanBeNull]
  public AbilityManager abilityManager { get; internal init; }

  [NotNull]
  internal AnimCharacterCollection characters { get; }


  /// <summary>
  /// Whether this <see cref="AnimCharacter"/> is intended to be enabled on the <see cref="Player"/>.
  /// <para/>
  /// This being <see langword="true"/> does not guarantee this <see cref="AnimCharacter"/> is active,
  /// as another character of a higher <see cref="Priority"/> may be active instead.
  /// </summary>
  /// <seealso cref="TryEnable"/>
  /// <seealso cref="IsActive"/>
  public bool IsEnabled { get; private set; }

  /// <summary>
  /// Whether this <see cref="AnimCharacter"/> is the current active character on the <see cref="Player"/>.
  /// <para/>
  /// Only one <see cref="AnimCharacter"/> instance may be active on a character at a given time.
  /// </summary>
  /// <seealso cref="TryEnable"/>
  /// <seealso cref="IsEnabled"/>
  public bool IsActive => IsEnabled && ReferenceEquals(this, characters.ActiveCharacter);

  /// <summary>
  /// The way that the character was enabled by the player.
  /// </summary>
  public Priority currentPriority { get; private set; }

  /// <summary>
  /// Returns a value representing whether you are able to enable the character at this time.
  /// This will return <see langword="false"/> if another <see cref="AnimCharacter"/> of an equal or higher <see cref="Priority"/> is already enabled.
  /// </summary>
  /// <param name="priority">The priority you would be using.</param>
  /// <returns></returns>
  public bool CanEnable(Priority priority) => characters.CanEnable(priority);

  /// <summary>
  /// Attempt to enable your character. Note that you may not be able to enable your character
  /// if another character of a similar or higher <see cref="Priority"/> is already active.
  /// </summary>
  /// <param name="priority">The way that the player enabled the character.</param>
  /// <returns></returns>
  public bool TryEnable(Priority priority = Priority.Default) {
    if (!CanEnable(priority)) return false;
    characters.Enable(this, priority);
    return true;
  }

  internal void Enable(Priority priority = Priority.Default) {
    IsEnabled = true;
    currentPriority = priority;
    OnEnable?.Invoke();
  }

  /// <summary>
  /// Disable your character.
  /// </summary>
  public bool TryDisable() {
    if (!IsEnabled) return false;
    characters.Disable(this);
    return true;
  }

  internal void Disable() {
    IsEnabled = false;
    OnDisable?.Invoke();
  }

  /// <summary>
  /// Event called when the <see cref="AnimCharacter"/> is enabled.
  /// </summary>
  public event Action OnEnable;

  /// <summary>
  /// Event called when the <see cref="AnimCharacter"/> is disabled.
  /// </summary>
  public event Action OnDisable;

  internal void Update() {
    animationController?.UpdateConditions();
    abilityManager?.Update();
  }

  internal void PostUpdate() {
    if (abilityManager is not null) {
      try {
        abilityManager.PostUpdate();
      }
      catch (Exception ex) {
        Log.Error($"[{abilityManager.Mod.Name}:{abilityManager.GetType().UniqueTypeName()}]: Caught exception.", ex);
        Main.NewText($"AnimLib -> {abilityManager.Mod.Name}: Caught exception while updating abilities. See client.log for more information.", Color.Red);
      }
    }

    if (animationController is not null) {
      try {
        if (animationController.PreUpdate()) {
          var options = animationController.Update();
          if (options.TagName is not null) {
            animationController.UpdateAnimation(options);
          }
        }
      }
      catch (Exception ex) {
        Log.Error($"[{animationController.Mod.Name}:{animationController.GetType().UniqueTypeName()}]: Caught exception.", ex);
        Main.NewText($"AnimLib -> {animationController.Mod.Name}: Caught exception while updating animations. See client.log for more information.", Color.Red);
      }
      animationController.UpdateConditionsPost();
    }
  }


  /// <summary>
  /// Creates an instance of AnimCharacterWrapper using the provided types as members.
  /// </summary>
  /// <typeparam name="TAnimation">Your type of <see cref="AnimationController"/></typeparam>
  /// <typeparam name="TAbility">Your type of <see cref="AbilityManager"/></typeparam>
  public AnimCharacterWrapper<TAnimation, TAbility> GetWrapped<TAnimation, TAbility>()
    where TAnimation : AnimationController where TAbility : AbilityManager {
    if (animationController is not null and not TAnimation) {
      throw new ArgumentException(
        $"Type parameter {typeof(TAnimation)} does not match member type {animationController.GetType()}",
        nameof(TAnimation));
    }

    if (abilityManager is not null and not TAbility) {
      throw new ArgumentException(
        $"Type parameter {typeof(TAbility)} does not match member type {abilityManager.GetType()}", nameof(TAbility));
    }

    return new AnimCharacterWrapper<TAnimation, TAbility>(this);
  }
}
