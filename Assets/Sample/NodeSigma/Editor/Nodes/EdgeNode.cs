using System.Reflection;
using UnityEngine;
using UnityEditor.ShaderGraph;

namespace NodeSigma.Nodes.Editor
{
    [Title("NodeSigma", "Effect", "Edge Detection")]
    public class EdgeNode : CodeFunctionNode
    {
        public EdgeNode()
        {
            name = "Edge Detection";
        }

        // public override string documentationURL
        // {
        //     get { return "https://github.com/Unity-Technologies/ShaderGraph/wiki/Gradient-Noise-Node"; }
        // }

        protected override MethodInfo GetFunctionToConvert()
        {
            return GetType().GetMethod("NodeSigma_GradientNoise", BindingFlags.Static | BindingFlags.NonPublic);
        }

        static string NodeSigma_GradientNoise(
            [Slot(0, Binding.ScreenPosition)] Vector2 ScreenPos,
            [Slot(1, Binding.None)] ColorRGBA Color,
            [Slot(2, Binding.None)] Texture2D DepthTexture,
            [Slot(3, Binding.None)] SamplerState DepthTextureState,
            [Slot(4, Binding.None)] Vector2 DepthTextureTexel,
            [Slot(5, Binding.None)] out Vector4 Out)
        {
            Out = Vector4.zero;

            return @"
{
    Out = nodesigma_edgecolor(ScreenPos, Color, DepthTexture, DepthTextureState, DepthTextureTexel);    
}
";
        }

        public override void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode)
        {            
            registry.ProvideFunction("ns_DecodeFloatRG", s => s.Append(@"
float ns_DecodeFloatRG( float2 enc )
{
    float2 kDecodeDot = float2(1.0, 1/255.0);
    return dot( enc, kDecodeDot );
}
"));

            registry.ProvideFunction("nodesigma_CheckSame", s => s.Append(@"
half nodesigma_CheckSame(half2 centerNormal, float centerDepth, half4 theSample)
{
    float _SensitivityNormals = 0.82;
    // float _SensitivityNormals = 10.0;
    // float _SensitivityDepth = 3.75;
    float _SensitivityDepth = 3.75;

    // differene in normals
    // do not bother decoding normals - there's no need here
    half2 diff = abs(centerNormal - theSample.xy) * _SensitivityNormals;
    int isSameNormal = (diff.x + diff.y) * _SensitivityNormals < 0.1;

    // differenece in depth
    float sampleDepth = ns_DecodeFloatRG(theSample.zw);
    float zdiff = abs(centerDepth - sampleDepth);
    // scale the requireed threshold by the distance
    int isSameDepth = zdiff * _SensitivityDepth < 0.99 * centerDepth;

    // return:
    // 1 - if normals and depth are similar enough
    // 0 - otherwise

    return (isSameNormal * isSameDepth) ? 1.0 : 0.0;

}
"));

            registry.ProvideFunction("nodesigma_edgecolor", s => s.Append(@"
float4 nodesigma_edgecolor(float2 screenUV, float4 color, Texture2D depthTex, SamplerState depthTexState, float2 depthTexelSize)
{
    float _SampleDistance = 2;
    float _Falloff = 10.0;

    float sampleSizeX = depthTexelSize.x;
    float sampleSizeY = depthTexelSize.y;
    float2 _uv2 = screenUV + float2(-sampleSizeX, +sampleSizeY) * _SampleDistance;
    float2 _uv3 = screenUV + float2(+sampleSizeX, -sampleSizeY) * _SampleDistance;
    float2 _uv4 = screenUV + float2( sampleSizeX,  sampleSizeY) * _SampleDistance;
    float2 _uv5 = screenUV + float2(-sampleSizeX, -sampleSizeY) * _SampleDistance;

    half4 center = SAMPLE_TEXTURE2D(depthTex, depthTexState, screenUV);
    half4 sample1 = SAMPLE_TEXTURE2D(depthTex, depthTexState, _uv2);
    half4 sample2 = SAMPLE_TEXTURE2D(depthTex, depthTexState, _uv3);
    half4 sample3 = SAMPLE_TEXTURE2D(depthTex, depthTexState, _uv4);
    half4 sample4 = SAMPLE_TEXTURE2D(depthTex, depthTexState, _uv5);    

    half edge = 1.0;

    // encode normal
    half2 centerNormal = center.xy;
    //decode depth
    float centerDepth = ns_DecodeFloatRG(center.zw);

    // // calculate how faded the edge is
    float d = clamp(centerDepth * _Falloff - 0.05, 0.0, 1.0);    
    half4 depthFade = half4(d, d, d, 1.0);

    // is it an edge? 0 if yes, 1 if no    
    edge *= nodesigma_CheckSame(centerNormal, centerDepth, sample1);
    edge *= nodesigma_CheckSame(centerNormal, centerDepth, sample2);
    edge *= nodesigma_CheckSame(centerNormal, centerDepth, sample3);
    edge *= nodesigma_CheckSame(centerNormal, centerDepth, sample4);

    return edge * color + (1.0 - edge) * depthFade * color;
}
"));
    


            base.GenerateNodeFunction(registry, graphContext, generationMode);
        }
    }
}