using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod.Messages;
using Torch.Mod;
using VRage.Game.ModAPI;
using NLog;
using Sandbox.Game.Entities;

namespace Sclean.Commands
{
    [Category("sclean")]
    public class ScleanModule : CommandModule
    {
        private static readonly Logger Log = LogManager.GetLogger("Sclean");

        [Command("info", "Information about the plugin")]
        [Permission(MyPromoteLevel.Admin)]
        public void Info()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Information {ScleanPlugin.Instance.Version}");
            sb.AppendLine($"Beacon SubtypeId ends with: {ScleanPlugin.Instance.Config.BeaconSubtype}");
            sb.AppendLine("Ranges");
            sb.AppendLine($"  Player: {ScleanPlugin.Instance.Config.PlayerRange}");
            sb.AppendLine($"  Scrap Beacon: {ScleanPlugin.Instance.Config.ScrapBeaconRange}");

            Context.Respond(sb.ToString());
        }

        [Command("delete", "Delete grids using the ScrapYard rules")]
        [Permission(MyPromoteLevel.Admin)]
        public void Delete()
        {
            Log.Info("delete command");
            CommandImp.GridData gridData = CommandImp.FilteredGridData(true, false);

            var c = deleteGrids(gridData);

            Context.Respond($"Deleted {c} grids matching the Scrapyard rules.");
            Log.Info($"Sclean deleted {c} grids matching the Scrapyard rules.");

        }

        [Command("delete nop", "Delete grids using the ScrapYard rules but ignoring players")]
        [Permission(MyPromoteLevel.Admin)]
        public void DeleteNop()
        {
            Log.Info("delete command");
            CommandImp.GridData gridData = CommandImp.FilteredGridData(true, true);

            var c = deleteGrids(gridData);

            Context.Respond($"Deleted {c} grids matching the Scrapyard rules.");
            Log.Info($"Sclean deleted {c} grids matching the Scrapyard rules.");

        }

        private int deleteGrids(CommandImp.GridData gridData)
        {
            var c = 0;
            foreach (var gridGroup in gridData.GridGroups)
            {
                foreach (var grid in gridGroup)
                {
                    c++;
                    Log.Info($"Deleting grid: {grid.EntityId}: {grid.DisplayName}");

                    //Eject Pilot
                    var blocks = grid.GetFatBlocks<MyCockpit>();
                    foreach (var cockpit in blocks)
                    {
                        cockpit.RemovePilot();
                    }

                    grid.Close();
                }
            }
            return c;
        }

        [Command("list", "List potental removals")]
        [Permission(MyPromoteLevel.Admin)]
        public void List()
        {
            Log.Info("list command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData(true, false);
            RespondGridData(gridData);
        }

        [Command("list nop", "List potental removals ignoring players")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListNop()
        {
            Log.Info("list nop command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData(true, true);
            RespondGridData(gridData);
        }

        [Command("list all", "List all grids considered")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListAll()
        {
            Log.Info("list all command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData(false, true);
            RespondGridData(gridData);
        }

        private void RespondGridData(CommandImp.GridData gridData)
        {
            Context.Respond("Listing");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Active Safe Zones: Player={gridData.PlayerPositions.Count} Beacon={gridData.BeaconPositions.Count}");
            int c = 0;
            foreach (var gridGroup in gridData.GridGroups)
            {
                sb.AppendLine("---");
                foreach (var grid in gridGroup)
                {
                    c++;
                    sb.AppendLine($"  {grid.DisplayName} ({grid.BlocksCount} block(s))");
                }
            }

            if (Context.SentBySelf)
            {
                Context.Respond(sb.ToString());
                Context.Respond($"Found {gridData.GridGroups.Count()} groups, total {c} grids matching.");
            }
            else
            {
                var m = new DialogMessage("Sclean", null, $"Found {gridData.GridGroups.Count()} groups, total {c} matching the Scrapyard rules", sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
        }
    }
}
