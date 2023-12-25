using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.viddiesToolbox.Tools {
    public static class Util {
        private static ViddiesToolboxModule Mod => ViddiesToolboxModule.Instance;

        public static T GetPrivateProperty<T>(object obj, string propertyName, bool isField = true, bool isPublic = false) {
            if (obj == null) {
                Mod.Log($"Object is null! Property: {propertyName}", LogLevel.Warn);
                return default;
            } else if (propertyName == null) {
                Mod.Log($"Property name is null! Object: {obj.GetType().Name}", LogLevel.Warn);
                return default;
            }

            Type type = obj.GetType();

            BindingFlags flags = BindingFlags.Instance;

            if (isPublic) {
                flags |= BindingFlags.Public;
            } else {
                flags |= BindingFlags.NonPublic;
            }

            if (isField) {
                FieldInfo field = type.GetField(propertyName, flags);
                if (field == null) {
                    Mod.Log($"Field '{propertyName}' not found in {type.Name}! Available fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]", LogLevel.Warn);
                    return default;
                }
                return (T)field.GetValue(obj);

            } else {
                PropertyInfo property = type.GetProperty(propertyName, flags);
                if (property == null) {
                    Mod.Log($"Property '{propertyName}' not found in {type.Name}! Available Fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]", LogLevel.Warn);
                    return default;
                }
                return (T)property.GetValue(obj, null);
            }
        }
        public static void SetPrivateProperty<T>(object obj, string propertyName, T value, bool isField = true, bool isPublic = false) {
            if (obj == null) {
                Mod.Log($"Object is null! Property: {propertyName}", LogLevel.Warn);
                return;
            } else if (propertyName == null) {
                Mod.Log($"Property name is null! Object: {obj.GetType().Name}", LogLevel.Warn);
                return;
            }

            Type type = obj.GetType();

            BindingFlags flags = BindingFlags.Instance;

            if (isPublic) {
                flags |= BindingFlags.Public;
            } else {
                flags |= BindingFlags.NonPublic;
            }

            if (isField) {
                FieldInfo field = type.GetField(propertyName, flags);
                if (field == null) {
                    Mod.Log($"Field '{propertyName}' not found in {type.Name}! Available fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]", LogLevel.Warn);
                    return;
                }
                field.SetValue(obj, value);

            } else {
                PropertyInfo property = type.GetProperty(propertyName, flags);
                if (property == null) {
                    Mod.Log($"Property '{propertyName}' not found in {type.Name}! Available Fields: [{string.Join(", ", type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}], Available Properties: [{string.Join(", ", type.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance).Select(p => p.Name))}] | Public Fields: [{string.Join(", ", type.GetFields(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}], Public Properties: [{string.Join(", ", type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name))}]", LogLevel.Warn);
                    return;
                }
                property.SetValue(obj, value);
            }
        }
    }
}
