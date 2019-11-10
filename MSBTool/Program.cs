using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using xivModdingFramework.General.Enums;
using xivModdingFramework.SqPack.FileTypes;
using Index = xivModdingFramework.SqPack.FileTypes.Index;
using Note = Melanchall.DryWetMidi.Smf.Interaction.Note;

namespace MSBTool
{
    public static class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("MSBTool - Import and export FFXIV Perform note sheets\n\nUsage:\nEXPORT TO MIDI: MSBTool.exe export [PATH TO MSB FILE]\nCREATE MSB: MSBTool.exe create [PATH TO MIDI]\nIMPORT TO GAME: MSBTool.exe import [PATH TO MIDI] [PATH TO GAME INSTALL] [SLOT NUMBER IN-GAME]");
                return;
            }

            switch (args[0])
            {
                case "export":
                    ExportMsb(args[1]);
                    break;
                case "create":
                {
                    var data = CreateMsbFromMidi(args[1]);

                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(args[1]) + ".msb", data);
                }
                    break;
                case "import":
                    ImportMsb(args[1], args[3], int.Parse(args[2]));
                    break;
            }


        }

        private static void ExportMsb(string path)
        {
            var file = MsbFile.Read(File.Open(path, FileMode.Open));

            var midiFile = new MidiFile();

            var tempoMap = TempoMap.Create(new TicksPerQuarterNoteTimeDivision(960),
                Tempo.FromBeatsPerMinute((int)file.BpmEntries[0].Bpm));

            foreach (var score in file.ScoreEntries)
            {
                var track = new TrackChunk();

                var scoreNotes = new List<Note>();

                foreach (var bar in score.Bars)
                {
                    scoreNotes.Add(new Note(new SevenBitNumber((byte)(bar.Note + 24)), bar.Length, bar.Offset));
                }

                track.AddNotes(scoreNotes);

                midiFile.Chunks.Add(track);
            }

            midiFile.ReplaceTempoMap(tempoMap);

            midiFile.Write(Path.GetFileNameWithoutExtension(path) + ".mid");
        }

        private static byte[] CreateMsbFromMidi(string path)
        {
            var file = new MsbFile
            {
                BpmEntries = new List<MsbFile.MsbBpm> {new MsbFile.MsbBpm {Bpm = 60, Measure1 = 4, Measure2 = 4}},
                ScoreEntries = new List<MsbFile.MsbScore>()
            };

            // Hardcoded for now

            var midiFile = MidiFile.Read(path);

            var trackChunks = midiFile.GetTrackChunks();

            for (var trackIndex = 0; trackIndex < trackChunks.Count(); trackIndex++)
            {
                var notes = trackChunks.ElementAt(trackIndex).GetNotes();

                var msbScore = new MsbFile.MsbScore {Bars = new List<MsbFile.MsbBar>()};

                for (var i = 0; i < notes.Count(); i++)
                {
                    var note = notes.ElementAt(i);

                    Console.WriteLine($"{note.NoteNumber - 24} - {note.Time} - {note.Length}");

                    var msbBar = new MsbFile.MsbBar();

                    msbBar.Note = (byte)(note.NoteNumber - 24);
                    msbBar.Length = (uint)note.Length;
                    msbBar.Offset = (uint)note.Time;

                    msbScore.Bars.Add(msbBar);
                }

                file.ScoreEntries.Add(msbScore);
            }

            return file.GetBytes();
        }

        public static string ImportMsb(string path, string gamePath, int slot)
        {
            var msbData = CreateMsbFromMidi(path);

            var index = new Index(new DirectoryInfo(gamePath));
            var dat = new Dat(new DirectoryInfo(gamePath));


            if (index.IsIndexLocked(XivDataFile._07_Sound))
            {
                Console.WriteLine("Could not access index file. Game may be open.");
                return "Could not access index file. Game may be open.";
            }

            var offset = dat.ImportType2Data(msbData, $"Custom Perform Score for slot {slot:D3}",
                $"sound/score/bgm_score_{slot:D3}.msb", "Custom Performance", "MSBTool").GetAwaiter().GetResult();

            index.UpdateIndexDatCount(XivDataFile._07_Sound, 2);
            Console.WriteLine($"Custom performance score for slot {slot:D3} was successfully written to {offset:X}");
            return $"Custom performance score for slot {slot:D3} was successfully written to {offset:X}";
        }
    }
}
