struct FragmentIn
{
    float4 fPosition : SV_Position;
    float4 fNormal : NORMAL0;
    float2 fTexture : TEXCOORD0;
    uint1 fBone : POSITION1;
};

Texture2D uTexture;
Texture2D uLightmap;
SamplerState uSampler;

float4 main(FragmentIn input) : SV_Target0
{
    float4 tex = uTexture.Sample(uSampler, input.fTexture);
    if (tex.a < 0.05) discard;

    return tex;
}
