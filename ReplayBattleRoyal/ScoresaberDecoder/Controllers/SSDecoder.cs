using SevenZip.Compression.LZMA;
using SSDecoder.Decoder;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using static SSDecoder.Decoder.ReplayDecoding;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SSDecoder.Controllers
{

    public class SSDecoder
    {
        public static byte[] ReadFully(Stream stream, int initialLength)
        {
            if (initialLength < 1)
                initialLength = 32768;
            byte[] numArray = new byte[initialLength];
            int length = 0;
            int num1;
            try {
                while ((num1 = stream.Read(numArray, length, numArray.Length - length)) > 0)
                {
                    length += num1;
                    if (length == numArray.Length)
                    {
                        int num2 = stream.ReadByte();
                        if (num2 == -1)
                            return numArray;
                        byte[] destinationArray = new byte[numArray.Length * 2];
                        Array.Copy((Array)numArray, (Array)destinationArray, numArray.Length);
                        destinationArray[length] = (byte)num2;
                        numArray = destinationArray;
                        ++length;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
         
            byte[] destinationArray1 = new byte[length];
            Array.Copy((Array)numArray, (Array)destinationArray1, length);
            return destinationArray1;
        }

        public Result GetEmpDetails(string? playerID = null, string? songID = null, string? link = null) {
            Result? result = null; 
            string? error = null;


            if (link != null) {
                (result, error) = DecodeByLink(link);
            } else if (songID != null && playerID != null) {
                (result, error) = DecodeByLink("https://scoresaber.com/game/replays/" + songID + "-" + playerID + ".dat");
            }

            return result;
        }

        public (Result?, string?) DecodeByLink(string link)
        {
            try
            {
                using (WebClient webClient = new WebClient())
                {
                    webClient.Credentials = (ICredentials)CredentialCache.DefaultNetworkCredentials;
                    HttpWebResponse response = (HttpWebResponse)WebRequest.Create(link).GetResponse();
                    int contentLength = (int)response.ContentLength;
                    var f = SSDecoder.ReadFully(response.GetResponseStream(), contentLength);
                    return DecodeBuffer(f, contentLength);
                }
            }
            catch (WebException ex)
            {
                return (null, ((HttpWebResponse)ex.Response).StatusCode == HttpStatusCode.NotFound ? "{\"errorMessage\": \"Replay not found. Try better ranked play.\"}" : "{\"errorMessage\": \"Failed to download replay\"}");
            }
            catch (Exception ex)
            {
                return (null, "{\"errorMessage\": \"Failed to process replay\"}");
            }
        }

        private (Result?, string?) DecodeBuffer(byte[] buffer, long arrayLength)
        {
            byte[] bytes = Encoding.ASCII.GetBytes("ScoreSaber Replay \uD83D\uDC4C\uD83E\uDD20\r\n");
            int sourceIndex = bytes.Length + 4;
            bool flag = false;
            for (int index = 0; index < sourceIndex - 12 && (int)buffer[index] == (int)bytes[index]; ++index)
            {
                if (index == sourceIndex - 13)
                    flag = true;
            }
            if (!flag)
                return (null, "{\"errorMessage\": \"Old replay format not supported.\"}");
            byte[] numArray = new byte[arrayLength - sourceIndex];
            Array.Copy((Array)buffer, sourceIndex, (Array)numArray, 0, arrayLength - sourceIndex);
            return (new ReplayDecoding(SevenZipHelper.Decompress(numArray)).Decode(), null);
        }
    }
}