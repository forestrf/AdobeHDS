using System;
using System.Collections.Generic;
using System.Xml;
using System.Net;
using System.IO;


public class F4F : Functions
{
	public int fragCount = -1,
		lastFrag = 0,
		fixWindow = 1000,
		prevAudioTS = INVALID_TIMESTAMP,
		prevVideoTS = INVALID_TIMESTAMP,
		baseTS = INVALID_TIMESTAMP,
		negTS = INVALID_TIMESTAMP,
		prevTagSize = 4,
		tagHeaderLen = 11;
	public string baseUrl,
		baseFilename,
		outFileGlobal = "",
		outDir = "",
		proxy = "",
		quality = "",
		auth = "",
		userAgent = "",
		referrer = "";
	public bool processed = false,
		audio = false,
		video = false,
		prevAVC_Header = false,
		prevAAC_Header = false,
		AVC_HeaderWritten = false,
		AAC_HeaderWritten = false,
		delete_fragments_at_end = false,
		manifest_parsed = false,
		fragments_proxy = false,
		error = false,
		play = false;
	public byte[] bootstrapInfo;
	public Manifest_parsed_media media;
	public SegTable_content segTable;
	public List<Frag_table_content> fragTable;
	public List<Frag_response> frags = new List<Frag_response> ();
	public Stream file;
	WebClient webClientFragments = new WebClient();



	public F4F ()
	{
	}

