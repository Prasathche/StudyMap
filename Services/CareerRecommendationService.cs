using StudyMap.Models;

namespace StudyMap.Services;

/// <summary>
/// Rule-based career discovery for Class 8-12 students.
/// The assessment focuses on interests, activities, problem types and strengths
/// rather than asking students to already know professional career names.
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

        var positiveWeightTotal = tagScores.Values.Where(v => v > 0).Sum();
        var maxPossibleScore = Math.Max(positiveWeightTotal, 1);
        var scored = new List<CareerRecommendation>();

        foreach (var career in careers)
        {
            var evidence = GetCareerEvidence(career);
            var score = 0.0;
            var matchedSignals = new List<string>();

            foreach (var signal in tagScores)
            {
                if (signal.Value <= 0)
                    continue;

                if (MatchesSignalToCareer(signal.Key, evidence))
                {
                    score += signal.Value;
                    matchedSignals.Add(signal.Key);
                }
            }

            // Mathematics is a supporting signal, not a career selector.
            // Students who dislike Maths should be less likely to receive
            // strongly mathematics-dependent careers.
            if (tagScores.TryGetValue("Mathematics", out var mathWeight) && mathWeight < 0 &&
                IsMathHeavyCareer(evidence))
            {
                score += mathWeight * 2;
            }

            // A specialist direction must have direct evidence. This is the
            // important guard against Biomedical Engineer winning from generic
            // Maths + puzzles + computers alone.
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
        var normalizedSignal = Normalize(signal);
        var normalizedEvidence = evidence.Select(Normalize).ToList();

        // These are intentionally directional and narrow. Broad aliases such as
        // Mathematics -> Engineering are avoided because they create false matches.
        string[] aliases = normalizedSignal switch
        {
            "technology" => ["technology", "computer science", "information technology", "software", "programming", "coding", "artificial intelligence", "machine learning", "cybersecurity", "data science"],
            "engineering" => ["engineering", "mechanical", "civil", "electrical", "electronics", "aerospace", "automotive", "robotics", "mechatronics"],
            "healthcare" => ["healthcare", "medicine", "medical", "hospital", "clinical", "biomedical", "nursing", "pharmacy", "physiotherapy", "dentistry"],
            "science & research" => ["science & research", "science", "research", "laboratory", "laboratory / research"],
            "design & media" => ["design & media", "art & design", "design", "media", "creative", "animation", "film", "visual"],
            "business & finance" => ["business & finance", "business", "finance", "economics", "commerce", "accounting", "marketing", "management", "corporate"],
            "law & public service" => ["law & public service", "law", "legal", "government", "public service", "civil services", "policy"],
            "education" => ["education", "teaching", "teacher", "academic", "professor", "lecturer"],
            "environment & sustainability" => ["environment", "environmental", "sustainability", "agriculture", "forestry", "wildlife", "conservation"],
            "communication" => ["communication", "writing", "languages", "journalism", "media", "public relations", "content"],
            "helping people" => ["healthcare", "medicine", "education", "teaching", "social", "community", "counselling", "psychology", "public service"],
            "creativity" => ["design", "creative", "media", "art", "animation", "film", "fashion", "architecture"],
            "leadership" => ["leadership", "management", "business", "corporate", "public service", "government"],
            "mathematics" => ["mathematics", "statistics", "probability", "quantitative", "financial modelling", "financial modeling", "mathematical"],
            "analytical thinking" => ["analytical", "analysis", "problem-solving", "problem solving", "logical thinking", "statistics", "research", "data"],
            "building & practical work" => ["engineering", "construction", "architecture", "manufacturing", "automotive", "mechanical", "civil", "workshop"],
            "curiosity & research" => ["research", "science", "laboratory", "analysis", "discovery"],
            "nature & environment" => ["environment", "environmental", "sustainability", "agriculture", "forestry", "wildlife", "conservation", "outdoors"],
            "working with numbers" => ["mathematics", "statistics", "accounting", "finance", "economics", "quantitative", "data"],
            _ => [normalizedSignal]
        };

        return aliases.Any(alias => normalizedEvidence.Any(value =>
            value.Equals(Normalize(alias), StringComparison.OrdinalIgnoreCase) ||
            value.Contains(Normalize(alias), StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsMathHeavyCareer(HashSet<string> evidence)
    {
        var mathTerms = new[] { "mathematics", "statistics", "probability", "quantitative", "financial modelling", "financial modeling", "mathematical" };
        return evidence.Select(Normalize).Any(value => mathTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private static int GetMissingDomainPenalty(HashSet<string> evidence, Dictionary<string, int> signals)
    {
        var penalty = 0;

        if (ContainsAny(evidence, "healthcare", "medicine", "hospital", "clinical", "medical", "biomedical") &&
            !HasAnyPositiveSignal(signals, "Biology", "Healthcare", "Helping people", "Understanding the human body", "Health-related problems"))
            penalty += 8;

        if (ContainsAny(evidence, "finance", "markets", "investment", "financial modelling", "fintech", "quantitative") &&
            !HasAnyPositiveSignal(signals, "Business & Finance", "Understanding money and investments", "Business problems", "Financial problems", "Working with numbers"))
            penalty += 7;

        if (ContainsAny(evidence, "law", "legal", "public service", "government") &&
            !HasAnyPositiveSignal(signals, "Law & Public Service", "Social problems", "Social Studies", "People & Community"))
            penalty += 6;

        if (ContainsAny(evidence, "design & media", "art & design", "creative studio", "animation", "film") &&
            !HasAnyPositiveSignal(signals, "Design & Media", "Creativity", "Design problems", "Creating designs"))
            penalty += 6;

        if (ContainsAny(evidence, "education", "teaching", "teacher", "professor", "lecturer") &&
            !HasAnyPositiveSignal(signals, "Education", "Teaching & explaining", "Helping people", "Communication"))
            penalty += 5;

        return penalty;
    }

    private static bool HasAnyPositiveSignal(Dictionary<string, int> signals, params string[] names)
    {
        return names.Any(name => signals.TryGetValue(name, out var weight) && weight > 0);
    }

    private static bool ContainsAny(HashSet<string> evidence, params string[] terms)
    {
        return evidence.Select(Normalize).Any(value => terms.Any(term => value.Contains(Normalize(term), StringComparison.OrdinalIgnoreCase)));
    }

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string BuildWhyItMatches(List<string> matchedTags)
    {
        if (matchedTags.Count == 0)
            return "This is a career worth exploring further.";

        var shown = matchedTags
            .Where(t => !t.Equals("Mathematics", StringComparison.OrdinalIgnoreCase))
            .Take(4)
            .ToList();

        if (shown.Count == 0)
            shown = matchedTags.Take(4).ToList();

        var bullets = string.Join(" · ", shown.Select(t => $"✓ {t}"));
        return $"Your answers point towards: {bullets}\n\nThese are strong signals for this career direction. Explore the career to learn what it involves and which subjects can help you get there.";
    }

    private static readonly List<CareerQuestion> _questions = BuildQuestions();

    private static List<CareerQuestion> BuildQuestions()
    {
        var questions = new List<CareerQuestion>
        {
            new CareerQuestion
            {
                Number = 1,
                Text = "Which school subjects do you enjoy the most?",
                AllowMultiple = true,
                Options =
                [
                    OptSubject("q1-math", "Mathematics", "🔢", "Mathematics"),
                    OptSubject("q1-science", "Science", "🔬", "Science & Research"),
                    OptSubject("q1-biology", "Biology", "🧬", "Biology"),
                    OptSubject("q1-computers", "Computers", "💻", "Technology"),
                    OptSubject("q1-business", "Business & Economics", "📈", "Business & Finance"),
                    OptSubject("q1-languages", "Languages", "🗣️", "Communication"),
                    OptSubject("q1-art", "Art & Design", "🎨", "Design & Media"),
                    OptSubject("q1-social", "Social Studies", "🌍", "Law & Public Service"),
                ]
            },
            new CareerQuestion
            {
                Number = 2,
                Text = "What do you enjoy doing in your free time?",
                AllowMultiple = true,
                Options =
                [
                    OptActivity("q2-puzzles", "Solving puzzles or brain teasers", "🧩", "Analytical Thinking"),
                    OptActivity("q2-building", "Building or fixing things", "🛠️", "Building & Practical Work"),
                    OptActivity("q2-helping", "Helping or caring for people", "🤝", "Helping People"),
                    OptActivity("q2-designing", "Drawing, designing or creating", "🎨", "Creativity"),
                    OptActivity("q2-computers", "Using computers or technology", "💻", "Technology"),
                    OptActivity("q2-leading", "Organising or leading a group", "👥", "Leadership"),
                    OptActivity("q2-writing", "Writing, speaking or telling stories", "✍️", "Communication"),
                    OptActivity("q2-understanding", "Finding out how things work", "⚙️", "Curiosity & Research"),
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
                    OptWeighted("q3-not", "I don't enjoy it", "😕", "Mathematics", -3),
                ]
            },
            new CareerQuestion
            {
                Number = 4,
                Text = "Which of these would you most like to spend time doing?",
                AllowMultiple = true,
                Options =
                [
                    OptWeighted("q4-technology", "Create an app, website or other technology", "💻", "Technology", 4),
                    OptWeighted("q4-research", "Do experiments and discover how things work", "🔬", "Science & Research", 4),
                    OptWeighted("q4-body", "Understand the human body and help people stay healthy", "🩺", "Healthcare", 4),
                    OptWeighted("q4-engineering", "Design and build machines or useful things", "⚙️", "Engineering", 4),
                    OptWeighted("q4-design", "Create drawings, designs, videos or animations", "🎨", "Design & Media", 4),
                    OptWeighted("q4-environment", "Protect nature and solve environmental problems", "🌱", "Environment & Sustainability", 4),
                    OptWeighted("q4-business", "Start or run a business and make decisions", "💼", "Business & Finance", 4),
                    OptWeighted("q4-law", "Solve social problems and help through rules or laws", "⚖️", "Law & Public Service", 4),
                    OptWeighted("q4-education", "Teach, explain things and help others learn", "📚", "Education", 4),
                ]
            },
            new CareerQuestion
            {
                Number = 5,
                Text = "Which kind of problem would you enjoy solving?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q5-technology", "A technology or computer problem", "💻", "Technology"),
                    Opt("q5-building", "A machine, structure or practical problem", "🛠️", "Engineering"),
                    Opt("q5-health", "A health or wellbeing problem", "🩺", "Healthcare"),
                    Opt("q5-business", "A business, money or customer problem", "💼", "Business & Finance"),
                    Opt("q5-environment", "A nature or environmental problem", "🌱", "Environment & Sustainability"),
                    Opt("q5-social", "A people or community problem", "🌍", "People & Community"),
                    Opt("q5-design", "A design or creative problem", "🎨", "Design & Media"),
                    Opt("q5-learning", "A learning or teaching problem", "📚", "Education"),
                ]
            },
            new CareerQuestion
            {
                Number = 6,
                Text = "Where would you enjoy spending time while doing your work?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q6-office", "Office with computers and technology", "💻", "Technology"),
                    Opt("q6-lab", "Laboratory where I can test and discover things", "🔬", "Science & Research"),
                    Opt("q6-hospital", "Hospital or healthcare setting", "🏥", "Healthcare"),
                    Opt("q6-workshop", "Workshop, site or place where things are built", "🛠️", "Engineering"),
                    Opt("q6-studio", "Creative studio or media space", "🎨", "Design & Media"),
                    Opt("q6-outdoors", "Outdoors, nature or environmental settings", "🌳", "Environment & Sustainability"),
                    Opt("q6-school", "School or learning environment", "📚", "Education"),
                    Opt("q6-government", "Government, court or public service setting", "🏛️", "Law & Public Service"),
                ]
            },
            new CareerQuestion
            {
                Number = 7,
                Text = "Which of these do you think you are naturally good at?",
                AllowMultiple = true,
                Options =
                [
                    Opt("q7-logical", "Thinking logically and spotting patterns", "🧠", "Analytical Thinking"),
                    Opt("q7-creativity", "Coming up with creative ideas", "🎨", "Creativity"),
                    Opt("q7-building", "Making, building or working with things", "🛠️", "Building & Practical Work"),
                    Opt("q7-communication", "Explaining ideas and communicating", "🗣️", "Communication"),
                    Opt("q7-people", "Understanding and working with people", "🤝", "Helping People"),
                    Opt("q7-numbers", "Working with numbers", "🔢", "Working with Numbers"),
                    Opt("q7-leadership", "Taking the lead and organising others", "👑", "Leadership"),
                    Opt("q7-curiosity", "Asking questions and finding out why", "🔍", "Curiosity & Research"),
                ]
            },
            new CareerQuestion
            {
                Number = 8,
                Text = "Which areas would you like to explore more?",
                AllowMultiple = true,
                Options =
                [
                    OptWeighted3("q8-technology", "Technology and computers", "💻", "Technology"),
                    OptWeighted3("q8-healthcare", "Health and the human body", "🩺", "Healthcare"),
                    OptWeighted3("q8-engineering", "Engineering and how things are built", "⚙️", "Engineering"),
                    OptWeighted3("q8-science", "Science and discovery", "🔬", "Science & Research"),
                    OptWeighted3("q8-design", "Design, art and media", "🎨", "Design & Media"),
                    OptWeighted3("q8-business", "Business, money and entrepreneurship", "💼", "Business & Finance"),
                    OptWeighted3("q8-law", "Law, government and public service", "⚖️", "Law & Public Service"),
                    OptWeighted3("q8-education", "Teaching and learning", "📚", "Education"),
                    OptWeighted3("q8-environment", "Environment, nature and sustainability", "🌱", "Environment & Sustainability"),
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
        new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = 3 }] };

    private static CareerQuestionOption OptSubject(string id, string text, string icon, string tag) =>
        new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = 1 }] };

    private static CareerQuestionOption OptActivity(string id, string text, string icon, string tag) =>
        new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = 3 }] };

    private static CareerQuestionOption OptWeighted(string id, string text, string icon, string tag, int weight) =>
        new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = weight }] };

    private static CareerQuestionOption OptWeighted3(string id, string text, string icon, string tag) =>
        new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = 3 }] };
}
