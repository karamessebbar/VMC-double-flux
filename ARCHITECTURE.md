# Architecture Technique - VMC Double Flux

## 📊 Vue d'Ensemble

Application .NET/C# utilisant le framework Avalonia UI pour créer une interface cross-platform (Linux, Windows, macOS).

## 🏗️ Architecture en Couches

```
┌─────────────────────────────────────────────┐
│           Couche Présentation               │
│  (MainWindow, VueGraphique, Contrôles UI)   │
├─────────────────────────────────────────────┤
│           Couche Logique Métier             │
│         (Calculs, Validations)              │
├─────────────────────────────────────────────┤
│           Couche Données                     │
│  (ProjetVMC, Etage, BoucheVMC, Mur)         │
├─────────────────────────────────────────────┤
│           Couche Persistance                │
│         (Sérialisation JSON)                │
└─────────────────────────────────────────────┘
```

## 📦 Structure des Classes

### Modèle de Données (Models.cs)

#### ProjetVMC
**Responsabilité**: Conteneur principal du projet
```csharp
class ProjetVMC
{
    + string Nom
    + double Echelle
    + List<Etage> Etages
    + string EtageActifId
    + Etage EtageActif
    
    + void AjouterEtage(Etage)
    + void SupprimerEtage(string id)
    + BilanAeraulique CalculerBilan()
    + string SauvegarderJson()
    + static ProjetVMC ChargerJson(string)
}
```

#### Etage
**Responsabilité**: Représente un niveau de la maison
```csharp
class Etage
{
    + string Id
    + string Nom
    + double Largeur
    + double Hauteur
    + double HauteurSousPlafond
    + List<Mur> Murs
    + List<BoucheVMC> Bouches
    
    + void AjouterBouche(BoucheVMC)
    + void SupprimerBouche(string id)
    + void AjouterMur(Mur)
    + void SupprimerMur(string id)
    - void CreerMursExterieurs()
}
```

#### BoucheVMC
**Responsabilité**: Représente une bouche de ventilation
```csharp
class BoucheVMC
{
    + string Id
    + string Nom
    + TypeBouche Type
    + double Diametre (mm)
    + double Debit (m³/h)
    + Point2D Position
}

enum TypeBouche
{
    Extraction,
    Insufflation,
    Porte,
    Fenetre
}
```

#### Mur
**Responsabilité**: Représente un mur (intérieur ou extérieur)
```csharp
class Mur
{
    + string Id
    + Point2D Debut
    + Point2D Fin
    + bool EstExterieur
    + List<Ouverture> Ouvertures
    
    + bool EstHorizontal
    + bool EstVertical
    + double Longueur
}
```

#### BilanAeraulique
**Responsabilité**: Calcule et stocke le bilan du système
```csharp
class BilanAeraulique
{
    + List<BoucheVMC> BouchesExtraction
    + List<BoucheVMC> BouchesInsufflation
    + List<BoucheVMC> Portes
    + List<BoucheVMC> Fenetres
    + double DebitTotalExtraction
    + double DebitTotalInsufflation
    + int NombreTotalBouches
    
    + void AjouterBouche(BoucheVMC)
    + bool EstEquilibre()
}
```

### Couche Présentation

#### MainWindow (MainWindow.axaml.cs)
**Responsabilité**: Fenêtre principale et orchestration UI

**Sections UI:**
- Panneau de contrôle (gauche)
- Vue graphique (centre)
- Onglets (Vue Graphique / Bilan Aéraulique)

**Événements gérés:**
- Sélection d'étage
- Sélection de bouche
- Import/Export JSON
- Mise à jour du bilan

#### VueGraphique (VueGraphique.cs)
**Responsabilité**: Rendu graphique et interactions

**Modes d'édition:**
```csharp
enum ModeEdition
{
    Selection,
    AjouterBouche,
    TracerMur,
    AjouterPorte
}
```

