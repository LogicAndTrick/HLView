struct FragmentIn
{
    float4 fPosition : SV_Position;
    float4 fNormal : NORMAL0;
    float2 fTexture : TEXCOORD0;
};

Texture2D uTexture;
SamplerState uSampler;

float4 main(FragmentIn input) : SV_Target0
{
    //return float4(input.fTexture.y, 0, 0, 1);
    return uTexture.Sample(uSampler, input.fTexture);
    //return input.fNormal;
    // return float4(1, 1, 0, 1);
}
