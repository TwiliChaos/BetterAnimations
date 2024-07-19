using System.Linq;
using AsepriteDotNet;
using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Common;
using AsepriteDotNet.Processors;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using Texture = AsepriteDotNet.Texture;

namespace AnimLib.Aseprite.Processors;

using FrameEntry = (int FrameIndex, int RootLayerIndex, Rgba32[] ColorData, bool IsEmpty);
using LayerEntry = (string LayerName, (int FrameIndex, int RootLayerIndex, Rgba32[] ColorData, bool IsEmpty)[] Frames);

/// <summary>
/// Defines a processor for processing multiple <see cref="TextureAtlas"/>es from an <see cref="AsepriteFile"/>,
/// each corresponding to a root-level layer.
///
/// Creates a Dictionary where each entry key is the root-level layer name, and the value is an atlas that represents either
///  the image of the root-level image layer, or
///  the flattened result of a root-level group layer that contains one or more image layers.
/// </summary>
public static class AnimTextureAtlasProcessor {
  /// <summary>
  /// Processes multiple <see cref="TextureAtlas"/>s from an <see cref="AsepriteFile"/>.
  /// </summary>
  /// <param name="file">The <see cref="AsepriteFile"/> to process.</param>
  /// <param name="options">
  ///   Optional options to use when processing.  If <see langword="null"/>, then
  ///   <see cref="ProcessorOptions.Default"/> will be used.
  /// </param>
  /// <returns>A Dictionary of root-level layers where each key is the layer name and the value is a flattened representation of that layer.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="file"/> is <see langword="null"/>.</exception>
  public static Dictionary<string, AnimTextureAtlas>
    Process(AsepriteFile file, [CanBeNull] ProcessorOptions options = null) {
    ArgumentNullException.ThrowIfNull(file);
    options ??= ProcessorOptions.Default;

    var allFrames = GetAllFrames(file, options);

    var atlasData = CreateTextureAtlases(file, options, allFrames);

    return atlasData;
  }

  private static LayerEntry[] GetAllFrames(AsepriteFile file, ProcessorOptions options) {
    // We want to know what all the valid root layers are
    // These are layers that do not have a parent, and any children will be flattened into them
    var rootLayers = GetRootLayers(file, options);

    int frameCount = file.Frames.Length;
    var allFrames = new LayerEntry[rootLayers.Length];
    for (int i = 0; i < rootLayers.Length; i++) {
      allFrames[i] = (rootLayers[i].Name, new FrameEntry[frameCount]);
    }

    for (int frameIndex = 0; frameIndex < frameCount; frameIndex++) {
      var frameColorData = ProcessorHelper.FlattenFrameToTopLayers(file, frameIndex, options, rootLayers);

      for (int rootLayerIndex = 0; rootLayerIndex < frameColorData.Length; rootLayerIndex++) {
        var colorData = frameColorData[rootLayerIndex];
        bool isEmpty = colorData is null;
        allFrames[rootLayerIndex].Frames[frameIndex] = new FrameEntry(frameIndex, rootLayerIndex, colorData, isEmpty);
      }
    }

    return allFrames;
  }

  private static AsepriteLayer[] GetRootLayers(AsepriteFile file, ProcessorOptions options) {
    var rootLayers = new List<AsepriteLayer>(file.Layers.Length);

    for (int i = 0; i < file.Layers.Length; i++) {
      AsepriteLayer layer = file.Layers[i];
      if (layer.ChildLevel == 0 &&
          layer is not AsepriteGroupLayer { Children.Length: 0 } &&
          (layer.IsVisible || !options.OnlyVisibleLayers) &&
          (!layer.IsBackgroundLayer || options.IncludeBackgroundLayer)) {
        rootLayers.Add(layer);
      }
    }

    return rootLayers.ToArray();
  }

