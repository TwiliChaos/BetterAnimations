namespace AnimLib;

public abstract partial class AnimCharacter {
  /// <inheritdoc cref="ModPlayer.AddStartingItems"/>
  public virtual IEnumerable<Item> AddStartingItems(bool mediumCoreDeath) => [];

  /// <inheritdoc cref="ModPlayer.ModifyStartingInventory"/>
  public virtual void
    ModifyStartingInventory(IReadOnlyDictionary<string, List<Item>> itemsByMod, bool mediumCoreDeath) {
  }

  /// <inheritdoc cref="ModPlayer.AddMaterialsForCrafting"/>
  public virtual IEnumerable<Item> AddMaterialsForCrafting(out ModPlayer.ItemConsumedCallback? itemConsumedCallback) {
    itemConsumedCallback = null;
    return [];
  }
}
