using System;
using System.Threading.Tasks;

namespace TinaPASS.Main;

public partial class MainWindow
{
    private async void ShowToast(string message)
    {
        // Тост использует тот же стек уведомлений, что и успех/ошибка,
        // поэтому всегда появляется справа сверху и не перекрывает другие.
        await ShowNotification(message, "#323232", TimeSpan.FromMilliseconds(2000));
    }
}
