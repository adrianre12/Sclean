using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace Sclean
{
    public static class Extensions
    {
        public static bool HasBlockType(this IMyCubeGrid grid, string typeName)
        {
            foreach (var block in ((MyCubeGrid)grid).GetFatBlocks())

                if (string.Compare(block.BlockDefinition.Id.TypeId.ToString().Substring(16), typeName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return true;

            return false;
        }
        /// <summary>
        /// Return the Scrap Beacons found or or empty list if none
        /// </summary>
        /// <param name="grid"></param>
        /// <returns></returns>
        public static List<MyCubeBlock> GetScrapBeacons(this IMyCubeGrid grid)
        {
            List<MyCubeBlock> beacons = new List<MyCubeBlock> ();
            //ScleanPlugin.Log.Info($"GetScrapBeacons grid name>{grid.DisplayName}");

            foreach (var block in ((MyCubeGrid)grid).GetFatBlocks())
            {
               //ScleanPlugin.Log.Info($"grid name>{grid.DisplayName} TypeId: {block.BlockDefinition.Id.TypeId.ToString()}");
               
                if (block.BlockDefinition.Id.SubtypeId.ToString().EndsWith("Beacon"))
                {
                    ScleanPlugin.Log.Info($"grid name>{grid.DisplayName} Found SubtypeId: {block.BlockDefinition.Id.SubtypeId.ToString()}");
                    beacons.Add(block); 
                }

            }
            if (beacons.Count > 0) 
                ScleanPlugin.Log.Info($"Number of beacons: {beacons.Count}");

            return beacons;

        }

    }
}
