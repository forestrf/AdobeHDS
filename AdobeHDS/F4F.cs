using System;
using System.Collections.Generic;
using System.Xml;
using System.Net;
using System.Diagnostics;
using System.IO;


public class F4F : Functions
{
	public string baseUrl, baseFilename;
	public byte[] bootstrapInfo;
	public Manifest_parsed_media media;
	public SegTable_content segTable;
	public Dictionary<int,Frag_table_content> fragTable;
	public int fragCount = -1;

	public Dictionary<int, Frag_response> frags = new Dictionary<int, Frag_response>();

	public string outFileGlobal = "";

	public string outDir = "";

	public int lastFrag = 0;

	public bool processed = false;

	public FileStream file;










	public int discontinuity = 0;
	public int fixWindow = 1000;
	public int prevAudioTS = INVALID_TIMESTAMP;
	public int prevVideoTS = INVALID_TIMESTAMP;
	public int baseTS = INVALID_TIMESTAMP;
	public int negTS = INVALID_TIMESTAMP;

	public bool audio = false;
	public bool video = false;
	public int prevTagSize = 4;
	public int tagHeaderLen = 11;
	public int pAudioTagLen = 0;
	public int pVideoTagLen = 0;
	public bool prevAVC_Header = false;
	public bool prevAAC_Header = false;
	public bool AVC_HeaderWritten = false;
	public bool AAC_HeaderWritten = false;

	public string auth = "";

	public F4F ()
	{
	}

