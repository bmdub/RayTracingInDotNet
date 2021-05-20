using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;

namespace RayTracingInDotNet.Vulkan
{
	static class Enumerate
	{
		/// <summary>Simplifies (possibly) vulkan list enumeration.</summary>
		/// <typeparam name="IN1">The input type of the first input into the Vulkan function.</typeparam>
		/// <typeparam name="IN2">The input type of the second input into the Vulkan function.</typeparam>
		/// <typeparam name="OUT">The element type of the element list to be returned by the Vulkan function.</typeparam>
		/// <param name="input1">The first input into the Vulkan function.</param>
		/// <param name="input2">The second input into the Vulkan function.</param>
		/// <param name="enumerate">A function that calls into the target Vulkan enumeration function using the given inputs.</param>
		/// <returns>A list of the items intended to be returned from the Vulkan function.</returns>
		public static unsafe List<OUT> Get<IN1, IN2, OUT>(IN1 input1, IN2 input2, Func<IN1, IN2, nint, nint, Result> enumerate) where OUT : unmanaged
		{
			uint count = 0;
			Util.Verify(enumerate(input1, input2, (nint)(&count), 0), $"Could not get the count for enumeration {enumerate.Method.Name}");

			List<OUT> items = new List<OUT>((int)count);

			// Note: stackalloc here could be bad if the enumerated set is large.
			fixed (OUT* values = stackalloc OUT[(int)count])
			{
				Util.Verify(enumerate(input1, input2, (nint)(&count), (nint)values), $"Could not enumerate {enumerate.Method.Name}");

				for (int i = 0; i < count; i++)
					items.Add(values[i]);
			}

			return items;
		}

		/// <summary>Simplifies (possibly) vulkan list enumeration.</summary>
		/// <typeparam name="IN1">The input type of the first input into the Vulkan function.</typeparam>
		/// <typeparam name="OUT">The element type of the element list to be returned by the Vulkan function.</typeparam>
		/// <param name="input1">The first input into the Vulkan function.</param>
		/// <param name="enumerate">A function that calls into the target Vulkan enumeration function using the given inputs.</param>
		/// <returns>A list of the items intended to be returned from the Vulkan function.</returns>
		public static unsafe List<OUT> Get<IN1, OUT>(IN1 input1, Action<IN1, nint, nint> enumerate) where OUT : unmanaged
		{
			uint count = 0;
			enumerate(input1, (nint)(&count), 0);

			List<OUT> items = new List<OUT>((int)count);

			// Note: stackalloc here could be bad if the enumerated set is large.
			fixed (OUT* values = stackalloc OUT[(int)count])
			{
				enumerate(input1, (nint)(&count), (nint)values);

				for (int i = 0; i < count; i++)
					items.Add(values[i]);
			}

			return items;
		}
	}
}