	public void ParseManifest (string manifest)
	{
		LogInfo ("Processing manifest info...");

		if (manifest == null) {
			LogInfo ("Manifest not specified");
			return;
		}

		XmlDocument doc = new XmlDocument ();
		try {
			WebClient webClientManifest = new WebClient();
			if(proxy.Length > 0){
				webClientManifest.Proxy = new WebProxy(proxy);
				LogDebug ("Proxy setted");
			}
			if(userAgent.Length > 0){
				webClientManifest.Headers.Add ("user-agent", userAgent);
			}
			if(referrer.Length > 0){
				webClientManifest.Headers.Add ("referer", referrer);
			}
			LogDebug ("Downloading manifest");
			string manifest_xml = webClientManifest.DownloadString(manifest);

			// Remove annoying manifests
			int nm_i = manifest_xml.IndexOf ("xmlns");
			string nm = manifest_xml.Substring (nm_i, manifest_xml.IndexOf ("\"", nm_i + 9) - nm_i + 1);
			LogDebug ("namespace removed:" + nm);
			manifest_xml = manifest_xml.Replace (nm, "");

			doc.LoadXml (manifest_xml);
		} catch (Exception e) {
			LogError ("Unable to download the manifest: " + manifest);
			LogError (e.ToString());
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

		LogDebug ("Nodes found:"+nodes.Count);

		foreach (XmlNode node in nodes) {

			Manifest_parsed_media manifest_parsed_media = new Manifest_parsed_media ();

			if (node.Attributes ["bitrate"] != null && node.Attributes ["bitrate"].InnerText != "") {
				manifest_parsed_media.bitrate = int.Parse (node.Attributes ["bitrate"].InnerText);
			} else {
				manifest_parsed_media.bitrate = 0;
			}
			LogDebug ("Parsed bitrate of node: " + manifest_parsed_media.bitrate);
			manifest_parsed_media.baseUrl = baseUrl;
			manifest_parsed_media.url = node.Attributes ["url"].InnerText;

			if (isRtmpUrl (manifest_parsed_media.baseUrl) || isRtmpUrl (manifest_parsed_media.url)) {
				LogError ("Provided manifest is not a valid HDS manifest");
				return;
			}

			int idx = manifest_parsed_media.url.IndexOf ("?");
			if (idx > -1) {
				manifest_parsed_media.queryString = manifest_parsed_media.url.Substring (idx);
				manifest_parsed_media.url = manifest_parsed_media.url.Substring (0, idx);
				if (auth.Length != 0) {
					LogDebug ("Manifest overrides 'auth': " + manifest_parsed_media.queryString);
				}
			} else {
				manifest_parsed_media.queryString = auth;
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

		manifest_parsed_media_list.Sort(delegate(Manifest_parsed_media x, Manifest_parsed_media y) {
			return y.bitrate.CompareTo(x.bitrate);
		});

		foreach (Manifest_parsed_media media_2 in manifest_parsed_media_list) {
			LogInfo ("Bitrate availabe: " + media_2.bitrate + ", url: " + media_2.url);
		}

		switch (quality) {
		case "high":
		case "":
			media = manifest_parsed_media_list [0];
			break;
		case "medium":
			media = manifest_parsed_media_list [((manifest_parsed_media_list.Count-1)/2)|0];
			break;
		case "low":
			media = manifest_parsed_media_list [manifest_parsed_media_list.Count - 1];
			break;
		default:
			// number
			foreach (Manifest_parsed_media media_2 in manifest_parsed_media_list) {
				if (media_2.bitrate == int.Parse(quality)) {
					media = media_2;
					break;
				}
			}
			break;
		}

		LogInfo ("Bitrate selected: " + media.bitrate);

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

		baseUrl = media.baseUrl;
		bootstrapInfo = media.bootstrap;

		manifest_parsed = true;
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
		fragTable = new List<Frag_table_content> ();


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
				ParseAsrtBox (bootstrapInfo, pos);
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

	public void ParseAsrtBox (byte[] asrt, int pos)
	{
		byte version = asrt [pos];
		int flags = ReadInt24 (asrt, pos + 1);
		byte qualityEntryCount = asrt [pos + 4];
		pos += 5;
		for (int i = 0; i < qualityEntryCount; i++) {
			ReadString (asrt, ref pos);
		}
		int segCount = ReadInt32 (asrt, pos);
		pos += 4;
		LogDebug ("Number - Fragments");
		for (int i = 0; i < segCount; i++) {
			int firstSegment = ReadInt32 (asrt, pos);
			segTable = new SegTable_content (firstSegment, ReadInt32 (asrt, pos + 4));
			if ((segTable.fragmentsPerSegment & 0x80000000) != 0) {
				segTable.fragmentsPerSegment = 0;
			}
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
			fragTable.Add(new Frag_table_content (firstFragment, ReadInt64 (afrt, pos + 4), ReadInt32 (afrt, pos + 12), new byte ()));
			pos += 16;
			if (fragTable [i].fragmentDuration == 0) {
				fragTable [i].discontinuityIndicator = afrt [pos++];
			}
		}
		foreach (Frag_table_content fragEntry in fragTable) {
			LogDebug (fragEntry.firstFragment + " - " + fragEntry.firstFragmentTimestamp + " - " + fragEntry.fragmentDuration + " - " + fragEntry.discontinuityIndicator);
		}
		LogDebug ("");
	}

	public void ParseSegAndFragTable ()
	{
		// Count total fragments by adding all entries in compactly coded segment table
		fragCount = segTable.fragmentsPerSegment;

		if ((fragCount & 0x80000000) == 0) {
			fragCount += fragTable [0].firstFragment - 1;
		}

		if (fragCount < fragTable [fragTable.Count-1].firstFragment) {
			fragCount = fragTable [fragTable.Count-1].firstFragment;
		}

		// Determine starting segment and fragment
		if (segTable.firstSegment < 1) {
			segTable.firstSegment = 1;
		}

		int fragStart = fragTable [0].firstFragment - 1;
		if (fragStart < 0) {
			segTable.firstSegment = 1;
		}
	}

	public void DownloadFragments (string manifest)
	{
		ParseManifest (manifest);

		if (!manifest_parsed) {
			LogInfo ("Stopping due to a problem parsing the manifest.");
			return;
		}

		int segNum = segTable.firstSegment;
		int fragNum = fragTable [0].firstFragment;

		lastFrag = fragNum;
		LogInfo ("Fragments Total: " + fragCount + ", First: " + fragTable [0].firstFragment + ", Start: " + (fragNum + 1));

		// Extract baseFilename
		baseFilename = media.url;
		if (baseFilename [baseFilename.Length - 1] == '/') {
			baseFilename = baseFilename.Substring (0, baseFilename.Length - 2);
		}
		baseFilename = RemoveExtension (baseFilename);
		int lastSlash = baseFilename.LastIndexOf ("/");
		if (lastSlash != -1) {
			baseFilename = baseFilename.Substring (lastSlash + 1);
		}
		if (manifest.IndexOf ("?") != -1) {
			baseFilename = CalculateMD5Hash (manifest.Substring (0, manifest.IndexOf ("?"))) + "_" + baseFilename;
		} else {
			baseFilename = CalculateMD5Hash (manifest) + "_" + baseFilename;
		}
		baseFilename += "Seg" + segNum + "-Frag";

		if (fragNum >= fragCount) {
			LogDebug ("fragNum = "+fragNum);
			LogDebug ("fragCount = "+fragCount);
			LogError ("No fragment available for downloading");
		}

		string fragUrl = AbsoluteUrl (baseUrl, media.url);
		LogDebug ("Base Fragment Url:\n" + fragUrl + "\n");
		LogDebug ("Downloading Fragments:\n");

		if(proxy.Length > 0 && fragments_proxy){
			webClientFragments.Proxy = new WebProxy(proxy);
		}
		if (userAgent.Length > 0) {
			webClientFragments.Headers.Add ("user-agent", userAgent);
		}
		if(referrer.Length > 0){
			webClientFragments.Headers.Add ("referer", referrer);
		}

		while (fragNum <= fragCount) {
			Frag_response frag = new Frag_response ();
			frag.id = fragNum;
			LogInfo ("Downloading " + fragNum + "/" + fragCount + " fragments");
			frag.filename = baseFilename + fragNum;
			frag.filenamePath = JoinUrl (outDir, frag.filename);
			if (File.Exists (frag.filenamePath)) {
				// If the file is corrupted or incompleted it will fail making error = true
				LogDebug ("Fragment fragNum is already downloaded");
				frag.response = file_get_contents (frag.filenamePath);
			} else {
				LogDebug ("Downloading fragment "+fragNum);
				segNum = segTable.firstSegment;
				string url = fragUrl + "Seg" + segNum + "-Frag" + fragNum + media.queryString;
				LogDebug ("Frag to download: " + url);

				webClientFragments.DownloadFile (url, frag.filenamePath);

				frag.response = file_get_contents (frag.filenamePath);
			}
			LogDebug ("Fragment " + frag.filename + " successfully downloaded");
			if (frag.response.Length != 0) {
				WriteFragment (ref file, frag);
				if (error) {
					LogError ("An error ocurred. Maybe the fragment is incomplete.");
					fragNum--;
					File.Delete (frag.filenamePath);
					error = false;
					continue;
				}
				frag.response = new byte[0];
				frags.Add (frag);
				fragNum++;
				if (delete_fragments_at_end) {
					File.Delete (frag.filenamePath);
				}
			} else {
				fragNum--;
				File.Delete (frag.filenamePath);
				LogDebug ("Fragment " + frag.id + " bad downloaded. Trying downloading it again.");
			}
		}

		LogInfo ("All fragments downloaded successfully");

		if (delete_fragments_at_end) {
			foreach (Frag_response frag in frags) {
				if (File.Exists (frag.filenamePath)) {
					File.Delete (frag.filenamePath);
				}
			}
		}

		processed = true;
	}

	public byte[] DecodeFragment (byte[] frag, int fragNum)
	{
		byte[] flvData = new byte[0];
		int fragPos = 0;
		int packetTS = 0;
		long fragLen = frag.LongLength;

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
		while (fragPos < fragLen && !error) {
			try{
				byte packetType = frag [fragPos];
				int packetSize = ReadInt24 (frag, fragPos + 1);
				packetTS = ReadInt24 (frag, fragPos + 4);
				packetTS = packetTS | (frag [fragPos + 7] << 24);
				if ((packetTS & 0x80000000) != 0) {
					packetTS &= 0x7FFFFFFF;
				}
				long totalTagLen = tagHeaderLen + packetSize + prevTagSize;

				int lastTS = prevVideoTS >= prevAudioTS ? prevVideoTS : prevAudioTS;
				int fixedTS = lastTS + FRAMEFIX_STEP;
				if (baseTS == INVALID_TIMESTAMP && (packetType == AUDIO || packetType == VIDEO)) {
					baseTS = packetTS;
				}
				if (baseTS > 1000 && packetTS >= baseTS) {
					packetTS -= baseTS;
				}
				if (lastTS != INVALID_TIMESTAMP) {
					int timeShift = packetTS - lastTS;
					if (timeShift > fixWindow) {
						LogDebug ("Timestamp gap detected: PacketTS=" + packetTS + " LastTS=" + lastTS + " Timeshift=" + timeShift);
						if (baseTS < packetTS) {
							baseTS += timeShift - FRAMEFIX_STEP;
						} else {
							baseTS = timeShift - FRAMEFIX_STEP;
						}
						packetTS = fixedTS;
					} else {
						lastTS = packetType == VIDEO ? prevVideoTS : prevAudioTS;
						if (packetTS < (lastTS - fixWindow)) {
							if ((negTS != INVALID_TIMESTAMP) && ((packetTS + negTS) < (lastTS - fixWindow))) {
								negTS = INVALID_TIMESTAMP;
							}
							if (negTS == INVALID_TIMESTAMP) {
								negTS = fixedTS - packetTS;
								LogDebug ("Negative timestamp detected: PacketTS=" + packetTS + " LastTS=" + lastTS + " NegativeTS=" + negTS);
								packetTS = fixedTS;
							} else {
								if ((packetTS + negTS) <= (lastTS + fixWindow)) {
									packetTS += negTS;
								} else {
									negTS = fixedTS - packetTS;
									LogDebug ("Negative timestamp override: PacketTS=" + packetTS + " LastTS=" + lastTS + " NegativeTS=" + negTS);
									packetTS = fixedTS;
								}
							}
						}
					}
				}

				switch (packetType) {
				case AUDIO:
					if (packetTS > prevAudioTS - fixWindow) {
						byte FrameInfo = frag [fragPos + tagHeaderLen];
						int CodecID = (FrameInfo & 0xF0) >> 4;
						byte AAC_PacketType = new byte ();
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
							if (!(CodecID == CODEC_ID_AAC && (AAC_PacketType == AAC_SEQUENCE_HEADER || prevAAC_Header))) {
								if ((prevAudioTS != INVALID_TIMESTAMP) && (packetTS <= prevAudioTS)) {
									LogDebug (" Fixing audio timestamp - AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize + "\n");
									packetTS += (FRAMEFIX_STEP / 5) + (prevAudioTS - packetTS);
									WriteFlvTimestamp (frag, fragPos, packetTS);
								}
							}

							byte[] new_flvData = new byte[flvData.Length + totalTagLen];
							Buffer.BlockCopy (flvData, 0, new_flvData, 0, flvData.Length);
							Buffer.BlockCopy (frag, fragPos, new_flvData, flvData.Length, (int)totalTagLen);
							flvData = new_flvData;
							new_flvData = null;

							LogDebug ("AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize);
									
							if (CodecID == CODEC_ID_AAC && AAC_PacketType == AAC_SEQUENCE_HEADER) {
								prevAAC_Header = true;
							} else {
								prevAAC_Header = false;
							}
							prevAudioTS = packetTS;
						} else {
							LogDebug ("Skipping small sized audio packet - AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize);
						}
					} else {
						LogDebug ("Skipping audio packet in fragment " + fragNum + " - AUDIO - " + packetTS + " - " + prevAudioTS + " - " + packetSize);
					}
					if (!audio) {
						audio = true;
					}
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
						byte AVC_PacketType = new byte ();
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
								if (cts != 0) {
									LogDebug ("DTS: "+packetTS+" CTS: " + cts + " PTS: " + pts);
								}
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
							Buffer.BlockCopy (frag, fragPos, new_flvData, flvData.Length, (int)totalTagLen);

							flvData = new_flvData;

							LogDebug ("VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);

							if (CodecID == CODEC_ID_AVC && AVC_PacketType == AVC_SEQUENCE_HEADER) {
								prevAVC_Header = true;
							} else {
								prevAVC_Header = false;
							}
							prevVideoTS = packetTS;
						} else {
							LogDebug ("Skipping small sized video packet - VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);
						}
					} else {
						LogDebug ("Skipping video packet in fragment " + fragNum + " - VIDEO - " + packetTS + " - " + prevVideoTS + " - " + packetSize);
					}
					if (!video) {
						video = true;
					}
					break;
				case SCRIPT_DATA:
					break;
				default:
					if (packetType == 10 || packetType == 11) {
						LogError ("This stream is encrypted with Akamai DRM. Decryption of such streams isn't currently possible with this script.");
					} else if (packetType == 40 || packetType == 41) {
						LogError ("This stream is encrypted with FlashAccess DRM. Decryption of such streams isn't currently possible with this script.");
					} else {
						LogInfo ("Unknown packet type " + packetType + " encountered! Unable to process fragment " + fragNum);
					}
					error = true;
					break;
				}
				fragPos += (int)totalTagLen;
			}catch(Exception e){
				LogError (e.ToString ());
				LogError ("Fragment with error. Deleting fragment and downloading it again.");
				error = true;
				break;
			}
		}
		return flvData;
	}

	public void WriteFragment (ref Stream flv, Frag_response frag)
	{
		if (frag.response.Length != 0) {
			LogDebug ("Writing fragment " + frag.id + " to flv file");
			byte[] flvData;
			if (flv == null) {
				if (play) {
					flv = Console.OpenStandardOutput ();
				} else {
					// In case of saving the file. Do another thing to stream it for the play option.
					string outFile = "";
					if (outFileGlobal.Length > 0) {
						outFile = JoinUrl (outDir, outFileGlobal + ".flv");
					} else {
						outFile = JoinUrl (outDir, baseFilename + ".flv");
					}
					file = File.Create (outFile);
				}
				flvData = DecodeFragment (frag.response, frag.id);
				if (error) {
					LogError ("An error ocurred");
					return;
				}
				byte[] flvHeader = CreateFlvHeader(audio, video);

				file.Write (flvHeader, 0, flvHeader.Length);

				if (media.metadata != null) {
					if (media.metadata.Length != 0) {
						WriteMetadata (file);
					}
				}
			} else {
				flvData = DecodeFragment (frag.response, frag.id);
				if (error) {
					LogError ("An error ocurred");
					return;
				}
			}
			if (flvData.Length != 0) {
				((FileStream)flv).Write (flvData, 0, flvData.Length);
			}
			lastFrag = frag.id;
		} else {
			lastFrag += 1;
			LogDebug ("Skipping failed fragment " + lastFrag);
		}
	}

	public byte[] CreateFlvHeader(bool audio, bool video)
	{
		byte[] flvHeader = new byte[]{ 0x46, 0x4c, 0x56, 0x01, 0x05, 0x00, 0x00, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00 };

		// Set proper Audio/Video marker
		flvHeader [4] = (byte)((audio ? 1 : 0) << 2 | (video ? 1 : 0));

		return flvHeader;
	}

	public void WriteMetadata (Stream flv)
	{
		if (media.metadata.Length != 0) {
			int metadataSize = media.metadata.Length;
			byte[] a = new byte[11];
			a [0] = SCRIPT_DATA;
			WriteInt24 (a, 1, metadataSize);
			WriteInt24 (a, 4, 0);
			WriteToByteArray (a, 7, BitConverter.GetBytes (0));


			byte[] res = new byte[a.Length + media.metadata.Length + 4];

			Buffer.BlockCopy (a, 0, res, 0, a.Length);
			Buffer.BlockCopy (media.metadata, 0, res, a.Length, media.metadata.Length);

			res [tagHeaderLen + metadataSize - 1] = 0x09;
			WriteToByteArray (res, tagHeaderLen + metadataSize, BitConverter.GetBytes (tagHeaderLen + metadataSize));

			media.metadata = res;
			flv.Write (media.metadata, 0, media.metadata.Length);
		}
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
	public string filenamePath;

	public Frag_response ()
	{
	}
}