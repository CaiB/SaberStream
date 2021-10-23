#version 460
layout (location = 0) in vec2 aPosition;
layout (location = 1) in vec2 aTexCoord;

layout (location = 0) uniform mat4 model;
layout (location = 1) uniform mat4 projection;

out vec2 TexCoord;

void main()
{
    gl_Position = projection * model * vec4(aPosition, 0.0, 1.0);
	TexCoord = aTexCoord;
}