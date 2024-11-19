using System.Linq;
using AnimLib.Animations;
using AnimLib.Networking;
using JetBrains.Annotations;


namespace AnimLib.States;

/// <summary>
/// <see cref="State"/> which can contain children <see cref="State">states</see>.
/// </summary>
/// <param name="entity">The instance of <see cref="Entity"/> this state machine would belong to.</param>
/// <remarks>
/// You should inherit either from
/// <see cref="StateMachine"/> for "Finite State Machine" behaviour, or
/// <see cref="ConcurrentState"/> for multiple states running in parallel.
/// </remarks>
[PublicAPI]
public abstract partial class CompositeState(Entity entity) : State(entity) {
  public abstract IEnumerable<State> ActiveChildren { get; }

  /// <summary>
  /// Collection of children which are the immediate children of this instance.
  /// </summary>
  public IReadOnlyCollection<State> Children => _children;

  /// <summary>
  /// Collection of children which are the children of this instance's children, or their children.
  /// Instances contained in this collection are not included in <see cref="Children"/>.
  /// </summary>
  public IReadOnlyCollection<State> NestedChildren => _nestedChildren;

  /// <summary>
  /// Children as indexed by their type <see cref="Type.Name"/>.
  /// </summary>
  public IReadOnlyDictionary<string, State> ChildrenByType => _childrenByType;

  /// <summary>
  /// Concatenation of <see cref="Children"/> and <see cref="NestedChildren"/>
  /// </summary>
  // TODO: Have AllChildren of non-root instead be a span slice of root's AllChildren array?
  public IEnumerable<State> AllChildren => _children.Concat(_nestedChildren);

  public int AllChildrenCount => _children.Count + _nestedChildren.Count;

  public bool HasChildren => _children.Count > 0;

  private readonly List<State> _children = [];

  private readonly List<State> _nestedChildren = [];

  private readonly Dictionary<string, State> _childrenByType = [];

  [field: AllowNull, MaybeNull]
  private State[] NetChildren => field ??= CreateNetChildren();

  /// <summary>
  /// Adds the specified <see cref="State"/> as a child of this instance.
  /// <para />
  /// When called on a <see cref="StateMachine"/>,the first use of this call will set
  /// <paramref name="child"/> as the initial state of the <see cref="StateMachine"/>.
  /// </summary>
  /// <param name="child">The <see cref="State"/> to add.</param>
  /// <exception cref="ArgumentException">
  /// Cannot add more than one child of the same type as <paramref name="child"/>.
  /// </exception>
  protected T AddChild<T>(T child) where T : State {
    ArgumentNullException.ThrowIfNull(child);

    if (!_childrenByType.TryAdd(child.GetType().Name, child)) {
      throw new ArgumentException($"State {Name} already contains Child {child.Name}");
    }

    _children.Add(child);
    AddAsNestedChild(child);
    child.Parent = this;
    child.UpdateRoot();
    return child;
  }

  /// <summary>
  /// Get the child <see cref="State"/> whose type is <typeparamref name="T"/>.
  /// </summary>
  /// <typeparam name="T">The <see cref="State"/> type.</typeparam>
  /// <returns>The child <see cref="State"/> of type <typeparamref name="T"/>.</returns>
  /// <exception cref="ArgumentException">
  /// This instance has no children of the specified <see cref="Type"/>.
  /// </exception>
  public T GetChild<T>() where T : State => (T)GetChild(typeof(T).Name);

  /// <summary>
  /// Get the child <see cref="State"/> whose type name is <paramref name="type"/>.
  /// </summary>
  /// <param name="type">The name of the <see cref="Type"/> to match.</param>
  /// <returns>The child <see cref="State"/> of type name matching <paramref name="type"/>.</returns>
  /// <exception cref="ArgumentException">
  /// This instance has no children of the specified <see cref="Type"/>.
  /// </exception>
  protected State GetChild(string type) =>
    _childrenByType.TryGetValue(type, out State? state)
      ? state
      : throw new ArgumentException($"{Name} does not contain Child with name \"{type}\"");

  protected State GetChild(int netId) => NetChildren[netId];

  public bool TryGetChild(string type, [MaybeNullWhen(false)] out State child) =>
    _childrenByType.TryGetValue(type, out child);

  public bool HasChild(string type) => _childrenByType.ContainsKey(type);

