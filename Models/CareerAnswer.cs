namespace StudyMap.Models;

/// <summary>
/// The student's selection for a single questionnaire question.
/// </summary>
public class CareerAnswer
{
    public int QuestionNumber { get; set; }
    public List<string> SelectedOptionIds { get; set; } = [];
    public List<string> SelectedOptionTexts { get; set; } = [];
}

/// <summary>
/// The full set of answers collected from the "Not Sure What to Choose" questionnaire.
/// </summary>
public class QuestionnaireResult
{
    public List<CareerAnswer> Answers { get; set; } = [];
}
