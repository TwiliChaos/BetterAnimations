using AnimLib.States;
using JetBrains.Annotations;
using Terraria.ModLoader.IO;

namespace AnimLib;

[UsedImplicitly]
public sealed class UnloadedStatesPlayer : ModPlayer {
  private const string UnloadedStatesKey = "unloadedStates";
  private const string ModNameKey = "mod";
  private const string StateNameKey = "state";
  private const string DataKey = "data";
  public readonly IList<TagCompound> UnloadedStates = [];

  public override void SaveData(TagCompound tag) {
    tag[UnloadedStatesKey] = UnloadedStates;
  }

  public override void LoadData(TagCompound tag) {
    LoadStates(tag.GetList<TagCompound>(UnloadedStatesKey));
  }

  private void LoadStates(IList<TagCompound> states) {
    AnimPlayer animPlayer = Player.GetModPlayer<AnimPlayer>();
    foreach (TagCompound stateTag in states) {
      string modName = stateTag.GetString(ModNameKey);
      string stateName = stateTag.GetString(StateNameKey);
      if (!ModContent.TryFind(modName, stateName, out State templateState)) {
        UnloadedStates.Add(stateTag);
        continue;
      }

      State state = animPlayer.GetState(templateState);

      try {
        state.LoadData(stateTag.GetCompound(DataKey));
      }
      catch (Exception e) {
        Log.Error($"Failed to load state {stateName} from mod {modName}.", e);
      }
    }
  }
}
