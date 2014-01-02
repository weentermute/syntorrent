using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SynologyWebApi
{
    /// <summary>
    /// Represents a single HTTP request sent to the Synology Disk-station.
    /// </summary>
    public class HttpRequest
    {
        public class HttpParameters : Dictionary<string, object>
        {
            public string UrlEncode()
            {
                string result = "";
                foreach (string key in this.Keys)
                {
                    dynamic value = this[key];
                    if (value is string)
                    {
                        result += key + "=" + System.Web.HttpUtility.UrlEncode((string)value) + "&";
                    }
                    else
                    {
                        throw new ArgumentException("Invalid URL request parameter");
                    }
                }

                // Remove trailing "&"
                return result.TrimEnd(new char[] { '&' });
            }

            public byte[] ByteEncode()
            {
                return System.Text.Encoding.ASCII.GetBytes(this.UrlEncode());
            }
        }

        public HttpParameters GetParameters;
        public HttpParameters PostParameters;

        public string Get(string url)
        {
            string getURL = url + "?" + GetParameters.UrlEncode();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(getURL);
            request.Method = "GET";

            // Send Web-Request and receive a Web-Response
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Translate data from the Web-Response to a string
            Stream dataStream = response.GetResponseStream();
            StreamReader streamreader = new StreamReader(dataStream, Encoding.UTF8);
            string html = streamreader.ReadToEnd();
            streamreader.Close();

            // clear request parameters
            GetParameters.Clear();
            PostParameters.Clear();

            return html;
        }

        public string Post(string url)
        {
            string getURL = url + "?" + GetParameters.UrlEncode();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(getURL);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            // Attach data to the Web-Request
            byte[] postData = this.PostParameters.ByteEncode();
            request.ContentLength = postData.Length;
            Stream dataStream = request.GetRequestStream();
            dataStream.Write(postData, 0, postData.Length);
            dataStream.Close();

            // Send Web-Request and receive a Web-Response
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            // Translate data from the Web-Response to a string
            dataStream = response.GetResponseStream();
            StreamReader streamreader = new StreamReader(dataStream, Encoding.UTF8);
            string html = streamreader.ReadToEnd();
            streamreader.Close();

            // Clear request parameters
            GetParameters.Clear();
            PostParameters.Clear();

            return html;
        }

        public string PostMultipartFormData(string url)
        {
            // Send Web-Request and receive a Web-Response
            HttpWebResponse response = (HttpWebResponse)FormUpload.MultipartFormDataPost(url, null, PostParameters);

            // Translate data from the Web-Response to a string
            Stream dataStream = response.GetResponseStream();
            StreamReader streamreader = new StreamReader(dataStream, Encoding.UTF8);
            string html = streamreader.ReadToEnd();
            streamreader.Close();

            // Clear request parameters
            GetParameters.Clear();
            PostParameters.Clear();

            return html;
        }


        public HttpRequest()
        {
            GetParameters = new HttpParameters();
            PostParameters = new HttpParameters();
        }
    }


    /// <summary>
    /// Found on https://gist.github.com/bgrins/1789787
    /// 
    /// Implements multipart/form-data POST in C# http://www.ietf.org/rfc/rfc2388.txt
    /// http://www.briangrinstead.com/blog/multipart-form-post-in-c
    /// </summary>
    public static class FormUpload
    {
        private static readonly Encoding encoding = Encoding.UTF8;
        public static HttpWebResponse MultipartFormDataPost(string postUrl, string userAgent, Dictionary<string, object> postParameters)
        {
            string formDataBoundary = String.Format("----------{0:N}", Guid.NewGuid());
            string contentType = "multipart/form-data; boundary=" + formDataBoundary;

            byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

            return PostForm(postUrl, userAgent, contentType, formData);
        }
        private static HttpWebResponse PostForm(string postUrl, string userAgent, string contentType, byte[] formData)
        {
            HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;

            if (request == null)
            {
                throw new NullReferenceException("request is not a http request");
            }

            // Set up the request properties.
            request.Method = "POST";
            request.ContentType = contentType;
            request.UserAgent = userAgent;
            request.CookieContainer = new CookieContainer();
            request.ContentLength = formData.Length;

            // You could add authentication here as well if needed:
            // request.PreAuthenticate = true;
            // request.AuthenticationLevel = System.Net.Security.AuthenticationLevel.MutualAuthRequested;
            // request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.Default.GetBytes("username" + ":" + "password")));

            // Send the form data to the request.
            using (Stream requestStream = request.GetRequestStream())
            {
                requestStream.Write(formData, 0, formData.Length);
                requestStream.Close();
            }

            return request.GetResponse() as HttpWebResponse;
        }

        private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
        {
            Stream formDataStream = new System.IO.MemoryStream();
            bool needsCLRF = false;

            foreach (var param in postParameters)
            {
                // Thanks to feedback from commenters, add a CRLF to allow multiple parameters to be added.
                // Skip it on the first parameter, add it to subsequent parameters.
                if (needsCLRF)
                    formDataStream.Write(encoding.GetBytes("\r\n"), 0, encoding.GetByteCount("\r\n"));

                needsCLRF = true;

                if (param.Value is FileParameter)
                {
                    FileParameter fileToUpload = (FileParameter)param.Value;

                    // Add just the first part of this param, since we will write the file data directly to the Stream
                    string header = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
                        boundary,
                        param.Key,
                        fileToUpload.FileName ?? param.Key,
                        fileToUpload.ContentType ?? "application/octet-stream");

                    formDataStream.Write(encoding.GetBytes(header), 0, encoding.GetByteCount(header));

                    // Write the file data directly to the Stream, rather than serializing it to a string.
                    formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
                }
                else
                {
                    string postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);
                    formDataStream.Write(encoding.GetBytes(postData), 0, encoding.GetByteCount(postData));
                }
            }

            // Add the end of the request.  Start with a newline
            string footer = "\r\n--" + boundary + "--\r\n";
            formDataStream.Write(encoding.GetBytes(footer), 0, encoding.GetByteCount(footer));

            // Dump the Stream into a byte[]
            formDataStream.Position = 0;
            byte[] formData = new byte[formDataStream.Length];
            formDataStream.Read(formData, 0, formData.Length);
            formDataStream.Close();

            return formData;
        }

        /// <summary>
        /// Parameter which can be used with Web requests expecting a file upload.
        /// </summary>
        public class FileParameter
        {
            public byte[] File { get; set; }
            public string FileName { get; set; }
            public string ContentType { get; set; }
            public FileParameter(byte[] file) : this(file, null) { }
            public FileParameter(byte[] file, string filename) : this(file, filename, null) { }
            public FileParameter(byte[] file, string filename, string contenttype)
            {
                File = file;
                FileName = filename;
                ContentType = contenttype;
            }
        }
    }
}
