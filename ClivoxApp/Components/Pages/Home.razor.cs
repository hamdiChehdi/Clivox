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
    public partial class Home : ComponentBase
    {
        [Inject] private ClientRepository ClientRepository { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
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
            clients = (await ClientRepository.GetAllClientsAsync()).ToList();
        }

        private async Task DeleteClient(Guid clientId)
        {
            await ClientRepository.DeleteClientAsync(clientId);
            clients = clients.Where(c => c.Id != clientId).ToList();
        }

        private async Task EditClient(Client client)
        {
            var options = new DialogOptions { CloseOnEscapeKey = true };
            var parameters = new DialogParameters { ["Client"] = client };
            var dialog = await DialogService.ShowAsync<EditClientDialog>("Edit Client", parameters, options);
            var result = await dialog.Result;
            if (result is null || result.Canceled)
                return;
            await ClientRepository.UpdateClientAsync(client);
            clients = (await ClientRepository.GetAllClientsAsync()).ToList();
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
            clients = (await ClientRepository.GetAllClientsAsync()).ToList();
        }
    }
}
