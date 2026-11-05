using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace DetectEncodingNet48
{
  /// <summary>
  /// Logique d'interaction pour MainWindow.xaml
  /// </summary>
  public partial class MainWindow: Window
  {
    private bool _wasMaximized;
    private bool _isClosing;
    private bool _isLoaded;

    public MainWindow()
    {
      InitializeComponent();
      AddVersionToTitle();
      StateChanged += (sender, args) =>
      {
        if (!_isClosing && WindowState != WindowState.Minimized)
        {
          _wasMaximized = WindowState == WindowState.Maximized;
          SaveWindowBounds();
        }
      };
      Loaded += (sender, args) =>
      {
        if (_wasMaximized)
          WindowState = WindowState.Maximized;
        _isLoaded = true;
      };
      RestoreWindowBounds();
      Closing += (sender, args) =>
      {
        _isClosing = true;
        if (WindowState != WindowState.Minimized)
          _wasMaximized = WindowState == WindowState.Maximized;
      };
      Closed += (sender, args) => SaveWindowBounds();
    }

    private static string WindowBoundsPath
    {
      get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "window-bounds.xml"); }
    }

    private void RestoreWindowBounds()
    {
      try
      {
        if (!File.Exists(WindowBoundsPath))
          return;

        WindowBounds bounds;
        var serializer = new XmlSerializer(typeof(WindowBounds));
        using (var stream = File.OpenRead(WindowBoundsPath))
          bounds = (WindowBounds)serializer.Deserialize(stream);

        if (!HasValidBounds(bounds))
          throw new InvalidDataException("Les dimensions enregistrées sont invalides.");

        var width = Math.Min(bounds.Width, SystemParameters.VirtualScreenWidth);
        var height = Math.Min(bounds.Height, SystemParameters.VirtualScreenHeight);
        Width = width;
        Height = height;
        Left = Clamp(
          bounds.Left,
          SystemParameters.VirtualScreenLeft,
          SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - width);
        Top = Clamp(
          bounds.Top,
          SystemParameters.VirtualScreenTop,
          SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - height);
        _wasMaximized = bounds.IsMaximized;
      }
      catch (Exception exception)
      {
        if (exception is IOException ||
            exception is UnauthorizedAccessException ||
            exception is InvalidOperationException ||
            exception is InvalidDataException)
        {
          MessageBox.Show(
            "Impossible de charger la position de la fenêtre : " + exception.Message,
            "Paramètres de fenêtre",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        }
        else
        {
          throw;
        }
      }
    }

    private void SaveWindowBounds()
    {
      var bounds = RestoreBounds;
      if (!_isLoaded || !HasValidBounds(bounds))
        return;

      try
      {
        var serializer = new XmlSerializer(typeof(WindowBounds));
        using (var stream = File.Create(WindowBoundsPath))
        {
          serializer.Serialize(stream, new WindowBounds
          {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            IsMaximized = _wasMaximized
          });
        }
      }
      catch (Exception exception)
      {
        if (exception is IOException || exception is UnauthorizedAccessException)
        {
          MessageBox.Show(
            "Impossible d'enregistrer la position de la fenêtre : " + exception.Message,
            "Paramètres de fenêtre",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
        }
        else
        {
          throw;
        }
      }
    }

    private static bool IsFinite(double value)
    {
      return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool HasValidBounds(WindowBounds bounds)
    {
      return bounds != null &&
             IsFinite(bounds.Left) &&
             IsFinite(bounds.Top) &&
             IsFinite(bounds.Width) &&
             IsFinite(bounds.Height) &&
             bounds.Width > 0 &&
             bounds.Height > 0;
    }

    private static bool HasValidBounds(Rect bounds)
    {
      return IsFinite(bounds.Left) &&
             IsFinite(bounds.Top) &&
             IsFinite(bounds.Width) &&
             IsFinite(bounds.Height) &&
             bounds.Width > 0 &&
             bounds.Height > 0;
    }

    private static double Clamp(double value, double minimum, double maximum)
    {
      return Math.Max(minimum, Math.Min(value, maximum));
    }

    private void AddVersionToTitle()
    {
      var version = Assembly.GetExecutingAssembly().GetName().Version;
      Title += " v" + version;
    }

    public class WindowBounds
    {
      public double Left { get; set; }
      public double Top { get; set; }
      public double Width { get; set; }
      public double Height { get; set; }
      public bool IsMaximized { get; set; }
    }

    private async void BtnDetecter_Click(object sender, RoutedEventArgs e)
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
        var result = await Task.Run(() => EncodingDetector.DetectFile(path));
        TxtEncodage.Text = "Encodage : " + result.EncodingName;
        TxtConfiance.Text = string.Format(
          "Confiance : {0:P1} | BOM : {1}",
          result.Confidence,
          result.HasBom ? "Oui" : "Non");
        TxtContenu.Text = await Task.Run(
          () => File.ReadAllText(path, result.Encoding));
      }
      catch (Exception exception)
      {
        MessageBox.Show(exception.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
