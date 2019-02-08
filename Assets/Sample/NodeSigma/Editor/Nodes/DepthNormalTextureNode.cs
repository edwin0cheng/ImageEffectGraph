using UnityEditor.ShaderGraph;

namespace NodeSigma.Nodes.Editor
{
    [Title("Input", "NodeSigma", "Depth Texture")]
    public class DepthTextureNode : BaseUnityTextureNode
    {
        public DepthTextureNode() : base("_GlobalEdgeTex")
        {
            name = "Depth Normal Texture";
        }
    }
}
