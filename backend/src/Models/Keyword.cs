namespace src.Models;

public class Keyword
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LearningStatus { get; set; } = "not_learned";
    public DateTime CreatedAt { get; set; }

    public List<Job> Jobs { get; set; } = [];
    public List<Project> Projects { get; set; } = [];
    public List<WorkExperience> WorkExperiences { get; set; } = [];
}
