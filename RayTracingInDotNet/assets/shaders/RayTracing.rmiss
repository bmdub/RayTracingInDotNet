#version 460
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_ray_tracing : require
#include "RayPayload.glsl"
#include "UniformBufferObject.glsl"

layout(binding = 3) readonly uniform UniformBufferObjectStruct { UniformBufferObject Camera; };

layout(location = 0) rayPayloadInEXT RayPayload Ray;

void main()
{
	// Sky color
	const float t = 0.5*(normalize(gl_WorldRayDirectionEXT).y + 1);
	const vec3 skyColor =  mix(vec3(Camera.SkyColor1), vec3(Camera.SkyColor2), t);

	Ray.ColorAndDistance = vec4(skyColor, -1);
}
