# VMC Double Flux - Application d'Aide à l'Installation

## 📋 Description

Application professionnelle développée en C# / .NET pour aider à la conception et l'installation de systèmes de VMC (Ventilation Mécanique Contrôlée) double flux.

### Fonctionnalités

✅ **Modélisation d'étages**
- Création de plusieurs étages avec dimensions personnalisables
- Murs extérieurs automatiques (horizontaux/verticaux)
- Ajout de murs intérieurs avec portes et fenêtres
- Système d'échelle configurable (pixels/mètre)

✅ **Placement des bouches VMC**
- Bouches d'extraction (rouge) - air vicié
- Bouches d'insufflation (orange) - air neuf
- Portes (bleu)
- Fenêtres (bleu clair)
- Configuration: nom, diamètre (mm), débit (m³/h)

✅ **Vue graphique interactive**
- Grille avec échelles en mètres
- Affichage du plan avec connexions VMC
- Mode sélection et déplacement des bouches
- Mode traçage de murs
- Visualisation du caisson VMC DF central

✅ **Bilan aéraulique**
- Calcul automatique des débits totaux
- Vérification de l'équilibre extraction/insufflation
- Liste détaillée de toutes les bouches
- Export des données

✅ **Sauvegarde/Chargement**
- Export au format JSON
- Import de projets existants
- Persistance complète des données

## 🖥️ Technologies

- **Framework**: .NET 8.0
- **UI Framework**: Avalonia UI 11.0 (cross-platform)
- **Langage**: C# 12
- **Sérialisation**: Newtonsoft.Json
- **Compatible**: Linux, Windows, macOS

## 📦 Installation sur Ubuntu

### Prérequis

Installer .NET 8.0 SDK:

#### Option 1: Via le script officiel
```bash
wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Ajouter au PATH
export PATH="$PATH:$HOME/.dotnet"
echo 'export PATH="$PATH:$HOME/.dotnet"' >> ~/.bashrc
```

#### Option 2: Via APT (Ubuntu 22.04+)
```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

### Vérification de l'installation
```bash
dotnet --version
# Devrait afficher: 8.0.x
```

## 🚀 Compilation et Exécution

### Méthode 1: Script automatique
```bash
cd VMCDoubleFlux
./run.sh
```

### Méthode 2: Commandes manuelles
```bash
cd VMCDoubleFlux

# Restaurer les packages
dotnet restore

# Compiler
dotnet build --configuration Release

# Exécuter
dotnet run --configuration Release
```

### Méthode 3: Créer un exécutable
```bash
# Publication pour Linux
dotnet publish --configuration Release --runtime linux-x64 --self-contained true

