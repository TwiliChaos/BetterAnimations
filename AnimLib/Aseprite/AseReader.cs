using System.IO;
using System.Text;
using AsepriteDotNet;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Common;
using AsepriteDotNet.IO;
using AsepriteDotNet.Processors;
using ReLogic.Content.Readers;
using Terraria.ModLoader.IO;
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

    string name = asepriteFile.Layers[0].Name;
    var texture2D = AseTextureToTexture2DAsset(spriteSheet.TextureAtlas.Texture, name);
    AseAsset aseAsset = new(asepriteFile, texture2D, spriteSheet);

    return aseAsset as T;
  }

  private static Asset<Texture2D> AseTextureToTexture2DAsset(Texture aseTexture, string name) {
    // We do not use "using" here, the reader will close the stream once it creates the Texture2D.
    MemoryStream stream = new();

    // We create a stream representing a "rawimg" so that an existing reader can create a Asset<Texture2D> for us
    using (BinaryWriter writer = new(stream, Encoding.Default, leaveOpen: true)) {
      writer.Write(ImageIO.VERSION);
      writer.Write(aseTexture.Size.Width);
      writer.Write(aseTexture.Size.Height);
      foreach (Rgba32 t in aseTexture.Pixels) {
        writer.Write(t.PackedValue);
      }

      stream.Seek(0, SeekOrigin.Begin);
      writer.Close();
    }

    string filename = name + ".rawimg";
    return AnimLibMod.Instance.AseAssets.CreateUntracked<Texture2D>(stream, filename, AssetRequestMode.AsyncLoad);
  }
}
