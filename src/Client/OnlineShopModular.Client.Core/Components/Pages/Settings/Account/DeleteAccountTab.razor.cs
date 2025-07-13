﻿using OnlineShopModular.Shared.Controllers.Identity;

namespace OnlineShopModular.Client.Core.Components.Pages.Settings.Account;

public partial class DeleteAccountTab
{
    private bool isDialogOpen;

    [AutoInject] IUserController userController = default!;

    private async Task DeleteAccount()
    {
        if (await AuthManager.TryEnterElevatedAccessMode(CurrentCancellationToken))
        {
            await userController.Delete(CurrentCancellationToken);

            await AuthManager.SignOut(CurrentCancellationToken);
        }
    }
}
