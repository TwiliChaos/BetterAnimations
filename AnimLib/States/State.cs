using System.Linq;
using AnimLib.Animations;
using AnimLib.Networking;
using Terraria.ID;

namespace AnimLib.States;

/// <summary>
/// Base class for all States. Represents a single state which the specified <paramref name="entity"/> can be in.
/// </summary>
/// <param name="entity">
/// The instance of <see cref="Entity"/> this state would belong to.
/// </param>
public abstract partial class State(Entity entity) {
  /// <summary>
  /// The <see cref="Entity"/> which this instance belongs to.
  /// </summary>
  public virtual Entity Entity { get; } = entity;

  /// <summary>
  /// The name of this <see cref="State"/>. By default, the name of this <see cref="Type"/>.
  /// </summary>
  public virtual string Name => GetType().Name;

  /// <summary>
  /// The parent which this instance belongs to, -or-
  /// <see langword="null"/> if this instance has no parent.
  /// </summary>
  public CompositeState? Parent { get; internal set; }

  private CompositeState? Root { get; set; }

  /// <summary>
  /// Whether this Entity should be updated locally.
  /// </summary>
  protected bool IsLocal {
    get {
      return Entity switch {
        Player player => player.whoAmI == Main.myPlayer,

        // If Player owner, similar to above;
        Projectile projectile => Main.netMode == NetmodeID.SinglePlayer || projectile.owner == Main.myPlayer,

        // If NPC owner, must not be multiplayer client
        _ => Main.netMode == NetmodeID.SinglePlayer || Main.dedServ
      };
    }
  }

  /// <summary>
  /// Time this State was active, in ticks.
  /// <para>
  /// This is incremented every tick before <see cref="State.OnUpdate"/>,
  /// and is reset to 0 before <see cref="State.OnEnter"/>/<see cref="State.OnExit"/>.
  /// </para>
  /// This property is always synced for calls to <see cref="NetSync"/>, and can safely be used for conditional syncing.
  /// </summary>
  public int ActiveTime {
    get => _activeTime;
    private protected set => _activeTime = value;
  }

  private int _activeTime;

  /// <summary>
  /// Whether this <see cref="State"/> is currently active.
  /// </summary>
  public bool IsActive => (Parent?.ActiveChildren.Contains(this) ?? true) && ActiveCondition;

  /// <summary>
  /// Additional condition to determine whether <see cref="IsActive"/> should return <see langword="true"/>.
  /// </summary>
  protected virtual bool ActiveCondition => true;

  #region Internal Methods (calls OnX)

  /// <summary>
  /// Calls <see cref="State.OnInitialize()"/> on <see langword="this"/>, then on all children.
  /// Used to ensure all children are added in the parent <see cref="State.OnInitialize()"/>
  /// prior to their <see cref="State.OnInitialize()"/> being called.
  /// </summary>
  internal virtual void Initialize() {
    if (StatesNet.NetStates is not null) {
      NetId = StatesNet.GetIdFromState(this);
    }

    OnInitialize();
  }

  /// <summary>
  /// Enter logic to set <see cref="ActiveTime"/> to 0,
  /// call this <see cref="OnEnter"/> and children <see cref="Enter"/>.
  /// Any overrides should still make those calls.
  /// </summary>
  internal virtual void Enter(State? fromState) {
    ActiveTime = 0;
    OnEnter(fromState);
  }

  /// <summary>
  /// Exit logic to call children <see cref="Exit"/>, this <see cref="OnExit"/>,
  /// and set <see cref="ActiveTime"/> to 0.
  /// Currently, no reason to allow overrides.
  /// </summary>
  internal virtual void Exit() {
    OnExit();
    ActiveTime = 0;
  }

  /// <summary>
  /// PreUpdate logic to increment <see cref="ActiveTime"/>,
  /// call this <see cref="OnPreUpdate"/> and children <see cref="PreUpdate"/>.
  /// Any overrides should still make those calls.
  /// </summary>
  internal virtual void PreUpdate() {
    ActiveTime++;
    OnPreUpdate();
  }

  /// <summary>
  /// Update logic to call <see cref="OnUpdate"/> and children <see cref="Update"/>.
  /// </summary>
  internal virtual void Update() {
    OnUpdate();
  }

  /// <summary>
  /// PostUpdate logic to call <see cref="OnPostUpdate"/> and children <see cref="PostUpdate"/>.
  /// </summary>
  internal virtual void PostUpdate() {
    OnPostUpdate();
  }

  internal virtual AnimationOptions? GetAnimationOptionsInternal() => GetAnimationOptions();

  #endregion

  #region OnX Methods

  /// <summary>
  /// Whether this <see cref="State"/> can be entered, irrespective of other state conditions..
  /// </summary>
  /// <returns><see langword="true"/> to allow entering the State.</returns>
  public virtual bool CanEnter() => true;

  /// <summary>
  /// Called during <see cref="AnimCharacter.Initialize"/>,
  /// after the parent <see cref="CompositeState"/> has fully initialized.
  /// Any siblings added in the parent's <see cref="OnInitialize"/> can be accessed here.
  /// </summary>
  protected virtual void OnInitialize() {
  }

  /// <summary>
  /// Method called once the parent <see cref="CompositeState"/> actives this as its child <see cref="State"/>.
  /// <para />
  /// If anything set here, or in <see cref="OnPreUpdateInterruptible"/>, needs to be synced, =
  /// set <see cref="NetUpdate"/> to <see langword="true"/> here.
  /// </summary>
  /// <seealso cref="OnExit"/>
  protected virtual void OnEnter(State? fromState) {
  }

