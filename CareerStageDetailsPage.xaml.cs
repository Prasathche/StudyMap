using StudyMap.Models;

namespace StudyMap;

public partial class CareerStageDetailsPage : ContentPage
{
    private CareerStage _selectedStage = new CareerStage();

    public CareerStage SelectedStage
    {
        get => _selectedStage;
        set
        {
            _selectedStage = value ?? new CareerStage();
            EnrichStageDetails(_selectedStage);
            OnPropertyChanged();
        }
    }

    public CareerStageDetailsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public CareerStageDetailsPage(CareerStage selectedStage)
        : this()
    {
        SelectedStage = selectedStage;
    }

    private static void EnrichStageDetails(CareerStage stage)
    {
        if (!string.Equals(stage.Title, "Undergraduate Degree", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // ACCA is a professional accounting qualification rather than an
        // undergraduate degree. Show the common degree routes that students
        // can combine with ACCA instead of presenting a generic UG message.
        if (stage.Description.Contains("ACCA", StringComparison.OrdinalIgnoreCase))
        {
            stage.Title = "Undergraduate Degree + ACCA";
            stage.Description =
                "ACCA is a professional accounting qualification, not an undergraduate degree. " +
                "A student can pursue a relevant degree such as B.Com, B.Com (Accounting & Finance), " +
                "BBA or BBM alongside the ACCA qualification, depending on the institution and current ACCA rules.";

            stage.Details = new List<string>
            {
                "B.Com (Bachelor of Commerce) — a strong general foundation for accounting, finance and business.",
                "B.Com (Accounting & Finance) — especially relevant for accounting and financial reporting careers.",
                "BBA (Bachelor of Business Administration) — useful for a broader business and finance pathway.",
                "BBM (Bachelor of Business Management) — another business-focused degree option.",
                "ACCA professional qualification — complete the required ACCA examinations and practical experience; check the latest ACCA entry and exemption rules.",
                "ACCA-integrated degree programmes — some universities and colleges allow students to study a degree and ACCA together."
            };

            stage.Tips = new List<string>
            {
                "Compare the university's degree + ACCA structure, exemptions and total cost before enrolling.",
                "Verify the latest ACCA eligibility, examination and practical-experience requirements.",
                "Build strong skills in financial accounting, financial reporting, taxation and audit."
            };
        }
    }
}
