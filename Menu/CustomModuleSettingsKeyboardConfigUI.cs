using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox.Menu {
    public class CustomModuleSettingsKeyboardConfigUI : ModuleSettingsKeyboardConfigUI {
        public CustomModuleSettingsKeyboardConfigUI(EverestModule module) : base(module) {
            
        }

        public override void Reload(int index = -1) {
            if (Module == null)
                return;

            Clear();
            Add(new Header(Dialog.Clean("KEY_CONFIG_TITLE")));
            Add(new InputMappingInfo(false));

            object settings = Module._Settings;

            // The default name prefix.
            string typeName = Module.SettingsType.Name.ToLowerInvariant();
            if (typeName.EndsWith("settings"))
                typeName = typeName.Substring(0, typeName.Length - 8);
            string nameDefaultPrefix = $"modoptions_{typeName}_";

            SettingInGameAttribute attribInGame;

            foreach (PropertyInfo prop in Module.SettingsType.GetProperties()) {
                if ((attribInGame = prop.GetCustomAttribute<SettingInGameAttribute>()) != null &&
                    attribInGame.InGame != (Engine.Scene is Level))
                    continue;

                if (prop.GetCustomAttribute<SettingIgnoreAttribute>() != null)
                    continue;

                if (!prop.CanRead || !prop.CanWrite)
                    continue;

                if (typeof(ButtonBinding).IsAssignableFrom(prop.PropertyType)) {
                    if (!(prop.GetValue(settings) is ButtonBinding binding))
                        continue;

                    string name = prop.GetCustomAttribute<SettingNameAttribute>()?.Name ?? $"{nameDefaultPrefix}{prop.Name.ToLowerInvariant()}";
                    name = name.DialogCleanOrNull() ?? (prop.Name.ToLowerInvariant().StartsWith("button") ? prop.Name.Substring(6) : prop.Name).SpacedPascalCase();

                    DefaultButtonBindingAttribute defaults = prop.GetCustomAttribute<DefaultButtonBindingAttribute>();

                    Bindings.Add(new ButtonBindingEntry(binding, defaults));

                    string subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
                    if (subheader != null)
                        Add(new SubHeader(subheader.DialogCleanOrNull() ?? subheader));

                    AddMapForceLabel(name, binding.Binding);
                    
                } else if (prop.GetValue(settings) is Dictionary<string, ButtonBinding> bindingMap) { //New changes
                    string name = prop.GetCustomAttribute<SettingNameAttribute>()?.Name ?? $"{nameDefaultPrefix}{prop.Name.ToLowerInvariant()}";
                    name = name.DialogCleanOrNull() ?? (prop.Name.ToLowerInvariant().StartsWith("buttons") ? prop.Name.Substring(7) : prop.Name).SpacedPascalCase();

                    string subheader = prop.GetCustomAttribute<SettingSubHeaderAttribute>()?.SubHeader;
                    if (subheader != null)
                        Add(new SubHeader(subheader.DialogCleanOrNull() ?? subheader));

                    foreach (KeyValuePair<string, ButtonBinding> entry in bindingMap) {
                        Bindings.Add(new ButtonBindingEntry(entry.Value, null));
                        AddMapForceLabel($"{name}: {entry.Key}", entry.Value.Binding);
                    }
                }
            }

            Add(new SubHeader(""));
            Add(new Button(Dialog.Clean("KEY_CONFIG_RESET")) {
                IncludeWidthInMeasurement = false,
                AlwaysCenter = true,
                OnPressed = () => ResetPressed()
            });

            if (index >= 0)
                Selection = index;
        }
    }
}