	public void ParseManifest (string manifest)
	{
		LogInfo ("Processing manifest info....");

		XmlDocument doc = new XmlDocument ();
		try {
			string manifest_xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<manifest xmlns=\"http://ns.adobe.com/f4m/1.0\">\n\t<id>assets5/2014/07/25/59508586-1ACD-446B-9A90-2128427BC6A8/es.smil</id>\n\t<width>1280</width>\n\t<height>720</height>\n\t<duration>853.88</duration>\n\t<mimeType>video/mp4</mimeType>\n\t<streamType>recorded</streamType>\n\t<deliveryType>streaming</deliveryType>\n\t<bootstrapInfo profile=\"named\" id=\"bootstrap1\">AAAFvmFic3QBAAAAAAAAAQAAAAPoAAAAAAANB3gAAAAAAAAAAAABAAEAAAABAAAAGmFzcnQBAAAAAQAAAAABAAAAAQAAAFYBAAAFdmFmcnQBAAAAAAAD6AEAAAAAVgAAAAEAAAAAAAAAAAAAJxAAAAACAAAAAAAAJxAAACcQAAAAAwAAAAAAAE4gAAAnEAAAAAQAAAAAAAB1MAAAJxAAAAAFAAAAAAAAnEAAACcQAAAABgAAAAAAAMNQAAAnEAAAAAcAAAAAAADqYAAAJxAAAAAIAAAAAAABEXAAACcQAAAACQAAAAAAATiAAAAnEAAAAAoAAAAAAAFfkAAAJxAAAAALAAAAAAABhqAAACcQAAAADAAAAAAAAa2wAAAnEAAAAA0AAAAAAAHUwAAAJxAAAAAOAAAAAAAB+9AAACcQAAAADwAAAAAAAiLgAAAnEAAAABAAAAAAAAJJ8AAAJxAAAAARAAAAAAACcQAAACcQAAAAEgAAAAAAApgQAAAnEAAAABMAAAAAAAK/IAAAJxAAAAAUAAAAAAAC5jAAACcQAAAAFQAAAAAAAw1AAAAnEAAAABYAAAAAAAM0UAAAJxAAAAAXAAAAAAADW2AAACcQAAAAGAAAAAAAA4JwAAAnEAAAABkAAAAAAAOpgAAAJxAAAAAaAAAAAAAD0JAAACcQAAAAGwAAAAAAA/egAAAnEAAAABwAAAAAAAQesAAAJxAAAAAdAAAAAAAERcAAACcQAAAAHgAAAAAABGzQAAAnEAAAAB8AAAAAAAST4AAAJxAAAAAgAAAAAAAEuvAAACcQAAAAIQAAAAAABOIAAAAnEAAAACIAAAAAAAUJEAAAJxAAAAAjAAAAAAAFMCAAACcQAAAAJAAAAAAABVcwAAAnEAAAACUAAAAAAAV+QAAAJxAAAAAmAAAAAAAFpVAAACcQAAAAJwAAAAAABcxgAAAnEAAAACgAAAAAAAXzcAAAJxAAAAApAAAAAAAGGoAAACcQAAAAKgAAAAAABkGQAAAnEAAAACsAAAAAAAZooAAAJxAAAAAsAAAAAAAGj7AAACcQAAAALQAAAAAABrbAAAAnEAAAAC4AAAAAAAbd0AAAJxAAAAAvAAAAAAAHBOAAACcQAAAAMAAAAAAAByvwAAAnEAAAADEAAAAAAAdTAAAAJxAAAAAyAAAAAAAHehAAACcQAAAAMwAAAAAAB6EgAAAnEAAAADQAAAAAAAfIMAAAJxAAAAA1AAAAAAAH70AAACcQAAAANgAAAAAACBZQAAAnEAAAADcAAAAAAAg9YAAAJxAAAAA4AAAAAAAIZHAAACcQAAAAOQAAAAAACIuAAAAnEAAAADoAAAAAAAiykAAAJxAAAAA7AAAAAAAI2aAAACcQAAAAPAAAAAAACQCwAAAnEAAAAD0AAAAAAAknwAAAJxAAAAA+AAAAAAAJTtAAACcQAAAAPwAAAAAACXXgAAAnEAAAAEAAAAAAAAmc8AAAJxAAAABBAAAAAAAJxAAAACcQAAAAQgAAAAAACesQAAAnEAAAAEMAAAAAAAoSIAAAJxAAAABEAAAAAAAKOTAAACcQAAAARQAAAAAACmBAAAAnEAAAAEYAAAAAAAqHUAAAJxAAAABHAAAAAAAKrmAAACcQAAAASAAAAAAACtVwAAAnEAAAAEkAAAAAAAr8gAAAJxAAAABKAAAAAAALI5AAACcQAAAASwAAAAAAC0qgAAAnEAAAAEwAAAAAAAtxsAAAJxAAAABNAAAAAAALmMAAACcQAAAATgAAAAAAC7/QAAAnEAAAAE8AAAAAAAvm4AAAJxAAAABQAAAAAAAMDfAAACcQAAAAUQAAAAAADDUAAAAnEAAAAFIAAAAAAAxcEAAAJxAAAABTAAAAAAAMgyAAACbAAAAAVAAAAAAADKngAAAnEAAAAFUAAAAAAAzQ8AAAJxAAAABWAAAAAAAM+AAAAA94</bootstrapInfo>\n\t<media streamId=\"1\" bootstrapInfoId=\"bootstrap1\" width=\"640\" height=\"360\" bitrate=\"707\" url=\"media_b724000.abst/\">\n\t\t<metadata>AgAKb25NZXRhRGF0YQgAAAAAAAl0cmFja2luZm8KAAAAAgMACGxhbmd1YWdlAgADdW5kAAl0aW1lc2NhbGUAQPX5AAAAAAAABmxlbmd0aABBklKAwAAAAAARc2FtcGxlZGVzY3JpcHRpb24KAAAAAQMACnNhbXBsZXR5cGUCAARhdmMxAAAJAAAJAwAIbGFuZ3VhZ2UCAAN1bmQACXRpbWVzY2FsZQBA53AAAAAAAAAGbGVuZ3RoAEGDinwAAAAAABFzYW1wbGVkZXNjcmlwdGlvbgoAAAABAwAKc2FtcGxldHlwZQIABG1wNGEAAAkAAAkADWF1ZGlvY2hhbm5lbHMAQAAAAAAAAAAAD2F1ZGlvc2FtcGxlcmF0ZQBA53AAAAAAAAAOdmlkZW9mcmFtZXJhdGUAQDkAAAAAAAAABmFhY2FvdABAAAAAAAAAAAAIYXZjbGV2ZWwAQD4AAAAAAAAACmF2Y3Byb2ZpbGUAQFNAAAAAAAAADGF1ZGlvY29kZWNpZAIABG1wNGEADHZpZGVvY29kZWNpZAIABGF2YzEABXdpZHRoAECEAAAAAAAAAAZoZWlnaHQAQHaAAAAAAAAACmZyYW1lV2lkdGgAQIQAAAAAAAAAC2ZyYW1lSGVpZ2h0AEB2gAAAAAAAAAxkaXNwbGF5V2lkdGgAQIQAAAAAAAAADWRpc3BsYXlIZWlnaHQAQHaAAAAAAAAACWZyYW1lcmF0ZQBAOQAAAAAAAAAMbW9vdnBvc2l0aW9uAEBEAAAAAAAAAAhkdXJhdGlvbgBAiq8KPXCj1wAACQ==</metadata>\n\t</media>\n\t<media streamId=\"2\" bootstrapInfoId=\"bootstrap1\" width=\"640\" height=\"360\" bitrate=\"1029\" url=\"media_b1054000.abst/\">\n\t\t<metadata>AgAKb25NZXRhRGF0YQgAAAAAAAl0cmFja2luZm8KAAAAAgMACGxhbmd1YWdlAgADdW5kAAl0aW1lc2NhbGUAQPX5AAAAAAAABmxlbmd0aABBklKAwAAAAAARc2FtcGxlZGVzY3JpcHRpb24KAAAAAQMACnNhbXBsZXR5cGUCAARhdmMxAAAJAAAJAwAIbGFuZ3VhZ2UCAAN1bmQACXRpbWVzY2FsZQBA53AAAAAAAAAGbGVuZ3RoAEGDinwAAAAAABFzYW1wbGVkZXNjcmlwdGlvbgoAAAABAwAKc2FtcGxldHlwZQIABG1wNGEAAAkAAAkADWF1ZGlvY2hhbm5lbHMAQAAAAAAAAAAAD2F1ZGlvc2FtcGxlcmF0ZQBA53AAAAAAAAAOdmlkZW9mcmFtZXJhdGUAQDkAAAAAAAAABmFhY2FvdABAAAAAAAAAAAAIYXZjbGV2ZWwAQD4AAAAAAAAACmF2Y3Byb2ZpbGUAQFNAAAAAAAAADGF1ZGlvY29kZWNpZAIABG1wNGEADHZpZGVvY29kZWNpZAIABGF2YzEABXdpZHRoAECEAAAAAAAAAAZoZWlnaHQAQHaAAAAAAAAACmZyYW1lV2lkdGgAQIQAAAAAAAAAC2ZyYW1lSGVpZ2h0AEB2gAAAAAAAAAxkaXNwbGF5V2lkdGgAQIQAAAAAAAAADWRpc3BsYXlIZWlnaHQAQHaAAAAAAAAACWZyYW1lcmF0ZQBAOQAAAAAAAAAMbW9vdnBvc2l0aW9uAEBEAAAAAAAAAAhkdXJhdGlvbgBAiq8KPXCj1wAACQ==</metadata>\n\t</media>\n\t<media streamId=\"3\" bootstrapInfoId=\"bootstrap1\" width=\"720\" height=\"404\" bitrate=\"1458\" url=\"media_b1494000.abst/\">\n\t\t<metadata>AgAKb25NZXRhRGF0YQgAAAAAAAl0cmFja2luZm8KAAAAAgMACGxhbmd1YWdlAgADdW5kAAl0aW1lc2NhbGUAQPX5AAAAAAAABmxlbmd0aABBklKAwAAAAAARc2FtcGxlZGVzY3JpcHRpb24KAAAAAQMACnNhbXBsZXR5cGUCAARhdmMxAAAJAAAJAwAIbGFuZ3VhZ2UCAAN1bmQACXRpbWVzY2FsZQBA53AAAAAAAAAGbGVuZ3RoAEGDinwAAAAAABFzYW1wbGVkZXNjcmlwdGlvbgoAAAABAwAKc2FtcGxldHlwZQIABG1wNGEAAAkAAAkADWF1ZGlvY2hhbm5lbHMAQAAAAAAAAAAAD2F1ZGlvc2FtcGxlcmF0ZQBA53AAAAAAAAAOdmlkZW9mcmFtZXJhdGUAQDkAAAAAAAAABmFhY2FvdABAAAAAAAAAAAAIYXZjbGV2ZWwAQD4AAAAAAAAACmF2Y3Byb2ZpbGUAQFNAAAAAAAAADGF1ZGlvY29kZWNpZAIABG1wNGEADHZpZGVvY29kZWNpZAIABGF2YzEABXdpZHRoAECGaAAAAAAAAAZoZWlnaHQAQHlAAAAAAAAACmZyYW1lV2lkdGgAQIaAAAAAAAAAC2ZyYW1lSGVpZ2h0AEB5QAAAAAAAAAxkaXNwbGF5V2lkdGgAQIZoAAAAAAAADWRpc3BsYXlIZWlnaHQAQHlAAAAAAAAACWZyYW1lcmF0ZQBAOQAAAAAAAAAMbW9vdnBvc2l0aW9uAEBEAAAAAAAAAAhkdXJhdGlvbgBAiq8KPXCj1wAACQ==</metadata>\n\t</media>\n\t<bootstrapInfo profile=\"named\" id=\"bootstrap2\">AAAFvmFic3QBAAAAAAAAAQAAAAPoAAAAAAANBygAAAAAAAAAAAABAAEAAAABAAAAGmFzcnQBAAAAAQAAAAABAAAAAQAAAFYBAAAFdmFmcnQBAAAAAAAD6AEAAAAAVgAAAAEAAAAAAAAAAAAAJxAAAAACAAAAAAAAJxAAACcQAAAAAwAAAAAAAE4gAAAnEAAAAAQAAAAAAAB1MAAAJxAAAAAFAAAAAAAAnEAAACcQAAAABgAAAAAAAMNQAAAnEAAAAAcAAAAAAADqYAAAJxAAAAAIAAAAAAABEXAAACcQAAAACQAAAAAAATiAAAAnEAAAAAoAAAAAAAFfkAAAJxAAAAALAAAAAAABhqAAACcQAAAADAAAAAAAAa2wAAAnEAAAAA0AAAAAAAHUwAAAJxAAAAAOAAAAAAAB+9AAACcQAAAADwAAAAAAAiLgAAAnEAAAABAAAAAAAAJJ8AAAJxAAAAARAAAAAAACcQAAACcQAAAAEgAAAAAAApgQAAAnEAAAABMAAAAAAAK/IAAAJxAAAAAUAAAAAAAC5jAAACcQAAAAFQAAAAAAAw1AAAAnEAAAABYAAAAAAAM0UAAAJxAAAAAXAAAAAAADW2AAACcQAAAAGAAAAAAAA4JwAAAnEAAAABkAAAAAAAOpgAAAJxAAAAAaAAAAAAAD0JAAACcQAAAAGwAAAAAAA/egAAAnEAAAABwAAAAAAAQesAAAJxAAAAAdAAAAAAAERcAAACcQAAAAHgAAAAAABGzQAAAnEAAAAB8AAAAAAAST4AAAJxAAAAAgAAAAAAAEuvAAACcQAAAAIQAAAAAABOIAAAAnEAAAACIAAAAAAAUJEAAAJxAAAAAjAAAAAAAFMCAAACcQAAAAJAAAAAAABVcwAAAnEAAAACUAAAAAAAV+QAAAJxAAAAAmAAAAAAAFpVAAACcQAAAAJwAAAAAABcxgAAAnEAAAACgAAAAAAAXzcAAAJxAAAAApAAAAAAAGGoAAACcQAAAAKgAAAAAABkGQAAAnEAAAACsAAAAAAAZooAAAJxAAAAAsAAAAAAAGj7AAACcQAAAALQAAAAAABrbAAAAnEAAAAC4AAAAAAAbd0AAAJxAAAAAvAAAAAAAHBOAAACcQAAAAMAAAAAAAByvwAAAnEAAAADEAAAAAAAdTAAAAJxAAAAAyAAAAAAAHehAAACcQAAAAMwAAAAAAB6EgAAAnEAAAADQAAAAAAAfIMAAAJxAAAAA1AAAAAAAH70AAACcQAAAANgAAAAAACBZQAAAnEAAAADcAAAAAAAg9YAAAJxAAAAA4AAAAAAAIZHAAACcQAAAAOQAAAAAACIuAAAAnEAAAADoAAAAAAAiykAAAJxAAAAA7AAAAAAAI2aAAACcQAAAAPAAAAAAACQCwAAAnEAAAAD0AAAAAAAknwAAAJxAAAAA+AAAAAAAJTtAAACcQAAAAPwAAAAAACXXgAAAnEAAAAEAAAAAAAAmc8AAAJxAAAABBAAAAAAAJxAAAACcQAAAAQgAAAAAACesQAAAnEAAAAEMAAAAAAAoSIAAAJxAAAABEAAAAAAAKOTAAACcQAAAARQAAAAAACmBAAAAnEAAAAEYAAAAAAAqHUAAAJxAAAABHAAAAAAAKrmAAACcQAAAASAAAAAAACtVwAAAnEAAAAEkAAAAAAAr8gAAAJxAAAABKAAAAAAALI5AAACcQAAAASwAAAAAAC0qgAAAnEAAAAEwAAAAAAAtxsAAAJxAAAABNAAAAAAALmMAAACcQAAAATgAAAAAAC7/QAAAnEAAAAE8AAAAAAAvm4AAAJxAAAABQAAAAAAAMDfAAACcQAAAAUQAAAAAADDUAAAAnEAAAAFIAAAAAAAxcEAAAJxAAAABTAAAAAAAMgyAAACbAAAAAVAAAAAAADKngAAAnEAAAAFUAAAAAAAzQ8AAAJxAAAABWAAAAAAAM+AAAAA8o</bootstrapInfo>\n\t<media streamId=\"4\" bootstrapInfoId=\"bootstrap2\" width=\"1280\" height=\"720\" bitrate=\"1996\" url=\"media_b2044000.abst/\">\n\t\t<metadata>AgAKb25NZXRhRGF0YQgAAAAAAAl0cmFja2luZm8KAAAAAgMACGxhbmd1YWdlAgADdW5kAAl0aW1lc2NhbGUAQPX5AAAAAAAABmxlbmd0aABBklIQQAAAAAARc2FtcGxlZGVzY3JpcHRpb24KAAAAAQMACnNhbXBsZXR5cGUCAARhdmMxAAAJAAAJAwAIbGFuZ3VhZ2UCAAN1bmQACXRpbWVzY2FsZQBA53AAAAAAAAAGbGVuZ3RoAEGDihwAAAAAABFzYW1wbGVkZXNjcmlwdGlvbgoAAAABAwAKc2FtcGxldHlwZQIABG1wNGEAAAkAAAkADWF1ZGlvY2hhbm5lbHMAQAAAAAAAAAAAD2F1ZGlvc2FtcGxlcmF0ZQBA53AAAAAAAAAOdmlkZW9mcmFtZXJhdGUAQDkAAAAAAAAABmFhY2FvdABAAAAAAAAAAAAIYXZjbGV2ZWwAQD8AAAAAAAAACmF2Y3Byb2ZpbGUAQFNAAAAAAAAADGF1ZGlvY29kZWNpZAIABG1wNGEADHZpZGVvY29kZWNpZAIABGF2YzEABXdpZHRoAECUAAAAAAAAAAZoZWlnaHQAQIaAAAAAAAAACmZyYW1lV2lkdGgAQJQAAAAAAAAAC2ZyYW1lSGVpZ2h0AECGgAAAAAAAAAxkaXNwbGF5V2lkdGgAQJQAAAAAAAAADWRpc3BsYXlIZWlnaHQAQIaAAAAAAAAACWZyYW1lcmF0ZQBAOQAAAAAAAAAMbW9vdnBvc2l0aW9uAEBEAAAAAAAAAAhkdXJhdGlvbgBAiq5mZmZmZgAACQ==</metadata>\n\t</media>\n</manifest>\n";
			//string manifest_xml = new WebClient().DownloadString(manifest);

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
			LogInfo ("Bitrate availabe: " + media_2.bitrate+", url: "+media_2.url);
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
		if (segTable.firstSegment < 1)
			segTable.firstSegment = 1;

		int fragStart = fragTable [1].firstFragment - 1;
		if (fragStart < 0)
			segTable.firstSegment = 1;
	}

	public void DownloadFragments (string manifest)
	{
		ParseManifest (manifest);

		int segNum = segTable.firstSegment;
		int fragNum = fragTable [1].firstFragment;

		lastFrag = fragNum;
		LogInfo ("Fragments Total: " + fragCount + ", First: " + fragTable [1].firstFragment + ", Start: " + (fragNum + 1));

		// Extract baseFilename
		baseFilename = media.url;
		if (baseFilename[baseFilename.Length -1] == '/')
			baseFilename = baseFilename.Substring (0, baseFilename.Length -2);
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

		bool w2 = true;
		while (fragNum <= fragCount) {
			w2 = true;
			Frag_response frag = new Frag_response ();
			fragNum = fragNum++;
			frag.id = fragNum;
			LogInfo ("Downloading " + fragNum + "/" + fragCount + " fragments");
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
				string url = fragUrl + "Seg" + segNum + "-Frag" + fragNum + media.queryString;
				LogDebug("Frag to download: "+url);
				new WebClient ().DownloadFile (url, frag.filename);
				frag.response = file_get_contents (frag.filename);
			}
			if (VerifyFragment (frag.response)) {
				LogDebug ("Fragment " + frag.filename + " successfully downloaded");
				if (frag.response.Length != 0) {
					WriteFragment (frag);
					frag.response = new byte[0];
					frags.Add (fragNum, frag);
					fragNum++;
				} else {
					fragNum--;
					File.Delete (frag.filename);
					LogDebug ("Fragment " + frag.id + " bad downloaded");
				}
			} else {
				fragNum--;
				File.Delete (frag.filename);
				LogDebug ("Fragment " + frag.id + " failed to verify");
			}
		}

		LogInfo ("");
		LogDebug ("\nAll fragments downloaded successfully\n");
		processed = true;
	}

