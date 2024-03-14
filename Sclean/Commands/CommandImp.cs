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

namespace Sclean.Commands
{
    public static class CommandImp
    {
        public struct GridData
        {
            public List<MyCubeGrid> Grids;
            public List<MyCubeBlock> Beacons;

        }
        /// <summary>
        /// Scan all the grids to find those elegable for removal and Scrap Beacons. If fullList = true the list does not have inelegable grids removed.
        /// </summary>
        /// <param name="fullList"></param>
        /// <returns></returns>
        public static GridData GetGridData(bool fullList)
        {
            var gridData = new GridData();
            var gridList = new List<MyCubeGrid>();
            var beaconList = new List<MyCubeBlock>();

            Parallel.ForEach(MyCubeGridGroups.Static.Logical.Groups, (group) =>
            {
                //Due to the locking do two stages, first does all the filtering and takes a long time. Second is a quick add to results.
                bool store = true;
                foreach (var node in group.Nodes.Where(x => x.NodeData.Projector == null))
                {
                    MyCubeGrid grid = node.NodeData;

                    //ScleanPlugin.Log.Info($"grid name>{grid.DisplayName}");

                    var beaconsFound = grid.GetScrapBeacons();
                    if (beaconsFound.Count > 0)
                    {
                        lock (beaconList)
                        {
                            beaconList.AddRange(beaconsFound);
                        }
                        store = fullList;
                        break; // skip the rest of the checking as this group has a beacon
                    }
                    
                    // player grid
                    
                    // has power
                }

                if (store)
                    lock (gridList)
                    {
                        foreach (var node in group.Nodes.Where(x => x.NodeData.Projector == null))
                        {
                            gridList.Add(node.NodeData);
                        }
                    }

            });

            gridData.Grids = gridList;
            gridData.Beacons = beaconList;

            return gridData;
        }
    }
}
