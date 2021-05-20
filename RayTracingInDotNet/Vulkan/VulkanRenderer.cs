using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using RayTracingInDotNet.Scene;
using Serilog;
using Silk.NET.Core;
using Silk.NET.Vulkan;
using VkCommandBuffer = Silk.NET.Vulkan.CommandBuffer;
using VkSemaphore = Silk.NET.Vulkan.Semaphore;

namespace RayTracingInDotNet.Vulkan
{
	class VulkanRenderer : IRenderer
	{
		public event DrawGuiDelegate DrawGui;

		private readonly Window _window;
		private readonly Api _api;
		private readonly UserSettings _userSettings;
		private readonly Surface _surface;
		private readonly CommandPool _commandPool;
		private readonly List<Semaphore> _imageAvailableSemaphores = new List<Semaphore>();
		private readonly List<Semaphore> _renderFinishedSemaphores = new List<Semaphore>();
		private readonly List<Fence> _inFlightFences = new List<Fence>();
		private readonly List<UniformBuffer> _uniformBuffers = new List<UniformBuffer>();
		private readonly RayTracingProperties _rayTracingProperties;
		private readonly List<BottomLevelAccelerationStructure> _bottomAs = new List<BottomLevelAccelerationStructure>();
		private readonly List<TopLevelAccelerationStructure> _topAs = new List<TopLevelAccelerationStructure>();
		private Matrix4x4[] _transforms;
		private IScene _scene;
		private UserSettings _previousSettings;
		private PresentModeKHR _presentMode;
		private SwapChain _swapChain;
		private DepthBuffer _depthBuffer;
		private GraphicsPipeline _graphicsPipeline;
		private List<Framebuffer> _swapChainFramebuffers;
		private CommandBuffers _commandBuffers;
		private RayTracingPipeline _rayTracingPipeline;
		private ShaderBindingTable _shaderBindingTable;
		private VulkanScene _vulkanScene;
		private int _currentFrame;
		private bool _resetAccumulation;
		private UserInterface _userInterface;
		private Buffer _bottomBuffer;
		private DeviceMemory _bottomBufferMemory;
		private Buffer _bottomScratchBuffer;
		private DeviceMemory _bottomScratchBufferMemory;
		private Buffer _topBuffer;
		private DeviceMemory _topBufferMemory;
		private Buffer _topScratchBuffer;
		private DeviceMemory _topScratchBufferMemory;
		private Buffer _instancesBuffer;
		private DeviceMemory _instancesBufferMemory;
		private Image _accumulationImage;
		private DeviceMemory _accumulationImageMemory;
		private ImageView _accumulationImageView;
		private Image _outputImage;
		private DeviceMemory _outputImageMemory;
		private ImageView _outputImageView;
		private AccelerationStructureInstanceKHR[] _instances;
		private bool _disposedValue;

