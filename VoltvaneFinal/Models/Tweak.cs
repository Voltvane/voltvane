using System.Collections.Generic;

namespace Voltvane.Models
{
    public enum FormFactor
    {
        PC,
        Laptop,
        Both
    }

    public enum TweakKind
    {
        Registry,      // writes a registry value
        PowerCfg,      // runs a powercfg command
        Command,       // runs an arbitrary shell command
        Guide          // no automated action - opens/explains an external tool (ThrottleStop, Afterburner)
    }

    public enum RiskLevel
    {
        Safe,          // green - reversible, no downside
        Caution,       // amber - reversible but has tradeoffs (e.g. heat on laptop)
        Advanced       // red - requires user judgement / external tooling
    }

    /// <summary>
    /// A single optimization. Every tweak carries a plain-English explanation of
    /// what it does and why it helps (or hurts) on the chosen form factor, plus a
    /// fully reversible apply/revert pair. Nothing is a black box.
    /// </summary>
    public class Tweak
    {
        public string Id { get; set; } = "";
        public string Title { get; set; } = "";
        public string ShortDescription { get; set; } = "";

        /// <summary>The "why" - shown expanded. This is the honest part that EXM/Hone never gave you.</summary>
        public string Explanation { get; set; } = "";

        /// <summary>Extra note shown specifically for laptops (e.g. heat warnings).</summary>
        public string? LaptopWarning { get; set; }

        public FormFactor Target { get; set; } = FormFactor.Both;
        public TweakKind Kind { get; set; } = TweakKind.Registry;
        public RiskLevel Risk { get; set; } = RiskLevel.Safe;

        public string Category { get; set; } = "General";

        /// <summary>If true, this tweak requires the paid Pro tier. Free/guest users see it locked.</summary>
        public bool IsPro { get; set; } = false;

        // Whether this tweak is recommended ON by default for the given form factor.
        public bool RecommendedForPC { get; set; } = true;
        public bool RecommendedForLaptop { get; set; } = true;

        // --- Registry fields ---
        public string? RegHive { get; set; }      // e.g. "HKLM"
        public string? RegPath { get; set; }      // e.g. @"SYSTEM\CurrentControlSet\Control\..."
        public string? RegName { get; set; }
        public string? RegValueApply { get; set; }
        public string? RegValueRevert { get; set; } // null = delete on revert
        public string? RegType { get; set; }        // "DWORD" or "STRING"

        // --- Command / PowerCfg fields ---
        public List<string> ApplyCommands { get; set; } = new();
        public List<string> RevertCommands { get; set; } = new();

        // --- Guide fields (for ThrottleStop / Afterburner) ---
        public string? GuideToolName { get; set; }
        public List<string> GuideSteps { get; set; } = new();
        public string? GuideDownloadUrl { get; set; }

        // Runtime state (set by the engine after checking the system)
        public bool IsCurrentlyApplied { get; set; }
    }
}
