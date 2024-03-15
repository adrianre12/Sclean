using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod.Messages;
using Torch.Mod;
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
            Context.Respond($"Found grids {gridData.Grids.Count().ToString()} beacons {gridData.BeaconPositions.Count()}" );
        }

        [Command("info", "Information about the plugin")]
        [Permission(MyPromoteLevel.None)]
        public void Info()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Information");
            sb.AppendLine("Ranges");
            sb.AppendLine($"  Player: {ScleanPlugin.Instance.Config.PlayerRange}");
            sb.AppendLine($"  Scrap Beacon: {ScleanPlugin.Instance.Config.ScrapBeaconRange}");

            Context.Respond(sb.ToString());
        }

        [Command("list", "List potental removals")]
        [Permission(MyPromoteLevel.None)]
        public void ListRemovals()
        {
            ScleanPlugin.Log.Info("list command");
            Context.Respond("Listing");
            var gridData = CommandImp.GetGridData(false);
            
            if (Context.SentBySelf)
            {
                Context.Respond(String.Join("\n", gridData.Grids.Select((g, i) => $"{i + 1}. {gridData.Grids[i].DisplayName} ({gridData.Grids[i].BlocksCount} block(s))")));
                Context.Respond($"Found {gridData.Grids.Count} grids matching the given conditions.");
            }
            else
            {
                var m = new DialogMessage("Cleanup", null, $"Found {gridData.Grids.Count} matching", String.Join("\n", gridData.Grids.Select((g, i) => $"{i + 1}. {gridData.Grids[i].DisplayName} ({gridData.Grids[i].BlocksCount} block(s))")));
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
        }
    }
}