		public unsafe VulkanRenderer(UserSettings userSettings, Window window, IScene scene, CameraInitialState cameraInitialState, ILogger logger, bool enableDebugLogging)
		{
			_userSettings = userSettings;
			_window = window;

			_api = new Api(enableDebugLogging, logger);

			_presentMode = _userSettings.VSync ? PresentModeKHR.PresentModeFifoKhr : PresentModeKHR.PresentModeImmediateKhr;

			_api.Instance = new Instance(_api, _window, new Version32(1, 2, 0));
			_surface = new Surface(_api, _window, _api.Instance);

			// Find the vulkan device we want
			var physicalDevice = _api.Instance.PhysicalDevices.Where(d =>
			{
				_api.Vk.GetPhysicalDeviceFeatures(d, out var deviceFeatures);

				if (deviceFeatures.GeometryShader == false)
					return false;

				var queueFamilies = Enumerate.Get<PhysicalDevice, QueueFamilyProperties>(d, (device, count, values) =>
					_api.Vk.GetPhysicalDeviceQueueFamilyProperties(device, (uint*)count, (QueueFamilyProperties*)values));

				for (var i = 0; i < queueFamilies.Count; i++)
				{
					if (queueFamilies[i].QueueCount > 0 && queueFamilies[i].QueueFlags.HasFlag(QueueFlags.QueueGraphicsBit))
						return true;
				}

				return false;
			}).FirstOrDefault();

			if (physicalDevice.Handle == 0)
				throw new Exception($"{nameof(VulkanRenderer)}: Could not find a suitable graphics device.");

			var deviceProps = new PhysicalDeviceProperties2();
			deviceProps.SType = StructureType.PhysicalDeviceProperties2;

			_api.Vk.GetPhysicalDeviceProperties2(physicalDevice, &deviceProps);

			_api.Logger.Debug($"{nameof(VulkanRenderer)}: Setting physical device: {deviceProps.Properties.DeviceID} ({Marshal.PtrToStringAnsi((nint)deviceProps.Properties.DeviceName)})");

			// Opt-in into mandatory device features.
			var shaderClockFeatures = new PhysicalDeviceShaderClockFeaturesKHR();
			shaderClockFeatures.SType = StructureType.PhysicalDeviceShaderClockFeaturesKhr;
			shaderClockFeatures.PNext = null;
			shaderClockFeatures.ShaderSubgroupClock = true;

			var deviceFeatures = new PhysicalDeviceFeatures();
			deviceFeatures.FillModeNonSolid = true;
			deviceFeatures.SamplerAnisotropy = true;
			deviceFeatures.ShaderInt64 = true;

			// Required device features.
			var bufferDeviceAddressFeatures = new PhysicalDeviceBufferDeviceAddressFeatures();
			bufferDeviceAddressFeatures.SType = StructureType.PhysicalDeviceBufferDeviceAddressFeatures;
			bufferDeviceAddressFeatures.PNext = &shaderClockFeatures;
			bufferDeviceAddressFeatures.BufferDeviceAddress = true;

			var indexingFeatures = new PhysicalDeviceDescriptorIndexingFeatures();
			indexingFeatures.SType = StructureType.PhysicalDeviceDescriptorIndexingFeatures;
			indexingFeatures.PNext = &bufferDeviceAddressFeatures;
			indexingFeatures.RuntimeDescriptorArray = true;
			indexingFeatures.ShaderSampledImageArrayNonUniformIndexing = true;

			var accelerationStructureFeatures = new PhysicalDeviceAccelerationStructureFeaturesKHR();
			accelerationStructureFeatures.SType = StructureType.PhysicalDeviceAccelerationStructureFeaturesKhr;
			accelerationStructureFeatures.PNext = &indexingFeatures;
			accelerationStructureFeatures.AccelerationStructure = true;

			var rayTracingFeatures = new PhysicalDeviceRayTracingPipelineFeaturesKHR();
			rayTracingFeatures.SType = StructureType.PhysicalDeviceRayTracingPipelineFeaturesKhr;
			rayTracingFeatures.PNext = &accelerationStructureFeatures;
			rayTracingFeatures.RayTracingPipeline = true;

			_api.Device = new Device(_api, physicalDevice, _surface, deviceFeatures, Unsafe.AsPointer(ref rayTracingFeatures));

			// Now that we have an instance and a device, load all of the extentions we need.
			_api.InitializeExtensions();

			_commandPool = new CommandPool(_api, _api.Device.GraphicsFamilyIndex, true);

			_rayTracingProperties = new RayTracingProperties(_api);

			// Load the scene, and create the swap chain / command buffers
			LoadScene(scene, cameraInitialState);

			_currentFrame = 0;

			_api.Device.WaitIdle();
		}

		public uint NumberOfSamples { get; private set; }
		public uint TotalNumberOfSamples { get; private set; }

		public void ResetAccumulation() =>
			_resetAccumulation = true;

