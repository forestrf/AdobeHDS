using System;
using System.Collections.Generic;
using System.Xml;
using System.Net;


public class F4F : Functions
{
	object audio, auth, baseFilename, baseTS, bootstrapUrl, baseUrl, debug, duration, fileCount, filesize, fixWindow;
	object format, media, metadata, outDir, outFile, parallel, play, processed, quality, rename, video;
	object prevTagSize, tagHeaderLen;
	object segTable, fragTable, segNum, fragNum, frags, fragCount, lastFrag, fragUrl, discontinuity;
	object prevAudioTS, prevVideoTS, pAudioTagLen, pVideoTagLen, pAudioTagPos, pVideoTagPos;
	object prevAVC_Header, prevAAC_Header, AVC_HeaderWritten, AAC_HeaderWritten;
	object segStart, fragStart, negTS;

	public F4F ()
	{
		this.auth = "";
		this.baseFilename = "";
		this.bootstrapUrl = "";
		this.debug = false;
		this.duration = 0;
		this.fileCount = 1;
		this.fixWindow = 1000;
		this.format = "";
		this.metadata = true;
		this.outDir = "";
		this.outFile = "";
		this.parallel = 8;
		this.play = false;
		this.processed = false;
		this.quality = "high";
		this.rename = false;
		this.segTable = new Dictionary<string, object> ();
		this.fragTable = new Dictionary<string, object> ();
		this.segStart = false;
		this.fragStart = false;
		this.frags = new Dictionary<string, object> ();
		this.fragCount = 0;
		this.lastFrag = 0;
		this.discontinuity = "";
		this.InitDecoder ();
	}

	public void InitDecoder ()
	{
		this.audio = false;
		this.filesize = 0;
		this.video = false;
		this.prevTagSize = 4;
		this.tagHeaderLen = 11;
		this.baseTS = INVALID_TIMESTAMP;
		this.negTS = INVALID_TIMESTAMP;
		this.prevAudioTS = INVALID_TIMESTAMP;
		this.prevVideoTS = INVALID_TIMESTAMP;
		this.pAudioTagLen = 0;
		this.pVideoTagLen = 0;
		this.pAudioTagPos = 0;
		this.pVideoTagPos = 0;
		this.prevAVC_Header = false;
		this.prevAAC_Header = false;
		this.AVC_HeaderWritten = false;
		this.AAC_HeaderWritten = false;
	}

