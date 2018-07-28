#version 330 core

in vec4 fPosition;
in vec4 fNormal;
in vec2 fTexture;

out vec4 oFragmentColour;

//uniform sampler2D uTexture;

void main()
{
    //vec4 fragmentColor = texture(uTexture, fTexture);
    //oFragmentColour = fragmentColor;
    oFragmentColour = vec4(1, 0, 0, 1);
}
