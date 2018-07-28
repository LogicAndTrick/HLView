#version 330 core

layout(location = 0) in vec3 vPosition;
layout(location = 1) in vec3 vNormal;
//layout(location = 2) in vec2 vTexture;

out vec4 fPosition;
out vec4 fNormal;
//out vec2 fTexture;

uniform Projection {
    mat4 uModel;
    mat4 uView;
    mat4 uProjection;
};


void main()
{
	vec4 position = vec4(vPosition, 1);
    vec4 normal = vec4(vNormal, 1);
    
	vec4 modelPos = uModel * position;
	vec4 cameraPos = uView * modelPos;
    vec4 viewportPos = uProjection * cameraPos;
    
	fPosition = position;
    fNormal = normal;
//    fTexture = vTexture;
    
	gl_Position = viewportPos;
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates
}