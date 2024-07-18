using AsepriteDotNet.Aseprite;
using AsepriteDotNet.Aseprite.Types;
using AsepriteDotNet.Common;
using AsepriteDotNet.Processors;
using Rectangle = AsepriteDotNet.Common.Rectangle;

namespace AnimLib.Aseprite.Processors;

public static class ProcessorHelper {
  public static Rgba32[][] FlattenFrameToTopLayers(AsepriteFile file, int frameIndex,
    ProcessorOptions options, AsepriteLayer[] rootLayers) {
    ArgumentNullException.ThrowIfNull(file);

    AsepriteFrame frame = file.Frames[frameIndex];

    var result = new Rgba32[rootLayers.Length][];

    for (int i = 0; i < rootLayers.Length; i++) {
      AsepriteLayer rootLayer = rootLayers[i];

      if (rootLayer is AsepriteGroupLayer groupLayer) {
        result[i] = MergeGroupCels(options, frame, groupLayer);
        continue;
      }

      var cels = frame.Cels;
      for (int j = 0; j < cels.Length; j++) {
        AsepriteCel cel = cels[j];
        if (!ReferenceEquals(cel.Layer, rootLayer)) {
          continue;
        }

        Rgba32[] flattenedLayerPixels = null;
        if (MergeCel(options, frame, cel, ref flattenedLayerPixels)) {
          result[i] = flattenedLayerPixels;
        }

        break;
      }
    }

    return result;
  }

  [CanBeNull]
  private static Rgba32[] MergeGroupCels(ProcessorOptions options, AsepriteFrame frame, AsepriteGroupLayer groupLayer) {
    Rgba32[] flattenedLayerPixels = null;

    var cels = GetCels(groupLayer, frame);
    foreach (AsepriteCel cel in cels) {
      MergeCel(options, frame, cel, ref flattenedLayerPixels);
    }

    return flattenedLayerPixels;
  }


  private static List<AsepriteCel> GetCels(AsepriteGroupLayer groupLayer, AsepriteFrame frame) {
    List<AsepriteCel> result = [];

    int childrenLength = groupLayer.Children.Length;
    int celsLength = frame.Cels.Length;
    int jStart = 0;

    // If only we could use Union here...
    for (int i = 0; i < celsLength; i++) {
      AsepriteCel cel = frame.Cels[i];
      for (int j = jStart; j < childrenLength; j++) {
        AsepriteLayer child = groupLayer.Children[j];
        if (ReferenceEquals(child, cel.Layer)) {
          result.Add(cel);
          jStart = j + 1;
          break;
        }
      }
    }

    return result;
  }

  private static bool MergeCel(ProcessorOptions options, AsepriteFrame frame, AsepriteCel cel,
    [CanBeNull] ref Rgba32[] flattenedLayerPixels) {
    cel = cel is AsepriteLinkedCel linkedCel ? linkedCel.Cel : cel;

    if (options.OnlyVisibleLayers && !cel.Layer.IsVisible) {
      return false;
    }

    if (cel.Layer.IsBackgroundLayer && !options.IncludeBackgroundLayer) {
      return false;
    }

    switch (cel) {
      case AsepriteImageCel imageCel:
        flattenedLayerPixels ??= new Rgba32[frame.Size.Width * frame.Size.Height];
        BlendCel(frame, flattenedLayerPixels, imageCel);
        return true;
      case AsepriteTilemapCel tilemapCel when options.IncludeTilemapLayers:
        flattenedLayerPixels ??= new Rgba32[frame.Size.Width * frame.Size.Height];
        BlendTilemapCel(frame, flattenedLayerPixels, tilemapCel);
        return true;
      default:
        return false;
    }
  }

  private static void BlendCel(AsepriteFrame frame, Rgba32[] flattenedLayerPixels, AsepriteImageCel imageCel) {
    AsepriteFrameExtensions.BlendCel(flattenedLayerPixels, imageCel.Pixels, imageCel.Layer.BlendMode,
      new Rectangle(imageCel.Location, imageCel.Size), frame.Size.Width, imageCel.Opacity,
      imageCel.Layer.Opacity);
  }

  private static void BlendTilemapCel(AsepriteFrame frame, Rgba32[] flattenedLayerPixels, AsepriteTilemapCel tilemapCel) {
    AsepriteFrameExtensions.BlendTilemapCel(flattenedLayerPixels, tilemapCel, frame.Size.Width);
  }
}
