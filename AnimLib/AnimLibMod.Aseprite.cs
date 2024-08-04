using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using AnimLib.Animations.Aseprite;
using AsepriteDotNet.Common;
using ReLogic.Content.Readers;
using ReLogic.Content.Sources;
using ReLogic.Utilities;
using Terraria.ModLoader.IO;
using Texture = AsepriteDotNet.Texture;

namespace AnimLib;

public sealed partial class AnimLibMod {
  /// <summary>
  /// Add the Aseprite reader <see cref="AseReader"/>.
  /// This will allow mods using AnimLib to request Aseprite files from Assets.
  /// </summary>
  /// <remarks>
  /// Autoload for aseprite files will not work for mods that load earlier than Animlib,
  /// unless this <see cref="AseReader"/>, or a derivative of it, is added to tML proper.
  /// </remarks>
  public override IContentSource CreateDefaultContentSource() {
    GetAssetReaderCollection().RegisterReader(new AseReader(), ".ase", ".aseprite");
    return base.CreateDefaultContentSource();
  }

  /// <summary>
  /// Contains all Texture2D Assets that were generated during
  /// <see cref="AnimLib.Animations.Aseprite.Processors.AnimSpriteSheetProcessor"/>
  /// </summary>
  public readonly AssetRepository AseAssets = new(GetAssetReaderCollection());

  private static AssetReaderCollection GetAssetReaderCollection() =>
    Main.instance.Services.Get<AssetReaderCollection>();

  /// <summary>
  /// Converts the provided Aseprite <see cref="AsepriteDotNet.Texture"/> to an XNA <see cref="Texture2D"/>.
  /// </summary>
  /// <param name="aseTexture"></param>
  /// <param name="name"></param>
  /// <returns>A tModLoader <see cref="Asset{T}"/> of a <see cref="Texture2D"/></returns>
  /// <remarks>
  /// From the Texture, this method creates a stream that represents a "rawimg"
  /// so that <see cref="Terraria.ModLoader.Assets.RawImgReader"/> can read it to a <see cref="Texture2D"/> for us.
  /// </remarks>
  internal Asset<Texture2D> AseTextureToTexture2DAsset(Texture aseTexture, string name) {
    // We do not use "using" here, the reader will close the stream once it creates the Texture2D.
    MemoryStream stream = new();

    // We create a stream that represents a "rawimg" file so that an existing reader can create the Asset<Texture2D> for us.
    using (BinaryWriter writer = new(stream, Encoding.Default, leaveOpen: true)) {
      writer.Write(ImageIO.VERSION);
      writer.Write(aseTexture.Size.Width);
      writer.Write(aseTexture.Size.Height);
      foreach (Rgba32 t in aseTexture.Pixels) {
        writer.Write(t.PackedValue);
      }

      stream.Position = 0;
      writer.Close();
    }

    string filename = name + ".rawimg";
    return AseAssets.CreateUntracked<Texture2D>(stream, filename, AssetRequestMode.AsyncLoad);
  }

  private static void UnloadAse() {
    // Method exists just to remove our AseReader which was registered in CreateDefaultContentSource()

    const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

    AssetReaderCollection collection = GetAssetReaderCollection();
    Type collectionType = collection.GetType();
    var readers = (Dictionary<string, IAssetReader>)collectionType
      .GetField("_readersByExtension", flags)!
      .GetValue(collection)!;

    readers.Remove(".ase");
    readers.Remove(".aseprite");

    string[] extensions = readers.Keys.ToArray();
    collectionType.GetField("_extensions", flags)!.SetValue(collection, extensions);
  }
}
