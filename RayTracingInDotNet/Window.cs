using Silk.NET.Input;
using Silk.NET.Windowing;
using System;
using System.Diagnostics;
using System.Linq;

namespace RayTracingInDotNet
{
	unsafe class Window : IDisposable
	{
		public delegate void DrawFrameDelegate(double time);
		public delegate void OnKeyDelegate(Key key);
		public delegate void OnCursorPositionDelegate(float xpos, float ypos);
		public delegate void OnMouseButtonDelegate(MouseButton button);
		public delegate void OnScrollDelegate(float xoffset, float yoffset);

		public event DrawFrameDelegate DrawFrame;
		public event OnKeyDelegate OnKeyDown;
		public event OnKeyDelegate OnKeyUp;
		public event OnCursorPositionDelegate OnCursorPosition;
		public event OnMouseButtonDelegate OnMouseUp;
		public event OnMouseButtonDelegate OnMouseDown;
		public event OnScrollDelegate OnScroll;

		private readonly WindowConfig _config;
		private readonly IWindow _window;
		private readonly IInputContext _input;
		private readonly Stopwatch _stopwatch = new Stopwatch();
		private bool _disposedValue;

		public Window(WindowConfig config)
		{
			_config = config;

			var opts = WindowOptions.DefaultVulkan;
			opts.WindowBorder = WindowBorder.Resizable;
			opts.Size = new Silk.NET.Maths.Vector2D<int>((int)config.Width, (int)config.Height);
			opts.Title = config.Title;
			opts.WindowState = config.Fullscreen ? WindowState.Fullscreen : WindowState.Normal;

			_window = Silk.NET.Windowing.Window.Create(opts);
			_window.Render += (time) => DrawFrame?.Invoke(time);

			_window.Initialize();

			_input = _window.CreateInput();
			var primaryKeyboard = _input.Keyboards.FirstOrDefault();

			if (primaryKeyboard != null)
			{
				primaryKeyboard.KeyDown += (keyboard, key, code) => OnKeyDown?.Invoke(key);
				primaryKeyboard.KeyUp += (keyboard, key, code) => OnKeyUp?.Invoke(key);
			}
			for (int i = 0; i < _input.Mice.Count; i++)
			{
				_input.Mice[i].Cursor.CursorMode = config.CursorDisabled ? CursorMode.Disabled : CursorMode.Normal;
				_input.Mice[i].MouseMove += (mouse, pos) => OnCursorPosition?.Invoke(pos.X, pos.Y);
				_input.Mice[i].Scroll += (mouse, wheel) => OnScroll?.Invoke(wheel.X, wheel.Y);
				_input.Mice[i].MouseDown += (mouse, button) => OnMouseDown?.Invoke(button);
				_input.Mice[i].MouseUp += (mouse, button) => OnMouseUp?.Invoke(button);
			}
		}

		public WindowConfig Config => _config;
		public IWindow IWindow => _window;
		public IInputContext InputContext => _input;

		public int FrameBufferWidth => _window.FramebufferSize.X;
		public int FrameBufferHeight => _window.FramebufferSize.Y;

		public int WindowWidth => _window.Size.X;
		public int WindowHeight => _window.Size.Y;

		public string[] GetRequiredInstanceExtensions()
		{
			var stringArrayPtr = _window.VkSurface.GetRequiredExtensions(out var count);
			return Silk.NET.Core.Native.SilkMarshal.PtrToStringArray((nint)stringArrayPtr, (int)count);
		}

		public double GetTime() => _stopwatch.Elapsed.TotalSeconds;

		public void Close() => _window.Close();

		public bool IsMinimized() => _window.WindowState == WindowState.Minimized;

		public void Run()
		{
			_stopwatch.Start();
			_window.Run();
		}

		public void WaitForEvents() => _window.DoEvents();

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposedValue)
			{
				if (disposing)
				{
					_window.Dispose();
				}

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
