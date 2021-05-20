using Silk.NET.Vulkan;
using VkImage = Silk.NET.Vulkan.Image;
using VkImageMemoryBarrier = Silk.NET.Vulkan.ImageMemoryBarrier;

namespace RayTracingInDotNet.Vulkan
{
	static class ImageMemoryBarrier
	{
		public static unsafe void Insert(
			Api api,
			in CommandBuffer commandBuffer,
			in VkImage image,
			in ImageSubresourceRange subresourceRange,
			AccessFlags srcAccessMask,
			AccessFlags dstAccessMask,
			ImageLayout oldLayout,
			ImageLayout newLayout)
		{
			const uint VK_QUEUE_FAMILY_IGNORED = (~0U);

			var barrier = new VkImageMemoryBarrier();
			barrier.SType = StructureType.ImageMemoryBarrier;
			barrier.PNext = null;
			barrier.SrcAccessMask = srcAccessMask;
			barrier.DstAccessMask = dstAccessMask;
			barrier.OldLayout = oldLayout;
			barrier.NewLayout = newLayout;
			barrier.SrcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
			barrier.DstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED;
			barrier.Image = image;
			barrier.SubresourceRange = subresourceRange;

			api.Vk.CmdPipelineBarrier(commandBuffer, PipelineStageFlags.PipelineStageAllCommandsBit, PipelineStageFlags.PipelineStageAllCommandsBit, 0, 0, default, 0, default, 1, barrier);
		}
	};
}
