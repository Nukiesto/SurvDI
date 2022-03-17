using System;
using JetBrains.Annotations;

namespace Plugins.SurvDI.Core.Common
{
    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class InjectAttribute : Attribute
    {
        public string Id = "";
    }

    /// <summary>
    /// Тип должен быть List[T]
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class InjectMultiAttribute : Attribute
    {
        public string Id = "";
    }
}