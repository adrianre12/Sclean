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
        /// <summary>
        /// Performs the distance filter on gridData
        /// </summary>
        /// <returns></returns>
       public static GridData DistanceFilterGridData(GridData gridData)
        {
            int playerRange = ScleanPlugin.Instance.Config.PlayerRange;
            int playerRangeSqr = playerRange * playerRange;
            int beaconRange = ScleanPlugin.Instance.Config.ScrapBeaconRange;
            int beaconRangeSqr = beaconRange * beaconRange;
            int maxPlayerBeaconOffset = playerRange - beaconRange;
            int maxPlayerBeaconOffsetSqr = maxPlayerBeaconOffset * maxPlayerBeaconOffset;

            GridData filteredGridData = new GridData
            {
                Grids = new List<MyCubeGrid>(),
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

            // filter
            bool useGrid;
            foreach(var grid in gridData.Grids)
            {
                useGrid = true;
                foreach(var playerPosition in filteredGridData.PlayerPositions)
                {
                    if(Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), playerPosition) < playerRangeSqr)
                    {
                        useGrid = false;
                    }
                }
                if (useGrid) // no point checking beacons if the grid is already in a protected area.
                {
                    foreach (var beaconPosition in filteredGridData.BeaconPositions)
                    {
                        if (Vector3D.DistanceSquared(grid.PositionComp.GetPosition(), beaconPosition) < beaconRangeSqr)
                        {
                            useGrid = false;
                        }
                    }
                }
                if (useGrid)
                {
                    filteredGridData.Grids.Add(grid);
                }
            }

            return filteredGridData;
        }

        public struct GridData
        {
            public List<MyCubeGrid> Grids;
            public List<Vector3D> BeaconPositions;
            public List<Vector3D> PlayerPositions;
        }

        /// <summary>
        /// Scan all the grids to find those elegable for removal and Scrap Beacon positions. If fullList = true the list does not have inelegable grids removed.
        /// </summary>
        /// <param name="fullList"></param>
        /// <returns></returns>
        public static GridData GetGridData(bool fullList)
        {
            var gridData = new GridData();
            var gridList = new List<MyCubeGrid>();
            var beaconPositions = new List<Vector3D>();

            Parallel.ForEach(MyCubeGridGroups.Static.Logical.Groups, (group) =>
            {
                //ScleanPlugin.Log.Info($"Starting group, nodes count {group.Nodes.Count()}");
                //Due to the locking do two stages, first does all the filtering and takes a long time. Second is a quick add to results.
                bool store = true;
                foreach (var node in group.Nodes.Where(x => x.NodeData.Projector == null))
                {
                    store = true;
                    ScleanPlugin.Log.Info($"Starting {node.NodeData.DisplayName}");

                    MyCubeGrid grid = node.NodeData;
                    var gridInfo = GetGridInfo(grid);

                    // has player beacon
                    if (gridInfo.BeaconPositions.Count > 0 && gridInfo.Owner == OwnerType.Player)
                    {
                        lock (beaconPositions)
                        {
                            beaconPositions.AddRange(gridInfo.BeaconPositions);
                        }
                        store = fullList; // dont store this grid if it has a scrap beacon except for fullList=true
                    }
                    
                    // player grid and has power
                    if (gridInfo.Owner == OwnerType.Player && gridInfo.IsPowered)
                    {
                        store = fullList;
                    }
                }

                if (store)
                    lock (gridList)
                    {
                        foreach (var node in group.Nodes.Where(x => x.NodeData.Projector == null))
                        {
                            ScleanPlugin.Log.Info($"Adding {node.NodeData.DisplayName}");
                            gridList.Add(node.NodeData);
                        }
                    }

            });

            gridData.Grids = gridList;
            gridData.BeaconPositions = beaconPositions;

            return gridData;
        }

        private struct GridInfo
        {
            //public List<MyCubeBlock> Beacons;
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
        /// Consolodated scanning of a grid to retrieve all info in one pass
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        private static GridInfo GetGridInfo(MyCubeGrid grid)
        {
            GridInfo gridInfo = new GridInfo
            {
                BeaconPositions = new List<Vector3D>()
            };

            //ScleanPlugin.Log.Info($"GetScrapBeacons grid name>{grid.DisplayName}");

            MyResourceSourceComponent? component;
            long ownerId;

            foreach (var block in ((MyCubeGrid)grid).GetFatBlocks())
            {
                //ScleanPlugin.Log.Info($"grid name>{grid.DisplayName} TypeId: {block.BlockDefinition.Id.TypeId.ToString()}");

                if (block.BlockDefinition.Id.SubtypeId.ToString().EndsWith("Beacon"))
                {
                    ScleanPlugin.Log.Info($"grid name>{grid.DisplayName} Found SubtypeId: {block.BlockDefinition.Id.SubtypeId}");
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
            if (gridInfo.BeaconPositions.Count > 0)
                ScleanPlugin.Log.Info($"GridInfo  #beacons: {gridInfo.BeaconPositions.Count}");
            ScleanPlugin.Log.Info($"GridInfo IsPowered: {gridInfo.IsPowered} (grid.Ispowered {grid.IsPowered} GridInfo Owner: {gridInfo.Owner}");
            return gridInfo;

        }
    }
}
