using RunProcessAsTask;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ShaderCompiler
{
	public static class ShaderCompiler
	{
		static readonly string[] ShaderExtensions = new string[]
		{
			".vert", ".frag", ".rchit", ".rint", ".rchit", ".rgen", ".rmiss"
		};

		static SemaphoreSlim _throttleSemaphore = new SemaphoreSlim(Environment.ProcessorCount, Environment.ProcessorCount);

		public static async Task Compile(string baseFolder, string outputDir)
		{
			Directory.CreateDirectory(outputDir);

			var tasks = new List<Task>();

			foreach (var extension in ShaderExtensions)
			{
				foreach (var shaderPath in Directory.GetFiles(baseFolder, $"*{extension}", SearchOption.AllDirectories))
				{
					await _throttleSemaphore.WaitAsync();

					tasks.Add(CompileFile(shaderPath, outputDir));
				}
			}

			await Task.WhenAll(tasks);
		}

		static async Task CompileFile(string codeFilePath, string outputDir)
		{
			try
			{
				var spvFile = Path.Combine(outputDir, Path.GetFileName(codeFilePath) + ".spv");

				const string command = "glslangValidator";
				var commandArgs = $"--target-env vulkan1.2 -V \"{codeFilePath}\" -o \"{spvFile}\"";

				var startInfo = new ProcessStartInfo(command, commandArgs);
				startInfo.WorkingDirectory = outputDir;

				ProcessResults processResults = default;
				try
				{
					processResults = await ProcessEx.RunAsync(startInfo);
				}
				catch (Exception ex) when (ex.Message.Contains("find"))
				{
					throw new Exception($"Error running the shader compiler. Please make sure that the Vulkan SDK is installed, and the {command} command's path is visible in your PATH environment variable.", ex);
				}
				catch (Exception ex)
				{
					throw new Exception($"Error running the shader compiler.", ex);
				}

				Console.WriteLine($"{command} {commandArgs}{Environment.NewLine} {string.Join(Environment.NewLine, processResults.StandardOutput)} {Environment.NewLine}");
				if (processResults.ExitCode != 0)
					Console.WriteLine($"{command} {commandArgs}{Environment.NewLine} {string.Join(Environment.NewLine, processResults.StandardError)} {Environment.NewLine}");

				if (processResults.ExitCode != 0)
					throw new Exception($"Error compiling shader {codeFilePath}: {Environment.NewLine} {string.Join(Environment.NewLine, processResults.StandardError)}");
			}
			finally
			{
				_throttleSemaphore.Release();
			}
		}
	}
}
