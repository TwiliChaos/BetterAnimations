namespace AnimLib.States;

/// <summary>
/// <see cref="CompositeState"/> where all children are always active and receive logic updates,
/// as long as this instance is active.
/// </summary>
public abstract class ConcurrentState(Entity entity) : CompositeState(entity) {
  /// <summary>
  /// Overridden to represent all the children of this instance.
  /// </summary>
  public sealed override IEnumerable<State> ActiveChildren => Children;
}
