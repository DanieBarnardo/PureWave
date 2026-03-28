using PureWave.Web.Models;

namespace PureWave.Web.Data;

public static class PureWavePlans
{
    public static IReadOnlyList<ServicePlan> All { get; } =
    [
        new ServicePlan
        {
            Slug = "blueprint",
            Name = "The Blueprint",
            Tagline = "A focused consultation for clients who want the right system before they buy or build.",
            BestFor = "Renovations, new cinema rooms, or anyone who wants expert direction before spending more money.",
            PricingModel = "Fixed consultation fee",
            Turnaround = "Usually a 2-hour site visit plus a written recommendations report",
            AccentLabel = "Strategy First",
            Deliverables =
            [
                "On-site evaluation of the room, current equipment, and desired viewing experience",
                "Guidance on projection size, seating geometry, speaker layout, and subwoofer approach",
                "Recommendations for acoustic treatment, wiring priorities, and upgrade sequencing",
                "A clear next-step report that can guide the client or their contractors"
            ],
            Outcomes =
            [
                "Avoids expensive mismatches between room, gear, and expectations",
                "Gives homeowners a technically grounded plan before the build hardens",
                "Creates confidence for premium decisions without retail pressure"
            ]
        },
        new ServicePlan
        {
            Slug = "tuning",
            Name = "The Tuning",
            Tagline = "A calibration-led service for systems that should be performing better than they are.",
            BestFor = "Existing rooms with disappointing bass, muddy dialogue, weak immersion, or unreliable control.",
            PricingModel = "Hourly rate with minimum booking",
            Turnaround = "Half-day to full-day tuning sessions depending on room complexity",
            AccentLabel = "Performance Upgrade",
            Deliverables =
            [
                "Advanced audio calibration beyond stock AVR auto-setup",
                "DSP tuning, crossover refinement, and subwoofer placement optimization",
                "Projector and screen alignment checks where relevant",
                "A summary of what was changed, what improved, and where future gains still exist"
            ],
            Outcomes =
            [
                "Unlocks performance already hidden inside existing equipment",
                "Improves dialogue clarity, tonal balance, and low-frequency impact",
                "Reduces guesswork with measured decisions instead of forum folklore"
            ]
        },
        new ServicePlan
        {
            Slug = "director",
            Name = "The Director",
            Tagline = "A full design and oversight engagement for serious home cinema projects.",
            BestFor = "High-end dedicated rooms, complex smart-home integrations, and premium builds with multiple stakeholders.",
            PricingModel = "Retainer or percentage of build cost",
            Turnaround = "Phased engagement from concept to commissioning",
            AccentLabel = "End-to-End Oversight",
            Deliverables =
            [
                "End-to-end room strategy covering acoustics, layout, equipment, and user experience",
                "Contractor-ready guidance for cabling, power, lighting logic, and system placement",
                "Automation planning for lighting scenes, playback triggers, and integrated control",
                "Oversight through the build so performance decisions survive real-world compromises"
            ],
            Outcomes =
            [
                "Protects the project from fragmented decisions across trades and suppliers",
                "Creates a premium, intuitive room rather than a pile of premium products",
                "Supports large-screen, high-fidelity rooms with long-term upgrade logic"
            ]
        }
    ];
}
