using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArchivumWpf.Models;
using ArchivumWpf.Services;
using MaterialDesignThemes.Wpf;

namespace ArchivumWpf.ViewModels;

public partial class CirculationViewModel : ObservableObject
{
    private readonly IArchiveService _archiveService;
    
    [ObservableProperty] private string _issueRrNumber = string.Empty;
    [ObservableProperty] private string _issueBorrowerName = string.Empty;
    [ObservableProperty] private string _returnRrNumber = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private string _statusColor = "White";
    [ObservableProperty] private ObservableCollection<BorrowRecord> _activeLoans = new();

    public CirculationViewModel(IArchiveService archiveService)
    {
        _archiveService = archiveService;
        _ = LoadActiveLoadAsync();
    }

    private async Task LoadActiveLoadAsync()
    {
        var loans = await _archiveService.GetActiveLoansAsync();
        ActiveLoans.Clear();
        foreach (var loan in loans) ActiveLoans.Add(loan);
    }

    [RelayCommand]
    private async Task IssueFileAsync()
    {
        if (string.IsNullOrWhiteSpace(IssueRrNumber) || string.IsNullOrWhiteSpace(IssueBorrowerName))
        {
            ShowStatus ("Please fill in both RR Number and Borrower Name", "Red");
            return;
        }

        var result = await _archiveService.IssueFileAsync(IssueRrNumber, IssueBorrowerName);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");

        if (result.Success)
        {
            IssueRrNumber = string.Empty;
            IssueBorrowerName = string.Empty;
            await LoadActiveLoadAsync();
        }
    }

    [RelayCommand]
    private async Task ReturnFileAsync()
    {
        if (string.IsNullOrWhiteSpace(ReturnRrNumber))
        {
            ShowStatus("Please enter the RR Number to return.", "Red");
            return;
        }

        var result = await _archiveService.ReturnFileASync(ReturnRrNumber);
        ShowStatus(result.Message, result.Success ? "#4CAF50" : "#F44336");

        if (result.Success)
        {
            ReturnRrNumber = string.Empty;
            await LoadActiveLoadAsync();
        }
    }

    private void ShowStatus(string message, string color)
    {
        StatusMessage = message;
        StatusColor = color;
    }

}