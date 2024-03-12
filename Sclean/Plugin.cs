using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.Views;

namespace Sclean
{
    public class Plugin : TorchPluginBase, IWpfPlugin
    {
        private Persistent<Config> _config = null!;

        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            _config = Persistent<Config>.Load(Path.Combine(StoragePath, "Sclean.cfg"));
        }

        public UserControl GetControl() => new PropertyGrid
        {
            Margin = new(3),
            DataContext = _config.Data
        };
    }
}