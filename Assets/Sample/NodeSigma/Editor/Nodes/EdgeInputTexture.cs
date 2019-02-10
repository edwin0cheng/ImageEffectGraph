using UnityEditor.ShaderGraph;

namespace NodeSigma.Nodes.Editor
{
    [Title("NodeSigma", "Input", "Edge Texture")]
    public class EdgeTextureNode : BaseUnityTextureNode
    {
        public EdgeTextureNode() : base("_EdgeRTDepth")
        {
            name = "Edge Texture";
        }
    }
}
