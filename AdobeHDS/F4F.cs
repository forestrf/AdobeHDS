using System;
using System.Collections.Generic;
using System.Xml;
using System.Net;
using System.Diagnostics;
using System.IO;


public class F4F : Functions
{
	string baseUrl, baseFilename;
	byte[] bootstrapInfo;
	Manifest_parsed_media media;
	SegTable_content segTable;
	Dictionary<int,Frag_table_content> fragTable;
	int segStart = -1, fragStart = -1, fragCount = -1, parallel = 8;

	Dictionary<int, Frag_response> frags;

	Dictionary<string, object> opt;

	string outFileGlobal = "";

	string outDir = "";

	int lastFrag = 0;

	bool processed = false;










	int discontinuity = 0;
	object audio, auth, baseTS, fixWindow;
	object video;
	object prevTagSize, tagHeaderLen;
	object prevAudioTS, prevVideoTS, pAudioTagLen, pVideoTagLen, pAudioTagPos, pVideoTagPos;
	object prevAVC_Header, prevAAC_Header, AVC_HeaderWritten, AAC_HeaderWritten;
	object negTS;

	public F4F ()
	{
		this.auth = "";
		this.fixWindow = 1000;
		this.InitDecoder ();
	}

	public void InitDecoder ()
	{
		this.audio = false;
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
		LogInfo ("Processing manifest info....");

		XmlDocument doc = new XmlDocument ();
		try {
			string manifest_xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?><manifest xmlns=\"http://ns.adobe.com/f4m/1.0\"><id>assets5/2014/07/25/59508586-1ACD-446B-9A90-2128427BC6A8/es.smil</id><width>1280</width><height>720</height><duration>853.88</duration><mimeType>video/mp4</mimeType><streamType>recorded</streamType><deliveryType>streaming</deliveryType><bootstrapInfo profile=\"named\" id=\"bootstrap1\">AAAFvmFic3QBAAAAAAAAAQAAAAPoAAAAAAANB3gAAAAAAAAAAAABAAEAAAABAAAAGmFzcnQBAAAAAQAAAAABAAAAAQAAAFYBAAAFdmFmcnQBAAAAAAAD6AEAAAAAVgAAAAEAAAAAAAAAAAAAJxAAAAACAAAAAAAAJxAAACcQAAAAAwAAAAAAAE4gAAAnEAAAAAQAAAAAAAB1MAAAJxAAAAAFAAAAAAAAnEAAACcQAAAABgAAAAAAAMNQAAAnEAAAAAcAAAAAAADqYAAAJxAAAAAIAAAAAAABEXAAACcQAAAACQAAAAAAATiAAAAnEAAAAAoAAAAAAAFfkAAAJxAAAAALAAAAAAABhqAAACcQAAAADAAAAAAAAa2wAAAnEAAAAA0AAAAAAAHUwAAAJxAAAAAOAAAAAAAB+9AAACcQAAAADwAAAAAAAiLgAAAnEAAAABAAAAAAAAJJ8AAAJxAAAAARAAAAAAACcQAAACcQAAAAEgAAAAAAApgQAAAnEAAAABMAAAAAAAK/IAAAJxAAAAAUAAAAAAAC5jAAACcQAAAAFQAAAAAAAw1AAAAnEAAAABYAAAAAAAM0UAAAJxAAAAAXAAAAAAADW2AAACcQAAAAGAAAAAAAA4JwAAAnEAAAABkAAAAAAAOpgAAAJxAAAAAaAAAAAAAD0JAAACcQAAAAGwAAAAAAA/egAAAnEAAAABwAAAAAAAQesAAAJxAAAAAdAAAAAAAERcAAACcQAAAAHgAAAAAABGzQAAAnEAAAAB8AAAAAAAST4AAAJxAAAAAgAAAAAAAEuvAAACcQAAAAIQAAAAAABOIAAAAnEAAAACIAAAAAAAUJEAAAJxAAAAAjAAAAAAAFMCAAACcQAAAAJAAAAAAABVcwAAAnEAAAACUAAAAAAAV+QAAAJxAAAAAmAAAAAAAFpVAAACcQAAAAJwAAAAAABcxgAAAnEAAAACgAAAAAAAXzcAAAJxAAAAApAAAAAAAGGoAAACcQAAAAKgAAAAAABkGQAAAnEAAAACsAAAAAAAZooAAAJxAAAAAsAAAAAAAGj7AAACcQAAAALQAAAAAABrbAAAAnEAAAAC4AAAAAAAbd0AAAJxAAAAAvAAAAAAAHBOAAACcQAAAAMAAAAAAAByvwAAAnEAAAADEAAAAAAAdTAAAAJxAAAAAyAAAAAAAHehAAACcQAAAAMwAAAAAAB6EgAAAnEAAAADQAAAAAAAfIMAAAJxAAAAA1AAAAAAAH70AAACcQAAAANgAAAAAACBZQAAAnEAAAADcAAAAAAAg9YAAAJxAAAAA4AAAAAAAIZHAAACcQAAAAOQAAAAAACIuAAAAnEAAAADoAAAAAAAiykAAAJxAAAAA7AAAAAAAI2aAAACcQAAAAPAAAAAAACQCwAAAnEAAAAD0AAAAAAAknwAAAJxAAAAA+AAAAAAAJTtAAACcQAAAAPwAAAAAACXXgAAAnEAAAAEAAAAAAAAmc8AAAJxAAAABBAAAAAAAJxAAAACcQAAAAQgAAAAAACesQAAAnEAAAAEMAAAAAAAoSIAAAJxAAAABEAAAAAAAKOTAAACcQAAAARQAAAAAACmBAAAAnEAAAAEYAAAAAAAqHUAAAJxAAAABHAAAAAAAKrmAAACcQAAAASAAAAAAACtVwAAAnEAAAAEkAAAAAAAr8gAAAJxAAAABKAAAAAAALI5AAACcQAAAASwAAAAAAC0qgAAAnEAAAAEwAAAAAAAtxsAAAJxAAAABNAAAAAAALmMAAACcQAAAATgAAAAAAC7/QAAAnEAAAAE8AAAAAAAvm4AAAJxAAAABQAAAAAAAMDfAAACcQAAAAUQAAAAAADDUAAAAnEAAAAFIAAAAAAAxcEAAAJxAAAABTAAAAAAAMgyAAACbAAAAAVAAAAAAADKngAAAnEAAAAFUAAAAAAAzQ8AAAJxAAAABWAAAAAAAM+AAAAA94</bootstrapInfo><media streamId=\"1\" bootstrapInfoId=\"bootstrap1\" width=\"640\" height=\"360\" bitrate=\"707\" url=\"media_b724000.abst/\"><metadata>AgAKb25NZXRhRGF0YQgAAAAAAAl0cmFja2luZm8KAAAAAgMACGxhbmd1YWdlAgADdW5kAAl0aW1lc2NhbGUAQPX5AAAAAAAABmxlbmd0aABBklKAwAAAAAARc2FtcGxlZGVzY3JpcHRpb24KAAAAAQMACnNhbXBsZXR5cGUCAARhdmMxAAAJAAAJAwAIbGFuZ3VhZ2UCAAN1bmQACXRpbWVzY2FsZQBA53AAAAAAAAAGbGVuZ3RoAEGDinwAAAAAABFzYW1wbGVkZXNjcmlwdGlvbgoAAAABAwAKc2FtcGxldHlwZQIABG1wNGEAAAkAAAkADWF1ZGlvY2hhbm5lbHMAQAAAAAAAAAAAD2F1ZGlvc2FtcGxlcmF0ZQBA53AAAAAAAAAOdmlkZW9mcmFtZXJhdGUAQDkAAAAAAAAABmFhY2FvdABAAAAAAAAAAAAIYXZjbGV2ZWwAQD4AAAAAAAAACmF2Y3Byb2ZpbGUAQFNAAAAAAAAADGF1ZGlvY29kZWNpZAIABG1wNGEADHZpZGVvY29kZWNpZAIABGF2YzEABXdpZHRoAECEAAAAAAAAAAZoZWlnaHQAQHaAAAAAAAAACmZyYW1lV2lkdGgAQIQAAAAAAAAAC2ZyYW1lSGVpZ2h0AEB2gAAAAAAAAAxkaXNwbGF5V2lkdGgAQIQAAAAAAAAADWRpc3BsYXlIZWlnaHQAQHaAAAAAAAAACWZyYW1lcmF0ZQBAOQAAAAAAAAAMbW9vdnBvc2l0aW9uAEBEAAAAAAAAAAhkdXJhdGlvbgBAiq8KPXCj1wAACQ==</metadata></media><media streamId=\"2\" bootstrapInfoId=\"bootstrap1\" width=\"640\" height=\"360\" bitrate=\"1029\" url=\"media_b1054000.abst/\"><metadata>AgAKb25NZXRhRGF0YQgAAAAAAAl0cmFja2luZm8KAAAAAgMACGxhbmd1YWdlAgADdW5kAAl0aW1lc2NhbGUAQPX5AAAAAAAABmxlbmd0aABBklKAwAAAAAARc2FtcGxlZGVzY3JpcHRpb24KAAAAAQMACnNhbXBsZXR5cGUCAARhdmMxAAAJAAAJAwAIbGFuZ3VhZ2UCAAN1bmQACXRpbWVzY2FsZQBA53AAAAAAAAAGbGVuZ3RoAEGDinwAAAAAABFzYW1wbGVkZXNjcmlwdGlvbgoAAAABAwAKc2FtcGxldHlwZQIABG1wNGEAAAkAAAkADWF1ZGlvY2hhbm5lbHMAQAAAAAAAAAAAD2F1ZGlvc2FtcGxlcmF0ZQBA53AAAAAAAAAOdmlkZW9mcmFtZXJhdGUAQDkAAAAAAAAABmFhY2FvdABAAAAAAAAAAAAIYXZjbGV2ZWwAQD4AAAAAAAAACmF2Y3Byb2ZpbGUAQFNAAAAAAAAADGF1ZGlvY29kZWNpZAIABG1wNGEADHZpZGVvY29kZWNpZAIABGF2YzEABXdpZHRoAECEAAAAAAAAAAZoZWlnaHQAQHaAAAAAAAAACmZyYW1lV2lkdGgAQIQAAAAAAAAAC2ZyYW1lSGVpZ2h0AEB2gAAAAAAAAAxkaXNwbGF5V2lkdGgAQIQAAAAAAAAADWRpc3BsYXlIZWlnaHQAQHaAAAAAAAAACWZyYW1lcmF0ZQBAOQAAAAAAAAAMbW9vdnBvc2l0aW9uAEBEAAAAAAAAAAhkdXJhdGlvbgBAiq8KPXCj1wAACQ==</metadata></media><media streamId=\"3\" bootstrapInfoId=\"bootstrap1\" width=\"720\" height=\"404\" bitrate=\"1458\" url=\"media_b1494000.abst/\"><metadata>AgAKb25NZXRhRGF0YQgAAAAAAAl0cmFja2luZm8KAAAAAgMACGxhbmd1YWdlAgADdW5kAAl0aW1lc2NhbGUAQPX5AAAAAAAABmxlbmd0aABBklKAwAAAAAARc2FtcGxlZGVzY3JpcHRpb24KAAAAAQMACnNhbXBsZXR5cGUCAARhdmMxAAAJAAAJAwAIbGFuZ3VhZ2UCAAN1bmQACXRpbWVzY2FsZQBA53AAAAAAAAAGbGVuZ3RoAEGDinwAAAAAABFzYW1wbGVkZXNjcmlwdGlvbgoAAAABAwAKc2FtcGxldHlwZQIABG1wNGEAAAkAAAkADWF1ZGlvY2hhbm5lbHMAQAAAAAAAAAAAD2F1ZGlvc2FtcGxlcmF0ZQBA53AAAAAAAAAOdmlkZW9mcmFtZXJhdGUAQDkAAAAAAAAABmFhY2FvdABAAAAAAAAAAAAIYXZjbGV2ZWwAQD4AAAAAAAAACmF2Y3Byb2ZpbGUAQFNAAAAAAAAADGF1ZGlvY29kZWNpZAIABG1wNGEADHZpZGVvY29kZWNpZAIABGF2YzEABXdpZHRoAECGaAAAAAAAAAZoZWlnaHQAQHlAAAAAAAAACmZyYW1lV2lkdGgAQIaAAAAAAAAAC2ZyYW1lSGVpZ2h0AEB5QAAAAAAAAAxkaXNwbGF5V2lkdGgAQIZoAAAAAAAADWRpc3BsYXlIZWlnaHQAQHlAAAAAAAAACWZyYW1lcmF0ZQBAOQAAAAAAAAAMbW9vdnBvc2l0aW9uAEBEAAAAAAAAAAhkdXJhdGlvbgBAiq8KPXCj1wAACQ==</metadata></media><bootstrapInfo profile=\"named\" id=\"bootstrap2\">AAAFvmFic3QBAAAAAAAAAQAAAAPoAAAAAAANBygAAAAAAAAAAAABAAEAAAABAAAAGmFzcnQBAAAAAQAAAAABAAAAAQAAAFYBAAAFdmFmcnQBAAAAAAAD6AEAAAAAVgAAAAEAAAAAAAAAAAAAJxAAAAACAAAAAAAAJxAAACcQAAAAAwAAAAAAAE4gAAAnEAAAAAQAAAAAAAB1MAAAJxAAAAAFAAAAAAAAnEAAACcQAAAABgAAAAAAAMNQAAAnEAAAAAcAAAAAAADqYAAAJxAAAAAIAAAAAAABEXAAACcQAAAACQAAAAAAATiAAAAnEAAAAAoAAAAAAAFfkAAAJxAAAAALAAAAAAABhqAAACcQAAAADAAAAAAAAa2wAAAnEAAAAA0AAAAAAAHUwAAAJxAAAAAOAAAAAAAB+9AAACcQAAAADwAAAAAAAiLgAAAnEAAAABAAAAAAAAJJ8AAAJxAAAAARAAAAAAACcQAAACcQAAAAEgAAAAAAApgQAAAnEAAAABMAAAAAAAK/IAAAJxAAAAAUAAAAAAAC5jAAACcQAAAAFQAAAAAAAw1AAAAnEAAAABYAAAAAAAM0UAAAJxAAAAAXAAAAAAADW2AAACcQAAAAGAAAAAAAA4JwAAAnEAAAABkAAAAAAAOpgAAAJxAAAAAaAAAAAAAD0JAAACcQAAAAGwAAAAAAA/egAAAnEAAAABwAAAAAAAQesAAAJxAAAAAdAAAAAAAERcAAACcQAAAAHgAAAAAABGzQAAAnEAAAAB8AAAAAAAST4AAAJxAAAAAgAAAAAAAEuvAAACcQAAAAIQAAAAAABOIAAAAnEAAAACIAAAAAAAUJEAAAJxAAAAAjAAAAAAAFMCAAACcQAAAAJAAAAAAABVcwAAAnEAAAACUAAAAAAAV+QAAAJxAAAAAmAAAAAAAFpVAAACcQAAAAJwAAAAAABcxgAAAnEAAAACgAAAAAAAXzcAAAJxAAAAApAAAAAAAGGoAAACcQAAAAKgAAAAAABkGQAAAnEAAAACsAAAAAAAZooAAAJxAAAAAsAAAAAAAGj7AAACcQAAAALQAAAAAABrbAAAAnEAAAAC4AAAAAAAbd0AAAJxAAAAAvAAAAAAAHBOAAACcQAAAAMAAAAAAAByvwAAAnEAAAADEAAAAAAAdTAAAAJxAAAAAyAAAAAAAHehAAACcQAAAAMwAAAAAAB6EgAAAnEAAAADQAAAAAAAfIMAAAJxAAAAA1AAAAAAAH70AAACcQAAAANgAAAAAACBZQAAAnEAAAADcAAAAAAAg9YAAAJxAAAAA4AAAAAAAIZHAAACcQAAAAOQAAAAAACIuAAAAnEAAAADoAAAAAAAiykAAAJxAAAAA7AAAAAAAI2aAAACcQAAAAPAAAAAAACQCwAAAnEAAAAD0AAAAAAAknwAAAJxAAAAA+AAAAAAAJTtAAACcQAAAAPwAAAAAACXXgAAAnEAAAAEAAAAAAAAmc8AAAJxAAAABBAAAAAAAJxAAAACcQAAAAQgAAAAAACesQAAAnEAAAAEMAAAAAAAoSIAAAJxAAAABEAAAAAAAKOTAAACcQAAAARQAAAAAACmBAAAAnEAAAAEYAAAAAAAqHUAAAJxAAAABHAAAAAAAKrmAAACcQAAAASAAAAAAACtVwAAAnEAAAAEkAAAAAAAr8gAAAJxAAAABKAAAAAAALI5AAACcQAAAASwAAAAAAC0qgAAAnEAAAAEwAAAAAAAtxsAAAJxAAAABNAAAAAAALmMAAACcQAAAATgAAAAAAC7/QAAAnEAAAAE8AAAAAAAvm4AAAJxAAAABQAAAAAAAMDfAAACcQAAAAUQAAAAAADDUAAAAnEAAAAFIAAAAAAAxcEAAAJxAAAABTAAAAAAAMgyAAACbAAAAAVAAAAAAADKngAAAnEAAAAFUAAAAAAAzQ8AAAJxAAAABWAAAAAAAM+AAAAA8o</bootstrapInfo><media streamId=\"4\" bootstrapInfoId=\"bootstrap2\" width=\"1280\" height=\"720\" bitrate=\"1996\" url=\"media_b2044000.abst/\"><metadata>AgAKb25NZXRhRGF0YQgAAAAAAAl0cmFja2luZm8KAAAAAgMACGxhbmd1YWdlAgADdW5kAAl0aW1lc2NhbGUAQPX5AAAAAAAABmxlbmd0aABBklIQQAAAAAARc2FtcGxlZGVzY3JpcHRpb24KAAAAAQMACnNhbXBsZXR5cGUCAARhdmMxAAAJAAAJAwAIbGFuZ3VhZ2UCAAN1bmQACXRpbWVzY2FsZQBA53AAAAAAAAAGbGVuZ3RoAEGDihwAAAAAABFzYW1wbGVkZXNjcmlwdGlvbgoAAAABAwAKc2FtcGxldHlwZQIABG1wNGEAAAkAAAkADWF1ZGlvY2hhbm5lbHMAQAAAAAAAAAAAD2F1ZGlvc2FtcGxlcmF0ZQBA53AAAAAAAAAOdmlkZW9mcmFtZXJhdGUAQDkAAAAAAAAABmFhY2FvdABAAAAAAAAAAAAIYXZjbGV2ZWwAQD8AAAAAAAAACmF2Y3Byb2ZpbGUAQFNAAAAAAAAADGF1ZGlvY29kZWNpZAIABG1wNGEADHZpZGVvY29kZWNpZAIABGF2YzEABXdpZHRoAECUAAAAAAAAAAZoZWlnaHQAQIaAAAAAAAAACmZyYW1lV2lkdGgAQJQAAAAAAAAAC2ZyYW1lSGVpZ2h0AECGgAAAAAAAAAxkaXNwbGF5V2lkdGgAQJQAAAAAAAAADWRpc3BsYXlIZWlnaHQAQIaAAAAAAAAACWZyYW1lcmF0ZQBAOQAAAAAAAAAMbW9vdnBvc2l0aW9uAEBEAAAAAAAAAAhkdXJhdGlvbgBAiq5mZmZmZgAACQ==</metadata></media></manifest>";
			//manifest_xml = new WebClient().DownloadString(manifest);

			// Remove annoying manifests
			int nm_i = manifest_xml.IndexOf ("xmlns");
			string nm = manifest_xml.Substring (nm_i, manifest_xml.IndexOf ("\"", nm_i + 9) - nm_i + 1);
			Debug.WriteLine ("namespace removed:" + nm);
			manifest_xml = manifest_xml.Replace (nm, "");

			doc.LoadXml (manifest_xml);
		} catch (Exception e) {
			LogError ("Unable to download the manifest: " + e);
			return;
		}


		XmlNode node_baseURL = doc.SelectSingleNode ("/manifest/baseURL");
		if (node_baseURL != null) {
			baseUrl = node_baseURL.InnerText;
		} else {
			baseUrl = manifest;
			int pos_int = baseUrl.IndexOf ("?");
			if (pos_int != -1) {
				baseUrl = baseUrl.Substring (0, pos_int);
			}
			baseUrl = baseUrl.Substring (0, baseUrl.LastIndexOf ("/"));
		}


		List<Manifest_parsed_media> manifest_parsed_media_list = new List<Manifest_parsed_media> ();


		XmlNodeList nodes = doc.DocumentElement.SelectNodes ("/manifest/media");

		Console.WriteLine (nodes.Count);

		foreach (XmlNode node in nodes) {

			Manifest_parsed_media manifest_parsed_media = new Manifest_parsed_media ();

			if (node.Attributes ["bitrate"] != null) {
				manifest_parsed_media.bitrate = int.Parse (node.Attributes ["bitrate"].InnerText);
			}
			manifest_parsed_media.baseUrl = baseUrl;
			manifest_parsed_media.url = node.Attributes ["url"].InnerText;

			if (manifest_parsed_media.baseUrl.IndexOf ("rtmp") == 0 || manifest_parsed_media.url.IndexOf ("rtmp") == 0) {
				LogError ("Provided manifest is not a valid HDS manifest");
				return;
			}

			int idx = manifest_parsed_media.url.IndexOf ("?");
			if (idx > -1) {
				manifest_parsed_media.queryString = manifest_parsed_media.url.Substring (idx);
				manifest_parsed_media.url = manifest_parsed_media.url.Substring (0, idx);
			}

			XmlElement bootstrapInfoId = node ["bootstrapInfoId"];
			XmlNode bootstrap;
			if (bootstrapInfoId != null) {
				bootstrap = doc.SelectSingleNode ("/manifest/bootstrapInfo[@id='" + node.InnerText.ToLower () + "']");
			} else {
				bootstrap = doc.SelectSingleNode ("/manifest/bootstrapInfo");
			}

			if (bootstrap.Attributes ["url"] != null) {
				// download bootstrap
				LogError ("Invalid bootstrap (url bootstrap)");
			} else {
				manifest_parsed_media.bootstrap = Convert.FromBase64String (bootstrap.InnerText);
			}

			// Metadata
			if (node.FirstChild != null) {
				manifest_parsed_media.metadata = Convert.FromBase64String (node.FirstChild.InnerText);
			}

			manifest_parsed_media_list.Add (manifest_parsed_media);
		}

		// Manifest parsed.
		// Select best quality

		if (manifest_parsed_media_list.Count == 0) {
			LogError ("No media found.");
			return;
		}

		media = manifest_parsed_media_list [0];

		foreach (Manifest_parsed_media media_2 in manifest_parsed_media_list) {
			if (media.bitrate < media_2.bitrate) {
				media = media_2;
			}
			LogInfo ("Bitrate availabe: " + media_2.bitrate);
		}

		LogInfo ("Bitrate autoselected: " + media.bitrate);

		// Parse bootstrap info
		int pos = 0;
		long boxSize = 0;
		string boxType = "";
		ReadBoxHeader (media.bootstrap, ref pos, ref boxType, ref boxSize);

		if (boxType == "abst") {
			ParseBootstrapBox (media.bootstrap, pos);
		} else {
			LogError ("Failed to parse bootstrap info");
			return;
		}

		// Parse initial bootstrap info
		baseUrl = media.baseUrl;
		bootstrapInfo = media.bootstrap;
	}
	/*
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
	*/

