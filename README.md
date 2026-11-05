# DetectEncoding

Cette solution contient deux applications WPF qui détectent l'encodage d'un
fichier texte et en affichent le contenu :

- **DetectEncoding** cible .NET 9 (`net9.0-windows`).
- **DetectEncodingNet48** cible .NET Framework 4.8 (`net48`).

## Utilisation

1. Démarrer l'application souhaitée.
2. Cliquer sur **Sélectionner un fichier** et choisir un fichier `.txt`, `.csv`,
   `.log` ou un autre fichier avec **Tous les fichiers**.
3. L'application détecte l'encodage en arrière-plan, puis affiche son nom, le
   niveau de confiance et la présence éventuelle d'un BOM. Le contenu est lu
   avec l'encodage détecté et affiché dans la zone de texte.
4. En cas d'erreur lors de la détection ou de la lecture, un message d'erreur
   est affiché.

La détection vérifie d'abord les signatures BOM UTF-8, UTF-16 et UTF-32, puis
utilise la bibliothèque `UTF.Unknown` pour estimer l'encodage lorsque le fichier
n'a pas de BOM. Si cette estimation ne fournit pas d'objet `Encoding`, l'application
essaie le nom d'encodage détecté et utilise Windows-1252 en dernier recours.

## Fenêtre et préférences

Les deux applications mémorisent la taille, la position et l'état maximisé de
leur fenêtre dans un fichier placé à côté de l'exécutable :

- **DetectEncoding** utilise `window-bounds.json`.
- **DetectEncodingNet48** utilise `window-bounds.xml`.

Les dimensions et la position normales sont restaurées au prochain démarrage.
Si la position enregistrée sort de la zone virtuelle des écrans, elle est
ajustée pour rendre la fenêtre visible. Les paramètres sont enregistrés lors
d'un changement d'état de la fenêtre et à sa fermeture. Le dossier de
l'application doit donc être accessible en écriture.

## Compilation

Depuis la racine du dépôt :

```powershell
dotnet build DetectEncoding\DetectEncoding.csproj
dotnet build DetectEncodingNet48\DetectEncodingNet48.csproj
```

Le projet .NET Framework 4.8 requiert les outils de build WPF/.NET Framework
4.8 ainsi que les composants nécessaires pour restaurer les dépendances NuGet.