  private static Dictionary<string, AnimTextureAtlas> CreateTextureAtlases(AsepriteFile file, ProcessorOptions options,
    LayerEntry[] allFrames) {
    Dictionary<string, AnimTextureAtlas> atlasData = [];

    // Allows upscaling during import, so art created at 1px is upscaled to the 2x2 pixel thing Terraria does
    // TODO: Better handling of Aseprite sprite UserData properties.
    bool upscale = file.UserData.Text?.Contains("upscale", StringComparison.OrdinalIgnoreCase) ?? false;
    int scale = upscale ? 2 : 1;

    foreach ((string name, var frames) in allFrames) {
      int frameCount = frames.Length;

      Dictionary<int, int> duplicateMap = null;
      if (options.MergeDuplicateFrames) {
        duplicateMap = GetDuplicateMap(frames);
        frameCount -= duplicateMap.Count;
      }

      double sqrt = Math.Sqrt(frameCount);
      int columns = (int)Math.Ceiling(sqrt);
      int rows = (frameCount + columns - 1) / columns;

      int frameWidth = file.CanvasWidth * scale;
      int frameHeight = file.CanvasHeight * scale;
      Size imageSize = new() {
        Width = columns * frameWidth
          + options.BorderPadding * 2
          + options.Spacing * (columns - 1)
          + options.InnerPadding * 2 * columns,
        Height = columns * frameHeight
          + options.BorderPadding * 2
          + options.Spacing * (rows - 1)
          + options.InnerPadding * 2 * rows
      };

      var imagePixels = new Rgba32[imageSize.Width * imageSize.Height];

      var regions = new Rectangle[file.Frames.Length];
      int offset = 0;
      var originalToDuplicateLookup = new Dictionary<int, Rectangle>();

      for (int i = 0; i < frames.Length; i++) {
        var frame = frames[i];

        // Create region for duplicate frame, don't write to texture
        if (options.MergeDuplicateFrames && duplicateMap!.TryGetValue(i, out int value)) {
          regions[frame.FrameIndex] = originalToDuplicateLookup[value];
          offset++;
          continue;
        }

        // Get X and Y coords for where to write the color data to
        int column = (i - offset) % columns;
        int row = (i - offset) / columns;

        int x = column * frameWidth
          + options.BorderPadding
          + options.Spacing * column
          + options.InnerPadding * (column + column + 1);

        int y = row * frameHeight
          + options.BorderPadding
          + options.Spacing * row
          + options.InnerPadding * (row + row + 1);

        // Write the color data
        if (!frame.IsEmpty) {
          if (upscale) {
            WriteScaledPixels(imagePixels, imageSize.Width, frame.ColorData, x, y, frameWidth);
          }
          else {
            WritePixels(imagePixels, imageSize.Width, frame.ColorData, x, y, frameWidth, frameHeight);
          }
        }

        Rectangle bounds = new(x, y, frameWidth, frameHeight);
        regions[frame.FrameIndex] = bounds;
        originalToDuplicateLookup.Add(i, bounds);
      }

      Texture aseTexture = new(name, imageSize, imagePixels);
      var texture = AnimLibMod.Instance.AseTextureToTexture2DAsset(aseTexture, name);
      AnimTextureAtlas atlas = new(regions, texture);
      atlasData.Add(name, atlas);
    }

    return atlasData;
  }

  private static Dictionary<int, int> GetDuplicateMap(FrameEntry[] layerFrames) {
    int emptyIndex = -1;
    var duplicateMap = new Dictionary<int, int>();

    for (int i = 0; i < layerFrames.Length; i++) {
      var frame = layerFrames[i];
      if (frame.IsEmpty) {
        // Frame is empty, map to shared empty and continue to next frame
        if (emptyIndex == -1) {
          // First instance of empty
          emptyIndex = i;
        }
        else {
          duplicateMap.Add(i, emptyIndex);
        }

        continue;
      }

      for (int d = 0; d < i; d++) {
        // Expensive checks
        if (IsDuplicate(layerFrames[i], layerFrames[d])) {
          duplicateMap.Add(i, d);
          break;
        }
      }
    }

    return duplicateMap;
  }

  private static bool IsDuplicate(FrameEntry firstFrame, FrameEntry secondFrame) {
    // Only compare to non-empty candidates that originate from the original layer and are not itself
    if (secondFrame.IsEmpty ||
        secondFrame.RootLayerIndex != firstFrame.RootLayerIndex ||
        secondFrame.FrameIndex == firstFrame.FrameIndex) {
      return false;
    }

    var firstData = firstFrame.ColorData;
    var secondData = secondFrame.ColorData;

    // Attempt early terminations by comparing sparse individual pixels of the texture
    // Equality is very slow otherwise
    int size = firstData.Length;
    int interval = size > 50 ? size / 50 : 1;
    for (int i = interval; i < size; i += interval) {
      if (firstData[i] != secondData[i]) {
        return false;
      }
    }

    // Last resort, actually compare the arrays
    return firstData.SequenceEqual(secondData, null);
  }

  private static void WritePixels(Rgba32[] imagePixels, int imageWidth, Rgba32[] pixels, int x, int y, int w, int h) {
    int length = pixels.Length;
    for (int p = 0; p < length; p++) {
      int px = x + p % w;
      int py = y + p / h;
      int index = py * imageWidth + px;
      imagePixels[index] = pixels[p];
    }
  }

  private static void WriteScaledPixels(Rgba32[] imagePixels, int imageWidth, Rgba32[] pixels,
    int x, int y, int w) {
    int length = pixels.Length;
    for (int p = 0; p < length; p++) {
      int p2 = p * 2;
      int px = x + p2 % w; // increase x by 2 per pixel
      int py = y + p2 / w * 2; // increase y by 2 per row of pixels
      int row1 = py * imageWidth + px;
      int row2 = row1 + imageWidth;

      Rgba32 pixel = pixels[p];
      imagePixels[row1] = pixel;
      imagePixels[row1 + 1] = pixel;
      imagePixels[row2] = pixel;
      imagePixels[row2 + 1] = pixel;
    }
  }
}