		public unsafe void DrawFrame(double delta, in Matrix4x4 modelView, CameraInitialState cameraInitialState)
		{
			bool updateTransforms = _scene.UpdateTransforms(delta, _userSettings, _transforms);

			if (updateTransforms)
			{
				Util.Submit(_api, _commandPool, commandBuffer =>
				{
					UpdateTopLevelStructures(commandBuffer, _transforms);
				});
			}

			// Note: If we aren't waiting for VBlank, then rays will accumulate faster.
			// Therefore immediate and/or mailbox mode can actually make things look better.
			if (_presentMode == PresentModeKHR.PresentModeFifoKhr && _userSettings.VSync == false)
			{
				_presentMode = PresentModeKHR.PresentModeImmediateKhr;
				RecreateSwapChain();
				return;
			}
			else if (_presentMode == PresentModeKHR.PresentModeImmediateKhr && _userSettings.VSync == true)
			{
				_presentMode = PresentModeKHR.PresentModeFifoKhr;
				RecreateSwapChain();
				return;
			}

			// Check if the accumulation buffer needs to be reset.
			if (_resetAccumulation || _userSettings.RequiresAccumulationReset(_previousSettings) || !_userSettings.AccumulateRays || updateTransforms)
			{
				TotalNumberOfSamples = 0;
				_resetAccumulation = false;
			}

			// Clone the settings
			_previousSettings = _userSettings with { };

			// Keep track of our sample count.
			NumberOfSamples = Math.Clamp(_userSettings.MaxNumberOfSamples - TotalNumberOfSamples, 0u, _userSettings.NumberOfSamples);
			TotalNumberOfSamples += NumberOfSamples;

			const ulong noTimeout = ulong.MaxValue;

			var inFlightFence = _inFlightFences[_currentFrame];
			var imageAvailableSemaphore = _imageAvailableSemaphores[_currentFrame].VkSemaphore;
			var renderFinishedSemaphore = _renderFinishedSemaphores[_currentFrame].VkSemaphore;

			inFlightFence.Wait(noTimeout);

			uint imageIndex = 0;
			var result = _api.KhrSwapchain.AcquireNextImage(_api.Device.VkDevice, _swapChain.VkSwapChain, noTimeout, imageAvailableSemaphore, default, ref imageIndex);
			if (result == Result.ErrorOutOfDateKhr || result == Result.SuboptimalKhr)
			{
				RecreateSwapChain();
				return;
			}

			if (result != Result.Success && result != Result.SuboptimalKhr)
				throw new Exception($"{nameof(VulkanRenderer)}: Failed to acquire next image: {result}");

			var commandBuffer = _commandBuffers.Begin(imageIndex);

			Render(commandBuffer, (int)imageIndex, delta);

			_commandBuffers.End(imageIndex);

			UpdateUniformBuffer((int)imageIndex, modelView, cameraInitialState);

			var submitInfo = new SubmitInfo();
			submitInfo.SType = StructureType.SubmitInfo;

			var commandBuffers = stackalloc VkCommandBuffer[] { commandBuffer };
			var waitSemaphores = stackalloc VkSemaphore[] { imageAvailableSemaphore };
			var waitStages = stackalloc PipelineStageFlags[] { PipelineStageFlags.PipelineStageColorAttachmentOutputBit };
			var signalSemaphores = stackalloc VkSemaphore[] { renderFinishedSemaphore };

			submitInfo.WaitSemaphoreCount = 1;
			submitInfo.PWaitSemaphores = waitSemaphores;
			submitInfo.PWaitDstStageMask = waitStages;
			submitInfo.CommandBufferCount = 1;
			submitInfo.PCommandBuffers = commandBuffers;
			submitInfo.SignalSemaphoreCount = 1;
			submitInfo.PSignalSemaphores = signalSemaphores;

			inFlightFence.Reset();

			Util.Verify(_api.Vk.QueueSubmit(_api.Device.GraphicsQueue, 1, submitInfo, inFlightFence.VkFence), $"{nameof(VulkanRenderer)}: Unable to submit the command buffer");

			var swapChains = stackalloc SwapchainKHR[] { _swapChain.VkSwapChain };

			var presentInfo = new PresentInfoKHR();
			presentInfo.SType = StructureType.PresentInfoKhr;
			presentInfo.WaitSemaphoreCount = 1;
			presentInfo.PWaitSemaphores = signalSemaphores;
			presentInfo.SwapchainCount = 1;
			presentInfo.PSwapchains = swapChains;
			presentInfo.PImageIndices = &imageIndex;
			presentInfo.PResults = null; // Optional

			var result2 = _api.KhrSwapchain.QueuePresent(_api.Device.PresentationQueue, presentInfo);

			if (result2 == Result.ErrorOutOfDateKhr || result2 == Result.SuboptimalKhr)
			{
				RecreateSwapChain();
				return;
			}

			if (result2 != Result.Success)
				throw new Exception($"{nameof(VulkanRenderer)}: Failed to present next image: {result2}");

			_currentFrame = (_currentFrame + 1) % _inFlightFences.Count;
		}

		private void Render(in VkCommandBuffer commandBuffer, int imageIndex, double timeDelta)
		{
			// Render the scene
			RenderScene(commandBuffer, imageIndex);

			// Render the UI
			_userInterface.Update(timeDelta);
			DrawGui?.Invoke();
			_userInterface.Render(commandBuffer, _swapChainFramebuffers[imageIndex]);
		}

