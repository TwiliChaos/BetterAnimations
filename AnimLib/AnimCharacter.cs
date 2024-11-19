using System.Linq;
using AnimLib.Animations;
using AnimLib.States;
using AnimLib.UI.Debug;
using JetBrains.Annotations;
using Terraria.ModLoader.IO;

namespace AnimLib;

/// <summary>
/// <see cref="ConcurrentState"/> which represents a Character.
/// <see cref="State"/>s which are the immediate child of an <see cref="AnimCharacter"/> are always active,
/// as long as the <see cref="AnimCharacter"/> is active.
/// </summary>
[PublicAPI]
public abstract partial class AnimCharacter : ConcurrentState {
  /// <summary>
  /// Creates an instance of <see cref="AnimCharacter"/> for the specified <paramref name="modPlayer"/>.
  /// You should not create more than one of the same type of <see cref="AnimCharacter"/>
  /// for the same <paramref name="modPlayer"/>
  /// </summary>
  /// <param name="modPlayer"></param>
  /// <exception cref="InvalidOperationException">
  /// An instance of this type was already created for the specified <paramref name="modPlayer"/>.
  /// </exception>
  protected AnimCharacter(ModPlayer modPlayer) : base(modPlayer.Player) {
    ModPlayer = modPlayer;
    Characters = modPlayer.Player.GetModPlayer<AnimPlayer>().Characters;
    Parent = Characters;

    if (!Characters.TryAddCharacter(this)) {
      throw new InvalidOperationException(
        $"Cannot create more than one instance of {GetType().Name} for ModPlayer {modPlayer}");
    }
  }

  /// <summary>
  /// Enum representing the priority of the active character, for determining replacing the active state of a character.
  /// Used to determine if <see cref="AnimCharacter"/> can disable by other <see cref="AnimCharacter">AnimCharacters</see>.
  /// </summary>
  [PublicAPI]
  public enum ActivationPriority {
    /// <summary>
    /// Low priority. This character can only be enabled if no other characters are in use,
    /// and can be deactivated by any other character.
    /// <para />
    /// A use case may be characters which only provide visual effects.
    /// </summary>
    Lowest = 1,

    /// <summary>
    /// The standard priority. This priority is typically for when the character is enabled by toggle (i.e. right-click item or tile, temporary buff),
    /// and can be disabled by other characters of <see cref="ActivationPriority.High">Priority.High</see> or higher priority.
    /// </summary>
    Default = 2,

    /// <summary>
    /// The character should be enabled by the player wearing equipment,
    /// and cannot be disabled by other characters (except by <see cref="ActivationPriority.Highest">Priority.Highest</see>).
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

  public override Player Entity => (Player)base.Entity;

  public abstract ActivationPriority Priority { get; }

  /// <summary>
  /// The <see cref="ModPlayer"/> which created this.
  /// </summary>
  public virtual ModPlayer ModPlayer { get; }

  public AnimCharacterCollection Characters { get; }

  [field: AllowNull, MaybeNull]
  public IEnumerable<AbilityState> AbilityStates => field ??= AllChildren.OfType<AbilityState>().ToArray();

  private bool _isEnabled;


  /// <summary>
  /// Whether this <see cref="AnimCharacter"/> is intended to be enabled on the <see cref="Player"/>.
  /// <para/>
  /// This being <see langword="true"/> does not guarantee this <see cref="AnimCharacter"/> is active,
  /// as another character of a higher <see cref="ActivationPriority"/> may be active instead.
  /// </summary>
  /// <seealso cref="TryEnable"/>
  /// <seealso cref="ActiveCondition"/>
  public bool IsEnabled {
    get => _isEnabled;
    internal set => _isEnabled = value;
  }

  /// <summary>
  /// Whether this <see cref="AnimCharacter"/> is the current active character on the <see cref="Player"/>.
  /// <para/>
  /// Only one <see cref="AnimCharacter"/> instance may be active on a character at a given time.
  /// </summary>
  /// <seealso cref="TryEnable"/>
  /// <seealso cref="IsEnabled"/>
  protected override bool ActiveCondition => IsEnabled && AnimationUpdEnabledCompat;

  public AbilityState GetAbility(string name) {
    foreach (AbilityState asm in AbilityStates) {
      if (asm.Name == name) {
        return asm;
      }
    }

    throw new ArgumentException($"No Ability matches name {name}");
  }

