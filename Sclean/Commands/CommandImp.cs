using NLog;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            int maxPlayerBeaconOffset = playerRange - beaconRange;
            int maxPlayerBeaconOffsetSqr = maxPlayerBeaconOffset * maxPlayerBeaconOffset;

            // Grid specific filtering is done in GetGridData
            GridData gridData = GetGridData(false);
            GridData filteredGridData = new GridData
            {
                GridGroups = new List<List<MyCubeGrid>>(),
                BeaconPositions = new List<Vector3D>(),
                PlayerPositions = new List<Vector3D>(),
            };

            // get player positions
            foreach(var player in MySession.Static.Players.GetOnlinePlayers()) {
                filteredGridData.PlayerPositions.Add(player.GetPosition());
            }

            //Assume player range is larger than beacon range and find if beacons are inside players range
            bool useBeacon;
            foreach (var beaconPosition in gridData.BeaconPositions)
            {
                useBeacon = true;
                foreach(var playerPosition in filteredGridData.PlayerPositions)
                {
                    if(Vector3D.DistanceSquared(beaconPosition,playerPosition) < maxPlayerBeaconOffsetSqr)
                    {
                        useBeacon = false;
                    }
                }
                if (useBeacon)
                {
                    filteredGridData.BeaconPositions.Add(beaconPosition);
                }
            }

            // filter by distance from player and beacon. Non player grids also have to be protected.
            bool useGroup;
            foreach (var gridGroup in gridData.GridGroups)
            {
                useGroup = true;
                foreach (var grid in gridGroup)
                {
                    foreach (var playerPosition in filteredGridData.PlayerPositions)
                    {
                        if (Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), playerPosition) < playerRangeSqr)
                        {
                            useGroup = false;
                            break;
                        }
                    }

                    foreach (var beaconPosition in filteredGridData.BeaconPositions)
                    {
                        if (Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), beaconPosition) < beaconRangeSqr)
                        {
                            useGroup = false;
                            break;
                        }
                    }

                    if(!useGroup)
                        break;
                }

                if (useGroup)
                {
                    filteredGridData.GridGroups.Add(gridGroup);
                }             
            }

            return filteredGridData;
        }


        public struct GridData
        {
            public List<List<MyCubeGrid>> GridGroups;
            public List<Vector3D> BeaconPositions;
            public List<Vector3D> PlayerPositions;

            public int CountGrids()
            {
                int c = 0;
                foreach (var grid in GridGroups)
                {
                    c += grid.Count();
                }
                return c;
            }
        }

        /// <summary>
        /// Scan all the grids to find those elegable for removal by grid features (powered etc) and Scrap Beacon positions. If fullList = true the list does not have inelegable grids removed.
        /// </summary>
        /// <param name="fullList"></param>
        /// <returns></returns>
        public static GridData GetGridData(bool fullList)
        {
            var gridData = new GridData();
            var gridGroups = new List<List<MyCubeGrid>>();
            var beaconPositions = new List<Vector3D>();

            Log.Info($"Number of groups {MyCubeGridGroups.Static.Logical.Groups.Count}");
            Parallel.ForEach(MyCubeGridGroups.Static.Logical.Groups, (group) =>
            {
                //Due to the locking do two stages, first does all the filtering and takes a long time. Second is a quick add to results.
                Log.Info($"Number of Nodes in group {group.Nodes.Count}");
                int c = 0;
                bool store = true;
                foreach (var node in group.Nodes.Where(x => x.NodeData.Projector == null))
                {
                    MyCubeGrid grid = node.NodeData;
                    var gridInfo = GetGridInfo(grid);
                    Log.Info($"Grid: {node.NodeData.DisplayName} #Beacons: {gridInfo.BeaconPositions.Count} Owner: {gridInfo.Owner} IsPowered: {gridInfo.IsPowered}");

                    // has player beacon
                    if (gridInfo.BeaconPositions.Count > 0 && gridInfo.Owner == OwnerType.Player)
                    {
                        lock (beaconPositions)
                        {
                            beaconPositions.AddRange(gridInfo.BeaconPositions);
                        }
                        store = fullList; // dont store this group if it has a scrap beacon except for fullList=true
                    }
                    
                    // player grid and has power
                    if (gridInfo.Owner == OwnerType.Player && gridInfo.IsPowered)
                    {
                        store = fullList;
                    }
                    //Log.Info($"Grid: {node.NodeData.DisplayName} Store: {store}");

                }

                if (store)
                {
                    lock (gridGroups)
                    {
                        List<MyCubeGrid> gridGroup = new List<MyCubeGrid>();
                        foreach (var node in group.Nodes.Where(x => x.NodeData.Projector == null))
                        {
                            gridGroup.Add(node.NodeData);
                            c++;
                        }
                        gridGroups.Add(gridGroup);
                        Log.Info($"GridGroups Added group size {gridGroup.Count}");
                    }
                }

            });

            gridData.GridGroups = gridGroups;
            gridData.BeaconPositions = beaconPositions;

            Log.Info($"GridGroups count {gridGroups.Count}");
            return gridData;
        }

        private struct GridInfo
        {
            public List<Vector3D> BeaconPositions;
            public bool IsPowered;
            public OwnerType Owner;
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
                BeaconPositions = new List<Vector3D>()
            };

            MyResourceSourceComponent? component;
            long ownerId;

            foreach (var block in ((MyCubeGrid)grid).GetFatBlocks())
            {
                //Log.Info($"grid name>{grid.DisplayName} TypeId: {block.BlockDefinition.Id.TypeId.ToString()}");

                if (block.BlockDefinition.Id.SubtypeId.ToString().EndsWith("Beacon"))
                {
                    //Log.Info($"grid name>{grid.DisplayName} Found SubtypeId: {block.BlockDefinition.Id.SubtypeId}");
                    gridInfo.BeaconPositions.Add(block.PositionComp.GetPosition());
                }


                component = block.Components?.Get<MyResourceSourceComponent>();
                if (component != null && component.ResourceTypes.Contains(MyResourceDistributorComponent.ElectricityId))
                {
                    if (component.HasCapacityRemainingByType(MyResourceDistributorComponent.ElectricityId) && component.ProductionEnabledByType(MyResourceDistributorComponent.ElectricityId))
                        gridInfo.IsPowered = true;
                }

                if (grid.BigOwners.Count > 0 && grid.BigOwners[0] != 0)
                    ownerId = grid.BigOwners[0];
                else if (grid.BigOwners.Count > 1)
                    ownerId = grid.BigOwners[1];
                else
                    ownerId = 0L;

                if (ownerId == 0L)
                    gridInfo.Owner = OwnerType.Nobody;
                else if (MySession.Static.Players.IdentityIsNpc(ownerId))
                    gridInfo.Owner = OwnerType.NPC;
                else
                    gridInfo.Owner = OwnerType.Player;
            }
            /*if (gridInfo.BeaconPositions.Count > 0)
                Log.Info($"GridInfo  #beacons: {gridInfo.BeaconPositions.Count}");
            Log.Info($"GridInfo IsPowered: {gridInfo.IsPowered} (grid.Ispowered {grid.IsPowered} GridInfo Owner: {gridInfo.Owner}");*/
            return gridInfo;

        }
    }
}
