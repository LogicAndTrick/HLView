struct VertexIn
{
    float3 vPosition : POSITION0;
    float3 vNormal : NORMAL0;
    float4 vColour : COLOR0;
    float2 vTexture : TEXCOORD0;
    float2 vLightmap : TEXCOORD1;
};

struct FragmentIn
{
    float4 fPosition : SV_Position;
    float4 fNormal : NORMAL0;
    float4 fColour : COLOR0;
    float2 fTexture : TEXCOORD0;
    float2 fLightmap : TEXCOORD1;
};

cbuffer Projection
{
    matrix uModel;
    matrix uView;
    matrix uProjection;
};

FragmentIn main(VertexIn input)
{
    matrix tModel = transpose(uModel);
    matrix tView = transpose(uView);
    matrix tProjection = transpose(uProjection);

    FragmentIn output;

    float4 position = float4(input.vPosition, 1);
    float4 normal = float4(input.vNormal, 1);

    float4 modelPos = mul(position, tModel);
    float4 cameraPos = mul(modelPos, tView);
    float4 viewportPos = mul(cameraPos, tProjection);

    output.fPosition = viewportPos;
    output.fNormal = normal;
    output.fColour = input.vColour;
    output.fTexture = input.vTexture;
    output.fLightmap = input.vLightmap;

    return output;
}
