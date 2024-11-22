using System.IO;
using System.Linq;
using AnimLib.Networking;
using AnimLib.States;
using AnimLib.UI.Debug;

namespace AnimLib;

public sealed class AnimCharacterCollection : StateMachine {
  internal AnimCharacterCollection(Player player) : base(player) {
  }

  public override Player Entity => (Player)base.Entity;

  public AnimCharacter? ActiveCharacter => ActiveChild as AnimCharacter;

  public IEnumerable<AnimCharacter> Characters => Children.OfType<AnimCharacter>();

  private readonly CharacterStack _characterStack = new();

  private TimeSpan _lastAnimationUpdate;

  internal bool TryAddCharacter(AnimCharacter character) {
    if (ChildrenByType.ContainsKey(character.GetType().Name)) {
      return false;
    }

    AddChild(character);
    return true;
  }

  public T FindAbility<T>() where T : AbilityState {
    foreach (AnimCharacter character in Characters) {
      T? ability = character.AbilityStates.OfType<T>().FirstOrDefault();
      if (ability is not null) {
        return ability;
      }
    }

    throw new ArgumentException($"No character contains Ability of type {typeof(T).Name}");
  }

  public bool CanEnable(AnimCharacter character) {
    ArgumentNullException.ThrowIfNull(character);
    return ActiveChild is not AnimCharacter activeCharacter ||
      activeCharacter.Priority == AnimCharacter.ActivationPriority.Lowest ||
      activeCharacter.Priority != AnimCharacter.ActivationPriority.Highest ||
      activeCharacter.Priority < character.Priority;
  }

  /// <summary>
  /// Enables the given <see cref="AnimCharacter"/> with the given <see cref="AnimCharacter.ActivationPriority"/>.
  /// If there was an <see cref="ActiveCharacter"/>, it will be disabled and put into the character stack.
  /// </summary>
  /// <param name="character"></param>
  internal void Enable(AnimCharacter character) {
    if (ActiveCharacter is not null) {
      // Set stack position of previous active char to most recent.
      _characterStack.TryRemove(ActiveCharacter);
      _characterStack.Push(ActiveCharacter);
    }

    TrySetActiveChild(character);

    _characterStack.TryRemove(character);
    ModContent.GetInstance<DebugUISystem>().TrySetActiveCharacter(this);
  }

  /// <summary>
  /// Disable the given <see cref="AnimCharacter"/>.
  /// If <paramref name="character"/> was <see cref="ActiveCharacter"/>,
  /// <see cref="ActiveCharacter"/> will be replaced with the next character in the stack.
  /// </summary>
  /// <param name="character">The <see cref="AnimCharacter"/> to disable.</param>
  internal void Disable(AnimCharacter character) {
    _characterStack.TryRemove(character);
    if (character != ActiveCharacter) {
      return;
    }

    AnimCharacter? newCharacter = _characterStack.Pop();
    if (newCharacter is not null) {
      Enable(newCharacter);
    }
    else {
      ClearActiveChild();
      ModContent.GetInstance<DebugUISystem>().TrySetActiveCharacter(this);
    }
  }

  internal void UpdateAnimations() {
    TimeSpan currentTime = Main.gameTimeCache.TotalGameTime;
    float delta = (float)(currentTime - _lastAnimationUpdate).TotalSeconds;
    _lastAnimationUpdate = currentTime;
    if (delta > 0) {
      ActiveCharacter?.UpdateAnimations(delta);
    }
  }

  internal void NetSyncAll(ISync sync) {
    var iter = sync.SyncEnumerate(this, GetAllChildrenCount, GetAllChildren, WriteChildId, ReadChildId);

    foreach (State? netUpdateChild in iter) {
      netUpdateChild.NetSyncInternal(sync);
    }

    return;

    static int GetAllChildrenCount(AnimCharacterCollection me) => me.AllChildrenCount;
    static IEnumerable<State> GetAllChildren(AnimCharacterCollection me) => me.AllChildren;
    static void WriteChildId(BinaryWriter writer, State child) => writer.Write(child.NetId);
    static State ReadChildId(AnimCharacterCollection me, BinaryReader reader) => me.GetChild(reader.ReadInt16());
  }

  /// <summary>
  /// If only one nested child needs a <see cref="State.NetUpdate"/>,
  /// this method will call the non-internal <see cref="State.NetSync"/>,
  /// "<inheritdoc cref="StateMachine.NetSyncActiveChild"/>"
  /// and call that one child's <see cref="State.NetSyncInternal"/>
  /// <para />
  /// If more than one nested child requires an update,
  /// instead performs base class functionality (all the below):
  /// <para />
  /// Calls StateMachine:
  /// <para><inheritdoc cref="StateMachine.NetSyncInternal"/></para>
  /// End StateMachine.
  /// </summary>
  /// <param name="sync"></param>
  internal override void NetSyncInternal(ISync sync) {
    // Should reduce packet size
    if (sync is IWriteSync writeSync && TryWriteSingleUpdate(writeSync) ||
        sync is IReadSync readSync && TryReadSingleUpdate(readSync)) {
      return;
    }

    base.NetSyncInternal(sync);
  }

  private bool TryWriteSingleUpdate(IWriteSync write) {
    BinaryWriter writer = write.Writer;

    int netCount = GetAllNetChildren(includeIndirect: false).Count();

    bool hasSingleUpdate = netCount == 1;
    writer.Write(!hasSingleUpdate);
    if (!hasSingleUpdate) {
      return false;
    }

    NetSync(write);
    NetSyncActiveChild(write);
    State child = GetAllNetChildren(includeIndirect: false).First();
    writer.Write7BitEncodedInt(child.NetId);
    child.NetSyncInternal(write);
    return true;
  }

  private bool TryReadSingleUpdate(IReadSync read) {
    BinaryReader reader = read.Reader;

    bool multiUpdate = reader.ReadBoolean();
    if (multiUpdate) {
      return false;
    }

    NetSync(read);
    NetSyncActiveChild(read);
    int id = reader.Read7BitEncodedInt();
    State child = GetChild(id);
    child.NetSyncInternal(read);
    return true;
  }
}

internal class CharacterStack {
  private readonly List<AnimCharacter> _items = [];

  public void Push(AnimCharacter item) {
    ArgumentNullException.ThrowIfNull(item);
    _items.Add(item);
  }

  public AnimCharacter? Pop() {
    if (_items.Count <= 0) {
      return default;
    }

    AnimCharacter last = _items[^1];
    _items.RemoveAt(_items.Count - 1);
    return last;
  }

  public void TryRemove(AnimCharacter item) {
    ArgumentNullException.ThrowIfNull(item);
    int index = _items.IndexOf(item);
    if (index >= 0) {
      _items.RemoveAt(index);
    }
  }
}
