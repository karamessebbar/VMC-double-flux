# Guide de Démarrage Rapide - VMC Double Flux

## 🚀 Installation Express sur Ubuntu

### Étape 1: Installer .NET 8.0 SDK

```bash
# Télécharger et installer .NET
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 8.0

# Ajouter au PATH (dans le terminal actuel)
export PATH="$PATH:$HOME/.dotnet"

# Ajouter de manière permanente
echo 'export PATH="$PATH:$HOME/.dotnet"' >> ~/.bashrc
source ~/.bashrc
```

**Alternative pour Ubuntu 22.04+:**
```bash
sudo apt-get update
sudo apt-get install -y dotnet-sdk-8.0
```

### Étape 2: Vérifier l'installation

```bash
dotnet --version
# Doit afficher: 8.0.x
```

### Étape 3: Lancer l'application

```bash
cd VMCDoubleFlux
./run.sh
```

## 📋 Utilisation Rapide

### 1️⃣ Créer un nouvel étage
- Panneau gauche → **+ Ajouter Étage**

### 2️⃣ Placer des bouches VMC

**Extraction (rouge):**
- Cliquez sur "● Extraction"
- Cliquez sur le plan pour placer
- Utilisé pour: Cuisine, Salle de bain, WC, Buanderie

**Insufflation (orange):**
- Cliquez sur "● Insufflation"
- Cliquez sur le plan pour placer
- Utilisé pour: Salon, Chambres, Bureau

### 3️⃣ Éditer une bouche
- Mode "✓ Sélection"
- Cliquez sur une bouche
- Modifiez: Nom, Diamètre (mm), Débit (m³/h)
- Cliquez "Sauvegarder"

### 4️⃣ Tracer des murs
- Cliquez "╱ Tracer mur"
- Cliquez deux fois (début et fin)
- Les murs sont automatiquement droits

### 5️⃣ Consulter le bilan
- Cliquez "AFFICHER BILAN"
- Vérifiez l'équilibre extraction/insufflation
- ✓ Vert = Équilibré
- ⚠ Rouge = Déséquilibré

### 6️⃣ Sauvegarder
- Cliquez "EXPORTER JSON"
- Choisissez un nom de fichier
- Le projet est sauvegardé

### 7️⃣ Charger un projet
- Cliquez "IMPORTER JSON"
- Sélectionnez le fichier
- Le projet est restauré

## 🎨 Légende des Couleurs

- 🔴 **Rouge**: Bouches d'extraction (air vicié)
- 🟠 **Orange**: Bouches d'insufflation (air neuf)
- 🔵 **Bleu**: Portes
- 🔵 **Bleu clair**: Fenêtres
- 🟢 **Vert (centre)**: Caisson VMC Double Flux

## 💡 Conseils

### Débits recommandés:
- **Cuisine**: 45-135 m³/h (extraction)
- **Salle de bain**: 45-105 m³/h (extraction)
- **WC**: 30 m³/h (extraction)
- **Chambre**: 30 m³/h (insufflation)
- **Salon**: 30-45 m³/h (insufflation)

### Diamètres standards:
- **80 mm**: WC, petites pièces
- **125 mm**: Standard (chambres, cuisine)
- **150 mm**: Grandes pièces (salon, master bedroom)

### Équilibrage:
- Le débit total d'extraction doit être ≈ égal au débit d'insufflation
- Tolérance: ±5 m³/h

## 🔧 Dépannage Express

### Problème: "dotnet: command not found"
```bash
export PATH="$PATH:$HOME/.dotnet"
```

### Problème: Erreur de compilation
```bash
cd VMCDoubleFlux
dotnet clean
dotnet restore
dotnet build
```

### Problème: Fenêtre ne s'affiche pas
```bash
# Vérifier X11
echo $DISPLAY

# Si vide:
export DISPLAY=:0
```

## 📞 Support

Pour toute question concernant le projet académique, contactez votre professeur B. MARTIN.

## 🎓 Projet

**M1 Informatique 2025/2026**  
**Cours**: Projet .NET  
**Objectif**: Application d'aide à l'installation VMC double flux

---

**Bon développement! 🚀**
