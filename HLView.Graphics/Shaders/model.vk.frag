#version 420

layout(location = 0) out vec4 fPosition;
layout(location = 1) out vec4 fNormal;
layout(location = 2) out vec2 fTexture;
layout(location = 3) out uint fBone;

out vec4 oFragmentColour;

layout(set = 1, binding = 0) uniform texture2D uTexture;
layout(set = 1, binding = 1) uniform texture2D uLightmap;
layout(set = 1, binding = 2) uniform sampler uSampler;

void main()
{
    vec4 tex = texture(sampler2D(uTexture, uSampler), fTexture);
    if (tex.a < 0.05) discard;

    oFragmentColour = tex;
}
