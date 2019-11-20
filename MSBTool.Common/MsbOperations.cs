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

            //Ensures every chunk in the midi has at least 1 note
            foreach (var chunk in midiFile.GetTrackChunks())
            {
                if(chunk.GetNotes().Count() == 0)
                {
                    List<Note> dummyTrack = new List<Note>();
                    dummyTrack.Add(new Note(new SevenBitNumber((byte)60), 1000, 1));
                    chunk.AddNotes(dummyTrack);
                }
            }

            //Ensures there are at least 7 chunks in the midi
            while(midiFile.Chunks.Count < 7)
            {
                TrackChunk chunk = new TrackChunk();
                List<Note> dummyTrack = new List<Note>();
                dummyTrack.Add(new Note(new SevenBitNumber((byte)60), 1000, 1));
                chunk.AddNotes(dummyTrack);
                midiFile.Chunks.Add(chunk);
            }

            //Ensures there are no more than 7 chunks in the midi
            while(midiFile.GetTrackChunks().Count() > 7)
            {
                midiFile.Chunks.RemoveAt(7);
            }

            //Reads BPM from midi if possible; if no BPM is declared, defaults to 100
            ValueChange<Tempo> tempo = midiFile.GetTempoMap().Tempo.FirstOrDefault<ValueChange<Tempo>>();
            float bpm = 100;
            if(tempo != null)
            {
                bpm = (float)tempo.Value.BeatsPerMinute;
            }

            //Assigns time signature based on first midi time signature event, or defaults to 4/4
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

            //If the first chunk contains only one note (dummy chunk or empty chunk)
            //Instead, re-order list to sort the list with the most notes into the first slot.
            if(trackChunks.ElementAt(0).GetNotes().Count() == 1)
            {
                trackChunks = trackChunks.OrderByDescending(c => c.GetNotes().Count());
            }

            for (var trackIndex = 0; trackIndex < trackChunks.Count(); trackIndex++)
            {
                var notes = trackChunks.ElementAt(trackIndex).GetNotes();
                notes = notes.OrderBy(c => c.Time);

                var msbScore = new MsbFile.MsbScore { Bars = new List<MsbFile.MsbBar>() };

                for (var i = 0; i < notes.Count(); i++)
                {
                    var note = notes.ElementAt(i);

                    if((int)note.NoteNumber < 48 || (int)note.NoteNumber > 84)
                    {
                        throw new Exception("Your Midi contains notes outside the range of FFXIV's playable range. " +
                            $"Please enter only notes between C3 and C6 (inclusive). Detected incorrect range at Note#{i + 1} in Track#{trackIndex + 1}");
                    }
                    
                    //Detects note overlaps and chops the length to be strictly < the next note's time.
                    var lengthChopOffset = 0L;
                    if(i != notes.Count() - 1)
                    {
                        var nextNote = notes.ElementAt(i + 1);
                        if(note.Time + note.Length > nextNote.Time)
                        {
                            lengthChopOffset = (note.Time + note.Length) - nextNote.Time;
                        }
                    }

                    var msbBar = new MsbFile.MsbBar();

                    msbBar.Note = (byte)(note.NoteNumber - 24);
                    msbBar.Length = (uint)(note.Length - lengthChopOffset);
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
