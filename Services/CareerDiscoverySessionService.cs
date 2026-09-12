using StudyMap.Models;

namespace StudyMap.Services;

/// <summary>
/// Holds in-progress answers for the "Not Sure What to Choose" questionnaire
/// so the student can move forward/back between questions without losing selections.
/// Session-scoped only (not persisted) — reset with <see cref="Reset"/> when retaking.
/// </summary>
public class CareerDiscoverySessionService
{
    private static CareerDiscoverySessionService? _instance;
    public static CareerDiscoverySessionService Instance => _instance ??= new CareerDiscoverySessionService();

    public List<CareerAnswer> Answers { get; } = [];

    public void Reset() => Answers.Clear();

    public void SetAnswer(int questionNumber, List<string> optionIds, List<string> optionTexts)
    {
        Answers.RemoveAll(a => a.QuestionNumber == questionNumber);
        Answers.Add(new CareerAnswer
        {
            QuestionNumber = questionNumber,
            SelectedOptionIds = optionIds,
            SelectedOptionTexts = optionTexts
        });
    }

    public CareerAnswer? GetAnswer(int questionNumber) =>
        Answers.FirstOrDefault(a => a.QuestionNumber == questionNumber);
}
