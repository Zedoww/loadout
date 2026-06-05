using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Loadout.App.Converters;
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
    [ObservableProperty] private string _sizeText = "—";
    [ObservableProperty] private bool _scanned;
}

public partial class CleanupViewModel : ObservableObject
{
    private readonly CleanupService _cleanup;
    private readonly RestorePointService _restorePoint;

    [ObservableProperty] private string _scanSummary = "Run a scan to preview reclaimable space per category.";
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<CleanupCategoryItem> Categories { get; } = new();
    public ObservableCollection<string> Log { get; } = new();

    public CleanupViewModel(CleanupService cleanup, RestorePointService restorePoint)
    {
        _cleanup = cleanup;
        _restorePoint = restorePoint;

        foreach (var category in _cleanup.Categories)
            Categories.Add(new CleanupCategoryItem(category));
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
                row.SizeText = BytesToReadableConverter.Format(item.Bytes);
                row.Scanned = true;
                total += item.Bytes;
            }
            ScanSummary = $"Total reclaimable: {BytesToReadableConverter.Format(total)} " +
                          "— tick the categories you want, then Clean selected.";
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

        IsBusy = true;
        Log.Clear();
        try
        {
            // A Caution category has a user-visible effect: take a restore point
            // first so the system state is recoverable.
            if (selected.Any(c => c.IsCaution))
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
                if (row is not null) row.SizeText = "—";
            }

            ScanSummary = $"Cleanup complete: {BytesToReadableConverter.Format(freed)} freed.";
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
