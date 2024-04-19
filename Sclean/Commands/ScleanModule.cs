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
using System.Net.Sockets;
using Torch.API.Managers;

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
            respondGridData(gridData, "List Deletable", false, false, false, true);
        }

        [Command("list nop", "List potental removals ignoring players")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListNop()
        {
            Log.Info("list nop command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            respondGridData(gridData,"List Deletable Ignoring Players", true, false, false, true);
        }

        [Command("list all", "List all grids considered")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListAll()
        {
            Log.Info("list all command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            respondGridData(gridData, "List All", true, true, true, true);
        }

        [Command("list prot", "List all grids that are protected from deletion.")]
        [Permission(MyPromoteLevel.Admin)]
        public void ListProt()
        {
            Log.Info("list prot command");
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            respondGridData(gridData, "List Protected", true, true, true, false);
        }

        [Command("stats prot", "Stats of player's protected grid sizes'. Add say at the end to send to chat")]
        [Permission(MyPromoteLevel.Admin)]
        public void StatsProt(string opt1=null)
        {
            Log.Info("stats command");
            bool toChat = false;
            if (opt1 != null && opt1.ToLower() == "say")
            {
                toChat = true;
            }
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            statsGridData(gridData, toChat, "Stats Protected Grids",true,true,true,false);
        }

        [Command("stats prot nop", "Stats of player's protected grid sizes' ignoring players. Add say at the end to send to chat")]
        [Permission(MyPromoteLevel.Admin)]
        public void StatsProtNop(string opt1 = null)
        {
            Log.Info("stats command");
            bool toChat = false;
            if (opt1 != null && opt1.ToLower() == "say")
            {
                toChat = true;
            }
            CommandImp.GridData gridData;
            gridData = CommandImp.FilteredGridData();
            statsGridData(gridData, toChat, "Stats Protected Grids Ignoring Players", false, true, true, false);
        }

        private void respondGridData(CommandImp.GridData gridData, string title, bool selectPlayer, bool selectBeacon, bool selectPowered, bool selectNone)
        {
            Context.Respond(title);
            
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Active Safe Zones: Player={gridData.PlayerInfos.Count} Beacon={gridData.BeaconInfos.Count}");
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

            sb.AppendLine("");
            sb.AppendLine($"Found {g} groups, total {c} matching the Scrapyard rules");
            
            if (Context.SentBySelf)
            {
                Context.Respond(Environment.NewLine + sb.ToString());
            }
            else
            {
                var m = new DialogMessage("Sclean", null, title, sb.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
        }

        private class PlayerStat()
        {
            public string Name = "";
            public string Prefix = "";
            public int Bin1;
            public int Bin2;
            public int Bin5;
            public int Bin10;
            public int Bin20;
            public int Bin50;
            public int BinMax;
            public int Total;

            public void Update(int value)
            {
                switch (value)
                {
                    case <= 0:
                        break;
                    case <= 1:
                        {
                            ++Bin1;
                            break;
                        }
                    case <= 2:
                        {
                            ++Bin2;
                            break;
                        }
                    case <= 5:
                        {
                            ++Bin5;
                            break;
                        }
                    case <= 10:
                        {
                            ++Bin10;
                            break;
                        }
                    case <= 20:
                        {
                            ++Bin20;
                            break;
                        }
                    case <= 50:
                        {
                            ++Bin50;
                            break;
                        }
                    default:
                        {
                            ++BinMax;
                            break;
                        }
                }
                ++Total;
            }
            public override string ToString()
            {
                return $"{"",-6}{Bin1,7}{Bin2,7}{Bin5,7}{Bin10,7}{Bin20,7}{Bin50,7}{BinMax,7}";
            }

            public string Header()
            {
                return $"{Prefix,-6}{"Bin1",7}{"Bin2",7}{"Bin5",7}{"Bin10",7}{"Bin20",7}{"Bin50",7}{"BinMax",7}";
            }
        }

        private void statsGridData(CommandImp.GridData gridData, bool toChat, string title, bool selectPlayer, bool selectBeacon, bool selectPowered, bool selectNone)
        {
            string playerName = "";
            List<PlayerStat> stats = new List<PlayerStat>();
            PlayerStat playerStat = new PlayerStat();

            gridData.GridGroupInfos.Sort();

            foreach (var gridGroupInfo in gridData.GridGroupInfos)
            {
                if (!gridGroupInfo.Protector.Selection(selectPlayer, selectBeacon, selectPowered, selectNone))
                    continue;

                playerName = gridGroupInfo.Protector.OwnerName;
                if (!playerName.Equals(playerStat.Name))
                {
                    playerStat = new PlayerStat{
                        Name = playerName, 
                        Prefix = "Size"
                    };
                    stats.Add(playerStat);
                }
                foreach (var grid in gridGroupInfo.GridGroup)
                {
                    playerStat.Update(grid.BlocksCount);
                }
            }

            StringBuilder sb = new StringBuilder();
            Context.Respond(title);
            foreach (var stat in stats) {
                sb.AppendLine($"### {stat.Name} Total: {stat.Total}");
                sb.AppendLine($"``{stat.Header()}``");
                sb.AppendLine($"``{stat.ToString()}``");
                sb.AppendLine("");      
            }

            if (toChat)
            {
                Context.Torch.CurrentSession.Managers.GetManager<IChatManagerClient>().SendMessageAsSelf($"{Environment.NewLine}## {title}{Environment.NewLine}{sb.ToString()}");
                return;
            }

            if (Context.SentBySelf)
            {
                Context.Respond(Environment.NewLine + sb.ToString());
                return;
            }

            var m = new DialogMessage("Sclean", null, title, sb.ToString());
            ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
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
