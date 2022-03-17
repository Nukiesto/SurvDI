using System;
using System.Collections.Generic;
using SurvDI.Core.Common;

namespace SurvDI.Core.Container
{
    [Flags]
    public enum InjectMode
    {
        BaseTypeAndSelf,
        InterfacesAndSelf,
        All = BaseTypeAndSelf | InterfacesAndSelf
    }
    public static class ContainerBinderExt
    {
        public static ContainerUnit BindSingle<T>(this DiContainer diContainer, InjectMode injectMode = InjectMode.All)
        {
            return diContainer.BindSingle(typeof(T), injectMode);
        }

        public static ContainerUnit BindSingle(this DiContainer diContainer, Type type, InjectMode injectMode = InjectMode.All)
        {
            var unit = new ContainerUnit(diContainer, type, injectMode)
            {
                BindingType = BindingType.Single
            };
            diContainer.AllUnits.Add(unit);
            
            //Добавляем в сингл словарь
            diContainer.ContainerSingleUnits.Add(type, unit);
            
            //Добавляем в As словарь
            diContainer.AddToAsDic(unit, injectMode);
        
            diContainer.InvokeBindNewInstance(unit);
            return unit;
        }

        
        public static ContainerUnit BindInstanceSingle<T>(this DiContainer diContainer, T instance, InjectMode injectMode = InjectMode.All)
        {
            return BindInstanceSingle(diContainer, typeof(T), instance, injectMode);
        }
        public static ContainerUnit BindInstanceSingle(this DiContainer diContainer, Type type, object instance, InjectMode injectMode = InjectMode.All)
        {
            var unit = new ContainerUnit(diContainer, type, injectMode, instance)
            {
                BindingType = BindingType.Single
            };
            diContainer.AllUnits.Add(unit);
            
            //Добавляем в сингл словарь
            diContainer.ContainerSingleUnits.Add(type, unit);
            
            //Добавляем в As словарь
            diContainer.AddToAsDic(unit, injectMode);
        
            diContainer.InvokeBindNewInstance(unit);
            return unit;
        }
        public static ContainerUnit BindMulti<T>(this DiContainer diContainer, InjectMode injectMode = InjectMode.All, bool bindSelf = false)
        {
            return diContainer.BindMulti(typeof(T), injectMode);
        }
        public static ContainerUnit BindMulti(this DiContainer diContainer, Type type, InjectMode injectMode = InjectMode.All, bool bindSelf = false)
        {
            var unit = new ContainerUnit(diContainer, type, injectMode)
            {
                BindingType = BindingType.Multy
            };
            
            diContainer.AllUnits.Add(unit);
            
            //Добавляем в multi словарь
            diContainer.AddToMultyDic(unit);
            
            //Добавляем в as словарь
            diContainer.AddToAsDic(unit, injectMode);
            
            diContainer.InvokeBindNewInstance(unit);
            
            if (!diContainer.ContainerSingleUnits.ContainsKey(type))
                diContainer.ContainerSingleUnits.Add(type, unit);
            
            return unit;
        }
        public static ContainerUnit BindInstanceMulti<T>(this DiContainer diContainer, T instance, InjectMode injectMode = InjectMode.All)
        {
            return BindInstanceMulti(diContainer, typeof(T), instance, injectMode);
        }
        public static ContainerUnit BindInstanceMulti(this DiContainer diContainer, Type type, object instance, InjectMode injectMode = InjectMode.All)
        {
            var unit = new ContainerUnit(diContainer, type, injectMode, instance)
            {
                BindingType = BindingType.Single
            };
            diContainer.AllUnits.Add(unit);
            
            //Добавляем в multi словарь
            diContainer.AddToMultyDic(unit);
            
            //Добавляем в as словарь
            diContainer.AddToAsDic(unit, injectMode);
            
            diContainer.InvokeBindNewInstance(unit);
            return unit;
        }
        private static void AddToAsDic(this DiContainer diContainer, ContainerUnit unit, InjectMode injectMode)
        {
            var interfaces = unit.Interfaces;
            var containerAs = diContainer.ContainerAsTypeUnits;
         
            if (injectMode == InjectMode.InterfacesAndSelf || injectMode == InjectMode.All)
                foreach (var i in interfaces)
                    AddType(i);
            if ((injectMode == InjectMode.BaseTypeAndSelf || injectMode == InjectMode.All)
                && unit.BaseType != null
                && unit.BaseType != typeof(object))
                AddType(unit.BaseType);
            
            void AddType(Type type)
            {
                if (!containerAs.ContainsKey(type))
                    containerAs.Add(type, new List<ContainerUnit>{unit});
                else
                    containerAs[type].Add(unit);
            }
        }

        private static void AddToMultyDic(this DiContainer diContainer, ContainerUnit unit)
        {
            diContainer.AddToMultyDic(unit, unit.Type);
        }
        private static void AddToMultyDic(this DiContainer diContainer, ContainerUnit unit, Type type)
        {
            if (diContainer.ContainerMultiUnits.ContainsKey(type))
                diContainer.ContainerMultiUnits[type].Add(unit);
            else
                diContainer.ContainerMultiUnits.Add(type, new List<ContainerUnit>{unit});
        }
    }
}