	public void ParseManifest (string manifest)
	{
		XmlDocument doc = new XmlDocument ();
		try {
			doc.Load (manifest);
		} catch (Exception e) {
			try {
				doc.LoadXml (manifest);
			} catch (Exception f) {
				LogError ("Unable to download the manifest");
				// END HERE
				return;
			}
		}

		Manifest_parsed manifest_parsed = new Manifest_parsed ();

		XmlNodeList nodes = doc.DocumentElement.ChildNodes;
		//manifest_parsed
		foreach (XmlNode node in nodes) {
			switch (node.Name) {
			case "bootstrapInfo":
				manifest_parsed.bootstrapInfo.Add (node.Attributes ["id"].InnerText, node.InnerText);
				break;
			case "media":
				Manifest_parsed_media manifest_parsed_media = new Manifest_parsed_media ();
				manifest_parsed_media.metadata = node.InnerText;
				foreach (XmlAttribute node_media in node.Attributes) {
					switch (node_media.Name) {
					case "bootstrapInfoId":
						manifest_parsed_media.bootstrapInfoId = node_media.InnerText;
						break;
					case "width":
						manifest_parsed_media.width = int.Parse(node_media.InnerText);
						break;
					case "height":
						manifest_parsed_media.height = int.Parse(node_media.InnerText);
						break;
					case "bitrate":
						manifest_parsed_media.bitrate = int.Parse(node_media.InnerText);
						break;
					case "url":
						manifest_parsed_media.url = node_media.InnerText;
						break;
					}
				}
				manifest_parsed.media.Add (manifest_parsed_media);
				break;
			}
		}

		manifest_parsed = manifest_parsed;




		/*
		// Extract baseUrl from manifest url
		string baseUrl;
		pre_baseUrl = xml.xpath("/ns:manifest/ns:baseURL");
		if (isset(baseUrl[0]))
			baseUrl = GetString(pre_baseUrl[0]);
		else
		{
			baseUrl = parentManifest;
			if (strpos(baseUrl, "?") != false)
				baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("?"));
			baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf("/"));
		}

		int count;

		urls = xml.xpath("/ns:manifest/ns:media[@*]");
		if (isset(urls[0]["href"]))
		{
			count = 1;
			foreach (Dictionary<string, object> childManifest in urls)
			{
				if (isset(childManifest["bitrate"]))
					bitrate = Math.Floor(GetString(childManifest["bitrate"]));
				else
					bitrate = count++;
				Dictionary entry = new Dictionary<string, object>();
				entry =& childManifests[bitrate];
				entry["bitrate"] = bitrate;
				entry["url"]     = AbsoluteUrl(baseUrl, GetString(childManifest["href"]));
				entry["xml"]     = this.GetManifest(cc, entry["url"] as string);
			}
			unset(entry, childManifest);
		}
		else
		{
			childManifests[0]["bitrate"] = 0;
			childManifests[0]["url"]     = parentManifest;
			childManifests[0]["xml"]     = xml;
		}

		count = 1;
		foreach (childManifests as childManifest)
		{
			xml = childManifest["xml"];

			// Extract baseUrl from manifest url
			baseUrl = xml.xpath("/ns:manifest/ns:baseURL");
			if (isset(baseUrl[0]))
				baseUrl = GetString(baseUrl[0]);
			else
			{
				baseUrl = childManifest["url"];
				if (baseUrl.IndexOf("?") != -1)
					baseUrl = baseUrl.Substring(0, baseUrl.IndexOf("?"));
				baseUrl = substr(baseUrl, 0, strrpos(baseUrl, "/"));
			}

			streams = xml.xpath("/ns:manifest/ns:media");
			foreach (streams as stream)
			{
				array = array();
				foreach (KeyValuePair<string, object> kv in stream.attributes())
					array[kv.Key.ToLower()] = GetString(kv.Value);
				array["metadata"] = GetString(stream->{'metadata'});
				stream            = array;

				if (isset(stream["bitrate"]))
					bitrate = floor($stream["bitrate"]);
				else if (childManifest["bitrate"] > 0)
					bitrate = childManifest["bitrate"];
				else
					bitrate = count++;
				while (isset(this.media[bitrate]))
					bitrate++;
				$streamId = isset($stream[strtolower('streamId')]) ? $stream[strtolower('streamId')] : "";
				$mediaEntry =& this.media[$bitrate];

				$mediaEntry["baseUrl"] = $baseUrl;
				$mediaEntry["url"]     = $stream["url"];
				if (isRtmpUrl($mediaEntry["baseUrl"]) or isRtmpUrl($mediaEntry["url"]))
					LogError("Provided manifest is not a valid HDS manifest");

				// Use embedded auth information when available
				int idx = mediaEntry["url"].IndexOf("?");
				if (idx != -1)
				{
					mediaEntry["queryString"] = substr($mediaEntry["url"], $idx);
					mediaEntry["url"]         = substr($mediaEntry["url"], 0, $idx);
					if (strlen(this.auth) != 0 and strcmp(this.auth, $mediaEntry["queryString"]) != 0)
						LogDebug("Manifest overrides 'auth': " + $mediaEntry["queryString"]);
				}
				else
					mediaEntry["queryString"] = this.auth;

				if (isset($stream[strtolower("bootstrapInfoId")]))
					$bootstrap = xml.xpath("/ns:manifest/ns:bootstrapInfo[@id='" + $stream[strtolower("bootstrapInfoId")] . "']");
				else
					$bootstrap = xml.xpath("/ns:manifest/ns:bootstrapInfo");
				if (isset($bootstrap[0]["url"]))
				{
					$mediaEntry["bootstrapUrl"] = AbsoluteUrl($mediaEntry["baseUrl"], GetString($bootstrap[0]["url"]));
					if (strpos($mediaEntry["bootstrapUrl"], '?') === false)
						$mediaEntry["bootstrapUrl"] .= this.auth;
				}
				else
					$mediaEntry["bootstrap"] = base64_decode(GetString($bootstrap[0]));
				if (isset($stream["metadata"]))
					$mediaEntry["metadata"] = base64_decode($stream["metadata"]);
				else
					$mediaEntry["metadata"] = "";
			}
			unset($mediaEntry, $childManifest);
		}

		// Available qualities
		$bitrates = array();
		if (!count(this.media))
			LogError("No media entry found");
		krsort(this.media, SORT_NUMERIC);
		LogDebug("Manifest Entries:\n");
		LogDebug(sprintf(" %-8s%s", "Bitrate", "URL"));
		for ($i = 0; $i < count(this.media); $i++)
		{
			$key        = KeyName(this.media, $i);
			$bitrates[] = $key;
			LogDebug(sprintf(" %-8d%s", $key, this.media[$key]["url"]));
		}
		LogDebug("");
		LogInfo("Quality Selection:\n Available: " + implode(' ', bitrates));

		// Quality selection
		if (is_numeric(this.quality) && isset(this.media[this.quality]))
		{
			key        = this.quality;
			this.media = this.media[key];
		}
		else
		{
			this.quality = strtolower(this.quality);
			switch (this.quality)
			{
			case "low":
				this.quality = 2;
				break;
			case "medium":
				this.quality = 1;
				break;
			default:
				this.quality = 0;
			}
			while (this.quality >= 0)
			{
			key = KeyName(this.media, this.quality);
			if (key !== NULL)
			{
				this.media = this.media[$key];
				break;
			}
			else
				this.quality -= 1;
			}
		}
		LogInfo(" Selected : " + key);

		// Parse initial bootstrap info
		this.baseUrl = this.media["baseUrl"];
		if (isset(this.media["bootstrapUrl"]))
		{
			this.bootstrapUrl = this.media["bootstrapUrl"];
			this.UpdateBootstrapInfo(cc, this.bootstrapUrl);
		}
		else
		{
			bootstrapInfo = this.media["bootstrap"];
			ReadBoxHeader(bootstrapInfo, pos, boxType, boxSize);
			if (boxType == "abst")
				this.ParseBootstrapBox(bootstrapInfo, pos);
			else
				LogError("Failed to parse bootstrap info");
		}
	}

	function UpdateBootstrapInfo(cURL cc, string bootstrapUrl)
	{
		int fragNum = this.fragCount;
		int retries = 0;

		// Backup original headers and add no-cache directive for fresh bootstrap info
		// Hacer copia, no referencia, en la siguiente instrucción.
		headers       = cc.headers;
		cc.headers[] = "Cache-Control: no-cache";
		cc.headers[] = "Pragma: no-cache";

		while (($fragNum == this.fragCount) and (retries < 30))
		{
			$bootstrapPos = 0;
			LogDebug("Updating bootstrap info, Available fragments: " + this.fragCount);
			int status = cc.get(bootstrapUrl);
			if (status != 200)
				LogError("Failed to refresh bootstrap info, Status: " + $status);
			$bootstrapInfo = $cc->response;
			string boxType;
			int boxSize;
			ReadBoxHeader($bootstrapInfo, $bootstrapPos, boxType, boxSize);
			if (boxType == "abst")
				this.ParseBootstrapBox($bootstrapInfo, $bootstrapPos);
			else
				LogError("Failed to parse bootstrap info");
			LogDebug("Update complete, Available fragments: " + this.fragCount);
			if (fragNum == this.fragCount)
			{
				LogInfo("Updating bootstrap info, Retries: " + ++retries, true);
				usleep(4000000);
			}
		}

		// Restore original headers
		cc.headers = headers;
	}

	public static void ParseBootstrapBox($bootstrapInfo, int pos)
	{
		$version          = ReadByte($bootstrapInfo, pos);
		$flags            = ReadInt24($bootstrapInfo, pos + 1);
		$bootstrapVersion = ReadInt32($bootstrapInfo, pos + 4);
		$byte             = ReadByte($bootstrapInfo, pos + 8);
		$profile          = ($byte & 0xC0) >> 6;
		if (($byte & 0x20) >> 5)
		{
			this.metadata = false;
		}
		bool update = ($byte & 0x10) >> 4;
		if (!update)
		{
			this.segTable  = array();
			this.fragTable = array();
		}
		$timescale           = ReadInt32($bootstrapInfo, $pos + 9);
		$currentMediaTime    = ReadInt64($bootstrapInfo, $pos + 13);
		$smpteTimeCodeOffset = ReadInt64($bootstrapInfo, $pos + 21);
		pos += 29;
		$movieIdentifier  = ReadString($bootstrapInfo, pos);
		$serverEntryCount = ReadByte($bootstrapInfo, pos++);
		for (int i = 0; i < $serverEntryCount; i++)
			$serverEntryTable[i] = ReadString($bootstrapInfo, pos);
		$qualityEntryCount = ReadByte($bootstrapInfo, pos++);
		for (int i = 0; i < $qualityEntryCount; i++)
			$qualityEntryTable[i] = ReadString($bootstrapInfo, pos);
		$drmData          = ReadString($bootstrapInfo, pos);
		$metadata         = ReadString($bootstrapInfo, pos);
		$segRunTableCount = ReadByte($bootstrapInfo, pos++);
		LogDebug(sprintf("%s:", "Segment Tables"));
		for (int i = 0; i < $segRunTableCount; i++)
		{
			LogDebug(sprintf("\nTable %d:", i + 1));
			string boxType;
			ReadBoxHeader($bootstrapInfo, pos, $boxType, $boxSize);
			if (boxType == "asrt")
				$segTable[i] = this.ParseAsrtBox($bootstrapInfo, pos);
			pos += $boxSize;
		}
		$fragRunTableCount = ReadByte($bootstrapInfo, $pos++);
		LogDebug(sprintf("%s:", "Fragment Tables"));
		for ($i = 0; $i < $fragRunTableCount; $i++)
		{
			LogDebug(sprintf("\nTable %d:", $i + 1));
			ReadBoxHeader($bootstrapInfo, $pos, $boxType, $boxSize);
			if ($boxType == "afrt")
				$fragTable[$i] = this.ParseAfrtBox($bootstrapInfo, $pos);
			$pos += $boxSize;
		}
		this.segTable  = array_replace(this.segTable, $segTable[0]);
		this.fragTable = array_replace(this.fragTable, $fragTable[0]);
		this.ParseSegAndFragTable();
	}

	function ParseAsrtBox($asrt, $pos)
	{
		$segTable          = array();
		$version           = ReadByte($asrt, $pos);
		$flags             = ReadInt24($asrt, $pos + 1);
		$qualityEntryCount = ReadByte($asrt, $pos + 4);
		$pos += 5;
		for ($i = 0; $i < $qualityEntryCount; $i++)
			$qualitySegmentUrlModifiers[$i] = ReadString($asrt, $pos);
		$segCount = ReadInt32($asrt, $pos);
		$pos += 4;
		LogDebug(sprintf(" %-8s%-10s", "Number", "Fragments"));
		for ($i = 0; $i < $segCount; $i++)
		{
			$firstSegment = ReadInt32($asrt, $pos);
			$segEntry =& $segTable[$firstSegment];
			$segEntry["firstSegment"]        = $firstSegment;
			$segEntry["fragmentsPerSegment"] = ReadInt32($asrt, $pos + 4);
			if ($segEntry["fragmentsPerSegment"] & 0x80000000)
				$segEntry["fragmentsPerSegment"] = 0;
			$pos += 8;
		}
		unset($segEntry);
		foreach ($segTable as $segEntry)
			LogDebug(sprintf(" %-8s%-10s", $segEntry["firstSegment"], $segEntry["fragmentsPerSegment"]));
		LogDebug("");
		return $segTable;
	}

	function ParseAfrtBox($afrt, $pos)
	{
		$fragTable         = array();
		$version           = ReadByte($afrt, $pos);
		$flags             = ReadInt24($afrt, $pos + 1);
		$timescale         = ReadInt32($afrt, $pos + 4);
		$qualityEntryCount = ReadByte($afrt, $pos + 8);
		$pos += 9;
		for ($i = 0; $i < $qualityEntryCount; $i++)
			$qualitySegmentUrlModifiers[$i] = ReadString($afrt, $pos);
		$fragEntries = ReadInt32($afrt, $pos);
		$pos += 4;
		LogDebug(sprintf(" %-12s%-16s%-16s%-16s", "Number", "Timestamp", "Duration", "Discontinuity"));
		for ($i = 0; $i < $fragEntries; $i++)
		{
			$firstFragment = ReadInt32($afrt, $pos);
			$fragEntry =& $fragTable[$firstFragment];
			$fragEntry["firstFragment"]          = $firstFragment;
			$fragEntry["firstFragmentTimestamp"] = ReadInt64($afrt, $pos + 4);
			$fragEntry["fragmentDuration"]       = ReadInt32($afrt, $pos + 12);
			$fragEntry["discontinuityIndicator"] = "";
			$pos += 16;
			if ($fragEntry["fragmentDuration"] == 0)
				$fragEntry["discontinuityIndicator"] = ReadByte($afrt, $pos++);
		}
		unset($fragEntry);
		foreach ($fragTable as $fragEntry)
			LogDebug(sprintf(" %-12s%-16s%-16s%-16s", $fragEntry["firstFragment"], $fragEntry["firstFragmentTimestamp"], $fragEntry["fragmentDuration"], $fragEntry["discontinuityIndicator"]));
		LogDebug("");
		return $fragTable;
	}

	function ParseSegAndFragTable()
	{
		$firstSegment  = reset(this.segTable);
		$lastSegment   = end(this.segTable);
		$firstFragment = reset(this.fragTable);
		$lastFragment  = end(this.fragTable);

		// Count total fragments by adding all entries in compactly coded segment table
		$invalidFragCount = false;
		$prev             = reset(this.segTable);
		this.fragCount  = $prev["fragmentsPerSegment"];
		while ($current = next(this.segTable))
		{
			this.fragCount += ($current["firstSegment"] - $prev["firstSegment"] - 1) * $prev["fragmentsPerSegment"];
			this.fragCount += $current["fragmentsPerSegment"];
			$prev = $current;
		}
		if (!(this.fragCount & 0x80000000))
			this.fragCount += $firstFragment["firstFragment"] - 1;
		if (this.fragCount & 0x80000000)
		{
			this.fragCount  = 0;
			$invalidFragCount = true;
		}
		if (this.fragCount < $lastFragment["firstFragment"])
			this.fragCount = $lastFragment["firstFragment"];

		// Determine starting segment and fragment
		if (this.segStart === false)
		{
			this.segStart = $firstSegment["firstSegment"];
			if (this.segStart < 1)
				this.segStart = 1;
		}
		if (this.fragStart === false)
		{
			this.fragStart = $firstFragment["firstFragment"] - 1;
			if (this.fragStart < 0)
				this.fragStart = 0;
		}
	}

	function GetSegmentFromFragment($fragNum)
	{
		$firstSegment  = reset(this.segTable);
		$lastSegment   = end(this.segTable);
		$firstFragment = reset(this.fragTable);
		$lastFragment  = end(this.fragTable);

		if (count(this.segTable) == 1)
			return $firstSegment["firstSegment"];
		else
		{
			$prev  = $firstSegment["firstSegment"];
			$start = $firstFragment["firstFragment"];
			for ($i = $firstSegment["firstSegment"]; $i <= $lastSegment["firstSegment"]; $i++)
			{
				if (isset(this.segTable[$i]))
					$seg = this.segTable[$i];
				else
					$seg = $prev;
				$end = $start + $seg["fragmentsPerSegment"];
				if (($fragNum >= $start) and ($fragNum < $end))
					return $i;
				$prev  = $seg;
				$start = $end;
			}
		}
		return $lastSegment["firstSegment"];
	}

	function DownloadFragments($cc, $manifest, $opt = array())
	{
		$start = 0;
		extract($opt, EXTR_IF_EXISTS);

		this.ParseManifest($cc, $manifest);
		$segNum  = this.segStart;
		$fragNum = this.fragStart;
		if ($start)
		{
			$segNum          = this.GetSegmentFromFragment($start);
			$fragNum         = $start - 1;
			this.segStart  = $segNum;
			this.fragStart = $fragNum;
		}
		this.lastFrag  = $fragNum;
		$opt["cc"]       = $cc;
		$opt["duration"] = 0;
		$firstFragment   = reset(this.fragTable);
		LogInfo(sprintf("Fragments Total: %s, First: %s, Start: %s, Parallel: %s", this.fragCount, $firstFragment["firstFragment"], $fragNum + 1, this.parallel));

		// Extract baseFilename
		this.baseFilename = this.media["url"];
		if (substr(this.baseFilename, -1) == '/')
			this.baseFilename = substr(this.baseFilename, 0, -1);
		this.baseFilename = RemoveExtension(this.baseFilename);
		$lastSlash          = strrpos(this.baseFilename, '/');
		if ($lastSlash !== false)
			this.baseFilename = substr(this.baseFilename, $lastSlash + 1);
		if (strpos($manifest, '?'))
			this.baseFilename = md5(substr($manifest, 0, strpos($manifest, '?'))) . '_' . this.baseFilename;
		else
			this.baseFilename = md5($manifest) . '_' . this.baseFilename;
		this.baseFilename .= "Seg" + $segNum . "-Frag";

		if ($fragNum >= this.fragCount)
			LogError("No fragment available for downloading");

		this.fragUrl = AbsoluteUrl(this.baseUrl, this.media["url"]);
		LogDebug("Base Fragment Url:\n" + this.fragUrl . "\n");
		LogDebug("Downloading Fragments:\n");

		while (($fragNum < this.fragCount) or $cc->active)
		{
			while ((count($cc->ch) < this.parallel) and ($fragNum < this.fragCount))
			{
				$frag       = array();
				$fragNum    = $fragNum + 1;
				$frag["id"] = $fragNum;
				LogInfo("Downloading $fragNum/this.fragCount fragments", true);
				if (in_array_field($fragNum, "firstFragment", this.fragTable, true))
					this.discontinuity = value_in_array_field($fragNum, "firstFragment", "discontinuityIndicator", this.fragTable, true);
				else
				{
					$closest = reset(this.fragTable);
					$closest = $closest["firstFragment"];
					while ($current = next(this.fragTable))
					{
						if ($current["firstFragment"] < $fragNum)
							$closest = $current["firstFragment"];
						else
							break;
					}
					this.discontinuity = value_in_array_field($closest, "firstFragment", "discontinuityIndicator", this.fragTable, true);
				}
				if (this.discontinuity !== "")
				{
					LogDebug("Skipping fragment $fragNum due to discontinuity, Type: " + this.discontinuity);
					$frag["response"] = false;
					this.rename     = true;
				}
				else if (file_exists(this.baseFilename . $fragNum))
				{
					LogDebug("Fragment $fragNum is already downloaded");
					$frag["response"] = file_get_contents(this.baseFilename . $fragNum);
				}
				if (isset($frag["response"]))
				{
					if (this.WriteFragment($frag, $opt) === STOP_PROCESSING)
						break 2;
					else
						continue;
				}

				LogDebug("Adding fragment $fragNum to download queue");
				$segNum = this.GetSegmentFromFragment($fragNum);
				$cc->addDownload(this.fragUrl . "Seg" + $segNum . "-Frag" + $fragNum . this.media["queryString"], $fragNum);
			}

			$downloads = $cc->checkDownloads();
			if ($downloads !== false)
			{
				for ($i = 0; $i < count($downloads); $i++)
				{
					$frag       = array();
					$download   = $downloads[$i];
					$frag["id"] = $download["id"];
					if ($download["status"] == 200)
					{
						if (this.VerifyFragment($download["response"]))
						{
							LogDebug("Fragment " + this.baseFilename . $download["id"] . " successfully downloaded");
							file_put_contents(this.baseFilename . $download["id"], $download["response"]);
							$frag["response"] = $download["response"];
						}
						else
						{
							LogDebug("Fragment " + $download["id"] . " failed to verify");
							LogDebug("Adding fragment " + $download["id"] . " to download queue");
							$cc->addDownload($download["url"], $download["id"]);
						}
					}
					else if ($download["status"] === false)
					{
						LogDebug("Fragment " + $download["id"] . " failed to download");
						LogDebug("Adding fragment " + $download["id"] . " to download queue");
						$cc->addDownload($download["url"], $download["id"]);
					}
					else if ($download["status"] == 403)
						LogError("Access Denied! Unable to download fragments.");
					else if ($download["status"] == 503)
					{
						LogDebug("Fragment " + $download["id"] . " seems temporary unavailable");
						LogDebug("Adding fragment " + $download["id"] . " to download queue");
						$cc->addDownload($download["url"], $download["id"]);
					}
					else
					{
						LogDebug("Fragment " + $download["id"] . " doesn't exist, Status: " + $download["status"]);
						$frag["response"] = false;
						this.rename     = true;
					}
					if (isset($frag["response"]))
					if (this.WriteFragment($frag, $opt) === STOP_PROCESSING)
						break 2;
				}
				unset($downloads, $download);
			}
		}

		LogInfo("");
		LogDebug("\nAll fragments downloaded successfully\n");
		$cc->stopDownloads();
		this.processed = true;
	}

	function VerifyFragment(&$frag)
	{
		$fragPos = 0;
		$fragLen = strlen($frag);

		//Some moronic servers add wrong boxSize in header causing fragment verification to fail so we have to fix the boxSize before processing the fragment.          
		while ($fragPos < $fragLen)
		{
			ReadBoxHeader($frag, $fragPos, $boxType, $boxSize);
			if ($boxType == "mdat")
			{
				$len = strlen(substr($frag, $fragPos, $boxSize));
				if ($boxSize and ($len == $boxSize))
					return true;
				else
				{
					$boxSize = $fragLen - $fragPos;
					WriteBoxSize($frag, $fragPos, $boxType, $boxSize);
					return true;
				}
			}
			$fragPos += $boxSize;
		}
		return false;
	}

	function DecodeFragment($frag, $fragNum, $opt = array())
	{
		$debug = this.debug;
		$flv   = false;
		extract($opt, EXTR_IF_EXISTS);

		$flvData  = "";
		$fragPos  = 0;
		$packetTS = 0;
		$fragLen  = strlen($frag);

		if (!this.VerifyFragment($frag))
		{
			LogInfo("Skipping fragment number $fragNum");
			return false;
		}

		while ($fragPos < $fragLen)
		{
			ReadBoxHeader($frag, $fragPos, $boxType, $boxSize);
			if ($boxType == "mdat")
			{
				$fragLen = $fragPos + $boxSize;
				break;
			}
			$fragPos += $boxSize;
		}

		LogDebug(sprintf("\nFragment %d:\n" + this.format . "%-16s", $fragNum, "Type", "CurrentTS", "PreviousTS", "Size", "Position"), $debug);
		while ($fragPos < $fragLen)
		{
			$packetType = ReadByte($frag, $fragPos);
			$packetSize = ReadInt24($frag, $fragPos + 1);
			$packetTS   = ReadInt24($frag, $fragPos + 4);
			$packetTS   = $packetTS | (ReadByte($frag, $fragPos + 7) << 24);
			if ($packetTS & 0x80000000)
				$packetTS &= 0x7FFFFFFF;
			$totalTagLen = this.tagHeaderLen + $packetSize + this.prevTagSize;

			// Try to fix the odd timestamps and make them zero based
			$currentTS = $packetTS;
			$lastTS    = this.prevVideoTS >= this.prevAudioTS ? this.prevVideoTS : this.prevAudioTS;
			$fixedTS   = $lastTS + FRAMEFIX_STEP;
			if ((this.baseTS == INVALID_TIMESTAMP) and (($packetType == AUDIO) or ($packetType == VIDEO)))
				this.baseTS = $packetTS;
			if ((this.baseTS > 1000) and ($packetTS >= this.baseTS))
				$packetTS -= this.baseTS;
			if ($lastTS != INVALID_TIMESTAMP)
			{
				$timeShift = $packetTS - $lastTS;
				if ($timeShift > this.fixWindow)
				{
					LogDebug("Timestamp gap detected: PacketTS=" + $packetTS . " LastTS=" + $lastTS . " Timeshift=" + $timeShift, $debug);
					if (this.baseTS < $packetTS)
						this.baseTS += $timeShift - FRAMEFIX_STEP;
					else
						this.baseTS = $timeShift - FRAMEFIX_STEP;
					$packetTS = $fixedTS;
				}
				else
				{
					$lastTS = $packetType == VIDEO ? this.prevVideoTS : this.prevAudioTS;
					if ($packetTS < ($lastTS - this.fixWindow))
					{
						if ((this.negTS != INVALID_TIMESTAMP) and (($packetTS + this.negTS) < ($lastTS - this.fixWindow)))
							this.negTS = INVALID_TIMESTAMP;
						if (this.negTS == INVALID_TIMESTAMP)
						{
							this.negTS = $fixedTS - $packetTS;
							LogDebug("Negative timestamp detected: PacketTS=" + $packetTS . " LastTS=" + $lastTS . " NegativeTS=" + this.negTS, $debug);
							$packetTS = $fixedTS;
						}
						else
						{
							if (($packetTS + this.negTS) <= ($lastTS + this.fixWindow))
								$packetTS += this.negTS;
							else
							{
								this.negTS = $fixedTS - $packetTS;
								LogDebug("Negative timestamp override: PacketTS=" + $packetTS . " LastTS=" + $lastTS . " NegativeTS=" + this.negTS, $debug);
								$packetTS = $fixedTS;
							}
						}
					}
				}
			}
			if ($packetTS != $currentTS)
				WriteFlvTimestamp($frag, $fragPos, $packetTS);

			switch ($packetType)
			{
			case AUDIO:
				if ($packetTS > this.prevAudioTS - this.fixWindow)
				{
					$FrameInfo = ReadByte($frag, $fragPos + this.tagHeaderLen);
					$CodecID   = ($FrameInfo & 0xF0) >> 4;
					if ($CodecID == CODEC_ID_AAC)
					{
						$AAC_PacketType = ReadByte($frag, $fragPos + this.tagHeaderLen + 1);
						if ($AAC_PacketType == AAC_SEQUENCE_HEADER)
						{
							if (this.AAC_HeaderWritten)
							{
								LogDebug(sprintf("%s\n" + this.format, "Skipping AAC sequence header", "AUDIO", $packetTS, this.prevAudioTS, $packetSize), $debug);
								break;
							}
							else
							{
								LogDebug("Writing AAC sequence header", $debug);
								this.AAC_HeaderWritten = true;
							}
						}
						else if (!this.AAC_HeaderWritten)
						{
							LogDebug(sprintf("%s\n" + this.format, "Discarding audio packet received before AAC sequence header", "AUDIO", $packetTS, this.prevAudioTS, $packetSize), $debug);
							break;
						}
					}
					if ($packetSize > 0)
					{
						// Check for packets with non-monotonic audio timestamps and fix them
						if (!(($CodecID == CODEC_ID_AAC) and (($AAC_PacketType == AAC_SEQUENCE_HEADER) or this.prevAAC_Header)))
						if ((this.prevAudioTS != INVALID_TIMESTAMP) and ($packetTS <= this.prevAudioTS))
						{
							LogDebug(sprintf("%s\n" + this.format, "Fixing audio timestamp", "AUDIO", $packetTS, this.prevAudioTS, $packetSize), $debug);
							$packetTS += (FRAMEFIX_STEP / 5) + (this.prevAudioTS - $packetTS);
							WriteFlvTimestamp($frag, $fragPos, $packetTS);
						}
						if (is_resource($flv))
						{
							this.pAudioTagPos = ftell($flv);
							$status             = fwrite($flv, substr($frag, $fragPos, $totalTagLen), $totalTagLen);
							if (!$status)
								LogError("Failed to write flv data to file");
							if ($debug)
								LogDebug(sprintf(this.format . "%-16s", "AUDIO", $packetTS, this.prevAudioTS, $packetSize, this.pAudioTagPos));
						}
						else
						{
							$flvData .= substr($frag, $fragPos, $totalTagLen);
							if ($debug)
								LogDebug(sprintf(this.format, "AUDIO", $packetTS, this.prevAudioTS, $packetSize));
						}
						if (($CodecID == CODEC_ID_AAC) and ($AAC_PacketType == AAC_SEQUENCE_HEADER))
							this.prevAAC_Header = true;
						else
							this.prevAAC_Header = false;
						this.prevAudioTS  = $packetTS;
						this.pAudioTagLen = $totalTagLen;
					}
					else
						LogDebug(sprintf("%s\n" + this.format, "Skipping small sized audio packet", "AUDIO", $packetTS, this.prevAudioTS, $packetSize), $debug);
				}
				else
					LogDebug(sprintf("%s\n" + this.format, "Skipping audio packet in fragment $fragNum", "AUDIO", $packetTS, this.prevAudioTS, $packetSize), $debug);
				if (!this.audio)
					this.audio = true;
				break;
			case VIDEO:
				if ($packetTS > this.prevVideoTS - this.fixWindow)
				{
					$FrameInfo = ReadByte($frag, $fragPos + this.tagHeaderLen);
					$FrameType = ($FrameInfo & 0xF0) >> 4;
					$CodecID   = $FrameInfo & 0x0F;
					if ($FrameType == FRAME_TYPE_INFO)
					{
						LogDebug(sprintf("%s\n" + this.format, "Skipping video info frame", "VIDEO", $packetTS, this.prevVideoTS, $packetSize), $debug);
						break;
					}
					if ($CodecID == CODEC_ID_AVC)
					{
						$AVC_PacketType = ReadByte($frag, $fragPos + this.tagHeaderLen + 1);
						if ($AVC_PacketType == AVC_SEQUENCE_HEADER)
						{
							if (this.AVC_HeaderWritten)
							{
								LogDebug(sprintf("%s\n" + this.format, "Skipping AVC sequence header", "VIDEO", $packetTS, this.prevVideoTS, $packetSize), $debug);
								break;
							}
							else
							{
								LogDebug("Writing AVC sequence header", $debug);
								this.AVC_HeaderWritten = true;
							}
						}
						else if (!this.AVC_HeaderWritten)
						{
							LogDebug(sprintf("%s\n" + this.format, "Discarding video packet received before AVC sequence header", "VIDEO", $packetTS, this.prevVideoTS, $packetSize), $debug);
							break;
						}
					}
					if ($packetSize > 0)
					{
						$pts = $packetTS;
						if (($CodecID == CODEC_ID_AVC) and ($AVC_PacketType == AVC_NALU))
						{
							$cts = ReadInt24($frag, $fragPos + this.tagHeaderLen + 2);
							$cts = ($cts + 0xff800000) ^ 0xff800000;
							$pts = $packetTS + $cts;
							if ($cts != 0)
								LogDebug("DTS: $packetTS CTS: $cts PTS: $pts", $debug);
						}

						// Check for packets with non-monotonic video timestamps and fix them
						if (!(($CodecID == CODEC_ID_AVC) and (($AVC_PacketType == AVC_SEQUENCE_HEADER) or ($AVC_PacketType == AVC_SEQUENCE_END) or this.prevAVC_Header)))
						if ((this.prevVideoTS != INVALID_TIMESTAMP) and ($packetTS <= this.prevVideoTS))
						{
							LogDebug(sprintf("%s\n" + this.format, "Fixing video timestamp", "VIDEO", $packetTS, this.prevVideoTS, $packetSize), $debug);
							$packetTS += (FRAMEFIX_STEP / 5) + (this.prevVideoTS - $packetTS);
							WriteFlvTimestamp($frag, $fragPos, $packetTS);
						}
						if (is_resource($flv))
						{
							this.pVideoTagPos = ftell($flv);
							$status             = fwrite($flv, substr($frag, $fragPos, $totalTagLen), $totalTagLen);
							if (!$status)
								LogError("Failed to write flv data to file");
							if ($debug)
								LogDebug(sprintf(this.format . "%-16s", "VIDEO", $packetTS, this.prevVideoTS, $packetSize, this.pVideoTagPos));
						}
						else
						{
							$flvData .= substr($frag, $fragPos, $totalTagLen);
							if ($debug)
								LogDebug(sprintf(this.format, "VIDEO", $packetTS, this.prevVideoTS, $packetSize));
						}
						if (($CodecID == CODEC_ID_AVC) and ($AVC_PacketType == AVC_SEQUENCE_HEADER))
							this.prevAVC_Header = true;
						else
							this.prevAVC_Header = false;
						this.prevVideoTS  = $packetTS;
						this.pVideoTagLen = $totalTagLen;
					}
					else
						LogDebug(sprintf("%s\n" + this.format, "Skipping small sized video packet", "VIDEO", $packetTS, this.prevVideoTS, $packetSize), $debug);
				}
				else
					LogDebug(sprintf("%s\n" + this.format, "Skipping video packet in fragment $fragNum", "VIDEO", $packetTS, this.prevVideoTS, $packetSize), $debug);
				if (!this.video)
					this.video = true;
				break;
			case SCRIPT_DATA:
				break;
			default:
				if (($packetType == 10) or ($packetType == 11))
					LogError("This stream is encrypted with Akamai DRM. Decryption of such streams isn't currently possible with this script.", 2);
				else if (($packetType == 40) or ($packetType == 41))
					LogError("This stream is encrypted with FlashAccess DRM. Decryption of such streams isn't currently possible with this script.", 2);
				else
				{
					LogInfo("Unknown packet type " + $packetType . " encountered! Unable to process fragment $fragNum");
					break 2;
				}
			}
			$fragPos += $totalTagLen;
		}
		this.duration = round($packetTS / 1000, 0);
		if (is_resource($flv))
		{
			this.filesize = ftell($flv) / (1024 * 1024);
			return true;
		}
		else
			return $flvData;
	}

	function WriteFragment($download, &$opt)
	{
		this.frags[$download["id"]] = $download;

		$available = count(this.frags);
		for ($i = 0; $i < $available; $i++)
		{
			if (isset(this.frags[this.lastFrag + 1]))
			{
				$frag = this.frags[this.lastFrag + 1];
				if ($frag["response"] !== false)
				{
					LogDebug("Writing fragment " + $frag["id"] . " to flv file");
					if (!isset($opt["file"]))
					{
						$opt["debug"] = false;
						if (this.play)
							$outFile = STDOUT;
						else if (this.outFile)
						{
							if ($opt["filesize"])
								$outFile = JoinUrl(this.outDir, this.outFile . '-' . this.fileCount++ . ".flv");
							else
								$outFile = JoinUrl(this.outDir, this.outFile . ".flv");
						}
						else
						{
							if ($opt["filesize"])
								$outFile = JoinUrl(this.outDir, this.baseFilename . '-' . this.fileCount++ . ".flv");
							else
								$outFile = JoinUrl(this.outDir, this.baseFilename . ".flv");
						}
						this.InitDecoder();
						this.DecodeFragment($frag["response"], $frag["id"], $opt);
						$opt["file"] = WriteFlvFile($outFile, this.audio, this.video);
						if (this.metadata)
							WriteMetadata($this, $opt["file"]);

						$opt["debug"] = this.debug;
						this.InitDecoder();
					}
					$flvData = this.DecodeFragment($frag["response"], $frag["id"], $opt);
					if (strlen($flvData))
					{
						$status = fwrite($opt["file"], $flvData, strlen($flvData));
						if (!$status)
							LogError("Failed to write flv data");
						if (!this.play)
							this.filesize = ftell($opt["file"]) / (1024 * 1024);
					}
					this.lastFrag = $frag["id"];
				}
				else
				{
					this.lastFrag += 1;
					LogDebug("Skipping failed fragment " + this.lastFrag);
				}
				unset(this.frags[this.lastFrag]);
			}
			else
				break;

			if ($opt["tDuration"] and (($opt["duration"] + this.duration) >= $opt["tDuration"]))
			{
				LogInfo("");
				LogInfo(($opt["duration"] + this.duration) . " seconds of content has been recorded successfully.", true);
				return STOP_PROCESSING;
			}
			if ($opt["filesize"] and (this.filesize >= $opt["filesize"]))
			{
				this.filesize = 0;
				$opt["duration"] += this.duration;
				fclose($opt["file"]);
				unset($opt["file"]);
			}
		}

		if (this.frags.Count() == 0)
			unset(this.frags);
		return true;
	*/
	}
}

class Manifest_parsed
{
	public Dictionary<string, string> bootstrapInfo = new Dictionary<string, string> ();
	public List<Manifest_parsed_media> media = new List<Manifest_parsed_media> ();

	public Manifest_parsed ()
	{
	}

	public Manifest_parsed (Dictionary<string, string> bootstrapInfo, List<Manifest_parsed_media> media)
	{
		this.bootstrapInfo = bootstrapInfo;
		this.media = media;
	}
}

class Manifest_parsed_media
{
	public string bootstrapInfoId;
	public int width;
	public int height;
	public int bitrate;
	public string url;

	public string metadata;

	public Manifest_parsed_media ()
	{
	}

	public Manifest_parsed_media (string bootstrapInfoId, int width, int height, int bitrate, string url, string metadata)
	{
		this.bootstrapInfoId = bootstrapInfoId;
		this.width = width;
		this.height = height;
		this.bitrate = bitrate;
		this.url = url;

		this.metadata = metadata;
	}
}