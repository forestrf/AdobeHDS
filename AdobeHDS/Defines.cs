using System;


public class Defines
{
	public const byte AUDIO = 0x08;
	public const byte VIDEO = 0x09;
	public const byte SCRIPT_DATA = 0x12;
	public const byte FRAME_TYPE_INFO = 0x05;
	public const byte CODEC_ID_AVC = 0x07;
	public const byte CODEC_ID_AAC = 0x0A;
	public const byte AVC_SEQUENCE_HEADER = 0x00;
	public const byte AAC_SEQUENCE_HEADER = 0x00;
	public const byte AVC_NALU = 0x01;
	public const byte AVC_SEQUENCE_END = 0x02;
	public const int FRAMEFIX_STEP = 40;
	public const int INVALID_TIMESTAMP = -1;
	public const int STOP_PROCESSING = 2;
}