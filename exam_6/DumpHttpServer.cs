using System.Collections.Specialized;
using System.Net;
using System.Web;
using RazorEngine;
using RazorEngine.Templating;
using Encoding = System.Text.Encoding;

namespace exam_6;

public class DumpHttpServer
{
    private string _siteDirectory; 
    private HttpListener _listener; 
    private int _port; 

    private List<TaskModel> _taskModels;

    public DumpHttpServer(List<TaskModel> taskModels)
    {
        _taskModels = taskModels;
    }

    public async Task RunAsync(string path, int port)
    {
        _siteDirectory = path;
        _port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:" + _port.ToString() + "/");
        _listener.Start();
        Console.WriteLine($"Сервер запущен на порту: {port}");
        Console.WriteLine($"Файлы сайта лежат в папке: {path}");
        await ListenAsync();
    }
    
    public void Stop()
    {
        _listener.Abort();
        _listener.Stop();
    }
    
    private async Task ListenAsync()
    {
        try
        {
            while (true)
            {
                HttpListenerContext context = await _listener.GetContextAsync();
                Process(context);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    
    private void Process(HttpListenerContext context)
    {
       string filename = context.Request.Url.AbsolutePath;
       Console.WriteLine(filename);
       filename = filename.Substring(1);
       filename = Path.Combine(_siteDirectory, filename);
       
       if (File.Exists(filename))
       {
           try
           {
               if (context.Request.HttpMethod == "POST")
               {
                   using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
                   {
                       string requestData = reader.ReadToEnd();
                       string name = HttpUtility.ParseQueryString(requestData)["title"];
                       string desc = HttpUtility.ParseQueryString(requestData)["description"];
                       string executorName = HttpUtility.ParseQueryString(requestData)["executorName"]; 
                       if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(desc) && !string.IsNullOrEmpty(executorName))
                       {
                           _taskModels.Add(new TaskModel(name, desc, executorName));
                           MySerializer.WriteTasks(_taskModels);
                           string redirectUrl = $"http://localhost:8888/index.html";
                           context.Response.Redirect(redirectUrl);
                       }
                   }
               }
               else if (context.Request.HttpMethod == "GET")
               {
                   NameValueCollection queryParams = context.Request.QueryString;
                   string action = queryParams["action"];
                   string taskId = queryParams["id"];

                   if (!string.IsNullOrEmpty(action))
                   {
                       if (action == "view")
                       {
                           TaskModel task = _taskModels.FirstOrDefault(t => t.Id == Convert.ToInt32(taskId));
                           if (task != null)
                           {
                               string template = File.ReadAllText(Path.Combine(_siteDirectory, "taskDetails.html"));
                               string html = Engine.Razor.RunCompile(template, "taskDetailsTemplate", null, task);
                               byte[] htmlBufferBytes = Encoding.UTF8.GetBytes(html);
                               context.Response.ContentType = "text/html";
                               context.Response.ContentLength64 = htmlBufferBytes.Length;
                               context.Response.OutputStream.Write(htmlBufferBytes, 0, htmlBufferBytes.Length);
                               context.Response.StatusCode = (int)HttpStatusCode.OK;
                               context.Response.OutputStream.Flush();
                           }
                           else
                           {
                               context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                               context.Response.OutputStream.Close();
                           }
                           return;
                       }
                       HandleCatAction(action, taskId);
                       context.Response.Redirect("/index.html");
                   }
               }
               
               NameValueCollection query = context.Request.QueryString;
               string content = "";
               if (filename.Contains("html"))
               {
                   content = BuildHtml(filename, query["arg"]);
               }
               else
               {
                   content = File.ReadAllText(filename);
               }

               byte[] htmlBytes = Encoding.UTF8.GetBytes(content);
               Stream fileStream = new MemoryStream(htmlBytes);
               context.Response.ContentType = GetContentType(filename);
               context.Response.ContentLength64 = fileStream.Length;
               byte[] buffer = new byte[16 * 1024]; 
               int dataLength;
               do
               {
                   dataLength = fileStream.Read(buffer, 0, buffer.Length);
                   context.Response.OutputStream.Write(buffer, 0, dataLength);
               } while (dataLength > 0);
               fileStream.Close();
               context.Response.StatusCode = (int)HttpStatusCode.OK;
               context.Response.OutputStream.Flush();
           }
           catch (Exception e)
           {
               Console.WriteLine(e.Message);
               context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
           }
       }
       else
       {
           context.Response.StatusCode = (int)HttpStatusCode.NotFound;
       }
       context.Response.OutputStream.Close();
    }
    
    private static string GetContentType(string filename)
    {
       var dictionary = new Dictionary<string, string>
       {
           { ".css", "text/css" },
           { ".html", "text/html; charset=utf-8" }, 
           { ".ico", "image/x-icon" },
           { ".js", "application/x-javascript" },
           { ".json", "application/json" },
           { ".png", "image/png" }
       };
       string contentType = "";
       string fileExtension = Path.GetExtension(filename);
       dictionary.TryGetValue(fileExtension, out contentType);
       return contentType;
    }

    private string BuildHtml(string filename, string userstring)
    {
        string html = "";
        string layoutPath = "../../../site/layout.html";

        var razorService = Engine.Razor;
        if (!razorService.IsTemplateCached("layout", null))
        {
            razorService.AddTemplate("layout", File.ReadAllText(layoutPath));
        }

        if (!razorService.IsTemplateCached(filename, null))
        {
            razorService.AddTemplate(filename, File.ReadAllText(filename));
            razorService.Compile(filename);
        }

        html = razorService.Run(filename, null, new
        {
            Tasks = _taskModels
        });
        return html;
    }
    
    private void HandleCatAction(string action, string taskId)
    {
        TaskModel task = _taskModels.FirstOrDefault(t => t.Id == Convert.ToInt32(taskId));
        if (task != null)
        {
            switch (action)
            {
                case "complete":
                    if (task.Status == "new")
                    {
                        task.Status = "in_progress";
                        MySerializer.WriteTasks(_taskModels); 
                    }
                    else if(task.Status == "in_progress")
                    {
                        task.Status = "done";
                        task.CompletedDate = DateTime.Now.ToString(); 
                        MySerializer.WriteTasks(_taskModels);
                    }
                    break;
                case "delete":
                    TaskModel taskToRemove = _taskModels.FirstOrDefault(t => t.Id == Convert.ToInt32(taskId));
                    if (taskToRemove != null)
                    {
                        _taskModels.Remove(taskToRemove);
                        MySerializer.WriteTasks(_taskModels); 
                    }
                    break;
            }
        }
    }

}