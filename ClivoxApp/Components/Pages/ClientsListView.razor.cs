using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ClivoxApp.Models.Clients;
using ClivoxApp.Components.Pages.Clients;
using ClivoxApp.Models.Shared;

namespace ClivoxApp.Components.Pages
{
    public partial class ClientsListView : ComponentBase
    {
        [Inject] private ClientRepository ClientRepository { get; set; } = null!;
        [Inject] private IDialogService DialogService { get; set; } = null!;
        [Inject] private ISnackbar Snackbar { get; set; } = null!;
        [Inject] private NavigationManager NavigationManager { get; set; } = null!;
        
        private List<Client> clients = new();
        private string searchQuery = string.Empty;
        private Client client = new();
        private ClientFilter currentFilter = new();
        private bool isLoading = false;

        // Filter data
        private List<int> clientCreationYears = new();
        private List<int> invoiceYears = new();
        private List<Countries> availableCountries = new();

        private IEnumerable<Client> filteredClients => string.IsNullOrWhiteSpace(searchQuery)
            ? clients
            : clients.Where(c =>
                (c.IsCompany && !string.IsNullOrWhiteSpace(c.CompanyName) && c.CompanyName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                (!c.IsCompany && c.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                (c.Email is not null && c.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));

        protected override async Task OnInitializedAsync()
        {
            await LoadClients();
            await LoadFilterData();
        }

        private async Task LoadClients()
        {
            isLoading = true;
            StateHasChanged();

            try
            {
                if (currentFilter.HasActiveFilters)
                {
                    clients = (await ClientRepository.GetFilteredClientsAsync(currentFilter)).ToList();
                }
                else
                {
                    clients = (await ClientRepository.GetAllClientsWithJobCountsAsync()).ToList();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading clients: {ex.Message}", Severity.Error);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        }

        private async Task LoadFilterData()
        {
            try
            {
                clientCreationYears = await ClientRepository.GetClientCreationYearsAsync();
                invoiceYears = await ClientRepository.GetInvoiceYearsAsync();
                availableCountries = await ClientRepository.GetAvailableCountriesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading filter data: {ex.Message}");
            }
        }

        private async Task OpenFilterDialog()
        {
            var filterCopy = currentFilter.DeepCopy();
            
            var parameters = new DialogParameters 
            { 
                ["Filter"] = filterCopy,
                ["ClientCreationYears"] = clientCreationYears,
                ["InvoiceYears"] = invoiceYears,
                ["AvailableCountries"] = availableCountries
            };
            
            var options = new DialogOptions 
            { 
                CloseOnEscapeKey = true,
                MaxWidth = MaxWidth.Large,
                FullWidth = true
            };
            
            var dialog = await DialogService.ShowAsync<ClientFilterDialog>("Advanced Client Filters", parameters, options);
            var result = await dialog.Result;
            
            if (result is not null && !result.Canceled && result.Data is ClientFilter newFilter)
            {
                currentFilter = newFilter;
                searchQuery = currentFilter.SearchQuery ?? string.Empty;
                await LoadClients();
                
                if (currentFilter.HasActiveFilters)
                {
                    Snackbar.Add($"Filters applied - showing {clients.Count} clients", Severity.Info);
                }
            }
        }

        private async Task ClearAllFilters()
        {
            currentFilter.Reset();
            searchQuery = string.Empty;
            await LoadClients();
            Snackbar.Add("All filters cleared", Severity.Success);
        }

        private async Task OnSearchChanged(string newSearchQuery)
        {
            searchQuery = newSearchQuery;
            currentFilter.SearchQuery = newSearchQuery;
            
            if (currentFilter.HasActiveFilters && newSearchQuery.Length >= 2)
            {
                await LoadClients();
            }
        }

        private async Task DeleteClient(Guid clientId)
        {
            var confirmation = await DialogService.ShowMessageBox(
                "Delete Client",
                "Are you sure you want to delete this client?",
                yesText: "Delete", cancelText: "Cancel");

            if (confirmation == true)
            {
                try
                {
                    await ClientRepository.DeleteClientAsync(clientId);
                    clients = clients.Where(c => c.Id != clientId).ToList();
                    Snackbar.Add("Client deleted successfully", Severity.Success);
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error deleting client: {ex.Message}", Severity.Error);
                }
            }
        }

        private async Task EditClient(Client client)
        {
            var clientCopy = client.DeepCopy();
            
            var options = new DialogOptions { CloseOnEscapeKey = true };
            var parameters = new DialogParameters { ["Client"] = clientCopy };
            var dialog = await DialogService.ShowAsync<EditClientDialog>("Edit Client", parameters, options);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
                return;
            
            try
            {
                clientCopy.Id = client.Id;
                clientCopy.Version = client.Version;
                clientCopy.CreatedOn = client.CreatedOn;
                
                await ClientRepository.UpdateClientAsync(clientCopy);
                await LoadClients();
                Snackbar.Add("Client updated successfully", Severity.Success);
            }
            catch (ArgumentException ex)
            {
                Snackbar.Add($"Failed to update client: {ex.Message}", Severity.Error);
            }
        }

        private async Task OpenAddClientDialog()
        {
            client = new Client();
            var options = new DialogOptions { CloseOnEscapeKey = true };
            var parameters = new DialogParameters { ["Client"] = client };
            var dialog = await DialogService.ShowAsync<EditClientDialog>("New Client", parameters, options);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
                return;

            try
            {
                await ClientRepository.AddClientAsync(client);
                await LoadClients();
                await LoadFilterData();
                Snackbar.Add("Client created successfully", Severity.Success);
            }
            catch (ArgumentException ex)
            {
                Snackbar.Add($"Failed to create client: {ex.Message}", Severity.Error);
            }
        }

        private void NavigateToJobs(Guid clientId)
        {
            NavigationManager.NavigateTo($"/jobs/{clientId}");
        }
    }
}