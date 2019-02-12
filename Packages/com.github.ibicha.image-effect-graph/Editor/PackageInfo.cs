using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.EditorTests-testable")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests")]
[assembly: InternalsVisibleTo("Unity.VisualEffectGraph.RuntimeTests-testable")]

namespace ImageEffectGraph.Editor
{
    public static class ImageEffectGraphPackageInfo
    {
        static string m_PackagePath;

        // public static string fileSystemPackagePath
        // {
        //     get
        //     {
        //         if (m_PackagePath == null)
        //         {
        //             foreach (var pkg in UnityEditor.PackageManager.Packages.GetAll())
        //             {
        //                 if (pkg.name == "com.unity.visualeffectgraph")
        //                 {
        //                     m_PackagePath = pkg.resolvedPath.Replace("\\", "/");
        //                     break;
        //                 }
        //             }
        //         }
        //         return m_PackagePath;
        //     }
        // }
        public static string assetPackagePath
        {
            get
            {
                return "Packages/com.github.ibicha.image-effect-graph";
            }
        }
    }
}