	public void ParseBootstrapBox (byte[] bootstrapInfo, int pos)
	{
		byte version = bootstrapInfo [pos];
		long flags = ReadInt24 (bootstrapInfo, pos + 1);
		long bootstrapVersion = ReadInt32 (bootstrapInfo, pos + 4);
		byte bbyte = bootstrapInfo [pos + 8];
		int profile = (bbyte & 0xC0) >> 6;
		if ((bbyte & 0x20) >> 5 != 0) {
			LogError ("Live is not supported.");
			return;
		}
		segTable = new SegTable_content ();
		fragTable = new Dictionary<int,Frag_table_content> ();


		int timescale = ReadInt32 (bootstrapInfo, pos + 9);
		long currentMediaTime = ReadInt64 (bootstrapInfo, pos + 13);
		long smpteTimeCodeOffset = ReadInt64 (bootstrapInfo, pos + 21);
		pos += 29;
		string movieIdentifier = ReadString (bootstrapInfo, ref pos);
		byte serverEntryCount = bootstrapInfo [pos++];
		for (int i = 0; i < serverEntryCount; i++) {
			ReadString (bootstrapInfo, ref pos);
		}
		byte qualityEntryCount = bootstrapInfo [pos++];
		for (int i = 0; i < qualityEntryCount; i++) {
			LogDebug (ReadString (bootstrapInfo, ref pos));
		}
		string drmData = ReadString (bootstrapInfo, ref pos);
		string metadata = ReadString (bootstrapInfo, ref pos);
		byte segRunTableCount = bootstrapInfo [pos++];
		LogDebug ("Segment Tables:");
		for (int i = 0; i < segRunTableCount; i++) {
			LogDebug ("\nTable " + (i + 1) + ":");
			long boxSize = 0;
			string boxType = null;
			ReadBoxHeader (bootstrapInfo, ref pos, ref boxType, ref boxSize);
			if (boxType == "asrt")
				ParseAsrtBox (segTable, bootstrapInfo, pos);
			pos += (int)boxSize;
		}
		byte fragRunTableCount = bootstrapInfo [pos++];
		LogDebug ("Fragment Tables:");
		for (int i = 0; i < fragRunTableCount; i++) {
			LogDebug ("\nTable " + (i + 1) + ":");
			long boxSize = 0;
			string boxType = null;
			ReadBoxHeader (bootstrapInfo, ref pos, ref boxType, ref boxSize);
			if (boxType == "afrt")
				ParseAfrtBox (bootstrapInfo, pos);
			pos += (int)boxSize;
		}
		ParseSegAndFragTable ();
	}


