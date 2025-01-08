using System.IO;
using AnimLib.Animations;
using AnimLib.Aseprite.Processors;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.IO;
using JetBrains.Annotations;
using ReLogic.Content.Readers;

namespace AnimLib.Aseprite;

/// <summary>
/// AssetReader to read an Aseprite file (*.ase/*.aseprite) into a format usable by Terraria.
/// This Reader uses AsepriteDotNet to load the file into an <see cref="AsepriteFile"/> object,
/// which this reader then uses to create an object that AnimLib and Terraria can use.
/// https://github.com/AristurtleDev/AsepriteDotNet
/// </summary>
[PublicAPI]
public class AseReader : ModSystem, IAssetReader {
  private static readonly Dictionary<Type, IAsepriteProcessor> Processors = new();

  internal static void AddDefaultProcessors() {
    AddProcessor<AnimSpriteSheetProcessor, AnimSpriteSheet>();
    AddProcessor<TextureProcessor, Texture2D>();
    AddProcessor<TextureDictionaryProcessor, Dictionary<string, Asset<Texture2D>>>();
  }

  /// <summary>
  /// Add a processor of type <typeparam name="TProcessor"></typeparam>
  /// which can process an <see cref="AsepriteFile"/> (*.ase/*.aseprite)
  /// into an object of type <typeparam name="T"></typeparam>.
  /// <para />
  /// This allows for `ModContent.Request&lt;<typeparamref name="T"/>&gt;("Path/To/MyAsepriteFile")`
  /// <para />
  /// This must be called during your mod's <see cref="Mod.CreateDefaultContentSource"/>.
  /// <see cref="Mod.Load"/> is too late in the loading process.
  /// </summary>
  /// <typeparam name="TProcessor">Type of processor.</typeparam>
  /// <typeparam name="T">The resulting object from the processor.</typeparam>
  public static void AddProcessor<TProcessor, T>() where T : class where TProcessor : IAsepriteProcessor<T>, new() {
    Processors.Add(typeof(T), new TProcessor());
  }

  private static bool TryGetProcessor<T>([NotNullWhen(true)] out IAsepriteProcessor<T>? processor) where T : class {
    if (Processors.TryGetValue(typeof(T), out IAsepriteProcessor? baseProcessor)) {
      processor = (IAsepriteProcessor<T>)baseProcessor;
      return true;
    }

    processor = null;
    return false;
  }

  public override void Unload() {
    Processors.Clear();
  }


  public T FromStream<T>(Stream stream) where T : class {
    if (!TryGetProcessor(out IAsepriteProcessor<T>? processor)) {
      throw AssetLoadException.FromInvalidReader<AseReader, T>();
    }

    AsepriteFile asepriteFile;
    if (stream.CanSeek) {
      // We have no access to the file name to name this object.
      asepriteFile = AsepriteFileLoader.FromStream("", stream);
    }
    else {
      // AsepriteFileLoader requires the Seek function.
      // We cannot guarantee that the incoming stream supports Seeking (e.g. DeflateStream),
      // So we have to create a new Stream that allows it.
      using MemoryStream newStream = new();
      stream.CopyTo(newStream);
      newStream.Position = 0;
      asepriteFile = AsepriteFileLoader.FromStream("", newStream);
    }

#if DEBUG
    // We have no access to the file name or the mod, so these warnings may just be annoying.
    var warnings = asepriteFile.Warnings;
    if (!warnings.IsEmpty) {
      Log.Warn($"Aseprite file loaded with the following warnings:");
      foreach (string warning in warnings) {
        Log.Warn(warning);
      }
    }
#endif

    return processor.Process(asepriteFile);
  }
}
