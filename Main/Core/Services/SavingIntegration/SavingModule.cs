using System;
using System.Collections.Generic;
using System.Reflection;
using SurvDI.UnityIntegration.Debugging;
using UsefulScripts.NetScripts.Data;

namespace SurvDI.Core.Services.SavingIntegration
{
    [Serializable]
    public class SavingData
    {
        [Serializable]
        public class Unit
        {
            public string data;

            public Unit(object set)
            {
                SetData(set);
            }

            public void SetData(object set)
            {
                data = DataSaver.Serialize(set);
            }
        }

        public Dictionary<string, Unit> Units = new();
    }
    public interface IOnSave
    {
        /// <summary>
        /// Do not invoke manually
        /// </summary>
        void OnSave();
    }
    public abstract class SaveData : IOnSave
    {
        public event Action OnSaveEvent;

        public void OnSave()
        {
            OnSaveEvent?.Invoke();
        }
    }

    public class SavingModule
    {
        private static readonly Dictionary<Type, FieldInfo[]> SaveableFieldsByType = new();

        public static void LoadAll(Type classType, object obj)
        {
            if (classType == null)
                return;

            var fieldsSaveable = GetSaveableFields(classType);
            if (fieldsSaveable.Length == 0)
                return;

            var saveData = LoadExistingData(classType);
            var dirty = false;

            foreach (var fieldInfo in fieldsSaveable)
            {
                if (saveData.Units.TryGetValue(fieldInfo.Name, out var data))
                {
                    var valueGet = DataSaver.Deserialize(data.data, fieldInfo.FieldType);
                    fieldInfo.SetValue(obj, valueGet);
                }
                else
                {
                    try
                    {
                        var valueGet = fieldInfo.GetValue(obj) ?? CreateDefaultValue(fieldInfo.FieldType);
                        saveData.Units.Add(fieldInfo.Name, new SavingData.Unit(valueGet));
                        fieldInfo.SetValue(obj, valueGet);
                        dirty = true;
                    }
                    catch
                    {
                        fieldInfo.SetValue(obj, default);
                    }
                }
            }

            if (dirty)
                DataSaver.Save(GetName(classType), saveData);
        }

        public static void SaveAll(Type classType, object obj)
        {
            if (classType == null)
                return;

            var fieldsSaveable = GetSaveableFields(classType);
            if (fieldsSaveable.Length == 0)
                return;

            Debugger.Log("Save");
            var saveData = LoadExistingData(classType);

            foreach (var fieldInfo in fieldsSaveable)
            {
                var valueGet = fieldInfo.GetValue(obj);
                if (valueGet is IOnSave onSave)
                    onSave.OnSave();

                if (saveData.Units.TryGetValue(fieldInfo.Name, out var data))
                    data.SetData(valueGet);
                else
                    saveData.Units.Add(fieldInfo.Name, new SavingData.Unit(valueGet));
            }
            DataSaver.Save(GetName(classType), saveData);
        }

        private static FieldInfo[] GetSaveableFields(Type classType)
        {
            if (SaveableFieldsByType.TryGetValue(classType, out var cachedFields))
                return cachedFields;

            var fields = classType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var fieldsSaveable = new List<FieldInfo>();
            foreach (var fieldInfo in fields)
            {
                if (fieldInfo.GetCustomAttribute<SaveableAttribute>() != null)
                    fieldsSaveable.Add(fieldInfo);
            }

            cachedFields = fieldsSaveable.ToArray();
            SaveableFieldsByType.Add(classType, cachedFields);
            return cachedFields;
        }

        private static SavingData LoadExistingData(Type classType)
        {
            var name = GetName(classType);
            var saveData = DataSaver.Load<SavingData>(name);
            if (saveData != null)
                return saveData;

            var legacyName = GetLegacyName(classType);
            if (legacyName != name)
                saveData = DataSaver.Load<SavingData>(legacyName);

            return saveData ?? new SavingData();
        }

        private static object CreateDefaultValue(Type type)
        {
            if (type == typeof(string))
                return "";
            return Activator.CreateInstance(type);
        }

        private static string GetName(Type type)
        {
            return $"{type.FullName ?? type.Name}.saveModuleData";
        }

        private static string GetLegacyName(Type type)
        {
            return $"{type.Name}.saveModuleData";
        }
    }
}
