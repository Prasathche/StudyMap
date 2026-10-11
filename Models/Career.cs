namespace StudyMap.Models;

public class Career
{
	public string Id { get; set; } = string.Empty;
	
	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	public string AccentColor { get; set; } = "#F6A64F";

	public bool IsPopular { get; set; }

	public string SalaryRangeIndia { get; set; } = string.Empty;

	public string FutureDemand { get; set; } = string.Empty;

	public string EducationPath { get; set; } = string.Empty;

	public List<string> SearchAliases { get; set; } = [];

	public List<string> EntranceExams { get; set; } = [];

	public List<string> KeySkills { get; set; } = [];

	public List<string> Workplaces { get; set; } = [];

	private List<string> _roadmapSteps = [];

	public List<string> RoadmapSteps
	{
		get => _roadmapSteps;
		set
		{
			_roadmapSteps = value ?? [];
			ApplyUndergraduateCourseGuidance();
		}
	}

	public List<string> SalaryInsights { get; set; } = [];

	public List<string> TopColleges { get; set; } = [];

	// Bonus features support
	public bool IsFavorite { get; set; }

	public bool IsRecentlyViewed { get; set; }

	public List<string> RelatedCareers { get; set; } = [];

	public string Duration { get; set; } = string.Empty;

	public string FutureScope { get; set; } = string.Empty;

	public List<string> RecommendedSubjects { get; set; } = [];

	// Career Discovery ("Not Sure What to Choose") recommendation matching
	public List<string> CareerCategories { get; set; } = [];

	public List<string> Interests { get; set; } = [];

	public List<string> PreferredSubjects { get; set; } = [];

	public List<string> WorkStyles { get; set; } = [];

	// Parent Insights — optional structured overrides.
	// When left empty, ParentInsightsService derives a sensible value from the fields above,
	// so existing careers.json entries keep working without any data migration.
	public int? SalaryMinLpa { get; set; }

	public int? SalaryMaxLpa { get; set; }

	public string SalaryDescription { get; set; } = string.Empty;

	public string StabilityLevel { get; set; } = string.Empty;

	public string StabilityDescription { get; set; } = string.Empty;

	public string FutureDemandLevel { get; set; } = string.Empty;

	public string FutureDemandDescription { get; set; } = string.Empty;

	public string DifficultyLevel { get; set; } = string.Empty;

	public string DifficultyDescription { get; set; } = string.Empty;

	public string CompetitionLevel { get; set; } = string.Empty;

	public string CompetitionDescription { get; set; } = string.Empty;

	public string TypicalUGDuration { get; set; } = string.Empty;

	public string EducationInvestment { get; set; } = string.Empty;

	public List<string> UGCourses { get; set; } = [];

	public List<string> PGCourses { get; set; } = [];

	public List<string> ParentConsiderations { get; set; } = [];

	public List<string> SuitableFor { get; set; } = [];

	public List<string> ThingsToConsider { get; set; } = [];

	private void ApplyUndergraduateCourseGuidance()
	{
		var guidance = GetUndergraduateCourseGuidance();
		if (string.IsNullOrWhiteSpace(guidance))
		{
			return;
		}

		// Roadmap stage 3 is the undergraduate stage used by CareerRoadmapViewModel.
		// Replace only that stage's description so every career gets a specific course
		// recommendation while the remaining roadmap data stays unchanged.
		while (_roadmapSteps.Count < 3)
		{
			_roadmapSteps.Add(string.Empty);
		}

		_roadmapSteps[2] = guidance;
	}

