using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

public class Functions : Defines
{
	public bool debug = false;


	public int ReadInt24 (byte[] bytes, int pos)
	{
		int res = BitConverter.ToInt32 (new byte[4]{ 0x00, bytes [pos], bytes [pos + 1], bytes [pos + 2] }, 0);
		return BitConverter.IsLittleEndian ? SwapInt32 (res) : res;
	}

	public string ReadString (byte[] bytes, int start, int length)
	{
		return Encoding.UTF8.GetString (bytes, start, length);
	}

	public string ReadString (byte[] bytes, ref int start)
	{
		string to_return = "";
		while (bytes [start] != 0x00) {
			to_return = Encoding.UTF8.GetString (bytes, start++, 1);
		}
		start++;
		return to_return;
	}

	public int ReadInt32 (byte[] bytes, int pos)
	{
		int res = BitConverter.ToInt32 (bytes, pos);
		return BitConverter.IsLittleEndian ? SwapInt32 (res) : res;
	}

	public long ReadInt64 (byte[] bytes, int pos)
	{
		long res = BitConverter.ToInt64 (bytes, pos);
		return BitConverter.IsLittleEndian ? SwapInt64 (res) : res;
	}

	public short SwapInt16 (short v)
	{
		return (short)(((v & 0xff) << 8) | ((v >> 8) & 0xff));
	}

	public int SwapInt32 (int v)
	{
		return (((SwapInt16 ((short)v) & 0xffff) << 0x10) |
		(SwapInt16 ((short)(v >> 0x10)) & 0xffff));
	}

	public long SwapInt64 (long v)
	{
		return (long)(((SwapInt32 ((int)v) & 0xffffffffL) << 0x20) |
		(SwapInt32 ((int)(v >> 0x20)) & 0xffffffffL));
	}

	public void ReadBoxHeader (byte[] bootstrap, ref int pos, ref string boxType, ref long boxSize)
	{

		boxSize = ReadInt32 (bootstrap, pos);
	
		boxType = ReadString (bootstrap, pos + 4, 4);
		if (boxSize == 1) {
			boxSize = ReadInt64 (bootstrap, pos + 8) - 16;
			pos += 16;
		} else {
			boxSize -= 8;
			pos += 8;
		}
		
		if (boxSize <= 0) {
			boxSize = 0;
		}
	}
	
	public void WriteBoxSize (byte[] str, int pos, string type, long size)
	{
		if (ReadString (str, pos - 4, 4) == type) {
			WriteToByteArray (str, pos - 8, BitConverter.GetBytes (size));
		} else {
			WriteToByteArray (str, pos - 8, BitConverter.GetBytes (0));
			WriteToByteArray (str, pos - 4, BitConverter.GetBytes (size));
		}
	}

	public void WriteToByteArray (byte[] str, int pos, byte[] replace)
	{
		if (BitConverter.IsLittleEndian)
			Array.Reverse (replace);
		for (int i = 0; i < replace.Length; i++) {
			str [i + pos] = replace [i];
		}
	}

	public void WriteFlvTimestamp (byte[] frag, int fragPos, int packetTS)
	{
		WriteToByteArray (frag, fragPos + 4, BitConverter.GetBytes (packetTS & 0x00FFFFFF));
		frag [fragPos + 7] = (byte)((packetTS & 0xFF000000) >> 24);
	}

	public string AbsoluteUrl (string baseUrl, string url)
	{
		if (!isHttpUrl (url)) {
			url = JoinUrl (baseUrl, url);
		}
		return NormalizePath (url);
	}

	public bool isHttpUrl (string url)
	{
		return url.IndexOf ("http") == 0;
	}

	public bool isRtmpUrl (string url)
	{
		return url.IndexOf ("rtmp") == 0 || url.IndexOf ("rtmfp") == 0;
	}

	public string JoinUrl (string firstUrl, string secondUrl)
	{
		if (firstUrl != "" && secondUrl != "") {
			if (firstUrl [firstUrl.Length - 1] == '/') {
				firstUrl = firstUrl.Substring (0, -1);
			}
			if (secondUrl [0] == '/') {
				secondUrl = secondUrl.Substring (1);
			}
			return firstUrl + "/" + secondUrl;
		} else if (firstUrl != "") {
			return firstUrl;
		} else {
			return secondUrl;
		}
	}

	public void LogDebug (string msg)
	{
		if (debug) {
			LogInfo ("DEBUG: " + msg);
		} else {
			Debug.WriteLine ("DEBUG: " + msg);
		}
	}

	public void LogError (string msg)
	{
		LogInfo (msg);
	}

	public void LogInfo (string msg)
	{
		Console.WriteLine (msg);
	}

	public string NormalizePath (string path)
	{
		string pattern = "(?<!\\/)\\/(?!\\/)";
		string[] inSegs = Regex.Split (path, pattern);

		List<string> outSegs = new List<string> ();

		foreach (string seg in inSegs) {
			if (seg == "" || seg == ".") {
				continue;
			}
			if (seg == "..") {
				outSegs.RemoveAt (outSegs.Count - 1);
			} else {
				outSegs.Add (seg);
			}
		}
		string outPath = string.Join ("/", outSegs);

		if (path [0] == '/') {
			outPath = "/" + outPath;
		}
		if (path [path.Length - 1] == '/') {
			outPath += "/";
		}
		return outPath;
	}

	public string CalculateMD5Hash (string input)
	{
		// step 1, calculate MD5 hash from input
		MD5 md5 = System.Security.Cryptography.MD5.Create ();
		byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes (input);
		byte[] hash = md5.ComputeHash (inputBytes);

		// step 2, convert byte array to hex string
		StringBuilder sb = new StringBuilder ();
		for (int i = 0; i < hash.Length; i++) {
			sb.Append (hash [i].ToString ("X2"));
		}
		return sb.ToString ();
	}

	public string RemoveExtension (string outFile)
	{
		string pattern = "\\.\\w{1,4}$";
		string[] extension = Regex.Split (outFile, pattern, RegexOptions.IgnoreCase);

		if (extension.Length >= 1) {
			outFile = outFile.Substring (0, outFile.Length - extension [0].Length);
		}
		return outFile;
	}

	public void WriteInt24 (byte[] bytes, int pos, int number)
	{
		byte[] n = BitConverter.GetBytes (number);
		WriteToByteArray (bytes, pos, new byte[]{ n [0], n [1], n [2] });
	}

	public bool in_array_field (int needle, Dictionary<int, Frag_table_content> haystack)
	{
		foreach (KeyValuePair<int, Frag_table_content> item in haystack) {
			if (item.Value.firstFragment == needle) {
				return true;
			}
		}

		return false;
	}

	public bool value_in_array_field (int needle, Dictionary<int, Frag_table_content> haystack)
	{
		foreach (KeyValuePair<int, Frag_table_content> item in haystack) {
			if (item.Value.discontinuityIndicator == needle) {
				return true;
			}
		}
		return false;
	}

	public byte[] file_get_contents (string path)
	{
		byte[] b = new byte[0];
		using (FileStream fs = File.OpenRead (path)) {
			b = new byte[fs.Length];
			fs.Read (b, 0, b.Length);
		}
		return b;
	}
}