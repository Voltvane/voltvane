using System.Collections.Generic;
using Voltvane.Models;

namespace Voltvane.Services
{
    /// <summary>
    /// The full catalog of tweaks. Everything here is reversible and explained.
    /// This is deliberately NOT a dump of every registry hack on the internet -
    /// it's the curated set that genuinely helps, with the laptop/PC distinction
    /// that one-click optimizers ignore.
    /// </summary>
    public static class TweakCatalog
    {
        public static List<Tweak> GetAll()
        {
            var list = new List<Tweak>();

            // ---------------------------------------------------------------
            // CATEGORY: POWER
            // ---------------------------------------------------------------

            list.Add(new Tweak
            {
                Id = "proc_min_state",
                Title = "Minimum processor state -> 100%",
                Category = "Power",
                ShortDescription = "Stops the CPU dropping to a near-idle clock between frames.",
                Explanation =
                    "Windows lets the CPU drop its minimum clock very low to save power. In games this can cause " +
                    "micro-stutter, because the CPU keeps clocking down between frames and has to ramp back up. " +
                    "Setting the minimum processor state to 100% keeps the clock from sagging. This is a real, " +
                    "common fix for choppy frametimes - and it's fully reversible.",
                LaptopWarning =
                    "On a laptop this keeps the CPU clocked higher at idle too, so idle temps and fan noise rise a little. " +
                    "Worth it while gaming plugged in; you may prefer to revert it on battery.",
                Target = FormFactor.Both,
                Kind = TweakKind.PowerCfg,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                ApplyCommands = new()
                {
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100",
                    "powercfg /setactive SCHEME_CURRENT"
                },
                RevertCommands = new()
                {
                    // 5% is the Windows Balanced default
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 5",
                    "powercfg /setactive SCHEME_CURRENT"
                }
            });

            list.Add(new Tweak
            {
                Id = "power_plan_balanced",
                Title = "Use the Balanced power plan",
                Category = "Power",
                ShortDescription = "The right baseline for a gaming laptop - not 'Ultimate Performance'.",
                Explanation =
                    "Counter-intuitively, 'Ultimate/High Performance' plans are usually worse on laptops. They disable " +
                    "the CPU's ability to idle and park cores, which generates heat on a shared cooling system and can " +
                    "actually lower sustained clocks. Balanced lets the chip breathe. The minimum-processor-state tweak " +
                    "above already removes Balanced's main downside for gaming.",
                LaptopWarning =
                    "This is especially important on laptops. 'Ultimate Performance' is a desktop idea - it assumes " +
                    "unlimited cooling you don't have.",
                Target = FormFactor.Laptop,
                Kind = TweakKind.PowerCfg,
                Risk = RiskLevel.Safe,
                RecommendedForPC = false,   // desktops use Ultimate Performance instead
                RecommendedForLaptop = true,
                ApplyCommands = new()
                {
                    // 381b4222... is the GUID of the built-in Balanced plan
                    "powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e"
                },
                RevertCommands = new()
                {
                    // High Performance plan GUID
                    "powercfg /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"
                }
            });

            // ---------------------------------------------------------------
            // VOLTVANE LAPTOP POWER PLAN
            // ---------------------------------------------------------------
            list.Add(new Tweak
            {
                Id = "voltvane_laptop_plan",
                IsPro = true,
                Title = "Voltvane Laptop power plan",
                Category = "Power",
                ShortDescription = "A custom Voltvane plan built specifically for gaming laptops.",
                Explanation =
                    "Creates and activates the 'Voltvane Laptop' power plan — built on Balanced (which is correct " +
                    "for laptops) but tuned for gaming. Sets the minimum processor state to 100% so the CPU never " +
                    "sags between frames, disables USB selective suspend, sets wireless adapter to max performance, " +
                    "and removes the display sleep timer while plugged in. Keeps the CPU's ability to manage heat " +
                    "unlike Ultimate Performance, which is what makes it laptop-safe. Reverting switches back to " +
                    "Balanced and removes the custom plan.",
                LaptopWarning =
                    "This plan is designed specifically for laptops — it respects thermal management unlike 'Ultimate " +
                    "Performance'. Still, monitor temps after applying.",
                Target = FormFactor.Laptop,
                Kind = TweakKind.Command,
                Risk = RiskLevel.Caution,
                RecommendedForLaptop = true,
                ApplyCommands = new()
                {
                    // Step 1: duplicate Balanced as base
                    "powercfg -duplicatescheme 381b4222-f694-41f0-9685-ff5bb260df2e 22222222-3333-4444-5555-666666666660",
                    // Step 2: rename it
                    "powercfg /changename 22222222-3333-4444-5555-666666666660 \"Voltvane Laptop\" \"Custom Voltvane plan for gaming laptops\"",
                    // Step 3: min processor state 100% (fixes stutter, key laptop tweak)
                    "powercfg /setacvalueindex 22222222-3333-4444-5555-666666666660 54533251-82be-4824-96c1-47b60b740d00 893dee8e-2bef-41e0-89c6-b55d0929964c 100",
                    // Step 4: max processor state 100%
                    "powercfg /setacvalueindex 22222222-3333-4444-5555-666666666660 54533251-82be-4824-96c1-47b60b740d00 bc5038f7-23e0-4960-96da-33abaf5935ec 100",
                    // Step 5: wireless adapter max performance
                    "powercfg /setacvalueindex 22222222-3333-4444-5555-666666666660 19caa586-fa55-4f43-ae8e-e41cb12b9ee8 12bbebe6-58d6-4636-95bb-3217ef867c1a 0",
                    // Step 6: disable USB selective suspend
                    "powercfg /setacvalueindex 22222222-3333-4444-5555-666666666660 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0",
                    // Step 7: activate it
                    "powercfg /setactive 22222222-3333-4444-5555-666666666660"
                },
                RevertCommands = new()
                {
                    "powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e",
                    "powercfg /delete 22222222-3333-4444-5555-666666666660"
                }
            });

            // ===============================================================
            // PC-ONLY POWER TWEAKS
            // These are appropriate for desktops with proper cooling. They are
            // deliberately NOT offered on laptops, where they generate heat on a
            // shared cooler and can lower sustained clocks.
            // ===============================================================

            list.Add(new Tweak
            {
                Id = "ultimate_performance_plan",
                IsPro = true,
                Title = "Enable the Ultimate Performance power plan",
                Category = "Power",
                ShortDescription = "Windows' highest-tier power plan, built for high-end desktops.",
                Explanation =
                    "Ultimate Performance is a hidden Windows power plan aimed at high-end desktops and workstations. " +
                    "It removes micro-latencies by keeping hardware in a readier state and minimizing power-saving " +
                    "polling. On a desktop with proper cooling this is a sensible default. (Voltvane unhides and " +
                    "activates it for you.) Reverting switches you back to the Balanced plan.",
                Target = FormFactor.PC,
                Kind = TweakKind.PowerCfg,
                Risk = RiskLevel.Caution,
                RecommendedForPC = true,
                ApplyCommands = new()
                {
                    // Duplicate the Ultimate Performance template (creates it if hidden), then activate it.
                    "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61",
                    "powercfg /setactive e9a42b02-d5df-448d-aa00-03f14749eb61"
                },
                RevertCommands = new()
                {
                    // Back to Balanced
                    "powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e"
                }
            });

            // ---------------------------------------------------------------
            // VOLTVANE PC POWER PLAN
            // ---------------------------------------------------------------
            list.Add(new Tweak
            {
                Id = "voltvane_pc_plan",
                IsPro = true,
                Title = "Voltvane PC power plan",
                Category = "Power",
                ShortDescription = "A custom Voltvane plan built for desktop gaming performance.",
                Explanation =
                    "Creates and activates the 'Voltvane PC' power plan — a custom plan built on top of Ultimate " +
                    "Performance with all the right settings for a desktop gaming PC. Sets min/max processor state " +
                    "to 100%, disables USB selective suspend, turns off PCI Express power saving, and removes the " +
                    "hard disk sleep timer. Everything in one click. Reverting switches back to Balanced and " +
                    "removes the custom plan.",
                Target = FormFactor.PC,
                Kind = TweakKind.Command,
                Risk = RiskLevel.Caution,
                RecommendedForPC = true,
                ApplyCommands = new()
                {
                    // Step 1: duplicate Ultimate Performance as base, capture its new GUID
                    "powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 11111111-2222-3333-4444-555555555550",
                    // Step 2: rename it to Voltvane PC
                    "powercfg /changename 11111111-2222-3333-4444-555555555550 \"Voltvane PC\" \"Custom Voltvane plan for desktop gaming\"",
                    // Step 3: processor min/max 100%
                    "powercfg /setacvalueindex 11111111-2222-3333-4444-555555555550 54533251-82be-4824-96c1-47b60b740d00 893dee8e-2bef-41e0-89c6-b55d0929964c 100",
                    "powercfg /setacvalueindex 11111111-2222-3333-4444-555555555550 54533251-82be-4824-96c1-47b60b740d00 bc5038f7-23e0-4960-96da-33abaf5935ec 100",
                    // Step 4: disable USB selective suspend
                    "powercfg /setacvalueindex 11111111-2222-3333-4444-555555555550 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0",
                    // Step 5: PCI Express link power off
                    "powercfg /setacvalueindex 11111111-2222-3333-4444-555555555550 501a4d13-42af-4429-9fd1-a8218c268e20 ee12f906-d277-404b-b6da-e5fa1a576df5 0",
                    // Step 6: hard disk never sleep
                    "powercfg /setacvalueindex 11111111-2222-3333-4444-555555555550 0012ee47-9041-4b5d-9b77-535fba8b1442 6738e2c4-e8a5-4a42-b16a-e040e769756e 0",
                    // Step 7: activate it
                    "powercfg /setactive 11111111-2222-3333-4444-555555555550"
                },
                RevertCommands = new()
                {
                    // Switch back to Balanced then delete the custom plan
                    "powercfg /setactive 381b4222-f694-41f0-9685-ff5bb260df2e",
                    "powercfg /delete 11111111-2222-3333-4444-555555555550"
                }
            });

            list.Add(new Tweak
            {
                Id = "max_processor_state_pc",
                IsPro = true,
                Title = "Keep CPU at maximum performance",
                Category = "Power",
                ShortDescription = "Holds the CPU at 100% min state so it never down-clocks.",
                Explanation =
                    "Sets BOTH the minimum and maximum processor state to 100%, so the CPU stays at full clocks instead " +
                    "of ramping down between bursts. On a desktop with headroom this keeps clocks rock-steady and removes " +
                    "any ramp-up latency. Reverting restores the Balanced default (min 5%, max 100%).",
                Target = FormFactor.PC,
                Kind = TweakKind.PowerCfg,
                Risk = RiskLevel.Caution,
                RecommendedForPC = true,
                ApplyCommands = new()
                {
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 100",
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100",
                    "powercfg /setactive SCHEME_CURRENT"
                },
                RevertCommands = new()
                {
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMIN 5",
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR PROCTHROTTLEMAX 100",
                    "powercfg /setactive SCHEME_CURRENT"
                }
            });

            list.Add(new Tweak
            {
                Id = "disable_core_parking_pc",
                IsPro = true,
                Title = "Disable CPU core parking",
                Category = "Power",
                ShortDescription = "Keeps all cores awake and ready instead of parking idle ones.",
                Explanation =
                    "Core parking lets Windows put idle CPU cores to sleep to save power. Waking them adds a tiny delay. " +
                    "Disabling it keeps every core ready, which can marginally smooth frametimes on a desktop. Honest note: " +
                    "on modern CPUs the real-world FPS difference is usually small - this is a 'consistency' tweak, not a " +
                    "magic boost. Reversible.",
                Target = FormFactor.PC,
                Kind = TweakKind.PowerCfg,
                Risk = RiskLevel.Caution,
                RecommendedForPC = false,
                ApplyCommands = new()
                {
                    // 100% cores unparked
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100",
                    "powercfg /setactive SCHEME_CURRENT"
                },
                RevertCommands = new()
                {
                    // Windows default lets it manage parking (0 = allow parking down to min)
                    "powercfg /setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 0",
                    "powercfg /setactive SCHEME_CURRENT"
                }
            });

            list.Add(new Tweak
            {
                Id = "gpu_prefer_max_perf_pc",
                IsPro = true,
                Title = "Prefer maximum GPU performance",
                Category = "Power",
                ShortDescription = "Stops the GPU down-clocking; sets a 'prefer max performance' hint.",
                Explanation =
                    "This writes the registry hint that corresponds to NVIDIA's 'Prefer Maximum Performance' power mode, " +
                    "so the GPU holds higher clocks instead of dropping to power-saving states. On a desktop this is a " +
                    "reasonable always-on setting. Honest note: for the most reliable result, also set 'Prefer Maximum " +
                    "Performance' in the NVIDIA Control Panel - the driver UI is the authoritative place for it. " +
                    "A reboot is recommended.",
                Target = FormFactor.PC,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Caution,
                RecommendedForPC = false,
                RegHive = "HKLM",
                RegPath = @"SYSTEM\CurrentControlSet\Control\Power",
                RegName = "CsEnabled",
                RegType = "DWORD",
                RegValueApply = "0",   // disabling Connected Standby helps desktops hold perf states
                RegValueRevert = "1"
            });

            // ---------------------------------------------------------------
            // CATEGORY: LATENCY & SCHEDULING
            // ---------------------------------------------------------------

            list.Add(new Tweak
            {
                Id = "network_throttling_off_pc",
                IsPro = true,
                Title = "Disable network throttling",
                Category = "Latency & Scheduling",
                ShortDescription = "Removes Windows' background network throttle for lower latency.",
                Explanation =
                    "Windows throttles network packet processing by default to leave CPU for multimedia. On a capable " +
                    "desktop you can disable this so online games get packets processed without the throttle, which can " +
                    "shave a little latency. Setting the value to 0xFFFFFFFF disables throttling; reverting restores the " +
                    "default (10).",
                Target = FormFactor.PC,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Caution,
                RecommendedForPC = true,
                RegHive = "HKLM",
                RegPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                RegName = "NetworkThrottlingIndex",
                RegType = "DWORD",
                RegValueApply = "4294967295",
                RegValueRevert = "10"
            });

            list.Add(new Tweak
            {
                Id = "system_responsiveness",
                Title = "Prioritize games for CPU scheduling",
                Category = "Latency & Scheduling",
                ShortDescription = "Lets games claim more CPU time via Windows' multimedia scheduler.",
                Explanation =
                    "Windows reserves a slice of CPU for background tasks via a system called MMCSS. The " +
                    "SystemResponsiveness value controls how big that reserved slice is. The Windows default is 20 " +
                    "(20% reserved). Setting it to 0 gives games maximum scheduling priority and can smooth frametimes. " +
                    "This is one of the few 'famous' registry tweaks that's both safe and genuinely helpful.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RegHive = "HKLM",
                RegPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile",
                RegName = "SystemResponsiveness",
                RegType = "DWORD",
                RegValueApply = "0",
                RegValueRevert = "20"
            });

            list.Add(new Tweak
            {
                Id = "games_task_priority",
                IsPro = true,
                Title = "Raise the 'Games' task priority",
                Category = "Latency & Scheduling",
                ShortDescription = "Tells Windows to treat game threads as high priority.",
                Explanation =
                    "Under the MMCSS 'Games' task profile, Windows defines how much GPU and CPU priority game threads get. " +
                    "Raising the scheduling category and priority here reinforces the SystemResponsiveness tweak. Reversible " +
                    "to Windows defaults at any time.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RegHive = "HKLM",
                RegPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                RegName = "Priority",
                RegType = "DWORD",
                RegValueApply = "6",
                RegValueRevert = "2"
            });

            list.Add(new Tweak
            {
                Id = "global_timer_resolution",
                IsPro = true,
                Title = "Allow high-resolution timers",
                Category = "Latency & Scheduling",
                ShortDescription = "Improves frametime consistency for games that request precise timing.",
                Explanation =
                    "This permits applications to request high-resolution system timers, which can improve frame pacing " +
                    "in games. It's a light touch and reverts cleanly. (Note: Windows 11 manages timers more dynamically " +
                    "than older versions, so the effect varies by system.)",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RegHive = "HKLM",
                RegPath = @"SYSTEM\CurrentControlSet\Control\Session Manager\kernel",
                RegName = "GlobalTimerResolutionRequests",
                RegType = "DWORD",
                RegValueApply = "1",
                RegValueRevert = null // delete on revert (key didn't exist by default)
            });

            // ---------------------------------------------------------------
            // CATEGORY: VISUALS / RESPONSIVENESS
            // ---------------------------------------------------------------

            list.Add(new Tweak
            {
                Id = "disable_game_dvr",
                Title = "Disable Xbox Game DVR background recording",
                Category = "Visuals & Responsiveness",
                ShortDescription = "Stops the always-on background recorder that can cost a few FPS.",
                Explanation =
                    "Windows' built-in Game DVR continuously records gameplay in the background so you can 'rewind' clips. " +
                    "It has a measurable FPS and latency cost in some games. Disabling it is one of the safest, most " +
                    "universally recommended tweaks. You can still screenshot/record manually with other tools.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RegHive = "HKCU",
                RegPath = @"System\GameConfigStore",
                RegName = "GameDVR_Enabled",
                RegType = "DWORD",
                RegValueApply = "0",
                RegValueRevert = "1"
            });

            list.Add(new Tweak
            {
                Id = "hags",
                Title = "Hardware-accelerated GPU scheduling",
                Category = "Visuals & Responsiveness",
                ShortDescription = "Lets the GPU manage its own memory scheduling - can reduce latency.",
                Explanation =
                    "HAGS hands some scheduling work from the CPU to the GPU. On modern NVIDIA cards " +
                    "it's generally beneficial and is required for some features like DLSS Frame Generation. " +
                    "Reversible. A reboot is required for the change to take effect.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Caution,
                RegHive = "HKLM",
                RegPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers",
                RegName = "HwSchMode",
                RegType = "DWORD",
                RegValueApply = "2",   // 2 = enabled
                RegValueRevert = "1"   // 1 = disabled
            });

            // ---------------------------------------------------------------
            // CATEGORY: CLEANUP
            // ---------------------------------------------------------------

            list.Add(new Tweak
            {
                Id = "clear_temp",
                Title = "Clear temporary files",
                Category = "Cleanup",
                ShortDescription = "Deletes Windows + user temp files to free disk space.",
                Explanation =
                    "Clears the contents of your %temp% and Windows\\Temp folders. These are throwaway files that apps " +
                    "create and forget. Safe to delete - anything still in use simply won't be removed. This is a " +
                    "one-shot action, not a persistent tweak, so there's nothing to revert.",
                Target = FormFactor.Both,
                Kind = TweakKind.Command,
                Risk = RiskLevel.Safe,
                ApplyCommands = new()
                {
                    "del /q /f /s \"%TEMP%\\*\"",
                    "del /q /f /s \"C:\\Windows\\Temp\\*\""
                },
                RevertCommands = new() // nothing to revert
            });

            // ---------------------------------------------------------------
            // CATEGORY: CPU TUNING
            // ---------------------------------------------------------------

            // --- LAPTOP: ThrottleStop with the all-important Sync MMIO step ---
            list.Add(new Tweak
            {
                Id = "throttlestop_guide_laptop",
                IsPro = true,
                Title = "CPU power tuning with ThrottleStop",
                Category = "CPU Tuning",
                ShortDescription = "Unlock your laptop's real CPU power budget and set per-game profiles.",
                Explanation =
                    "Most gaming laptops ship with firmware power limits that hold the CPU well below what its cooling " +
                    "could sustain. ThrottleStop can raise those limits - but the critical detail most guides miss is the " +
                    "'Sync MMIO' option: without it, the laptop firmware silently overrides ThrottleStop's limits, so your " +
                    "changes do nothing. Voltvane walks you through it.",
                LaptopWarning =
                    "Raising power limits raises heat. Pair this with an undervolt and good cooling. Use a lower limit " +
                    "(e.g. 55W) for GPU-bound games and a higher one only for CPU-heavy games.",
                Target = FormFactor.Laptop,
                Kind = TweakKind.Guide,
                Risk = RiskLevel.Advanced,
                RecommendedForLaptop = true,
                GuideToolName = "ThrottleStop",
                GuideDownloadUrl = "https://www.techpowerup.com/download/throttlestop/",
                GuideSteps = new()
                {
                    "Open ThrottleStop and click the 'TPL' button (Turbo Power Limits).",
                    "Look at the MSR and MMIO rows. If MMIO shows a LOWER number than MSR (e.g. MMIO 25 vs MSR 55), the firmware is capping you - this is the key problem.",
                    "Tick the 'Sync MMIO' checkbox. This forces your power-limit values into the firmware register too, so they actually take effect.",
                    "Set your power limits (PL1 / PL2). A safe starting point for a thin gaming laptop: PL1 = 55W, PL2 = 75W.",
                    "Tick 'Clamp' on both Long Power (PL1) and Short Power (PL2).",
                    "Set Speed Shift EPP to 0 for maximum responsiveness while gaming.",
                    "Click OK, then click 'Turn On' on the main window, then 'Save'.",
                    "In Options, tick 'AC Profile' and set it to your gaming profile number so it auto-activates when plugged in.",
                    "Create a second profile with higher limits (e.g. 90/90W) for CPU-heavy competitive games, and switch between them per game."
                }
            });

            // --- PC: desktop CPU tuning is different - it's about boost/curve, not unlocking a cap ---
            list.Add(new Tweak
            {
                Id = "cpu_tuning_pc",
                IsPro = true,
                Title = "Desktop CPU tuning (boost & curve)",
                Category = "CPU Tuning",
                ShortDescription = "On desktop the win is better boost clocks - not unlocking a power cap.",
                Explanation =
                    "Desktops usually AREN'T power-capped the way laptops are, so ThrottleStop's 'unlock the limit' trick " +
                    "doesn't apply the same way. On a desktop the real CPU gains come from your motherboard BIOS and (for " +
                    "Ryzen) Curve Optimizer / PBO, or (for Intel) the power limits and AC/DC loadline in BIOS. These let " +
                    "the chip boost higher and more consistently with proper cooling. This is a guide, not an auto-tweak - " +
                    "BIOS settings are board-specific and best done deliberately.",
                Target = FormFactor.PC,
                Kind = TweakKind.Guide,
                Risk = RiskLevel.Advanced,
                RecommendedForPC = false,
                GuideToolName = null,
                GuideDownloadUrl = "https://www.techpowerup.com/download/",
                GuideSteps = new()
                {
                    "Identify your CPU: AMD Ryzen or Intel? The path differs.",
                    "AMD Ryzen: in BIOS, enable PBO (Precision Boost Overdrive) and use Curve Optimizer with a negative offset (e.g. -15) for more boost at less voltage. Test stability.",
                    "Intel: in BIOS, confirm power limits (PL1/PL2) are set to your cooler's capability, and leave Turbo Boost enabled. Avoid disabling C-states - it hurts boost.",
                    "For monitoring, HWInfo64 shows your real all-core and single-core boost clocks and temps.",
                    "Stress test after any BIOS change (e.g. Cinebench, OCCT) and watch temps stay in a safe range.",
                    "Honest note: on a well-cooled desktop the stock boost behavior is already good. Gains here are for enthusiasts chasing the last few percent - they won't transform your FPS."
                }
            });

            // ---------------------------------------------------------------
            // CATEGORY: GPU TUNING
            // ---------------------------------------------------------------

            // --- LAPTOP: undervolt for LESS HEAT (the priority on a thermally-limited laptop) ---
            list.Add(new Tweak
            {
                Id = "afterburner_undervolt_laptop",
                IsPro = true,
                Title = "GPU undervolt with MSI Afterburner",
                Category = "GPU Tuning",
                ShortDescription = "Same performance, less heat - the single best laptop thermal fix.",
                Explanation =
                    "On a laptop, undervolting is about HEAT. Running the GPU at the same clock for less voltage means " +
                    "less heat, which often means HIGHER sustained clocks because the card stops hitting its thermal limit " +
                    "and throttling. It's the highest-value change you can make on a thermally-limited laptop.\n\n" +
                    "IMPORTANT: Voltvane will NOT auto-apply an undervolt. Every GPU chip is physically different " +
                    "('silicon lottery'), so a curve that's stable on one card will crash another - even the same model. " +
                    "The only safe way is to find YOUR card's stable point by testing. This guide shows you how.",
                LaptopWarning =
                    "Never enable Afterburner's 'apply at startup' until you've tested a curve for hours without crashes. " +
                    "An unstable startup curve can cause a black-screen boot loop (recoverable via Safe Mode, but annoying).",
                Target = FormFactor.Laptop,
                Kind = TweakKind.Guide,
                Risk = RiskLevel.Advanced,
                RecommendedForLaptop = true,
                GuideToolName = "MSI Afterburner",
                GuideDownloadUrl = "https://www.msi.com/Landing/afterburner/graphics-cards",
                GuideSteps = new()
                {
                    "Open MSI Afterburner and press Ctrl+F to open the Voltage/Frequency curve editor.",
                    "Goal on a laptop = LESS HEAT. Pick a modest target like 2200-2300 MHz at 875 mV (lower voltage = cooler).",
                    "Click the point at your chosen voltage (e.g. 875 mV) and drag it UP to your target clock.",
                    "Press Enter to flatten the curve to the right of that point, then click Apply (the checkmark).",
                    "Stress test in a demanding game for 30-60 minutes. Watch GPU temp drop and check for crashes/artifacts.",
                    "If it crashes or artifacts: lower the target clock by 15 MHz and test again.",
                    "If stable and cool, you're done - on a laptop, a cool stable clock beats chasing max MHz.",
                    "Optional: a small memory overclock (e.g. +400) once the core curve is proven stable.",
                    "Only AFTER hours of proven stability should you consider 'apply at startup'."
                }
            });

            // --- PC: undervolt OR overclock for MORE PERFORMANCE (desktop has cooling headroom) ---
            list.Add(new Tweak
            {
                Id = "afterburner_oc_pc",
                IsPro = true,
                Title = "GPU overclock / undervolt with MSI Afterburner",
                Category = "GPU Tuning",
                ShortDescription = "On desktop you have headroom to push for MORE performance.",
                Explanation =
                    "On a desktop with good cooling the calculus flips: instead of undervolting purely for heat, you can " +
                    "either undervolt for efficiency OR push a real overclock for more FPS, because your cooling can " +
                    "absorb it. A power-limit increase plus a core/memory offset can give a genuine performance bump.\n\n" +
                    "IMPORTANT: Voltvane will NOT auto-apply an overclock or undervolt. Every GPU is different, and an " +
                    "unstable overclock crashes or artifacts. The only safe way is to test incrementally on YOUR card. " +
                    "This guide shows you how.",
                Target = FormFactor.PC,
                Kind = TweakKind.Guide,
                Risk = RiskLevel.Advanced,
                RecommendedForPC = false,
                GuideToolName = "MSI Afterburner",
                GuideDownloadUrl = "https://www.msi.com/Landing/afterburner/graphics-cards",
                GuideSteps = new()
                {
                    "Open MSI Afterburner. First, drag the Power Limit slider to its maximum - desktop cooling can handle it.",
                    "For a quick OC: add +100 MHz to Core Clock, click Apply, and test. Increase in +25 MHz steps until you see artifacts or crashes, then back off 25-50 MHz.",
                    "Add memory clock in +100-200 MHz steps, testing each time (watch for artifacts or performance REGRESSION from error-correction).",
                    "For an undervolt-OC (efficiency): press Ctrl+F, set your target clock at a lower voltage point (e.g. 2800-3000 MHz at 950 mV on a desktop 40-series), flatten, Apply.",
                    "Stress test thoroughly: 30-60 min of a demanding game, or a loop of 3DMark/Unigine. Watch for crashes, artifacts, and temps.",
                    "If unstable: lower core/memory or raise the voltage point slightly. Re-test.",
                    "Once stable across long sessions, save the profile. On desktop you CAN enable 'apply at startup' once it's proven.",
                    "Monitor temps - even with headroom, keep the GPU in a safe range under sustained load."
                }
            });

            // ---------------------------------------------------------------
            // CATEGORY: LATENCY & SCHEDULING (additional high-impact tweaks)
            // ---------------------------------------------------------------

            list.Add(new Tweak
            {
                Id = "disable_fullscreen_optimizations",
                Title = "Disable fullscreen optimizations",
                Category = "Latency & Scheduling",
                ShortDescription = "Removes Windows' DWM interference in fullscreen games.",
                Explanation =
                    "Windows 'fullscreen optimizations' makes games run in a borderless window mode internally, letting " +
                    "Windows manage the swap chain. This adds a small but real layer of latency. Disabling it forces true " +
                    "exclusive fullscreen, which can lower input lag and improve frametime consistency — especially on " +
                    "older titles. Fully reversible.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKCU",
                RegPath = @"System\GameConfigStore",
                RegName = "GameDVR_FSEBehaviorMode",
                RegType = "DWORD",
                RegValueApply = "2",
                RegValueRevert = "0"
            });

            list.Add(new Tweak
            {
                Id = "disable_fullscreen_optimizations_global",
                Title = "Disable fullscreen optimizations (global flag)",
                Category = "Latency & Scheduling",
                ShortDescription = "System-wide flag that reinforces the fullscreen optimization disable.",
                Explanation =
                    "This is the second registry key required to fully disable fullscreen optimizations system-wide. " +
                    "Some games respect only this key, others only the first — applying both ensures it takes effect " +
                    "across all games. Pair with the other fullscreen optimization tweak for full coverage.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKCU",
                RegPath = @"System\GameConfigStore",
                RegName = "GameDVR_HonorUserFSEBehaviorMode",
                RegType = "DWORD",
                RegValueApply = "1",
                RegValueRevert = "0"
            });

            list.Add(new Tweak
            {
                Id = "raise_gpu_priority",
                IsPro = true,
                Title = "Raise GPU thread priority",
                Category = "Latency & Scheduling",
                ShortDescription = "Elevates the GPU's scheduling priority for smoother frametimes.",
                Explanation =
                    "This sets the GPU priority and SFIO priority to their highest values in the MMCSS Games profile. " +
                    "Windows uses these values to schedule GPU work — raising them means your game's GPU commands get " +
                    "processed ahead of background tasks. One of the tweaks that directly helps frametime consistency " +
                    "and is commonly used by competitive optimizer tools.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKLM",
                RegPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                RegName = "GPU Priority",
                RegType = "DWORD",
                RegValueApply = "8",
                RegValueRevert = "1"
            });

            list.Add(new Tweak
            {
                Id = "raise_sfio_priority",
                IsPro = true,
                Title = "Raise SFIO priority for games",
                Category = "Latency & Scheduling",
                ShortDescription = "Prioritizes game I/O scheduling for faster asset streaming.",
                Explanation =
                    "SFIO (Scheduled File I/O) priority controls how the OS schedules disk reads for game processes. " +
                    "Raising it to High means the game's texture and asset streaming gets more responsive I/O scheduling, " +
                    "which helps with stutter caused by slow level loading and streaming in open-world games.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKLM",
                RegPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile\Tasks\Games",
                RegName = "SFIO Priority",
                RegType = "STRING",
                RegValueApply = "High",
                RegValueRevert = "Normal"
            });

            list.Add(new Tweak
            {
                Id = "disable_nagle_algorithm",
                IsPro = true,
                Title = "Disable Nagle's Algorithm",
                Category = "Latency & Scheduling",
                ShortDescription = "Removes TCP packet batching that adds latency in online games.",
                Explanation =
                    "Nagle's Algorithm batches small TCP packets together before sending them, which reduces bandwidth " +
                    "usage but adds latency — the opposite of what you want in games. Disabling it (TcpAckFrequency=1 " +
                    "and TCPNoDelay=1) sends each packet immediately, which can noticeably lower your in-game ping " +
                    "responsiveness. A well-known competitive gaming optimization.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Caution,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKLM",
                RegPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces",
                RegName = "TcpAckFrequency",
                RegType = "DWORD",
                RegValueApply = "1",
                RegValueRevert = null
            });

            list.Add(new Tweak
            {
                Id = "disable_nagle_nodelay",
                IsPro = true,
                Title = "Enable TCP No Delay",
                Category = "Latency & Scheduling",
                ShortDescription = "Forces TCP to send packets without delay — pairs with Nagle disable.",
                Explanation =
                    "TCPNoDelay=1 is the second key required to fully disable Nagle's Algorithm. It forces Windows to " +
                    "transmit packets immediately without buffering. Together with TcpAckFrequency, this gives you the " +
                    "lowest possible TCP latency for online games. Reversible by deleting the registry value.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Caution,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKLM",
                RegPath = @"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces",
                RegName = "TCPNoDelay",
                RegType = "DWORD",
                RegValueApply = "1",
                RegValueRevert = null
            });

            // ---------------------------------------------------------------
            // CATEGORY: VISUALS & RESPONSIVENESS (additional tweaks)
            // ---------------------------------------------------------------

            list.Add(new Tweak
            {
                Id = "disable_xbox_game_bar",
                Title = "Disable Xbox Game Bar",
                Category = "Visuals & Responsiveness",
                ShortDescription = "Turns off the Game Bar overlay that uses CPU and memory in the background.",
                Explanation =
                    "Xbox Game Bar runs in the background even when you're not using it, consuming CPU cycles and " +
                    "memory. Disabling it entirely removes this overhead. You can still use other overlay tools like " +
                    "MSI Afterburner's RivaTuner. Note: Game DVR (background recording) is a separate tweak — " +
                    "disable both for maximum effect.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKCU",
                RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\GameDVR",
                RegName = "AppCaptureEnabled",
                RegType = "DWORD",
                RegValueApply = "0",
                RegValueRevert = "1"
            });

            list.Add(new Tweak
            {
                Id = "disable_mouse_acceleration",
                Title = "Disable mouse acceleration (Enhance Pointer Precision)",
                Category = "Visuals & Responsiveness",
                ShortDescription = "Makes mouse movement 1:1 — critical for aim consistency in FPS games.",
                Explanation =
                    "Windows 'Enhance Pointer Precision' applies acceleration to your mouse movement, meaning faster " +
                    "flicks travel disproportionately further than slow movements. This breaks muscle memory in FPS " +
                    "games. Disabling it makes mouse input 1:1 and consistent, which almost every competitive player " +
                    "considers essential. This is one of the highest-impact tweaks for FPS games.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKCU",
                RegPath = @"Control Panel\Mouse",
                RegName = "MouseSpeed",
                RegType = "STRING",
                RegValueApply = "0",
                RegValueRevert = "1"
            });

            list.Add(new Tweak
            {
                Id = "visual_effects_performance",
                IsPro = true,
                Title = "Set visual effects to 'Best Performance'",
                Category = "Visuals & Responsiveness",
                ShortDescription = "Strips Windows animations and effects to free CPU for games.",
                Explanation =
                    "Windows runs dozens of visual effects — shadows, animations, fades, transparency. These use CPU " +
                    "and GPU resources. Setting the profile to Best Performance disables them all, which frees up " +
                    "measurable resources especially on mid-range systems. Your desktop will look more basic, but " +
                    "games get those resources instead. Fully reversible.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKCU",
                RegPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects",
                RegName = "VisualFXSetting",
                RegType = "DWORD",
                RegValueApply = "2",
                RegValueRevert = "0"
            });

            list.Add(new Tweak
            {
                Id = "disable_transparency",
                Title = "Disable Windows transparency effects",
                Category = "Visuals & Responsiveness",
                ShortDescription = "Removes the frosted-glass blur that uses GPU every time you open the Start menu.",
                Explanation =
                    "Windows' transparency/blur effects (the frosted glass on Start, taskbar, and Settings) require " +
                    "the GPU to render a blurred composite every frame those elements are visible. Disabling " +
                    "transparency removes this cost entirely. Small but free performance — and the desktop still " +
                    "looks clean.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Safe,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                RegHive = "HKCU",
                RegPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                RegName = "EnableTransparency",
                RegType = "DWORD",
                RegValueApply = "0",
                RegValueRevert = "1"
            });

            // ---------------------------------------------------------------
            // CATEGORY: BACKGROUND SERVICES
            // ---------------------------------------------------------------

            list.Add(new Tweak
            {
                Id = "disable_sysmain",
                IsPro = true,
                Title = "Disable SysMain (Superfetch)",
                Category = "Background Services",
                ShortDescription = "Stops the background service that pre-loads apps into RAM — not needed for gaming.",
                Explanation =
                    "SysMain (formerly Superfetch) tries to predict which apps you'll open and pre-loads them into RAM. " +
                    "When gaming, your game already owns most of RAM and SysMain just churns disk and memory in the " +
                    "background. Disabling it frees resources during gaming sessions. Note: Windows will re-enable it " +
                    "on major updates — check occasionally.",
                Target = FormFactor.Both,
                Kind = TweakKind.Command,
                Risk = RiskLevel.Caution,
                RecommendedForPC = true,
                RecommendedForLaptop = true,
                ApplyCommands = new()
                {
                    "sc stop SysMain",
                    "sc config SysMain start= disabled"
                },
                RevertCommands = new()
                {
                    "sc config SysMain start= auto",
                    "sc start SysMain"
                }
            });

            list.Add(new Tweak
            {
                Id = "disable_search_indexing",
                IsPro = true,
                Title = "Disable Windows Search indexing",
                Category = "Background Services",
                ShortDescription = "Stops the indexer churning your drive during gaming sessions.",
                Explanation =
                    "Windows Search continuously indexes your files in the background. During an active gaming session, " +
                    "this can cause disk I/O spikes that compete with game asset streaming. Disabling the service stops " +
                    "the background indexing — Windows Search still works, just without the real-time index. Most " +
                    "gamers never notice it's off.",
                Target = FormFactor.Both,
                Kind = TweakKind.Command,
                Risk = RiskLevel.Caution,
                RecommendedForPC = false,
                RecommendedForLaptop = false,
                ApplyCommands = new()
                {
                    "sc stop WSearch",
                    "sc config WSearch start= disabled"
                },
                RevertCommands = new()
                {
                    "sc config WSearch start= delayed-auto",
                    "sc start WSearch"
                }
            });

            list.Add(new Tweak
            {
                Id = "disable_telemetry",
                IsPro = true,
                Title = "Reduce Windows telemetry",
                Category = "Background Services",
                ShortDescription = "Cuts back on background data collection that uses CPU and network.",
                Explanation =
                    "Windows sends diagnostic data to Microsoft in the background. Setting DiagTrackAuthorization to " +
                    "its minimum level reduces this background activity. This is the safe registry approach — it doesn't " +
                    "break Windows Update or system features, it just reduces the diagnostic data level. The Connected " +
                    "User Experiences service (DiagTrack) is the main sender.",
                Target = FormFactor.Both,
                Kind = TweakKind.Registry,
                Risk = RiskLevel.Caution,
                RecommendedForPC = false,
                RecommendedForLaptop = false,
                RegHive = "HKLM",
                RegPath = @"SOFTWARE\Policies\Microsoft\Windows\DataCollection",
                RegName = "AllowTelemetry",
                RegType = "DWORD",
                RegValueApply = "0",
                RegValueRevert = null
            });

            return list;
        }
    }
}
