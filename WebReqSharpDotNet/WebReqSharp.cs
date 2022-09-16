using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;

namespace WebReqSharpDotNet
{
    /// <summary>
    /// 文字类型数据
    /// </summary>
    public class TextData
    {
        /// <summary>
        /// 数据名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 数据内容
        /// </summary>
        public string Context { get; set; }
    }
    public class ByteData
    {
        /// <summary>
        /// 数据名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 请求FileName属性
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 请求ContentType属性
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// 数据内容
        /// </summary>
        public byte[] Bytes { get; set; }
    }
    /// <summary>
    /// 请求类型
    /// </summary>
    public enum RequestType
    {
        GET = 0,
        POST = 1
    }

    public class Request
    {
        /// <summary>
        /// 文字类型的数据
        /// </summary>
        public List<TextData> TextDatas { get; set; }

        /// <summary>
        /// byte[]类型的数据（仅Post可用）
        /// </summary>
        public List<ByteData> ByteDatas { get; set; }

        /// <summary>
        /// 所有的Cookies
        /// </summary>
        public CookieCollection Cookies { get; set; }

        /// <summary>
        /// 请求发往的URL
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 请求类型，默认为GET
        /// </summary>
        public RequestType Type { get; set; } = RequestType.GET;

        /// <summary>
        /// 是否使用KeepAlive属性，默认为false.
        /// </summary>
        public bool KeepAlive { get; set; } = false;

        private string boundary;

        /// <summary>
        /// 发送请求后的返回流，只可调用一次，之后无法调整指针位置
        /// </summary>
        public HttpWebResponse Response { internal set; get; }

        /// <summary>
        /// 发送当前http请求
        /// </summary>
        public void Send()
        {
            //获取cookies并放进cookiecontainer
            CookieContainer cookieContainer = new CookieContainer();
            if (this.Cookies != null)
            {
                foreach (Cookie cookie in this.Cookies)
                {
                    cookieContainer.Add(cookie);
                }
            }



            //Get模式下，将参数加在Url末尾
            if (Type == RequestType.GET & TextDatas != null)
            {

                Url += "?";
                for (int i = 0; i < this.TextDatas.Count; i++)
                {
                    TextData data = this.TextDatas[i];
                    if (i > 1) { Url += "&"; }
                    Url += $"{data.Name}={data.Context}";
                }
            }



            //创建请求
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(this.Url);
            httpWebRequest.Method = Type.ToString();
            httpWebRequest.KeepAlive = KeepAlive;



            //post类型下 设置请求头类型
            if (Type == RequestType.POST & this.ByteDatas != null)
            {
                this.boundary = "--------------------------" + DateTime.Now.Ticks.ToString("x");
                httpWebRequest.ContentType = $"multipart/form-data; boundary={boundary}";
            }

            if (Type == RequestType.POST & this.ByteDatas == null)
            {
                httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            }



            //发送body
            if (Type == RequestType.POST & this.TextDatas != null & this.ByteDatas == null)
            {
                byte[] postDataBytes = GetUrlEncodedDataBytes(boundary, TextDatas);
                Stream stream = httpWebRequest.GetRequestStream();
                stream.Write(postDataBytes, 0, postDataBytes.Length);
                stream.Close();
            }

            if (Type == RequestType.POST & this.TextDatas != null & this.ByteDatas != null)
            {
                byte[] postDataBytes = GetFormDataBytes(boundary, TextDatas, ByteDatas);

                Stream stream = httpWebRequest.GetRequestStream();
                stream.Write(postDataBytes, 0, postDataBytes.Length);
                stream.Close();
            }



            //获取返回
            Response = (HttpWebResponse)httpWebRequest.GetResponse();

            //获取返回的cookie
            foreach (Cookie item in Response.Cookies)
            {
                this.Cookies.Add(item);
            }
        }

        /// <summary>
        /// 获取发送请求后返回的byte[]值，调用了ResponseStream.
        /// </summary>
        /// <returns>请求发送完后，返回的byte[]</returns>
        public byte[] GetResponseBytes()
        {
            MemoryStream memoryStream = new MemoryStream();
            Response.GetResponseStream().CopyTo(memoryStream);
            byte[] buffer = new byte[memoryStream.Length];
            memoryStream.Position = 0;
            memoryStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// 获取发送请求后返回的字符串值，调用了ResponseStream.
        /// </summary>
        /// <returns>请求发送完后，返回的string</returns>
        public string GetResponseString()
        {
            StreamReader streamReader = new StreamReader(Response.GetResponseStream());
            return streamReader.ReadToEnd();
        }


        //序列化data，转换为对应的byte[]
        private byte[] GetFormDataBytes(string boundary, List<TextData> textDatas, List<ByteData> fileDatas)
        {
            MemoryStream memStream = new MemoryStream();

            foreach (TextData textData in textDatas)
            {
                var header = GetTextHeader(this.boundary, textData.Name);
                memStream.Write(header,0,header.Length);
                memStream.Write(Encoding.UTF8.GetBytes("\r\n"), 0, Encoding.UTF8.GetBytes("\r\n").Length);
                break;
            }

            foreach (ByteData fileData in fileDatas)
            {
                var header = GetFileHeader(this.boundary, fileData.Name, fileData.FileName, fileData.ContentType);
                memStream.Write(header, 0,header.Length);
                memStream.Write(fileData.Bytes,0, fileData.Bytes.Length);
                memStream.Write(Encoding.UTF8.GetBytes("\r\n"),0, Encoding.UTF8.GetBytes("\r\n").Length);
                break;
            }

            byte[] data = new byte[memStream.Length];
            memStream.Position = 0;
            memStream.Read(data, 0, data.Length);
            return data;
        }
        private byte[] GetUrlEncodedDataBytes(string boundary, List<TextData> textDatas)
        {
            var encodedData = "";

            for (int i = 0; i < textDatas.Count; i++)
            {
                TextData textData = textDatas[i];
                if (i > 0) { encodedData += "&"; }
                encodedData += $"{textData.Name}={textData.Context}";
            }

            byte[] data = Encoding.UTF8.GetBytes(encodedData);
            return data;
        }


        //获取FormData类型的Headers
        private byte[] GetTextHeader(string boundary, string name)
        {
            string headerStr = "--" + boundary + "\r\n";
            headerStr += $"Content-Disposition: form-data; name=\"{name}\"\r\n\r\n";
            byte[] headers = Encoding.UTF8.GetBytes(headerStr);

            return headers;
        }
        private byte[] GetFileHeader(string boundary, string name, string fileName, string ContentType)
        {

            if (ContentType == "") { ContentType = "application/octet-stream"; }
            string headerStr = "--" + boundary + "\r\n";
            headerStr += $"Content-Disposition: form-data; name=\"{name}\";filename=\"{fileName}\"\r\n\r\n";
            if (ContentType != null) { headerStr += $"Content-Type: {ContentType}\r\n\r\n"; }

            return Encoding.UTF8.GetBytes(headerStr);
        }
    }
}
