using System.Text;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod.Messages;
using Torch.Mod;
using VRage.Game.ModAPI;
using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using static Sclean.Commands.CommandImp;

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
            CommandImp.GridData gridData = CommandImp.FilteredGridData();

            var c = deleteGrids(gridData, false, false, false, true);

            Context.Respond($"Deleted {c} grids matching the Scrapyard rules.");
            Log.Info($"Sclean deleted {c} grids matching the Scrapyard rules.");

        }

        [Command("delete nop", "Delete grids using the ScrapYard rules but ignoring players")]
        [Permission(MyPromoteLevel.Admin)]
        public void DeleteNop()
        {
            Log.Info("delete command");
            CommandImp.GridData gridData = CommandImp.FilteredGridData();

            var c = deleteGrids(gridData, true, false, false, true);

            Context.Respond($"Deleted {c} grids matching the Scrapyard rules.");
            Log.Info($"Sclean deleted {c} grids matching the Scrapyard rules.");

        }

        [Command("list", "List potental removals")]
        [Permission(MyPromoteLevel.Admin)]
        public void List()
        {
            Log.Info("list command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            respondGridData(gridData, false, false, false, true);
        }

        [Command("list nop", "List potental removals ignoring players")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListNop()
        {
            Log.Info("list nop command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            respondGridData(gridData, true, false, false, true);
        }

        [Command("list all", "List all grids considered")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListAll()
        {
            Log.Info("list all command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            respondGridData(gridData, true, true, true, true);
        }

        [Command("list prot", "List all grids that are protected from deletion.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListProt()
        {
            Log.Info("list prot command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            respondGridData(gridData, true, true, true, false);
        }

        private void respondGridData(CommandImp.GridData gridData, bool selectPlayer, bool selectBeacon, bool selectPowered, bool selectNone)
        {
            Context.Respond("Listing");
            
            StringBuilder sb = new StringBuilder();
            int numPlayers = selectPlayer ? 0 : gridData.PlayerInfos.Count;
            sb.AppendLine($"Active Safe Zones: Player={numPlayers} Beacon={gridData.BeaconInfos.Count}");
            int c = 0;
            int g = 0;
            string sectionTitle = "";
            string newSectionTitle = "";
            gridData.GridGroupInfos.Sort();
            foreach (var gridGroupInfo in gridData.GridGroupInfos)
            {
                if (!gridGroupInfo.Protector.Selection(selectPlayer, selectBeacon, selectPowered, selectNone))
                    continue;

                ++g;
                newSectionTitle = $"{gridGroupInfo.Protector.ProtectionType} {gridGroupInfo.SortString()}";
                if (sectionTitle.Equals(newSectionTitle))
                {
                    sb.AppendLine($"---");
                } else
                {
                    sectionTitle = newSectionTitle;
                    sb.AppendLine("");
                    sb.AppendLine(sectionTitle);
                }

                foreach (var grid in gridGroupInfo.GridGroup)
                {
                    ++c;
                    sb.AppendLine($"   {getGridOwner(grid)}: {grid.DisplayName} ({grid.BlocksCount} block(s))");
                }
            }

            if (Context.SentBySelf)
            {
                Context.Respond(sb.ToString());
                Context.Respond($"Found {g} groups, total {c} grids matching.");
            }
            else
            {
                var m = new DialogMessage("Sclean", null, $"Found {g} groups, total {c} matching the Scrapyard rules", sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
        }

        private string getGridOwner(MyCubeGrid grid)
        {
            long ownerId;
            if (grid.BigOwners.Count > 0 && grid.BigOwners[0] != 0)
                ownerId = grid.BigOwners[0];
            else if (grid.BigOwners.Count > 1)
                ownerId = grid.BigOwners[1];
            else
                return "Nobody";

            MyIdentity player = MySession.Static.Players.TryGetIdentity(ownerId);
            if (player == null)
                return "Not Found";

            return player.DisplayName;
        }


        private int deleteGrids(CommandImp.GridData gridData, bool selectPlayer, bool selectBeacon, bool selectPowered, bool selectNone)
        {
            var c = 0;
            foreach (var gridGroupInfo in gridData.GridGroupInfos)
            {
                if (!gridGroupInfo.Protector.Selection(selectPlayer, selectBeacon, selectPowered, selectNone))
                    continue;

                foreach (var grid in gridGroupInfo.GridGroup)
                {
                    c++;
                    Log.Info($"Deleting grid: {grid.EntityId}: {getGridOwner(grid)}: {grid.DisplayName}");

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
    }
}
