using System;
using System.IO;
using System.Collections.Generic;

namespace AdobeHDS
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Dictionary<string, string>[] options = new Dictionary<string, string>[2] {
				new Dictionary<string, string> (),
				new Dictionary<string, string> ()
			};

			options [0] ["help"] = "displays this help";
			options [0] ["debug"] = "show debug output";
			options [0] ["delete"] = "delete fragments after processing";
			options [0] ["fproxy"] = "force proxy for downloading of fragments";
			//options [0] ["play"] = "dump stream to stdout for piping to media player";
			//options [0] ["rename"] = "rename fragments sequentially before processing";
			//options [0] ["update"] = "update the script to current git version";
			
			options [1] ["auth"] = "authentication string for fragment requests";
			//options [1] ["duration"] = "stop recording after specified number of seconds";
			//options [1] ["filesize"] = "split output file in chunks of specified size (MB)";
			options [1] ["fragments"] = "base filename for fragments";
			options [1] ["fixwindow"] = "timestamp gap between frames to consider as timeshift";
			options [1] ["manifest"] = "manifest file for downloading of fragments";
			//options [1] ["maxspeed"] = "maximum bandwidth consumption (KB) for fragment downloading";
			options [1] ["outdir"] = "destination folder for output file";
			options [1] ["outfile"] = "filename to use for output file";
			options [1] ["parallel"] = "number of fragments to download simultaneously";
			options [1] ["proxy"] = "proxy for downloading of manifest";
			options [1] ["quality"] = "selected quality level (low|medium|high) or exact bitrate";
			//options [1] ["referrer"] = "Referer to use for emulation of browser requests";
			//options [1] ["start"] = "start from specified fragment";
			//options [1] ["useragent"] = "User-Agent to use for emulation of browser requests";


			Args_parser args_parser = new Args_parser (args);

			if (args_parser.args.ContainsKey ("help")) {
				// Show help
				foreach (KeyValuePair<string, string> option in options[0]) {
					Console.WriteLine ("--{0, -9}         {1}", option.Key, option.Value);
				}
				foreach (KeyValuePair<string, string> option in options[1]) {
					Console.WriteLine ("--{0, -9} [param] {1}", option.Key, option.Value);
				}
				return;
			}


			string manifest = null;
			string baseFilename = null;
			bool debug = false;
			int fixWindow = 0;
			string metadata = null;
			string outDir = null;
			string outFile = null;

			if (args_parser.args.ContainsKey ("manifest")) {
				manifest = args_parser.args ["manifest"];
			}

			if (args_parser.args.ContainsKey ("debug")) {
				switch (args_parser.args ["debug"]) {
				case "1":
					debug = true;
					break;
				case "true":
					debug = true;
					break;
				default:
					debug = false;
					break;
				}
			}



















			F4F f4f = new F4F ();
			f4f.DownloadFragments (manifest);
		}
	}
}
