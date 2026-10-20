using StudyMap.Models;

namespace StudyMap.Services;

/// <summary>
/// Rule-based (no AI, no external API) recommendation engine for the
/// "Not Sure What to Choose" career discovery module.
///
/// Directional activities and career interests are intentionally weighted
/// more strongly than school subjects. This prevents a broad subject such as
/// Mathematics from making unrelated engineering or science careers outrank
/// a student's actual career direction.
/// </summary>
public class CareerRecommendationService
{
    private readonly CareersService _careersService;

    public CareerRecommendationService(CareersService? careersService = null)
    {
        _careersService = careersService ?? new CareersService();
    }

    public IReadOnlyList<CareerQuestion> Questions => _questions;

    public async Task<List<CareerRecommendation>> GetRecommendationsAsync(
        IEnumerable<CareerAnswer> answers, int topCount = 5)
    {
        var answerList = answers?.ToList() ?? [];
        var tagScores = BuildTagScores(answerList);
        var careers = await _careersService.GetAllCareersAsync();

        if (careers.Count == 0)
            return [];

        var positiveWeightTotal = tagScores.Values.Where(w => w > 0).Sum();
        var maxPossibleScore = Math.Max(positiveWeightTotal, 1);
        var scored = new List<CareerRecommendation>();

        foreach (var career in careers)
        {
            var evidence = GetCareerEvidence(career);
            var matchedSignals = new List<string>();
            var score = 0.0;

            foreach (var signal in tagScores)
            {
                if (signal.Value == 0)
                    continue;

                if (MatchesSignalToCareer(signal.Key, evidence))
                {
                    score += signal.Value;
                    if (signal.Value > 0)
                        matchedSignals.Add(signal.Key);
                }
            }

            // A negative Mathematics response should count against genuinely
            // mathematics-heavy careers.
            if (tagScores.TryGetValue("Mathematics", out var mathWeight) && mathWeight < 0 &&
                IsMathHeavyCareer(evidence))
            {
                score += mathWeight * 2;
            }

            // Specialist domains need explicit supporting signals. For example,
            // Biomedical Engineer should not become a strong recommendation
            // merely because the student likes Mathematics and puzzles.
            score -= GetMissingDomainPenalty(evidence, tagScores);

            if (score <= 0)
                continue;

            var coverage = Math.Clamp(score / maxPossibleScore, 0.0, 1.0);
            var percentage = 35 + (int)Math.Round(60 * coverage);
            percentage = Math.Clamp(percentage, 35, 95);

            var distinctMatches = matchedSignals
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(6)
                .ToList();

            scored.Add(new CareerRecommendation
            {
                Career = career,
                Score = (int)Math.Round(score * 10),
                MatchPercentage = percentage,
                MatchedTags = distinctMatches,
                WhyItMatches = BuildWhyItMatches(distinctMatches)
            });
        }

        var ranked = scored
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.MatchPercentage)
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

    private static Dictionary<string, int> BuildTagScores(IEnumerable<CareerAnswer> answers)
    {
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var answer in answers)
        {
            var question = _questions.FirstOrDefault(q => q.Number == answer.QuestionNumber);
            if (question is null)
                continue;

            foreach (var optionId in answer.SelectedOptionIds)
            {
                var option = question.Options.FirstOrDefault(o => o.Id == optionId);
                if (option is null)
                    continue;

                foreach (var tagWeight in option.TagWeights)
                {
                    scores.TryGetValue(tagWeight.Tag, out var existing);
                    scores[tagWeight.Tag] = existing + tagWeight.Weight;
                }
            }
        }

