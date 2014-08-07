using System;
using System.Collections.Generic;

public class Args_parser
{
	public Dictionary<string, string> args = new Dictionary<string, string> ();

	public Args_parser (string[] arguments)
	{
		if (arguments.Length == 0) {
			Console.WriteLine ("You must specify at least one option. Type --help to view available options");
		}/* else if (arguments [0].IndexOf ("--") != 0) {
			Console.WriteLine ("The first parameter needs to be an option");
		}*/

		string current_option = null;
		for (int i = 0; i < arguments.Length; i++) {
			if (arguments [i].IndexOf ("--") == 0) {
				// option
				current_option = arguments [i].Substring(2);
				args [current_option] = "";
			} else if (current_option != null) {
				// value
				args [current_option] += arguments [i];
			}
		}
	}

	public bool getBool(string option){
		if (args.ContainsKey (option)) {
			return args [option] == "1" || args [option] == "true";
		}
		return false;
	}
}

