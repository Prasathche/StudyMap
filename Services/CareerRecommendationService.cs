using StudyMap.Models;

namespace StudyMap.Services;

/// <summary>
/// Rule-based career discovery for Class 8-12 students.
/// The assessment uses school subjects, interests, activities, learning style,
/// natural strengths, broad areas to explore and light career-priority signals.
/// </summary>
public class CareerRecommendationService
{
    private readonly CareersService _careersService;

    public CareerRecommendationService(CareersService? careersService = null)
    {
        _careersService = careersService ?? new CareersService();
    }

    public IReadOnlyList<CareerQuestion> Questions => _questions;

    public async Task<List<CareerRecommendation>> GetRecommendationsAsync(IEnumerable<CareerAnswer> answers, int topCount = 5)
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

                if (MatchesSignalToCareer(signal.Key, evidence, career))
                {
                    score += signal.Value;
                    matchedSignals.Add(signal.Key);
                }
            }

            if (tagScores.TryGetValue("Mathematics", out var mathWeight) && mathWeight < 0 && IsMathHeavyCareer(evidence))
                score += mathWeight * 2;

            score -= GetMissingDomainPenalty(evidence, tagScores);

            if (score <= 0)
                continue;

            var coverage = Math.Clamp(score / maxPossibleScore, 0.0, 1.0);
            var percentage = 35 + (int)Math.Round(60 * coverage);
            percentage = Math.Clamp(percentage, 35, 95);

            var distinctMatches = matchedSignals.Distinct(StringComparer.OrdinalIgnoreCase).Take(6).ToList();

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
            ranked = careers.Where(c => c.IsPopular).OrderBy(c => c.Name).Take(topCount)
                .Select(c => new CareerRecommendation
                {
                    Career = c,
                    Score = 0,
                    MatchPercentage = 0,
                    MatchedTags = [],
                    WhyItMatches = "A popular career area many students explore first."
                }).ToList();
        }

        return ranked;
    }

    private static Dictionary<string, int> BuildTagScores(IEnumerable<CareerAnswer> answers)
    {
        var scores = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var answer in answers)
        {
            var question = _questions.FirstOrDefault(q => q.Number == answer.QuestionNumber);
            if (question is null) continue;

            foreach (var optionId in answer.SelectedOptionIds)
            {
                var option = question.Options.FirstOrDefault(o => o.Id == optionId);
                if (option is null) continue;

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
        AddEvidence(evidence, career.FutureDemand);
        AddEvidence(evidence, career.SalaryRangeIndia);
        return evidence;
    }

    private static void AddEvidence(HashSet<string> evidence, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) evidence.Add(value.Trim());
    }

    private static void AddEvidence(HashSet<string> evidence, IEnumerable<string>? values)
    {
        if (values is null) return;
        foreach (var value in values) AddEvidence(evidence, value);
    }

    private static bool MatchesSignalToCareer(string signal, HashSet<string> evidence, Career career)
    {
        var normalizedSignal = Normalize(signal);
        var normalizedEvidence = evidence.Select(Normalize).ToList();

        if (normalizedSignal == "high salary")
            return HasHighSalaryPotential(career);
        if (normalizedSignal == "stability")
            return HasStableCareerPotential(career);
        if (normalizedSignal == "career growth")
            return HasGrowthPotential(career);
        if (normalizedSignal == "enjoyment")
            return false;

        string[] aliases = normalizedSignal switch
        {
            "technology" => ["technology", "computer science", "information technology", "software", "programming", "coding", "artificial intelligence", "machine learning", "cybersecurity", "data science"],
            "engineering" => ["engineering", "mechanical", "civil", "electrical", "electronics", "aerospace", "automotive", "robotics", "mechatronics"],
            "healthcare" => ["healthcare", "medicine", "medical", "hospital", "clinical", "biomedical", "nursing", "pharmacy", "physiotherapy", "dentistry"],
            "biology" => ["biology", "biological", "life science", "life sciences", "biomedical", "medicine", "medical", "botany", "zoology", "genetics"],
            "science & research" => ["science & research", "science", "research", "laboratory", "laboratory / research"],
            "design & media" => ["design & media", "art & design", "design", "media", "creative", "animation", "film", "visual", "fashion"],
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

    private static bool HasHighSalaryPotential(Career career)
    {
        var text = career.SalaryRangeIndia ?? string.Empty;
        var numbers = System.Text.RegularExpressions.Regex.Matches(text, @"\d+(?:\.\d+)?")
            .Select(m => double.TryParse(m.Value, out var n) ? n : 0)
            .ToList();
        return numbers.Any(n => n >= 12);
    }

    private static bool HasStableCareerPotential(Career career)
    {
        var demand = Normalize(career.FutureDemand ?? string.Empty);
        return demand is "very high" or "high" or "stable" ||
               career.CareerCategories.Any(x => Normalize(x).Contains("government") || Normalize(x).Contains("public service"));
    }

    private static bool HasGrowthPotential(Career career)
    {
        var demand = Normalize(career.FutureDemand ?? string.Empty);
        return demand is "very high" or "high";
    }

    private static bool IsMathHeavyCareer(HashSet<string> evidence)
    {
        var mathTerms = new[] { "mathematics", "statistics", "probability", "quantitative", "financial modelling", "financial modeling", "mathematical" };
        return evidence.Select(Normalize).Any(value => mathTerms.Any(term => value.Contains(term, StringComparison.OrdinalIgnoreCase)));
    }

    private static int GetMissingDomainPenalty(HashSet<string> evidence, Dictionary<string, int> signals)
    {
        var penalty = 0;
        if (ContainsAny(evidence, "healthcare", "medicine", "hospital", "clinical", "medical", "biomedical") && !HasAnyPositiveSignal(signals, "Biology", "Healthcare", "Helping People")) penalty += 8;
        if (ContainsAny(evidence, "finance", "markets", "investment", "financial modelling", "fintech", "quantitative") && !HasAnyPositiveSignal(signals, "Business & Finance", "Working with Numbers")) penalty += 7;
        if (ContainsAny(evidence, "law", "legal", "public service", "government") && !HasAnyPositiveSignal(signals, "Law & Public Service", "Helping People", "Communication")) penalty += 6;
        if (ContainsAny(evidence, "design & media", "art & design", "creative studio", "animation", "film") && !HasAnyPositiveSignal(signals, "Design & Media", "Creativity")) penalty += 6;
        if (ContainsAny(evidence, "education", "teaching", "teacher", "professor", "lecturer") && !HasAnyPositiveSignal(signals, "Education", "Helping People", "Communication")) penalty += 5;
        return penalty;
    }

    private static bool HasAnyPositiveSignal(Dictionary<string, int> signals, params string[] names) =>
        names.Any(name => signals.TryGetValue(name, out var weight) && weight > 0);

    private static bool ContainsAny(HashSet<string> evidence, params string[] terms) =>
        evidence.Select(Normalize).Any(value => terms.Any(term => value.Contains(Normalize(term), StringComparison.OrdinalIgnoreCase)));

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string BuildWhyItMatches(List<string> matchedTags)
    {
        if (matchedTags.Count == 0) return "This is a career worth exploring further.";
        var shown = matchedTags.Where(t => !t.Equals("Mathematics", StringComparison.OrdinalIgnoreCase)).Take(4).ToList();
        if (shown.Count == 0) shown = matchedTags.Take(4).ToList();
        var bullets = string.Join(" · ", shown.Select(t => $"✓ {t}"));
        return $"Your answers point towards: {bullets}\n\nThese are strong signals for this career direction. Explore the career to learn what it involves and which subjects can help you get there.";
    }

    private static readonly List<CareerQuestion> _questions = BuildQuestions();

    private static List<CareerQuestion> BuildQuestions()
    {
        var questions = new List<CareerQuestion>
        {
            new CareerQuestion { Number = 1, Text = "Which subjects do you enjoy studying the most?", AllowMultiple = true, Options =
            [
                OptSubject("q1-math", "Mathematics", "🔢", "Mathematics"), OptSubject("q1-science", "Science", "🔬", "Science & Research"), OptSubject("q1-computers", "Computer / IT", "💻", "Technology"), OptSubject("q1-biology", "Biology", "🧬", "Biology"), OptSubject("q1-languages", "English / Languages", "🗣️", "Communication"), OptSubject("q1-social", "Social Science", "🌍", "Law & Public Service"), OptSubject("q1-business", "Business / Commerce", "📈", "Business & Finance"), OptSubject("q1-economics", "Economics", "📊", "Business & Finance"), OptSubject("q1-art", "Art / Design", "🎨", "Design & Media")
            ]},
            new CareerQuestion { Number = 2, Text = "What do you usually enjoy doing when you have free time?", AllowMultiple = true, Options =
            [
                OptActivity("q2-sports", "Playing games or sports", "🏃", "Sports"), OptActivity("q2-technology", "Using computers or mobile apps", "💻", "Technology"), OptActivity("q2-media", "Watching or making videos", "🎬", "Design & Media"), OptActivity("q2-creative", "Drawing, designing or creating things", "🎨", "Creativity"), OptActivity("q2-reading", "Reading or learning about interesting topics", "📚", "Curiosity & Research"), OptActivity("q2-puzzles", "Solving puzzles or brain teasers", "🧩", "Analytical Thinking"), OptActivity("q2-helping", "Helping friends or family", "🤝", "Helping People"), OptActivity("q2-social", "Talking and spending time with people", "🗣️", "Communication"), OptActivity("q2-building", "Building or fixing things", "🛠️", "Building & Practical Work"), OptActivity("q2-why", "Exploring things and asking ‘why?’", "🔍", "Curiosity & Research")
            ]},
            new CareerQuestion { Number = 3, Text = "If you could spend a day learning something outside your regular school classes, which would you choose?", AllowMultiple = true, Options =
            [
                OptWeighted("q3-technology", "Make an app, website or game", "💻", "Technology", 4), OptWeighted("q3-science", "Do science experiments and discover how things work", "🔬", "Science & Research", 4), OptWeighted("q3-engineering", "Design or build something", "🏗️", "Engineering", 4), OptWeighted("q3-health", "Learn about the human body and health", "🩺", "Healthcare", 4), OptWeighted("q3-environment", "Explore animals, plants or the environment", "🌱", "Environment & Sustainability", 4), OptWeighted("q3-creative", "Draw, design, photograph or create something", "🎨", "Design & Media", 4), OptWeighted("q3-media", "Make a video, animation or other media", "🎬", "Design & Media", 4), OptWeighted("q3-business", "Try running a small business or learn how money works", "💼", "Business & Finance", 4), OptWeighted("q3-people", "Explore how people think, learn and behave", "🧠", "Helping People", 4), OptWeighted("q3-society", "Learn about law, society, government or current affairs", "⚖️", "Law & Public Service", 4), OptWeighted("q3-teaching", "Teach or help another student learn something", "🧑‍🏫", "Education", 4), OptWeighted("q3-culture", "Learn about countries, cultures or languages", "🌍", "Communication", 4)
            ]},
            new CareerQuestion { Number = 4, Text = "Which type of school project would you enjoy doing?", AllowMultiple = true, Options =
            [
                OptWeighted("q4-technology", "Create an app, website or computer project", "💻", "Technology", 4), OptWeighted("q4-building", "Build a model, machine or working prototype", "🛠️", "Engineering", 4), OptWeighted("q4-experiment", "Conduct an experiment and explain the result", "🔬", "Science & Research", 4), OptWeighted("q4-research", "Research a topic and present what I discovered", "📚", "Curiosity & Research", 4), OptWeighted("q4-creative", "Create a drawing, poster, video or animation", "🎨", "Design & Media", 4), OptWeighted("q4-business", "Organise an event or try a small business idea", "💼", "Business & Finance", 4), OptWeighted("q4-social", "Study a problem affecting people or society", "🌍", "Law & Public Service", 4), OptWeighted("q4-nature", "Study plants, animals, nature or the environment", "🌱", "Environment & Sustainability", 4), OptWeighted("q4-teaching", "Teach something to my classmates", "🧑‍🏫", "Education", 4)
            ]},
            new CareerQuestion { Number = 5, Text = "When you face a difficult problem, what do you naturally tend to do?", AllowMultiple = false, Options =
            [
                Opt("q5-breakdown", "Break it into smaller parts and solve it step by step", "🧩", "Analytical Thinking"), Opt("q5-ideas", "Try different ideas until something works", "💡", "Creativity"), Opt("q5-research", "Search and learn more about it", "🔍", "Curiosity & Research"), Opt("q5-discuss", "Discuss it with someone and get their ideas", "🗣️", "Communication"), Opt("q5-visual", "Draw or visualise the problem", "✏️", "Creativity"), Opt("q5-numbers", "Look at the numbers and calculations", "🔢", "Working with Numbers"), Opt("q5-people", "Think about how it affects people", "🤝", "Helping People"), Opt("q5-guide", "Prefer someone to guide me", "🧑‍🏫", "Education")
            ]},
            new CareerQuestion { Number = 6, Text = "If you could choose one school activity for a whole afternoon, what would you pick?", AllowMultiple = true, Options =
            [
                OptActivity("q6-experiment", "Do an experiment or science activity", "🔬", "Science & Research"), OptActivity("q6-computer", "Make something using a computer", "💻", "Technology"), OptActivity("q6-building", "Build or make something with my hands", "🛠️", "Building & Practical Work"), OptActivity("q6-design", "Draw, design or create something", "🎨", "Creativity"), OptActivity("q6-research", "Read, research and learn about a topic", "📚", "Curiosity & Research"), OptActivity("q6-debate", "Debate, discuss or give a presentation", "🗣️", "Communication"), OptActivity("q6-sport", "Play a sport or take part in a physical activity", "🏃", "Sports"), OptActivity("q6-nature", "Do something related to plants, animals or nature", "🌱", "Nature & Environment"), OptActivity("q6-help", "Help or work with other students", "🤝", "Helping People"), OptActivity("q6-performance", "Act, perform, make music or create videos", "🎭", "Design & Media")
            ]},
            new CareerQuestion { Number = 7, Text = "What do you enjoy most when you finally understand something difficult?", AllowMultiple = false, Options =
            [
                Opt("q7-how", "Finding out how or why something works", "💡", "Curiosity & Research"), Opt("q7-solve", "Solving the problem myself", "🧩", "Analytical Thinking"), Opt("q7-creative", "Finding a creative way to explain or show it", "🎨", "Creativity"), Opt("q7-explain", "Explaining it to someone else", "🗣️", "Communication"), Opt("q7-experiment", "Finding the answer by experimenting or researching", "🔬", "Science & Research"), Opt("q7-numbers", "Getting the correct answer using numbers or logic", "🔢", "Working with Numbers"), Opt("q7-help", "Understanding how it can help someone", "🤝", "Helping People"), Opt("q7-make", "Using what I learned to make something", "🛠️", "Building & Practical Work")
            ]},
            new CareerQuestion { Number = 8, Text = "When teachers or friends ask you for help, what do they usually ask you to help with?", AllowMultiple = true, Options =
            [
                OptActivity("q8-maths", "Maths or solving difficult problems", "🔢", "Working with Numbers"), OptActivity("q8-science", "Science or experiments", "🔬", "Science & Research"), OptActivity("q8-technology", "Computers, apps or technology", "💻", "Technology"), OptActivity("q8-research", "Finding information or doing research", "📚", "Curiosity & Research"), OptActivity("q8-writing", "Writing, English or languages", "✍️", "Communication"), OptActivity("q8-design", "Drawing, designing or making presentations", "🎨", "Design & Media"), OptActivity("q8-speaking", "Explaining something or speaking in front of others", "🗣️", "Communication"), OptActivity("q8-helping", "Helping someone understand or solve a problem", "🤝", "Helping People"), OptActivity("q8-sports", "Sports or physical activities", "🏃", "Sports"), OptActivity("q8-organising", "Organising or coordinating something", "👥", "Leadership"), new CareerQuestionOption { Id = "q8-none", Text = "Usually they don't ask me for help with these things", Icon = "➖", TagWeights = [] }
            ]},
            new CareerQuestion { Number = 9, Text = "Which areas would you like to explore more?", AllowMultiple = true, Options =
            [
                OptWeighted3("q9-technology", "Technology & Computers", "💻", "Technology"), OptWeighted3("q9-engineering", "Engineering & Machines", "⚙️", "Engineering"), OptWeighted3("q9-science", "Science & Research", "🔬", "Science & Research"), OptWeighted3("q9-healthcare", "Medicine & Healthcare", "🩺", "Healthcare"), OptWeighted3("q9-business", "Business & Finance", "💼", "Business & Finance"), OptWeighted3("q9-design", "Design & Creative Work", "🎨", "Design & Media"), OptWeighted3("q9-law", "Law, Government & Public Service", "⚖️", "Law & Public Service"), OptWeighted3("q9-education", "Teaching & Education", "🧑‍🏫", "Education"), OptWeighted3("q9-environment", "Environment & Nature", "🌱", "Environment & Sustainability"), OptWeighted3("q9-media", "Media & Communication", "🎬", "Communication"), new CareerQuestionOption { Id = "q9-not-sure", Text = "I'm still exploring", Icon = "🤔", TagWeights = [] }
            ]},
            new CareerQuestion { Number = 10, Text = "When you think about your future career, what matters most to you?", AllowMultiple = true, Options =
            [
                OptWeighted("q10-salary", "A high-paying career", "💰", "High Salary", 1),
                OptWeighted("q10-stable", "A stable and secure career", "🛡️", "Stability", 1),
                OptWeighted("q10-enjoy", "A career I enjoy and find interesting", "❤️", "Enjoyment", 1),
                OptWeighted("q10-help", "A career where I can help people", "🤝", "Helping People", 1),
                OptWeighted("q10-growth", "A career with opportunities to grow", "📈", "Career Growth", 1),
                OptWeighted("q10-creative", "A career where I can be creative", "🎨", "Creativity", 1),
                OptWeighted("q10-technology", "A career where I can work with technology", "💻", "Technology", 1),
                OptWeighted("q10-difference", "A career where I can make a difference", "🌍", "Helping People", 1)
            ]}
        };

        foreach (var question in questions)
        {
            if (question.Options.Any(o => o.Id.EndsWith("-none", StringComparison.OrdinalIgnoreCase)))
                continue;
            question.Options.Add(new CareerQuestionOption { Id = $"q{question.Number}-none", Text = "None of the above", Icon = "➖", TagWeights = [] });
        }
        return questions;
    }

    private static CareerQuestionOption Opt(string id, string text, string icon, string tag) => new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = 3 }] };
    private static CareerQuestionOption OptSubject(string id, string text, string icon, string tag) => new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = 1 }] };
    private static CareerQuestionOption OptActivity(string id, string text, string icon, string tag) => new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = 3 }] };
    private static CareerQuestionOption OptWeighted(string id, string text, string icon, string tag, int weight) => new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = weight }] };
    private static CareerQuestionOption OptWeighted3(string id, string text, string icon, string tag) => new() { Id = id, Text = text, Icon = icon, TagWeights = [new CareerTagWeight { Tag = tag, Weight = 3 }] };
}
