using System.Linq;
using System.Runtime.CompilerServices;
using AnimLib.States;
using AnimLib.Utilities;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;

namespace AnimLib.UI.Debug;

/// <summary>
/// <see cref="UIState"/> which supports appending lines of stack-allocated text.
/// </summary>
public abstract class DebugUIState : DraggablePanel {
  private const int MaxStackallocSize = 128;

  protected DebugUIState() {
    Width.Set(0, 1);
    Height.Set(30, 0);
  }

  protected UIElement? Header { get; private set; }

  /// <summary>
  /// Horizontal size of the "key" portion of a text.
  /// </summary>
  protected virtual int XOffset => 200;

  /// <summary>
  /// Vertical size of a line of text
  /// </summary>
  protected virtual int YOffset => 18;

  /// <summary>
  /// Horizontal size of all text that represents one "Character" info
  /// </summary>
  protected virtual int XSize => 360;

  /// <summary>
  /// Determines whether the selected element may change in the UI if it becomes inactive.
  /// If <see langword="true"/>, the element will not be changed if it goes inactive.
  /// If <see langword="false"/>, the element will be replaced with the active sibling.
  /// </summary>
  public bool Locked { get; internal set; }


  public Color Color = Color.White;
  protected Vector2 TextPosition = Vector2.Zero;
  private SpriteBatch? _spriteBatch;

  public static Color Green => new(0, 1f, 0);
  public static Color Red => new(1f, 0.2f, 0.2f);

  protected void AddHeader(string value) {
    Header = new UIText(value, 1.2f) {
      HAlign = 0.5f
    };
    Header.Top.Set(0, 0);
    Append(Header);
  }

  #region DrawAppend Methods

  /// <summary>
  /// Draws the specified boolean value, color either the specified color, or green if <paramref name="value"/> is <see langword="true"/>, red if <see langword="false"/>.
  /// The label, if not specified, will be the argument expression for <paramref name="value"/>.
  /// </summary>
  /// <param name="value">The boolean value to draw.</param>
  /// <param name="key">The label to draw.</param>
  /// <param name="color">The color for the text. If null, the color will be green if <paramref name="value"/> is <see langword="true"/>, red if <see langword="false"/>.</param>
  public void DrawAppendBoolean(bool value, [CallerArgumentExpression(nameof(value))] string key = default!,
    Color? color = null) {
    DrawAppendLabelValue(key, value.ToString().AsSpan(), color ?? (value ? Green : Red));
  }

  /// <summary>
  /// Draws the specified text, as a label-value pair, and increases the Y position by <see cref="YOffset"/>.
  /// The drawn value will be displayed as a fraction, where the numerator is the value and the denominator is the max value.
  /// </summary>
  /// <param name="label">The label to draw.</param>
  /// <param name="value">The value to draw.</param>
  /// <param name="max">The expected maximum value of <paramref name="value"/>.</param>
  /// <param name="color">The color for the text.</param>
  /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format.</param>
  /// <param name="provider">An optional object that supplies culture-specific formatting information.</param>
  /// <typeparam name="T">Type that implements <see cref="ISpanFormattable"/>, where <see cref="ISpanFormattable.TryFormat"/> is used for the value.</typeparam>
  public void DrawAppendLabelValue<T>(string label, T value, T max, Color? color = null,
    ReadOnlySpan<char> format = default, IFormatProvider? provider = null) where T : ISpanFormattable {
    StackString str = new(stackalloc char[MaxStackallocSize]);
    str.Append(value, format, provider);
    str.Append(" / ");
    str.Append(max, format, provider);
    DrawAppendLabelValue(label, !str.ExceededCapacity ? str.AsReadOnlySpan() : "...".AsSpan(), color);
  }

