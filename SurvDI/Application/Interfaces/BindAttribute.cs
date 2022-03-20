using System;
using JetBrains.Annotations;
using SurvDI.Core.Container;

namespace SurvDI.Application.Interfaces
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