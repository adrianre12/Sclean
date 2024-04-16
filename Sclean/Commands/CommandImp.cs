using NLog;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using VRageMath;
using static Sclean.Commands.CommandImp;

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
            GridData filteredGridData = new GridData
            {
                GridGroupInfos = new List<GridGroupInfo>(),
                BeaconInfos = gridData.BeaconInfos,
                PlayerInfos = new List<ProtectorInfo>()
            };

            // get player positions
            foreach (var player in MySession.Static.Players.GetOnlinePlayers())
            {
                ProtectorInfo playerInfo = new ProtectorInfo
                {
                    ProtectionType = ProtectionTypeEnum.Player,
                    Name = player.Identity.DisplayName,
                    OwnerId = player.Identity.IdentityId,
                    Position = player.GetPosition()
                };
                    
                filteredGridData.PlayerInfos.Add(playerInfo);
            }


            // filter by distance from player and beacon. Non player grids also have to be protected.
            bool isProtectedGroup = false;
            ProtectorInfo protector = new ProtectorInfo();

            foreach (var gridGroupInfo in gridData.GridGroupInfos)
            {
                if (gridGroupInfo.Protector.ProtectionType != ProtectionTypeEnum.None)
                    continue;

                isProtectedGroup = false;
                foreach (var grid in gridGroupInfo.GridGroup)
                {
                    foreach (var beaconInfo in filteredGridData.BeaconInfos)
                    {
                        if (Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), beaconInfo.Position) < beaconRangeSqr)
                        {
                            isProtectedGroup = true;
                            protector = beaconInfo;
                            break;
                        }
                    }

                    if (isProtectedGroup)
                        break;

                    foreach (var playerInfo in filteredGridData.PlayerInfos)
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

            return filteredGridData;
        }

        public class ProtectorInfo
        {
            public Vector3D Position = Vector3D.Zero;
            public string Name = "";
            public long OwnerId = 0L;
            public ProtectionTypeEnum ProtectionType = ProtectionTypeEnum.None;


            public bool Selection(bool showAll, bool ignorePlayers)
            {
                if (showAll)
                    return true;

                if (ignorePlayers && ProtectionType == ProtectionTypeEnum.Player)
                    return false;

                return true;
            }
        }


        public enum ProtectionTypeEnum
        {
            None,
            Beacon,
            Player,
            Powered
        }

        public class GridGroupInfo
        {
            public List<MyCubeGrid> GridGroup = new List<MyCubeGrid>();
            public ProtectorInfo Protector = new ProtectorInfo();
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
                long protectingOwnerId = 0;
                bool isUnprotected = true;
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
                        isUnprotected = false;
                        protectingOwnerId = gridInfo.OwnerId;
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
                        OwnerId = protectingOwnerId,
                    }
                };

                if (isUnprotected)
                {
                    gridGroupInfo.Protector.ProtectionType = ProtectionTypeEnum.Powered;
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
            Log.Info($"Unprotected GridGroups: {unprotectedGrids} Protected GridGroups: {protectedGrids}");
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

            if (grid.BigOwners.Count > 0 && grid.BigOwners[0] != 0)
                gridInfo.OwnerId = grid.BigOwners[0];
            else if (grid.BigOwners.Count > 1)
                gridInfo.OwnerId = grid.BigOwners[1];
            else
                gridInfo.OwnerId = 0L;

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
                        Position = block.PositionComp.GetPosition(),
                        Name = block.Name,
                        OwnerId = gridInfo.OwnerId
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
