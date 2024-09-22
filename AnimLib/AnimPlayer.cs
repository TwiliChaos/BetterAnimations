using AnimLib.Animations;
using AnimLib.Networking;
using JetBrains.Annotations;

namespace AnimLib;

/// <summary>
/// Main <see cref="ModPlayer"/> class for <see cref="AnimLibMod"/>, contains and updates <see cref="AnimLib.States.State"/>.
/// </summary>
[UsedImplicitly]
public sealed class AnimPlayer : ModPlayer {
  internal AnimCharacterCollection Characters => _characters ??= new AnimCharacterCollection(Player);
  private AnimCharacterCollection? _characters;

  private bool _hasInitialized;

  public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
    base.ModifyMaxStats(out health, out mana);

    // Treating this method as a "PostInitialize",
    // as it needs to run after all other mods' Initialize
    // and this is the closest hook after Initialize
    if (_hasInitialized) {
      return;
    }

    Characters.Initialize();
    _hasInitialized = true;
    if (Main.dedServ) {
      StatesNet.CreateNetIDs(Characters);
      StatesNet.AssignNetIDs(Characters);
    }
  }

  /// <inheritdoc/>
  public override void SendClientChanges(ModPlayer clientPlayer) {
    if (Characters.IndirectNetUpdate) {
      ModContent.GetInstance<ModNetHandler>().StatePacketHandler.SendPacket(255, Player.whoAmI);
    }
  }

  // ReSharper disable once RedundantOverriddenMember
  public override void CopyClientState(ModPlayer targetCopy) => base.CopyClientState(targetCopy);

  /// <summary>
  /// Updates the <see cref="AnimCharacterCollection.ActiveCharacter"/>.
  /// </summary>
  public override void PostUpdateRunSpeeds() {
    Characters.PreUpdate();
    Characters.Update();
  }

  /// <summary>
  /// PostUpdate for the <see cref="AnimCharacterCollection.ActiveCharacter"/>.
  /// </summary>
  public override void PostUpdate() {
    Characters.PostUpdate();
  }
}
