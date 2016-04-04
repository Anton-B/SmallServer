using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SmallServer
{
    class Client
    {
        public Client(TcpClient client)
        {
            using (client)
            {
                var request = new StringBuilder();
                var buffer = new byte[1024];
                int count;
                while ((count = client.GetStream().Read(buffer, 0, buffer.Length)) > 0)
                {
                    request.Append(Encoding.ASCII.GetString(buffer, 0, buffer.Length));
                    if (request.ToString().IndexOf("\r\n\r\n") >= 0)
                        break;
                }
                var requestUri = Uri.UnescapeDataString(request.ToString().Split(' ')[1]);
                if (requestUri.IndexOf("..") >= 0)
                {
                    SendError(client, 400);
                    return;
                }
                requestUri = requestUri.TrimStart('/');
                if (File.Exists(requestUri))
                {
                    OpenFile(client, requestUri);
                    return;
                }
                if (!requestUri.EndsWith("/"))
                    requestUri = string.Format("{0}/", requestUri);
                if (Directory.Exists(requestUri) || requestUri.Equals("/"))
                {
                    OpenDir(client, requestUri);
                    return;
                }
                SendError(client, 400);
            }
        }

        private void OpenDir(TcpClient client, string path)
        {
            using (client)
            {
                var dirsArray = new DirectoryInfo(path.Equals("/") ? Environment.CurrentDirectory : path).GetDirectories();
                var dirs = new StringBuilder();
                string parentPath = path.TrimEnd('/');
                parentPath = (parentPath.Contains('/')) ? parentPath.Substring(0, parentPath.LastIndexOf('/')) : string.Empty;
                if (!path.Equals("/"))
                    dirs.AppendFormat("<a href=\"/{0}\">{1}/</a><br>", parentPath, "[Родительская директория]");
                else if (File.Exists("index.html"))
                {
                    OpenFile(client, "index.html");
                    return;
                }
                foreach (var dir in dirsArray)
                    dirs.AppendFormat("<a href=\"/{0}{1}\">{1}/</a><br>", path.TrimStart('/'), dir.Name);
                var filesArray = new DirectoryInfo(path.Equals("/") ? Environment.CurrentDirectory : path).GetFiles();
                var files = new StringBuilder();
                foreach (var file in filesArray)
                    files.AppendFormat("<a href=\"/{0}{1}\">{1}</a><br>", path.TrimStart('/'), file.Name);
                var html = string.Format("<html><body><h1>Current directory content</h1><br>{0}<br>{1}</body></html>",
                    dirs.ToString(), files.ToString());
                SendHtmlResponse(client, 200, html);
                return;
            }
        }

        private void OpenFile(TcpClient client, string path)
        {
            using (client)
            {
                var file = new FileInfo(path);
                var contentType = string.Empty;
                switch (file.Extension)
                {
                    case ".htm":
                    case ".html":
                        contentType = "text/html";
                        break;
                    case ".css":
                        contentType = "text/css";
                        break;
                    case ".js":
                        contentType = "text/javascript";
                        break;
                    case ".jpg":
                        contentType = "image/jpeg";
                        break;
                    case ".jpeg":
                    case ".png":
                    case ".gif":
                        contentType = string.Format("image/{0}", file.Extension.Substring(1));
                        break;
                    case ".mp3":
                        contentType = "audio/mp3";
                        break;
                    default:
                        contentType = file.Extension.Length > 1
                            ? string.Format("text/{0}", file.Extension.Substring(1)) : "application/unknown";
                        break;
                }
                FileStream fs;
                try
                {
                    fs = file.OpenRead();
                }
                catch
                {
                    SendError(client, 500);
                    return;
                }
                var content = new List<byte>();
                while (true)
                {
                    var n = fs.ReadByte();
                    if (n == -1)
                        break;
                    content.Add((byte)n);
                }
                SendResponse(client, 200, contentType, content.ToArray());
            }
        }

        private void SendHtmlResponse(TcpClient client, int code, string html)
        {
            using (client)
            {
                var response = string.Format("HTTP/1.1 {0} {1}\r\nContent-Type: text/html\r\nContent-Length: {2}\r\n\r\n{3}",
                    code, (HttpStatusCode)code, html.Length, html);
                var buffer = Encoding.ASCII.GetBytes(response);
                client.GetStream().Write(buffer, 0, buffer.Length);
            }
        }

        private void SendResponse(TcpClient client, int code, string contentType, byte[] content)
        {
            using (client)
            {
                var responseHeader = string.Format("HTTP/1.1 {0} {1}\r\nContent-Type: {2}\r\nContent-Length: {3}\r\n\r\n",
                    code, (HttpStatusCode)code, contentType, content.Length);
                var headerBuffer = Encoding.ASCII.GetBytes(responseHeader);
                client.GetStream().Write(headerBuffer, 0, headerBuffer.Length);
                client.GetStream().Write(content, 0, content.Length);
            }
        }

        private void SendError(TcpClient client, int code)
        {
            using (client)
            {
                var html = string.Format("<html><body><h1>{0} {1}</h1></body></html>", code, (HttpStatusCode)code);
                SendHtmlResponse(client, code, html);
            }
        }
    }
}
