# Sclean
A Torch plugin to do cleanup for the Scrapyard scenario.

Unlike Essential cleanup this works on grid groups, a grid group is a grid with subgrids and connected grids and subgrids.  Example, a base with a four wheeled rover docked via connectors would be one grid group consisting of six grids. The grids are base, rover body and four wheels.
If any grid in a group satisfies one of the requiremets to be kept then the whole grid group is kept.

The requirements for a grid to be kept are:
Is player owned and powered.
Is within the safe zone range of the player.
Is within the safe zone range of the Scrap Beacon.

The safe zones apply to any ownership, this ensures found scrap is not removed.
Beacons do not beed to be operational or fully built.
Any grid group that do not meet these requirments are deleted.

The ranges for the safe zones can be set in configuration.

Use:

!list
Show which grids/groups would be deleted.

!list all
Show all grids/groups.

!deleted
Delete grids/groups

!info
Show current configuration.