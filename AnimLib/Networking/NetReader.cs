using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace AnimLib.Networking;

/// <summary>
/// Default implementation of <see cref="IReadSync"/>.
/// </summary>
/// <param name="reader"></param>
public readonly struct NetReader(BinaryReader reader) : IReadSync {
  private BinaryReader Reader { get; } = reader;
  BinaryReader IReadSync.Reader => Reader;

  bool ISync.Reading => true;
  void ISync.Sync(ref bool value) => value = Reader.ReadBoolean();

  void ISync.Sync(ref bool b1, ref bool b2, ref bool b3, ref bool b4, ref bool b5, ref bool b6, ref bool b7,
    ref bool b8) {
    BitsByte bb = Reader.ReadByte();
    bb.Retrieve(ref b1, ref b2, ref b3, ref b4, ref b5, ref b6, ref b7, ref b8);
  }

  void ISync.Sync(ref byte value) => value = Reader.ReadByte();

  void ISync.Sync(ref sbyte value) => value = Reader.ReadSByte();

  void ISync.Sync(ref byte[] buffer) {
    byte len = Reader.ReadByte();
    if (len > buffer.Length) {
      buffer = Reader.ReadBytes(len);
      return;
    }

    for (int i = 0; i < len; i++) {
      buffer[i] = Reader.ReadByte();
    }

    if (len == buffer.Length) {
      return;
    }

    for (int i = len; i < buffer.Length; i++) {
      buffer[i] = 0;
    }
  }

  void ISync.Sync(ref char ch) => ch = Reader.ReadChar();

  void ISync.Sync(ref char[] buffer) {
    byte len = Reader.ReadByte();
    if (len > buffer.Length) {
      buffer = Reader.ReadChars(len);
      return;
    }

    for (int i = 0; i < len; i++) {
      buffer[i] = Reader.ReadChar();
    }

    if (len == buffer.Length) {
      return;
    }

    for (int i = len; i < buffer.Length; i++) {
      buffer[i] = (char)0;
    }
  }

  void ISync.Sync(ref short value) => value = Reader.ReadInt16();
  void ISync.Sync(ref ushort value) => value = Reader.ReadUInt16();

  void ISync.Sync(ref int value) => value = Reader.ReadInt32();

  void ISync.Sync(ref uint value) => value = Reader.ReadUInt32();

  void ISync.Sync(ref long value) => value = Reader.ReadInt64();

  void ISync.Sync(ref ulong value) => value = Reader.ReadUInt64();

  void ISync.Sync(ref double value) => value = Reader.ReadDouble();

  void ISync.Sync(ref decimal value) => value = Reader.ReadDecimal();

  void ISync.Sync(ref float value) => value = Reader.ReadSingle();

  void ISync.Sync(ref Half value) => value = Reader.ReadHalf();

  void ISync.Sync(ref string value) => value = Reader.ReadString();
  void ISync.Sync(Func<bool> write, Action<bool> read) => read(Reader.ReadBoolean());

  void ISync.Sync(Func<byte> write, Action<byte> read) => read(Reader.ReadByte());

  void ISync.Sync(Func<sbyte> write, Action<sbyte> read) => read(Reader.ReadSByte());

  void ISync.Sync(Func<char> write, Action<char> read) => read(Reader.ReadChar());

  void ISync.Sync(Func<short> write, Action<short> read) => read(Reader.ReadInt16());

  void ISync.Sync(Func<ushort> write, Action<ushort> read) => read(Reader.ReadUInt16());
  void ISync.Sync(Func<int> write, Action<int> read) => read(Reader.ReadInt32());

  void ISync.Sync(Func<uint> write, Action<uint> read) => read(Reader.ReadUInt32());

  void ISync.Sync(Func<long> write, Action<long> read) => read(Reader.ReadInt64());

  void ISync.Sync(Func<ulong> write, Action<ulong> read) => read(Reader.ReadUInt64());

  void ISync.Sync(Func<double> write, Action<double> read) => read(Reader.ReadDouble());

  void ISync.Sync(Func<decimal> write, Action<decimal> read) => read(Reader.ReadDecimal());

  void ISync.Sync(Func<float> write, Action<float> read) => read(Reader.ReadSingle());

  void ISync.Sync(Func<Half> write, Action<Half> read) => read(Reader.ReadHalf());

  void ISync.Sync(Func<string> write, Action<string> read) => read(Reader.ReadString());

  void ISync.Sync(ref Vector2 value) => value = Reader.ReadVector2();
  void ISync.SyncSign(ref int value) => value = Reader.ReadSByte();

  void ISync.Sync7BitEncodedInt(ref int value) => value = Reader.Read7BitEncodedInt();

  void ISync.Sync7BitEncodedInt64(ref long value) => value = Reader.Read7BitEncodedInt64();

  public void SyncSmallestCast(ref uint value, uint netMaxValue) {
    value = netMaxValue switch {
      <= byte.MaxValue => Reader.ReadByte(),
      <= ushort.MaxValue >> 2 => (uint)Reader.Read7BitEncodedInt(),
      <= ushort.MaxValue => Reader.ReadUInt16(),
      <= uint.MaxValue >> 4 => (uint)Reader.Read7BitEncodedInt(),
      _ => Reader.ReadUInt32()
    };
  }

  void ISync.SyncFunc<TOwner>(TOwner owner, Action<TOwner, BinaryWriter> writeFunc,
    Action<TOwner, BinaryReader> readFunc) => readFunc(owner, Reader);

  void ISync.SyncPositionAndVelocity(Entity entity) {
    entity.position = Reader.ReadVector2();
    entity.velocity = Reader.ReadVector2();
  }

  void ISync.SyncNullable<T>(ref T? value, Action<BinaryWriter, T> onWriteNotNull, Func<BinaryReader, T> onReadNotNull,
    Action? onReadNull) where T : default {
    bool isNotNull = Reader.ReadBoolean();
    if (isNotNull) {
      value = onReadNotNull(Reader);
    }
    else {
      value = default;
      onReadNull?.Invoke();
    }
  }

  void ISync.SyncNullable<TOwner, TValue>(TOwner owner, ref TValue? value,
    Action<BinaryWriter, TValue> onWriteNotNull,
    Action<BinaryReader, TOwner> onReadNotNull,
    Action<TOwner>? onReadNull) where TValue : default {
    bool isNotNull = Reader.ReadBoolean();
    if (isNotNull) {
      onReadNotNull(Reader, owner);
    }
    else {
      value = default;
      onReadNull?.Invoke(owner);
    }
  }

  public void SyncEntity(ref Entity? entity) {
    byte type = Reader.ReadByte();
    entity = type switch {
      ISync.Null => null,
      ISync.PlayerType => Main.player[Reader.Read7BitEncodedInt()],
      ISync.NpcType => Main.npc[Reader.Read7BitEncodedInt()],
      ISync.ProjectileType => FindProjectile(Reader),
      ISync.ItemType => Main.item[Reader.Read7BitEncodedInt()],
      _ => null
    };
    return;

    Projectile? FindProjectile(BinaryReader r) {
      ushort identity = r.ReadUInt16();
      return Main.projectile.FirstOrDefault(p => p.identity == identity);
    }
  }

  IEnumerable<TValue> ISync.SyncEnumerate<TOwner, TValue>(TOwner owner,
    Func<TOwner, int> count,
    Func<TOwner, IEnumerable<TValue>> onWrite,
    Action<BinaryWriter, TValue> writeFunc,
    Func<TOwner, BinaryReader, TValue> readFunc) {
    int loopCount = Reader.Read7BitEncodedInt();
    for (int i = 0; i < loopCount; i++) {
      yield return readFunc(owner, Reader);
    }
  }
}

