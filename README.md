A Torch plugin to do cleanup for the Scrapyard scenario.

Unlike Essential Cleanup, Sclean works on grid groups, a grid group is a grid with subgrids and connected grids and subgrids. For example, a base with a four-wheeled rover docked via connectors would be one grid group consisting of six grids. The grids are the base, rover body and four wheels. If any grid in a group satisfies one of the requirements to be kept then the whole grid group is kept.
The requirements for a grid to be kept are: Is player owned and powered. Is within the safe zone range of the player. Is within the safe zone range of the Scrap Beacon.
The safe zones apply to any ownership, this ensures that found scrap is not removed. Beacons do not need to be operational or fully built. Any grid group that does not meet these requirements are deleted.
The ranges for the safe zones can be set in configuration.

Use:

!list
Show which grids/groups would be deleted.

!list all
Show all grids/groups.

!deleted
Delete grids/groups.

!info
Show the current configuration.

Install:
Putting Sclean.zip in the plugins folder will not work as Torch assumes it is a downloaded plugin. It needs to be installed as a local plugin, just extract the zip into a folder called Sclean and Torch will use it.
