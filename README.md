# MSBTool

Tool for importing and exporting FFXIV Performance scores to and from midi.

## Making your own tracks
* Imported midi files *must* have 7 tracks with each having at least one note. At this time, no track besides 1 is played, but we think the other ones are reserved for ensembles and already have notes for the built-in songs.
* Take care that no notes are overlapping. If this is the case they will not travel to the hit point and may not be played.
* All notes must be between C3 and C6. If this is not the case, they will not be shown on the keyboard and playback will stop.

## Usage
```
EXPORT TO MIDI: MSBTool.exe export [PATH TO MSB FILE]
CREATE MSB: MSBTool.exe create [PATH TO MIDI]
IMPORT TO GAME: MSBTool.exe import [PATH TO MIDI] [PATH TO GAME INSTALL] [SLOT NUMBER IN-GAME]
```

## Credits
Thanks to Mino for their research and jesh for helping to implement this!