/// <summary>
/// Default implementation of <see cref="IWriteSync"/>.
/// </summary>
/// <param name="writer"></param>
public readonly struct NetWriter(BinaryWriter writer) : IWriteSync {
  private BinaryWriter Writer { get; } = writer;
  BinaryWriter IWriteSync.Writer => Writer;

  bool ISync.Reading => false;
  void ISync.Sync(ref bool value) => Writer.Write(value);

  void ISync.Sync(ref bool b1, ref bool b2, ref bool b3, ref bool b4, ref bool b5, ref bool b6, ref bool b7,
    ref bool b8) {
    BitsByte bb = new(b1, b2, b3, b4, b5, b6, b7, b8);
    Writer.Write(bb);
  }

  void ISync.Sync(ref byte value) => Writer.Write(value);
  void ISync.Sync(ref sbyte value) => Writer.Write(value);

  void ISync.Sync(ref byte[] buffer) =>
    Writer.Write(buffer, 0, (ushort)(buffer.Length < ushort.MaxValue ? buffer.Length : ushort.MaxValue));

  void ISync.Sync(ref char ch) => Writer.Write(ch);

  void ISync.Sync(ref char[] buffer) =>
    Writer.Write(buffer, 0, (ushort)(buffer.Length < ushort.MaxValue ? buffer.Length : ushort.MaxValue));

  void ISync.Sync(ref short value) => Writer.Write(value);
  void ISync.Sync(ref ushort value) => Writer.Write(value);
  void ISync.Sync(ref int value) => Writer.Write(value);
  void ISync.Sync(ref uint value) => Writer.Write(value);
  void ISync.Sync(ref long value) => Writer.Write(value);
  void ISync.Sync(ref ulong value) => Writer.Write(value);
  void ISync.Sync(ref double value) => Writer.Write(value);
  void ISync.Sync(ref decimal value) => Writer.Write(value);
  void ISync.Sync(ref float value) => Writer.Write(value);
  void ISync.Sync(ref Half value) => Writer.Write(value);
  void ISync.Sync(ref string value) => Writer.Write(value);
  void ISync.Sync(Func<bool> write, Action<bool> read) => Writer.Write(write());
  void ISync.Sync(Func<byte> write, Action<byte> read) => Writer.Write(write());
  void ISync.Sync(Func<sbyte> write, Action<sbyte> read) => Writer.Write(write());
  void ISync.Sync(Func<char> write, Action<char> read) => Writer.Write(write());
  void ISync.Sync(Func<short> write, Action<short> read) => Writer.Write(write());
  void ISync.Sync(Func<ushort> write, Action<ushort> read) => Writer.Write(write());
  void ISync.Sync(Func<int> write, Action<int> read) => Writer.Write(write());
  void ISync.Sync(Func<uint> write, Action<uint> read) => Writer.Write(write());
  void ISync.Sync(Func<long> write, Action<long> read) => Writer.Write(write());
  void ISync.Sync(Func<ulong> write, Action<ulong> read) => Writer.Write(write());
  void ISync.Sync(Func<double> write, Action<double> read) => Writer.Write(write());
  void ISync.Sync(Func<decimal> write, Action<decimal> read) => Writer.Write(write());
  void ISync.Sync(Func<float> write, Action<float> read) => Writer.Write(write());
  void ISync.Sync(Func<Half> write, Action<Half> read) => Writer.Write(write());
  void ISync.Sync(Func<string> write, Action<string> read) => Writer.Write(write());

  void ISync.Sync(ref Vector2 value) => Writer.WriteVector2(value);
  void ISync.SyncSign(ref int value) => Writer.Write((sbyte)(value > 0 ? 1 : value < 0 ? -1 : 0));

  void ISync.Sync7BitEncodedInt(ref int value) => Writer.Write7BitEncodedInt(value);

  void ISync.Sync7BitEncodedInt64(ref long value) => Writer.Write7BitEncodedInt64(value);

  public void SyncSmallestCast(ref uint value, uint netMaxValue) {
    switch (netMaxValue) {
      case <= 0xFF:
        // Value always 1 byte long
        Writer.Write((byte)value);
        break;
      case <= 0x4000:
        // Value always either 1 or 2 bytes long
        Writer.Write7BitEncodedInt((int)value);
        break;
      case <= 0xFFFF:
        // Value always 2 bytes long
        Writer.Write((ushort)value);
        break;
      case <= 0x10000000:
        // Value always between 1 and 4
        Writer.Write7BitEncodedInt((int)value);
        break;
      default:
        // Value always 4 bytes long
        Writer.Write(value);
        break;
    }
  }

  void ISync.SyncFunc<TOwner>(TOwner owner,
    Action<TOwner, BinaryWriter> writeFunc,
    Action<TOwner, BinaryReader> readFunc) => writeFunc(owner, Writer);

  void ISync.SyncPositionAndVelocity(Entity entity) {
    Writer.WriteVector2(entity.position);
    Writer.WriteVector2(entity.velocity);
  }

  void ISync.SyncNullable<T>(ref T? value, Action<BinaryWriter, T> onWriteNotNull, Func<BinaryReader, T> onReadNotNull,
    Action? onReadNull) where T : default {
    Writer.Write(value is not null);
    if (value is not null) {
      onWriteNotNull(Writer, (T)value);
    }
  }

  void ISync.SyncNullable<TOwner, TValue>(TOwner owner, ref TValue? value,
    Action<BinaryWriter, TValue> onWriteNotNull,
    Action<BinaryReader, TOwner> onReadNotNull,
    Action<TOwner>? onReadNull) where TValue : default {
    Writer.Write(value is not null);
    if (value is not null) {
      onWriteNotNull(Writer, value);
    }
  }

  public void SyncEntity(ref Entity? entity) {
    switch (entity) {
      case null:
        Writer.Write(ISync.Null);
        break;
      case Player:
        Writer.Write(ISync.PlayerType);
        Writer.Write7BitEncodedInt(entity.whoAmI);
        break;
      case NPC:
        Writer.Write(ISync.NpcType);
        Writer.Write7BitEncodedInt(entity.whoAmI);
        break;
      case Item:
        Writer.Write(ISync.ItemType);
        Writer.Write7BitEncodedInt(entity.whoAmI);
        break;
      case Projectile projectile:
        Writer.Write(ISync.ProjectileType);
        Writer.Write(projectile.identity);
        break;
    }
  }

  IEnumerable<TValue> ISync.SyncEnumerate<TOwner, TValue>(TOwner owner,
    Func<TOwner, int> count,
    Func<TOwner, IEnumerable<TValue>> onWrite,
    Action<BinaryWriter, TValue> writeFunc,
    Func<TOwner, BinaryReader, TValue> readFunc) {
    Writer.Write7BitEncodedInt(count(owner));
    foreach (TValue t in onWrite(owner)) {
      writeFunc(Writer, t);
      yield return t;
    }
  }
}

