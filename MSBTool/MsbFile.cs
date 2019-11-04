using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MSBTool
{
    class MsbFile
    {
        public enum MsbNote
        {

        }

        public class MsbBar
        {
            public uint Length { get; set; }
            public uint Offset { get; set; }
            public byte Note { get; set; }
        }

        public class MsbScore
        {
            public List<MsbBar> Bars { get; set; }
        }

        public class MsbBpm
        {
            public byte Measure1 { get; set; }
            public byte Measure2 { get; set; }

            public float Bpm { get; set; }
        }

        public List<MsbScore> ScoreEntries { get; private set; }
        public List<MsbBpm> BpmEntries { get; private set; }

        public static MsbFile Read(Stream stream)
        {
            var file = new MsbFile();   

            file.ScoreEntries = new List<MsbScore>();

            using (var reader = new BinaryReader(stream))
            {
                if (reader.ReadInt32() != 0x4642534D) // MSBF
                    throw new ArgumentException("Not a MSB file", nameof(stream));

                stream.Position++; //?

                var scoreCount = reader.ReadByte();
                var headerSize = reader.ReadUInt16();

                var musicLength = reader.ReadUInt32();

                stream.Position += 4;

                var extendedHeaderSize = reader.ReadUInt32();
                stream.Position = extendedHeaderSize; // Extended header, maybe instruments for ensembles?



                // BPM HEADER
                stream.Position += 4;
                var bpmHeaderCount = reader.ReadUInt32();

                stream.Position += 8;

                file.BpmEntries = new List<MsbBpm>();
                for (var bpmIndex = 0; bpmIndex < bpmHeaderCount; bpmIndex++)
                {
                    var bpm = new MsbBpm();

                    stream.Position++; //?

                    bpm.Measure1 = reader.ReadByte();
                    bpm.Measure2 = reader.ReadByte();

                    stream.Position++;

                    bpm.Bpm = reader.ReadSingle();

                    reader.ReadUInt32(); // Unknown
                    reader.ReadUInt32(); 

                    file.BpmEntries.Add(bpm);
                }


                for (var i = 0; i < scoreCount; i++)
                {
                    var score = new MsbScore();
                    score.Bars = new List<MsbBar>();

                    stream.Position += 4;

                    var noteCount = reader.ReadUInt32();

                    stream.Position += 8;

                    for (var noteIndex = 0; noteIndex < noteCount; noteIndex++)
                    {
                        var bar = new MsbBar();

                        stream.Position++; //?

                        bar.Note = reader.ReadByte();

                        stream.Position += 2; //?

                        bar.Offset = reader.ReadUInt32();
                        bar.Length = reader.ReadUInt32();

                        reader.ReadUInt32(); // Unknown

                        score.Bars.Add(bar);
                    }

                    file.ScoreEntries.Add(score);
                }
            }

            return file;
        }

        // This is just terrible but it works
        public byte[] GetBytes()
        {
            var length = CalculateFileLength();
            var lastBar = GetLastBar();
            var musicLength = lastBar.Offset + lastBar.Length;

            using (var stream = new MemoryStream(length))
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(0x4642534D);

                    writer.Write((byte) 0x00);

                    writer.Write((byte) ScoreEntries.Count); // Should be 7, anything else is undefined

                    writer.Write((ushort) 0x0010); // Base header len

                    writer.Write((uint) musicLength); // Music length, lets hardcode for now

                    writer.Write((uint) 0x00000000);

                    writer.Write(new byte[]
                    {
                        0x30, 0x00, 0x00, 0x00, 0x50, 0x00, 0x00, 0x00, 0xC0, 0x03, 0x00, 0x00, 0xD0, 0x0A, 0x00, 0x00,
                        0xE0, 0x17, 0x00, 0x00, 0x70, 0x1A, 0x00, 0x00, 0x70, 0x1C, 0x00, 0x00, 0x60, 0x1F, 0x00, 0x00
                    }); // Extended header?


                    // BPM

                    writer.Write((uint) 0x00101000); //???

                    writer.Write((uint) 0x00000001); // BPM Entries
                    writer.Write((uint) 0x00000000);
                    writer.Write((uint) 0x00000000);

                    writer.Write((byte) 0x00);

                    writer.Write((byte) BpmEntries[0].Measure1);
                    writer.Write((byte) BpmEntries[0].Measure2);

                    writer.Write((byte)0x00);

                    writer.Write(BpmEntries[0].Bpm);

                    writer.Write((uint)0x00000000);
                    writer.Write((uint)0x00000000);



                    foreach (var scoreEntry in ScoreEntries)
                    {
                        // SCORE HEADER
                        writer.Write((uint) 0x01101001); // TODO: figure out what these mean

                        writer.Write((uint) scoreEntry.Bars.Count);

                        writer.Write((uint)0x00000000);
                        writer.Write((uint)0x00000000);

                        foreach (var bar in scoreEntry.Bars)
                        {
                            writer.Write((byte)0x00);
                            writer.Write((byte) bar.Note);
                            writer.Write((ushort) 0x0000);

                            writer.Write((uint) bar.Offset);
                            writer.Write((uint)bar.Length);

                            writer.Write((uint)0x00000000);
                        }
                    }

                    return stream.GetBuffer();
                }
            }
        }

        private MsbBar GetLastBar()
        {
            return ScoreEntries.OrderByDescending(i => i.Bars.Last().Offset).First().Bars.Last();
        }

        private int CalculateFileLength()
        {
            var length = 0x50 + // File + BPM header
                         ScoreEntries.Count * 0x10 + // Score headers
                         ScoreEntries.Sum(scoreEntry => scoreEntry.Bars.Count * 0x10); // Notes

            return length;
        }
    }
}
