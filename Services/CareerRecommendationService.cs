using StudyMap.Models;

namespace StudyMap.Services;

/// <summary>
/// Rule-based (no AI, no external API) recommendation engine for the
/// "Not Sure What to Choose" career discovery module.
/// Scores careers against questionnaire answers and returns a ranked list.
/// </summary>
public class CareerRecommendationService
{
    private readonly CareersService _careersService;

    public CareerRecommendationService(CareersService? careersService = null)
    {
        _careersService = careersService ?? new CareersService();
    }

    // ── Public API ───────────────────────────────────────────────────
    public IReadOnlyList<CareerQuestion> Questions => _questions;

    public async Task<List<CareerRecommendation>> GetRecommendationsAsync(
        IEnumerable<CareerAnswer> answers, int topCount = 5)
    {
        var tagScores = BuildTagScores(answers);
        var careers = await _careersService.GetAllCareersAsync();

        if (careers.Count == 0)
            return [];

        var positiveWeightTotal = tagScores.Values.Where(w => w > 0).Sum();
        var maxPossibleScore = positiveWeightTotal > 0 ? positiveWeightTotal : 1;

        var scored = new List<CareerRecommendation>();

        foreach (var career in careers)
        {
            var careerTags = GetCareerTags(career);
            var matchedTags = new List<string>();
            var score = 0;

            foreach (var tag in careerTags)
            {
                if (tagScores.TryGetValue(tag, out var weight))
                {
                    score += weight;
                    if (weight > 0)
                    {
                        matchedTags.Add(tag);
                    }
                }
            }

            if (score <= 0)
            {
                continue;
            }

            var percentage = (int)Math.Round(100.0 * score / maxPossibleScore);
            percentage = Math.Clamp(percentage, 35, 97);

            scored.Add(new CareerRecommendation
            {
                Career = career,
                Score = score,
                MatchPercentage = percentage,
                MatchedTags = matchedTags.Distinct().Take(6).ToList(),
                WhyItMatches = BuildWhyItMatches(matchedTags.Distinct().ToList())
            });
        }

        var ranked = scored
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.Career.IsPopular)
            .ThenBy(r => r.Career.Name)
            .Take(topCount)
            .ToList();

        if (ranked.Count == 0)
        {
            ranked = careers
                .Where(c => c.IsPopular)
                .OrderBy(c => c.Name)
                .Take(topCount)
                .Select(c => new CareerRecommendation
                {
                    Career = c,
                    Score = 0,
                    MatchPercentage = 0,
                    MatchedTags = [],
                    WhyItMatches = "A popular career area many students explore first."
                })
                .ToList();
        }

