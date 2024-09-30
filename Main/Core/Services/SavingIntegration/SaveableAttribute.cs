using System;
using JetBrains.Annotations;

namespace SurvDI.Core.Services.SavingIntegration
{
    [AttributeUsage(AttributeTargets.Field)]
    [MeansImplicitUse(ImplicitUseKindFlags.Assign)]
    public class SaveableAttribute : Attribute
    {
        
    }
}