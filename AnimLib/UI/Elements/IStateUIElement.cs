using AnimLib.States;

namespace AnimLib.UI.Elements;

public interface IStateUIElement : IComparable<IStateUIElement> {
  public State? State { get; }
  public StateHierarchy? Hierarchy => State?.Hierarchy;

  int IComparable<IStateUIElement>.CompareTo(IStateUIElement? other) {
    return Hierarchy?.CompareTo(other?.Hierarchy) ?? (other?.Hierarchy is null ? 0 : 1);
  }
}

public interface IStateUIElement<out T> : IStateUIElement where T : State {
  public new T? State => ((IStateUIElement)this).State as T;
}
