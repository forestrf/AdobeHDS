using System;
using System.Collections.Generic;

namespace AdobeHDS
{
	public class Functions
	{
		function ReadByte($str, $pos)
		{
			$int = unpack('C', $str[$pos]);
			return $int[1];
		}

		function ReadInt24($str, $pos)
		{
			$int32 = unpack('N', "\x00" . substr($str, $pos, 3));
			return $int32[1];
		}

		function ReadInt32($str, $pos)
		{
			$int32 = unpack('N', substr($str, $pos, 4));
			return $int32[1];
		}

		function ReadInt64($str, $pos)
		{
			$hi    = sprintf("%u", ReadInt32($str, $pos));
			$lo    = sprintf("%u", ReadInt32($str, $pos + 4));
			$int64 = bcadd(bcmul($hi, "4294967296"), $lo);
			return $int64;
		}

		function ReadString($str, &$pos)
		{
			$len = 0;
			while ($str[$pos + $len] != "\x00")
				$len++;
			$str = substr($str, $pos, $len);
			$pos += $len + 1;
			return $str;
		}

		function ReadBoxHeader($str, &$pos, &$boxType, &$boxSize)
		{
			if (!isset($pos))
				$pos = 0;
			$boxSize = ReadInt32($str, $pos);
			$boxType = substr($str, $pos + 4, 4);
			if ($boxSize == 1)
			{
				$boxSize = ReadInt64($str, $pos + 8) - 16;
				$pos += 16;
			}
			else
			{
				$boxSize -= 8;
				$pos += 8;
			}
			if ($boxSize <= 0)
				$boxSize = 0;
		}

		function WriteByte(&$str, $pos, $int)
		{
			$str[$pos] = pack('C', $int);
		}

		function WriteInt24(&$str, $pos, $int)
		{
			$str[$pos]     = pack('C', ($int & 0xFF0000) >> 16);
			$str[$pos + 1] = pack('C', ($int & 0xFF00) >> 8);
			$str[$pos + 2] = pack('C', $int & 0xFF);
		}

		function WriteInt32(&$str, $pos, $int)
		{
			$str[$pos]     = pack('C', ($int & 0xFF000000) >> 24);
			$str[$pos + 1] = pack('C', ($int & 0xFF0000) >> 16);
			$str[$pos + 2] = pack('C', ($int & 0xFF00) >> 8);
			$str[$pos + 3] = pack('C', $int & 0xFF);
		}

		function WriteBoxSize(&$str, $pos, $type, $size)
		{
			if (substr($str, $pos - 4, 4) == $type)
				WriteInt32($str, $pos - 8, $size);
			else
			{
				WriteInt32($str, $pos - 8, 0);
				WriteInt32($str, $pos - 4, $size);
			}
		}

		function WriteFlvTimestamp(&$frag, $fragPos, $packetTS)
		{
			WriteInt24($frag, $fragPos + 4, ($packetTS & 0x00FFFFFF));
			WriteByte($frag, $fragPos + 7, ($packetTS & 0xFF000000) >> 24);
		}

		function AbsoluteUrl($baseUrl, $url)
		{
			if (!isHttpUrl($url))
				$url = JoinUrl($baseUrl, $url);
			return NormalizePath($url);
		}

		function GetString($object)
		{
			return trim(strval($object));
		}

		function isHttpUrl($url)
		{
			return (strncasecmp($url, "http", 4) == 0) ? true : false;
		}

		function isRtmpUrl($url)
		{
			return (preg_match('/^rtm(p|pe|pt|pte|ps|pts|fp):\/\//i', $url)) ? true : false;
		}

		function JoinUrl($firstUrl, $secondUrl)
		{
			if ($firstUrl and $secondUrl)
			{
				if (substr($firstUrl, -1) == '/')
					$firstUrl = substr($firstUrl, 0, -1);
				if (substr($secondUrl, 0, 1) == '/')
					$secondUrl = substr($secondUrl, 1);
				return $firstUrl . '/' . $secondUrl;
			}
			else if ($firstUrl)
				return $firstUrl;
			else
				return $secondUrl;
		}

		function KeyName(array $a, $pos)
		{
			$temp = array_slice($a, $pos, 1, true);
			return key($temp);
		}