		private unsafe void RenderScene(in VkCommandBuffer commandBuffer, int imageIndex)
		{
			var extent = _swapChain.Extent;

			var descriptorSets = stackalloc DescriptorSet[] { _rayTracingPipeline.DescriptorSet(imageIndex) };

			var subresourceRange = new ImageSubresourceRange();
			subresourceRange.AspectMask = ImageAspectFlags.ImageAspectColorBit;
			subresourceRange.BaseMipLevel = 0;
			subresourceRange.LevelCount = 1;
			subresourceRange.BaseArrayLayer = 0;
			subresourceRange.LayerCount = 1;

			// Acquire destination images for rendering.
			ImageMemoryBarrier.Insert(_api, commandBuffer, _accumulationImage.VkImage, subresourceRange, 0,
				 AccessFlags.AccessShaderWriteBit, ImageLayout.Undefined, ImageLayout.General);

			ImageMemoryBarrier.Insert(_api, commandBuffer, _outputImage.VkImage, subresourceRange, 0,
				 AccessFlags.AccessShaderWriteBit, ImageLayout.Undefined, ImageLayout.General);

			// Bind ray tracing pipeline.
			_api.Vk.CmdBindPipeline(commandBuffer, PipelineBindPoint.RayTracingKhr, _rayTracingPipeline.VkPipeline);

			_api.Vk.CmdBindDescriptorSets(commandBuffer, PipelineBindPoint.RayTracingKhr, _rayTracingPipeline.PipelineLayout.VkPipelineLayout, 0, 1, descriptorSets, 0, 0);

			// Describe the shader binding table.
			var raygenShaderBindingTable = new StridedDeviceAddressRegionKHR();
			raygenShaderBindingTable.DeviceAddress = _shaderBindingTable.RayGenDeviceAddress;
			raygenShaderBindingTable.Stride = _shaderBindingTable.RayGenEntrySize;
			raygenShaderBindingTable.Size = _shaderBindingTable.RayGenSize;

			var missShaderBindingTable = new StridedDeviceAddressRegionKHR();
			missShaderBindingTable.DeviceAddress = _shaderBindingTable.MissDeviceAddress;
			missShaderBindingTable.Stride = _shaderBindingTable.MissEntrySize;
			missShaderBindingTable.Size = _shaderBindingTable.MissSize;

			var hitShaderBindingTable = new StridedDeviceAddressRegionKHR();
			hitShaderBindingTable.DeviceAddress = _shaderBindingTable.HitGroupDeviceAddress;
			hitShaderBindingTable.Stride = _shaderBindingTable.HitGroupEntrySize;
			hitShaderBindingTable.Size = _shaderBindingTable.HitGroupSize;

			var callableShaderBindingTable = new StridedDeviceAddressRegionKHR();

			// Execute ray tracing shaders.
			_api.KhrRayTracingPipeline.CmdTraceRays(commandBuffer, raygenShaderBindingTable, missShaderBindingTable, hitShaderBindingTable, callableShaderBindingTable, extent.Width, extent.Height, 1);

			// Acquire output image and swap-chain image for copying.
			ImageMemoryBarrier.Insert(_api, commandBuffer, _outputImage.VkImage, subresourceRange,
				 AccessFlags.AccessShaderWriteBit, AccessFlags.AccessTransferReadBit, ImageLayout.General, ImageLayout.TransferSrcOptimal);

			ImageMemoryBarrier.Insert(_api, commandBuffer, _swapChain.VkImages[imageIndex], subresourceRange, 0,
				 AccessFlags.AccessTransferWriteBit, ImageLayout.Undefined, ImageLayout.TransferDstOptimal);

			// Copy output image into swap-chain image.
			var copyRegion = new ImageCopy();
			copyRegion.SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit, 0, 0, 1);
			copyRegion.SrcOffset = new Offset3D(0, 0, 0);
			copyRegion.DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit, 0, 0, 1);
			copyRegion.DstOffset = new Offset3D(0, 0, 0);
			copyRegion.Extent = new Extent3D(extent.Width, extent.Height, 1);

			_api.Vk.CmdCopyImage(commandBuffer, _outputImage.VkImage, ImageLayout.TransferSrcOptimal, _swapChain.VkImages[imageIndex], ImageLayout.TransferDstOptimal, 1, copyRegion);

