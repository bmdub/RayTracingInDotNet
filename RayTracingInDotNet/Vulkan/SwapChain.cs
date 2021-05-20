using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using VkImage = Silk.NET.Vulkan.Image;

namespace RayTracingInDotNet.Vulkan
{
	class SwapChain : IDisposable
	{
		private readonly Api _api;
		private readonly SwapchainKHR _swapChainKhr;
		private readonly uint _minImageCount;
		private readonly PresentModeKHR _presentMode;
		private readonly Format _format;
		private readonly Extent2D _extent;
		private readonly List<VkImage> _images;
		private readonly List<ImageView> _imageViews;
		private bool _disposedValue;

		public unsafe SwapChain(Api api, Window window, Surface surface, PresentModeKHR presentMode)
		{
			_api = api;

			QuerySwapChainSupport(_api.Device.PhysicalDevice, surface.VkServiceKHR, out var details);
			if (details.Formats.Count == 0 || details.PresentModes.Count == 0)
				throw new Exception($"{nameof(SwapChain)}: Empty swap chain support");

			ChooseSwapSurfaceFormat(details.Formats, out var surfaceFormat);
			var actualPresentMode = ChooseSwapPresentMode(details.PresentModes, presentMode);
			ChooseSwapExtent(window, details.Capabilities, out var extent);
			var imageCount = ChooseImageCount(details.Capabilities);

			var createInfo = new SwapchainCreateInfoKHR();
			createInfo.SType = StructureType.SwapchainCreateInfoKhr;
			createInfo.Surface = surface.VkServiceKHR;
			createInfo.MinImageCount = imageCount;
			createInfo.ImageFormat = surfaceFormat.Format;
			createInfo.ImageColorSpace = surfaceFormat.ColorSpace;
			createInfo.ImageExtent = extent;
			createInfo.ImageArrayLayers = 1;
			createInfo.ImageUsage = ImageUsageFlags.ImageUsageColorAttachmentBit | ImageUsageFlags.ImageUsageTransferDstBit;
			createInfo.PreTransform = details.Capabilities.CurrentTransform;
			createInfo.CompositeAlpha = CompositeAlphaFlagsKHR.CompositeAlphaOpaqueBitKhr;
			createInfo.PresentMode = actualPresentMode;
			createInfo.Clipped = true;
			createInfo.OldSwapchain = default;

			Span<uint> queueFamilyIndices = stackalloc uint[]
			{
				api.Device.GraphicsFamilyIndex,
				api.Device.PresentationFamilyIndex,
			};

			if (api.Device.GraphicsFamilyIndex != api.Device.PresentationFamilyIndex)
			{
				createInfo.ImageSharingMode = SharingMode.Concurrent;
				createInfo.QueueFamilyIndexCount = 2;
				createInfo.PQueueFamilyIndices = (uint*)Unsafe.AsPointer(ref queueFamilyIndices[0]);
			}
			else
			{
				createInfo.ImageSharingMode = SharingMode.Exclusive;
				createInfo.QueueFamilyIndexCount = 0; // Optional
				createInfo.PQueueFamilyIndices = null; // Optional
			}

			Util.Verify(_api.KhrSwapchain.CreateSwapchain(_api.Device.VkDevice, createInfo, default, out _swapChainKhr), $"{nameof(SwapChain)}: Could not create a swap chain");

			_minImageCount = Math.Max(2, details.Capabilities.MinImageCount);
			_presentMode = actualPresentMode;
			_format = surfaceFormat.Format;
			_extent = extent;
			_images = Enumerate.Get<Silk.NET.Vulkan.Device, SwapchainKHR, VkImage>(_api.Device.VkDevice, _swapChainKhr, (d, s, count, values) =>
							_api.KhrSwapchain.GetSwapchainImages(d, s, (uint*)count, (VkImage*)values));
			_imageViews = new List<ImageView>(_images.Count);

			foreach (var image in _images)
				_imageViews.Add(new ImageView(_api, image, _format, ImageAspectFlags.ImageAspectColorBit));

			for(int i=0; i<_images.Count; i++)
			{
				_api.SetDebugName(_images[i].Handle, $"Swapchain Image #{i}", ObjectType.Image);
				_api.SetDebugName(_imageViews[i].VkImageView.Handle, $"Swapchain ImageView #{i}", ObjectType.ImageView);
			}
		}

		public SwapchainKHR VkSwapChain => _swapChainKhr;
		public Extent2D Extent => _extent;
		public List<ImageView> ImageViews => _imageViews;
		public List<VkImage> VkImages => _images;
		public Format Format => _format;
		public uint MinImageCount => _minImageCount;

		struct SupportDetails
		{
			public SurfaceCapabilitiesKHR Capabilities;
			public List<SurfaceFormatKHR> Formats;
			public List<PresentModeKHR> PresentModes;
		};

		private unsafe void QuerySwapChainSupport(in PhysicalDevice physicalDevice, in SurfaceKHR surface, out SupportDetails details)
		{
			details = new SupportDetails();

			_api.KhrSurface.GetPhysicalDeviceSurfaceCapabilities(physicalDevice, surface, out details.Capabilities);

			details.Formats = Enumerate.Get<PhysicalDevice, SurfaceKHR, SurfaceFormatKHR>(physicalDevice, surface, (d, s, count, values) =>
				_api.KhrSurface.GetPhysicalDeviceSurfaceFormats(d, s, (uint*)count, (SurfaceFormatKHR*)values));

			details.PresentModes = Enumerate.Get<PhysicalDevice, SurfaceKHR, PresentModeKHR>(physicalDevice, surface, (d, s, count, values) =>
				_api.KhrSurface.GetPhysicalDeviceSurfacePresentModes(d, s, (uint*)count, (PresentModeKHR*)values));
		}