		function LogDebug($msg, $display = true)
		{
			global $debug, $showHeader;
			if ($showHeader)
			{
				ShowHeader();
				$showHeader = false;
			}
			if ($display and $debug)
				fwrite(STDERR, $msg . "\n");
		}

		public void LogError(string msg)
		{
			LogError(msg, 1);
		}
		public void LogError(string msg, int code)
		{
			LogInfo(msg);
			exit(code);
		}

		public void LogInfo(string msg)
		{
			LogInfo(msg, false);
		}
		public void LogInfo(string msg, bool progress = false)
		{
			global quiet, showHeader;
			if (showHeader)
			{
				ShowHeader();
				showHeader = false;
			}
			if (!quiet)
				PrintLine(msg, progress);
		}

		function NormalizePath($path)
		{
			$inSegs  = preg_split('/(?<!\/)\/(?!\/)/u', $path);
			$outSegs = array();

			foreach ($inSegs as $seg)
			{
				if ($seg == '' or $seg == '.')
					continue;
				if ($seg == '..')
					array_pop($outSegs);
				else
					array_push($outSegs, $seg);
			}
			$outPath = implode('/', $outSegs);

			if (substr($path, 0, 1) == '/')
				$outPath = '/' . $outPath;
			if (substr($path, -1) == '/')
				$outPath .= '/';
			return $outPath;
		}

		function PrintLine($msg, $progress = false)
		{
			if ($msg)
			{
				printf("\r%-79s\r", "");
				if ($progress)
					printf("%s\r", $msg);
				else
					printf("%s\n", $msg);
			}
			else
				printf("\n");
		}

		function RemoveExtension($outFile)
		{
			preg_match("/\.\w{1,4}$/i", $outFile, $extension);
			if (isset($extension[0]))
			{
				$extension = $extension[0];
				$outFile   = substr($outFile, 0, -strlen($extension));
				return $outFile;
			}
			return $outFile;
		}

		function RenameFragments($baseFilename, $fragNum, $fileExt)
		{
			$files   = array();
			$retries = 0;

			while (true)
			{
				if ($retries >= 50)
					break;
				$file = $baseFilename . ++$fragNum;
				if (file_exists($file))
				{
					$files[] = $file;
					$retries = 0;
				}
				else if (file_exists($file . $fileExt))
				{
					$files[] = $file . $fileExt;
					$retries = 0;
				}
				else
					$retries++;
			}

			$fragCount = count($files);
			natsort($files);
			for ($i = 0; $i < $fragCount; $i++)
				rename($files[$i], $baseFilename . ($i + 1));
		}

		function ShowHeader()
		{
			$header = "KSV Adobe HDS Downloader";
			$len    = strlen($header);
			$width  = floor((80 - $len) / 2) + $len;
			$format = "\n%" . $width . "s\n\n";
			printf($format, $header);
		}

		function WriteFlvFile($outFile, $audio = true, $video = true)
		{
			$flvHeader    = pack("H*", "464c5601050000000900000000");
			$flvHeaderLen = strlen($flvHeader);

			// Set proper Audio/Video marker
			WriteByte($flvHeader, 4, $audio << 2 | $video);

			if (is_resource($outFile))
				$flv = $outFile;
			else
				$flv = fopen($outFile, "w+b");
			if (!$flv)
				LogError("Failed to open " . $outFile);
			fwrite($flv, $flvHeader, $flvHeaderLen);
			return $flv;
		}

		public static object WriteMetadata(F4F f4f, string flv)
		{
			if (f4f.media.length > 0 && f4f.media["metadata"])
			{
				int metadataSize = f4f.media["metadata"].length;
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



		public static bool in_array_field(object needle, string needle_field, Dictionary<string, Dictionary<string, object>> haystack)
		{
			foreach (KeyValuePair<string, Dictionary<string, object>> item in haystack)
				if (item.Value.ContainsKey(needle_field) && item.Value[needle_field] == needle)
					return true;

			return false;
		}



		public static object value_in_array_field(object needle, string needle_field, string value_field, Dictionary<string, Dictionary<string, object>> haystack)
		{
			foreach (KeyValuePair<string, Dictionary<string, object>> item in haystack)
				if (item.Value.ContainsKey(needle_field) && item.Value[needle_field] == needle)
					return item.Value[value_field];

			return false;
		}
	}
}

