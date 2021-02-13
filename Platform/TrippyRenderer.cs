using System;
using System.Drawing;
using System.Numerics;
using Myra.Platform;
using TrippyGL;

namespace Myra.Samples.AllWidgets
{
	internal class TrippyRenderer: IMyraRenderer, IDisposable
	{
		private bool _beginCalled;
		
		private readonly GraphicsDevice _device;
		private readonly SimpleShaderProgram _shaderProgram;
		private readonly TextureBatcher _batch;

		private Matrix3x2? _transform;

		private DepthState _oldDepthState;
		private bool _oldFaceCullingEnabled;
		private BlendState _oldBlendState;
		private bool _oldScissorEnabled;
		private Rectangle _scissorRectangle;

		public Rectangle Scissor
		{
			get
			{
				return _scissorRectangle;
			}

			set
			{
				Flush();

				value.X += _device.Viewport.X;
				value.Y += _device.Viewport.Y;

				// TripplyGL Scissor Rect has y-axis facing upwards
				// Hence we require some transforms
				var result = new Viewport(value.X, (int)(_device.Viewport.Height - value.Height - value.Y), (uint)value.Width, (uint)value.Height);
				_device.ScissorRectangle = result;

				_scissorRectangle = value;
			}
		}


		public TrippyRenderer(GraphicsDevice graphicsDevice)
		{
			if (graphicsDevice == null)
			{
				throw new ArgumentNullException(nameof(graphicsDevice));
			}

			_device = graphicsDevice;

			_shaderProgram = SimpleShaderProgram.Create<VertexColorTexture>(graphicsDevice, 0, 0, true);
			UpdateProjection();
			_batch = new TextureBatcher(_device);
			_batch.SetShaderProgram(_shaderProgram);

			AllWidgetsTest.Instance.SizeChanged += Instance_SizeChanged;
		}

		public void Dispose()
		{
			AllWidgetsTest.Instance.SizeChanged -= Instance_SizeChanged;
		}

		private void Instance_SizeChanged(object sender, EventArgs e)
		{
			UpdateProjection();
		}

		private void UpdateProjection()
		{
			_shaderProgram.Projection = Matrix4x4.CreateOrthographicOffCenter(0, _device.Viewport.Width, _device.Viewport.Height, 0, 0, 1);
		}

		public void Begin(Matrix3x2? transform)
		{
			// Save old state
			_oldDepthState = _device.DepthState;
			_oldFaceCullingEnabled = _device.FaceCullingEnabled;
			_oldBlendState = _device.BlendState;
			_oldScissorEnabled = _device.ScissorTestEnabled;

			// Set new state
			_device.DepthState = DepthState.None;
			_device.FaceCullingEnabled = false;
			_device.BlendState = BlendState.AlphaBlend;
			_device.ScissorTestEnabled = true;

			_batch.Begin();

			_beginCalled = true;
			_transform = transform;
		}

		public void End()
		{
			_batch.End();
			_beginCalled = false;

			// Restore old state
			_device.DepthState = _oldDepthState;
			_device.FaceCullingEnabled = _oldFaceCullingEnabled;
			_device.BlendState = _oldBlendState;
			_device.ScissorTestEnabled = _oldScissorEnabled;
		}

		public void Draw(object texture, Vector2 position, Rectangle? sourceRectangle, Color color, float rotation, Vector2 origin, Vector2 scale, float depth)
		{
			var tex = (Texture2D)texture;

			_batch.Draw(tex,
				position,
				sourceRectangle,
				color.ToTrippy(),
				scale,
				rotation,
				origin,
				depth);
		}

		public void Draw(object texture, Rectangle dest, Rectangle? src, Color color)
		{
			var tex = (Texture2D)texture;

			Vector2 srcSize = src != null ? new Vector2(src.Value.Width, src.Value.Height) : new Vector2(tex.Width, tex.Height);

			Vector2 scale = new Vector2(dest.Width / srcSize.X,
				dest.Height / srcSize.Y);

			_batch.Draw(tex,
				new Vector2(dest.X, dest.Y),
				src,
				color.ToTrippy(),
				scale, 
				0);
		}

		private void Flush()
		{
			if (_beginCalled)
			{
				End();
				Begin(_transform);
			}
		}
	}
}
