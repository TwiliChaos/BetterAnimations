using System.Text;
using AnimLib.UI.Debug;

namespace AnimLib.States;

/// <summary>
/// <see cref="State"/> with additional logic such as Leveling and Cooldown.
/// <para />
/// This class also includes <see cref="Save"/>/<see cref="Load"/> functionality
/// <para />
/// This class requires that <see cref="State.Entity"/> is of type <see cref="Player"/>.
/// </summary>
/// <param name="player">The instance of <see cref="Player"/> this state would belong to.</param>
public abstract partial class AbilityState(Player player) : State(player) {
  public override Player Entity => (Player)base.Entity;

  /// <summary>
  /// Current level of the Ability on this <see cref="Player"/>.
  /// If this is 0, the ability is considered locked and cannot be used.
  /// </summary>
  public int Level {
    get => _level;
    set => _level = value;
  }

  private int _level;
  private int _cooldownLeft;
  private bool _isOnCooldown;

  /// <summary>
  /// The maximum level this Ability may reach through normal gameplay.
  /// This value is not enforced, and actions such as <see cref="AnimLib.Commands.AnimAbilityCommand"/> will ignore it.
  /// </summary>
  public virtual int MaxLevel => 1;

  /// <summary>
  /// Whether the value of <see cref="Level"/> is at least 1.
  /// </summary>
  public bool Unlocked => Level >= 1;

  /// <summary>
  /// Whether this ability can be transitioned to, from the provided state.
  /// By default, returns <see langword="true"/> if
  /// <see cref="Unlocked"/> is <see langword="true"/>, and <see cref="IsOnCooldown"/> is <see langword="false"/>.
  /// </summary>
  public override bool CanEnter() => Unlocked && !IsOnCooldown;

  /// <summary>
  /// Whether this ability uses cooldown features.
  /// </summary>
  /// <remarks>
  /// Currently this is only used in the debug UI.
  /// </remarks>
  public virtual bool SupportsCooldown => false;

  /// <summary>
  /// The value which <see cref="CooldownLeft"/> will be set to when <see cref="StartCooldown"/> is called.
  /// By default, 0.
  /// </summary>
  public virtual int MaxCooldown => 0;

  /// <summary>
  /// Remaining time until this ability may be used again. This is set to <see cref="MaxCooldown"/>
  /// when <see cref="StartCooldown"/> is called.
  /// The ability may still be unusable if <see cref="CanRefresh"/> returns <see langword="false"/>
  /// despite being cooled down.
  /// </summary>
  public int CooldownLeft {
    get => _cooldownLeft;
    private set => _cooldownLeft = value;
  }

  /// <summary>
  /// Whether the ability is on cooldown and cannot be used.
  /// May return <see langword="true"/> even when <see cref="CooldownLeft"/> is <see langword="0"/>
  /// if <see cref="CanRefresh"/> has only returned <see langword="false"/>.
  /// </summary>
  /// <remarks>
  /// On a multiplayer player which is not the local player this will be false.
  /// </remarks>
  public bool IsOnCooldown {
    get => Entity.whoAmI == Main.myPlayer && _isOnCooldown;
    private set => _isOnCooldown = value;
  }

  /// <summary>
  /// Whether this ability should go off cooldown.
  /// By default, returns <see langword="true"/> once <see cref="CooldownLeft"/> reaches 0,
  /// <see langword="false"/> otherwise.
  /// </summary>
  /// <param name="cooledDown">Whether <see cref="CooldownLeft"/> reached 0.</param>
  /// <returns>
  /// <see langword="true"/> to allow this ability to go off cooldown,
  /// <see langword="false"/> to keep this ability on cooldown.
  /// </returns>
  /// <remarks>
  /// This method is called for every <see cref="AbilityState"/> every update.
  /// </remarks>
  protected virtual bool CanRefresh(bool cooledDown) => cooledDown;

  /// <summary>
  /// Put the ability on cooldown.
  /// </summary>
  public void StartCooldown() {
    CooldownLeft = MaxCooldown;
    IsOnCooldown = true;
    OnStartCooldown();
  }

  public void EndCooldown(bool force = false) {
    if (!force && !IsOnCooldown) {
      return;
    }

    CooldownLeft = 0;
    IsOnCooldown = false;
    OnEndCooldown();
  }

  internal void UpdateCooldown() {
    if (!IsOnCooldown) {
      return;
    }

    if (CooldownLeft > 0) {
      CooldownLeft--;
    }

    if (CanRefresh(CooldownLeft <= 0)) {
      EndCooldown();
    }
  }

  protected virtual bool StartCooldownOnEnter => false;
  protected virtual bool StartCooldownOnExit => false;

  /// <summary>
  /// Called when <see cref="StartCooldown"/> is called.
  /// </summary>
  protected virtual void OnStartCooldown() {
  }

  /// <summary>
  /// Called when <see cref="EndCooldown"/> is called.
  /// </summary>
  protected virtual void OnEndCooldown() {
  }

  /// <summary>
  /// Calls <see cref="StartCooldown"/> if <see cref="MaxCooldown"/> is greater than 0.
  /// <inheritdoc cref="State.Enter"/>
  /// </summary>
  internal override void Enter(State? fromState) {
    if (StartCooldownOnEnter) {
      StartCooldown();
    }

    base.Enter(fromState);
  }

  internal override void Exit() {
    if (StartCooldownOnExit) {
      StartCooldown();
    }

    base.Exit();
  }

  protected internal override void DebugText(DebugUIState ui) {
    base.DebugText(ui);
    ui.DrawAppendLabelValue("Level", Level, MaxLevel, color: Level > MaxLevel ? Color.Yellow : null);
    if (IsActive) {
      ui.DrawAppendLabelValue("Active Time:", ActiveTime);
    }

    if (MaxCooldown > 0 || IsOnCooldown) {
      ui.DrawAppendLabelValue("Is Off Cooldown", !IsOnCooldown ? "Yes" : "No",
        IsOnCooldown ? DebugUIState.Red : DebugUIState.Green);
      ui.DrawAppendLabelValue("Cooldown", CooldownLeft, MaxCooldown);
      if (IsOnCooldown && CooldownLeft <= 0) {
        using (ui.Indent()) {
          ui.DrawAppendLine("Another condition prevents cooldown.", Color.LightGray);
        }
      }
    }
  }

  internal string DebugText() {
    StringBuilder sb = new();
    sb.Append(Name);
    sb.Append(' ');
    sb.Append("(Level ");
    sb.Append(Level);
    sb.Append('/');
    sb.Append(MaxLevel);
    sb.Append(") ");
    if (IsActive) {
      sb.Append("ActiveTime: ");
      sb.Append(ActiveTime);
      sb.Append(' ');
    }

    if (MaxCooldown <= 0) {
      return sb.ToString();
    }

    if (IsOnCooldown) {
      sb.Append("Cooldown: ");
      sb.Append(CooldownLeft);
      sb.Append('/');
      sb.Append(MaxCooldown);
    }
    else {
      sb.Append("Off cooldown");
    }

    return sb.ToString();
  }
}
