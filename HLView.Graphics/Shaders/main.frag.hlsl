struct FragmentIn
{
    float4 fPosition : SV_Position;
    float4 fNormal : NORMAL0;
    //float2 fTexture : TEXCOORD0;
};

float4 main(FragmentIn input) : SV_Target0
{
    return input.fNormal;
    // return float4(1, 1, 0, 1);
}
