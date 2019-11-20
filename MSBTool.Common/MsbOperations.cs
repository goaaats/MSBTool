using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using xivModdingFramework.Exd.FileTypes;
using xivModdingFramework.General.Enums;
using xivModdingFramework.SqPack.FileTypes;

namespace MSBTool.Common
{
    public class MsbOperations
    {
        public static void ExportMsb(string filePath, string outPath)
        {
            ExportMsb(File.Open(filePath, FileMode.Open), outPath);
        }

        public static void ExportMsb(int slot, string gamePath, string outPath)
        {
            var index = new Index(new DirectoryInfo(gamePath));
            var dat = new Dat(new DirectoryInfo(gamePath));

            var msbData = Task.Run(() => dat.GetType2Data($"sound/score/bgm_score_{slot:D3}.msb", true)).Result;

            ExportMsb(new MemoryStream(msbData), outPath);
        }

        public static void ExportMsb(Stream dataStream, string outPath)
        {
            var file = MsbFile.Read(dataStream);

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

            midiFile.Write(outPath);
        }

        public static byte[] CreateMsbFromMidi(string path)
        {

            var midiFile = MidiFile.Read(path);
            while(midiFile.Chunks.Count < 7)
            {
                TrackChunk chunk = new TrackChunk();
                List<Note> dummyTrack = new List<Note>();
                dummyTrack.Add(new Note(new SevenBitNumber((byte)60), 1000, 1));
                chunk.AddNotes(dummyTrack);
                midiFile.Chunks.Add(chunk);
            }

            ValueChange<Tempo> tempo = midiFile.GetTempoMap().Tempo.FirstOrDefault<ValueChange<Tempo>>();
            float bpm = 100;
            if(tempo != null)
            {
                bpm = (float)tempo.Value.BeatsPerMinute;
            }

            ValueChange<TimeSignature> ts = midiFile.GetTempoMap().TimeSignature.FirstOrDefault<ValueChange<TimeSignature>>();
            int numerator = 4;
            int denominator = 4;
            if(ts != null)
            {
                numerator = ts.Value.Numerator;
                denominator = ts.Value.Denominator;
            }

            var file = new MsbFile
            {
                BpmEntries = new List<MsbFile.MsbBpm> { new MsbFile.MsbBpm { Bpm = bpm, Measure1 = (byte)numerator, Measure2 = (byte)denominator } },
                ScoreEntries = new List<MsbFile.MsbScore>()
            };

            var trackChunks = midiFile.GetTrackChunks();

            for (var trackIndex = 0; trackIndex < trackChunks.Count(); trackIndex++)
            {
                var notes = trackChunks.ElementAt(trackIndex).GetNotes();

                var msbScore = new MsbFile.MsbScore { Bars = new List<MsbFile.MsbBar>() };

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

        public static bool ImportMsb(string path, string gamePath, int slot)
        {
            var msbData = CreateMsbFromMidi(path);

            var index = new Index(new DirectoryInfo(gamePath));
            var dat = new Dat(new DirectoryInfo(gamePath));


            if (index.IsIndexLocked(XivDataFile._07_Sound))
            {
                throw new IOException("Could not access game data.");
            }

            var offset = Task.Run(() => dat.ImportType2Data(msbData, $"Custom Perform Score for slot {slot:D3}",
                $"sound/score/bgm_score_{slot:D3}.msb", "Custom Performance", "MSBTool")).Result;

            index.UpdateIndexDatCount(XivDataFile._07_Sound, 2);

            Debug.WriteLine($"Custom performance score for slot {slot:D3} was successfully written to {offset:X}");

            return true;
        }
    }
}
