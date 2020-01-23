using C_Sharp_MIDI_Player;
using C_Sharp_MIDI_Player.Properties;
using C_Sharp_MIDI_Player.Render;
using SharpDX;
using SharpDX.Direct3D11;

namespace C_Sharp_MIDI_Player
{
    class Scene : IDirect3D
    {
        public virtual D3D11 Renderer
        {
            get { return context; }
            set
            {
                if (Renderer != null)
                {
                    Renderer.Rendering -= ContextRendering;
                    Detach();
                }
                context = value;
                if (Renderer != null)
                {
                    Renderer.Rendering += ContextRendering;
                    Attach();
                }
            }
        }
        D3D11 context;

        DefaultRenderer renderer;

        Device device;
        DeviceContext ctx;
        RenderTargetView target;

        public Scene()
        {
        }

        void ContextRendering(object aCtx, DrawEventArgs args) { RenderScene(args); }

        protected void Attach()
        {
            if (Renderer == null)
                return;

            //Init objects here

            renderer = new DefaultRenderer(Renderer);
        }

        protected void Detach()
        {
            //Dispose of objects here
        }

        public void RenderScene(DrawEventArgs args)
        {
            renderer.Render(Renderer);
        }

        void IDirect3D.Reset(DrawEventArgs args)
        {
            if (Renderer != null)
                Renderer.Reset(args);
        }

        void IDirect3D.Render(DrawEventArgs args)
        {
            if (Renderer != null)
                Renderer.Render(args);
        }
    }
}