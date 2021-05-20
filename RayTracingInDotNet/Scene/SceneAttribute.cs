using System;

namespace RayTracingInDotNet.Scene
{
	[AttributeUsage(AttributeTargets.Class)]
    public class SceneAttribute : Attribute
	{
        public string Name { get; init; }

        public SceneAttribute(string name)
        {
            this.Name = name;
        }
    }
}
