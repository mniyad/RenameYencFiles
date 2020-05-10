using CommandLine;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace FixArchiveFiles
{
	class Program
	{
		private static readonly Regex fileRegex = new Regex("(\")?([A-Z,a-z,0-9,\\.]+)(\")?(\\syEnc\\s\\(\\d{3}\\/\\d{3}\\))?", RegexOptions.Compiled);

		static void Main(string[] args)
		{
			var options = Parser.Default.ParseArguments<Options>(args)
						.MapResult(options => RunAndReturnExitCode(options),	_ => 1);
		}

		private static object RunAndReturnExitCode(Options options)
		{
			if (!string.IsNullOrWhiteSpace(options.TestData))
				CreateSampelFiles(options.NzbFilename, options.TestData);

            var fileFormat = options.FilenameFormat ??  $"{Path.GetFileNameWithoutExtension(options.NzbFilename)}.part{{0}}";
            var files = GetFilenames(options.NzbFilename);
            var destinationFiles = Directory.GetFiles(options.Source);

            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                var mainPart = fileRegex.Match(file).Groups[2].Value;
                var sourceFile = destinationFiles.Single(f => f.Contains(mainPart));
                string destFileName = Path.Combine(options.Source, string.Format(fileFormat, i)); //new destinationfile
                
                File.Move(sourceFile, destFileName); //rename file
            }
            return 1;
		}

        public static string[] GetFilenames(string nzbFile)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(nzbFile);
            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("nzb", "http://www.newzbin.com/DTD/2003/nzb");
            return doc
                .SelectNodes("//nzb:file/@subject", nsmgr)
                .Cast<XmlNode>()
                .Select(a => a.Value)
                .ToArray();
        }

        public static void CreateSampelFiles(string nzbFilename, string destinationFolder)
        {
            var files = GetFilenames(nzbFilename);
            foreach (var f in Directory.GetFiles(destinationFolder))
            {
                File.Delete(f);
            }
            foreach (var file in files)
            {
                var match = fileRegex.Match(file);
                if (match.Success)
                {
                    var mainPart = match.Groups[2].Value;
                    var first = (mainPart.First() % 2) == 0;
                    var filename = first ? "`" : string.Empty;
                    filename += mainPart;
                    filename += first ? "` yEnc (001+261)" : string.Empty;

                    File.CreateText(Path.Combine(destinationFolder, filename));
                }
            }
        }
        public class Options
        {
            [Option('n', "nzb", Required = true)]
            public string NzbFilename { get; set; }
            [Option('s', "source", Required = true)]
            public string Source { get; set; }

            [Option('f', "format")]
            public string FilenameFormat { get; set; }
            [Option('d', HelpText = "Dry run")]
            public bool DryRun { get; set; }
            [Option('t', "test", HelpText = "Generate test data in folder")]
            public string TestData { get; set; }
        }
    }
}
