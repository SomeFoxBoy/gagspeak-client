using GagSpeak.PlayerData.Pairs;
using GagSpeak.WebAPI;
using ImGuiNET;

namespace GagSpeak.UI.Components.Combos;

// responsible for updating and unifying custom pair combos.
public class PairCombos
{
    private readonly ILogger<PairCombos> _logger;
    private readonly MainHub _apiHubMain;
    private readonly SetPreviewComponent _previews;
    private readonly UiSharedService _uiShared;


    // Make the following static and public to allow global access throughout custom combos.
    public static InteractionType Opened = InteractionType.None;
    public static int GagLayer { get; private set; } = 0;

    public PairCombos(ILogger<PairCombos> logger, MainHub apiHubMain,
        SetPreviewComponent previews, UiSharedService uiShared)
    {
        _logger = logger;
        _apiHubMain = apiHubMain;
        _previews = previews;
        _uiShared = uiShared;
    }

    public PairGagCombo[] GagApplyCombos { get; private set; } = new PairGagCombo[3];
    public PairPadlockGag[] GagPadlockCombos { get; private set; } = new PairPadlockGag[3];
    public PairRestraintCombo RestraintApplyCombo { get; private set; } = null!;
    public PairPadlockRestraint RestraintPadlockCombos { get; private set; } = null!;
    public PairPatternCombo PatternCombo { get; private set; } = null!;
    public PairAlarmCombo AlarmToggleCombo { get; private set; } = null!;
    public PairTriggerCombo TriggerToggleCombo { get; private set; } = null!;



    public void DrawGagLayerSelection(float comboWidth)
    {
        ImGui.SetNextItemWidth(comboWidth);
        // The combo will now modify the static GagLayer variable
        var tempLayer = GagLayer;
        if (ImGui.Combo("##GagLayerSelection", ref tempLayer, new string[] { "Layer 1", "Layer 2", "Layer 3" }, 3))
        {
            if (tempLayer != GagLayer)
            {
                GagLayer = tempLayer;
                Opened = InteractionType.None;
            }
        }
        UiSharedService.AttachToolTip("Select the layer to apply a Gag to.");
    }

    public void UpdateCombosForPair(Pair pair)
    {
        // Create an array of PairGagCombo with 3 elements
        GagApplyCombos = new PairGagCombo[3]
        {
            new PairGagCombo(_logger, _previews, _apiHubMain, _uiShared, pair, "Apply Gag 1", "Apply a Gag to " + pair.GetNickAliasOrUid()),
            new PairGagCombo(_logger, _previews, _apiHubMain, _uiShared, pair, "Apply Gag 2", "Apply a Gag to " + pair.GetNickAliasOrUid()),
            new PairGagCombo(_logger, _previews, _apiHubMain, _uiShared, pair, "Apply Gag 3", "Apply a Gag to " + pair.GetNickAliasOrUid())
        };

        // Create an array of PairPadlockGag with 3 elements
        GagPadlockCombos = new PairPadlockGag[3]
        {
            new PairPadlockGag(_logger, _apiHubMain, _uiShared, pair, "PadlockLock1" + pair.UserData.UID),
            new PairPadlockGag(_logger, _apiHubMain, _uiShared, pair, "PadlockLock2" + pair.UserData.UID),
            new PairPadlockGag(_logger, _apiHubMain, _uiShared, pair, "PadlockLock3" + pair.UserData.UID),
        };

        // Create the Restraint Combos for the Pair.
        RestraintApplyCombo = new PairRestraintCombo(_logger, _previews, _apiHubMain, _uiShared, pair, "Apply", "Apply a Restraint to " + pair.GetNickAliasOrUid());
        RestraintPadlockCombos = new PairPadlockRestraint(_logger, _apiHubMain, _uiShared, pair, "PadlocksRestraint" + pair.UserData.UID);

        // Create the Pattern Combo for the Pair.
        PatternCombo = new PairPatternCombo(_logger, _apiHubMain, _uiShared, pair, "Execute", "Apply a Pattern to " + pair.GetNickAliasOrUid());

        // Create the pattern Combo for alarm Toggling.
        AlarmToggleCombo = new PairAlarmCombo(_logger, _apiHubMain, _uiShared, pair, "Enable", "Enable this Alarm for " + pair.GetNickAliasOrUid(), "Disable", "Disable this Alarm for " + pair.GetNickAliasOrUid());

        // Create the Triggers combo for trigger toggling.
        TriggerToggleCombo = new PairTriggerCombo(_logger, _apiHubMain, _uiShared, pair, "Enable", "Enable this Trigger for " + pair.GetNickAliasOrUid(), "Disable", "Disable this Trigger for " + pair.GetNickAliasOrUid());
    }
}
