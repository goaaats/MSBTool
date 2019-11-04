using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using Note = Melanchall.DryWetMidi.Smf.Interaction.Note;

namespace MSBTool
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var file = MsbFile.Read(File.Open("bgm_score_003.msb", FileMode.Open));

            /*
            var midiFile = new MidiFile();

            var tempoMap = TempoMap.Create(new TicksPerQuarterNoteTimeDivision(960),
                Tempo.FromBeatsPerMinute((int) file.BpmEntries[0].Bpm));

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

            //midiFile.Write("test.mid");
            */

            file.BpmEntries[0].Bpm = 120;

            var midiFile = MidiFile.Read("test_fd.mid");

            var notes = midiFile.GetTrackChunks().First().GetNotes();

            for (var i = 0; i < file.ScoreEntries[0].Bars.Count(); i++)
            {
                var note = notes.ElementAt(i);

                Console.WriteLine($"{note.NoteNumber - 30} - {note.Time} - {note.Length}");

                file.ScoreEntries[0].Bars[i].Note = (byte) (note.NoteNumber - 30);
                file.ScoreEntries[0].Bars[i].Length = (uint) note.Length;
                file.ScoreEntries[0].Bars[i].Offset = (uint) note.Time;
            }

            File.WriteAllBytes("test.msb", file.GetBytes());
        }
    }
}
