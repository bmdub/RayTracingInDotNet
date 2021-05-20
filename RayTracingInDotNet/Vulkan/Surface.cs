using Silk.NET.Vulkan;
using System;

namespace RayTracingInDotNet.Vulkan
{
	class Surface : IDisposable
	{
		private readonly Api _api;
		private readonly Instance _instance;
		private readonly SurfaceKHR _vkSurfaceKHR;
		private bool _disposedValue;

		public unsafe Surface(Api api, Window window, Instance instance)
		{
			(_api, _instance) = (api, instance);

			_vkSurfaceKHR = window.IWindow.VkSurface.Create<AllocationCallbacks>(_instance.VkInstance.ToHandle(), null).ToSurface();
		}

		public SurfaceKHR VkServiceKHR => _vkSurfaceKHR;

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				_api.KhrSurface.DestroySurface(_instance.VkInstance, _vkSurfaceKHR, null);
				_disposedValue = true;
			}
		}

		~Surface()
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
