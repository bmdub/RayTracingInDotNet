using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.ImGui;
using System;

namespace RayTracingInDotNet.Vulkan
{
	class UserInterface : IDisposable
	{
		private readonly SwapChain _swapChain;
		private readonly ImGuiController _imGuiController;
		private bool _disposedValue;

		public unsafe UserInterface(Api api, Window window, SwapChain swapChain, DepthBuffer depthBuffer)
		{
			_swapChain = swapChain;

			// Initialise ImGui
			_imGuiController = new ImGuiController(api.Vk, 
				window.IWindow, 
				window.InputContext, 
				new ImGuiFontConfig("./assets/fonts/Cousine-Regular.ttf", 13), 
				api.Device.PhysicalDevice, 
				api.Device.GraphicsFamilyIndex, 
				swapChain.VkImages.Count, 
				swapChain.Format, 
				depthBuffer.Format);
		}

		public unsafe void Update(double timeDelta)
		{
			_imGuiController.Update((float)timeDelta);
		}

		public unsafe void Render(CommandBuffer commandBuffer, Framebuffer frameBuffer)
		{
			_imGuiController.Render(commandBuffer, frameBuffer.VkFrameBuffer, _swapChain.Extent);
		}		

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_imGuiController.Dispose();
				}

				_disposedValue = true;
			}
		}

		~UserInterface()
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