/// <summary>
/// <see cref="ISync"/> used during Reading.
/// </summary>
public interface IReadSync : ISync {
  bool ISync.Reading => true;

  public BinaryReader Reader { get; }
}

/// <summary>
/// <see cref="ISync"/> used during Writing.
/// </summary>
public interface IWriteSync : ISync {
  bool ISync.Reading => false;

  public BinaryWriter Writer { get; }
}

/// <summary>
/// Interface for <see cref="IReadSync"/> and <see cref="IWriteSync"/>.
/// This enables reading and writing functionality to occur in the same method.
/// </summary>
[PublicAPI]
public interface ISync {
  private protected const byte Null = 0;
  private protected const byte PlayerType = 1;
  private protected const byte NpcType = 2;
  private protected const byte ItemType = 3;
  private protected const byte ProjectileType = 4;
  /// <summary>
  /// Whether the sync method is currently receiving a <see cref="ModPacket"/>,
  /// and should make changes to the client.
  /// </summary>
  bool Reading { get; }

  /// <summary>
  /// Whether the sync method is currently writing to a <see cref="ModPacket"/>.
  /// </summary>
  bool Writing => !Reading;

  void Sync(ref bool value);

  void Sync(ref bool b1, ref bool b2) =>
    Sync(ref b1, ref b2, ref _null, ref _null, ref _null, ref _null, ref _null, ref _null);

