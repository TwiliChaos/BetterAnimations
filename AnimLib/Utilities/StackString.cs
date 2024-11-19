using System.Runtime.CompilerServices;

namespace AnimLib.Utilities;

/// <summary>
/// Ref struct that behaves like a list of <see cref="Char"/>, and supports appending from <see cref="ISpanFormattable"/>.
/// </summary>
/// <remarks>
/// This behaves like a list of chars, but does not auto expand.
/// As such, any chars written to this data structure which exceeds <see cref="Capacity"/> will be discarded.
/// </remarks>
public ref struct StackString(Span<char> span) {
  private Span<char> _span = span;

  /// <summary>
  /// Whether there was an attempt to write <see cref="Char"/> to this instance that exceeded capacity.
  /// Any further attempts to append to this instance will do nothing, even if there would be space in the
  /// backing span to support it.
  /// </summary>
  public bool ExceededCapacity { get; private set; }

  /// <summary>
  /// Amount of <see cref="Char"/>s written to this instance.
  /// </summary>
  public int Count {
    get;
    private set {
      if (value > _span.Length - 1) {
        value = _span.Length - 1;
      }

      field = value;
    }
  } = 0;

  /// <summary>
  /// Get the length of the backing <see cref="Span{T}"/>.
  /// </summary>
  public int Capacity {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    get => _span.Length;
  }

  /// <summary>
  /// Get the <see cref="Char"/> at the specified index.
  /// </summary>
  /// <param name="index"></param>
  public ref char this[int index] {
    get {
      ArgumentOutOfRangeException.ThrowIfNegative(index);
      ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
      return ref _span[index];
    }
  }

  /// <summary>
  /// Gets a <see cref="Span{T}"/> representing the <see cref="Char"/>s which were written.
  /// The size of the returned <see cref="Span{T}"/> is equal to <see cref="Count"/>.
  /// </summary>
  /// <returns></returns>
  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public Span<char> AsSpan() => _span[..Count];

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ReadOnlySpan<char> AsReadOnlySpan() => AsSpan();

  [MethodImpl(MethodImplOptions.AggressiveInlining)]
  public ReadOnlySpan<char>.Enumerator GetEnumerator() => AsReadOnlySpan().GetEnumerator();

  /// <summary>
  /// Appends the result of <see cref="ISpanFormattable.TryFormat"/> to this <see cref="StackString"/>.
  /// If the specified <paramref name="value"/> is <see langword="null"/>, this will append the string "null".
  /// If <see cref="ExceededCapacity"/> is <see langword="true"/>, this value is discarded.
  /// </summary>
  /// <param name="value">The value to append to this.</param>
  /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format.</param>
  /// <param name="provider">An optional object that supplies culture-specific formatting information.</param>
  /// <typeparam name="T">Type of <paramref name="value"/> which inherits from <see cref="ISpanFormattable"/>.</typeparam>
  public void Append<T>(T? value, ReadOnlySpan<char> format = default, IFormatProvider? provider = default) where T : ISpanFormattable {
    if (ExceededCapacity) {
      return;
    }

    if (value is null) {
      Append("null");
      return;
    }

    ExceededCapacity = !value.TryFormat(_span[Count..], out int charsWritten, format, provider);
    Count += charsWritten;
  }

  /// <summary>
  /// Appends a span of <see cref="Char"/>s to this <see cref="StackString"/>.
  /// If <see cref="ExceededCapacity"/> is <see langword="true"/>, this value is discarded.
  /// </summary>
  /// <param name="value">The chars to append to this.</param>
  public void Append(ReadOnlySpan<char> value) {
    if (ExceededCapacity) {
      return;
    }

    ExceededCapacity = !value.TryCopyTo(_span[Count..]);
    if (!ExceededCapacity) {
      Count += value.Length;
    }
  }

  /// <summary>
  /// Sets <see cref="Count"/> to 0 and <see cref="ExceededCapacity"/> to <see langword="false"/>
  /// </summary>
  public void Clear() {
    // Do we need to use Span.Clear() here? This is a span that does not contain reference types.
    Count = 0;
    ExceededCapacity = false;
  }

  public static implicit operator ReadOnlySpan<char>(StackString value) => value.AsReadOnlySpan();
}
