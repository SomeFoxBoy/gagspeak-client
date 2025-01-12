using Dalamud.Interface;
using GagspeakAPI.Data.Interfaces;
using GagspeakAPI.Extensions;
using ImGuiNET;
using OtterGui.Raii;
using OtterGui.Text;

namespace GagSpeak.UI.Components.Combos;

// The true core of the abstract combos for padlocks. Handles all shared logic operations.
public abstract class PadlockBase<T> where T : IPadlockable
{
    protected readonly ILogger _logger;
    protected readonly UiSharedService _uiShared;

    // Basic identifiers used for the combos.
    protected string _label = string.Empty;
    protected string _password = string.Empty;
    protected string _timer = string.Empty;
    protected Padlocks _selectedLock = Padlocks.None;

    protected PadlockBase(ILogger log, UiSharedService uiShared, string comboLabelBase)
    {
        _logger = log;
        _uiShared = uiShared;
    }

    /// <summary>
    /// Contains the list of padlocks. These locks are obtained via the extract padlocks function.
    /// </summary>
    protected IEnumerable<Padlocks> ComboPadlocks => ExtractPadlocks();

    // Resets selections and all inputs.
    public void ResetSelection()
    {
        _selectedLock = Padlocks.None;
        ResetInputs();
    }

    public void ResetInputs()
    {
        _password = string.Empty;
        _timer = string.Empty;
    }

    public float PadlockLockWindowHeight() => _selectedLock.IsTwoRowLock()
        ? ImGui.GetFrameHeight() * 3 + ImGui.GetStyle().ItemSpacing.Y * 2
        : ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y;

    public float PadlockUnlockWindowHeight() => GetLatestPadlock().IsPasswordLock()
        ? ImGui.GetFrameHeight() * 2 + ImGui.GetStyle().ItemSpacing.Y
        : ImGui.GetFrameHeight();


    public virtual void DrawLockComboWithActive(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // For pairs, display the active item prior to the combo.
        DisplayActiveItem(width);
        // then draw out the combo.
        DrawLockCombo(width, tt, btt, flags);
    }

    public virtual void DrawLockCombo(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // we need to calculate the size of the button for locking, so do so.
        var buttonWidth = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Lock, "Lock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        // draw the combo box.
        ImGui.SetNextItemWidth(comboWidth);
        using var scrollbarWidth = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 12f);
        using (var combo = ImRaii.Combo("##" + _label + "-LockCombo", _selectedLock.ToName()))
        {
            // display the tooltip for the combo with visible.
            using (ImRaii.Enabled())
            {
                UiSharedService.AttachToolTip(tt);
                // Handle right click clearing.
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    ResetSelection();
            }

            // handle combo.
            if (combo)
            {
                foreach (var item in ComboPadlocks)
                    if (ImGui.Selectable(item.ToName(), item == _selectedLock))
                        _selectedLock = item;
            }
        }

        // draw button thing for locking / unlocking.
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Lock, "Lock", disabled: _selectedLock is Padlocks.None, id: "##" + _selectedLock + "-LockButton"))
            OnLockButtonPress();
        UiSharedService.AttachToolTip(btt);

        // on next line show lock fields.
        ShowLockFields();
    }

    public virtual void DrawUnlockCombo(float width, string tt, string btt, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        // we need to calculate the size of the button for locking, so do so.
        var buttonWidth = _uiShared.GetIconTextButtonSize(FontAwesomeIcon.Unlock, "Unlock");
        var comboWidth = width - buttonWidth - ImGui.GetStyle().ItemInnerSpacing.X;

        var lastPadlock = GetLatestPadlock();

        // display the active padlock for the set in a disabled view.
        using (ImRaii.Disabled(true))
        {
            ImGui.SetNextItemWidth(comboWidth);
            if (ImGui.BeginCombo("##" + _label + "-DisplayLock", lastPadlock.ToName())) { ImGui.EndCombo(); }
        }

        // draw button thing.
        ImUtf8.SameLineInner();
        if (_uiShared.IconTextButton(FontAwesomeIcon.Unlock, "Unlock", disabled: lastPadlock is Padlocks.None, id: "##" + _label + "-UnlockButton"))
            OnUnlockButtonPress();
        UiSharedService.AttachToolTip(btt);

        // on next line show lock fields.
        ShowUnlockFields(lastPadlock);
    }

    private void DisplayActiveItem(float width)
    {
        T activeItem = GetLatestActiveItem();
        // disable the actively selected padlock.
        ImGui.SetNextItemWidth(width);
        using (ImRaii.Disabled(true))
        {
            if (ImGui.BeginCombo("##" + _label + "ActiveDisplay", ToActiveItemString(activeItem))) ImGui.EndCombo();
        }
    }

    /// <summary>
    /// Abstract function method used to determine how the ComboPadlocks fetches its padlock list.
    /// </summary>
    protected abstract IEnumerable<Padlocks> ExtractPadlocks();
    protected abstract Padlocks GetLatestPadlock();
    protected abstract T GetLatestActiveItem();
    protected virtual string ToActiveItemString(T item) => item?.ToString() ?? string.Empty;
    protected abstract void OnLockButtonPress();
    protected abstract void OnUnlockButtonPress();

    protected void ShowLockFields()
    {
        if (_selectedLock is Padlocks.None)
            return;

        float width = ImGui.GetContentRegionAvail().X;
        switch (_selectedLock)
        {
            case Padlocks.CombinationPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Combination_Input", "Enter 4 digit combination...", ref _password, 4);
                break;
            case Padlocks.PasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                break;
            case Padlocks.TimerPasswordPadlock:
                ImGui.SetNextItemWidth(width * (2 / 3f));
                ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                ImUtf8.SameLineInner();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputTextWithHint("##Timer_Input", "Ex: 0h2m7s", ref _timer, 12);
                break;
            case Padlocks.TimerPadlock:
            case Padlocks.OwnerTimerPadlock:
            case Padlocks.DevotionalTimerPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Timer_Input", "Ex: 0h2m7s", ref _timer, 12);
                break;
        }
    }

    protected void ShowUnlockFields(Padlocks padlock)
    {
        if (!LockHelperExtensions.IsPasswordLock(padlock))
            return;

        float width = ImGui.GetContentRegionAvail().X;
        switch (padlock)
        {
            case Padlocks.CombinationPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Combination_Input", "Enter 4 digit combination...", ref _password, 4);
                break;
            case Padlocks.PasswordPadlock:
            case Padlocks.TimerPasswordPadlock:
                ImGui.SetNextItemWidth(width);
                ImGui.InputTextWithHint("##Password_Input", "Enter password...", ref _password, 20);
                break;
        }
    }
}
