using Serilog.Events;
using Silk.NET.Vulkan;
using System;
using System.Runtime.InteropServices;

namespace RayTracingInDotNet.Vulkan
{
	class DebugUtilsMessenger : IDisposable
	{
		private readonly Api _api;
		private readonly DebugUtilsMessengerEXT _debugMessenger;
		private bool _disposedValue;

		public unsafe DebugUtilsMessenger(Api api)
		{
			_api = api;

			// Note: Logging warnings/errors may still be useful even when "debug"
			// logging is disabled. Consider changing this.
			if (api.DebugLoggingEnabled == false)
				return;

			var createInfo = new DebugUtilsMessengerCreateInfoEXT();
			createInfo.SType = StructureType.DebugUtilsMessengerCreateInfoExt;
			createInfo.MessageSeverity = DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt |
										 DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt |
										 DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt |
										 DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt;
			createInfo.MessageType = DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeGeneralBitExt |
									 DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypePerformanceBitExt |
									 DebugUtilsMessageTypeFlagsEXT.DebugUtilsMessageTypeValidationBitExt;
			createInfo.PfnUserCallback = (DebugUtilsMessengerCallbackFunctionEXT)DebugCallback;

			fixed (DebugUtilsMessengerEXT* debugMessenger = &_debugMessenger)
			{
				if (_api.ExtDebugUtils.CreateDebugUtilsMessenger(api.Instance.VkInstance, &createInfo, null, debugMessenger) != Result.Success)
					throw new Exception($"{nameof(DebugUtilsMessenger)}: Failed to create debug messenger.");
			}
		}

		private unsafe uint DebugCallback
		(
			DebugUtilsMessageSeverityFlagsEXT messageSeverity,
			DebugUtilsMessageTypeFlagsEXT messageTypes,
			DebugUtilsMessengerCallbackDataEXT* pCallbackData,
			void* pUserData
		)
		{
			LogEventLevel level = LogEventLevel.Error;

			switch(messageSeverity)
			{
				case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityErrorBitExt:
					level = LogEventLevel.Error;
					break;
				case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityWarningBitExt:
					level = LogEventLevel.Warning;
					break;
				case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityInfoBitExt:
					level = LogEventLevel.Information;
					break;
				case DebugUtilsMessageSeverityFlagsEXT.DebugUtilsMessageSeverityVerboseBitExt:
					level = LogEventLevel.Verbose;
					break;
			}

			_api.Logger.Write(level, $"{messageSeverity} {messageTypes}" + Marshal.PtrToStringAnsi((nint)pCallbackData->PMessage));

			return Vk.False;
		}

		public unsafe void SetDebugName(ulong handle, string name, ObjectType type)
		{
			if (_api.DebugLoggingEnabled == false)
				return;

			if (_api.ExtDebugUtils == null)
				throw new Exception($"{nameof(DebugUtilsMessenger)}: Api extensions must be enabled before setting debug object names.");

			var info = new DebugUtilsObjectNameInfoEXT();
			info.SType = StructureType.DebugUtilsObjectNameInfoExt;
			info.PNext = null;
			info.ObjectHandle = handle;
			info.ObjectType = type;
			var namePtr = Marshal.StringToHGlobalAnsi(name);
			info.PObjectName = (byte*)namePtr.ToPointer();

			Util.Verify(_api.ExtDebugUtils.SetDebugUtilsObjectName(_api.Device.VkDevice, info), $"{nameof(DebugUtilsMessenger)}: Unable to set object debug name.");

			Marshal.FreeHGlobal(namePtr);
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
				}

				if (_api.DebugLoggingEnabled)
					_api.ExtDebugUtils?.DestroyDebugUtilsMessenger(_api.Instance.VkInstance, _debugMessenger, null);
				_disposedValue = true;
			}
		}

		~DebugUtilsMessenger()
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