**Méthodes de rendu:**
- `DessinerGrille()`: Grille métrique
- `DessinerEchelles()`: Axes gradués
- `DessinerMurs()`: Murs intérieurs/extérieurs
- `DessinerConnexions()`: Lignes pointillées vers VMC DF
- `DessinerBouches()`: Bouches avec labels

**Interactions:**
- Clic: Placement/Sélection
- Glisser-déposer: Déplacement des bouches
- Double-clic: Édition des propriétés

## 🎨 Rendu Graphique

### Système de Coordonnées

```
Plan physique (mètres)         Plan écran (pixels)
┌─────────────────┐           ┌─────────────────┐
│ (0,0)      (L,0)│           │ (50,50)    (50+L*E,50)
│                 │  ====>    │                 │
│                 │           │                 │
│ (0,H)      (L,H)│           │ (50,50+H*E) ... │
└─────────────────┘           └─────────────────┘

Où:
- L = Largeur de l'étage (m)
- H = Hauteur de l'étage (m)
- E = Échelle (pixels/mètre)
- Offset = 50 pixels (pour les marges)
```

### Conversion Coordonnées

```csharp
// Métrique → Écran
screenX = offsetX + positionMetrique.X * echelle
screenY = offsetY + positionMetrique.Y * echelle

// Écran → Métrique
positionMetrique.X = (screenX - offsetX) / echelle
positionMetrique.Y = (screenY - offsetY) / echelle
```

### Code Couleurs

| Élément | Couleur RGB | Usage |
|---------|-------------|-------|
| Extraction | (255, 69, 58) | Air vicié sortant |
| Insufflation | (255, 159, 10) | Air neuf entrant |
| Porte | (52, 120, 246) | Circulation d'air |
| Fenêtre | (100, 210, 255) | Aération naturelle |
| VMC DF | (0, 128, 128) | Caisson central |
| Mur extérieur | (0, 0, 0) | Limite du bâtiment |
| Mur intérieur | (169, 169, 169) | Cloisons |
| Grille | (230, 230, 230) | Repères métriques |

## 💾 Persistance des Données

### Format JSON

Structure hiérarchique:
```json
ProjetVMC
├── Nom
├── Echelle
├── EtageActifId
└── Etages[]
    ├── Id
    ├── Nom
    ├── Dimensions (Largeur, Hauteur, HauteurSousPlafond)
    ├── Murs[]
    │   ├── Id
    │   ├── Points (Debut, Fin)
    │   ├── EstExterieur
    │   └── Ouvertures[]
    └── Bouches[]
        ├── Id
        ├── Nom
        ├── Type
        ├── Diametre
        ├── Debit
        └── Position (X, Y)
```

### Sérialisation

```csharp
// Export
string json = JsonConvert.SerializeObject(projet, Formatting.Indented);
File.WriteAllText(chemin, json);

// Import
string json = File.ReadAllText(chemin);
ProjetVMC projet = JsonConvert.DeserializeObject<ProjetVMC>(json);
```

## 🔄 Flux de Données

### Ajout d'une Bouche

```
1. User clique "● Extraction"
   ↓
2. VueGraphique.SetModeEdition(AjouterBouche, TypeBouche.Extraction)
   ↓
3. User clique sur le plan
   ↓
4. VueGraphique.OnPointerPressed()
   ├─ Conversion coordonnées écran → métrique
   ├─ Création BoucheVMC
   ├─ Etage.AjouterBouche()
   └─ InvalidateVisual() + ProjetModifie event
   ↓
5. MainWindow.OnProjetModifie()
   ├─ MettreAJourInterface()
   └─ MettreAJourBilan()
```

### Calcul du Bilan

```
ProjetVMC.CalculerBilan()
├─ Parcourir tous les Etages
├─ Parcourir toutes les Bouches
├─ Classer par Type
│   ├─ Extraction → Liste + Somme débit
│   ├─ Insufflation → Liste + Somme débit
│   ├─ Porte → Liste
│   └─ Fenetre → Liste
└─ Calculer équilibre (|Extraction - Insufflation| < 5)
```

