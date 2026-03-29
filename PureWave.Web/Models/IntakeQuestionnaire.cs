using System.ComponentModel.DataAnnotations;

namespace PureWave.Web.Models;

public sealed class IntakeQuestionnaire
{
    [Required(ErrorMessage = "Please tell us who this enquiry is for.")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please add an email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string EmailAddress { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid contact number.")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please share where the project is located.")]
    [StringLength(120)]
    public string SuburbOrArea { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the project stage.")]
    public string ProjectStage { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose the plan that fits best so far.")]
    public string InterestedPlan { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please describe the type of space.")]
    public string RoomType { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please tell us your main goals for the room.")]
    [StringLength(1200, MinimumLength = 20, ErrorMessage = "Please share at least a short summary of the project goals.")]
    public string PrimaryGoals { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please outline the room dimensions or what is known so far.")]
    [StringLength(400)]
    public string RoomDimensions { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a budget band.")]
    public string BudgetBand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose a target timeline.")]
    public string Timeline { get; set; } = string.Empty;

    public bool NeedsAcousticDesign { get; set; }
    public bool NeedsCalibration { get; set; }
    public bool NeedsAutomation { get; set; }
    public bool NeedsProcurementAdvice { get; set; }
    public bool NeedsExistingEquipmentInstallation { get; set; }
    public bool NeedsGuidanceOnly { get; set; }

    [StringLength(800)]
    public string ExistingEquipment { get; set; } = string.Empty;

    [StringLength(800)]
    public string KeyChallenges { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please choose your preferred follow-up method.")]
    public string ContactPreference { get; set; } = string.Empty;
}