        return ranked;
    }

    // ── Scoring helpers ──────────────────────────────────────────────
    private static Dictionary<string, int> BuildTagScores(IEnumerable<CareerAnswer> answers)
    {
        var scores = new Dictionary<string, int>();

        foreach (var answer in answers)
        {
            var question = _questions.FirstOrDefault(q => q.Number == answer.QuestionNumber);
            if (question is null)
            {
                continue;
            }

            foreach (var optionId in answer.SelectedOptionIds)
            {
                var option = question.Options.FirstOrDefault(o => o.Id == optionId);
                if (option is null)
                {
                    continue;
                }

                foreach (var tagWeight in option.TagWeights)
                {
                    scores.TryGetValue(tagWeight.Tag, out var existing);
                    scores[tagWeight.Tag] = existing + tagWeight.Weight;
                }
            }
        }

        return scores;
    }

    private static List<string> GetCareerTags(Career career)
    {
        var tags = new List<string>();
        tags.AddRange(career.CareerCategories);
        tags.AddRange(career.Interests);
        tags.AddRange(career.PreferredSubjects);
        tags.AddRange(career.WorkStyles);
        return tags;
    }

    private static string BuildWhyItMatches(List<string> matchedTags)
    {
        if (matchedTags.Count == 0)
        {
            return "This career is a popular starting point worth exploring.";
        }

        var shown = matchedTags.Take(4).ToList();
        var bullets = string.Join(" · ", shown.Select(t => $"✓ {t}"));
        return $"You selected: {bullets}\n\nThese interests are commonly useful in this field.";
    }

    // ── Question bank ────────────────────────────────────────────────
    private static readonly List<CareerQuestion> _questions = BuildQuestions();

    private static List<CareerQuestion> BuildQuestions()
    {
        return
        [
            new CareerQuestion
            {
                Number = 1,
                Text = "What subjects do you enjoy the most?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q1-math", "Mathematics", "🔢", "Mathematics"),
                    Opt("q1-science", "Science", "🔬", "Science"),
                    Opt("q1-biology", "Biology", "🧬", "Biology"),
                    Opt("q1-computers", "Computers", "💻", "Computers"),
                    Opt("q1-business", "Business & Economics", "📈", "Business & Economics"),
                    Opt("q1-languages", "Languages", "🗣️", "Languages"),
                    Opt("q1-art", "Art & Design", "🎨", "Art & Design"),
                    Opt("q1-social", "Social Studies", "🌍", "Social Studies"),
                ]
            },
            new CareerQuestion
            {
                Number = 2,
                Text = "What kind of activities do you enjoy?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q2-puzzles", "Solving puzzles", "🧩", "Solving puzzles"),
                    Opt("q2-building", "Building things", "🛠️", "Building things"),
                    Opt("q2-helping", "Helping people", "🤝", "Helping people"),
                    Opt("q2-designing", "Creating designs", "🖌️", "Creating designs"),
                    Opt("q2-computers", "Working with computers", "💻", "Working with computers"),
                    Opt("q2-leading", "Leading a team", "👥", "Leading a team"),
                    Opt("q2-writing", "Writing or communicating", "✍️", "Writing or communicating"),
                    Opt("q2-understanding", "Understanding how things work", "⚙️", "Understanding how things work"),
                ]
            },
            new CareerQuestion
            {
                Number = 3,
                Text = "How do you feel about Mathematics?",
                AllowMultiple = false,
                Options =
                [
                    OptWeighted("q3-love", "I love it", "😍", "Mathematics", 3),
                    OptWeighted("q3-enjoy", "I enjoy it", "🙂", "Mathematics", 2),
                    OptWeighted("q3-okay", "It's okay", "😐", "Mathematics", 1),
                    OptWeighted("q3-not", "I don't enjoy it", "😕", "Mathematics", -2),
                ]
            },
            new CareerQuestion
            {
                Number = 4,
                Text = "Which sounds most interesting to you?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q4-technology", "Creating technology", "💻", "Creating technology"),
                    Opt("q4-body", "Discovering how the human body works", "🩺", "Discovering how the human body works"),
                    Opt("q4-business", "Running a business", "💼", "Running a business"),
                    Opt("q4-design", "Designing creative things", "🎨", "Designing creative things"),
                    Opt("q4-environment", "Protecting the environment", "🌱", "Protecting the environment"),
                    Opt("q4-money", "Understanding money and investments", "💰", "Understanding money and investments"),
                    Opt("q4-helping", "Helping people", "🤝", "Helping people"),
                    Opt("q4-research", "Research and discovery", "🔍", "Research and discovery"),
                ]
            },
            new CareerQuestion
            {
                Number = 5,
                Text = "What are you naturally good at?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q5-logical", "Logical thinking", "🧠", "Logical thinking"),
                    Opt("q5-communication", "Communication", "🗣️", "Communication"),
                    Opt("q5-creativity", "Creativity", "🎨", "Creativity"),
                    Opt("q5-problem", "Problem solving", "🧩", "Problem solving"),
                    Opt("q5-leadership", "Leadership", "👑", "Leadership"),
                    Opt("q5-numbers", "Working with numbers", "🔢", "Working with numbers"),
                    Opt("q5-people", "Working with people", "🤝", "Working with people"),
                    Opt("q5-detail", "Attention to detail", "🔎", "Attention to detail"),
                ]
            },
            new CareerQuestion
            {
                Number = 6,
                Text = "How do you prefer to work?",
                AllowMultiple = false,
                Options =
                [
                    Opt("q6-independent", "Independently", "🧍", "Independently"),
                    Opt("q6-small-team", "With a small team", "👥", "With a small team"),
                    Opt("q6-lots-people", "With lots of people", "👨‍👩‍👧‍👦", "With lots of people"),
                    OptNoTag("q6-unsure", "I don't know yet", "🤷"),
                ]
            },
            new CareerQuestion
            {
                Number = 7,
                Text = "Which type of problems would you enjoy solving?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q7-technology", "Technology problems", "💻", "Technology"),
                    Opt("q7-business", "Business problems", "💼", "Business & Finance"),
                    Opt("q7-health", "Health-related problems", "🩺", "Healthcare"),
                    Opt("q7-design", "Design problems", "🎨", "Design & Media"),
                    OptMulti("q7-environment", "Environmental problems", "🌱", ("Science & Research", 2), ("Protecting the environment", 2)),
                    Opt("q7-financial", "Financial problems", "💰", "Business & Finance"),
                    Opt("q7-social", "Social problems", "🌍", "Law & Public Service"),
                ]
            },
            new CareerQuestion
            {
                Number = 8,
                Text = "What kind of work environment sounds interesting?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q8-office", "Office / Technology", "💻", "Office / Technology"),
                    Opt("q8-hospital", "Hospital / Healthcare", "🏥", "Hospital / Healthcare"),
                    Opt("q8-lab", "Laboratory / Research", "🔬", "Laboratory / Research"),
                    Opt("q8-corporate", "Business / Corporate", "🏢", "Business / Corporate"),
                    Opt("q8-studio", "Creative Studio", "🎨", "Creative Studio"),
                    Opt("q8-outdoors", "Outdoors", "🌳", "Outdoors"),
                    Opt("q8-government", "Government / Public Service", "🏛️", "Government / Public Service"),
                    OptNoTag("q8-unsure", "I'm not sure", "🤷"),
                ]
            },
            new CareerQuestion
            {
                Number = 9,
                Text = "What matters most to you in a future career?",
                AllowMultiple = true,
                Options =
                [
                    OptMulti("q9-earning", "High earning potential", "💵", ("Technology", 1), ("Business & Finance", 1)),
                    OptMulti("q9-stability", "Job stability", "🛡️", ("Government / Public Service", 1), ("Healthcare", 1)),
                    OptMulti("q9-creativity", "Creativity", "🎨", ("Design & Media", 1), ("Creativity", 1)),
                    OptMulti("q9-helping", "Helping people", "🤝", ("Helping people", 2), ("Healthcare", 1)),
                    OptMulti("q9-innovation", "Innovation", "💡", ("Technology", 1), ("Science & Research", 1)),
                    OptMulti("q9-leadership", "Leadership", "👑", ("Leading a team", 1)),
                    OptNoTag("q9-balance", "Work-life balance", "⚖️"),
                    OptNoTag("q9-international", "Working internationally", "🌐"),
                ]
            },
            new CareerQuestion
            {
                Number = 10,
                Text = "Which career areas would you like to explore?",
                AllowMultiple = true,
                Options =
                [
                    OptWeighted3("q10-technology", "Technology", "💻", "Technology"),
                    OptWeighted3("q10-healthcare", "Healthcare", "🩺", "Healthcare"),
                    OptWeighted3("q10-business", "Business & Finance", "💼", "Business & Finance"),
                    OptWeighted3("q10-engineering", "Engineering", "⚙️", "Engineering"),
                    OptWeighted3("q10-science", "Science & Research", "🔬", "Science & Research"),
                    OptWeighted3("q10-design", "Design & Media", "🎨", "Design & Media"),
                    OptWeighted3("q10-law", "Law & Public Service", "⚖️", "Law & Public Service"),
                    OptWeighted3("q10-education", "Education", "📚", "Education"),
                    OptNoTag("q10-unsure", "Not sure", "🤷"),
                ]
            },
        ];
    }

    private static CareerQuestionOption Opt(string id, string text, string icon, string tag) =>
        new()
        {
            Id = id,
            Text = text,
            Icon = icon,
            TagWeights = [new CareerTagWeight { Tag = tag, Weight = 2 }]
        };

    private static CareerQuestionOption OptWeighted(string id, string text, string icon, string tag, int weight) =>
        new()
        {
            Id = id,
            Text = text,
            Icon = icon,
            TagWeights = [new CareerTagWeight { Tag = tag, Weight = weight }]
        };

    private static CareerQuestionOption OptWeighted3(string id, string text, string icon, string tag) =>
        new()
        {
            Id = id,
            Text = text,
            Icon = icon,
            TagWeights = [new CareerTagWeight { Tag = tag, Weight = 3 }]
        };

    private static CareerQuestionOption OptMulti(string id, string text, string icon, params (string Tag, int Weight)[] tagWeights) =>
        new()
        {
            Id = id,
            Text = text,
            Icon = icon,
            TagWeights = [.. tagWeights.Select(tw => new CareerTagWeight { Tag = tw.Tag, Weight = tw.Weight })]
        };

    private static CareerQuestionOption OptNoTag(string id, string text, string icon) =>
        new() { Id = id, Text = text, Icon = icon, TagWeights = [] };
}
