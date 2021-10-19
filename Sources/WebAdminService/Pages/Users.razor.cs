using System.Threading.Tasks;
using WebAdminService.Data;

namespace WebAdminService.Pages
{
    public partial class Users
    {
        private CurrentUsersService.UserDataPresentor[]? _users;

        protected override async Task OnInitializedAsync()
        {
            var usrs = (await this.CurrentUsersService.GetUsersAsync())
                       ?? new CurrentUsersService.UserDataPresentor[0];

            this._users = usrs;
        }

        protected async Task SaveRow(CurrentUsersService.UserDataPresentor userData)
        {
            await this.CurrentUsersService.UpdateUserAsync(userData);
        }
    }
}