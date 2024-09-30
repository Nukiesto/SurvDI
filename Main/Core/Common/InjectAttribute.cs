using System;
using JetBrains.Annotations;

namespace SurvDI.Core.Common
{
    public abstract class InjectBaseAttribute : Attribute
    {
        public string Id = "";
    }
    
    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class InjectAttribute : InjectBaseAttribute
    {
       
    }

    /// <summary>
    /// Тип должен быть List[T]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class InjectMultiAttribute : InjectBaseAttribute
    {
        
    }
}