	public void ParseAsrtBox (SegTable_content segTable, byte[] asrt, int pos)
	{
		byte version = asrt [pos];
		int flags = ReadInt24 (asrt, pos + 1);
		byte qualityEntryCount = asrt [pos + 4];
		pos += 5;
		for (int i = 0; i < qualityEntryCount; i++)
			ReadString (asrt, ref pos);
		int segCount = ReadInt32 (asrt, pos);
		pos += 4;
		LogDebug ("Number - Fragments");
		for (int i = 0; i < segCount; i++) {
			int firstSegment = ReadInt32 (asrt, pos);
			segTable = new SegTable_content (firstSegment, ReadInt32 (asrt, pos + 4));
			if ((segTable.fragmentsPerSegment & 0x80000000) != 0)
				segTable.fragmentsPerSegment = 0;
			pos += 8;
		}

		LogDebug (segTable.firstSegment + " - " + segTable.fragmentsPerSegment);

		LogDebug ("");
	}

	public void ParseAfrtBox (byte[] afrt, int pos)
	{
		byte version = afrt [pos];
		long flags = ReadInt24 (afrt, pos + 1);
		long timescale = ReadInt32 (afrt, pos + 4);
		byte qualityEntryCount = afrt [pos + 8];
		pos += 9;
		for (int i = 0; i < qualityEntryCount; i++) {
			ReadString (afrt, ref pos);
		}
		long fragEntries = ReadInt32 (afrt, pos);
		pos += 4;
		LogDebug ("Number - Timestamp - Duration - Discontinuity");
		for (int i = 0; i < fragEntries; i++) {
			int firstFragment = ReadInt32 (afrt, pos);
			fragTable [firstFragment] = new Frag_table_content (firstFragment, ReadInt64 (afrt, pos + 4), ReadInt32 (afrt, pos + 12), new byte ());
			pos += 16;
			if (fragTable [firstFragment].fragmentDuration == 0)
				fragTable [firstFragment].discontinuityIndicator = afrt [pos++];
		}
		foreach (KeyValuePair<int, Frag_table_content> fragEntry in fragTable) {
			LogDebug (fragEntry.Value.firstFragment + " - " + fragEntry.Value.firstFragmentTimestamp + " - " + fragEntry.Value.fragmentDuration + " - " + fragEntry.Value.discontinuityIndicator);
		}
		LogDebug ("");
	}

