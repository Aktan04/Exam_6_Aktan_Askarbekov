using exam_6;

string site = "../../../site";
List<TaskModel> tasks =GetTasksFromJson();
DumpHttpServer server = new DumpHttpServer(tasks);

await server.RunAsync(site, 8888);

List<TaskModel> GetTasksFromJson()
{
    try
    {
        List<TaskModel> loadedTasks = MySerializer.LoadTasks();
        if (loadedTasks != null && loadedTasks.Count > 0)
            return loadedTasks;
        else
            return new List<TaskModel>();
    }
    catch (Exception e)
    {
        Console.WriteLine(e.Message);
        return new List<TaskModel>();
    }
    
}