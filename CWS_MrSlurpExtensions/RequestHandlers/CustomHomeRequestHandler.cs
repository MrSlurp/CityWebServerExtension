using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CityWebServer.Extensibility;
using ColossalFramework;
using UnityEngine;
using JetBrains.Annotations;
using System.IO;
using ApacheMimeTypes;
using ColossalFramework.Plugins;
using CityWebServer.Extensibility.Responses;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CWS_MrSlurpExtensions
{
    [UsedImplicitly]
    class CustomHomeRequestHandler : RequestHandlerBase
    {
        public CustomHomeRequestHandler(IWebServer server)
            : base(server, new Guid("C49B6F44-8E33-4603-869B-E2A1EEE603CD"), "Slurp UI home", "MrSlurp", 100, "/SlurpUI")
        {
        }

        /// <summary>
        /// override that allow handling any content requested to /SlurpUI
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public override Boolean ShouldHandle(HttpListenerRequest request)
        {
            //this.OnLogMessage(string.Format("should handle called with path={0} - {1}", request.Url.AbsolutePath.ToLower(), _mainPath.ToLower()));
            //this.OnLogMessage(string.Format("contains={0}", request.Url.AbsolutePath.ToLower().Contains("slurpui")));
            if (request.Url.AbsolutePath.ToLower().Contains("slurpui"))
            {
                //only if request is SlurpUI root or an existing www file
                var theoricFilePath = Path.Combine(GetModRoot() + Path.DirectorySeparatorChar + "www", cleanUrlFilePath(request.Url.AbsolutePath));
                this.OnLogMessage(string.Format("theoricFilePath={0} / Url.AbsolutePath={1} ({2})", theoricFilePath, request.Url.AbsolutePath, GetModRoot() + Path.DirectorySeparatorChar + "www"));
                if (request.Url.AbsolutePath.ToLower() == "/slurpui")
                {
                    OnLogMessage(string.Format("Take the deal for {0}", request.Url.AbsolutePath));
                    return true;
                }
                else if (File.Exists(theoricFilePath))
                {
                    OnLogMessage(string.Format("Take the deal for {0}", request.Url.AbsolutePath));
                    return true;
                }
                else
                {
                    OnLogMessage(string.Format("not for me or file does not exist {0}", theoricFilePath));
                }
                return false;
            }
            return false;
        }

        private string cleanUrlFilePath(string urlAbsPath)
        {
            var tmpUrl = Regex.Replace(urlAbsPath, "/slurpui", "", RegexOptions.IgnoreCase).Replace('/', Path.DirectorySeparatorChar);
            return tmpUrl.Length > 0 && tmpUrl[0] == Path.DirectorySeparatorChar ? tmpUrl.Remove(0, 1) : tmpUrl;
        }

        /// <summary>
        /// Gets the full path to the directory where static pages are served from.
        /// </summary>
        public String GetModRoot()
        {
            var modPaths = PluginManager.instance.GetPluginsInfo().Select(obj => obj.modPath);
            foreach (var path in modPaths)
            {
                var testPath = Path.Combine(path, "CWS_MrSlurpExtensions.dll");
                if (File.Exists(testPath))
                {
                    return path;
                }
            }
            return null;
        }

        public override IResponseFormatter Handle(HttpListenerRequest request)
        {
            var modPath = GetModRoot();
            if (modPath == null || !Directory.Exists(Path.Combine(modPath, "www")))
            {
                return this.PlainTextResponse("Unable to find current mod directory", HttpStatusCode.NotFound);
            }

            string localFilePath = Path.Combine(modPath, "www");
            if (request.Url.AbsolutePath.ToLower() == "/slurpui")
            {
                //this.OnLogMessage(string.Format("redirect to {0}", request.Url.AbsolutePath + "/index.html"));
                return new InternalHtmlResponseFormatter(File.ReadAllText(Path.Combine(localFilePath, "index.html")), request.Url.AbsolutePath + "/index.html");
            }
            else if (request.Url.AbsolutePath.ToLower().Contains("slurpui"))
            {
                var cleanedFilePath = cleanUrlFilePath(request.Url.AbsolutePath);
                //OnLogMessage(string.Format("uri contains slurpUI {0}, {1}", localFilePath, cleanedFilePath));
                var finalPath = Path.Combine(localFilePath, cleanedFilePath);
                //OnLogMessage(string.Format("final path {0}", finalPath));
                localFilePath = finalPath;
            }
            //OnLogMessage(string.Format("Tested local file path {0}", localFilePath));
            if (File.Exists(localFilePath))
                return new FileResponseFormatter(localFilePath);
            else
            {
                String body = String.Format("No resource is available at the specified filepath: {0}", localFilePath);
                return this.PlainTextResponse(body, HttpStatusCode.NotFound);
            }
        }

        public class FileResponseFormatter : IResponseFormatter
        {
            private string filePath;
            public FileResponseFormatter(string fileAbsolutePath)
            {
                filePath = fileAbsolutePath;
            }

            public override void WriteContent(HttpListenerResponse response)
            {
                var extension = Path.GetExtension(this.filePath);
                response.ContentType = Apache.GetMime(extension);
                response.StatusCode = 200; // HTTP 200 - SUCCESS

                // Open file, read bytes into buffer and write them to the output stream.
                using (FileStream fileReader = File.OpenRead(this.filePath))
                {
                    byte[] buffer = new byte[4096];
                    int read;
                    while ((read = fileReader.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        response.OutputStream.Write(buffer, 0, read);
                    }
                }
            }
        }
        internal class InternalHtmlResponseFormatter : IResponseFormatter
        {
            private readonly String _content;
            private readonly string _requestRedirect;
            private readonly HttpStatusCode _statusCode;

            public InternalHtmlResponseFormatter(String content, string requestRedirect=null, HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                _content = content;
                _statusCode = statusCode;
                _requestRedirect = requestRedirect;

            }
            public override void WriteContent(HttpListenerResponse response)
            {
                byte[] buf = Encoding.UTF8.GetBytes(_content);

                response.StatusCode = (int)_statusCode;
                if (_requestRedirect != null)
                    response.Redirect(_requestRedirect);

                response.ContentType = "text/html";
                response.ContentLength64 = buf.Length;
                response.OutputStream.Write(buf, 0, buf.Length);
            }
        }

    }
}
