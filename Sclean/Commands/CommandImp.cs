using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using System.Windows.Controls;
using VRageMath;

namespace Sclean.Commands
{
    public static class CommandImp
    {
        private static readonly Logger Log = LogManager.GetLogger("Sclean");

        /// <summary>
        /// Performs the distance filter on gridData
        /// </summary>
        /// <returns></returns>
        public static GridData FilteredGridData()
        {
            int playerRange = ScleanPlugin.Instance.Config.PlayerRange;
            int playerRangeSqr = playerRange * playerRange;
            int beaconRange = ScleanPlugin.Instance.Config.ScrapBeaconRange;
            int beaconRangeSqr = beaconRange * beaconRange;
 
            // Grid specific filtering is done in GetGridData
            GridData gridData = GetGridData();

            // get player positions
            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                ProtectorInfo playerInfo = new ProtectorInfo
                {
                    ProtectionType = ProtectionTypeEnum.Player,
                    Name = player.Identity.DisplayName,
                    OwnerName = player.Identity.DisplayName,
                    OwnerId =  player.Identity.IdentityId,
                    Position = player.GetPosition(),
                };
                    
                gridData.PlayerInfos.Add(playerInfo);
            }


            // filter by distance from player and beacon. Non player grids also have to be protected.
            bool isProtectedGroup = false;
            ProtectorInfo protector = new ProtectorInfo();
            double gridDistanceSqr = 0;
            double minDistanceSqr;

            foreach (var gridGroupInfo in gridData.GridGroupInfos)
            {
                isProtectedGroup = false;
                foreach (var grid in gridGroupInfo.GridGroup)
                {
                    minDistanceSqr = double.MaxValue;
                    foreach (var beaconInfo in gridData.BeaconInfos)
                    {
                        gridDistanceSqr = Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), beaconInfo.Position);
                        if (gridDistanceSqr < beaconRangeSqr)
                        {
                            isProtectedGroup = true;

                            if (FindOwner(grid.BigOwners) == beaconInfo.OwnerId) // grid and beacon owner match give up
                            {
                                protector = beaconInfo;
                                break;
                            }

                            if(gridDistanceSqr < minDistanceSqr) { // find the closest beacon
                                protector = beaconInfo;
                                minDistanceSqr = gridDistanceSqr;
                            }
                        }
                    }

                    if (isProtectedGroup)
                        break;

                    if (gridGroupInfo.Protector.ProtectionType == ProtectionTypeEnum.Powered)
                        continue;

