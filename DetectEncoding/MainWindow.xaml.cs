using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;

namespace DetectEncoding
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow: Window
  {
    public MainWindow()
    {
      InitializeComponent();
      AddVersionToTitle();
    }

    private void AddVersionToTitle()
    {
      var version = Assembly.GetExecutingAssembly().GetName().Version;
      Title += $" v{version}";
    }

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
