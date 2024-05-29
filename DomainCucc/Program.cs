using DomainCucc;
using DomainCucc.Remote;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

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
  
        while (true)
        {
            HttpListenerContext context = listener.GetContext(); 
            ThreadPool.QueueUserWorkItem(async (_) =>
            {

                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;


                string domain = request.Url.Host;
                var domainData = domainManager.domains.FirstOrDefault(i => i._Domain == domain);

               
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), domainData.Value, request.Url.LocalPath.TrimStart('/'));
               
                if(domainData.Type == DomainManager.DomainType.FOLDER)
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

                } else if(domainData.Type == DomainManager.DomainType.TRANSFER)
                {
                    string forwardUrl = domainData.Value + request.Url.PathAndQuery;
                    HttpResponseMessage responseMessage;
                    using (HttpClient httpClient = new HttpClient())
                    {
                        HttpRequestMessage forwardRequest = new HttpRequestMessage(new HttpMethod(request.HttpMethod), forwardUrl);

                        // Fejlécek másolása
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

                        // Kérés törzsének másolása (ha van)
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

                    // Válasz visszaküldése a kliensnek
                    response = context.Response;
                    response.StatusCode = (int)responseMessage.StatusCode;
                    response.ContentType = responseMessage.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";

                    // Válasz törzsének másolása
                    byte[] responseBody = await responseMessage.Content.ReadAsByteArrayAsync();
                    response.ContentLength64 = responseBody.Length;
                    await response.OutputStream.WriteAsync(responseBody, 0, responseBody.Length);
                }
                response.OutputStream.Close();
                
            });
        }
    }
}