			ImageMemoryBarrier.Insert(_api, commandBuffer, _swapChain.VkImages[imageIndex], subresourceRange, AccessFlags.AccessTransferWriteBit,
				0, ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr);
		}

		private void GetUniformBufferObject(in Extent2D extent, in Matrix4x4 modelView, CameraInitialState cameraInitialState, out UniformBufferObject ubo)
		{
			ubo = new UniformBufferObject();
			ubo.ModelView = modelView;
			ubo.Projection = Matrix4x4.CreatePerspectiveFieldOfView(MathExtensions.ToRadians(_userSettings.FieldOfView), (float)extent.Width / extent.Height, 0.1f, 10000.0f);
			ubo.Projection.M22 *= -1; // Inverting Y for Vulkan, https://matthewwellings.com/blog/the-new-vulkan-coordinate-system/
			Matrix4x4.Invert(ubo.ModelView, out ubo.ModelViewInverse);
			Matrix4x4.Invert(ubo.Projection, out ubo.ProjectionInverse);
			ubo.Aperture = _userSettings.Aperture;
			ubo.FocusDistance = _userSettings.FocusDistance;
			ubo.TotalNumberOfSamples = TotalNumberOfSamples;
			ubo.NumberOfSamples = NumberOfSamples;
			ubo.NumberOfBounces = _userSettings.NumberOfBounces;
			ubo.RandomSeed = 1;
			ubo.SkyColor1 = cameraInitialState.SkyColor1;
			ubo.SkyColor2 = cameraInitialState.SkyColor2;
			ubo.ShowHeatmap = (uint)(_userSettings.ShowHeatmap ? 1 : 0);
			ubo.HeatmapScale = _userSettings.HeatmapScale;
		}

		private void UpdateUniformBuffer(int imageIndex, in Matrix4x4 modelView, CameraInitialState cameraInitialState)
		{
			GetUniformBufferObject(_swapChain.Extent, modelView, cameraInitialState, out var ubo);
			_uniformBuffers[imageIndex].SetValue(ref ubo);
		}

		private void RecreateSwapChain()
		{
			_api.Device.WaitIdle();
			CreateSwapChain();
		}

		private void CreateSwapChain()
		{
			// Wait until the window is visible.
			while (_window.IsMinimized())
				_window.WaitForEvents();

			_swapChain?.Dispose();
			_swapChain = new SwapChain(_api, _window, _surface, _presentMode);

			_depthBuffer?.Dispose();
			_depthBuffer = new DepthBuffer(_api, _commandPool, _swapChain.Extent);

			DisposeListItems(_imageAvailableSemaphores);
			_imageAvailableSemaphores.Clear();
			DisposeListItems(_renderFinishedSemaphores);
			_renderFinishedSemaphores.Clear();
			DisposeListItems(_inFlightFences);
			_inFlightFences.Clear();
			DisposeListItems(_uniformBuffers);
			_uniformBuffers.Clear();
			for (int i = 0; i != _swapChain.ImageViews.Count; i++)
			{
				_imageAvailableSemaphores.Add(new Semaphore(_api));
				_renderFinishedSemaphores.Add(new Semaphore(_api));
				_inFlightFences.Add(new Fence(_api, true));
				_uniformBuffers.Add(new UniformBuffer(_api));
			}

			_graphicsPipeline?.Dispose();
			_graphicsPipeline = new GraphicsPipeline(_api, _swapChain, _depthBuffer, _uniformBuffers, _vulkanScene);

			DisposeListItems(_swapChainFramebuffers);
			_swapChainFramebuffers = new List<Framebuffer>(_swapChain.ImageViews.Count);
			foreach (var imageView in _swapChain.ImageViews)
				_swapChainFramebuffers.Add(new Framebuffer(_api, _swapChain, _depthBuffer, imageView, _graphicsPipeline.RenderPass));

			_commandBuffers?.Dispose();
			_commandBuffers = new CommandBuffers(_api, _commandPool, (uint)_swapChainFramebuffers.Count);

			CreateOutputImage();

			_rayTracingPipeline?.Dispose();
			_rayTracingPipeline = new RayTracingPipeline(_api, _swapChain, _topAs[0], _accumulationImageView, _outputImageView, _uniformBuffers, _vulkanScene);

			var rayGenPrograms = new List<ShaderBindingTable.Entry>() { new ShaderBindingTable.Entry(_rayTracingPipeline.RayGenShaderIndex, new byte[0]) };
			var missPrograms = new List<ShaderBindingTable.Entry>() { new ShaderBindingTable.Entry(_rayTracingPipeline.MissShaderIndex, new byte[0]) };
			var hitGroups = new List<ShaderBindingTable.Entry>() {
				new ShaderBindingTable.Entry(_rayTracingPipeline.TriangleHitGroupIndex, new byte[0]),
				new ShaderBindingTable.Entry(_rayTracingPipeline.ProceduralHitGroupIndex, new byte[0])
			};

			_shaderBindingTable?.Dispose();
			_shaderBindingTable = new ShaderBindingTable(_api, _rayTracingPipeline, _rayTracingProperties, rayGenPrograms, missPrograms, hitGroups);

			_userInterface?.Dispose();
			_userInterface = new UserInterface(_api, _window, _swapChain, _depthBuffer);
			_resetAccumulation = true;
		}

		private void CreateAccelerationStructures()
		{
			Util.Submit(_api, _commandPool, commandBuffer =>
			{
				CreateBottomLevelStructures(commandBuffer);
				CreateTopLevelStructures(commandBuffer);
			});
		}

		private void CreateBottomLevelStructures(in CommandBuffer commandBuffer)
		{
			// Bottom level acceleration structure
			// Triangles via vertex buffers. Procedurals via AABBs.
			uint vertexOffset = 0;
			uint indexOffset = 0;
			uint aabbOffset = 0;

			DisposeListItems(_bottomAs);
			_bottomAs.Clear();
			foreach (var model in _vulkanScene.Models)
			{
				var vertexCount = (uint)model.Vertices.Count;
				var indexCount = (uint)model.Indices.Count;
				var geometries = new BottomLevelGeometry();

				if (model.Procedural != null)
					geometries.AddGeometryAabb(_vulkanScene, aabbOffset, 1, true);
				else
					geometries.AddGeometryTriangles(_vulkanScene, vertexOffset, vertexCount, indexOffset, indexCount, true);

				_bottomAs.Add(new BottomLevelAccelerationStructure(_api, _rayTracingProperties, geometries));

				vertexOffset += vertexCount * (uint)Unsafe.SizeOf<Vertex>();
				indexOffset += indexCount * sizeof(uint);
				aabbOffset += (uint)Unsafe.SizeOf<AabbPositionsKHR>();
			}

			// Allocate the structures memory.
			var total = GetTotalRequirements(_bottomAs);

			_bottomBuffer?.Dispose();
			_bottomBuffer = new Buffer(_api, total.AccelerationStructureSize, BufferUsageFlags.BufferUsageAccelerationStructureStorageBitKhr);
			_api.SetDebugName(_bottomBuffer.VkBuffer.Handle, $"BLAS Buffer", ObjectType.Buffer);

			_bottomBufferMemory?.Dispose();
			_bottomBufferMemory = _bottomBuffer.AllocateMemory(MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
			_api.SetDebugName(_bottomBufferMemory.VkDeviceMemory.Handle, $"BLAS Memory", ObjectType.DeviceMemory);

			_bottomScratchBuffer?.Dispose();
			_bottomScratchBuffer = new Buffer(_api, total.BuildScratchSize, BufferUsageFlags.BufferUsageShaderDeviceAddressBit | BufferUsageFlags.BufferUsageAccelerationStructureStorageBitKhr);
			_api.SetDebugName(_bottomScratchBuffer.VkBuffer.Handle, $"BLAS Scratch Buffer", ObjectType.Buffer);

			_bottomScratchBufferMemory?.Dispose();
			_bottomScratchBufferMemory = _bottomScratchBuffer.AllocateMemory(MemoryAllocateFlags.MemoryAllocateDeviceAddressBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
			_api.SetDebugName(_bottomScratchBufferMemory.VkDeviceMemory.Handle, $"BLAS Scratch Memory", ObjectType.DeviceMemory);

			// Generate the structures.
			ulong resultOffset = 0;
			ulong scratchOffset = 0;

			for (int i = 0; i != _bottomAs.Count; i++)
			{
				_bottomAs[i].Generate(commandBuffer, _bottomScratchBuffer, scratchOffset, _bottomBuffer, resultOffset);

				resultOffset += _bottomAs[i].BuildSizes.AccelerationStructureSize;
				scratchOffset += _bottomAs[i].BuildSizes.BuildScratchSize;

				_api.SetDebugName(_bottomAs[i].VkAccelerationStructure.Handle, $"BLAS #{i}", ObjectType.AccelerationStructureKhr);
			}
		}

		private unsafe void UpdateTopLevelStructures(in VkCommandBuffer commandBuffer, Matrix4x4[] transforms)
		{
			if (_userSettings.Speed <= 0)
				return;

			//_api.Device.WaitIdle();

			// Update the transform buffer used in the shaders
			_vulkanScene.UpdateTransforms(transforms, _commandPool);

			// Update the TLAS with the new transforms
			for (int m = 0; m < transforms.Length; m++)
			{
				var transform = transforms[m];
				transform = Matrix4x4.Transpose(transform);
				var mat = new TransformMatrixKHR(); // 4x3
				Unsafe.CopyBlock(mat.Matrix, Unsafe.AsPointer(ref transform), 48);

				_instances[m].Transform = mat;
			}

			BufferUtil.CopyFromStagingBuffer(_api, _commandPool, _instancesBuffer, _instances);

			//AccelerationStructure.MemoryBarrier(_api, commandBuffer);

			_topAs[0].Rebuild(commandBuffer, _topScratchBuffer, 0);
		}

		private unsafe void CreateTopLevelStructures(in VkCommandBuffer commandBuffer)
		{
			// Top level acceleration structure
			_instances = new AccelerationStructureInstanceKHR[_vulkanScene.Models.Count];

			// Hit group 0: triangles
			// Hit group 1: procedurals
			int instanceId = 0;
			for (int m = 0; m < _vulkanScene.Models.Count; m++)
			{
				var transform = _vulkanScene.Models[m].Transform;
				transform = Matrix4x4.Transpose(transform);
				var mat = new TransformMatrixKHR(); // 4x3
				Unsafe.CopyBlock(mat.Matrix, Unsafe.AsPointer(ref transform), 48);

				_instances[m] = TopLevelAccelerationStructure.CreateInstance(_api,
					_bottomAs[instanceId], mat, (uint)instanceId, (uint)(_vulkanScene.Models[m].Procedural != null ? 1 : 0));
				instanceId++;
			}

			// Create and copy instances buffer (do it in a separate one-time synchronous command buffer).
			_instancesBuffer?.Dispose();
			_instancesBufferMemory?.Dispose();
			BufferUtil.CreateDeviceBuffer(_api, _commandPool, "TLAS Instances", BufferUsageFlags.BufferUsageShaderDeviceAddressBit, _instances, out _instancesBuffer, out _instancesBufferMemory);
			_api.SetDebugName(_instancesBuffer.VkBuffer.Handle, $"TLAS Instances Buffer", ObjectType.Buffer);
			_api.SetDebugName(_instancesBufferMemory.VkDeviceMemory.Handle, $"TLAS Instances Memory", ObjectType.DeviceMemory);

			// Memory barrier for the bottom level acceleration structure builds.
			AccelerationStructure.MemoryBarrier(_api, commandBuffer);

			DisposeListItems(_topAs);
			_topAs.Clear();
			_topAs.Add(new TopLevelAccelerationStructure(_api, _rayTracingProperties, _instancesBuffer.GetDeviceAddress(), (uint)_instances.Length));

			// Allocate the structure memory.
			var total = GetTotalRequirements(_topAs);

			_topBuffer?.Dispose();
			_topBuffer = new Buffer(_api, total.AccelerationStructureSize, BufferUsageFlags.BufferUsageAccelerationStructureStorageBitKhr);
			_api.SetDebugName(_topBuffer.VkBuffer.Handle, $"TLAS Buffer", ObjectType.Buffer);

			_topBufferMemory?.Dispose();
			_topBufferMemory = _topBuffer.AllocateMemory(MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
			_api.SetDebugName(_topBufferMemory.VkDeviceMemory.Handle, $"TLAS Memory", ObjectType.DeviceMemory);

			_topScratchBuffer?.Dispose();
			_topScratchBuffer = new Buffer(_api, total.BuildScratchSize, BufferUsageFlags.BufferUsageShaderDeviceAddressBit | BufferUsageFlags.BufferUsageAccelerationStructureStorageBitKhr);
			_api.SetDebugName(_topScratchBuffer.VkBuffer.Handle, $"TLAS Scratch Buffer", ObjectType.Buffer);

			_topScratchBufferMemory?.Dispose();
			_topScratchBufferMemory = _topScratchBuffer.AllocateMemory(MemoryAllocateFlags.MemoryAllocateDeviceAddressBit, MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
			_api.SetDebugName(_topScratchBufferMemory.VkDeviceMemory.Handle, $"TLAS Instances Memory", ObjectType.DeviceMemory);

			// Generate the structures.
			_topAs[0].Generate(commandBuffer, _topScratchBuffer, 0, _topBuffer, 0);
			_api.SetDebugName(_topAs[0].VkAccelerationStructure.Handle, $"TLAS", ObjectType.AccelerationStructureKhr);
		}

		private AccelerationStructureBuildSizesInfoKHR GetTotalRequirements<T>(List<T> accelerationStructures) where T : AccelerationStructure
		{
			var total = new AccelerationStructureBuildSizesInfoKHR();

			foreach (var accelerationStructure in accelerationStructures)
			{
				total.AccelerationStructureSize += accelerationStructure.BuildSizes.AccelerationStructureSize;
				total.BuildScratchSize += accelerationStructure.BuildSizes.BuildScratchSize;
				total.UpdateScratchSize += accelerationStructure.BuildSizes.UpdateScratchSize;
			}

			return total;
		}

		private void CreateOutputImage()
		{
			var extent = _swapChain.Extent;
			var format = _swapChain.Format;
			var tiling = ImageTiling.Optimal;

			_accumulationImage?.Dispose();
			_accumulationImage = new Image(_api, extent, Format.R32G32B32A32Sfloat, ImageTiling.Optimal, ImageUsageFlags.ImageUsageStorageBit);
			_api.SetDebugName(_accumulationImage.VkImage.Handle, $"Accumulation Image", ObjectType.Image);

			_accumulationImageMemory?.Dispose();
			_accumulationImageMemory = _accumulationImage.AllocateMemory(MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
			_api.SetDebugName(_accumulationImageMemory.VkDeviceMemory.Handle, $"Accumulation Image", ObjectType.DeviceMemory);

			_accumulationImageView?.Dispose();
			_accumulationImageView = new ImageView(_api, _accumulationImage.VkImage, Format.R32G32B32A32Sfloat, ImageAspectFlags.ImageAspectColorBit);
			_api.SetDebugName(_accumulationImageView.VkImageView.Handle, $"Accumulation Image", ObjectType.ImageView);

			_outputImage?.Dispose();
			_outputImage = new Image(_api, extent, format, tiling, ImageUsageFlags.ImageUsageStorageBit | ImageUsageFlags.ImageUsageTransferSrcBit);
			_api.SetDebugName(_outputImage.VkImage.Handle, $"Output Image", ObjectType.Image);

			_outputImageMemory?.Dispose();
			_outputImageMemory = _outputImage.AllocateMemory(MemoryPropertyFlags.MemoryPropertyDeviceLocalBit);
			_api.SetDebugName(_outputImageMemory.VkDeviceMemory.Handle, $"Output Image Image", ObjectType.DeviceMemory);

			_outputImageView?.Dispose();
			_outputImageView = new ImageView(_api, _outputImage.VkImage, format, ImageAspectFlags.ImageAspectColorBit);
			_api.SetDebugName(_outputImageView.VkImageView.Handle, $"Output Image", ObjectType.ImageView);
		}

		public void LoadScene(IScene scene, CameraInitialState cameraInitialState)
		{
			_api.Device.WaitIdle();

			_scene = scene;

			scene.Reset(cameraInitialState);

			// If there are no texture, add a dummy one. It makes the pipeline setup a lot easier.
			if (scene.Textures.Count == 0)
				scene.Textures.Add(Texture.LoadTexture("./assets/textures/white.png"));

			_vulkanScene?.Dispose();
			_vulkanScene = new VulkanScene(_api, _commandPool, scene.Models, scene.Textures);
			_transforms = _vulkanScene.Transforms; 

			_resetAccumulation = true;

			CreateAccelerationStructures();

			CreateSwapChain();
		}

		private void DisposeListItems(IEnumerable<IDisposable> list)
		{
			if (list == null)
				return;
			foreach (var item in list)
				item.Dispose();
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				_api.Device.WaitIdle();

				_vulkanScene?.Dispose();
				_userInterface.Dispose();
				_shaderBindingTable.Dispose();
				_rayTracingPipeline.Dispose();
				_outputImageView.Dispose();
				_outputImage.Dispose();
				_outputImageMemory.Dispose();
				_accumulationImageView.Dispose();
				_accumulationImage.Dispose();
				_accumulationImageMemory.Dispose();
				_commandBuffers.Dispose();
				DisposeListItems(_swapChainFramebuffers);
				_graphicsPipeline.Dispose();
				DisposeListItems(_uniformBuffers);
				DisposeListItems(_inFlightFences);
				DisposeListItems(_renderFinishedSemaphores);
				DisposeListItems(_imageAvailableSemaphores);
				_depthBuffer.Dispose();
				_swapChain.Dispose();
				DisposeListItems(_topAs);
				_instancesBuffer.Dispose();
				_instancesBufferMemory.Dispose();
				_topScratchBuffer?.Dispose();
				_topScratchBufferMemory?.Dispose();
				_topBuffer.Dispose();
				_topBufferMemory.Dispose();
				DisposeListItems(_bottomAs);
				_bottomScratchBuffer?.Dispose();
				_bottomScratchBufferMemory?.Dispose();
				_bottomBuffer.Dispose();
				_bottomBufferMemory.Dispose();
				_commandPool.Dispose();
				_api.Device.Dispose();
				_surface.Dispose();
				_api.Dispose();
				_api.Instance.Dispose();

				_disposedValue = true;
			}
		}

		public void Dispose()
		{
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