# L'exécutable sera dans: bin/Release/net8.0/linux-x64/publish/
./bin/Release/net8.0/linux-x64/publish/VMCDoubleFlux
```

## 📐 Architecture du Code

```
VMCDoubleFlux/
├── VMCDoubleFlux.csproj      # Fichier de projet .NET
├── Program.cs                 # Point d'entrée
├── App.axaml                  # Application Avalonia
├── App.axaml.cs              
├── MainWindow.axaml           # Fenêtre principale
├── MainWindow.axaml.cs        # Code-behind (UI)
├── Models.cs                  # Modèles de données
│   ├── ProjetVMC
│   ├── Etage
│   ├── BoucheVMC
│   ├── Mur
│   ├── Ouverture
│   ├── BilanAeraulique
│   └── TypeBouche (enum)
└── VueGraphique.cs            # Contrôle de rendu graphique
```

## 🎨 Structure des Données

### ProjetVMC
```csharp
{
    "Nom": "Mon projet VMC",
    "Echelle": 50.0,
    "Etages": [...],
    "EtageActifId": "guid"
}
```

### Etage
```csharp
{
    "Id": "guid",
    "Nom": "Rez-de-chaussee",
    "Largeur": 15.0,
    "Hauteur": 12.0,
    "HauteurSousPlafond": 2.5,
    "Murs": [...],
    "Bouches": [...]
}
```

### BoucheVMC
```csharp
{
    "Id": "guid",
    "Nom": "CUISINE",
    "Type": "Extraction",
    "Diametre": 125.0,
    "Debit": 45.0,
    "Position": { "X": 4.5, "Y": 3.0 }
}
```

## 🎯 Utilisation

### 1. Créer un étage
- Cliquez sur "**+ Ajouter Étage**" dans le panneau de contrôle
- L'étage apparaît dans la liste avec des murs extérieurs automatiques

### 2. Ajouter des bouches VMC
- Sélectionnez le type de bouche (Extraction/Insufflation)
- Cliquez sur "**● Extraction**" ou "**● Insufflation**"
- Cliquez sur le plan pour placer la bouche
- Éditez les propriétés dans le panneau de droite

### 3. Tracer des murs intérieurs
- Cliquez sur "**╱ Tracer mur**"
- Cliquez deux fois sur le plan (début et fin du mur)
- Les murs sont automatiquement horizontaux ou verticaux

### 4. Déplacer des éléments
- Cliquez sur "**✓ Sélection**"
- Cliquez et glissez les bouches pour les repositionner
- Modifiez les propriétés dans le panneau d'édition

### 5. Consulter le bilan
- Cliquez sur "**AFFICHER BILAN**"
- Consultez les débits totaux
- Vérifiez l'équilibre extraction/insufflation

### 6. Sauvegarder le projet
- Cliquez sur "**EXPORTER JSON**"
- Choisissez l'emplacement du fichier
- Le projet est sauvegardé au format JSON

## 🎨 Interface Utilisateur

### Panneau de Contrôle (Gauche)
- **ETAGES**: Liste et gestion des étages
- **OUTILS**: Modes d'édition (Sélection, Tracer mur, Bouches)
- **VUES**: Options d'affichage (Vue de dessus, face, etc.)
- **ÉCHELLE**: Réglage pixels/mètre
- **ÉDITION BOUCHE**: Propriétés de la bouche sélectionnée
- **FICHIER**: Export/Import JSON

### Vue Graphique (Centre)
- Grille avec échelles métriques
- Plan de l'étage avec murs
- Bouches VMC avec connexions au caisson central
- Légende des couleurs

### Bilan Aéraulique (Onglet)
- Résumé des débits
- Indicateur d'équilibre
- Liste détaillée des bouches
- Légende des types

## 🔧 Personnalisation

### Modifier l'échelle par défaut
Dans `Models.cs`:
```csharp
Echelle = 50.0; // Changez cette valeur
```

### Modifier les couleurs des bouches
Dans `VueGraphique.cs`, méthode `DessinerBouches()`:
```csharp
TypeBouche.Extraction => new SolidColorBrush(Color.FromRgb(255, 69, 58)),
```

### Ajouter des types de bouches
Dans `Models.cs`:
```csharp
public enum TypeBouche
{
    Extraction,
    Insufflation,
    Porte,
    Fenetre,
    // Ajoutez vos types ici
}
```

## 📊 Critères de Notation (Projet)

✅ **Qualité de l'interface**
- Choix judicieux des composants Avalonia
- Gestion cohérente des affichages
- Ergonomie et utilisabilité

✅ **Structures de données**
- Modélisation objet claire
- Relations entre entités
- Sérialisation JSON

✅ **Respect des fonctionnalités**
- Modélisation d'étages avec échelle
- Murs horizontaux/verticaux
- Bouches VMC (nom, diamètre, débit, rôle)
- Vue graphique
- Vue textuelle (bilan aéraulique)
- Sauvegarde/chargement

## 🐛 Dépannage

### Erreur: "dotnet: command not found"
```bash
# Vérifiez le PATH
echo $PATH | grep dotnet

# Si absent, ajoutez:
export PATH="$PATH:$HOME/.dotnet"
```

### Erreur: "Unable to load shared library 'libSkiaSharp'"
```bash
# Installez les dépendances Avalonia
sudo apt-get install -y libfontconfig1 libice6 libsm6
```

### Problème d'affichage graphique
```bash
# Vérifiez X11
echo $DISPLAY

# Si vide, exportez:
export DISPLAY=:0
```

## 📝 Format JSON du Projet

Exemple de fichier exporté:
```json
{
  "Nom": "Maison Familiale",
  "Echelle": 50.0,
  "Etages": [
    {
      "Id": "abc-123",
      "Nom": "Rez-de-chaussee",
      "Largeur": 15.0,
      "Hauteur": 12.0,
      "Murs": [...],
      "Bouches": [
        {
          "Nom": "CUISINE",
          "Type": "Extraction",
          "Diametre": 125.0,
          "Debit": 45.0,
          "Position": { "X": 4.5, "Y": 3.0 }
        }
      ]
    }
  ]
}
```

## 🎓 Contexte Académique

**Projet**: M1 Informatique 2025/2026  
**Professeur**: B. MARTIN  
**Cours**: Projet .NET  
**Objectif**: Développer une application d'aide à l'installation de VMC double flux

## 📄 Licence

Projet académique - M1 Informatique 2025/2026

## 👨‍💻 Auteur

Développé pour le cours de Projet .NET - M1 Informatique

---

**Note**: Cette application utilise Avalonia UI, un framework UI cross-platform moderne pour .NET, garantissant une compatibilité complète avec Ubuntu et autres systèmes Linux.
