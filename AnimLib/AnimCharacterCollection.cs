using System.Collections;

namespace AnimLib;

internal class AnimCharacterCollection : IReadOnlyDictionary<Mod, AnimCharacter> {
  private readonly CharStack<AnimCharacter> _characterStack = new();
  private AnimCharacter.Priority _activePriority;

  internal AnimCharacterCollection() {
  }

  internal readonly Dictionary<Mod, AnimCharacter> Dict = [];
  [CanBeNull] public AnimCharacter ActiveCharacter { get; private set; }

  public bool ContainsKey(Mod key) => Dict.ContainsKey(key);
  public bool TryGetValue(Mod key, out AnimCharacter value) => Dict.TryGetValue(key, out value);

  public AnimCharacter this[Mod mod] => Dict[mod];

  public IEnumerable<Mod> Keys => Dict.Keys;
  public IEnumerable<AnimCharacter> Values => Dict.Values;


  public IEnumerator<KeyValuePair<Mod, AnimCharacter>> GetEnumerator() => Dict.GetEnumerator();

  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public int Count => Dict.Count;

  public bool CanEnable(AnimCharacter.Priority priority = AnimCharacter.Priority.Default) {
    if (ActiveCharacter == null) return true;
    return _activePriority switch
    {
      AnimCharacter.Priority.Lowest => true,
      _ => priority > _activePriority,
    };
  }

  /// <summary>
  /// Enables the given <see cref="AnimCharacter"/> with the given <see cref="AnimCharacter.Priority"/>.
  /// If there was an <see cref="ActiveCharacter"/>, it will be disabled and put into the character stack.
  /// </summary>
  /// <param name="character"></param>
  /// <param name="priority"></param>
  internal void Enable([NotNull] AnimCharacter character, AnimCharacter.Priority priority) {
    AnimCharacter previous = ActiveCharacter;
    if (previous is not null) {
      previous.Disable();
      // Set stack position of previous active char to most recent.
      _characterStack.TryRemove(previous);
      _characterStack.Push(previous);
    }


    ActiveCharacter = character;
    _characterStack.TryRemove(character);
    ActiveCharacter.Enable();
    _activePriority = priority;
  }

  /// <summary>
  /// Disable the given <see cref="AnimCharacter"/>.
  /// If <paramref name="character"/> was <see cref="ActiveCharacter"/>, <see cref="ActiveCharacter"/> will be replaced with the next character in the stack.
  /// </summary>
  /// <param name="character">The <see cref="AnimCharacter"/> to disable.</param>
  internal void Disable([NotNull] AnimCharacter character) {
    _characterStack.TryRemove(character);
    if (character == ActiveCharacter) ActiveCharacter = _characterStack.Pop();
  }
}

internal class CharStack<T> {
  private readonly List<T> _items;
  public CharStack() => _items = [];
  public CharStack(int count) => _items = new List<T>(count);

  public int Count => _items.Count;

  public void Push([NotNull] T item) {
    ArgumentNullException.ThrowIfNull(item);
    _items.Add(item);
  }

  [CanBeNull]
  public T Pop() {
    if (_items.Count <= 0) return default;
    T temp = _items[^1];
    _items.RemoveAt(_items.Count - 1);
    return temp;
  }

  public bool Contains([NotNull] T item) {
    ArgumentNullException.ThrowIfNull(item);
    return _items.IndexOf(item) >= 0;
  }

  public void Remove(int itemAtPosition) => _items.RemoveAt(itemAtPosition);

  public void TryRemove([NotNull] T item) {
    ArgumentNullException.ThrowIfNull(item);
    int index = _items.IndexOf(item);
    if (index >= 0) Remove(index);
  }
}
