namespace StudyMap.Models;

/// <summary>
/// A tag contributed to the recommendation engine when an option is selected,
/// with a weight indicating how strongly it should influence matching careers.
/// </summary>
public class CareerTagWeight
{
    public string Tag { get; set; } = string.Empty;
    public int Weight { get; set; } = 2;
}

public class CareerQuestionOption
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public List<CareerTagWeight> TagWeights { get; set; } = [];
}

public class CareerQuestion
{
    public int Number { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool AllowMultiple { get; set; } = true;
    public List<CareerQuestionOption> Options { get; set; } = [];
}
