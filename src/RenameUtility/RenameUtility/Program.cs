using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RenameUtility
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            string projectPath;
            string oldName;
            string newName;

            if (args.Length < 3)
            {
                Console.WriteLine("Use: dotnet | Path to Renamer.dll | Path to BlackSlope Project | Old Name | New Name");
                Console.WriteLine(@"Ex: dotnet C:\Renamer\bin\Debug\netcoreapp2.0\Renamer.dll C:\blackslope.net BlackSlope BlockBuster");

                Console.WriteLine("\nWhat is the path to your project?");
                projectPath = Console.ReadLine();

                Console.WriteLine("\nWhat is the old project name?");
                oldName = Console.ReadLine();

                Console.WriteLine("\nWhat is the new project name?");
                newName = Console.ReadLine();

                Console.WriteLine($"\nProject path: {projectPath}\n Old name: {oldName}\n New name: {newName}\n");
                Console.WriteLine("Accept these values? Yes or No");
                var yesNo = Console.ReadLine() ?? "no";

                if (!(yesNo.ToLower() == "yes" || yesNo.ToLower() == "y")) return;
            }
            else
            {
                projectPath = args[0];
                oldName = args[1];
                newName = args[2];
            }

            var ignoreFile = projectPath + @"\IgnoreExtensions.RenameUtility";
            var ignoreExts = new List<string>();

            if (!string.IsNullOrEmpty(ignoreFile))
            {
                try
                {
                    var ignoreFileText = File.ReadAllText(ignoreFile).Split(",");
                    ignoreExts.AddRange(ignoreFileText);
                }
                catch
                {
                    Console.WriteLine($"ERROR - Failed to read IgnoreExtensions.RenameUtility file.");
                }
            }

            Renamer(projectPath, oldName, newName, ignoreExts);
        }

        private static void Renamer(string source, string search, string replace, ICollection<string> ignoreExts)
        {
            var files = Directory.GetFiles(source);

            foreach (var filePath in files)
            {
                ReplaceFileText(search, replace, filePath, ignoreExts);

                var fileIdx = filePath.LastIndexOf('\\');

                if (fileIdx == -1) // is Linux machine
                {
                    fileIdx = filePath.LastIndexOf('/');
                }

                var fileName = filePath.Substring(fileIdx);
                var ext = filePath.Split(".").Last();

                if (ignoreExts.Contains(ext) || !fileName.Contains(search)) continue;

                ReplaceFileName(search, replace, fileName, filePath, fileIdx);
            }

            var subdirectories = Directory.GetDirectories(source);
            foreach (var subdirectory in subdirectories)
            {
                Renamer(subdirectory, search, replace, ignoreExts);

                var folderNameIdx = subdirectory.LastIndexOf('\\') + 1;

                if (folderNameIdx == -1) // is Linux machine
                {
                    folderNameIdx = subdirectory.LastIndexOf('/') + 1;
                }

                var folderName = subdirectory.Substring(folderNameIdx);

                if (!folderName.ToLower().Contains(search.ToLower())) continue;

                ReplaceFolderName(search, replace, subdirectory, folderNameIdx, folderName);
            }
        }

        private static void ReplaceFolderName(string search, string replace, string subdirectory, int folderNameIdx,
            string folderName)
        {
            Console.WriteLine($"Replacing {search} with {replace} in folder name: {folderName}");

            var newDirectory = subdirectory.Substring(0, folderNameIdx) +
                               folderName.Replace(search, replace, StringComparison.OrdinalIgnoreCase);
            try
            {
                if (subdirectory != newDirectory)
                    Directory.Move(subdirectory, newDirectory);
            }
            catch
            {
                Console.WriteLine($"ERROR - Failed to rename folder: {subdirectory}.");
            }
        }

        private static void ReplaceFileName(string search, string replace, string filename, string filepath, int fileindex)
        {
            Console.WriteLine($"Replacing {search} with {replace} in file name: {filepath}");

            var startIndex = filename.IndexOf(search, StringComparison.OrdinalIgnoreCase);
            var endIndex = startIndex + search.Length;
            var newName = filename.Substring(0, startIndex);
            newName += replace;
            newName += filename.Substring(endIndex);

            var fileAddress = filepath.Substring(0, fileindex);
            fileAddress += newName;

            try
            {
                File.Move(filepath, fileAddress);
            }
            catch
            {
                Console.WriteLine($"ERROR - Failed to rename file: {filepath}.");
            }
        }

        private static void ReplaceFileText(string search, string replace, string filepath, ICollection<string> ignoreExts)
        {
            var text = File.ReadAllText(filepath);
            var ext = filepath.Split(".").Last();
            if (ignoreExts.Contains(ext) || !text.Contains(search)) return;

            Console.WriteLine($"Replacing {search} with {replace} in file: {filepath}");

            text = text.Replace(search, replace);
            try
            {
                File.WriteAllText(filepath, text);
            }
            catch
            {
                Console.WriteLine($"ERROR - Failed to replace text in file: {filepath}.");
            }
        }
    }
}