  public T GetAbility<T>() where T : AbilityState {
    return AbilityStates.OfType<T>().FirstOrDefault()
      ?? throw new ArgumentException($"No Ability is of type {typeof(T).Name}");
  }

  /// <summary>
  /// Returns a value representing whether you are able to enable the character at this time.
  /// This will return <see langword="false"/> if another <see cref="AnimCharacter"/> of an equal or higher <see cref="ActivationPriority"/> is already enabled.
  /// </summary>
  /// <returns></returns>
  public bool CanEnable() => Characters.CanEnable(this);

  /// <summary>
  /// Attempt to enable your character. Note that you may not be able to enable your character
  /// if another character with a similar or higher <see cref="ActivationPriority"/> is already active.
  /// </summary>
  /// <returns>
  /// <see langword="false"/> if <see cref="CanEnable"/> would return <see langword="false"/>;
  /// otherwise, enables your character and returns <see langword="true"/>
  /// </returns>
  public bool TryEnable() {
    bool canEnable = Characters.CanEnable(this);
    if (canEnable) {
      Characters.Enable(this);
      IsEnabled = true;
    }

    return canEnable;
  }

  /// <summary>
  /// Disable your character. This method does nothing if <see cref="IsEnabled"/> was already <see langword="false"/>.
  /// </summary>
  public void Disable() {
    if (IsEnabled) {
      Characters.Disable(this);
      IsEnabled = false;
    }
  }

  protected override void OnUpdate() {
    foreach (AbilityState asm in AbilityStates) {
      if (!asm.Unlocked) {
        continue;
      }

      asm.UpdateCooldown();
    }

    UpdateConditions();
    foreach (State? state in ActiveChildren) {
      state.Update();
    }
  }

  internal override void PostUpdate() {
    OnPostUpdate();
    foreach (State state in Children) {
      try {
        state.PostUpdate();
      }
      catch (Exception ex) {
        Log.Error($"[{state}]: Caught exception.", ex);
        Main.NewText(
          $"AnimLib -> {state}: Caught exception while updating states. See client.log for more information.",
          Color.Red);
      }
    }

    if (AnimationUpdEnabledCompat && !Main.dedServ && IsActive) {
      foreach (AnimatedStateMachine animatedState in ActiveChildren.OfType<AnimatedStateMachine>()) {
        AnimationOptions? options = null;
        try {
          options = animatedState.GetAnimationOptionsInternal();
        }
        catch (Exception ex) {
          Log.Error($"[{animatedState}]: Caught exception.", ex);
          Main.NewText(
            $"AnimLib -> {animatedState}: Caught exception while updating animations. See client.log for more information.",
            Color.Red);
        }

        if (options is not null) {
          animatedState.UpdateAnimation(options.Value);
        }
      }
    }

    UpdateConditionsPost();
  }

  /// <summary>
  /// Sets the level of all Abilities to their max level.
  /// </summary>
  public void UnlockAllAbilities() {
    foreach (AbilityState state in AbilityStates) {
      state.Level = state.MaxLevel;
    }
  }

  /// <summary>
  /// Sets the level of all Abilities to 0.
  /// </summary>
  public void ResetAllAbilities() {
    foreach (AbilityState state in AbilityStates) {
      state.Level = 0;
    }
  }

  /// <summary>
  /// Allows to add additional data to tag compound for this <see cref="AnimCharacter"/>
  /// </summary>
  /// <param name="tag"> An instance of <see cref="TagCompound"/> containing <see cref="States.AbilityState"/> save data. </param>
  protected virtual void SaveCustomData(TagCompound tag) {
  }

  /// <summary>
  /// Allows to get additional data from tag compound for this <see cref="AnimCharacter"/>
  /// </summary>
  /// <param name="tag"> An instance of <see cref="TagCompound"/> containing <see cref="States.AbilityState"/> save data. </param>
  protected virtual void LoadCustomData(TagCompound tag) {
  }

  protected internal override void DebugHeader(DebugUIState ui, bool redOnInactive = false) {
    ui.DrawAppendLabelValue("Character", Name,
      IsActive ? DebugUIState.Green : redOnInactive ? DebugUIState.Red : Color.LightGray);
  }
}
