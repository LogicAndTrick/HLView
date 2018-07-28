#version 420

in vec4 fPosition;
in vec4 fNormal;
in vec2 fTexture;

out vec4 oFragmentColour;

layout(set = 1, binding = 0) uniform texture2D uTexture;
layout(set = 1, binding = 1) uniform sampler uSampler;

void main()
{
    oFragmentColour = texture(sampler2D(uTexture, uSampler), fTexture);
    //oFragmentColour = fragmentColor;
    //oFragmentColour = fNormal;
}
