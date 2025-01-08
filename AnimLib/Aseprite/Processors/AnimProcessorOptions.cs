namespace AnimLib.Aseprite.Processors;

public ref struct AnimProcessorOptions {
  public AnimProcessorOptions(bool upscale,
    bool onlyVisibleLayers,
    bool includeBackgroundLayer,
    bool mergeDuplicateFrames,
    bool includeTilemapLayers,
    int borderPadding,
    int spacing,
    int innerPadding) {
    Upscale = upscale;
    OnlyVisibleLayers = onlyVisibleLayers;
    IncludeBackgroundLayer = includeBackgroundLayer;
    MergeDuplicateFrames = mergeDuplicateFrames;
    IncludeTilemapLayers = includeTilemapLayers;
    BorderPadding = borderPadding;
    Spacing = spacing;
    InnerPadding = innerPadding;
  }

  public static AnimProcessorOptions Default => new(false, true, false, true, true, 0, 0, 0);

  public bool Upscale { get; set; } = true;
  public bool OnlyVisibleLayers { get; set; } = true;
  public bool IncludeBackgroundLayer { get; set; }
  public bool MergeDuplicateFrames { get; set; } = true;
  public bool IncludeTilemapLayers { get; set; } = true;
  public int BorderPadding { get; set; }
  public int Spacing { get; set; }
  public int InnerPadding { get; set; }

  public AsepriteDotNet.Processors.ProcessorOptions ToAseprite() => new(OnlyVisibleLayers, IncludeBackgroundLayer,
    IncludeTilemapLayers, MergeDuplicateFrames, BorderPadding, Spacing, InnerPadding);
}
