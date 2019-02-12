#if UNITY_EDITOR
using System;
using System.Diagnostics;

namespace ImageEffectGraph.Editor
{
    [Conditional("UNITY_CCU")]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    sealed class OptionalDependencyAttribute : Attribute
    {
        public string dependentClass;
        public string define;

        public OptionalDependencyAttribute(string dependentClass, string define)
        {
            this.dependentClass = dependentClass;
            this.define = define;
        }
    }
}
#endif