		private void ChooseSwapSurfaceFormat(List<SurfaceFormatKHR> formats, out SurfaceFormatKHR surfaceFormat)
		{
			if (formats.Count == 1 && formats[0].Format == Format.Undefined)
			{
				surfaceFormat = new SurfaceFormatKHR(Format.B8G8R8A8Unorm, ColorSpaceKHR.ColorspaceSrgbNonlinearKhr);
				return;
			}

			foreach (var format in formats)
				if (format.Format == Format.B8G8R8A8Unorm && format.ColorSpace == ColorSpaceKHR.ColorspaceSrgbNonlinearKhr)
				{
					surfaceFormat = format;
					return;
				}

			throw new Exception($"{nameof(SwapChain)}: No suitable surface format found");
		}

		private PresentModeKHR ChooseSwapPresentMode(List<PresentModeKHR> presentModes, PresentModeKHR presentMode)
		{
			// VK_PRESENT_MODE_IMMEDIATE_KHR specifies that the presentation engine does not wait for a vertical blanking period
			// to update the current image, meaning this mode may result in visible tearing. No internal queuing of presentation
			// requests is needed, as the requests are applied immediately.

			// VK_PRESENT_MODE_MAILBOX_KHR specifies that the presentation engine waits for the next vertical blanking period to
			// update the current image. Tearing cannot be observed. An internal single-entry queue is used to hold pending
			// presentation requests. If the queue is full when a new presentation request is received, the new request replaces
			// the existing entry, and any images associated with the prior entry become available for re-use by the application.
			// One request is removed from the queue and processed during each vertical blanking period in which the queue is non-empty.

			// VK_PRESENT_MODE_FIFO_KHR specifies that the presentation engine waits for the next vertical blanking period to update
			// the current image. Tearing cannot be observed. An internal queue is used to hold pending presentation requests.
			// New requests are appended to the end of the queue, and one request is removed from the beginning of the queue and
			// processed during each vertical blanking period in which the queue is non-empty. This is the only value of presentMode
			// that is required to be supported.

			// VK_PRESENT_MODE_FIFO_RELAXED_KHR specifies that the presentation engine generally waits for the next vertical blanking
			// period to update the current image. If a vertical blanking period has already passed since the last update of the current
			// image then the presentation engine does not wait for another vertical blanking period for the update, meaning this mode
			// may result in visible tearing in this case. This mode is useful for reducing visual stutter with an application that will
			// mostly present a new image before the next vertical blanking period, but may occasionally be late, and present a new
			// image just after the next vertical blanking period. An internal queue is used to hold pending presentation requests.
			// New requests are appended to the end of the queue, and one request is removed from the beginning of the queue and
			// processed during or after each vertical blanking period in which the queue is non-empty.

			switch (presentMode)
			{
				case PresentModeKHR.PresentModeImmediateKhr:
				case PresentModeKHR.PresentModeMailboxKhr:
				case PresentModeKHR.PresentModeFifoKhr:
				case PresentModeKHR.PresentModeFifoRelaxedKhr:
					if (presentModes.Any(mode => mode == presentMode))
						return presentMode;
					break;
				default:
					throw new Exception($"{nameof(SwapChain)}: Unknown present mode: {presentMode}");
			}

			// Fallback
			return PresentModeKHR.PresentModeFifoKhr;
		}

		private void ChooseSwapExtent(Window window, in SurfaceCapabilitiesKHR capabilities, out Extent2D extent)
		{
			// Vulkan tells us to match the resolution of the window by setting the width and height in the currentExtent member.
			// However, some window managers do allow us to differ here and this is indicated by setting the width and height in
			// currentExtent to a special value: the maximum value of uint32_t. In that case we'll pick the resolution that best 
			// matches the window within the minImageExtent and maxImageExtent bounds.
			if (capabilities.CurrentExtent.Width != uint.MaxValue)
			{
				extent = capabilities.CurrentExtent;
				return;
			}

			extent = new Extent2D((uint)window.FrameBufferWidth, (uint)window.FrameBufferHeight);

			extent.Width = Math.Max(capabilities.MinImageExtent.Width, Math.Min(capabilities.MaxImageExtent.Width, extent.Width));
			extent.Height = Math.Max(capabilities.MinImageExtent.Height, Math.Min(capabilities.MaxImageExtent.Height, extent.Height));
		}

		private uint ChooseImageCount(in SurfaceCapabilitiesKHR capabilities)
		{
			// The implementation specifies the minimum amount of images to function properly
			// and we'll try to have one more than that to properly implement triple buffering.
			// (tanguyf: or not, we can just rely on VK_PRESENT_MODE_MAILBOX_KHR with two buffers)
			uint imageCount = Math.Max(2, capabilities.MinImageCount);

			if (capabilities.MaxImageCount > 0 && imageCount > capabilities.MaxImageCount)
				imageCount = capabilities.MaxImageCount;

			return imageCount;
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					foreach (var image in _imageViews)
						image.Dispose();
				}

				_api.KhrSwapchain.DestroySwapchain(_api.Device.VkDevice, _swapChainKhr, null);
				_disposedValue = true;
			}
		}

		~SwapChain()
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
