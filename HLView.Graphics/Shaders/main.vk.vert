#version 420

layout(location = 0) in vec3 vPosition;
layout(location = 1) in vec3 vNormal;
layout(location = 2) in vec4 vColour;
layout(location = 3) in vec2 vTexture;
layout(location = 4) in vec2 vLightmap;

layout(location = 0) out vec4 fPosition;
layout(location = 1) out vec4 fNormal;
layout(location = 2) out vec4 fColour;
layout(location = 3) out vec2 fTexture;
layout(location = 4) out vec2 fLightmap;

layout(set = 0, binding = 0) uniform Projection {
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
    fColour = vColour;
    fTexture = vTexture;
    fLightmap = vLightmap;
    
	gl_Position = viewportPos;
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates
}