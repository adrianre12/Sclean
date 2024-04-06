## A Torch plugin for Scrapyard scenario cleanup.
I wrote all the code but to avoid plagiarism I do admit that I have relied heavily on reading Essentials code to show me how to do it.


### Why another plugin? We have Essentials Cleanup.
Unlike Essential Cleanup, Sclean works on grid groups, a grid group is a grid with subgrids and connected grids and subgrids. For example, a base with a four-wheeled rover docked via connectors would be one grid group consisting of six grids. The grids are the base, rover body and four wheels. 
With Essentials Cleanup if any grid in the group fails a condition, the whole group fails. An example would be "!cleanup list hastype Beacon" for a rover with a Beacon, as the wheels do not have Beacons the whole rover fails the condition and is not returned.
With Sclean if any grid in a group satisfies one of the requirements then the whole grid group is kept. This means Sclean can find all grids with Beacons. 


In Sclean the requirements are hard coded partly for efficiency but mainly because the Scrapyard Scenario has very specific rules.
The requirements for a grid to be kept are: 
* Is player-owned and powered. 
* Is within the safe AOE range of the player. 
* Is within the safe AOE range of the Scrap Beacon.


Plus:
* The safe zones apply to any ownership, this ensures that found scrap is not removed. 
* Beacons do not need to be operational or fully built. 
* Any grid group that does not meet these requirements are deleted.
* The ranges for the safe zones can be set in configuration.
* Sclean optimises the scan by not using any beacon that has its AOE inside the player's AOE. 


Sclean only does what Essentials Cleanup can't do, you still need to use Cleanup to remove floating objects and reset voxels 


### Use:


Show which grids/groups would be deleted.
```
!sclean list
```


Show all grids/groups.
```
!sclean list all
```


Delete grids/groups.
```
!sclean delete
```


Show the current configuration.
```
!sclean info
```


### Install:
This has been published and is available via the Torch Client.



