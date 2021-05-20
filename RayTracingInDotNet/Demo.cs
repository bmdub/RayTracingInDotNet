using RayTracingInDotNet.Scene;
using RayTracingInDotNet.Vulkan;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;
using Silk.NET.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RayTracingInDotNet
{
	class Demo
	{
		private readonly Options _options;
		private readonly UserSettings _userSettings;
		private readonly ILogger _logger;
		private readonly ModelViewController _modelViewController = new ModelViewController();
		private readonly CameraInitialState _cameraInitialState = new CameraInitialState();
		private readonly Statistics _stats = new Statistics();
		private Window _window;
		private IRenderer _renderer;
		private IScene _scene;
		private int _sceneIndex = -1;

		public unsafe Demo(string[] args)
		{
			_options = Options.Parse(args);
			_userSettings = CreateUserSettings(_options);

			_logger = new LoggerConfiguration()
				.WriteTo.Console(theme: AnsiConsoleTheme.Code)
				.MinimumLevel.Is(_options.DebugLogging ? Serilog.Events.LogEventLevel.Verbose : Serilog.Events.LogEventLevel.Information)
				.CreateLogger();

			InitRenderer();
		}

		private void InitRenderer()
		{
			bool second = _renderer != null;

			if (_window != null)
			{
				_window.DrawFrame -= DrawFrame;
				_window.OnKeyDown -= OnKeyDown;
				_window.OnKeyUp -= OnKeyUp;
				_window.OnCursorPosition -= OnCursorPosition;
				_window.OnMouseDown -= OnMouseButtonDown;
				_window.OnMouseUp -= OnMouseButtonUp;
				_window.OnScroll -= OnScroll;
				_window.Close();
				_window.Dispose();
			}

			_renderer?.Dispose();

			var windowConfig = new WindowConfig()
			{
				Title = "Demo Window",
				Width = _options.Width,
				Height = _options.Height,
				CursorDisabled = false,
				Fullscreen = _userSettings.Fullscreen,
				Resizable = !_userSettings.Fullscreen,
			};

			_window = new Window(windowConfig);

			_scene = Scenes.MetaData[_userSettings.SceneIndex].Instantiate();

			// Select the renderer to use
			_renderer = new VulkanRenderer(_userSettings, _window, _scene, _cameraInitialState, _logger, _options.DebugLogging);

			_renderer.DrawGui += () =>
			{
				//ImGuiNET.ImGui.ShowStyleEditor();
				//ImGuiNET.ImGui.ShowDemoWindow();
				//ImGuiNET.ImGui.ShowFontSelector("Font");
				//ImGuiNET.ImGui.ShowMetricsWindow();
				//ImGuiNET.ImGui.ShowStyleSelector("Style");
				//ImGuiNET.ImGui.ShowUserGuide();
				DrawSettings();
				DrawOverlay();
			};

			_window.DrawFrame += DrawFrame;
			_window.OnKeyDown += OnKeyDown;
			_window.OnKeyUp += OnKeyUp;
			_window.OnCursorPosition += OnCursorPosition;
			_window.OnMouseDown += OnMouseButtonDown;
			_window.OnMouseUp += OnMouseButtonUp;
			_window.OnScroll += OnScroll;
			_window.Run();
		}

		private unsafe void DrawFrame(double delta)
		{
			// Check if the scene has been changed by the user.
			bool sceneChanged = _sceneIndex != (uint)_userSettings.SceneIndex;
			if(sceneChanged)
			{
				_sceneIndex = _userSettings.SceneIndex;

				_scene = Scenes.MetaData[_sceneIndex].Instantiate();

				_renderer.LoadScene(_scene, _cameraInitialState);

				// Now that a new scene is loaded, set user/camera settings to the scene-defined settings.
				_userSettings.FieldOfView = _cameraInitialState.FieldOfView;
				_userSettings.Aperture = _cameraInitialState.Aperture;
				_userSettings.FocusDistance = _cameraInitialState.FocusDistance;

				_modelViewController.Reset(_cameraInitialState.ModelView);
			}

			// Update the camera position / angle.
			bool cameraMoved = _modelViewController.UpdateCamera(_cameraInitialState.ControlSpeed, delta);
			if (cameraMoved)
				_renderer.ResetAccumulation();

			// Draw the frame, loading a new scene if necessary
			_renderer.DrawFrame(delta, _modelViewController.ModelView(), _cameraInitialState);

			_stats.FramebufferSize = (_window.FrameBufferWidth, _window.FrameBufferHeight);
			_stats.FrameRate = (float)(1 / delta);
			_stats.RayRate = (float)((double)(_window.FrameBufferWidth * _window.FrameBufferHeight) * _renderer.NumberOfSamples / (delta * 1000000000));
			_stats.TotalSamples = _renderer.TotalNumberOfSamples;
		}

		private static UserSettings CreateUserSettings(Options options)
		{
			var userSettings = new UserSettings();

			userSettings.SceneIndex = (int)options.SceneIndex;

			userSettings.AccumulateRays = true;
			userSettings.NumberOfSamples = options.Samples;
			userSettings.NumberOfBounces = options.Bounces;
			userSettings.MaxNumberOfSamples = options.MaxSamples;
			userSettings.VSync = options.VSync;
			userSettings.Fullscreen = options.Fullscreen;

			userSettings.ShowSettings = true;
			userSettings.ShowOverlay = true;

			userSettings.ShowHeatmap = false;
			userSettings.HeatmapScale = .5f;

			return userSettings;
		}

		private void OnKeyDown(Key key)
		{
			if (ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
				return;

			switch (key)
			{
				case Key.Escape: _window.Close(); break;
				default: break;
			}

			// Settings (toggle switches)
			switch (key)
			{
				case Key.Enter:
					_userSettings.Fullscreen = !_userSettings.Fullscreen;
					InitRenderer();
					break;
				case Key.F1: _userSettings.ShowSettings = !_userSettings.ShowSettings; break;
				case Key.F2: _userSettings.ShowOverlay = !_userSettings.ShowOverlay; break;
				case Key.H: _userSettings.ShowHeatmap = !_userSettings.ShowHeatmap; break;
				case Key.R: _renderer.LoadScene(_scene, _cameraInitialState); break;
				default: break;
			}

			// Camera motions
			if (_modelViewController.OnKeyDown(key))
				_renderer.ResetAccumulation();
		}

		private void OnKeyUp(Key key)
		{
			if (ImGuiNET.ImGui.GetIO().WantCaptureKeyboard)
				return;

			// Camera motions
			if (_modelViewController.OnKeyUp(key))
				_renderer.ResetAccumulation();
		}

		private void OnCursorPosition(float xpos, float ypos)
		{
			if (ImGuiNET.ImGui.GetIO().WantCaptureKeyboard || ImGuiNET.ImGui.GetIO().WantCaptureMouse)
				return;

			// Camera motions
			if (_modelViewController.OnCursorPosition(xpos, ypos))
				_renderer.ResetAccumulation();
		}

		private void OnMouseButtonDown(MouseButton button)
		{
			if (ImGuiNET.ImGui.GetIO().WantCaptureMouse)
				return;

			// Camera motions
			if (_modelViewController.OnMouseButtonDown(button))
				_renderer.ResetAccumulation();
		}

		private void OnMouseButtonUp(MouseButton button)
		{
			if (ImGuiNET.ImGui.GetIO().WantCaptureMouse)
				return;

			// Camera motions
			if (_modelViewController.OnMouseButtonUp(button))
				_renderer.ResetAccumulation();
		}

		private void OnScroll(float xoffset, float yoffset)
		{
			if (ImGuiNET.ImGui.GetIO().WantCaptureMouse)
				return;

			var prevFov = _userSettings.FieldOfView;
			_userSettings.FieldOfView = Math.Clamp((float)(prevFov - yoffset), UserSettings.FieldOfViewMinValue, UserSettings.FieldOfViewMaxValue);

			if (prevFov != _userSettings.FieldOfView)
				_renderer.ResetAccumulation();
		}

		private unsafe void DrawSettings()
		{
			if (!_userSettings.ShowSettings)
				return;

			float distance = 10.0f;
			var pos = new Vector2(distance, distance);
			var posPivot = new Vector2(0.0f, 0.0f);
			ImGuiNET.ImGui.SetNextWindowPos(pos, ImGuiNET.ImGuiCond.Always, posPivot);

			var flags = ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize |
				ImGuiNET.ImGuiWindowFlags.NoCollapse |
				ImGuiNET.ImGuiWindowFlags.NoMove |
				ImGuiNET.ImGuiWindowFlags.NoResize |
				ImGuiNET.ImGuiWindowFlags.NoSavedSettings;

			if (ImGuiNET.ImGui.Begin("Settings", ref _userSettings.ShowSettings, flags))
			{
				List<string> scenes = Scenes.MetaData.Select(md => md.Name).ToList();

				ImGuiNET.ImGui.Text("Help");
				ImGuiNET.ImGui.Separator();
				ImGuiNET.ImGui.BulletText("ENTER: toggle fullscreen.");
				ImGuiNET.ImGui.BulletText("F1: toggle Settings.");
				ImGuiNET.ImGui.BulletText("F2: toggle Statistics.");
				ImGuiNET.ImGui.BulletText("H: toggle heat map.");
				ImGuiNET.ImGui.BulletText("R: reset scene.");
				ImGuiNET.ImGui.BulletText($"WASD/SHIFT/CTRL: move camera.");
				ImGuiNET.ImGui.BulletText("L/R Mouse: rotate camera/scene.");
				ImGuiNET.ImGui.NewLine();

				ImGuiNET.ImGui.Text("Scene");
				ImGuiNET.ImGui.Separator();
				ImGuiNET.ImGui.PushItemWidth(-1);
				ImGuiNET.ImGui.Combo("", ref _userSettings.SceneIndex, scenes.ToArray(), scenes.Count);
				ImGuiNET.ImGui.PopItemWidth();
				ImGuiNET.ImGui.SliderInt("Speed", ref _userSettings.Speed, 0, 100);
				ImGuiNET.ImGui.NewLine();

				ImGuiNET.ImGui.Text("Ray Tracing");
				ImGuiNET.ImGui.Separator();
				ImGuiNET.ImGui.Checkbox("Accumulate rays between frames", ref _userSettings.AccumulateRays);
				uint min = 1, max = 128;
				fixed (uint* pNumberOfSamples = &_userSettings.NumberOfSamples)
				fixed (uint* pNumberOfBounces = &_userSettings.NumberOfBounces)
				{
					ImGuiNET.ImGui.SliderScalar("Samples", ImGuiNET.ImGuiDataType.U32, new IntPtr(pNumberOfSamples), new IntPtr(&min), new IntPtr(&max));
					min = 1;
					max = 32;
					ImGuiNET.ImGui.SliderScalar("Bounces", ImGuiNET.ImGuiDataType.U32, new IntPtr(pNumberOfBounces), new IntPtr(&min), new IntPtr(&max));
				}
				ImGuiNET.ImGui.Checkbox("VSync", ref _userSettings.VSync);
				ImGuiNET.ImGui.NewLine();

				ImGuiNET.ImGui.Text("Camera");
				ImGuiNET.ImGui.Separator();
				ImGuiNET.ImGui.SliderFloat("FoV", ref _userSettings.FieldOfView, UserSettings.FieldOfViewMinValue, UserSettings.FieldOfViewMaxValue, "%.0f");
				ImGuiNET.ImGui.SliderFloat("Aperture", ref _userSettings.Aperture, 0.0f, 1.0f, "%.2f");
				ImGuiNET.ImGui.SliderFloat("Focus", ref _userSettings.FocusDistance, 0.1f, 20.0f, "%.1f");
				ImGuiNET.ImGui.NewLine();

				ImGuiNET.ImGui.Text("Profiler");
				ImGuiNET.ImGui.Separator();
				ImGuiNET.ImGui.Checkbox("Show Heat Map", ref _userSettings.ShowHeatmap);
				ImGuiNET.ImGui.SliderFloat("Scaling", ref _userSettings.HeatmapScale, 0.10f, 10.0f, "%.2f", ImGuiNET.ImGuiSliderFlags.Logarithmic);
				ImGuiNET.ImGui.NewLine();
			}
			ImGuiNET.ImGui.End();
		}

		private void DrawOverlay()
		{
			if (!_userSettings.ShowOverlay)
				return;

			var io = ImGuiNET.ImGui.GetIO();
			float distance = 10.0f;
			Vector2 pos = new Vector2(io.DisplaySize.X - distance, distance);
			Vector2 posPivot = new Vector2(1.0f, 0.0f);
			ImGuiNET.ImGui.SetNextWindowPos(pos, ImGuiNET.ImGuiCond.Always, posPivot);
			ImGuiNET.ImGui.SetNextWindowBgAlpha(0.3f); // Transparent background

			var flags = ImGuiNET.ImGuiWindowFlags.AlwaysAutoResize |
				ImGuiNET.ImGuiWindowFlags.NoDecoration |
				ImGuiNET.ImGuiWindowFlags.NoFocusOnAppearing |
				ImGuiNET.ImGuiWindowFlags.NoMove |
				ImGuiNET.ImGuiWindowFlags.NoNav |
				ImGuiNET.ImGuiWindowFlags.NoSavedSettings;

			if (ImGuiNET.ImGui.Begin("Statistics", ref _userSettings.ShowOverlay, flags))
			{
				ImGuiNET.ImGui.Text($"Statistics {_stats.FramebufferSize.Width}x{_stats.FramebufferSize.Height}:");
				ImGuiNET.ImGui.Separator();
				ImGuiNET.ImGui.Text($"Frame rate: {_stats.FrameRate.ToString("F1")} fps");
				ImGuiNET.ImGui.Text($"Primary ray rate: {_stats.RayRate.ToString("F2")} Gr/s");
				ImGuiNET.ImGui.Text($"Accumulated samples: {_stats.TotalSamples.ToString("N0")}");
			}
			ImGuiNET.ImGui.End();
		}
	}
}