  /// <summary>
  /// Draws the specified text, as a label-value pair, and increases the Y position by <see cref="YOffset"/>.
  /// The label, if not specified, will be the argument expression for <paramref name="value"/>.
  /// </summary>
  /// <param name="value">The value to draw.</param>
  /// <param name="label">The label to draw. If not specified, is equal to the argument expression for <paramref name="value"/>.</param>
  /// <param name="color">The color for the text.</param>
  /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format.</param>
  /// <param name="provider">An optional object that supplies culture-specific formatting information.</param>
  /// <typeparam name="T">Type that implements <see cref="ISpanFormattable"/>, where <see cref="ISpanFormattable.TryFormat"/> is used for the value.</typeparam>
  public void DrawAppendLabelValue<T>(T value, [CallerArgumentExpression(nameof(value))] string label = default!,
    Color? color = null, ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    where T : ISpanFormattable =>
    DrawAppendLabelValue(label, value, color, format, provider);

  /// <summary>
  /// Draws the specified text, as a label-value pair, and increases the Y position by <see cref="YOffset"/>.
  /// </summary>
  /// <param name="label">The label to draw.</param>
  /// <param name="value">The value to draw.</param>
  /// <param name="color">The color for the text.</param>
  /// <param name="format">A span containing the characters that represent a standard or custom format string that defines the acceptable format.</param>
  /// <param name="provider">An optional object that supplies culture-specific formatting information.</param>
  /// <typeparam name="T">Type that implements <see cref="ISpanFormattable"/>, where <see cref="ISpanFormattable.TryFormat"/> is used for the value.</typeparam>
  public void DrawAppendLabelValue<T>(string label, T value, Color? color = null,
    ReadOnlySpan<char> format = default, IFormatProvider? provider = null)
    where T : ISpanFormattable {
    Span<char> buffer = stackalloc char[MaxStackallocSize];
    var result = value.TryFormat(buffer, out int charsWritten, format, provider)
      ? buffer[..charsWritten]
      : "...".AsSpan();
    DrawAppendLabelValue(label, result, color);
  }

  /// <summary>
  /// Draws the specified text, as a label-value pair, and increases the Y position by <see cref="YOffset"/>.
  /// If the value is <see langword="null"/>, "null" will be drawn instead.
  /// </summary>
  /// <param name="label">The label to draw.</param>
  /// <param name="value">The value to draw.</param>
  /// <param name="color">The color for the text.</param>
  public void DrawAppendLabelValue(string label, string? value, Color? color = null) =>
    DrawAppendLabelValue(label, (value ?? "null").AsSpan(), color);

  /// <summary>
  /// Draws the specified text, as a label-value pair, and increases the Y position by <see cref="YOffset"/>.
  /// </summary>
  /// <param name="label">The label to draw.</param>
  /// <param name="value">The value to draw.</param>
  /// <param name="color">The color for the text.</param>
  private void DrawAppendLabelValue(string label, ReadOnlySpan<char> value, Color? color = null) {
    if (_spriteBatch is null) {
      throw new InvalidOperationException("Method must be called during Draw.");
    }

    color ??= Color;
    _spriteBatch.DrawString(FontAssets.MouseText.Value, (ReadOnlySpan<char>)label, TextPosition, color.Value);
    TextPosition.X += XOffset;
    _spriteBatch.DrawString(FontAssets.MouseText.Value, value, TextPosition, color.Value);
    TextPosition.X -= XOffset;
    TextPosition.Y += YOffset;
  }

  /// <summary>
  /// Draws the specified text, and increases the Y position by <see cref="YOffset"/>.
  /// </summary>
  /// <param name="text">The line of text to draw.</param>
  /// <param name="color">The color for the text.</param>
  /// <exception cref="InvalidOperationException"></exception>
  public void DrawAppendLine(ReadOnlySpan<char> text, Color? color = null) {
    if (_spriteBatch is null) {
      throw new InvalidOperationException("Method must be called during Draw.");
    }

    _spriteBatch.DrawString(FontAssets.MouseText.Value, text, TextPosition, color ?? Color);
    TextPosition.Y += YOffset;
  }

  protected void DrawText(ReadOnlySpan<char> value, Color? color = null) {
    if (_spriteBatch is null) {
      throw new InvalidOperationException("Method must be called during Draw.");
    }

    color ??= Color;
    _spriteBatch.DrawString(FontAssets.MouseText.Value, value, TextPosition, color.Value);
  }

  protected void DrawText<T>(T value, Color? color = null, ReadOnlySpan<char> format = default,
    IFormatProvider? provider = null) where T : ISpanFormattable {
    Span<char> buffer = stackalloc char[MaxStackallocSize];
    var result = value.TryFormat(buffer, out int charsWritten, format, provider)
      ? buffer[..charsWritten]
      : "...".AsSpan();
    DrawText(result, color);
  }

  #endregion

  public override void Draw(SpriteBatch spriteBatch) {
    TextPosition = GetInnerDimensions().Position() + new Vector2(Parent.PaddingLeft, Parent.PaddingTop);
    TextPosition += new Vector2(0, 30);
    _spriteBatch = spriteBatch;
    base.Draw(spriteBatch);
    _spriteBatch = null;
  }

  public IndentScope Indent(int indent = 10) => new(this, indent);

  public readonly ref struct IndentScope {
    private readonly DebugUIState _state;
    private readonly int _indent;

    public IndentScope(DebugUIState state, int indent) {
      _state = state;
      _indent = indent;
      _state.TextPosition.X += _indent;
    }

    public void Dispose() {
      _state.TextPosition.X -= _indent;
    }
  }
}

public abstract class DebugUIState<TState> : DebugUIState where TState : State {
  protected TState? State { get; private set; }

  public void SetState(TState? element) {
    if (Locked) {
      return;
    }

    State = element;
    OnSetState(element);
  }

  protected virtual void OnSetState(TState? element) {
  }

  public override void Update(GameTime gameTime) {
    if (!Locked && State is { IsActive: false }) {
      State = State.Parent?.ActiveChildren.FirstOrDefault() as TState;
    }

    base.Update(gameTime);
  }
}