  void Sync(ref bool b1, ref bool b2, ref bool b3) =>
    Sync(ref b1, ref b2, ref b3, ref _null, ref _null, ref _null, ref _null, ref _null);

  void Sync(ref bool b1, ref bool b2, ref bool b3, ref bool b4) =>
    Sync(ref b1, ref b2, ref b3, ref b4, ref _null, ref _null, ref _null, ref _null);

  void Sync(ref bool b1, ref bool b2, ref bool b3, ref bool b4, ref bool b5) =>
    Sync(ref b1, ref b2, ref b3, ref b4, ref b5, ref _null, ref _null, ref _null);

  void Sync(ref bool b1, ref bool b2, ref bool b3, ref bool b4, ref bool b5, ref bool b6) =>
    Sync(ref b1, ref b2, ref b3, ref b4, ref b5, ref b6, ref _null, ref _null);

  void Sync(ref bool b1, ref bool b2, ref bool b3, ref bool b4, ref bool b5, ref bool b6, ref bool b7) =>
    Sync(ref b1, ref b2, ref b3, ref b4, ref b5, ref b6, ref b7, ref _null);

  void Sync(ref bool b1, ref bool b2, ref bool b3, ref bool b4, ref bool b5, ref bool b6, ref bool b7, ref bool b8);

