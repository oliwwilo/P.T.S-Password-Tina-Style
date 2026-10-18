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
    // ============ ПЕРЕМЕННЫЕ ============
     private Dictionary<string, List<PasswordEntry>> allPasswords = new();
    private string currentGroup = "";
    private List<PasswordEntry> currentPasswords = new();
    private PasswordEntry? editingPassword = null;
    private Dictionary<string, string> groupImages = new();
    private bool _isDrawerOpen;           //  Без = false
    private bool _isAuthenticated;
    private Point _dragStartPoint;
    private bool _isDraggingPassword;
    private string? _dragTargetGroup;
    private Border? _dragTargetBorder;
    private DispatcherTimer? _dragScrollTimer;
    private Point _dragLastPointViewport;
    private List<PasswordEntry> _selectedPasswords = new();
    private bool _isSelectionMode = false;
    private Border? _importOverlay;
    private ProgressBar? _importProgress;
    private TextBlock? _importPercentText;
    private TextBlock? _importLogText;
    private Button? _clearAllButton;
    private TextBlock? _importSpeedText;
    private Dictionary<string, Guid> groupIds = new();
   private CancellationTokenSource? _gifCancellationToken;
    private DatabaseService _dbService;
    private bool _isSaving = false;             // Защита от повторного сохранения
    private readonly object _saveLock = new object();
    private List<string> groupOrder = new List<string>();
    private readonly Dictionary<ScrollViewer, double> _smoothScrollTargets = new();
    private readonly Dictionary<ScrollViewer, double> _smoothScrollLastY = new();
    private DispatcherTimer? _smoothScrollTimer;
    
    // ============ КОНСТРУКТОР ============
    public MainWindow()
    {
        _dbService = new DatabaseService();
        InitializeComponent();
        SubscribeEvents();
        SetupControls();
        SetupDrawerAnimation();
        InitGroupIconDragDrop();
        this.Closing += OnWindowClosing; 
        _groupNamesVisible = LoadGroupNamesState();
        PrepareMainScreen();
        ApplyGroupNamesState();
        StartStarBackground();
        _ = InitializeAsync();
    }

}
