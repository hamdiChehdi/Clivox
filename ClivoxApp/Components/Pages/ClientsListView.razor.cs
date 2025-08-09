using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using ClivoxApp.Models.Clients;
using ClivoxApp.Components.Pages.Clients;

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

        private IEnumerable<Client> filteredClients => string.IsNullOrWhiteSpace(searchQuery)
            ? clients
            : clients.Where(c =>
                (c.IsCompany && !string.IsNullOrWhiteSpace(c.CompanyName) && c.CompanyName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                (!c.IsCompany && c.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)) ||
                (c.Email is not null && c.Email.Contains(searchQuery, StringComparison.OrdinalIgnoreCase)));

        protected override async Task OnInitializedAsync()
        {
            clients = (await ClientRepository.GetAllClientsWithJobCountsAsync()).ToList();
        }

        private async Task DeleteClient(Guid clientId)
        {
            var confirmation = await DialogService.ShowMessageBox(
                "Delete Client",
                "Are you sure you want to delete this client?",
                yesText: "Delete", cancelText: "Cancel");

            if (confirmation == true)
            {
                await ClientRepository.DeleteClientAsync(clientId);
                clients = clients.Where(c => c.Id != clientId).ToList();
                Snackbar.Add("Client deleted successfully", Severity.Success);
            }
        }

        private async Task EditClient(Client client)
        {
            // Create a deep copy of the original client for editing
            var clientCopy = client.DeepCopy();
            
            var options = new DialogOptions { CloseOnEscapeKey = true };
            var parameters = new DialogParameters { ["Client"] = clientCopy };
            var dialog = await DialogService.ShowAsync<EditClientDialog>("Edit Client", parameters, options);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
                return;
            
            try
            {
                // Copy the edited values back to the original client ID to maintain identity
                clientCopy.Id = client.Id;
                clientCopy.Version = client.Version;
                clientCopy.CreatedOn = client.CreatedOn;
                
                await ClientRepository.UpdateClientAsync(clientCopy);
                clients = (await ClientRepository.GetAllClientsWithJobCountsAsync()).ToList();
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

            await ClientRepository.AddClientAsync(client);
            clients = (await ClientRepository.GetAllClientsWithJobCountsAsync()).ToList();
            Snackbar.Add("Client created successfully", Severity.Success);
        }

        private void NavigateToJobs(Guid clientId)
        {
            NavigationManager.NavigateTo($"/jobs/{clientId}");
        }
    }
}