  void Sync(ref byte value);

  void Sync(ref sbyte value);

  void Sync(ref byte[] buffer);

  void Sync(ref char ch);

  void Sync(ref char[] buffer);

  void Sync(ref short value);

  void Sync(ref ushort value);

  void Sync(ref int value);

  void Sync(ref uint value);

  void Sync(ref long value);

  void Sync(ref ulong value);

  void Sync(ref double value);

  void Sync(ref decimal value);

  void Sync(ref float value);

  void Sync(ref Half value);

  void Sync(ref string value);

  void Sync(Func<bool> write, Action<bool> read);

  void Sync(Func<byte> write, Action<byte> read);

  void Sync(Func<sbyte> write, Action<sbyte> read);

  void Sync(Func<char> write, Action<char> read);

  void Sync(Func<short> write, Action<short> read);

  void Sync(Func<ushort> write, Action<ushort> read);

  void Sync(Func<int> write, Action<int> read);

  void Sync(Func<uint> write, Action<uint> read);

  void Sync(Func<long> write, Action<long> read);

  void Sync(Func<ulong> write, Action<ulong> read);

  void Sync(Func<double> write, Action<double> read);

  void Sync(Func<decimal> write, Action<decimal> read);

  void Sync(Func<float> write, Action<float> read);

  void Sync(Func<Half> write, Action<Half> read);

  void Sync(Func<string> write, Action<string> read);

  void Sync(ref Vector2 value);

  /// <summary>
  /// Sync the specified value as a byte representing the sign of the value.
  /// </summary>
  /// <param name="value"></param>
  void SyncSign(ref int value);

  void Sync7BitEncodedInt(ref int value);
  void Sync7BitEncodedInt64(ref long value);
  void SyncSmallestCast(ref uint value, uint netMaxValue);

  /// <summary>
  /// Sync arbitrary data
  /// </summary>
  /// <param name="owner">
  /// The owner of the data, used as argument for <paramref name="readFunc"/>/<paramref name="writeFunc"/>
  /// </param>
  /// <param name="writeFunc">Function called when writing. Ignored when reading.</param>
  /// <param name="readFunc">Function called when reading. Ignored when writing.</param>
  /// <typeparam name="TOwner">The type of object which owns the data.</typeparam>
  void SyncFunc<TOwner>(TOwner owner,
    Action<TOwner, BinaryWriter> writeFunc,
    Action<TOwner, BinaryReader> readFunc)
    where TOwner : class;