        return scores;
    }

    private static HashSet<string> GetCareerEvidence(Career career)
    {
        var evidence = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddEvidence(evidence, career.Name);
        AddEvidence(evidence, career.Category);
        AddEvidence(evidence, career.CareerCategories);
        AddEvidence(evidence, career.Interests);
        AddEvidence(evidence, career.PreferredSubjects);
        AddEvidence(evidence, career.WorkStyles);
        AddEvidence(evidence, career.KeySkills);
        AddEvidence(evidence, career.Workplaces);

        return evidence;
    }

    private static void AddEvidence(HashSet<string> evidence, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            evidence.Add(value.Trim());
    }

    private static void AddEvidence(HashSet<string> evidence, IEnumerable<string>? values)
    {
        if (values is null)
            return;

        foreach (var value in values)
            AddEvidence(evidence, value);
    }

    private static bool MatchesSignalToCareer(string signal, HashSet<string> evidence)
    {
        if (evidence.Contains(signal))
            return true;

        var normalizedSignal = Normalize(signal);
        var normalizedEvidence = evidence.Select(Normalize).ToList();

        // Keep aliases deliberately narrow. A broad alias such as
        // Mathematics -> Engineering was the reason careers such as
        // Biomedical Engineer could score too highly.
        string[] aliases = normalizedSignal switch
        {
            "creating technology" => [
                "technology", "computer science", "information technology",
                "software", "software development", "programming", "coding",
                "artificial intelligence", "machine learning", "cybersecurity"
            ],
            "working with computers" => [
                "computers", "computer science", "information technology", "technology",
                "software", "software development", "programming", "coding"
            ],
            "technology problems" => [
                "technology", "computer science", "information technology",
                "software", "software development", "programming", "coding",
                "artificial intelligence", "machine learning", "cybersecurity"
            ],
            "technology" => [
                "technology", "computer science", "information technology",
                "software", "software development", "programming", "coding",
                "artificial intelligence", "machine learning", "cybersecurity"
            ],
            "solving puzzles" => [
                "solving puzzles", "problem-solving", "problem solving", "logical thinking",
                "programming", "debugging", "system design"
            ],
            "logical thinking" => [
                "logical thinking", "problem-solving", "problem solving", "programming",
                "debugging", "statistics", "probability"
            ],
            "problem solving" => [
                "problem-solving", "problem solving", "logical thinking", "programming",
                "debugging", "system design"
            ],
            "working with numbers" => [
                "working with numbers", "mathematics", "statistics", "probability",
                "financial modelling", "financial modeling", "accounting"
            ],
            "business & economics" => [
                "business & economics", "business & finance", "business", "finance",
                "management", "economics", "markets", "corporate"
            ],
            "business & finance" => [
                "business & finance", "business & economics", "business", "finance",
                "management", "economics", "markets", "financial"
            ],
            "understanding money and investments" => [
                "finance", "business & finance", "markets", "investments", "investment",
                "financial modelling", "financial modeling", "fintech"
            ],
            "financial problems" => [
                "finance", "business & finance", "financial", "financial modelling",
                "financial modeling", "accounting", "markets", "investments"
            ],
            "research and discovery" => [
                "research and discovery", "science & research", "research", "laboratory",
                "statistics", "probability"
            ],
            "science & research" => [
                "science & research", "research", "science", "laboratory", "research and discovery"
            ],
            "mathematics" => [
                "mathematics", "statistics", "probability", "quantitative",
                "financial modelling", "financial modeling", "mathematical"
            ],
            "computers" => [
                "computers", "computer science", "information technology", "technology",
                "software", "programming", "coding"
            ],
            _ => []
        };

        if (aliases.Length == 0)
            return normalizedEvidence.Contains(normalizedSignal);

        return aliases.Any(alias => normalizedEvidence.Any(evidenceValue =>
            evidenceValue.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
            evidenceValue.Contains(alias, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsMathHeavyCareer(HashSet<string> evidence)
    {
        var mathTerms = new[]
        {
            "mathematics", "statistics", "probability", "quantitative",
            "financial modelling", "financial modeling", "mathematical"
        };

        return evidence.Select(Normalize).Any(value =>
            mathTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private static int GetMissingDomainPenalty(HashSet<string> evidence, Dictionary<string, int> signals)
    {
        var penalty = 0;

        if (ContainsAny(evidence, "finance", "markets", "investment", "investments", "financial modelling", "financial modeling", "fintech", "quantitative") &&
            !HasAnyPositiveSignal(signals,
                "business & economics", "business & finance", "understanding money and investments",
                "financial problems", "business problems", "working with numbers"))
        {
            penalty += 5;
        }

        // Healthcare/medical careers need a health or biology signal. This
        // prevents Biomedical Engineer and similar careers from winning on
        // generic mathematics + problem-solving overlap.
        if (ContainsAny(evidence, "healthcare", "medicine", "hospital", "clinical", "medical", "biomedical") &&
            !HasAnyPositiveSignal(signals,
                "biology", "discovering how the human body works", "healthcare",
                "health-related problems", "helping people"))
        {
            penalty += 5;
        }

        if (ContainsAny(evidence, "law", "legal", "public service", "government") &&
            !HasAnyPositiveSignal(signals, "law & public service", "social problems", "social studies"))
        {
            penalty += 4;
        }

        if (ContainsAny(evidence, "design & media", "art & design", "creative studio", "creating designs") &&
            !HasAnyPositiveSignal(signals, "art & design", "design & media", "designing creative things", "design problems", "creativity"))
        {
            penalty += 4;
        }

        if (ContainsAny(evidence, "education", "teaching") &&
            !HasAnyPositiveSignal(signals, "education", "helping people", "communication"))
        {
            penalty += 3;
        }

        return penalty;
    }

    private static bool HasAnyPositiveSignal(Dictionary<string, int> signals, params string[] names)
    {
        return names.Any(name => signals.TryGetValue(name, out var weight) && weight > 0);
    }

    private static bool ContainsAny(HashSet<string> evidence, params string[] terms)
    {
        return evidence.Select(Normalize).Any(value =>
            terms.Any(term => value.Contains(Normalize(term), StringComparison.OrdinalIgnoreCase)));
    }

    private static string Normalize(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static string BuildWhyItMatches(List<string> matchedTags)
    {
        if (matchedTags.Count == 0)
            return "This career is a popular starting point worth exploring.";

        var shown = matchedTags.Take(4).ToList();
        var bullets = string.Join(" · ", shown.Select(t => $"✓ {t}"));
        return $"You selected: {bullets}\n\nThese are strong signals for this career direction.";
    }

    private static readonly List<CareerQuestion> _questions = BuildQuestions();

    private static List<CareerQuestion> BuildQuestions()
    {
        var questions = new List<CareerQuestion>
        {
            new CareerQuestion
            {
                Number = 1,
                Text = "What subjects do you enjoy the most?",
                AllowMultiple = true,
                Options =
                [
                    OptSubject("q1-math", "Mathematics", "🔢", "Mathematics"),
                    OptSubject("q1-science", "Science", "🔬", "Science"),
                    OptSubject("q1-biology", "Biology", "🧬", "Biology"),
                    OptSubject("q1-computers", "Computers", "💻", "Computers"),
                    OptSubject("q1-business", "Business & Economics", "📈", "Business & Economics"),
                    OptSubject("q1-languages", "Languages", "🗣️", "Languages"),
                    OptSubject("q1-art", "Art & Design", "🎨", "Art & Design"),
                    OptSubject("q1-social", "Social Studies", "🌍", "Social Studies"),
                ]
            },
            new CareerQuestion
            {
                Number = 2,
                Text = "What kind of activities do you enjoy?",
                AllowMultiple = true,
                Options =
                [
                    OptActivity("q2-puzzles", "Solving puzzles", "🧩", "Solving puzzles"),
                    OptActivity("q2-building", "Building things", "🛠️", "Building things"),
                    OptActivity("q2-helping", "Helping people", "🤝", "Helping people"),
                    OptActivity("q2-designing", "Creating designs", "🖌️", "Creating designs"),
                    OptActivity("q2-computers", "Working with computers", "💻", "Working with computers"),
                    OptActivity("q2-leading", "Leading a team", "👥", "Leading a team"),
                    OptActivity("q2-writing", "Writing or communicating", "✍️", "Writing or communicating"),
                    OptActivity("q2-understanding", "Understanding how things work", "⚙️", "Understanding how things work"),
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
                ]
            }
        };

        foreach (var question in questions)
        {
            question.Options.Add(new CareerQuestionOption
            {
                Id = $"q{question.Number}-none",
                Text = "None of the above",
                Icon = "➖",
                TagWeights = []
            });
        }

        return questions;
    }

    private static CareerQuestionOption Opt(string id, string text, string icon, string tag) =>
        new()
        {
            Id = id,
            Text = text,
            Icon = icon,
            TagWeights = [new CareerTagWeight { Tag = tag, Weight = 2 }]
        };

    private static CareerQuestionOption OptSubject(string id, string text, string icon, string tag) =>
        new()
        {
            Id = id,
            Text = text,
            Icon = icon,
            TagWeights = [new CareerTagWeight { Tag = tag, Weight = 1 }]
        };

    private static CareerQuestionOption OptActivity(string id, string text, string icon, string tag) =>
        new()
        {
            Id = id,
            Text = text,
            Icon = icon,
            TagWeights = [new CareerTagWeight { Tag = tag, Weight = 3 }]
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
