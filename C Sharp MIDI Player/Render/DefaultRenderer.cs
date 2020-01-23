using SharpDX;
using SharpDX.DXGI;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace C_Sharp_MIDI_Player.Render
{
    [StructLayout(LayoutKind.Sequential)]
    struct VertexStruct
    {
        public Vector2 pos;
        public Color4 col;

        public VertexStruct(Vector2 pos, Color4 col)
        {
            this.pos = pos;
            this.col = col;
        }
    }
    class DefaultRenderer
    {
        private SharpDX.Direct3D11.Device device;
        private DeviceContext context;
        private RenderTargetView target;

        private ShaderManager noteShader;

        private static String noteShaderData = File.ReadAllText("C:/Users/Kyle Berkof/source/repos/C Sharp MIDI Player/C Sharp MIDI Player/Notes.fx", Encoding.UTF8);

        private InputLayout noteLayout;
        private InputLayout vertLayout;

        private float[] noteVertexBuffer = {1,1,0,1,0,0,1,0};

        private float[] noteColorBuffer = {1,1,1,1};

        private VertexStruct[] buffer = new VertexStruct[8 * 10000];

        private Buffer dxBuffer;

        public ShaderBytecode vertexShaderByteCode;
        public VertexShader vertexShader;
        public ShaderBytecode pixelShaderByteCode;
        public PixelShader pixelShader;

        private Random random;

        public DefaultRenderer(D3D11 renderer) {
            random = new Random();

            device = renderer.Device;
            context = device.ImmediateContext;
            target = renderer.RenderTargetView;

            dxBuffer = new Buffer(device, new BufferDescription()
            {
                BindFlags = BindFlags.VertexBuffer,
                CpuAccessFlags = CpuAccessFlags.Write,
                OptionFlags = ResourceOptionFlags.None,
                SizeInBytes = 6 * 4 * buffer.Length,
                Usage = ResourceUsage.Dynamic,
                StructureByteStride = 0
            });

            vertexShaderByteCode = ShaderBytecode.Compile(noteShaderData, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None);
            pixelShaderByteCode = ShaderBytecode.Compile(noteShaderData, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None);

            vertexShader = new VertexShader(device, vertexShaderByteCode);
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            noteShader = new ShaderManager(
                device,
                ShaderBytecode.Compile(noteShaderData, "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None),
                ShaderBytecode.Compile(noteShaderData, "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None)
            );

            vertLayout = new InputLayout(device, ShaderSignature.GetInputSignature(vertexShaderByteCode), new[] {
                new InputElement("POSITION",0,Format.R32G32_Float,0,0),
                new InputElement("COLOR",0,Format.R32G32B32A32_Float,8,0),
            });
        }

        public void Render(D3D11 renderer)
        {
            device = renderer.Device;
            context = device.ImmediateContext;
            target = renderer.RenderTargetView;

            context.InputAssembler.InputLayout = vertLayout;

            context.VertexShader.Set(vertexShader);
            context.PixelShader.Set(pixelShader);

            /*
            for (int i = 0; i < 100; i ++) {
                buffer[i * 3 + 0] = new VertexStruct(new Vector2((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1), new Color4((float) random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), 1));
                buffer[i * 3 + 1] = new VertexStruct(new Vector2((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1), new Color4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), 1));
                buffer[i * 3 + 2] = new VertexStruct(new Vector2((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1), new Color4((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble(), 1));
            }
            */

            buffer[0] = new VertexStruct(new Vector2(0, 0), new Color4(1, 1, 1, 1));
            buffer[1] = new VertexStruct(new Vector2(1, 0), new Color4(1, 1, 1, 1));
            buffer[2] = new VertexStruct(new Vector2(1, 1), new Color4(1, 1, 1, 1));

            buffer[3] = new VertexStruct(new Vector2(0, 0), new Color4(1, 1, 1, 1));
            buffer[4] = new VertexStruct(new Vector2(-1, 0), new Color4(1, 1, 1, 1));
            buffer[5] = new VertexStruct(new Vector2(-1, -1), new Color4(1, 1, 1, 1));

            context.ClearRenderTargetView(target, new Color4(0, 0, 0, 1));

            FlushNoteBuffer(context, buffer, buffer.Length);
        }

        void FlushNoteBuffer(DeviceContext context, VertexStruct[] notes, int count)
        {
            if (count == 0) return;
            DataStream data;
            context.MapSubresource(dxBuffer, 0, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out data);
            data.Position = 0;
            data.WriteRange(notes, 0, count);
            context.UnmapSubresource(dxBuffer, 0);
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(dxBuffer, 12, 0));
            context.Draw(count, 0);
            data.Dispose();
        }
    }
}
