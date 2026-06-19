using System;
using System.Collections.Generic;
using System.Linq;
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
        public static void LoadAll(Type classType, object obj)
        {
            if (classType != null)
            {
                var fields = classType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList();
                var fieldsSaveable = fields.Where(s => s.GetCustomAttribute<SaveableAttribute>() != null).ToList();
                if (fieldsSaveable.Count == 0)
                    return;
                var saveData = DataSaver.Load<SavingData>(GetName(classType)) ?? new SavingData();
                
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
                            var valueGet = fieldInfo.GetValue(obj) ?? Activator.CreateInstance(fieldInfo.FieldType);
                            saveData.Units.Add(fieldInfo.Name, new SavingData.Unit(valueGet));
                            fieldInfo.SetValue(obj, valueGet);
                        }
                        catch (Exception e)
                        {
                            fieldInfo.SetValue(obj, default);
                        }
                    }
                }
                DataSaver.Save(GetName(classType), saveData);
            }
        }

        private static object GetEmptyInstance(Type type)
        {
            if (type == typeof(string))
                return "";
            return Activator.CreateInstance(type);
        }
        public static void SaveAll(Type classType, object obj)
        {
            if (classType != null)
            {
                var fields = classType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).ToList();
                var fieldsSaveable = fields.Where(s => s.GetCustomAttribute<SaveableAttribute>() != null).ToList();
                if (fieldsSaveable.Count == 0)
                    return;
                Debugger.Log("Save");
                var saveData = DataSaver.Load<SavingData>(GetName(classType)) ?? new SavingData();
                
                foreach (var fieldInfo in fieldsSaveable)
                {
                    if (saveData.Units.TryGetValue(fieldInfo.Name, out var data))
                    {
                        var valueGet = fieldInfo.GetValue(obj);
                        if (valueGet is IOnSave onSave)
                            onSave.OnSave();
                        data.SetData(valueGet);
                    }
                    else
                    {
                        var valueGet = fieldInfo.GetValue(obj);
                        saveData.Units.Add(fieldInfo.Name, new SavingData.Unit(valueGet));
                    }
                }
                DataSaver.Save(GetName(classType), saveData);
            }
        }

        private static string GetName(Type type)
        {
            return $"{type.Name}.saveModuleData";
        }
    }
}