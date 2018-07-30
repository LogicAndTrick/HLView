struct FragmentIn
{
    float4 fPosition : SV_Position;
    float4 fNormal : NORMAL0;
    float4 fColour : COLOR0;
    float2 fTexture : TEXCOORD0;
    float2 fLightmap : TEXCOORD1;
};

Texture2D uTexture;
Texture2D uLightmap;
SamplerState uSampler;

float4 main(FragmentIn input) : SV_Target0
{
    float4 tex = uTexture.Sample(uSampler, input.fTexture);
    if (tex.a < 0.05) discard;
    float4 light = uLightmap.Sample(uSampler, input.fLightmap);

    // apply gamma correction on the lightmap only
    float gamma = 1.0 / 2.2;
    light.rgb = pow(light.rgb, float3(gamma, gamma, gamma));

    return tex * light * input.fColour;
}
