using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using VRage.Game.ModAPI;

namespace Sclean.Commands
{
    [Category("sclean")]
    public class ScleanModule : CommandModule
    {
        [Command("test", "Dev test")]
        [Permission(MyPromoteLevel.None)]
        public void Test()
        {
            ScleanPlugin.Log.Info("test command");
            Context.Respond("Testing");
            var gridData = CommandImp.GetGridData(true);
            Context.Respond($"Found grids {gridData.Grids.Count().ToString()} beacons {gridData.Beacons.Count()}" );
        }

        [Command("info", "Information about the plugin")]
        [Permission(MyPromoteLevel.None)]
        public void Info()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Information");
            sb.AppendLine("Ranges");
            sb.AppendLine($"  Player: {ScleanPlugin.Instance.Config.PlayerRange}");
            sb.AppendLine($"  Small Beacon: {ScleanPlugin.Instance.Config.SmallScrapBeacon}");
            sb.AppendLine($"  Large Beacon: {ScleanPlugin.Instance.Config.LargeScrapBeacon}");

            Context.Respond(sb.ToString());
        }
    }
}