	public void ParseSegAndFragTable ()
	{
		// Count total fragments by adding all entries in compactly coded segment table
		fragCount = segTable.fragmentsPerSegment;

		if ((fragCount & 0x80000000) == 0) {
			fragCount += fragTable [1].firstFragment - 1;
		}

		if (fragCount < fragTable [fragTable.Count].firstFragment)
			fragCount = fragTable [fragTable.Count].firstFragment;

		// Determine starting segment and fragment
		segStart = segTable.firstSegment;
		if (segStart < 1)
			segStart = 1;

		fragStart = fragTable [1].firstFragment - 1;
		if (fragStart < 0)
			fragStart = 0;
	}

	public void DownloadFragments (string manifest)
	{
		int start = 0;

		ParseManifest (manifest);

		int segNum = segStart;
		int fragNum = fragStart;

		lastFrag = fragNum;
		LogInfo ("Fragments Total: " + fragCount + ", First: " + fragTable [1].firstFragment + ", Start: " + (fragNum + 1) + ", Parallel: " + parallel);

		// Extract baseFilename
		baseFilename = media.url;
		if (baseFilename.Substring (-1) == "/")
			baseFilename = baseFilename.Substring (0, -1);
		baseFilename = RemoveExtension (baseFilename);
		int lastSlash = baseFilename.LastIndexOf ("/");
		if (lastSlash != -1)
			baseFilename = baseFilename.Substring (lastSlash + 1);
		if (manifest.IndexOf ("?") != -1)
			baseFilename = CalculateMD5Hash (manifest.Substring (0, manifest.IndexOf ("?"))) + "_" + baseFilename;
		else
			baseFilename = CalculateMD5Hash (manifest) + "_" + baseFilename;
		baseFilename += "Seg" + segNum + "-Frag";

		if (fragNum >= fragCount)
			LogError ("No fragment available for downloading");

		string fragUrl = AbsoluteUrl (baseUrl, media.url);
		LogDebug ("Base Fragment Url:\n" + fragUrl + "\n");
		LogDebug ("Downloading Fragments:\n");

		bool w1 = true, w2 = true;
		int downloads_in_process = 0;
		int frag_table_i = 1;
		while ((fragNum < fragCount) && w1) {
			w2 = true;
			Frag_response frag = new Frag_response ();
			fragNum = fragNum++;
			frag.id = fragNum;
			LogInfo ("Downloading fragNum/this.fragCount fragments");
			/*
			if (in_array_field (fragNum, fragTable)) {
				if (value_in_array_field (fragNum, fragTable)) {
					discontinuity = fragNum;
				}
			}
			else
			{
				int closest = fragTable[1].firstFragment;
				int i = 2;
				while (i < fragTable.Count)
				{
					if (fragTable[i].firstFragment < fragNum)
						closest = fragTable[i].firstFragment;
					else
						break;
					i++;
				}
				discontinuity = fragTable [closest].discontinuityIndicator;
			}
			if (discontinuity != 0)
			{
				LogDebug("Skipping fragment fragNum due to discontinuity, Type: " + discontinuity);
				frag["response"] = false;
				rename = true;
			}
			else */
			frag.filename = baseFilename + fragNum;
			if (File.Exists (frag.filename)) {
				LogDebug ("Fragment fragNum is already downloaded");
				frag.response = file_get_contents (frag.filename);
			} else {
				LogDebug ("Downloading fragment fragNum");
				segNum = segTable.firstSegment;
				new WebClient ().DownloadFile (fragUrl + "Seg" + segNum + "-Frag" + fragNum + media.queryString, frag.filename);
				frag.response = file_get_contents (frag.filename);
			}
			if (frag.response.Length != 0) {
				if (WriteFragment (frag) == STOP_PROCESSING) {
					break;
				}
			}

			if (!w1) {
				break;
			}

			if (VerifyFragment (frag.response)) {
				LogDebug ("Fragment " + baseFilename + frag.id + " successfully downloaded");
				file_put_contents (baseFilename + frag.id, frag.response);
			} else {
				fragNum--;
				File.Delete (frag.filename);
				LogDebug ("Fragment " + frag.id + " failed to verify");
			}
			if (frag.response) {
				if (WriteFragment (frag) == STOP_PROCESSING) {
					break;
				}
			}
		}

		LogInfo ("");
		LogDebug ("\nAll fragments downloaded successfully\n");
		processed = true;
	}
	/*
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
*/

