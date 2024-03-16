using NLog;
using System.IO;
using System.Windows.Controls;
using Torch;
using Torch.API;
using Torch.API.Plugins;
using Torch.Views;

namespace Sclean
{
    public class ScleanPlugin : TorchPluginBase, IWpfPlugin
    {
        public ScleanConfig Config => _config?.Data;
        public static Logger Log = LogManager.GetLogger("Sclean");
        private Persistent<ScleanConfig> _config = null!;

        public static ScleanPlugin Instance { get; private set; }


        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            _config = Persistent<ScleanConfig>.Load(Path.Combine(StoragePath, "Sclean.cfg"));
            Log.Info("Init");
            Instance = this;
        }

        public UserControl GetControl() => new PropertyGrid
        {
            Margin = new(3),
            DataContext = _config.Data
        };
    }
}