using C_Sharp_MIDI_Player.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace C_Sharp_MIDI_Player
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        D3D11 d3d;
        Scene scene;

        public MainWindow()
        {
            InitializeComponent();

            d3d = new D3D11();

            d3d.FPSLock = 60;
            d3d.SingleThreadedRender = false;
            d3d.SyncRender = false;

            scene = new Scene() { Renderer = d3d };

            dx11img.Renderer = scene;
        }
    }
}
