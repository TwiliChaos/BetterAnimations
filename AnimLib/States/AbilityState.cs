using System.Text;
using AnimLib.Networking;

namespace AnimLib.States;

/// <summary>
/// <see cref="State"/> with additional logic such as Leveling, Cooldown. Requires that <see cref="State"/>
/// "Entity" would be of type Player.
/// </summary>
/// <param name="player"></param>
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

  public void EndCooldown() {
    if (!IsOnCooldown) {
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

  internal void SyncFromCharacter(ISync sync) {
    sync.Sync7BitEncodedInt(ref _level);
  }
}
