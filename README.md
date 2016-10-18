# oMediaCenter
Another media center, web based, plugins to handle where the sources come from

## Current plugins
### uTorrent Plugin
uTorrent plugin can be set to read a utorrent web site and serve the files based on the directory given.


### Directory scanner plugin
Set a directory or set of directories in the app config, it'll scan for files that can be served (mp4 and mkv)

## Future Ideas
* Add libtorrent plugin
* Add transcoder to go between avi and mp4
* Change from polling to "push" (hold the connection open then send when a new command is recieved)
