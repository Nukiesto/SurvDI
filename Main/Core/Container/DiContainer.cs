using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using SurvDI.Core.Common;
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
        private readonly List<ContainerUnit> _unitsSnapshot = new();
        private readonly List<Type> _removeKeys = new();

        public List<T> ResolveMulti<T>()
        {
            var list = new List<T>();
            var elementType = typeof(T);

            if (ContainerAsTypeUnits.TryGetValue(elementType, out var asUnits))
                AddObjects(asUnits);
            if (ContainerMultiUnits.TryGetValue(elementType, out var multiUnits))
                AddObjects(multiUnits);

            return list;

            void AddObjects(List<ContainerUnit> source)
            {
                foreach (var containerUnit in source)
                {
                    if (containerUnit.IsDisposed)
                        continue;
                    if (containerUnit.Object is T obj)
                        list.Add(obj);
                }
            }
        }

        public bool TryResolveSingle<T>(out T obj)
        {
            if (TryResolveSingle(typeof(T), out var obj2))
            {
                if (obj2 is T obj3)
                {
                    obj = obj3;
                    return true;
                }
            }

            obj = default;
            return false;
        }

        public bool TryResolveSingle(Type type, out object obj)
        {
            if (TryGetUnit(type, null, out var unit))
            {
                obj = unit.Object;
                return true;
            }

            obj = null;
            return false;
        }

        public T ResolveSingle<T>()
        {
            return (T)ResolveSingle(typeof(T));
        }
        public object ResolveSingle(Type type)
        {
            if (TryGetUnit(type, null, out var unit))
                return unit.Object;
            throw new KeyNotFoundException("Can`t resolve type: " + type.Name);
        }

        public bool ContainsSingle<T>()
        {
            return ContainsSingle(typeof(T));
        }
        public bool ContainsSingle(Type type)
        {
            return TryGetUnit(type, null, out _);
        }

        internal bool TryGetUnit(Type type, string id, out ContainerUnit unit)
        {
            if (type == null)
            {
                unit = null;
                return false;
            }

            if (ContainerSingleUnits.TryGetValue(type, out unit) && unit.MatchesId(id) && !unit.IsDisposed)
                return true;

            if (ContainerAsTypeUnits.TryGetValue(type, out var asUnits) && TryGetSingleFromList(asUnits, id, out unit))
                return true;

            unit = null;
            return false;
        }

        internal bool TryGetUnitByObject(object obj, out ContainerUnit unit)
        {
            foreach (var containerUnit in AllUnits)
            {
                if (containerUnit.IsDisposed)
                    continue;
                if (!ReferenceEquals(containerUnit.Object, obj))
                    continue;
                unit = containerUnit;
                return true;
            }

            unit = null;
            return false;
        }

        internal void AddUnitToWaitingMultiInjects(ContainerUnit unit)
        {
            if (unit == null || unit.IsDisposed)
                return;

            AddUnitToWaitingMultiInjects(unit.Type, unit);
            AddUnitToWaitingMultiInjects(unit.BaseType, unit);
            foreach (var type in unit.Interfaces)
                AddUnitToWaitingMultiInjects(type, unit);
        }

        private void AddUnitToWaitingMultiInjects(Type type, ContainerUnit unit)
        {
            if (type == null)
                return;
            if (!ContainersMultyNeed.TryGetValue(type, out var listNeed))
                return;

            foreach (var containerUnit in listNeed)
                containerUnit.AddNewMulti(type, unit);
        }

        private static bool TryGetSingleFromList(List<ContainerUnit> units, string id, out ContainerUnit unit)
        {
            foreach (var containerUnit in units)
            {
                if (containerUnit.IsDisposed)
                    continue;
                if (containerUnit.BindingType != BindingType.Single)
                    continue;
                if (!containerUnit.MatchesId(id))
                    continue;
                unit = containerUnit;
                return true;
            }

            unit = null;
            return false;
        }

        public void InvokeConstructorsAll()
        {
            FillUnitsSnapshot();
            foreach (var containerUnit in _unitsSnapshot)
            {
                if (containerUnit.IsDisposed)
                    continue;
                containerUnit.InvokeConstructorInit(this);
            }
        }

        public void InvokeInjectAll()
        {
            FillUnitsSnapshot();
            foreach (var containerUnit in _unitsSnapshot)
            {
                if (containerUnit.IsDisposed)
                    continue;
                containerUnit.InvokeInjectsOnInit(this);
            }
        }

        public void InitModulesAll()
        {
            FillUnitsSnapshot();
            foreach (var containerUnit in _unitsSnapshot)
            {
                if (containerUnit.IsDisposed)
                    continue;
                containerUnit.InitModules();
            }
        }
        public List<T> GetInterfaceUnits<T>()
        {
            var type = typeof(T);
            var list = new List<T>();
            if (ContainerAsTypeUnits.TryGetValue(type, out var units))
            {
                foreach (var unit in units)
                {
                    if (unit.IsDisposed)
                        continue;
                    if (unit.Object is T obj)
                        list.Add(obj);
                }
                return list;
            }
            ContainerAsTypeUnits.Add(type, new List<ContainerUnit>());
            return list;
        }
        public List<ContainerUnit> GetInterfaceUnitsContainers<T>()
        {
            var type = typeof(T);
            var list = new List<ContainerUnit>();
            if (ContainerAsTypeUnits.TryGetValue(type, out var units))
            {
                foreach (var unit in units)
                {
                    if (!unit.IsDisposed)
                        list.Add(unit);
                }
                return list;
            }
            ContainerAsTypeUnits.Add(type, new List<ContainerUnit>());
            return list;
        }
        internal void FillInterfaceUnitsContainers<T>(List<ContainerUnit> result)
        {
            result.Clear();
            var type = typeof(T);
            if (ContainerAsTypeUnits.TryGetValue(type, out var units))
            {
                foreach (var unit in units)
                {
                    if (!unit.IsDisposed)
                        result.Add(unit);
                }
                return;
            }
            ContainerAsTypeUnits.Add(type, new List<ContainerUnit>());
        }
        internal void InvokeBindNewInstance(ContainerUnit containerUnit)
        {
            OnBindNewInstanceEvent?.Invoke(this, containerUnit);
        }

        public void RemoveUnit(ContainerUnit containerUnit)
        {
            if (containerUnit == null)
                return;

            OnRemoveInstanceEvent?.Invoke(this, containerUnit);
            //Debug.Log($"Remove: {containerUnit.Type}");
            if (ContainerSingleUnits.TryGetValue(containerUnit.Type, out var singleUnit) && ReferenceEquals(singleUnit, containerUnit))
                ContainerSingleUnits.Remove(containerUnit.Type);

            RemoveFromDictionary(ContainerAsTypeUnits, containerUnit);
            RemoveFromDictionary(ContainerMultiUnits, containerUnit);
            RemoveFromDictionary(ContainersMultyNeed, containerUnit);
            AllUnits.Remove(containerUnit);

            //Debug.Log(ContainerSingleUnits.ContainsKey(containerUnit.Type));
        }

        private void FillUnitsSnapshot()
        {
            _unitsSnapshot.Clear();
            _unitsSnapshot.AddRange(AllUnits);
        }

        private void RemoveFromDictionary(Dictionary<Type, List<ContainerUnit>> dictionary, ContainerUnit containerUnit)
        {
            _removeKeys.Clear();
            foreach (var keyPair in dictionary)
            {
                keyPair.Value.Remove(containerUnit);
                if (keyPair.Value.Count == 0)
                    _removeKeys.Add(keyPair.Key);
            }

            foreach (var key in _removeKeys)
                dictionary.Remove(key);
        }
    }
}
