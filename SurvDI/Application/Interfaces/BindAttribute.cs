using System;
using JetBrains.Annotations;
using Plugins.SurvDI.Core.Container;

namespace Plugins.SurvDI.Application.Interfaces
{
    [AttributeUsage(AttributeTargets.Class)]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class BindAttribute : Attribute
    {
        public bool Multy = false;
        public InjectMode InjectMode = InjectMode.InterfacesAndSelf;
        public string Id;
    }
}