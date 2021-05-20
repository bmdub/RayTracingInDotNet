using Silk.NET.Vulkan;
using System;
using VkSampler = Silk.NET.Vulkan.Sampler;

namespace RayTracingInDotNet.Vulkan
{
	class Sampler : IDisposable
	{
		private readonly Api _api;
		private readonly VkSampler _vkSampler;
		private bool _disposedValue;

		public unsafe Sampler(Api api, SamplerConfig config)
		{
			_api = api;

			// Wrap textures
			// TODO: make texture-specific
			config.AddressModeU = SamplerAddressMode.Repeat;
			config.AddressModeV = SamplerAddressMode.Repeat;
			config.AddressModeW = SamplerAddressMode.Repeat;

			var samplerInfo = new SamplerCreateInfo();
			samplerInfo.SType = StructureType.SamplerCreateInfo;
			samplerInfo.MagFilter = config.MagFilter;
			samplerInfo.MinFilter = config.MinFilter;
			samplerInfo.AddressModeU = config.AddressModeU;
			samplerInfo.AddressModeV = config.AddressModeV;
			samplerInfo.AddressModeW = config.AddressModeW;
			samplerInfo.AnisotropyEnable = config.AnisotropyEnable;
			samplerInfo.MaxAnisotropy = config.MaxAnisotropy;
			samplerInfo.BorderColor = config.BorderColor;
			samplerInfo.UnnormalizedCoordinates = config.UnnormalizedCoordinates;
			samplerInfo.CompareEnable = config.CompareEnable;
			samplerInfo.CompareOp = config.CompareOp;
			samplerInfo.MipmapMode = config.MipmapMode;
			samplerInfo.MipLodBias = config.MipLodBias;
			samplerInfo.MinLod = config.MinLod;
			samplerInfo.MaxLod = config.MaxLod;

			Util.Verify(_api.Vk.CreateSampler(_api.Device.VkDevice, samplerInfo, default, out _vkSampler), $"{nameof(Sampler)}: Failed to create sampler");
		}

		public VkSampler VkSampler => _vkSampler;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.Vk.DestroySampler(_api.Device.VkDevice, _vkSampler, default);
				_disposedValue = true;
			}
		}

		~Sampler()
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
