using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox.ThirdParty {
    public static class SpeedrunToolSupport {
        private static bool Loaded;

        public static void Load() {
            // don't load twice
            if (Loaded) return;
            Loaded = true;

            var action = new SpeedrunTool.SaveLoad.SaveLoadAction(SaveState, LoadState, ClearState);
            SpeedrunTool.SaveLoad.SaveLoadAction.SafeAdd(SaveState, LoadState, ClearState);
        }

        private static void SaveState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            //Logger.Log(nameof(ConsistencyTrackerModule), "saveState called!");
            ViddiesToolboxModule.Instance.SpeedrunToolSaveState(savedvalues, level);
        }

        private static void LoadState(Dictionary<Type, Dictionary<string, object>> savedvalues, Level level) {
            //Logger.Log(nameof(ConsistencyTrackerModule), "loadState called!");
            ViddiesToolboxModule.Instance.SpeedrunToolLoadState(savedvalues, level);
        }

        private static void ClearState() {
            //Logger.Log(nameof(ConsistencyTrackerModule), "clearState called!");
            ViddiesToolboxModule.Instance.SpeedrunToolClearState();
        }
    }
}
