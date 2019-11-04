using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Note = Melanchall.DryWetMidi.Smf.Interaction.Note;

namespace MSBTool
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("MSBTool - Import and export FFXIV Perform note sheets\n\nUsage:\nEXPORT TO MIDI: MSBTool.exe export [PATH TO MSB FILE]\nCREATE MSB: MSBTool.exe create [PATH TO MIDI]\nIMPORT TO GAME: MSBTool.exe import [PATH TO MIDI] [SLOT NUMBER IN-GAME]");
                return;
            }

            switch (args[0])
            {
                case "export":
                    ExportMsb(args[1]);
                    break;
                case "create":
                {
                    var data = CreateMsb(args[1]);

                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(args[1]) + ".msb", data);
                }
                    break;
                case "import":
                    //ImportMsb(args[1], int.Parse(args[2]));
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
                    scoreNotes.Add(new Note(new SevenBitNumber((byte)(bar.Note + 30)), bar.Length, bar.Offset));
                }

                track.AddNotes(scoreNotes);

                midiFile.Chunks.Add(track);
            }

            midiFile.ReplaceTempoMap(tempoMap);

            midiFile.Write(Path.GetFileNameWithoutExtension(path) + ".mid");
        }

        private static byte[] CreateMsb(string path)
        {
            var file = new MsbFile
            {
                BpmEntries = new List<MsbFile.MsbBpm> {new MsbFile.MsbBpm {Bpm = 105, Measure1 = 4, Measure2 = 4}},
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

                    Console.WriteLine($"{note.NoteNumber - 30} - {note.Time} - {note.Length}");

                    var msbBar = new MsbFile.MsbBar();

                    msbBar.Note = (byte)(note.NoteNumber - 30);
                    msbBar.Length = (uint)note.Length;
                    msbBar.Offset = (uint)note.Time;

                    msbScore.Bars.Add(msbBar);
                }

                file.ScoreEntries.Add(msbScore);
            }

            return file.GetBytes();
        }
    }
}
