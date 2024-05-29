using DomainCucc;
using DomainCucc.Remote;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Channels;

class Program
{
    static void Main(string[] args)
    {
        // A szerver indítása a 1234-es porton
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://*:5000/");
        listener.Start();
        Console.WriteLine("Web Server Running...");
        RemoteAuth remoteAuth = new RemoteAuth();
        DomainManager domainManager = new DomainManager();
        string adminDomain = "remote.admin.paraghtibor.hu";
  
        while (true)
        {
            HttpListenerContext context = listener.GetContext(); 
            ThreadPool.QueueUserWorkItem(async (_) =>
            {
               

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string domain = request.Url.Host;
                Log(request.HttpMethod, domain);


                if (adminDomain == domain && request.HttpMethod == "POST" && request.Url.AbsolutePath == "/login")
                {
                    using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string requestBody = await reader.ReadToEndAsync();

                        try
                        {
                            Dictionary<string,string> data = domainManager.DecodeBody(requestBody);
                            string username = data["username"];
                            string password = WebUtility.UrlDecode(data["password"]); 
                          
                            if(remoteAuth.Login(username, password))
                            {
                                response.StatusCode = 200;
                            } else
                            {
                                response.StatusCode = 401;
                            }
                        }
                        catch (Exception ex) { 
                            response.StatusCode = 415;
                        }
                    }
                } else
                {
                    var domainData = domainManager.domains.FirstOrDefault(i => i._Domain == domain);


                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), domainData.Value, request.Url.LocalPath.TrimStart('/'));

                    if (domainData.Type == DomainManager.DomainType.FOLDER)
                    {
                        try
                        {

                            if (request.Url.ToString().EndsWith("/"))
                            {
                                filePath += "/index.html";
                            }
                            byte[] buffer = File.ReadAllBytes(filePath);
                            response.ContentLength64 = buffer.Length;
                            response.OutputStream.Write(buffer, 0, buffer.Length);
                            response.OutputStream.Close();
                        }
                        catch (Exception ex)
                        {

                            Console.WriteLine($"Error serving {request.Url.LocalPath}: {ex.Message}");
                            response.StatusCode = (int)HttpStatusCode.InternalServerError;
                            response.OutputStream.Close();
                        }

                    }
                    else if (domainData.Type == DomainManager.DomainType.TRANSFER)
                    {
                        string forwardUrl = domainData.Value + request.Url.PathAndQuery;
                        HttpResponseMessage responseMessage;
                        using (HttpClient httpClient = new HttpClient())
                        {
                            HttpRequestMessage forwardRequest = new HttpRequestMessage(new HttpMethod(request.HttpMethod), forwardUrl);

                            foreach (string header in request.Headers)
                            {
                                if (!forwardRequest.Headers.TryAddWithoutValidation(header, request.Headers[header]))
                                {
                                    if (request.ContentType != null)
                                    {
                                        forwardRequest.Content?.Headers.TryAddWithoutValidation(header, request.Headers[header]);
                                    }
                                }
                            }


                            if (request.HasEntityBody)
                            {
                                using (StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding))
                                {
                                    string requestBody = await reader.ReadToEndAsync();
                                    forwardRequest.Content = new StringContent(requestBody, Encoding.UTF8, request.ContentType);
                                }
                            }

                            responseMessage = await httpClient.SendAsync(forwardRequest);
                        }

                        response = context.Response;
                        response.StatusCode = (int)responseMessage.StatusCode;
                        response.ContentType = responseMessage.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                       
                        byte[] responseBody = await responseMessage.Content.ReadAsByteArrayAsync();
                        response.ContentLength64 = responseBody.Length;
                        await response.OutputStream.WriteAsync(responseBody, 0, responseBody.Length);
                    }
                }
               
                response.OutputStream.Close();
                
            });
        }
    }
    private static void Log(string rType, string domain)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("[");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(rType);
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.Write("] => ");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(domain);
    }
}