  public bool TryGetChild<T>([MaybeNullWhen(false)] out T toState) where T : State {
    foreach (State state in _children) {
      if (state is not T t) {
        continue;
      }

      toState = t;
      return true;
    }

    toState = null;
    return false;
  }

  public bool HasActiveChild<T>() where T : State => ActiveChildren.OfType<T>().Any();

  public bool TryGetActiveChild<T>([MaybeNullWhen(false)] out T child) where T : State {
    return (child = ActiveChildren.OfType<T>().FirstOrDefault()) is not null;
  }

  public bool HasNestedChildOfType<T>() =>
    HasChildren && AllChildren.OfType<T>().Any();

  internal override void Initialize() {
    base.Initialize();
    foreach (State state in _children) {
      state.Initialize();
    }

    _children.TrimExcess();
    _childrenByType.EnsureCapacity(_children.Count);
    foreach (State sm in _children) {
      _childrenByType[sm.GetType().Name] = sm;
    }
  }

  /// <summary>
  /// Enter logic to set <see cref="State.ActiveTime"/> to 0,
  /// call this <see cref="State.OnEnter"/> and children <see cref="Enter"/>.
  /// Any overrides should still make those calls.
  /// </summary>
  internal override void Enter(State? fromState) {
    base.Enter(fromState);
    foreach (State child in ActiveChildren) {
      child.Enter(fromState);
    }
  }

  /// <summary>
  /// Exit logic to call children <see cref="Exit"/>, this <see cref="State.OnExit"/>,
  /// and set <see cref="State.ActiveTime"/> to 0.
  /// Currently, no reason to allow overrides.
  /// </summary>
  internal override void Exit() {
    foreach (State sm in ActiveChildren) {
      sm.Exit();
    }

    base.Exit();
  }

  /// <summary>
  /// PreUpdate logic to increment <see cref="State.ActiveTime"/>,
  /// call this <see cref="State.OnPreUpdate"/> and children <see cref="PreUpdate"/>.
  /// Any overrides should still make those calls.
  /// </summary>
  internal override void PreUpdate() {
    base.PreUpdate();
    foreach (State sm in ActiveChildren) {
      sm.PreUpdate();
    }
  }

  /// <summary>
  /// Update logic to call <see cref="State.OnUpdate"/> and children <see cref="Update"/>.
  /// Called only on active children.
  /// </summary>
  internal override void Update() {
    if (!IsActive) {
      return;
    }

    base.Update();
    foreach (State sm in ActiveChildren) {
      sm.Update();
    }
  }

  /// <summary>
  /// PostUpdate logic to call <see cref="State.OnPostUpdate"/> and children <see cref="PostUpdate"/>.
  /// Called on all children.
  /// </summary>
  internal override void PostUpdate() {
    if (!IsActive) {
      return;
    }

    base.PostUpdate();
    foreach (State sm in Children) {
      sm.PostUpdate();
    }
  }


  /// <summary>
  /// Whether to not call <see cref="State.GetAnimationOptions"/> of this <see cref="ActiveChildren"/>.
  /// </summary>
  /// <returns>
  /// <see langword="true"/> to use this instance's <see cref="State.GetAnimationOptions"/>.
  /// <see langword="false"/> to use <see cref="ActiveChildren"/>'s <see cref="State.GetAnimationOptions"/>,
  /// </returns>
  protected virtual bool BlockChildAnimation() => false;

  internal sealed override AnimationOptions? GetAnimationOptionsInternal() {
    if (BlockChildAnimation()) {
      return GetAnimationOptions();
    }

    State? child = ActiveChildren.FirstOrDefault();
    return child?.GetAnimationOptionsInternal() ?? GetAnimationOptions();
  }

  /// <summary>
  /// Called during <see cref="AddChild{T}"/>.
  /// Adds the specified <paramref name="child"/> to the <see cref="NestedChildren"/>
  /// of all <see cref="State.Parent"/>s in <see cref="State.GetParents"/>
  /// </summary>
  /// <param name="child"></param>
  private void AddAsNestedChild(State child) {
    foreach (CompositeState parent in GetParents()) {
      parent._nestedChildren.Add(child);
    }
  }

  private State[] CreateNetChildren() {
    var result = new State[StatesNet.Count];
    foreach (State child in AllChildren) {
      result[child.NetId] = child;
    }

    return result;
  }
}
