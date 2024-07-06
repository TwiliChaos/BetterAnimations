using System.IO;
using System.Threading;
using AsepriteDotNet;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;
using AsepriteDotNet.Processors;
using ReLogic.Content.Readers;
using Texture = AsepriteDotNet.Texture;

namespace AnimLib.Aseprite;

/// <summary>
/// AssetReader to read an Aseprite file (*.ase/*.aseprite) into a format usable by Terraria.
/// This Reader uses AsepriteDotNet to load the file into an easily understandable format.
/// This class returns an <see cref="AseAsset"/>,
/// with fields representing the file, sprite sheet data, and Texture2D of the sprite sheet.
/// https://github.com/AristurtleDev/AsepriteDotNet
/// </summary>
public class AseReader : IAssetReader {
  public T FromStream<T>(Stream stream) where T : class {
    if (typeof(T) != typeof(AseAsset)) {
      throw AssetLoadException.FromInvalidReader<AseReader, T>();
    }

    // AsepriteFileLoader requires the Seek function.
    // We cannot guarantee that the incoming stream supports Seeking (e.g. DeflateStream),
    // So we have to create a new Stream that allows it.
    MemoryStream newStream = new();
    stream.CopyTo(newStream);
    newStream.Seek(0, SeekOrigin.Begin);

    // We have no access to the file name to name this object.
    AsepriteFile asepriteFile = AsepriteFileLoader.FromStream("", newStream);

    // We have no access to the file name or the mod, so these warnings may just be annoying.
    var warnings = asepriteFile.Warnings;
    if (!warnings.IsEmpty) {
      AnimLibMod.Instance.Logger.Warn($"Aseprite file loaded with the following warnings:");
      foreach (string warning in warnings) {
        AnimLibMod.Instance.Logger.Warn(warning);
      }
    }

    // Converting it to a sprite sheet looks best for Terraria's cases,
    // and best for AnimLib, which will benefit from the included AnimationTags
    // https://github.com/AristurtleDev/AsepriteDotNet?tab=readme-ov-file#processor
    SpriteSheet spriteSheet = SpriteSheetProcessor.Process(asepriteFile);

    // We require the main thread to create a Texture2D instance.
    AseAsset aseAsset = null;
    using ManualResetEvent evt = new(false);
    Main.QueueMainThreadAction(() => {
      Texture2D texture2D = AseTextureToTexture2D(spriteSheet.TextureAtlas.Texture);
      aseAsset = new AseAsset(asepriteFile, texture2D, spriteSheet);
      // ReSharper disable once AccessToDisposedClosure
      evt.Set();
    });
    evt.WaitOne();

    return aseAsset as T;
  }

  private static Texture2D AseTextureToTexture2D(Texture aseTexture) {
    int width = aseTexture.Size.Width;
    int height = aseTexture.Size.Height;

    byte[] bytes = new byte[width * height * 4];
    for (int i = 0; i < aseTexture.Pixels.Length; i++) {
      (byte r, byte g, byte b, byte a) = aseTexture.Pixels[i];
      int idx = i * 4;
      bytes[idx] = r;
      bytes[idx + 1] = g;
      bytes[idx + 2] = b;
      bytes[idx + 3] = a;
    }

    Texture2D texture2D = new(Main.graphics.GraphicsDevice, width, height);
    texture2D.SetData(bytes);
    return texture2D;
  }
}
