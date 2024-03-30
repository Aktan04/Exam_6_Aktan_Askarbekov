using System.Text.Json;

namespace exam_6;

public class MySerializer
{
    private static List<TaskModel>? Tasks { get; set; }
    
    public static List<TaskModel>? LoadTasks()
    {
        if (File.Exists("../../../tasks.json"))
            return Tasks ?? JsonSerializer.Deserialize<List<TaskModel>>(File.ReadAllText("../../../tasks.json"));
        else
        {
            Console.WriteLine("Json not found, Creating...");
            File.WriteAllText("../../../tasks.json", "");
            return new List<TaskModel>();
        }
    }

    public static void WriteTasks(List<TaskModel> tasks)
    {
        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };
        File.WriteAllText("../../../tasks.json", JsonSerializer.Serialize(tasks, options));
    }
}