  /// <summary>
  /// Method called once the parent <see cref="CompositeState"/> switches from this child <see cref="State"/>
  /// to a different <see cref="State"/>.
  /// </summary>
  /// <remarks>
  /// Since a <see cref="State"/> can get exited for any reason, including "stun" states or dying,
  /// this method should be used to clean up things, such as killing or restoring dependent entities,
  /// rather than perform "happy path" last tick behaviours.
  /// </remarks>
  /// <seealso cref="OnEnter"/>
  protected virtual void OnExit() {
  }

  /// <summary>
  /// Called every tick, when <see cref="IsActive"/> is <see langword="true"/>.
  /// This method should include transition to other states when necessary. This method should not modify player states.
  /// </summary>
  protected virtual void OnPreUpdate() {
  }

  /// <summary>
  /// Called every tick, if this instance was added as the "to" state in a
  /// <see cref="StateMachine.AddInterruptible{TFrom}"/> call, and the "TFrom" state is currently active.
  /// <para />
  /// Returning <see langword="true"/> will transition the active state to this state.
  /// If one <see cref="OnPreUpdateInterruptible"/> returns <see langword="true"/>, any remaining ones will not be called.
  /// <para />
  /// By default, returns <see langword="false"/>.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> to change the active state to this instance,
  /// <see langword="false"/> to do nothing.</returns>
  /// <seealso cref="StateMachine.AddInterruptible{TFrom}"/>
  protected internal virtual bool OnPreUpdateInterruptible(State activeState) => false;

  /// <summary>
  /// Called every tick, when <see cref="IsActive"/> is <see langword="true"/>.
  /// This method should modify player state when necessary. This method should not include transitions to other states.
  /// </summary>
  protected virtual void OnUpdate() {
  }

  /// <summary>
  /// Called every tick. Note that this is called even when this state is not active.
  /// Check <see cref="IsActive"/> if needed.
  /// </summary>
  protected virtual void OnPostUpdate() {
  }

  /// <summary>
  /// Determines the frame of animation to play for the current character state.
  /// </summary>
  /// <returns></returns>
  /// <remarks>
  /// For simple animations, all that is needed is
  /// <para><c>
  /// override AnimationOptions? GetAnimationOptions() => new("MyAnimationName");
  /// </c></para>
  /// More complex animations may modify the various properties of <see cref="AnimationOptions"/>.
  /// </remarks>
  protected virtual AnimationOptions? GetAnimationOptions() => null;

  #endregion

  public override string ToString() => Name;

  /// <summary>
  /// Whether this state can be transitioned to, from the specified active state.
  /// By default, returns <see langword="true"/>.
  /// </summary>
  /// <param name="fromState">The currently active state to transition from.</param>
  /// <returns><see langword="true"/> to allow transitioning to this state, from the provided state.</returns>
  protected internal virtual bool CanTransitionFrom(State fromState) => true;

  /// <summary>
  /// Attempt to trigger the <see cref="State"/> of type <typeparamref name="T"/> to activate.
  /// <para />
  /// This method does nothing if the current instance's <see cref="State.IsActive"/> is <see langword="false"/>.
  /// <para />
  /// The transition will not occur if the target state's <see cref="State.CanEnter()"/> returns <see langword="false"/>.
  /// <para />
  /// This method will throw if the State Machine tree does not allow the transition,
  /// i.e. the target state is missing, or is not a child of any parent
  /// of the state which this method is called from.
  /// <para />
  /// For consistency, only call this from <see cref="OnPreUpdate"/>.
  /// </summary>
  /// <typeparam name="T">The <see cref="State"/> of type <typeparamref name="T"/> to activate.</typeparam>
  /// <returns>
  /// <see langword="true"/> if the active state was successfully changed; otherwise, <see langword="false"/>.
  /// </returns>
  /// <exception cref="ArgumentException">
  /// There is no parent which has a child of type <typeparamref name="T"/>.
  /// </exception>
  public bool TriggerState<T>() where T : State {
    if (!IsActive || GetType() == typeof(T)) {
      return false;
    }

    State root = Root ?? this;
    string typeName = typeof(T).Name;

    while (true) {
      switch (root) {
        case StateMachine csm when csm.ChildrenByType.TryGetValue(typeName, out State? toState):
          return csm.TrySetActiveChild(toState);
        case CompositeState sm:
          root =
            sm.ActiveChildren.FirstOrDefault(s => s is CompositeState sm2 && sm2.HasNestedChildOfType<T>()) ??
            throw new ArgumentException($"No parent state has child {typeName}", nameof(T));
          break;
        default:
          throw new ArgumentException($"No parent state has child {typeName}", nameof(T));
      }
    }
  }

  /// <summary>
  /// Updates the value of <see cref="Root"/>.
  /// </summary>
  internal void UpdateRoot() {
    CompositeState? root = Parent;
    if (root is null) {
      Root = null;
      return;
    }

    while (root.Parent is not null) {
      root = root.Parent;
    }

    Root = root;
  }

  /// <summary>
  /// Iterates through the chain of parents, where the current element is the previous element's <see cref="Parent"/>.
  /// </summary>
  protected IEnumerable<CompositeState> GetParents() {
    CompositeState? root = Parent;
    while (root is not null) {
      yield return root;

      root = root.Parent;
    }
  }

  /// <summary>
  /// Gets the <see cref="Parent"/> in the chain of parents whose type is <typeparamref name="T"/>.
  /// </summary>
  /// <typeparam name="T">The type of <see cref="Parent"/>.</typeparam>
  /// <returns>The parent of type <typeparamref name="T"/>.</returns>
  /// <exception cref="ArgumentException">No parent is of type <typeparamref name="T"/>.</exception>
  protected T GetParent<T>() where T : CompositeState =>
    GetParents().OfType<T>().FirstOrDefault() ??
    throw new ArgumentException($"No parent is of type {typeof(T).Name}", nameof(T));
}
