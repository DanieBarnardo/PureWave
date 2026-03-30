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
            ServiceTrack = "On-Site",
            Tagline = "For movie lovers who want to build or upgrade the room properly before spending money on the wrong gear.",
            BestFor = "New rooms, renovations, or upgrades where you want a clear plan for screen size, speaker layout, seating, and wiring.",
            PricingModel = "Fixed consultation fee",
            Turnaround = "Usually a 2-hour site visit plus a practical written recommendations report",
            AccentLabel = "Strategy First",
            Deliverables =
            [
                "An on-site look at the room, your current equipment, and the kind of sound and picture you want",
                "Clear advice on screen size, seating position, speaker placement, and bass setup",
                "Guidance on wiring, acoustic treatment, and what to do now versus later",
                "A plain-English next-step report you can use yourself or hand to your builder or installer"
            ],
            Outcomes =
            [
                "Helps you avoid wasting money on equipment that does not suit the room",
                "Gets you closer to that big-screen cinema feeling at home from the very start",
                "Takes the guesswork out of creating a room that is exciting for both movies and music"
            ]
        },
        new ServicePlan
        {
            Slug = "tuning",
            Name = "The Tuning",
            ServiceTrack = "On-Site",
            Tagline = "For systems that already have good equipment, but still do not sound as powerful, clear, or immersive as they should.",
            BestFor = "Rooms with weak bass, muddy dialogue, harsh sound, poor balance, or a system that never quite comes alive with movies or music.",
            PricingModel = "Hourly rate with minimum booking",
            Turnaround = "Half-day to full-day tuning sessions depending on room complexity",
            AccentLabel = "Performance Upgrade",
            Deliverables =
            [
                "Hands-on audio calibration that goes further than the basic AVR auto-setup",
                "Subwoofer positioning, crossover tuning, and cleaner bass integration",
                "Projector and screen alignment checks where the room includes video upgrades",
                "A summary of what was adjusted and how it improved the listening experience"
            ],
            Outcomes =
            [
                "Lets you hear what your equipment is actually capable of",
                "Makes movies hit harder, dialogue sound clearer, and music feel more natural and engaging",
                "Turns a frustrating system into something you genuinely want to sit down and listen to"
            ]
        },
        new ServicePlan
        {
            Slug = "director",
            Name = "The Director",
            ServiceTrack = "On-Site",
            Tagline = "For clients building a serious theatre room and wanting one person to keep the whole vision on track.",
            BestFor = "Dedicated cinema rooms, premium media rooms, and major projects where the room, equipment, lighting, and user experience all need to work together.",
            PricingModel = "Retainer or percentage of build cost",
            Turnaround = "Phased engagement from concept to commissioning",
            AccentLabel = "End-to-End Oversight",
            Deliverables =
            [
                "Full room planning covering acoustics, layout, equipment choices, and how the room should feel to use",
                "Contractor-ready guidance for cabling, power, lighting, control, and equipment placement",
                "A joined-up plan so the room works beautifully for film nights, background music, and everyday use",
                "Oversight during the build so the final room still matches the original vision"
            ],
            Outcomes =
            [
                "Protects you from expensive mistakes and mixed messages from different trades and suppliers",
                "Creates a real home cinema experience instead of just a room full of expensive boxes",
                "Delivers a room that feels special every time you dim the lights and press play"
            ]
        },
        new ServicePlan
        {
            Slug = "virtual-blueprint",
            Name = "The Virtual Blueprint",
            ServiceTrack = "Remote",
            Tagline = "For clients outside Durban who want expert planning help before they buy equipment or start building.",
            BestFor = "Early-stage room planning, remote renovations, and clients who want to get the layout, speaker positions, and wiring right from the start.",
            PricingModel = "Fixed remote consultation fee",
            Turnaround = "WhatsApp introduction, then a remote slot arranged at the client's convenience plus a written layout and wiring-priority report",
            AccentLabel = "Remote Planning",
            Deliverables =
            [
                "A structured remote session focused on your room, goals, and budget",
                "Guidance on seating, screen size, speaker positions, and the foundation for strong surround sound",
                "A wiring-priority guide so future upgrades are easier and cleaner",
                "A written plan you can follow yourself or give to the people doing the work"
            ],
            Outcomes =
            [
                "Gives you clarity before the room starts taking shape",
                "Helps you avoid the common mistakes that ruin surround sound and future upgrade options",
                "Brings PureWave planning to you even when an on-site visit is not practical"
            ]
        },
        new ServicePlan
        {
            Slug = "procurement-report",
            Name = "The Procurement Report",
            ServiceTrack = "Remote",
            Tagline = "For people who love great sound and picture but want help choosing the right equipment with confidence.",
            BestFor = "Clients who want honest buying advice, South African stock guidance, and a clear upgrade path for movies, music, and future expansion.",
            PricingModel = "Fixed report fee",
            Turnaround = "WhatsApp introduction, then a remote slot arranged at the client's convenience followed by research and a written report",
            AccentLabel = "Remote Research",
            Deliverables =
            [
                "A consultation slot to understand how you listen, what you watch, and where you want the system to go",
                "A shopping list with sensible options at different budget levels",
                "Clear reasons for each recommendation so you understand why something suits your room and goals",
                "South African pricing and stock guidance where available"
            ],
            Outcomes =
            [
                "Helps you buy equipment that will actually sound good in your space",
                "Keeps every rand focused on a better movie night and a better music-listening experience",
                "Stops you from ending up with a mismatched system full of regret buys"
            ]
        }
    ];
}
