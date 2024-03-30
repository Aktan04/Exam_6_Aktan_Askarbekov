namespace exam_6;

public class TaskModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string CreatedDate { get; set; } 
    public string Status { get; set; }
    public string Description { get; set; }
    public string CompletedDate { get; set; }
    public string ExecutorName { get; set; } 
    private static int counter;
    
    public TaskModel(string title, string description, string executorName)
    {
        Id = Interlocked.Increment(ref counter);
        Title = title;
        Description = description;
        CreatedDate = DateTime.Now.ToString();
        Status = "new";
        ExecutorName = executorName;
    }
}