	public int WriteFragment (Frag_response download)
	{
		frags [download.id] = download;

		int available = frags.Count;
		for (int i = 0; i < available; i++) {
			if (frags.ContainsKey (lastFrag + 1)) {
				Frag_response frag = frags [lastFrag + 1];
				if (frag.response.Length = 0) {
					LogDebug ("Writing fragment " + frag ["id"] + " to flv file");
					if (!opt.ContainsKey ("file")) {
						string outFile = "";
						if (outFileGlobal != "") {
							outFile = JoinUrl (outDir, outFile + ".flv");
						} else {
							outFile = JoinUrl (outDir, baseFilename + ".flv");
						}
						InitDecoder ();
						DecodeFragment (frag ["response"], frag ["id"]);
						opt ["file"] = WriteFlvFile (outFile, audio, video);
						if (metadata)
							WriteMetadata (opt ["file"]);

						InitDecoder ();
					}
					flvData = DecodeFragment (frag ["response"], frag ["id"]);
					if (strlen (flvData)) {
						status = fwrite (opt ["file"], flvData, strlen (flvData));
						if (!status)
							LogError ("Failed to write flv data");
						filesize = ftell (opt ["file"]) / (1024 * 1024);
					}
					lastFrag = frag ["id"];
				} else {
					lastFrag += 1;
					LogDebug ("Skipping failed fragment " + lastFrag);
				}
				unset (frags [lastFrag]);
			} else
				break;

			if (opt ["tDuration"] && ((opt ["duration"] + duration) >= opt ["tDuration"])) {
				LogInfo ("");
				LogInfo ((opt ["duration"] + this.duration) + " seconds of content has been recorded successfully.", true);
				return STOP_PROCESSING;
			}
		}

		if (this.frags.Count == 0)
			unset (this.frags);
		return true;
	}
}

public class Manifest_parsed_media
{
	public int bitrate;

	public string baseUrl;
	public string url;
	public string queryString;
	public byte[] bootstrap;
	public byte[] metadata;

	public Manifest_parsed_media ()
	{
	}
}

public class Frag_table_content
{
	public int firstFragment;
	public long firstFragmentTimestamp;
	public int fragmentDuration;
	public byte discontinuityIndicator;

	public Frag_table_content ()
	{
	}

	public Frag_table_content (int firstFragment, long firstFragmentTimestamp, int fragmentDuration, byte discontinuityIndicator)
	{
		this.firstFragment = firstFragment;
		this.firstFragmentTimestamp = firstFragmentTimestamp;
		this.fragmentDuration = fragmentDuration;
		this.discontinuityIndicator = discontinuityIndicator;
	}
}

public class SegTable_content
{
	public int firstSegment;
	public int fragmentsPerSegment;

	public SegTable_content ()
	{
	}

	public SegTable_content (int firstSegment, int fragmentsPerSegment)
	{
		this.firstSegment = firstSegment;
		this.fragmentsPerSegment = fragmentsPerSegment;
	}
}

public class Frag_response
{
	public byte[] response;
	public int id;
	public string filename;

	public Frag_response ()
	{
	}
}