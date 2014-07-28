using System;

namespace AdobeHDS
{
	class MainClass
	{
		public static void Main (string[] args)
		{

			F4F f4f = new F4F ();
			f4f.ParseManifest ("https://dl.dropboxusercontent.com/u/1630604/borrame.xml");


			Console.Read ();
		}
	}
}
