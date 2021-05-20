using Silk.NET.Vulkan;
using System;

namespace RayTracingInDotNet.Vulkan
{
	static partial class Util
	{
		public static void Verify(Result result, string failureMessage)
		{
			if (result != Result.Success)
				throw new Exception($"{failureMessage} ({result})");
		}

		public static unsafe void Submit(Api api, in CommandPool commandPool, Action<CommandBuffer> action)
		{
			var commandBuffers = new CommandBuffers(api, commandPool, 1);

			var beginInfo = new CommandBufferBeginInfo();
			beginInfo.SType = StructureType.CommandBufferBeginInfo;
			beginInfo.Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit;

			Verify(api.Vk.BeginCommandBuffer(commandBuffers[0], beginInfo), $"{nameof(Util.Submit)}: Failed to begin a command buffer");

			action(commandBuffers[0]);

			Verify(api.Vk.EndCommandBuffer(commandBuffers[0]), $"{nameof(Util.Submit)}: Failed to begin a command buffer");

			var submitInfo = new SubmitInfo();
			submitInfo.SType = StructureType.SubmitInfo;
			submitInfo.CommandBufferCount = 1;
			submitInfo.PCommandBuffers = commandBuffers.Ptr;

			var graphicsQueue = api.Device.GraphicsQueue;

			Verify(api.Vk.QueueSubmit(graphicsQueue, 1, submitInfo, default), $"{nameof(Util.Submit)}: Failed to begin a command buffer");

			Verify(api.Vk.QueueWaitIdle(graphicsQueue), $"{nameof(Util.Submit)}: Failed to begin a command buffer");

			commandBuffers.Dispose();
		}
	}
}