	private string GetUndergraduateCourseGuidance()
	{
		var id = (Id ?? string.Empty).Trim().ToLowerInvariant();
		var name = (Name ?? string.Empty).Trim().ToLowerInvariant();
		var key = $"{id} {name}";

		if (key.Contains("doctor") || key.Contains("physician"))
			return "MBBS (Bachelor of Medicine and Bachelor of Surgery) is the primary undergraduate medical degree, followed by internship and postgraduate specialization if desired.";

		if (key.Contains("dentist") || key.Contains("dental"))
			return "BDS (Bachelor of Dental Surgery) is the undergraduate degree for dentistry, followed by the required clinical training and registration pathway.";

		if (key.Contains("nurse") || key.Contains("nursing"))
			return "B.Sc Nursing is a common undergraduate route; GNM is another nursing qualification route. Choose an institution and programme approved by the relevant nursing authorities.";

		if (key.Contains("pharmacist") || key.Contains("pharmacy"))
			return "B.Pharm (Bachelor of Pharmacy) is the main undergraduate degree route. D.Pharm is a shorter diploma route where applicable.";

		if (key.Contains("physiotherapist") || key.Contains("physiotherapy"))
			return "BPT (Bachelor of Physiotherapy) is the undergraduate degree route, including supervised clinical training.";

		if (key.Contains("radiologic") || key.Contains("radiology"))
			return "B.Sc Medical Radiology / Medical Imaging or an equivalent approved undergraduate imaging programme can lead to radiologic technology roles.";

		if (key.Contains("occupational therapist") || key.Contains("occupational therapy"))
			return "BOT (Bachelor of Occupational Therapy) is the undergraduate route, with practical and clinical training.";

		if (key.Contains("biotechnolog"))
			return "B.Sc Biotechnology or B.Tech Biotechnology are common undergraduate routes. Choose B.Sc for a life-science focus or B.Tech for a stronger engineering/technology focus.";

		if (key.Contains("agricultural scientist") || key.Contains("agricultur"))
			return "B.Sc Agriculture, B.Tech Agricultural Engineering or a related agricultural science degree can provide the undergraduate foundation, depending on the target role.";

		if (key.Contains("civil engineer"))
			return "B.E./B.Tech Civil Engineering is the standard undergraduate degree, with specialization options such as structural, transportation, geotechnical or environmental engineering.";

		if (key.Contains("software engineer") || key.Contains("software developer") || key.Contains("computer engineer"))
			return "B.E./B.Tech Computer Science or Information Technology are common routes. B.Sc Computer Science and BCA are also suitable undergraduate options for software careers.";

		if (key.Contains("data scientist") || key.Contains("data analyst") || key.Contains("machine learning"))
			return "B.Tech/B.E. Computer Science, Data Science, AI or IT are strong routes. B.Sc Statistics, Mathematics, Data Science or Computer Science are also suitable options.";

		if (key.Contains("cyber security") || key.Contains("cybersecurity"))
			return "B.Tech/B.E. Cyber Security, Computer Science or Information Technology are common routes. B.Sc Cyber Security/Computer Science and BCA are also options.";

		if (key.Contains("cloud") || key.Contains("devops"))
			return "B.E./B.Tech Computer Science or Information Technology are common undergraduate foundations. B.Sc Computer Science/IT and BCA can also lead into cloud and DevOps roles.";

		if (key.Contains("electrical engineer"))
			return "B.E./B.Tech Electrical and Electronics Engineering (EEE) is the standard undergraduate route.";

		if (key.Contains("electronics") || key.Contains("embedded"))
			return "B.E./B.Tech Electronics and Communication Engineering (ECE), Electronics Engineering or Electrical/Electronics-related degrees are common routes.";

		if (key.Contains("mechanical engineer"))
			return "B.E./B.Tech Mechanical Engineering is the standard undergraduate degree, with later specialization in areas such as automotive, manufacturing, robotics or thermal engineering.";

		if (key.Contains("chemical engineer"))
			return "B.E./B.Tech Chemical Engineering is the standard undergraduate route.";

		if (key.Contains("aerospace") || key.Contains("aeronautical"))
			return "B.E./B.Tech Aerospace or Aeronautical Engineering is the direct undergraduate route; Mechanical Engineering can also support some aerospace careers.";

		if (key.Contains("architect"))
			return "B.Arch (Bachelor of Architecture) is the standard undergraduate degree for architecture, followed by practical training and the applicable registration pathway.";

		if (key.Contains("interior designer") || key.Contains("interior design"))
			return "B.Des Interior Design, B.Sc Interior Design or B.Voc Interior Design are relevant undergraduate routes; diploma programmes are also available.";

		if (key.Contains("graphic designer") || key.Contains("graphic design"))
			return "B.Des Graphic Design, BFA in Visual Communication/Applied Arts or related design degrees are suitable undergraduate routes.";

		if (key.Contains("ui/ux") || key.Contains("ui ux") || key.Contains("user experience"))
			return "B.Des Interaction/Communication/UX Design or related design degrees are strong routes. BCA/B.Sc Computer Science can also support a technology-focused UX path.";

		if (key.Contains("fashion designer") || key.Contains("fashion design"))
			return "B.Des Fashion Design or B.F.Tech Fashion Technology are common undergraduate routes, with B.Sc Fashion Design also available at some institutions.";

		if (key.Contains("animator") || key.Contains("animation"))
			return "B.Des Animation, BFA Animation, B.Sc Animation/VFX or related animation and visual-effects degrees are suitable undergraduate routes.";

		if (key.Contains("game designer") || key.Contains("game developer"))
			return "B.Des Game Design, B.Sc Game Development/Animation or B.Tech/BCA Computer Science are possible undergraduate routes depending on whether the focus is design or programming.";

		if (key.Contains("chartered accountant") || key.Contains("accountant"))
			return "B.Com or B.Com Accounting & Finance provides a strong undergraduate foundation. For CA, combine the degree with the applicable CA Foundation, Intermediate, articleship and final qualification pathway.";

		if (key.Contains("acca") || key.Contains("chartered certified accountant"))
			return "B.Com, B.Com Accounting & Finance, BBA or another relevant business degree can be combined with the ACCA professional qualification. ACCA-integrated degree programmes are also available at some institutions.";

		if (key.Contains("company secretary") || key == "company-secretary")
			return "B.Com or another relevant business/commerce degree is a useful undergraduate foundation, combined with the Company Secretary professional qualification pathway through ICSI.";

		if (key.Contains("cost accountant") || key.Contains("cma"))
			return "B.Com or B.Com Accounting & Finance provides a strong undergraduate foundation, combined with the applicable CMA professional qualification pathway.";

		if (key.Contains("investment banker") || key.Contains("financial planner") || key.Contains("finance manager") || key.Contains("financial analyst"))
			return "B.Com, B.Com Finance, BBA Finance, BMS or Economics are common undergraduate routes. Mathematics/Statistics can be particularly useful for analytical finance roles.";

		if (key.Contains("business analyst") || key.Contains("business analytics"))
			return "BBA/BMS, B.Com, B.Sc Statistics/Data Science, BCA or B.Tech Computer Science can lead to business analyst roles depending on the industry and analytics focus.";

		if (key.Contains("marketing manager") || key.Contains("marketing"))
			return "BBA/BMS Marketing, B.Com or BA/B.Sc with marketing and communication exposure are suitable undergraduate foundations; an MBA can be pursued later for management roles.";

		if (key.Contains("human resources") || key.Contains("hr manager"))
			return "BBA/BMS Human Resources, B.Com, BA Psychology or another relevant undergraduate degree can lead to HR roles; an MBA/PG qualification can support progression.";

		if (key.Contains("supply chain") || key.Contains("logistics"))
			return "BBA/BMS Supply Chain or Logistics, B.Com, BBA or a related business/operations degree are suitable undergraduate routes.";

		if (key.Contains("entrepreneur") || key.Contains("business owner"))
			return "BBA/BMS, B.Com, Economics or another business-focused undergraduate degree can provide a useful foundation, but entrepreneurship does not require one specific degree.";

		if (key.Contains("lawyer") || key.Contains("advocate") || key.Contains("law"))
			return "5-year integrated LL.B programmes such as BA LL.B, BBA LL.B or B.Com LL.B are direct routes after Class 12. A 3-year LL.B is available after completing a bachelor's degree.";

		if (key.Contains("psychologist") || key.Contains("psychology"))
			return "BA/B.Sc Psychology is the usual undergraduate foundation. Clinical and other regulated psychology roles require the applicable postgraduate education, supervised training and current professional requirements.";

		if (key.Contains("journalist") || key.Contains("journalism") || key.Contains("content creator"))
			return "BA Journalism/Mass Communication, BJMC or a related media and communication degree are common undergraduate routes. Other degrees can also work when combined with strong writing and media skills.";

		if (key.Contains("social worker") || key.Contains("social work"))
			return "BSW (Bachelor of Social Work) is the direct undergraduate route. BA Sociology, Psychology or related social-science degrees can also provide a foundation.";

		if (key.Contains("hotel manager") || key.Contains("hospitality"))
			return "BHM/BHMCT (Bachelor of Hotel Management / Hotel Management & Catering Technology), BBA Hospitality or related hospitality degrees are common undergraduate routes.";

		if (key.Contains("pilot") || key.Contains("aviation"))
			return "Aviation careers can combine a bachelor's degree such as B.Sc Aviation with the required pilot training and DGCA licensing pathway. Commercial pilot licensing is separate from the degree.";

		if (key.Contains("defence officer") || key.Contains("military"))
			return "For officer entry after Class 12, NDA is a major route followed by training at the relevant defence academy. A bachelor's degree is also part of several later officer-entry pathways.";

		if (key.Contains("ias") || key.Contains("ips") || key.Contains("civil services") || key.Contains("police officer"))
			return "Any recognized bachelor's degree can provide the undergraduate qualification for civil-service pathways. Common choices include BA, B.Com, B.Sc and B.Tech; the UPSC/state public-service exam pathway comes after graduation.";

		if (key.Contains("teacher") || key.Contains("school teacher") || key.Contains("education"))
			return "For school teaching, the undergraduate route depends on the subject and level: BA/B.Sc/B.Com followed by B.Ed is common, while integrated teacher-education degrees are also available.";

		if (key.Contains("economist") || key.Contains("economics"))
			return "BA/B.Sc Economics is the direct undergraduate route. Strong Mathematics and Statistics preparation is valuable for analytical economics careers.";

		if (key.Contains("statistician") || key.Contains("statistics"))
			return "B.Sc Statistics or B.Stat are direct undergraduate routes; Mathematics and data-analysis skills are important foundations.";

		if (key.Contains("scientist") || key.Contains("research scientist"))
			return "B.Sc in the relevant science subject is a common undergraduate foundation, followed by M.Sc/research training. For engineering-focused research, B.E./B.Tech can be appropriate.";

		if (key.Contains("environment") || key.Contains("environmental scientist"))
			return "B.Sc Environmental Science, Environmental Studies or a relevant science degree is a common undergraduate route; engineering-focused roles may use B.E./B.Tech Environmental Engineering.";

		if (key.Contains("nutrition") || key.Contains("dietitian"))
			return "B.Sc Nutrition & Dietetics, B.Sc Food & Nutrition or related nutrition/dietetics degrees are common undergraduate routes, followed by the applicable professional requirements.";

		if (key.Contains("food technologist") || key.Contains("food technology"))
			return "B.Tech/B.E. Food Technology or B.Sc Food Science/Food Technology are common undergraduate routes.";

		if (key.Contains("biomedical") || key.Contains("medical laboratory"))
			return "B.Sc Biomedical Science, B.Sc Medical Laboratory Technology or related allied-health degrees are suitable undergraduate routes, depending on the role and institution.";

		if (key.Contains("physicist"))
			return "B.Sc Physics is the usual undergraduate foundation, followed by M.Sc and research/specialization for professional physicist roles.";

		if (key.Contains("chemist") || key.Contains("chemistry"))
			return "B.Sc Chemistry is the usual undergraduate foundation, followed by M.Sc or specialized training for many professional chemistry roles.";

		if (key.Contains("biologist") || key.Contains("biology"))
			return "B.Sc Biology, Zoology, Botany or a related life-science degree is a common undergraduate foundation, followed by specialization or postgraduate study.";

		if (key.Contains("geologist") || key.Contains("geology"))
			return "B.Sc Geology or a related Earth Science degree is the direct undergraduate route, followed by specialization for many professional roles.";

		if (key.Contains("marine") || key.Contains("ocean"))
			return "B.Sc Marine Biology/Ocean Science or a related marine science degree is suitable for science roles; marine engineering follows a B.E./B.Tech Marine Engineering route.";

		if (key.Contains("fashion"))
			return "B.Des Fashion Design, B.F.Tech Fashion Technology or related fashion degrees are suitable undergraduate routes.";

		if (key.Contains("design"))
			return "B.Des in the relevant design specialization is a strong undergraduate route; BFA and related design degrees can also be suitable depending on the career.";

		if (key.Contains("media"))
			return "BA/BJMC Mass Communication, Journalism, Media Studies or a related communication degree are suitable undergraduate routes.";

		if (key.Contains("commerce") || key.Contains("finance") || key.Contains("business"))
			return "B.Com, B.Com Finance/Accounting, BBA, BMS or Economics are common undergraduate options; select the specialization that matches the target role.";

		if (key.Contains("engineering") || key.Contains("technology"))
			return "B.E./B.Tech in the relevant engineering or technology specialization is the primary undergraduate route. B.Sc/BCA may also be suitable for some technology careers.";

		if (key.Contains("science"))
			return "B.Sc in the relevant science subject is the usual undergraduate route, with B.Tech/B.E. where the career is engineering-focused.";

		if (key.Contains("arts") || key.Contains("humanities"))
			return "BA in the relevant Humanities/Arts subject is the usual undergraduate route, with specialized degrees available for some careers.";

		return $"Choose an undergraduate degree that directly matches {Name}. Common options should be selected from the career's subject area and checked against current college eligibility requirements.";
	}
}