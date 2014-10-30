using System;
using System.IO;
using System.Net;
using System.Threading;

namespace WebServiceTest
{
    public class HttpClient
    {
        static void Main(string[] args)
        {
            HttpClient hc = new HttpClient("localhost", 8080);

            string ticketId = hc.Upload(args[0]).Trim();
            string folderName = @"c:\local\" + ticketId;
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            ConversionResult result = null;
            do
            {
                Thread.Sleep(1000);
                result = hc.CheckStatus(ticketId);
            }
            while (result.Status == ConversionStatus.Converting);

            if (result.Status == ConversionStatus.Success)
            {
                foreach (string url in result.Urls)
                {
                    string[] parts = url.Split(
                        new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                    string fileName = parts[parts.Length - 1];
                    hc.Download(url, Path.Combine(folderName, fileName));
                }

                Console.WriteLine("Downloaded");
            }
            else
            {
                Console.WriteLine("Failed");
            }
        }

        public string Url
        {
            get;
            set;
        }

        public int Port
        {
            get;
            set;
        }

        public HttpClient(string url, int port)
        {
            this.Url = url;
            this.Port = port;
        }

        public HttpClient(string url) : this(url, 80)
        {

        }

        /// <summary>
        /// Read all data
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        protected static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        public static void Copy(Stream input, string file)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }

            byte[] buffer = new byte[16 * 1024];
            using (FileStream fs = new FileStream(file, FileMode.CreateNew, FileAccess.Write))
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    fs.Write(buffer, 0, read);
                }
            }
        }

        public void Download(string url, string fileName)
        {
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create(url);
               // string.Format(
              //"http://{0}:{1}/file/{2}/{3}", this.Url, this.Port, ticketId, url));

            // If required by the server, set the credentials.
            //request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "x-www-form-urlencoded";
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            byte[] data = ReadFully(dataStream);
            //Copy(dataStream, @"c:\temp\output.zip");

            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            File.WriteAllBytes(fileName, data);

            response.Close();
        }

        public ConversionResult CheckStatus(string ticketId)
        {
            // Create a request for the URL. 
            WebRequest request = WebRequest.Create(
                string.Format(
              "http://{0}:{1}/status/{2}", this.Url, this.Port, ticketId));

            // If required by the server, set the credentials.
            //request.Credentials = CredentialCache.DefaultCredentials;
            request.Method = "GET";
            request.ContentType = "x-www-form-urlencoded";
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            Stream dataStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(dataStream);

            string ret = sr.ReadLine();

            ConversionResult result = new ConversionResult();
            if (ret.Contains("Success"))
            {
                result.Status = ConversionStatus.Success;
                string lines = sr.ReadLine();
                result.NumberOfPages = Convert.ToInt32(lines);
                result.Urls = new string[result.NumberOfPages];
                for (int index = 0; index < result.NumberOfPages; index++)
                {
                    result.Urls[index] = sr.ReadLine();
                    result.Urls[index] = result.Urls[index].Trim();
                }
            }
            else if (ret.Contains("Converting"))
            {
                result.Status = ConversionStatus.Converting;
            }
            else if (ret.Contains("Failed"))
            {
                result.Status = ConversionStatus.Failed;
            }

            response.Close();

            return result;
        }

        public string Upload(string fileName)
        {
            FileInfo fi = new FileInfo(fileName);
            // Create a request using a URL that can receive a post. 
            WebRequest request = WebRequest.Create( //"http://localhost:8080/upload/" + fi.Name);
                    string.Format(
                        "http://{0}:{1}/upload/{2}", this.Url, this.Port, fi.Name));
            // Set the Method property of the request to POST.
            request.Method = "POST";
            // Create POST data and convert it to a byte array.
            //string postData = "This is a test that posts this string to a Web server.";
            byte[] byteArray = File.ReadAllBytes(fileName);
            //byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            // Set the ContentType property of the WebRequest.
            request.ContentType = "application/octet-stream"; // x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = byteArray.Length;
            // Get the request stream.
            Stream dataStream = request.GetRequestStream();
            // Write the data to the request stream.
            dataStream.Write(byteArray, 0, byteArray.Length);
            // Close the Stream object.
            dataStream.Close();
            // Get the response.
            WebResponse response = request.GetResponse();
            // Display the status.
            Console.WriteLine(((HttpWebResponse)response).StatusDescription);
            // Get the stream containing content returned by the server.
            dataStream = response.GetResponseStream();
            // Open the stream using a StreamReader for easy access.
            StreamReader reader = new StreamReader(dataStream);
            // Read the content.
            string responseFromServer = reader.ReadToEnd();
            // Display the content.
            Console.WriteLine(responseFromServer);
            // Clean up the streams.
            reader.Close();
            dataStream.Close();
            response.Close();

            return responseFromServer;
        }
    }
}
