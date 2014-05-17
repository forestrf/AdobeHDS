using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class cURL : Functions
{
	string user_agent, compression;
	WebHeaderCollection headers;
	string response;
	NetworkStream response_stream;
	bool cookies, cert_check;
	CookieContainer cookieJar;
	private static int reff = 0;

	List<HttpWebRequest> mh; // array descargar simultaneas
	List<string> ch; // Ids descargas simultaneas
	List<byte[]> bh; // Bytearray con la respuesta
	List<bool> th; // Respuestas terminadas o no


	public cURL()
	{
		cURL(true, new CookieContainer());
	}
	public cURL(bool cookies, CookieContainer cookieJar)
	{
		cURL(cookies, cookieJar, "gzip");
	}
	public cURL(bool cookies, CookieContainer cookieJar, string compression)
	{
		this.headers     = this.headersF();
		this.user_agent  = "Mozilla/5.0 (Windows NT 5.1; rv:26.0) Gecko/20100101 Firefox/26.0";
		this.compression = compression;
		this.cookies     = cookies;
		this.cookieJar   = cookieJar;
		this.cert_check  = false;
		this.mh = new List<HttpWebRequest> ();
		this.ch = new List<int> ();
		this.bh = new List<byte[]> ();
		++reff;
	}

	~cURL()
	{
		this.stopDownloads();
		--reff;
	}

	private void headersF(WebHeaderCollection headers)
	{
		headers.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
		headers.Add("Connection", "Keep-Alive");
	}

	public int get(string url)
	{
		HttpWebRequest process = new HttpWebRequest ();
		process.Address = url;
		this.headersF(headers);
		process.Headers = headers;
		process.UserAgent = this.user_agent;
		process.TransferEncoding = this.compression;
		process.AllowAutoRedirect = true;
		process.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None;
		process.Timeout = 30;

		process.UseDefaultCredentials = this.cert_check;

		if (this.cookies)
			process.CookieContainer = cookieJar;


		this.response_stream = process.GetRequestStream();

		int status = 0;

		if (process.HaveResponse) {
			byte[] buffer = new byte[this.response_stream.Length];
			this.response_stream.Read (buffer, 0, buffer.Length);

			this.response = Encoding.Default.GetString (this.response_stream);

			status = this.response.Substring (this.response.IndexOf (" ") + 1, 3);

			int fin_cabecera = this.response.IndexOf ("\r\n\r\n") + 4;
			this.response = this.response.Substring (fin_cabecera, this.response.Length - 4);
		}

		this.response_stream.Close ();

		return status;
	}

	public int post(string url, string data)
	{
		HttpWebRequest process = new HttpWebRequest ();
		process.Address = url;
		this.headersF(headers);
		process.Headers = headers;
		process.UserAgent = this.user_agent;
		process.TransferEncoding = this.compression;
		process.AllowAutoRedirect = true;
		process.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None;
		process.Timeout = 30;

		process.UseDefaultCredentials = this.cert_check;

		process.Method = "POST";
		process.ContentType = "application/x-www-form-urlencoded";

		byte[] data_byte = Encoding.Default.GetBytes (data);
		process.ContentLength = data_byte.Length;


		if (this.cookies)
			process.CookieContainer = cookieJar;


		this.response_stream = process.GetRequestStream();
		this.response_stream.Write (data_byte, 0, data_byte.Length);

		int status = 0;

		if (process.HaveResponse) {
			byte[] buffer = new byte[this.response_stream.Length];
			this.response_stream.Read (buffer, 0, buffer.Length);

			this.response = Encoding.Default.GetString (this.response_stream);

			status = this.response.Substring (this.response.IndexOf (" ") + 1, 3);

			int fin_cabecera = this.response.IndexOf ("rn") + 4;
			this.response = this.response.Substring (fin_cabecera, this.response.Length - 4);
		}

		this.response_stream.Close ();

		return status;
	}

	public bool addDownload(string url, int id)
	{
		if (ch.IndexOf(id) != -1)
			return false;

		HttpWebRequest process = new HttpWebRequest ();

		process.Address = url;
		this.headersF(headers);
		process.Headers = headers;
		process.UserAgent = this.user_agent;
		process.TransferEncoding = this.compression;
		process.AllowAutoRedirect = true;
		process.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.None;

		process.UseDefaultCredentials = this.cert_check;

		if (this.cookies)
			process.CookieContainer = this.cookieJar;

		this.ch.Insert (id, process);
		this.bh.Insert (id, null);
		this.th.Insert (id, false);

		process.BeginGetRequestStream (iar => {
			try{
				NetworkStream stream = process.EndGetRequestStream(iar);

				long length = stream.Length;

				int cutHeader = 4;
				while(cutHeader > 0){
					byte t;
					stream.WriteByte(t);
					if(t == 0x0D || t == 0x0A)
						--cutHeader;
					else
						cutHeader = 4;
					--length;
				}
				

				this.bh[id] = new byte[length];
				stream.Read (this.bh[id], 0, length);
				this.th[id] = true;
			}
			catch (Exception exc){}
		}, null);

		return true;
	}

	public void stopDownloads()
	{
		if (this.mh.Count > 0)
		{
			for (var id = 0; id < this.mh.Count; ++id) {
				(this.ch [id] as HttpWebRequest).Abort ();
				this.bh [id] = null;
				this.th [id] = false;
			}
		}
	}

	public void error(string error)
	{
		LogError("cURL Error : "+error);
	}
}