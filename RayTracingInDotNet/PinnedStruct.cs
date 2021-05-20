using System;
using System.Runtime.CompilerServices;

namespace RayTracingInDotNet
{
	unsafe class PinnedStruct<T> where T : unmanaged
	{
		public readonly T* Ptr;
		readonly byte[] _data;

		public PinnedStruct()
		{
			_data = GC.AllocateArray<byte>(sizeof(T), true);
			Ptr = (T*)Unsafe.AsPointer(ref _data[0]);
		}

		public ref T Value => ref *Ptr;
	}
}
