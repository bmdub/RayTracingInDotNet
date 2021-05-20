using System;

namespace ShaderCompiler
{
	public class Program
	{
		static void Main(string[] args)
		{
			var task = ShaderCompiler.Compile(args[0], args[1]);

			task.Wait();

			Console.WriteLine("Shader compilation finished successfully.");
		}
	}
}
