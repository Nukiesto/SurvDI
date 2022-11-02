using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;

namespace SurvDI.Core.Container
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [Serializable]
    public class DiContainer
    {
        //SINGLE UNITS
        //k-type;v-unit
        internal readonly Dictionary<Type, ContainerUnit> ContainerSingleUnits = new();
        
        //MULTUPLE UNITS
        //k-asType;v-units
        internal readonly Dictionary<Type, List<ContainerUnit>> ContainerAsTypeUnits = new();
        //k-type;v-units
        internal readonly Dictionary<Type, List<ContainerUnit>> ContainerMultiUnits = new();
        //k-needMultyType;v-units
        internal readonly Dictionary<Type, List<ContainerUnit>> ContainersMultyNeed = new();
        
        public event Action<DiContainer, ContainerUnit> OnBindNewInstanceEvent;
        public event Action<DiContainer, ContainerUnit> OnRemoveInstanceEvent;
        
        internal readonly List<ContainerUnit> AllUnits = new();
        public List<T> ResolveMulti<T>()
        {
            var list = new List<T>();
            var listSource = new List<ContainerUnit>();
            var elementType = typeof(T);
            if (ContainerAsTypeUnits.ContainsKey(elementType))
                listSource.AddRange(ContainerAsTypeUnits[elementType]);

            if (ContainerMultiUnits.ContainsKey(elementType))
                listSource.AddRange(ContainerMultiUnits[elementType]);

            if (listSource.Count > 0)
            {
                foreach (var containerUnit in listSource)
                {
                    if (containerUnit.Object is T obj)
                        list.Add(obj);
                }
            }

            return list;
        }
        public T ResolveSingle<T>()
        {
            return (T)ContainerSingleUnits[typeof(T)].Object;
        }
        public object ResolveSingle(Type type)
        {
            return ContainerSingleUnits[type].Object;
        }
        
        public bool ContainsSingle<T>()
        {
            return ContainerSingleUnits.ContainsKey(typeof(T));
        }
        public bool ContainsSingle(Type type)
        {
            return ContainerSingleUnits.ContainsKey(type);
        }
        
        public void InvokeConstructorsAll()
        {
            var copy = AllUnits.ToArray();
            foreach (var containerUnit in copy)
                containerUnit.InvokeConstructorInit(this);
        }

        public void InvokeInjectAll()
        {
            var copy = AllUnits.ToArray();
            foreach (var containerUnit in copy)
                containerUnit.InvokeInjectsOnInit(this);
        }

        public void LoadSavingAll()
        {
            var copy = AllUnits.ToArray();
            foreach (var containerUnit in copy)
                containerUnit.LoadSaveable();
        }
        public List<T> GetInterfaceUnits<T>()
        {
            var type = typeof(T);
            if (ContainerAsTypeUnits.ContainsKey(type))
                return ContainerAsTypeUnits[type].Select(s => s.Object).Cast<T>().ToList();
            ContainerAsTypeUnits.Add(type, new List<ContainerUnit>());
            return new List<T>();
        }
        public List<ContainerUnit> GetInterfaceUnitsContainers<T>()
        {
            var type = typeof(T);
            if (ContainerAsTypeUnits.ContainsKey(type))
                return ContainerAsTypeUnits[type].ToList();
            ContainerAsTypeUnits.Add(type, new List<ContainerUnit>());
            return new List<ContainerUnit>();
        }
        internal void InvokeBindNewInstance(ContainerUnit containerUnit)
        {
            OnBindNewInstanceEvent?.Invoke(this, containerUnit);
        }

        public void RemoveUnit(ContainerUnit containerUnit)
        {
            OnRemoveInstanceEvent?.Invoke(this, containerUnit);
            Debug.Log($"Remove: {containerUnit.Type}");
            if (ContainerSingleUnits.ContainsKey(containerUnit.Type))
                ContainerSingleUnits.Remove(containerUnit.Type);
            if (ContainerAsTypeUnits.ContainsKey(containerUnit.Type))
                ContainerAsTypeUnits.Remove(containerUnit.Type);
            if (ContainerMultiUnits.ContainsKey(containerUnit.Type))
                if (ContainerMultiUnits[containerUnit.Type].Contains(containerUnit))
                    ContainerMultiUnits[containerUnit.Type].Remove(containerUnit);
            if (ContainersMultyNeed.ContainsKey(containerUnit.Type))
                if (ContainersMultyNeed[containerUnit.Type].Contains(containerUnit))
                    ContainersMultyNeed[containerUnit.Type].Remove(containerUnit);
            
            Debug.Log(ContainerSingleUnits.ContainsKey(containerUnit.Type));
        }
    }
}