  void SyncPositionAndVelocity(Entity entity);

  /// <summary>
  /// Sync a value which is expected to be null.
  /// </summary>
  /// <param name="value">Value to check for null.</param>
  /// <param name="onWriteNotNull">Function to write if outgoing value is not null.</param>
  /// <param name="onReadNotNull">Function to read if incoming value is not null.</param>
  /// <param name="onReadNull">Function to read if incoming value is null.</param>
  /// <typeparam name="T"></typeparam>
  void SyncNullable<T>(ref T? value,
    Action<BinaryWriter, T> onWriteNotNull,
    Func<BinaryReader, T> onReadNotNull,
    Action? onReadNull = null);

  /// <summary>
  /// Sync a value which is expected to be null some of the time.
  /// This overload does not assign the result of <paramref name="onReadNotNull"/> to <paramref name="value"/>.
  /// </summary>
  /// <param name="owner">To be called</param>
  /// <param name="value">Value to check for null.</param>
  /// <param name="onWriteNotNull">Function to write if outgoing value is not null.</param>
  /// <param name="onReadNotNull">Function to read if incoming value is not null.</param>
  /// <param name="onReadNull">Function to read if incoming value is null.</param>
  /// <typeparam name="TOwner">Should always be "<see langword="this"/>"</typeparam>
  /// <typeparam name="TValue">Value to be synced</typeparam>
  /// <remarks>
  /// <typeparamref name="TOwner"/>/<paramref name="owner"/> exists to avoid closures.
  /// </remarks>
  void SyncNullable<TOwner, TValue>(TOwner owner, ref TValue? value,
    Action<BinaryWriter, TValue> onWriteNotNull,
    Action<BinaryReader, TOwner> onReadNotNull,
    Action<TOwner>? onReadNull = null)
    where TOwner : class;

  /// <summary>
  /// Syncs the reference to the specified <see cref="Entity"/>.
  /// </summary>
  /// <param name="entity">
  /// A <see cref="Player"/>, <see cref="NPC"/>, <see cref="Projectile"/>, <see cref="Item"/>, or <see langword="null"/>.
  /// </param>
  void SyncEntity(ref Entity? entity);

  /// <summary>
  /// Syncs an enumeration of values.
  /// </summary>
  /// <param name="owner">
  /// The object which owns the enumeration.
  /// This is used as an argument for <paramref name="count"/>, <paramref name="onWrite"/>, and <paramref name="readFunc"/>.
  /// </param>
  /// <param name="count">
  /// Function to get number of times to enumerate for.
  /// Used only when writing, ignored when reading.
  /// </param>
  /// <param name="onWrite">
  /// <see cref="IEnumerable{T}"/> to loop through for when writing.
  /// Used only when writing, ignored when reading.
  /// </param>
  /// <param name="writeFunc">
  /// Function to write for identifying the IEnumerable variable.
  /// Used only when writing, ignored when reading.
  /// </param>
  /// <param name="readFunc">
  /// Function to read to get the enumerated variable of type <typeparamref name="TValue"/>.
  /// Used only when reading, ignored while writing.
  /// </param>
  /// <typeparam name="TOwner">The type of object which owns the enumeration.</typeparam>
  /// <typeparam name="TValue">The type of enumerated value.</typeparam>
  /// <returns></returns>
  /// <remarks>
  /// This method assumes no need to enumerate more than 255 times on type <typeparamref name="TValue"/>.
  /// For syncing a byte array, use <see cref="Sync(ref byte[])"/>
  /// <para />
  /// <typeparamref name="TOwner"/>/<paramref name="owner"/> exists to avoid closures.
  /// </remarks>
  IEnumerable<TValue> SyncEnumerate<TOwner, TValue>(TOwner owner,
    Func<TOwner, int> count,
    Func<TOwner, IEnumerable<TValue>> onWrite,
    Action<BinaryWriter, TValue> writeFunc,
    Func<TOwner, BinaryReader, TValue> readFunc)
    where TOwner : class;

  private static bool _null;
}
