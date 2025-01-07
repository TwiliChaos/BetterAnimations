using Terraria.ModLoader.Exceptions;
using Terraria.ModLoader.IO;

namespace AnimLib.States;

internal static class StateIO {
  internal static List<TagCompound> SaveStateData(Player player) {
    List<TagCompound> list = [];
    TagCompound stateData = [];

    foreach (State state in player.GetStates()) {
      try {
        state.SaveData(stateData);
      }
      catch (Exception e) {
        Log.Error($"Failed to save state {state.Name} from mod {state.Mod.Name}.", e);
        list.Add(new TagCompound {
          ["mod"] = state.Mod.Name,
          ["state"] = state.Name,
          ["error"] = e.ToString()
        });
        stateData = [];
        continue;
      }

      if (stateData.Count == 0) {
        continue;
      }

      list.Add(new TagCompound {
        ["mod"] = state.Mod.Name,
        ["state"] = state.Name,
        ["data"] = stateData
      });
      stateData = [];
    }

    return list;
  }

  internal static void LoadStateData(Player player, IList<TagCompound> list) {
    foreach (TagCompound tag in list) {
      string modName = tag.GetString("mod");
      string stateName = tag.GetString("state");
      if (tag.TryGet("error", out string error)) {
        Log.Error($"Failed to save state {stateName} from mod {modName}.", new Exception(error));
        continue;
      }

      if (!ModContent.TryFind(modName, stateName, out State templateState)) {
        player.GetModPlayer<UnloadedStatesPlayer>().UnloadedStates.Add(tag);
        continue;
      }

      State state = player.GetState(templateState);

      try {
        state.LoadData(tag.GetCompound("data"));
      }
      catch (Exception e) {
        throw new CustomModDataException(state.Mod, $"Error in reading state {stateName} from mod {modName}.", e);
      }
    }
  }
}
