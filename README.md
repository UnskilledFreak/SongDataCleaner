# SongDataCleaner
This mod cleans unused or unwanted files and directories from  CustomLevels folder.
It uses SongCore to find the specified files needed by the game.
Every other file gets treated as unwanted or unused and will get deleted.

The deletion process will start after SongCore finishes loading one ore multiple maps or a new map gets selected.

## Why is this a thing?

I've seen many great maps made by the community that comes with more than just the "play-files" so to say. I saw Backup folders of mapping software, old json files and even some other music files that the game wont even load. 

So why wasting space when most people just want to play the game?

## Important

It will only delete files in the CustomLevels folder. 

It does not delete:
- any file in the WIP Folders
- contributor icons

## Dependencies

- [SongCore 2.9.11+](https://github.com/Kylemc1413/SongCore)
- BS Utils 1.4.11+
- BeatSaberMarkupLanguage 1.3.4+