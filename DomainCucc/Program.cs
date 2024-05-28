using System;
using System.IO;
using System.Net;
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

  
        while (true)
        {
            HttpListenerContext context = listener.GetContext(); 
            ThreadPool.QueueUserWorkItem((_) =>
            {
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
                string filePath = Path.Combine(Directory.GetCurrentDirectory(), request.Url.LocalPath.TrimStart('/'));
               
                    try
                    {
                        string domain = request.Url.Host;
                        //if domain ->  filepath += "domain amit vissza ad"

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
                
                
                    response.OutputStream.Close();
                
            });
        }
    }
}
