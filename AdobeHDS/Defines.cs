using System;

namespace AdobeHDS
{
	public class Defines
	{
		public static int AUDIO = 0x08;
		public static int VIDEO = 0x09;
		public static int SCRIPT_DATA = 0x12;
		public static int FRAME_TYPE_INFO = 0x05;
		public static int CODEC_ID_AVC = 0x07;
		public static int CODEC_ID_AAC = 0x0A;
		public static int AVC_SEQUENCE_HEADER = 0x00;
		public static int AAC_SEQUENCE_HEADER = 0x00;
		public static int AVC_NALU = 0x01;
		public static int AVC_SEQUENCE_END = 0x02;
		public static int FRAMEFIX_STEP = 40;
		public static int INVALID_TIMESTAMP = -1;
		public static int STOP_PROCESSING = 2;
	}
}

