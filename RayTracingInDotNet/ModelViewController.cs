using Silk.NET.Input;
using System;
using System.Numerics;

namespace RayTracingInDotNet
{
	class ModelViewController
	{
		// Matrices and vectors.
		private Matrix4x4 _orientation;

		private Vector4 _position;
		private Vector4 _right = new Vector4(1, 0, 0, 0);
		private Vector4 _up = new Vector4(0, 1, 0, 0);
		private Vector4 _forward = new Vector4(0, 0, -1, 0);

		// Control states.
		private bool _cameraMovingLeft;
		private bool _cameraMovingRight;
		private bool _cameraMovingBackward;
		private bool _cameraMovingForward;
		private bool _cameraMovingDown;
		private bool _cameraMovingUp;

		private float _cameraRotX;
		private float _cameraRotY;
		private float _modelRotX;
		private float _modelRotY;

		private double _mousePosX;
		private double _mousePosY;

		private bool _mouseLeftPressed;
		private bool _mouseRightPressed;

		public void Reset(in Matrix4x4 modelView)
		{
			Matrix4x4.Invert(modelView, out var inverse);

			_position = new Vector4(inverse.Translation.X, inverse.Translation.Y, inverse.Translation.Z, 0);
			_orientation = modelView;
			_orientation.M41 = _orientation.M42 = _orientation.M43 = _orientation.M44 = 0;
			_orientation.M14 = _orientation.M24 = _orientation.M34;
			_orientation.M44 = 1;

			_cameraRotX = 0;
			_cameraRotY = 0;
			_modelRotX = 0;
			_modelRotY = 0;

			_mouseLeftPressed = false;
			_mouseRightPressed = false;

			UpdateVectors();
		}

		public Matrix4x4 ModelView()
		{
			var cameraRotX = (float)(_modelRotY / 300.0);
			var cameraRotY = (float)(_modelRotX / 300.0);

			var model = Matrix4x4.Identity.RotateBy(new Vector3(0.0f, cameraRotY * MathExtensions.ToRadians(90.0f), 0)) *
				Matrix4x4.Identity.RotateBy(new Vector3(cameraRotX * MathExtensions.ToRadians(90.0f), 0, 0));

			var view = Matrix4x4.CreateTranslation(-new Vector3(_position.X, _position.Y, _position.Z)) * _orientation;

			return model * view;
		}

		public bool OnKeyDown(Key key)
		{
			switch (key)
			{
				case Key.S: _cameraMovingBackward = true; return true;
				case Key.W: _cameraMovingForward = true; return true;
				case Key.A: _cameraMovingLeft = true; return true;
				case Key.D: _cameraMovingRight = true; return true;
				case Key.ControlLeft: _cameraMovingDown = true; return true;
				case Key.ShiftLeft: _cameraMovingUp = true; return true;
				default: return false;
			}
		}

		public bool OnKeyUp(Key key)
		{
			switch (key)
			{
				case Key.S: _cameraMovingBackward = false; return true;
				case Key.W: _cameraMovingForward = false; return true;
				case Key.A: _cameraMovingLeft = false; return true;
				case Key.D: _cameraMovingRight = false; return true;
				case Key.ControlLeft: _cameraMovingDown = false; return true;
				case Key.ShiftLeft: _cameraMovingUp = false; return true;
				default: return false;
			}
		}

		public bool OnCursorPosition(double xpos, double ypos)
		{
			var deltaX = (float)(xpos - _mousePosX);
			var deltaY = (float)(ypos - _mousePosY);

			if (_mouseLeftPressed)
			{
				_cameraRotX += deltaX;
				_cameraRotY += deltaY;
			}

			if (_mouseRightPressed)
			{
				_modelRotX += deltaX;
				_modelRotY += deltaY;
			}

			_mousePosX = xpos;
			_mousePosY = ypos;

			return _mouseLeftPressed || _mouseRightPressed;
		}

		public bool OnMouseButtonDown(MouseButton button)
		{
			if (button == MouseButton.Left)
				_mouseLeftPressed = true;

			if (button == MouseButton.Right)
				_mouseRightPressed = true;

			return true;
		}

		public bool OnMouseButtonUp(MouseButton button)
		{
			if (button == MouseButton.Left)
				_mouseLeftPressed = false;

			if (button == MouseButton.Right)
				_mouseRightPressed = false;

			return true;
		}

		public bool UpdateCamera(double speed, double timeDelta)
		{
			var d = (float)(speed * timeDelta);

			if (_cameraMovingLeft) MoveRight(-d);
			if (_cameraMovingRight) MoveRight(d);
			if (_cameraMovingBackward) MoveForward(-d);
			if (_cameraMovingForward) MoveForward(d);
			if (_cameraMovingDown) MoveUp(-d);
			if (_cameraMovingUp) MoveUp(d);

			float rotationDiv = 300;
			Rotate(_cameraRotX / rotationDiv, _cameraRotY / rotationDiv);

			bool updated =
				_cameraMovingLeft ||
				_cameraMovingRight ||
				_cameraMovingBackward ||
				_cameraMovingForward ||
				_cameraMovingDown ||
				_cameraMovingUp ||
				_cameraRotY != 0 ||
				_cameraRotX != 0;

			_cameraRotY = 0;
			_cameraRotX = 0;

			return updated;
		}

		private void MoveForward(float d) => _position += d * _forward;
		private void MoveRight(float d) => _position += d * _right;
		private void MoveUp(float d) => _position += d * _up;
		private void Rotate(float y, float x)
		{
			_orientation = Matrix4x4.Identity.RotateBy(new Vector3(0, y, 0)) * _orientation * Matrix4x4.Identity.RotateBy(new Vector3(x, 0, 0));

			UpdateVectors();
		}

		private void UpdateVectors()
		{
			// Given the ortientation matrix, find out the x,y,z vector orientation.
			_right = _orientation.Multiply(new Vector4(1, 0, 0, 0));
			_up = _orientation.Multiply(new Vector4(0, 1, 0, 0));
			_forward = _orientation.Multiply(new Vector4(0, 0, -1, 0));
		}
	}
}
