using System;
using System.IO;
using System.Collections.Generic;

namespace AdobeHDS
{
	class MainClass
	{
		public static void Main (string[] args)
		{

			Console.WriteLine ("AdobeHDS v0.0.5 11-Aug-2014");
			Console.WriteLine ("(c) 2014 Andrés Leone, K-S-V; License: GPL");
			Console.WriteLine ("");

			Dictionary<string, string>[] options = new Dictionary<string, string>[2] {
				new Dictionary<string, string> (),
				new Dictionary<string, string> ()
			};

			options [0] ["help"] = "displays this help";
			options [0] ["debug"] = "show debug output";
			options [0] ["delete"] = "delete fragments after processing";
			options [0] ["fproxy"] = "force proxy for downloading of fragments";
			options [0] ["play"] = "dump stream to stdout for piping to media player";
			//options [0] ["rename"] = "rename fragments sequentially before processing";
			//options [0] ["update"] = "update the script to current git version";
			
			options [1] ["auth"] = "authentication string for fragment requests";
			//options [1] ["duration"] = "stop recording after specified number of seconds";
			//options [1] ["filesize"] = "split output file in chunks of specified size (MB)";
			//options [1] ["fragments"] = "base filename for fragments";
			options [1] ["fixwindow"] = "timestamp gap between frames to consider as timeshift";
			options [1] ["manifest"] = "manifest file for downloading of fragments";
			//options [1] ["maxspeed"] = "maximum bandwidth consumption (KB) for fragment downloading";
			options [1] ["outdir"] = "destination folder for output file";
			options [1] ["outfile"] = "filename to use for output file";
			//options [1] ["parallel"] = "number of fragments to download simultaneously";
			options [1] ["proxy"] = "proxy for downloading of manifest";
			options [1] ["quality"] = "selected quality level (low|medium|high) or exact bitrate";
			options [1] ["referrer"] = "Referer to use for emulation of browser requests";
			//options [1] ["start"] = "start from specified fragment";
			options [1] ["useragent"] = "User-Agent to use for emulation of browser requests";


			Args_parser args_parser = new Args_parser (args);

			if (args_parser.args.Count == 0) {
				return;
			}

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

			F4F f4f = new F4F ();

			string manifest = null;
			string metadata = null;

			if (args_parser.args.ContainsKey ("manifest")) {
				manifest = args_parser.args ["manifest"];
			}

			if (args_parser.args.ContainsKey ("debug")) {
				f4f.debug = true;
			}

			if (args_parser.args.ContainsKey ("delete")) {
				f4f.delete_fragments_at_end = true;
			}

			if (args_parser.args.ContainsKey ("proxy")) {
				f4f.proxy = args_parser.args ["proxy"];
				if (args_parser.args.ContainsKey ("fproxy")) {
					f4f.fragments_proxy = true;
				}
			}

			if (args_parser.args.ContainsKey ("fixWindow")) {
				f4f.fixWindow = int.Parse(args_parser.args ["fixWindow"]);
			}

			if (args_parser.args.ContainsKey ("outdir")) {
				f4f.outDir = args_parser.args ["outdir"];
				if (f4f.outDir [f4f.outDir.Length - 1] == '\\' || f4f.outDir [f4f.outDir.Length - 1] == '/') {
					// Remove last \ or / from the path
					f4f.outDir = f4f.outDir.Substring(0, f4f.outDir.Length - 1);
				}
			}

			if (args_parser.args.ContainsKey ("outfile")) {
				f4f.outFileGlobal = args_parser.args ["outfile"];
			}

			if (args_parser.args.ContainsKey ("quality")) {
				f4f.quality = args_parser.args ["quality"];
			}

			if (args_parser.args.ContainsKey ("auth")) {
				f4f.auth = args_parser.args ["auth"];
			}

			if (args_parser.args.ContainsKey ("useragent")) {
				f4f.userAgent = args_parser.args ["useragent"];
			}

			if (args_parser.args.ContainsKey ("referrer")) {
				f4f.referrer = args_parser.args ["referrer"];
			}

			if (args_parser.args.ContainsKey ("play")) {
				f4f.play = true;
			}


















			f4f.DownloadFragments (manifest);
		}
	}
}