## 🧩 Patterns de Conception Utilisés

### 1. Model-View (MV)
- **Model**: `ProjetVMC`, `Etage`, `BoucheVMC`
- **View**: `MainWindow`, `VueGraphique`

### 2. Observer Pattern
- Events: `BoucheSelectionneeChanged`, `ProjetModifie`
- Permet la synchronisation automatique de l'UI

### 3. Strategy Pattern
- `ModeEdition`: Différents comportements selon le mode
- `Selection`, `AjouterBouche`, `TracerMur`

### 4. Factory Pattern
- Création automatique des murs extérieurs
- Génération des IDs uniques (GUID)

## 🔐 Validation des Données

### Contraintes Métier

1. **Murs**
   - Obligatoirement horizontaux OU verticaux
   - Pas de diagonales autorisées

2. **Bouches**
   - Diamètre: 80-250 mm
   - Débit: 15-300 m³/h
   - Position dans les limites de l'étage

3. **Équilibrage**
   - Tolérance: ±5 m³/h entre extraction et insufflation

4. **Étages**
   - Au moins 1 étage requis
   - Impossible de supprimer le dernier étage

## 📈 Performances

### Optimisations

1. **Rendu graphique**
   - Invalidation partielle uniquement
   - Pas de re-dessin complet systématique

2. **Calculs**
   - Bilan recalculé uniquement sur modification
   - Pas de calculs en temps réel pendant le drag

3. **Mémoire**
   - Sérialisation JSON légère
   - Pas de cache inutile

### Complexité

| Opération | Complexité |
|-----------|------------|
| Ajouter bouche | O(1) |
| Supprimer bouche | O(n) |
| Calcul bilan | O(e × b) |
| Rendu graphique | O(m + b) |
| Sérialisation | O(e × (m + b)) |

Où:
- e = nombre d'étages
- b = nombre de bouches par étage
- m = nombre de murs par étage

## 🧪 Points d'Extension

### Ajouter un nouveau type de bouche

1. Modifier l'enum `TypeBouche`
2. Ajouter la couleur dans `VueGraphique.DessinerBouches()`
3. Ajouter dans `BilanAeraulique`

### Ajouter une nouvelle vue

1. Créer un nouveau TabItem dans `MainWindow`
2. Implémenter le rendu spécifique
3. Gérer les événements de synchronisation

### Exporter vers d'autres formats

1. Créer une classe `ExporteurPDF`, `ExporteurExcel`
2. Implémenter l'interface commune
3. Ajouter bouton dans le panneau de contrôle

## 📚 Technologies et Bibliothèques

| Technologie | Version | Usage |
|-------------|---------|-------|
| .NET | 8.0 | Runtime |
| C# | 12 | Langage |
| Avalonia UI | 11.0.10 | Framework UI |
| Newtonsoft.Json | 13.0.3 | Sérialisation |

## 🎯 Respect du Cahier des Charges

| Fonctionnalité | Implémentée | Classe/Méthode |
|----------------|-------------|----------------|
| Modélisation étages | ✅ | `Etage` |
| Échelle configurable | ✅ | `ProjetVMC.Echelle` |
| Murs H/V | ✅ | `Mur.EstHorizontal/Vertical` |
| Portes/Fenêtres | ✅ | `Ouverture` |
| Bouches VMC | ✅ | `BoucheVMC` |
| Propriétés bouches | ✅ | Nom, Diamètre, Débit, Type |
| Vue graphique | ✅ | `VueGraphique` |
| Bilan aéraulique | ✅ | `BilanAeraulique` |
| Sauvegarde/Chargement | ✅ | JSON |

---

**Architecture validée pour le projet M1 Informatique 2025/2026**
