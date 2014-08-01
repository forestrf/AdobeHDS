using System;
using System.IO;

namespace AdobeHDS
{
	class MainClass
	{
		public static void Main (string[] args)
		{

			F4F f4f = new F4F ();
			//f4f.DownloadFragments ("https://dl.dropboxusercontent.com/u/1630604/borrame.xml");
			f4f.DownloadFragments ("http://deswowa3player-tk.antena3.com/vsgsm/_definst_/assets5/2014/07/25/59508586-1ACD-446B-9A90-2128427BC6A8/es.smil/manifest.f4m?nvb=20140801002325&nva=20140801022325&token=0899bdbbf3c35b019ea50");

			string outFile = "jorl";


			f4f.LogDebug("Joining Fragments:");
			for (int i = f4f.fragTable [1].firstFragment; i < f4f.fragTable [1].firstFragment + f4f.fragCount; i++)
			{
				Frag_response frag = f4f.frags [i];
				if (File.Exists (frag.filename)) {
					f4f.LogDebug ("Fragment fragNum is already downloaded");
					frag.response = f4f.file_get_contents (frag.filename);
				}

				if (f4f.file == null)
				{
					f4f.DecodeFragment(frag.response, i);
					f4f.file = f4f.WriteFlvFile(f4f.JoinUrl(f4f.outDir, outFile + ".flv"), f4f.audio, f4f.video);
					if (f4f.media.metadata.Length != 0)
						f4f.WriteMetadata(f4f, f4f.file);
				}
				f4f.DecodeFragment(frag.response, i);
				f4f.LogInfo("Processed " + i + " fragments");
			}

			if (f4f.file != null)
				f4f.file.Close ();
			f4f.LogInfo("Joined fragments");






			Console.Read ();
		}
	}
}