                    foreach (var playerInfo in gridData.PlayerInfos)
                    {
                        if (Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), playerInfo.Position) < playerRangeSqr)
                        {
                            isProtectedGroup = true;
                            protector = playerInfo;
                            break;
                        }
                    }

                    if (isProtectedGroup)
                        break;
                }

                if (isProtectedGroup)
                {
                    gridGroupInfo.Protector = protector;
                }
            }

            return gridData;
        }

        public class ProtectorInfo
        {
            public Vector3D Position = Vector3D.Zero;
            public string Name = "";
            public string OwnerName = "";
            public long OwnerId = 0;
            public ProtectionTypeEnum ProtectionType = ProtectionTypeEnum.None;


            public bool Selection( bool selectPlayer, bool selectBeacon, bool selectPowered, bool selectNone)
            {
                if (selectPlayer && ProtectionType == ProtectionTypeEnum.Player)
                    return true;

                if (selectBeacon && ProtectionType == ProtectionTypeEnum.Beacon)
                    return true;

                if (selectPowered && ProtectionType == ProtectionTypeEnum.Powered)
                    return true; 
                
                if (selectNone && ProtectionType == ProtectionTypeEnum.None)
                    return true;

                return false;
            }
        }


        public enum ProtectionTypeEnum
        {
            None,
            Beacon,
            Player,
            Powered
        }

        public class GridGroupInfo : IComparable<GridGroupInfo> 
        {
            public List<MyCubeGrid> GridGroup = new List<MyCubeGrid>();
            public ProtectorInfo Protector = new ProtectorInfo();

            public int CompareTo(GridGroupInfo other)
            {
                string thisStr = SortString();
                string otherStr = other.SortString();

                return thisStr.CompareTo(otherStr);
            }

            public string SortString()
            {
                return Protector.OwnerName + ": " + Protector.Name;
            }
        }

        public class GridData
        {
            public List<GridGroupInfo> GridGroupInfos = new List<GridGroupInfo>();
            public List<ProtectorInfo> BeaconInfos = new List<ProtectorInfo>();
            public List<ProtectorInfo> PlayerInfos = new List<ProtectorInfo>();

            public void CountGrids(out int protectedGrids, out int unprotectedGrids)
            {
                protectedGrids = 0;
                unprotectedGrids = 0;
                foreach (var gridGroupInfo in GridGroupInfos)
                {
                    if (gridGroupInfo.Protector.ProtectionType == ProtectionTypeEnum.None)
                    {
                        unprotectedGrids += gridGroupInfo.GridGroup.Count();
                        continue;
                    }
                    protectedGrids += gridGroupInfo.GridGroup.Count();
                }
            }
        }

        /// <summary>
        /// Scan all the grids to find those elegable for removal by grid features (powered etc) and Scrap Beacon positions.
        /// </summary>
        /// <returns></returns>
        public static GridData GetGridData()
        {
            var gridData = new GridData();
            var gridGroupInfos = new List<GridGroupInfo>();
            var beaconInfos = new List<ProtectorInfo>();

            Parallel.ForEach(MyCubeGridGroups.Static.Logical.Groups, (group) =>
            {
                //Due to the locking do two stages, first does all the filtering and takes a long time. Second is a quick add to results.
                bool isPowered = false;
                long powerOwnerId = 0;
                foreach (var node in group.Nodes.Where(x => x.NodeData.Projector == null))
                {
                    MyCubeGrid grid = node.NodeData;
                    var gridInfo = GetGridInfo(grid);

                    // has beacon
                    if (gridInfo.BeaconInfos.Count > 0)
                    {
                        lock (beaconInfos)
                        {
                            beaconInfos.AddRange(gridInfo.BeaconInfos);
                        }
                    }

                    // player grid and has power
                    if (gridInfo.Owner == OwnerType.Player && gridInfo.IsPowered)
                    {
                        isPowered = true;
                        powerOwnerId = gridInfo.OwnerId;
                    }
                    //Log.Info($"Grid: {node.NodeData.DisplayName} use: {use} #Beacons: {gridInfo.BeaconPositions.Count} Owner: {gridInfo.Owner} IsPowered: {gridInfo.IsPowered}");
                }

                List<MyCubeGrid> gridGroup = new List<MyCubeGrid>();
                foreach (var node in group.Nodes.Where(x => x.NodeData.Projector == null))
                {
                    gridGroup.Add(node.NodeData);
                }
                GridGroupInfo gridGroupInfo = new GridGroupInfo
                {
                    GridGroup = gridGroup,
                    Protector = new ProtectorInfo
                    {
                        ProtectionType = ProtectionTypeEnum.None,
                    }
                };

                if (isPowered)
                {
                    gridGroupInfo.Protector.ProtectionType = ProtectionTypeEnum.Powered;
                    gridGroupInfo.Protector.OwnerId = powerOwnerId;
                }

                lock (gridGroupInfos)
                {
                    gridGroupInfos.Add(gridGroupInfo);
                    //Log.Info($"GridGroups Added group size {gridGroupInfo.GridGroups.Count}");
                }
            });

            gridData.GridGroupInfos = gridGroupInfos;
            gridData.BeaconInfos =  beaconInfos;

            gridData.CountGrids(out int protectedGrids, out int unprotectedGrids);
            Log.Info($"GetGridData Unprotected GridGroups: {unprotectedGrids} Protected GridGroups: {protectedGrids}");
            return gridData;
        }

        private struct GridInfo
        {
            public List<ProtectorInfo> BeaconInfos;
            public bool IsPowered;
            public OwnerType Owner;
            public long OwnerId;
        }

        /// <summary>
        /// OwnerType Enumerator.
        /// </summary>
        public enum OwnerType
        {
            Nobody,
            NPC,
            Player
        }

        private static string GetOwnerName(long ownerId) {
            MyIdentity player = MySession.Static.Players.TryGetIdentity(ownerId);

            return player == null ? "" : player.DisplayName;
        }

        private static long FindOwner(List<long> bigOwners)
        {
            if (bigOwners.Count > 1)
                return bigOwners[0] != 0 ? bigOwners[0] : bigOwners[1];

            if (bigOwners.Count > 0)
                return bigOwners[0];

            return 0;
        }

        /// <summary>
        /// Consolodated scanning of a grid to retrieve the info in one pass
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        private static GridInfo GetGridInfo(MyCubeGrid grid)
        {
            GridInfo gridInfo = new GridInfo
            {
                BeaconInfos = new List<ProtectorInfo>()
            };

            MyResourceSourceComponent? component;
            string endsWith = ScleanPlugin.Instance.Config.BeaconSubtype;
            gridInfo.OwnerId = FindOwner(grid.BigOwners);

            if (gridInfo.OwnerId == 0L)
                gridInfo.Owner = OwnerType.Nobody;
            else if (MySession.Static.Players.IdentityIsNpc(gridInfo.OwnerId))
                gridInfo.Owner = OwnerType.NPC;
            else
                gridInfo.Owner = OwnerType.Player;

            foreach (var block in ((MyCubeGrid)grid).GetFatBlocks())
            {
                //Log.Info($"grid name>{grid.DisplayName} TypeId: {block.BlockDefinition.Id.TypeId.ToString()}");

                if (block.BlockDefinition.Id.SubtypeId.ToString().EndsWith(endsWith))
                {
                    //Log.Info($"grid name>{grid.DisplayName} Found SubtypeId: {block.BlockDefinition.Id.SubtypeId}");
                    gridInfo.BeaconInfos.Add(new ProtectorInfo
                    {
                        ProtectionType = ProtectionTypeEnum.Beacon,
                        Position = block.PositionComp.GetPosition(),
                        Name = grid.DisplayName,
                        OwnerId = gridInfo.OwnerId,
                        OwnerName = GetOwnerName(gridInfo.OwnerId)
                    });
                }

                component = block.Components?.Get<MyResourceSourceComponent>();
                if (component != null && component.ResourceTypes.Contains(MyResourceDistributorComponent.ElectricityId))
                {
                    if (component.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId) && component.ProductionEnabledByType(MyResourceDistributorComponent.ElectricityId))
                        gridInfo.IsPowered = true;
                }
            }

            return gridInfo;
        }
    }
}
