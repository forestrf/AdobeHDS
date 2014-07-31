using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

public class Functions : Defines
{
	public int ReadInt24 (byte[] bytes, int pos)
	{
		int res = (bytes [pos + 0] << 16) + (bytes [pos + 1] << 8) + (bytes [pos + 2]);
		return BitConverter.IsLittleEndian ? SwapInt24 (res) : res;
	}

	public string ReadString (byte[] bytes, int start, int length)
	{
		return Encoding.UTF8.GetString (bytes, start, length);
	}

	public string ReadString (byte[] bytes, ref int start)
	{
		string to_return = "";
		while (bytes [start] != '\x00') {
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

	public int SwapInt24 (int v)
	{
		return ((v >> 8) & 0xff) | ((v << 16) & 0xff0000);
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
		
		if (boxSize <= 0)
			boxSize = 0;
	}

	public void WriteBoxSize(byte[] str, int pos, string type, long size)
	{
		if (ReadString (str, pos - 4, 4) == type) {
			WriteToByteArray (str, pos - 8, BitConverter.GetBytes (size));
		}
		else
		{
			WriteToByteArray (str, pos - 8, BitConverter.GetBytes (0));
			WriteToByteArray (str, pos - 4, BitConverter.GetBytes (size));
		}
	}

	public void WriteToByteArray(byte[] str, int pos, byte[] replace){
		if (BitConverter.IsLittleEndian)
			Array.Reverse(replace);
		for (int i = 0; i < replace.Length; i++) {
			str [i + pos] = replace [i];
		}
	}
		
	public void WriteFlvTimestamp(byte[] frag, int fragPos, int packetTS)
	{
		WriteToByteArray (frag, fragPos + 4, BitConverter.GetBytes (packetTS & 0x00FFFFFF));
		frag[fragPos + 7] = (byte)((packetTS & 0xFF000000) >> 24);
	}

	public string AbsoluteUrl(string baseUrl, string url)
	{
		if (!isHttpUrl(url))
			url = JoinUrl(baseUrl, url);
		return NormalizePath(url);
	}
	/*
	public string GetString(object o)
	{
		return o.ToString().Trim();
	}
*/
	public bool isHttpUrl(string url)
	{
		return url.IndexOf("http") == 0;
	}
	/*
	public bool isRtmpUrl(string url)
	{
		return Regex.IsMatch("^rtm(p|pe|pt|pte|ps|pts|fp)://", url, RegexOptions.IgnoreCase);
	}
*/
	public string JoinUrl(string firstUrl, string secondUrl)
	{
		if (firstUrl != null && secondUrl != null)
		{
			if (firstUrl[firstUrl.Length-1] == '/')
				firstUrl = firstUrl.Substring(0, -1);
			if (secondUrl[0] == '/')
				secondUrl = secondUrl.Substring(1);
			return firstUrl + "/" + secondUrl;
		}
		else if (firstUrl != null)
			return firstUrl;
		else
			return secondUrl;
	}
	/*
	public object KeyName(List a, int pos)
	{
		temp = array_slice(a, pos, 1, true);
		return key(temp);
	}
*/
	public void LogDebug (string msg)
	{
		Debug.WriteLine ("DEBUG: " + msg);
	}

	public void LogError (string msg)
	{
		LogInfo (msg);
	}

	public void LogInfo (string msg)
	{
		Console.WriteLine (msg);
	}

	public string NormalizePath(string path)
	{
		string pattern = "(?<!\\/)\\/(?!\\/)";
		string[] inSegs = Regex.Split(path, pattern);

		List<string> outSegs = new List<string>();

		foreach (string seg in inSegs)
		{
			if (seg == "" || seg == ".")
				continue;
			if (seg == "..")
				outSegs.RemoveAt (outSegs.Count - 1);
			else
				outSegs.Add (seg);
		}
		string outPath = string.Join ("/", outSegs);

		if (path[0] == '/')
			outPath = "/" + outPath;
		if (path[path.Length -1] == '/')
			outPath += "/";
		return outPath;
	}
	/*
	public void PrintLine(string msg)
	{
		PrintLine(msg, false)
	}
	public void PrintLine(string msg, bool progress)
	{
		if (msg)
		{
			Console.WriteLine("\r%-79s\r", "");
			if (progress)
				Console.WriteLine("%s\r", msg);
			else
				Console.WriteLine("%s\n", msg);
		}
		else
			Console.WriteLine("\n");
	}
*/
	public string CalculateMD5Hash(string input)
	{
		// step 1, calculate MD5 hash from input
		MD5 md5 = System.Security.Cryptography.MD5.Create();
		byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
		byte[] hash = md5.ComputeHash(inputBytes);

		// step 2, convert byte array to hex string
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < hash.Length; i++)
		{
			sb.Append(hash[i].ToString("X2"));
		}
		return sb.ToString();
	}

	public string RemoveExtension(string outFile)
	{
		string pattern = "\\.\\w{1,4}$";
		string[] extension = Regex.Split(outFile, pattern, RegexOptions.IgnoreCase);

		if (extension.Length >= 1)
		{
			outFile   = outFile.Substring(0, -extension[0].Length);
			return outFile;
		}
		return outFile;
	}
	/*
	public void RenameFragments(string baseFilename, int fragNum, string fileExt)
	{
		List<string>files = new List<string>();
		int retries = 0;

		while (true)
		{
			if (retries >= 50)
				break;
			string file = baseFilename + ++fragNum;
			if (File.Exists(file))
			{
				files.Add(file);
				retries = 0;
			}
			else if (File.Exists(file + fileExt))
			{
				files.Add(file + fileExt);
				retries = 0;
			}
			else
				retries++;
		}

		int fragCount = files.Count;
		natsort(files);
		for (int i = 0; i < fragCount; i++)
			rename(files[i], baseFilename + (i + 1));
	}

	public void ShowHeader()
	{
		string header = "KSV Adobe HDS Downloader";
		int len       = header.Length;
		int width     = Math.Floor((80 - len) / 2) + len;
		string format = "\n%" + width + "s\n\n";
		Console.WriteLine(format, header);
	}

	public object WriteFlvFile(string outFile)
	{
		return WriteFlvFile(outFile, true)
	}
	public object WriteFlvFile(string outFile, bool audio)
	{
		return WriteFlvFile(outFile, audio, true)
	}
	public object WriteFlvFile(string outFile, bool audio, bool video)
	{
		string flvHeader = pack("H*", "464c5601050000000900000000");
		int flvHeaderLen = flvHeader.Length;

		// Set proper Audio/Video marker
		WriteByte(flvHeader, 4, audio << 2 | video);

		if (is_resource(outFile))
			flv = outFile;
		else
			flv = fopen(outFile, "w+b");
		if (!flv)
			LogError("Failed to open " + outFile);
		fwrite(flv, flvHeader, flvHeaderLen);
		return flv;
	}

	public object WriteMetadata(F4F f4f, string flv)
	{
		if (f4f.media.Length > 0 && f4f.media["metadata"])
		{
			int metadataSize = f4f.media["metadata"].Length;
			WriteByte(metadata, 0, Defines.SCRIPT_DATA);
			WriteInt24(metadata, 1, metadataSize);
			WriteInt24(metadata, 4, 0);
			WriteInt32(metadata, 7, 0);
			string metadata = implode("", metadata) + f4f.media["metadata"];
			WriteByte(metadata, f4f.tagHeaderLen + metadataSize - 1, 0x09);
			WriteInt32(metadata, f4f.tagHeaderLen + metadataSize, f4f.tagHeaderLen + metadataSize);
			if (is_resource(flv))
			{
				fwrite(flv, metadata, f4f.tagHeaderLen + metadataSize + f4f.prevTagSize);
				return true;
			}
			else
				return metadata;
		}
		return false;
	}


*/
	public bool in_array_field(int needle, Dictionary<int, Frag_table_content> haystack)
	{
		foreach (KeyValuePair<int, Frag_table_content> item in haystack)
			if (item.Value.firstFragment == needle)
				return true;

		return false;
	}

	public bool value_in_array_field(int needle, Dictionary<int, Frag_table_content> haystack)
	{
		foreach (KeyValuePair<int, Frag_table_content> item in haystack)
			if (item.Value.discontinuityIndicator == needle)
				return true;
		return false;
	}

	public byte[] file_get_contents (string path){
		byte[] b = new byte[0];
		using (FileStream fs = File.OpenRead(path)) 
		{
			b = new byte[fs.Length];
			fs.Read (b, 0, b.Length);
		}
		return b;
	}
}