using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;

namespace DetectEncoding
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow: Window
  {
    private bool _wasMaximized;
    private bool _isClosing;

    public MainWindow()
    {
      InitializeComponent();
      AddVersionToTitle();
      StateChanged += (_, _) =>
      {
        if (!_isClosing && WindowState != WindowState.Minimized)
        {
          _wasMaximized = WindowState == WindowState.Maximized;
          SaveWindowBounds();
        }
      };
      Loaded += (_, _) =>
      {
        if (_wasMaximized)
          WindowState = WindowState.Maximized;
      };
      RestoreWindowBounds();
      Closing += (_, _) =>
      {
        _isClosing = true;
        if (WindowState != WindowState.Minimized)
          _wasMaximized = WindowState == WindowState.Maximized;
      };
      Closed += (_, _) => SaveWindowBounds();
    }

    private static string WindowBoundsPath =>
      Path.Combine(AppContext.BaseDirectory, "window-bounds.json");

    private void RestoreWindowBounds()
    {
      try
      {
        if (!File.Exists(WindowBoundsPath))
          return;

        var bounds = JsonSerializer.Deserialize<WindowBounds>(
          File.ReadAllText(WindowBoundsPath));

        if (bounds is null ||
            !double.IsFinite(bounds.Left) ||
            !double.IsFinite(bounds.Top) ||
            !double.IsFinite(bounds.Width) ||
            !double.IsFinite(bounds.Height) ||
            bounds.Width <= 0 ||
            bounds.Height <= 0)
        {
          throw new InvalidDataException("Les dimensions enregistrées sont invalides.");
        }

        var width = Math.Min(bounds.Width, SystemParameters.VirtualScreenWidth);
        var height = Math.Min(bounds.Height, SystemParameters.VirtualScreenHeight);
        Width = width;
        Height = height;
        Left = Math.Clamp(
          bounds.Left,
          SystemParameters.VirtualScreenLeft,
          SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - width);
        Top = Math.Clamp(
          bounds.Top,
          SystemParameters.VirtualScreenTop,
          SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - height);
        _wasMaximized = bounds.IsMaximized;
      }
      catch (Exception exception) when (
        exception is IOException or UnauthorizedAccessException or JsonException)
      {
        MessageBox.Show(
          $"Impossible de charger la position de la fenêtre : {exception.Message}",
          "Paramètres de fenêtre",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
      }
    }

    private void SaveWindowBounds()
    {
      var bounds = RestoreBounds;
      try
      {
        var directory = Path.GetDirectoryName(WindowBoundsPath)
          ?? throw new InvalidOperationException("Le dossier des paramètres est introuvable.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(WindowBoundsPath, JsonSerializer.Serialize(
          new WindowBounds(
            bounds.Left,
            bounds.Top,
            bounds.Width,
            bounds.Height,
            _wasMaximized)));
      }
      catch (Exception exception) when (
        exception is IOException or UnauthorizedAccessException)
      {
        MessageBox.Show(
          $"Impossible d'enregistrer la position de la fenêtre : {exception.Message}",
          "Paramètres de fenêtre",
          MessageBoxButton.OK,
          MessageBoxImage.Warning);
      }
    }

    private void AddVersionToTitle()
    {
      var version = Assembly.GetExecutingAssembly().GetName().Version;
      Title += $" v{version}";
    }

    private sealed record WindowBounds(
      double Left,
      double Top,
      double Width,
      double Height,
      bool IsMaximized = false);

    private async void BtnDetecter_Click(
           object sender, RoutedEventArgs e)
    {
      var dialog = new OpenFileDialog
      {
        Filter = "Fichiers texte|*.txt;*.csv;*.log|Tous les fichiers|*.*"
      };

      if (dialog.ShowDialog() != true)
        return;

      string path = dialog.FileName;

      TxtFichier.Text = path;

      try
      {
        // Détection en arrière-plan
        var result = await Task.Run(() => EncodingDetector.DetectFile(path));

        TxtEncodage.Text = $"Encodage : {result.EncodingName}";

        TxtConfiance.Text = $"Confiance : {result.Confidence:P1} | " + $"BOM : {(result.HasBom ? "Oui" : "Non")}";

        // Lecture avec l'encodage détecté
        TxtContenu.Text = await File.ReadAllTextAsync(path, result.Encoding);
      }
      catch (Exception exception)
      {
        MessageBox.Show(exception.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
