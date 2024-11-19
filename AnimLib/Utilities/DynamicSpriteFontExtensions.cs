using System.Collections;
using System.Reflection;
using ReLogic.Graphics;

namespace AnimLib.Utilities;

/// <summary>
/// This file exists to make use of
/// <see cref="DynamicSpriteFontExtensionMethods.DrawString(SpriteBatch,DynamicSpriteFont,string,Vector2,Color)"/>
/// without allocating strings, by allowing usage of a <see cref="ReadOnlySpan{T}"/> of chars.
/// </summary>
public static class DynamicSpriteFontExtensions {
  /// <summary>
  /// Draws text to the screen, where the text is represented by a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/>s.
  /// </summary>
  /// <param name="spriteBatch"><see cref="SpriteBatch"/> to draw to.</param>
  /// <param name="spriteFont">The font for the text.</param>
  /// <param name="text">The text to draw.</param>
  /// <param name="position">The screen position to draw at.</param>
  /// <param name="color">The color of the text.</param>
  public static void DrawString(this SpriteBatch spriteBatch, DynamicSpriteFont spriteFont, ReadOnlySpan<char> text,
    Vector2 position, Color color) {
    // TODO:
    //   Avoid ToString, use span directly to avoid allocation
    //   Requires PR in the tML repo, or unacceptably, copying decompiled code into mod code *and*
    //   a bunch of reflection 
    DynamicSpriteFontExtensionMethods.DrawString(spriteBatch, spriteFont, text.ToString(), position, color);
  }
}

