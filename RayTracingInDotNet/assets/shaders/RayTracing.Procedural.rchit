#version 460
#extension GL_EXT_nonuniform_qualifier : require
#extension GL_GOOGLE_include_directive : require
#extension GL_EXT_ray_tracing : require
#extension GL_EXT_debug_printf : enable
#include "Material.glsl"

layout(binding = 4) readonly buffer VertexArray { float Vertices[]; };
layout(binding = 5) readonly buffer IndexArray { uint Indices[]; };
layout(binding = 6) readonly buffer MaterialArray { Material[] Materials; };
layout(binding = 7) readonly buffer OffsetArray { uvec2[] Offsets; };
layout(binding = 8) readonly buffer TransformArray { mat4[] Transforms; };
layout(binding = 9) uniform sampler2D[] TextureSamplers;
layout(binding = 10) readonly buffer SphereArray { vec4[] Spheres; };

#include "Scatter.glsl"
#include "Vertex.glsl"

hitAttributeEXT vec4 Sphere;
rayPayloadInEXT RayPayload Ray;

vec2 GetSphereTexCoord(const vec3 point)
{
	const float phi = atan(point.x, point.z);
	const float theta = asin(point.y);
	const float pi = 3.1415926535897932384626433832795;

	return vec2
	(
		(phi + pi) / (2* pi),
		1 - (theta + pi /2) / pi
	);
}

void main()
{
	// Ignore re-collisions with same object, which causes artifacts.
	if(Ray.PrevInstanceID == gl_InstanceID)
		return;

	// Get the material.
	const uvec2 offsets = Offsets[gl_InstanceCustomIndexEXT];
	const mat4 transform = Transforms[gl_InstanceCustomIndexEXT];
	mat4 transformRot = transform;
	transformRot[3].x=0;
	transformRot[3].y=0;
	transformRot[3].z=0;
	transformRot[3].w=0;
	transformRot = transpose(transformRot);
	const uint indexOffset = offsets.x;
	const uint vertexOffset = offsets.y;
	const Vertex v0 = UnpackVertex(vertexOffset + Indices[indexOffset], transform);
	const Material material = Materials[v0.MaterialIndex];

	// Compute the ray hit point properties.
	const vec4 sphere = Spheres[gl_InstanceCustomIndexEXT];
	const vec3 center = vec3(transform * vec4(sphere.xyz, 1));
	const float radius = sphere.w;
	vec3 point = gl_WorldRayOriginEXT + gl_HitTEXT * gl_WorldRayDirectionEXT;
	vec3 normal = (point - center) / radius;
	const vec2 texCoord = GetSphereTexCoord(vec3(transformRot * vec4(normal, 1)));
	
	Ray = Scatter(material, gl_WorldRayDirectionEXT, normal, texCoord, gl_HitTEXT, Ray.RandomSeed);	
	
	Ray.PrevInstanceID = gl_InstanceID;

	//debugPrintfEXT("%d", gl_InstanceCustomIndexEXT);	
}
