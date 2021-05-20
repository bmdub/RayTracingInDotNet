using Silk.NET.Vulkan;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using VkShaderModule = Silk.NET.Vulkan.ShaderModule;

namespace RayTracingInDotNet.Vulkan
{
	class ShaderModule : IDisposable
	{
		private static readonly IntPtr _mainBytes;

		private readonly Api _api;
		private readonly VkShaderModule _vkShaderModule;
		private bool _disposedValue;

		static unsafe ShaderModule()
		{
			_mainBytes = Marshal.StringToHGlobalAnsi("main");
		}

		public ShaderModule(Api api, string filename) :
			this(api, File.ReadAllBytes(filename))
		{ }

		public unsafe ShaderModule(Api api, byte[] code)
		{
			_api = api;

			fixed (byte* pCode = &code[0])
			{
				var createInfo = new ShaderModuleCreateInfo();
				createInfo.SType = StructureType.ShaderModuleCreateInfo;
				createInfo.CodeSize = (uint)code.Length;
				createInfo.PCode = (uint*)pCode;

				Util.Verify(_api.Vk.CreateShaderModule(_api.Device.VkDevice, createInfo, default, out _vkShaderModule), $"{nameof(ShaderModule)}: Unable to create shader module");
			}
		}

		public VkShaderModule VkShaderModule => _vkShaderModule;

		public unsafe PipelineShaderStageCreateInfo CreateShaderStage(ShaderStageFlags stage)
		{
			var createInfo = new PipelineShaderStageCreateInfo();
			createInfo.SType = StructureType.PipelineShaderStageCreateInfo;
			createInfo.Stage = stage;
			createInfo.Module = _vkShaderModule;
			createInfo.PName = (byte*)_mainBytes;

			return createInfo;
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroyShaderModule(_api.Device.VkDevice, _vkShaderModule, default);
				_disposedValue = true;
			}
		}

		~ShaderModule()
		{
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
