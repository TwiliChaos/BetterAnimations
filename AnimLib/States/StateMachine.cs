using System.Linq;

namespace AnimLib.States;

/// <summary>
/// <see cref="CompositeState"/> where up to one child <see cref="State"/> is active at a time.
/// Supports transitions to and from children <see cref="State">States</see>.
/// Includes logic for <see cref="AddInterruptible{TFrom}"/> and <see cref="TrySetActiveChild"/>
/// <para />
/// The first child added with <see cref="CompositeState.AddChild{T}"/> will be the <see cref="ActiveChild"/>
/// when <see cref="Enter"/> is used.
/// </summary>
public abstract partial class StateMachine(Entity entity) : CompositeState(entity) {
  /// <summary>
  /// The current active child instance to receive game updates, -or-
  /// <see langword="null"/> if this instance has no active child instance.
  /// Setting this value will call <see cref="State.Exit"/> on the previous child,
  /// and <see cref="State.Enter"/> on the new child.
  /// </summary>
  public State? ActiveChild {
    get;
    private set {
      if (ReferenceEquals(value, field)) {
        return;
      }

      State? lastChild = field;

      field?.Exit();
      field = value;
      field?.Enter(lastChild);
    }
  }

  /// <summary>
  /// Overridden to represent up to one active child at a time.
  /// </summary>
  public sealed override IEnumerable<State> ActiveChildren {
    get {
      if (ActiveChild is not null) {
        yield return ActiveChild;
      }
    }
  }

  private State? _initialChild;

  internal readonly Dictionary<Type, List<State>> Interrupts = [];

  /// <summary>
  /// Adds an interruptible from the specified <typeparamref name="TFrom"/>, to the specified <paramref name="to"/>.
  /// The specified <paramref name="to"/> will have its <see cref="State.OnPreUpdateInterruptible"/> called while
  /// <typeparamref name="TFrom"/> is active.
  /// </summary>
  /// <typeparam name="TFrom">Child that would be active at time of transition.</typeparam>
  /// <param name="to">Child that can interrupt while <typeparamref name="TFrom"/> is active.</param>
  /// <exception cref="ArgumentNullException">
  /// <paramref name="to"/> cannot be <see langword="null"/>.
  /// </exception>
  /// <exception cref="ArgumentException">
  /// The specified arguments must already be children of this <see cref="StateMachine"/> instance,
  /// and must not already have an interruptible registered.
  /// <para>
  /// <typeparamref name="TFrom"/> cannot already have an interruptible to <paramref name="to"/>.
  /// </para>
  /// </exception>
  /// <remarks>
  /// This is mainly used for certain <see cref="State"/>s which can activate from a wider variety of states,
  /// and activation logic would be better suited in the <paramref name="to"/> state that would be activated.
  /// </remarks>
  /// <seealso cref="State.OnPreUpdateInterruptible"/>
  public void AddInterruptible<TFrom>(State to) where TFrom : State {
    ArgumentNullException.ThrowIfNull(to);

    string name = typeof(TFrom).Name;
    if (!ChildrenByType.TryGetValue(name, out State? from)) {
      throw new ArgumentException($"State {Name} does not have Child {name} to transition from", nameof(TFrom));
    }

    if (!ChildrenByType.Values.Contains(to)) {
      throw new ArgumentException($"State {Name} does not have Child {to} to transition to.", nameof(to));
    }

    if (!Interrupts.TryGetValue(from.GetType(), out var list)) {
      Interrupts.Add(from.GetType(), [to]);
      return;
    }

    if (list.Contains(to)) {
      throw new ArgumentException($"State {from} already contains transition {to}", nameof(to));
    }

    list.Add(to);
  }

  internal override void Initialize() {
    base.Initialize();

    // Additionally set first child to _initialChild
    State? firstChild = Children.FirstOrDefault();
    if (firstChild is not null) {
      _initialChild = firstChild;
    }
  }

  internal sealed override void Enter(State? fromState) {
    ActiveTime = 0;
    if (ActiveChild is null && _initialChild is not null) {
      ActiveChild = _initialChild;
    }

    OnEnter(fromState);
    ActiveChild?.Enter(fromState);
  }

  internal sealed override void PreUpdate() {
    if (!IsActive) {
      return;
    }

    ActiveTime++;
    OnPreUpdate();
    State? activeChild = ActiveChild;
    if (activeChild is not null && _interrupts.TryGetValue(activeChild.GetType(), out var list)) {
      State? interrupt = list.FirstOrDefault(i => i.CanEnter() && i.OnPreUpdateInterruptible(activeChild));
      if (interrupt is not null) {
        ActiveChild = interrupt;
        NetUpdate = true;
        return;
      }
    }

    foreach (State sm in ActiveChildren) {
      sm.PreUpdate();
    }
  }

  /// <summary>
  /// Attempt to set the <see cref="ActiveChild"/> to the current child.
  /// This method will do nothing if <see cref="ActiveChild"/> and <paramref name="toChild"/> are the same.
  /// </summary>
  /// <param name="toChild">
  /// The child to attempt to set <see cref="ActiveChild"/> to.
  /// </param>
  /// <param name="checkTransition">
  /// Whether to require
  /// <see cref="State.CanTransitionFrom"/>,
  /// and <see cref="State.CanEnter"/> be checked.
  /// The change will not occur if any of those methods return false.
  /// </param>
  /// <param name="silent">
  /// Whether to prevent NetUpdate if this method succeeds.
  /// </param>
  /// <exception cref="ArgumentException">
  /// <paramref name="toChild"/> is not a child of this instance.
  /// </exception>
  protected internal bool TrySetActiveChild(State toChild, bool checkTransition = true, bool silent = false) {
    if (ReferenceEquals(ActiveChild, toChild)) {
      return false;
    }

    if (!ChildrenByType.ContainsKey(toChild.GetType().Name)) {
      throw new ArgumentException($"{Name} does not contain child {toChild.Name}");
    }

    if (checkTransition) {
      if (ActiveChild is not null &&
          (!toChild.CanEnter() ||
            !toChild.CanTransitionFrom(ActiveChild))) {
        return false;
      }
    }

    ActiveChild = toChild;
    if (!silent) {
      NetUpdate = true;
    }

    return true;
  }

  private void ClearActiveChild(bool silent = false) {
    ActiveChild = null;
    if (!silent) {
      NetUpdate = true;
    }
  }
}
