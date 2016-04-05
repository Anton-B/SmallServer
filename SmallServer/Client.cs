using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SmallServer
{
    static class Client
    {
        public static void Request(TcpClient client)
        {
            using (client)
            {
                var reader = new StreamReader(client.GetStream());
                var request = reader.ReadLine();
                var requestUri = Uri.UnescapeDataString(request.Split(' ')[1]);
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

        private static void OpenDir(TcpClient client, string path)
        {
            using (client)
            {
                var dirsArray = new DirectoryInfo(path.Equals("/") ? Environment.CurrentDirectory : path).GetDirectories();
                var dirs = new StringBuilder();
                string parentPath = path.TrimEnd('/');
                parentPath = (parentPath.Contains('/')) ? parentPath.Substring(0, parentPath.LastIndexOf('/')) : string.Empty;
                if (!path.Equals("/"))
                    dirs.AppendFormat("<a href=\"/{0}\">{1}/</a><br>", parentPath, "[Parent directory]");
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
                SendResponse(client, 200, "text/html", Encoding.ASCII.GetBytes(html));
                return;
            }
        }

        private static void OpenFile(TcpClient client, string path)
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
                byte[] content;
                try
                {
                    content = File.ReadAllBytes(file.FullName);
                }
                catch
                {
                    SendError(client, 500);
                    return;
                }                
                SendResponse(client, 200, contentType, content);
            }
        }

        private static void SendResponse(TcpClient client, int code, string contentType, byte[] content)
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

        private static void SendError(TcpClient client, int code)
        {
            using (client)
            {
                var html = string.Format("<html><body><h1>{0} {1}</h1></body></html>", code, (HttpStatusCode)code);
                SendResponse(client, code, "text/html", Encoding.ASCII.GetBytes(html));
            }
        }
    }
}
