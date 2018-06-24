using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace FFXV_Image_Extractor
{
    static class Program
    {
        private static readonly string STEAM_DIRECTORY = @"%USERPROFILE%\Documents\My Games\FINAL FANTASY XV\Steam\";

        private static string FindSnapshotFolder()
        {
            //Translate the %USERPROFILE%
            var dir = Environment.ExpandEnvironmentVariables(STEAM_DIRECTORY);

            //Check that it exists
            if (Directory.Exists(dir))
            {
                //Enumerate all the directories in the FFXV\Steam folder
                var subdirs = Directory.EnumerateDirectories(dir);

                //Find the one with a savestorage folder
                var folder = subdirs.FirstOrDefault(sub =>
                {
                    return Directory.EnumerateDirectories(sub)
                                    .Select  (subsub => Path.GetFileName(subsub))
                                    .Contains("savestorage");
                });

                //If we found it then return the full path
                if (folder != null) return Path.Combine(folder, @"savestorage\snapshot\");
            }

            //Folder is somewhere else
            return null;
        }

        private static int FindByteSequence(ReadOnlySpan<byte> input, ReadOnlySpan<byte> sequence, int direction)
        {
            //Check if we're searching forwards
            if (direction > 0) return input.IndexOf(sequence);

            //Check if we're searching backwards
            if (direction < 0) return input.LastIndexOf(sequence);

            //Bad input
            return -1;
        }

        public static void Main(string[] args)
        {
            Console.WriteLine("Starting...\n");

            //Find the snapshot folder
            var dir = FindSnapshotFolder();

            //Check if we found it
            if (dir != null)
            {
                Console.WriteLine($"Found snapshot folder at {dir}");
            }
            else
            {
                //Ask the user
                Console.WriteLine("ERROR: Couldn't find the snapshot folder!");
                Console.WriteLine("Please find the \"My Games\\FINAL FANTASY XV\\Steam\\numbers\\savestorage\\snapshot\\\" folder.");
                Console.Write    ("Enter full path here: ");
                dir = Console.ReadLine();
            }

            //Ensure output directory exists
            var outputDirectory = Path.Combine(dir, "converted\\");
            Directory.CreateDirectory(outputDirectory);

            //Get the files
            var files = Directory.EnumerateFiles(dir)
                                 .Where (f => string.Equals(Path.GetExtension(f), ".ss"))
                                 .ToList();

            //Iterate over them
            Console.WriteLine($"\nProcessing {files.Count} files...\n");
            files.ForEach(f =>
            {
                Console.Write($"Processing {Path.GetFileName(f)}... ");

                //Catch any errors
                try
                {
                    //Read all the bytes
                    var data = File.ReadAllBytes(f);

                    //Search for the FFD8 JPEG starting bits
                    var start = FindByteSequence(data, new byte[] { 0xFF, 0xD8 }, 1);
                    if (start == -1) throw new Exception();

                    //Search for the FFD9 JPEG ending bits
                    var end = FindByteSequence(data, new byte[] { 0xFF, 0xD9 }, -1);
                    if (end == -1) throw new Exception();

                    //Check that the end is after the start
                    if (end <= start) throw new Exception();

                    //Extract the JPEG file and write it out
                    var jpeg = data.Skip(start).Take(end - start).ToArray();
                    var path = Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(f) + ".jpeg");
                    File.WriteAllBytes(path, jpeg);

                    //Success!
                    Console.WriteLine($"Done!");
                }
                catch
                {
                    //There was an error
                    Console.WriteLine($"Failed!");
                }
            });

            //Open the folder
            Process.Start("explorer.exe", Path.Combine(dir, "converted\\"));

            //Wait for user to exit
            Console.WriteLine("\nDone!");
            Console.WriteLine("Press ENTER/RETURN to exit.");
            Console.ReadLine();
        }
    }
}
