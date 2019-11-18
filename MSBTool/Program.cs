using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.MusicTheory;
using Melanchall.DryWetMidi.Smf;
using Melanchall.DryWetMidi.Smf.Interaction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using MSBTool.Common;


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
                    MsbOperations.ExportMsb(args[1], Path.GetFileNameWithoutExtension(args[1]) + ".mid");
                    break;
                case "create":
                {
                    var data = MsbOperations.CreateMsbFromMidi(args[1]);

                    File.WriteAllBytes(Path.GetFileNameWithoutExtension(args[1]) + ".msb", data);
                }
                    break;
                case "import":
                    MsbOperations.ImportMsb(args[1], args[3], int.Parse(args[2]));
                    break;
            }


        }

        
    }
}
