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

			Console.Read ();
		}
	}
}
