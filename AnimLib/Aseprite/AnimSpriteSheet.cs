using System.Linq;
using AsepriteDotNet;

namespace AnimLib.Aseprite;

[PublicAPI]
public record AnimSpriteSheet(Dictionary<string, AnimTextureAtlas> _atlases, AnimationTag[] _tags) {
  private readonly AnimationTag[] _tags = _tags;
  private readonly Dictionary<string, AnimTextureAtlas> _atlases = _atlases;
  private readonly Dictionary<string, AnimationTag> _tagDictionary = _tags.ToDictionary(tag => tag.Name, tag => tag);

  /// <summary>
  /// Animation tags as they appear in the Aseprite file in the program.
  /// </summary>
  public ReadOnlySpan<AnimationTag> Tags => (ReadOnlySpan<AnimationTag>)_tags;

  /// <summary>
  /// Animation tags as they appear in the Aseprite file in the program, indexable by animation name.
  /// </summary>
  public IReadOnlyDictionary<string, AnimationTag> TagDictionary => _tagDictionary;

  /// <summary>
  /// Represents pairs of root layer names and the texture atlas generated from the layer.
  /// </summary>
  public IReadOnlyDictionary<string, AnimTextureAtlas> Atlases => _atlases;
}
