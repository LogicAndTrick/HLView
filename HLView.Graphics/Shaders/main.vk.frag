#version 420

layout(location = 0) in vec4 fPosition;
layout(location = 1) in vec4 fNormal;
layout(location = 2) in vec4 fColour;
layout(location = 3) in vec2 fTexture;
layout(location = 4) in vec2 fLightmap;

out vec4 oFragmentColour;

layout(set = 1, binding = 0) uniform texture2D uTexture;
layout(set = 1, binding = 1) uniform texture2D uLightmap;
layout(set = 1, binding = 2) uniform sampler uSampler;

void main()
{
    vec4 tex = texture(sampler2D(uTexture, uSampler), fTexture);
    if (tex.a < 0.1) discard;
    vec4 light = texture(sampler2D(uLightmap, uSampler), fLightmap);
    

    // apply gamma correction on the lightmap only
    float gamma = 1.0 / 2.2;
    light.rgb = pow(light.rgb, vec3(gamma));

    oFragmentColour = tex * light * fColour;
}
