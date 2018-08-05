struct VertexIn
{
    float3 vPosition : POSITION0;
    float3 vNormal : NORMAL0;
    float2 vTexture : TEXCOORD0;
    uint1 vBone : POSITION1;
};

struct FragmentIn
{
    float4 fPosition : SV_Position;
    float4 fNormal : NORMAL0;
    float2 fTexture : TEXCOORD0;
    uint1 fBone : POSITION1;
};

cbuffer Projection
{
    matrix uModel;
    matrix uView;
    matrix uProjection;
};

cbuffer BoneTransforms
{
    matrix uTransforms[128];
};

FragmentIn main(VertexIn input)
{
    matrix tModel = transpose(uModel);
    matrix tView = transpose(uView);
    matrix tProjection = transpose(uProjection);

    FragmentIn output;

    float4 position = float4(input.vPosition, 1);
    float4 normal = float4(input.vNormal, 1);

    matrix bone = transpose(uTransforms[input.vBone.x]);
    position = mul(position, bone);
    normal = mul(normal, bone);

    float4 modelPos = mul(position, tModel);
    float4 cameraPos = mul(modelPos, tView);
    float4 viewportPos = mul(cameraPos, tProjection);

    output.fPosition = viewportPos;
    output.fNormal = normal;
    output.fTexture = input.vTexture;
    output.fBone = input.vBone;

    return output;
}