	public bool VerifyFragment (byte[] frag)
	{
		int fragPos = 0;
		int fragLen = frag.Length;

		//Some moronic servers add wrong boxSize in header causing fragment verification to fail so we have to fix the boxSize before processing the fragment.          
		while (fragPos < fragLen) {
			string boxType = "";
			long boxSize = 0;
			ReadBoxHeader (frag, ref fragPos, ref boxType, ref boxSize);
			if (boxType == "mdat") {
				long len = boxSize - fragPos;
				if (boxSize != 0 && len == boxSize)
					return true;
				else {
					boxSize = fragLen - fragPos;
					WriteBoxSize (frag, fragPos, boxType, boxSize);
					return true;
				}
			}
			fragPos += (int)boxSize;
		}
		return false;
	}

	public byte[] DecodeFragment (byte[] frag, int fragNum)
	{
		byte[] flvData = new byte[0];
		int fragPos = 0;
		int packetTS = 0;
		long fragLen = frag.LongLength;

		if (!VerifyFragment (frag)) {
			LogInfo ("Skipping fragment number " + fragNum);
			return new byte[0];
		}

		while (fragPos < fragLen) {
			long boxSize = 0;
			string boxType = "";
			ReadBoxHeader (frag, ref fragPos, ref boxType, ref boxSize);
			if (boxType == "mdat") {
				fragLen = fragPos + boxSize;
				break;
			}
			fragPos += (int)boxSize;
		}

		LogDebug ("\nFragment " + fragNum + ":\nType - CurrentTS - PreviousTS - Size - Position");
		while (fragPos < fragLen) {
			byte packetType = frag [fragPos];
			int packetSize = ReadInt24 (frag, fragPos + 1);
			packetTS = ReadInt24 (frag, fragPos + 4);
			packetTS = packetTS | (frag [fragPos + 7] << 24);
			if ((packetTS & 0x80000000) != 0)
				packetTS &= 0x7FFFFFFF;
			long totalTagLen = tagHeaderLen + packetSize + prevTagSize;

			// Try to fix the odd timestamps and make them zero based
			int currentTS = packetTS;
			int lastTS = prevVideoTS >= prevAudioTS ? prevVideoTS : prevAudioTS;
			int fixedTS = lastTS + FRAMEFIX_STEP;
			if (baseTS == INVALID_TIMESTAMP && (packetType == AUDIO || packetType == VIDEO))
				baseTS = packetTS;
			if (baseTS > 1000 && packetTS >= baseTS)
				packetTS -= baseTS;
			if (lastTS != INVALID_TIMESTAMP) {
				int timeShift = packetTS - lastTS;
				if (timeShift > fixWindow) {
					LogDebug ("Timestamp gap detected: PacketTS=" + packetTS + " LastTS=" + lastTS + " Timeshift=" + timeShift);
					if (baseTS < packetTS)
						baseTS += timeShift - FRAMEFIX_STEP;
					else
						baseTS = timeShift - FRAMEFIX_STEP;
					packetTS = fixedTS;
				} else {
					lastTS = packetType == VIDEO ? prevVideoTS : prevAudioTS;
					if (packetTS < (lastTS - fixWindow)) {
						if ((negTS != INVALID_TIMESTAMP) && ((packetTS + negTS) < (lastTS - fixWindow)))
							negTS = INVALID_TIMESTAMP;
						if (negTS == INVALID_TIMESTAMP) {
							negTS = fixedTS - packetTS;
							LogDebug ("Negative timestamp detected: PacketTS=" + packetTS + " LastTS=" + lastTS + " NegativeTS=" + negTS);
							packetTS = fixedTS;
						} else {
							if ((packetTS + negTS) <= (lastTS + fixWindow))
								packetTS += negTS;
							else {
								negTS = fixedTS - packetTS;
								LogDebug ("Negative timestamp override: PacketTS=" + packetTS + " LastTS=" + lastTS + " NegativeTS=" + negTS);
								packetTS = fixedTS;
							}
						}
					}
				}
			}
			if (packetTS != currentTS)
				WriteFlvTimestamp (frag, fragPos, packetTS);

			switch (ReadInt32 (new byte[]{ packetType, new byte (), new byte (), new byte () }, 0)) {
			case AUDIO:
				if (packetTS > prevAudioTS - fixWindow) {
					byte FrameInfo = frag [fragPos + tagHeaderLen];
					int CodecID = (FrameInfo & 0xF0) >> 4;
					byte AAC_PacketType = new byte();
					if (CodecID == CODEC_ID_AAC) {
						AAC_PacketType = frag [fragPos + tagHeaderLen + 1];
						if (AAC_PacketType == AAC_SEQUENCE_HEADER) {
							if (AAC_HeaderWritten) {
								LogDebug ("Skipping AAC sequence header\nformat Skipping AAC sequence header AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize + "\n");
								break;
							} else {
								LogDebug ("Writing AAC sequence header");
								AAC_HeaderWritten = true;
							}
						} else if (!AAC_HeaderWritten) {
							LogDebug ("Discarding audio packet received before AAC sequence header AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize + "\n");
							break;
						}
					}
					if (packetSize > 0) {
						// Check for packets with non-monotonic audio timestamps and fix them
						if (!(CodecID == CODEC_ID_AAC && (AAC_PacketType == AAC_SEQUENCE_HEADER || prevAAC_Header)))
						if ((prevAudioTS != INVALID_TIMESTAMP) && (packetTS <= prevAudioTS)) {
							LogDebug (" Fixing audio timestamp - AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize + "\n");
							packetTS += (FRAMEFIX_STEP / 5) + (prevAudioTS - packetTS);
							WriteFlvTimestamp (frag, fragPos, packetTS);
						}
						byte[] new_flvData = new byte[flvData.Length + totalTagLen];
						Buffer.BlockCopy (flvData, 0, new_flvData, 0, flvData.Length);
						Buffer.BlockCopy (frag, flvData.Length, new_flvData, fragPos, (int)totalTagLen);
						flvData = new_flvData;
						LogDebug ("AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize);
						
						if (CodecID == CODEC_ID_AAC && AAC_PacketType == AAC_SEQUENCE_HEADER)
							prevAAC_Header = true;
						else
							prevAAC_Header = false;
						prevAudioTS = packetTS;
						pAudioTagLen = (int)totalTagLen;
					} else
						LogDebug ("Skipping small sized audio packet - AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize);
				} else
					LogDebug ("Skipping audio packet in fragment " + fragNum + " - AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize);
				if (!audio)
					audio = true;
				break;
			case VIDEO:
				if (packetTS > prevVideoTS - fixWindow) {
					byte FrameInfo = frag [fragPos + tagHeaderLen];
					int FrameType = (FrameInfo & 0xF0) >> 4;
					int CodecID = FrameInfo & 0x0F;
					if (FrameType == FRAME_TYPE_INFO) {
						LogDebug ("Skipping video info frame - VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);
						break;
					}
					byte AVC_PacketType = new byte();
					if (CodecID == CODEC_ID_AVC) {
						AVC_PacketType = frag [fragPos + tagHeaderLen + 1];
						if (AVC_PacketType == AVC_SEQUENCE_HEADER) {
							if (AVC_HeaderWritten) {
								LogDebug ("Skipping AVC sequence header - VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);
								break;
							} else {
								LogDebug ("Writing AVC sequence header");
								AVC_HeaderWritten = true;
							}
						} else if (!AVC_HeaderWritten) {
							LogDebug ("Discarding video packet received before AVC sequence header - VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);
							break;
						}
					}
					if (packetSize > 0) {
						long pts = (int)packetTS;
						if (CodecID == CODEC_ID_AVC && AVC_PacketType == AVC_NALU) {
							long cts = ReadInt24 (frag, fragPos + tagHeaderLen + 2);
							cts = (cts + 0xff800000) ^ 0xff800000;
							pts = packetTS + cts;
							if (cts != 0)
								LogDebug ("DTS: $packetTS CTS: " + cts + " PTS: " + pts);
						}

						// Check for packets with non-monotonic video timestamps and fix them
						if (!(CodecID == CODEC_ID_AVC && (AVC_PacketType == AVC_SEQUENCE_HEADER || AVC_PacketType == AVC_SEQUENCE_END || prevAVC_Header)))
						if (prevVideoTS != INVALID_TIMESTAMP && packetTS <= prevVideoTS) {
							LogDebug ("Fixing video timestamp - VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);
							packetTS += (FRAMEFIX_STEP / 5) + (prevVideoTS - packetTS);
							WriteFlvTimestamp (frag, fragPos, packetTS);
						}
						
						byte[] new_flvData = new byte[flvData.Length + totalTagLen];
						Buffer.BlockCopy (flvData, 0, new_flvData, 0, flvData.Length);
						Buffer.BlockCopy (frag, flvData.Length, new_flvData, fragPos, (int)totalTagLen);
						flvData = new_flvData;
						LogDebug ("VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);

						if (CodecID == CODEC_ID_AVC && AVC_PacketType == AVC_SEQUENCE_HEADER)
							prevAVC_Header = true;
						else
							prevAVC_Header = false;
						prevVideoTS = packetTS;
						pVideoTagLen = (int)totalTagLen;
					} else
						LogDebug ("Skipping small sized video packet - VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);
				} else
					LogDebug ("Skipping video packet in fragment " + fragNum + " - VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);
				if (!video)
					video = true;
				break;
			case SCRIPT_DATA:
				break;
			default:
				if (packetType == 10 || packetType == 11)
					LogError ("This stream is encrypted with Akamai DRM. Decryption of such streams isn't currently possible with this script.");
				else if (packetType == 40 || packetType == 41)
					LogError ("This stream is encrypted with FlashAccess DRM. Decryption of such streams isn't currently possible with this script.");
				else {
					LogInfo ("Unknown packet type " + packetType + " encountered! Unable to process fragment " + fragNum);
					//break 2;
				}
			
				fragPos += (int)totalTagLen;
				break;
			}
		}
		return flvData;
	}

	public void WriteFragment (Frag_response frag)
	{
		if (frag.response.Length != 0) {
			LogDebug ("Writing fragment " + frag.id + " to flv file");
			if (file == null) {
				string outFile = "";
				if (outFileGlobal != "") {
					outFile = JoinUrl (outDir, outFileGlobal + ".flv");
				} else {
					outFile = JoinUrl (outDir, baseFilename + ".flv");
				}
				DecodeFragment (frag.response, frag.id);
				file = WriteFlvFile (outFile, audio, video);
				if (media.metadata.Length != 0)
					WriteMetadata (this, file);
			}
			byte[] flvData = DecodeFragment (frag.response, frag.id);
			if (flvData.Length != 0) {
				file.Write (flvData, 0, flvData.Length);
			}
			lastFrag = frag.id;
		} else {
			lastFrag += 1;
			LogDebug ("Skipping failed fragment " + lastFrag);
		}
		//unset (frags [lastFrag]);
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