using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace TinaPASS.Main;

public partial class MainWindow : Window
{
    // ============ Подписка на события ============
    
    private void SubscribeEvents()
    {
        if (DrawerPanelControl != null)
        {
            DrawerPanelControl.ThemeRequested += OnThemeRequested;
        }

// Загружаем тему
        
        if (LoginOverlayControl != null)
        {
            LoginOverlayControl.LoginCompleted += OnLoginCompleted;
            LoginOverlayControl.RegisterRequested += OnRegisterRequested;
            LoginOverlayControl.GuestRequested += OnGuestRequested;
        }

        if (DrawerPanelControl != null)
        {
            DrawerPanelControl.CloseRequested += (s, e) => _ = CloseDrawer();
            DrawerPanelControl.LoginRequested += OnDrawerLoginRequested;
            DrawerPanelControl.LogoutRequested += OnDrawerLogoutRequested;
            DrawerPanelControl.SaveModeChanged += OnDrawerSaveModeChanged;
            DrawerPanelControl.AvatarClick += OnDrawerAvatarClick;
            DrawerPanelControl.ProfileRequested += OnProfileRequested;
            DrawerPanelControl.ThemeRequested += OnThemeRequested; 
            DrawerPanelControl.LicenceRequested += OnLicenceRequested;
        }

        if (RegisterOverlayControl != null)
        {
            RegisterOverlayControl.BackRequested += OnBackToLogin;
            RegisterOverlayControl.RegisterCompleted += OnRegisterCompleted;
        }

        if (ImportOverlayControl != null)
        {
            ImportOverlayControl.ContinueRequested += OnImportContinue;
        }

        CreatePanel.GroupCreated += OnGroupCreated;
        CreatePanel.Cancelled += OnPanelCancelled;

        SwitshiControl.PasswordSaved += OnPasswordSaved;
        SwitshiControl.Cancelled += OnPasswordPanelCancelled;
    }

    // ============ Инициализация контролов ============
    private void SetupControls()
    {
        
        GroupNameEditor = this.FindControl<TextBox>("GroupNameEditor");
        if (GroupNameEditor != null)
        {
            GroupNameEditor.TextChanged += OnGroupNameChanged;
        }

        GroupIcon = this.FindControl<Image>("GroupIcon");
        GroupIconBorder = this.FindControl<Border>("GroupIconBorder");
        
        if (GroupIconBorder != null)
        {
            GroupIconBorder.CornerRadius = new CornerRadius(15);
            GroupIconBorder.ClipToBounds = true;
        }
        ImportButton = this.FindControl<Button>("ImportButton");
        if (ImportButton != null)
            ImportButton.IsVisible = false; // скрываем кнопку импорта
        _clearAllButton = this.FindControl<Button>("ClearAllButton");
        if (_clearAllButton != null)
            _clearAllButton.IsVisible = false; // скрываем кнопку очистки
        this.KeyDown += OnKeyDown;
        this.AddHandler(
            InputElement.PointerWheelChangedEvent,
            (EventHandler<PointerWheelEventArgs>)OnSmoothPointerWheelChanged,
            RoutingStrategies.Tunnel);
    }

}
