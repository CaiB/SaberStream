#version 460
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec3 aColour;

out vec4 vertexColour;

layout (location = 0) uniform mat4 model;
layout (location = 1) uniform mat4 projection;

void main()
{
    gl_Position = projection * model * vec4(aPosition, -0.1, 1.0);
	vertexColour = vec4(aColour, 1.0);
	//vertexColour = vec4(0.0, 0.0, 1.0, 1.0);
}