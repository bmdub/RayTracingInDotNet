using System;
using System.Collections.Generic;
using System.Linq;

namespace RayTracingInDotNet.Scene
{
	static class Scenes
	{
		static Scenes()
		{
			var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
				.Where(x => typeof(IScene).IsAssignableFrom(x) && !x.IsInterface && !x.IsAbstract)
				.OrderBy(type => type.Name)
				.ToList();

			List<SceneMetaData> list = new List<SceneMetaData>();
			foreach (var type in types)
			{
				var att = type.GetCustomAttributes(typeof(SceneAttribute), true).Cast<SceneAttribute>().FirstOrDefault();
				if (att == null) continue;

				list.Add(new SceneMetaData(att.Name, type));
			}

			MetaData = list;
		}

		public static IReadOnlyList<SceneMetaData> MetaData { get; private set; }

		public record SceneMetaData(string Name, Type Type)
		{
			public IScene Instantiate() =>
				Activator.CreateInstance(Type) as IScene;
		}
	}
}
