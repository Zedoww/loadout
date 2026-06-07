using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.App.Converters;
using Loadout.App.Services;
using Loadout.Core.Backup;
using Loadout.Core.Optimization.Cleanup;

namespace Loadout.App.ViewModels;

/// <summary>One selectable cleanup category row on the Cleanup page.</summary>
public partial class CleanupCategoryItem : ObservableObject
{
    public CleanupCategoryItem(CleanupCategory category)
    {
        Id = category.Id;
        Name = category.Name;
        Description = category.Description;
        IsCaution = category.Risk == CleanupRisk.Caution;
        IsSelected = category.SelectedByDefault;
    }

    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public bool IsCaution { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private long _bytes;
    [ObservableProperty] private string _sizeText = "—";
    [ObservableProperty] private bool _scanned;
}

public partial class CleanupViewModel : ObservableObject
{
    private readonly CleanupService _cleanup;
    private readonly RestorePointService _restorePoint;
    private readonly IConfirmationService _confirm;
    private bool _suppressSelectionSync;

    [ObservableProperty] private string _scanSummary = "Run a scan to preview reclaimable space per category.";
    [ObservableProperty] private string _selectedTotalText = "Nothing selected.";
    [ObservableProperty] private bool _isAllSelected;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<CleanupCategoryItem> Categories { get; } = new();
    public ObservableCollection<string> Log { get; } = new();

    public CleanupViewModel(CleanupService cleanup, RestorePointService restorePoint,
        IConfirmationService confirm)
    {
        _cleanup = cleanup;
        _restorePoint = restorePoint;
        _confirm = confirm;

        foreach (var category in _cleanup.Categories)
        {
            var item = new CleanupCategoryItem(category);
            item.PropertyChanged += OnItemPropertyChanged;
            Categories.Add(item);
        }

        SyncAllSelected();
        UpdateSelectedTotal();
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CleanupCategoryItem.IsSelected)) return;
        if (_suppressSelectionSync) return;

        SyncAllSelected();
        UpdateSelectedTotal();
    }

    partial void OnIsAllSelectedChanged(bool value)
    {
        if (_suppressSelectionSync) return;

        _suppressSelectionSync = true;
        foreach (var item in Categories) item.IsSelected = value;
        _suppressSelectionSync = false;

        UpdateSelectedTotal();
    }

    /// <summary>Reflects the header checkbox from the individual rows (no feedback loop).</summary>
    private void SyncAllSelected()
    {
        _suppressSelectionSync = true;
        IsAllSelected = Categories.Count > 0 && Categories.All(c => c.IsSelected);
        _suppressSelectionSync = false;
    }

    private void UpdateSelectedTotal()
    {
        var selected = Categories.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            SelectedTotalText = "Nothing selected.";
            return;
        }

        bool anyScanned = selected.Any(c => c.Scanned);
        long total = selected.Sum(c => c.Bytes);
        SelectedTotalText = anyScanned
            ? $"{selected.Count} selected — {BytesToReadableConverter.Format(total)} to free."
            : $"{selected.Count} selected — run a scan to size them.";
    }

    [RelayCommand]
    private async Task ScanAsync()
    {
        IsBusy = true;
        try
        {
            var items = await Task.Run(() => _cleanup.ScanAll());
            long total = 0;
            foreach (var item in items)
            {
                var row = Categories.FirstOrDefault(c => c.Id == item.Category.Id);
                if (row is null) continue;
                row.Bytes = item.Bytes;
                row.SizeText = BytesToReadableConverter.Format(item.Bytes);
                row.Scanned = true;
                total += item.Bytes;
            }
            ScanSummary = $"Total reclaimable: {BytesToReadableConverter.Format(total)} " +
                          "— tick the categories you want, then Clean selected.";
            UpdateSelectedTotal();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CleanAsync()
    {
        var selected = Categories.Where(c => c.IsSelected).ToList();
        if (selected.Count == 0)
        {
            Log.Add("Nothing selected — pick at least one category.");
            return;
        }

        // Confirm before deleting — Caution categories (Recycle Bin, caches) are
        // not individually recoverable.
        long total = selected.Sum(c => c.Bytes);
        bool anyCaution = selected.Any(c => c.IsCaution);
        string list = string.Join(", ", selected.Select(c => c.Name));
        string sizeNote = selected.Any(c => c.Scanned)
            ? $" (~{BytesToReadableConverter.Format(total)})"
            : "";
        string warn = anyCaution
            ? "\n\nItems in the Recycle Bin and browser caches cannot be recovered."
            : "";

        bool ok = await _confirm.ConfirmAsync(
            "Delete selected items?",
            $"This will clean: {list}{sizeNote}.{warn}",
            confirmText: "Clean");
        if (!ok) return;

        IsBusy = true;
        Log.Clear();
        try
        {
            // A Caution category has a user-visible effect: take a restore point
            // first so the system state is recoverable.
            if (anyCaution)
            {
                var rp = await Task.Run(() =>
                    _restorePoint.Create($"Loadout cleanup — {DateTime.Now:g}"));
                Log.Add((rp.Success ? "✓ " : "✗ ") + rp.Message);
            }

            var ids = selected.Select(c => c.Id).ToList();
            var results = await Task.Run(() => _cleanup.Clean(ids));

            long freed = 0;
            foreach (var r in results)
            {
                string line = (r.Result.Success ? "✓ " : "✗ ") + r.Category.Name + ": " + r.Result.Message;
                if (r.Result.BytesFreed > 0)
                    line += $" — {BytesToReadableConverter.Format(r.Result.BytesFreed)} freed";
                Log.Add(line);
                freed += r.Result.BytesFreed;

                var row = Categories.FirstOrDefault(c => c.Id == r.Category.Id);
                if (row is not null) { row.Bytes = 0; row.SizeText = "—"; row.Scanned = false; }
            }

            ScanSummary = $"Cleanup complete: {BytesToReadableConverter.Format(freed)} freed.";
            UpdateSelectedTotal();
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateRestorePointAsync()
    {
        IsBusy = true;
        try
        {
            var result = await Task.Run(() =>
                _restorePoint.Create($"Loadout — {DateTime.Now:g}"));
            Log.Add((result.Success ? "✓ " : "✗ ") + result.Message);
        }
        finally { IsBusy = false; }
    }
}
