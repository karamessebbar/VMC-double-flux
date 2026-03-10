using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;

using System;
using System.IO;
using System.Linq;
using VMCDoubleFlux.Models;
using VMCDoubleFlux.Controls;

namespace VMCDoubleFlux
{
    public partial class MainWindow : Window
    {
        private ProjetVMC _projet;
        private VueGraphique _vueGraphique;

        // Contrôles UI
        private ListBox _listeEtages;
        private ListBox _listeBouches;
        private TextBlock _texteBilanExtraction;
        private TextBlock _texteBilanInsufflation;
        private TextBlock _texteBilanTotal;
        private TextBlock _texteBilanEquilibre;
        private TabControl _tabControl;
        private TextBox _texteBilanDetaille;

        // Contrôles pour édition bouche
        private TextBox _txtNomBouche;
        private ComboBox _cmbTypeBouche;
        private NumericUpDown _numDiametre;
        private NumericUpDown _numDebit;
        private Button _btnSauvegarderBouche;
        private Button _btnSupprimerBouche;
        private Panel _panelEditionBouche;

        public MainWindow()
        {
            InitializeComponent();
            _projet = new ProjetVMC();

            // Créer l'interface utilisateur
            CreerInterface();

            // Initialiser avec le projet par défaut
            _vueGraphique.SetProjet(_projet);

            MettreAJourInterface();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CreerInterface()
        {
            Title = "VMC Double Flux - Edition Professionnelle";
            Width = 1400;
            Height = 900;
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 245));

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(270, GridUnitType.Pixel)); // Panneau gauche
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));    // Vue graphique

            // PANNEAU DE CONTRÔLE (gauche)
            var panneauControle = CreerPanneauControle();
            Grid.SetColumn(panneauControle, 0);
            mainGrid.Children.Add(panneauControle);

            // VUE PRINCIPALE (droite)
            var panneauDroite = new Grid();
            panneauDroite.RowDefinitions.Add(new RowDefinition(50, GridUnitType.Pixel));
            panneauDroite.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
            Grid.SetColumn(panneauDroite, 1);

            // Onglets Vue Graphique / Bilan Aéraulique
            _tabControl = new TabControl
            {
                Margin = new Thickness(10, 10, 10, 5)
            };

            var tabVueGraphique = new TabItem
            {
                Header = "VUE GRAPHIQUE"
            };

            _vueGraphique = new VueGraphique
            {
                Margin = new Thickness(10)
            };
            _vueGraphique.BoucheSelectionneeChanged += OnBoucheSelectionneeChanged;
            _vueGraphique.ProjetModifie += OnProjetModifie;

            tabVueGraphique.Content = _vueGraphique;
            _tabControl.Items.Add(tabVueGraphique);

            var tabBilan = new TabItem
            {
                Header = "BILAN AERAULIQUE"
            };
            tabBilan.Content = CreerPanneauBilan();
            _tabControl.Items.Add(tabBilan);

            Grid.SetRow(_tabControl, 1);
            panneauDroite.Children.Add(_tabControl);

            mainGrid.Children.Add(panneauDroite);

            Content = mainGrid;
        }

        private Control CreerPanneauControle()
        {
            var stackPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                Margin = new Thickness(0)
            };

            // En-tête
            var header = new TextBlock
            {
                Text = "PANNEAU DE CONTROLE",
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Padding = new Thickness(15, 15, 15, 10)
            };
            stackPanel.Children.Add(header);

            // Section ÉTAGES
            stackPanel.Children.Add(CreerSectionEtages());

            // Section OUTILS
            stackPanel.Children.Add(CreerSectionOutils());

            // ❌ Section VUES supprimée car on garde uniquement la vue de dessus

            // Section ÉCHELLE
            stackPanel.Children.Add(CreerSectionEchelle());

            // Section ÉDITION BOUCHE
            _panelEditionBouche = CreerSectionEditionBouche();
            stackPanel.Children.Add(_panelEditionBouche);

            // Boutons de sauvegarde
            stackPanel.Children.Add(CreerSectionSauvegarde());

            return new ScrollViewer
            {
                Content = stackPanel,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };
        }

        private Control CreerSectionEtages()
        {
            var section = CreerSection("ETAGES");

            _listeEtages = new ListBox
            {
                Background = Brushes.White,
                Height = 100,
                Margin = new Thickness(10, 5, 10, 5)
            };
            _listeEtages.SelectionChanged += OnEtageSelectionne;
            section.Children.Add(_listeEtages);

            // Boutons en ligne
            var panelBoutons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 10, 5)
            };

            var btnAjouterEtage = new Button
            {
                Content = "+ Ajouter",
                Background = Brushes.DarkGreen,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 5, 0),
                Padding = new Thickness(10, 8, 10, 8),
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            btnAjouterEtage.Click += BtnAjouterEtage_Click;
            panelBoutons.Children.Add(btnAjouterEtage);

            var btnSupprimerEtage = new Button
            {
                Content = "− Supprimer",
                Background = Brushes.DarkRed,
                Foreground = Brushes.White,
                Padding = new Thickness(10, 8, 10, 8),
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            btnSupprimerEtage.Click += BtnSupprimerEtage_Click;
            panelBoutons.Children.Add(btnSupprimerEtage);

            section.Children.Add(panelBoutons);

            return section;
        }

        private Control CreerSectionOutils()
        {
            var section = CreerSection("OUTILS");

            var btnSelection = CreerBouton("✓ Sélection", Brushes.Gray);
            btnSelection.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.Selection);
            section.Children.Add(btnSelection);

            var btnTracerMur = CreerBouton("╱ Tracer mur", Brushes.Gray);
            btnTracerMur.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.TracerMur);
            section.Children.Add(btnTracerMur);

            var btnExtraction = CreerBouton("● Extraction", new SolidColorBrush(Color.FromRgb(220, 53, 69)));
            btnExtraction.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Extraction);
            section.Children.Add(btnExtraction);

            var btnInsufflation = CreerBouton("● Insufflation", new SolidColorBrush(Color.FromRgb(13, 110, 253)));
            btnInsufflation.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Insufflation);
            section.Children.Add(btnInsufflation);

            var btnPorte = CreerBouton("🚪 Porte", new SolidColorBrush(Color.FromRgb(139, 69, 19)));
            btnPorte.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Porte);
            section.Children.Add(btnPorte);

            var btnFenetre = CreerBouton("🪟 Fenêtre", new SolidColorBrush(Color.FromRgb(70, 130, 180)));
            btnFenetre.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Fenetre);
            section.Children.Add(btnFenetre);

            // ✅ NOUVEAUX BOUTONS : Éléments VMC uniques
            var separateur = new TextBlock
            {
                Text = "── Éléments VMC ──",
                Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                FontSize = 9,
                Margin = new Thickness(10, 10, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            section.Children.Add(separateur);

            var btnEchangeur = CreerBouton("⇄ Échangeur", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
            btnEchangeur.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Echangeur);
            section.Children.Add(btnEchangeur);

            var btnPlenumExt = CreerBouton("◼ Plenum Extraction", new SolidColorBrush(Color.FromRgb(180, 30, 40)));
            btnPlenumExt.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.PlenumExtraction);
            section.Children.Add(btnPlenumExt);

            var btnPlenumIns = CreerBouton("◼ Plenum Insufflation", new SolidColorBrush(Color.FromRgb(30, 80, 200)));
            btnPlenumIns.Click += (s, e) => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.PlenumInsufflation);
            section.Children.Add(btnPlenumIns);

            return section;
        }

        private Control CreerSectionEchelle()
        {
            var section = CreerSection("ECHELLE");

            var stackH = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 5, 10, 5)
            };

            var label = new TextBlock
            {
                Text = "Pixels/mètre:",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var numEchelle = new NumericUpDown
            {
                Width = 80,
                Minimum = 10,
                Maximum = 200,
                Value = 50,
                Increment = 5
            };
            numEchelle.ValueChanged += (s, e) =>
            {
                if (_projet != null && numEchelle.Value.HasValue)
                {
                    _projet.Echelle = (double)numEchelle.Value.Value;
                    _vueGraphique.InvalidateVisual();
                }
            };

            stackH.Children.Add(label);
            stackH.Children.Add(numEchelle);
            section.Children.Add(stackH);

            return section;
        }

        private Panel CreerSectionEditionBouche()
        {
            var section = CreerSection("ÉDITION BOUCHE");
            section.IsVisible = false;

            var grid = new Grid
            {
                Margin = new Thickness(10, 5, 10, 5)
            };
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

            // Nom
            var lblNom = new TextBlock { Text = "Nom:", Foreground = Brushes.White, Margin = new Thickness(0, 5, 10, 5) };
            Grid.SetRow(lblNom, 0);
            Grid.SetColumn(lblNom, 0);
            _txtNomBouche = new TextBox { Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(_txtNomBouche, 0);
            Grid.SetColumn(_txtNomBouche, 1);

            // Type
            var lblType = new TextBlock { Text = "Type:", Foreground = Brushes.White, Margin = new Thickness(0, 5, 10, 5) };
            Grid.SetRow(lblType, 1);
            Grid.SetColumn(lblType, 0);
            _cmbTypeBouche = new ComboBox { Margin = new Thickness(0, 5, 0, 5) };
            foreach (TypeBouche type in Enum.GetValues(typeof(TypeBouche)))
            {
                _cmbTypeBouche.Items.Add(type);
            }
            Grid.SetRow(_cmbTypeBouche, 1);
            Grid.SetColumn(_cmbTypeBouche, 1);

            // Diamètre
            var lblDiam = new TextBlock { Text = "Ø (mm):", Foreground = Brushes.White, Margin = new Thickness(0, 5, 10, 5) };
            Grid.SetRow(lblDiam, 2);
            Grid.SetColumn(lblDiam, 0);
            _numDiametre = new NumericUpDown { Minimum = 80, Maximum = 250, Increment = 5, Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(_numDiametre, 2);
            Grid.SetColumn(_numDiametre, 1);

            // Débit
            var lblDebit = new TextBlock { Text = "Débit:", Foreground = Brushes.White, Margin = new Thickness(0, 5, 10, 5) };
            Grid.SetRow(lblDebit, 3);
            Grid.SetColumn(lblDebit, 0);
            _numDebit = new NumericUpDown { Minimum = 15, Maximum = 300, Increment = 5, Margin = new Thickness(0, 5, 0, 5) };
            Grid.SetRow(_numDebit, 3);
            Grid.SetColumn(_numDebit, 1);

            grid.Children.Add(lblNom);
            grid.Children.Add(_txtNomBouche);
            grid.Children.Add(lblType);
            grid.Children.Add(_cmbTypeBouche);
            grid.Children.Add(lblDiam);
            grid.Children.Add(_numDiametre);
            grid.Children.Add(lblDebit);
            grid.Children.Add(_numDebit);

            section.Children.Add(grid);

            // Boutons
            _btnSauvegarderBouche = CreerBouton("Sauvegarder", Brushes.DarkGreen);
            _btnSauvegarderBouche.Click += BtnSauvegarderBouche_Click;
            section.Children.Add(_btnSauvegarderBouche);

            _btnSupprimerBouche = CreerBouton("Supprimer", Brushes.DarkRed);
            _btnSupprimerBouche.Click += BtnSupprimerBouche_Click;
            section.Children.Add(_btnSupprimerBouche);

            return section;
        }

        private Control CreerSectionSauvegarde()
        {
            var section = CreerSection("FICHIER");

            var btnAfficherBilan = CreerBouton("AFFICHER BILAN", new SolidColorBrush(Color.FromRgb(0, 123, 255)));
            btnAfficherBilan.Click += (s, e) => { _tabControl.SelectedIndex = 1; MettreAJourBilan(); };
            section.Children.Add(btnAfficherBilan);

            var btnExporter = CreerBouton("EXPORTER JSON", new SolidColorBrush(Color.FromRgb(40, 167, 69)));
            btnExporter.Click += BtnExporterJson_Click;
            section.Children.Add(btnExporter);

            var btnImporter = CreerBouton("IMPORTER JSON", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
            btnImporter.Click += BtnImporterJson_Click;
            section.Children.Add(btnImporter);

            return section;
        }

        private StackPanel CreerSection(string titre)
        {
            var section = new StackPanel
            {
                Margin = new Thickness(0, 10, 0, 0)
            };

            var header = new TextBlock
            {
                Text = titre,
                Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Padding = new Thickness(15, 5, 15, 5)
            };
            section.Children.Add(header);

            return section;
        }

        private Button CreerBouton(string texte, IBrush background)
        {
            return new Button
            {
                Content = texte,
                Background = background,
                Foreground = Brushes.White,
                Margin = new Thickness(10, 3, 10, 3),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(10, 8, 10, 8),
                FontWeight = FontWeight.SemiBold
            };
        }

        private Control CreerPanneauBilan()
        {
            var mainPanel = new DockPanel();

            // BARRE D'ÉCHELLE EN BAS
            var scalePanel = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(20, 10, 20, 10),
                Height = 60
            };
            DockPanel.SetDock(scalePanel, Dock.Bottom);

            var scaleStack = new StackPanel { Orientation = Orientation.Horizontal };
            
            var scaleLabel = new TextBlock
            {
                Text = "Échelle cadre:",
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };
            scaleStack.Children.Add(scaleLabel);

            // Slider pour l'échelle
            var scaleSlider = new Slider
            {
                Minimum = 400,
                Maximum = 1200,
                Value = 800,
                Width = 200,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };
            scaleStack.Children.Add(scaleSlider);

            var scaleValue = new TextBlock
            {
                Text = "800x600 px",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 20, 0),
                FontWeight = FontWeight.SemiBold
            };
            scaleStack.Children.Add(scaleValue);

            // Conversion en mètres réels
            var realSizeLabel = new TextBlock
            {
                Text = "→ Réel:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            scaleStack.Children.Add(realSizeLabel);

            var realSizeValue = new TextBlock
            {
                Text = "16.0 × 12.0 m",
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                FontWeight = FontWeight.Bold
            };
            scaleStack.Children.Add(realSizeValue);

            scaleSlider.ValueChanged += (s, e) =>
            {
                double width = scaleSlider.Value * 800;
                double height = width * 0.75; // Ratio 4:3
                scaleValue.Text = $"{width:0}×{height:0} px";
                
                // Conversion en mètres (échelle = 50 px/m par défaut)
                double echelle = _projet?.Echelle ?? 50;
                double widthM = width / echelle;
                double heightM = height / echelle;
                realSizeValue.Text = $"{widthM:0.0} × {heightM:0.0} m";
            };

            scalePanel.Child = scaleStack;
            mainPanel.Children.Add(scalePanel);

            // SCROLL VIEWER AVEC CONTENU DU BILAN
            var scrollViewer = new ScrollViewer
            {
                Background = Brushes.White
            };
            DockPanel.SetDock(scrollViewer, Dock.Top);

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            // Titre
            var titre = new TextBlock
            {
                Text = "BILAN AÉRAULIQUE",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)),
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(titre);

            // Résumé avec cadre fixe
            var resumeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(236, 240, 241)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 0, 0, 20),
                MaxWidth = 800 // CADRE FIXE
            };

            var resumePanel = new StackPanel();

            _texteBilanExtraction = new TextBlock { FontSize = 14, Margin = new Thickness(0, 3, 0, 3) };
            _texteBilanInsufflation = new TextBlock { FontSize = 14, Margin = new Thickness(0, 3, 0, 3) };
            _texteBilanTotal = new TextBlock { FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 10, 0, 3) };
            _texteBilanEquilibre = new TextBlock { FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0, 3, 0, 3) };

            resumePanel.Children.Add(_texteBilanExtraction);
            resumePanel.Children.Add(_texteBilanInsufflation);
            resumePanel.Children.Add(_texteBilanTotal);
            resumePanel.Children.Add(_texteBilanEquilibre);

            resumeBorder.Child = resumePanel;
            stackPanel.Children.Add(resumeBorder);

            // Détails
            var detailsTitre = new TextBlock
            {
                Text = "Détail des bouches",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(detailsTitre);

            var detailsBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                MaxWidth = 800 // CADRE FIXE
            };

            _texteBilanDetaille = new TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                MinHeight = 300,
                FontFamily = new FontFamily("Courier New"),
                FontSize = 12,
                Background = Brushes.White,
                BorderThickness = new Thickness(0)
            };

            detailsBorder.Child = _texteBilanDetaille;
            stackPanel.Children.Add(detailsBorder);

            // Légende avec cadre fixe
            var legendeTitre = new TextBlock
            {
                Text = "Légende",
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                Margin = new Thickness(0, 20, 0, 10)
            };
            stackPanel.Children.Add(legendeTitre);

            var legendeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15),
                MaxWidth = 800 // CADRE FIXE
            };

            var legendePanel = new StackPanel();
            legendePanel.Children.Add(CreerLigneLegende("🔴 ROUGE: Extraction (air vicié)", 
                new SolidColorBrush(Color.FromRgb(220, 53, 69))));
            legendePanel.Children.Add(CreerLigneLegende("🔵 BLEU: Insufflation (air neuf réchauffé)", 
                new SolidColorBrush(Color.FromRgb(13, 110, 253))));
            legendePanel.Children.Add(CreerLigneLegende("🟤 MARRON: Porte", 
                new SolidColorBrush(Color.FromRgb(139, 69, 19))));
            legendePanel.Children.Add(CreerLigneLegende("🔵 BLEU ACIER: Fenêtre", 
                new SolidColorBrush(Color.FromRgb(70, 130, 180))));

            legendeBorder.Child = legendePanel;
            stackPanel.Children.Add(legendeBorder);

            scrollViewer.Content = stackPanel;
            mainPanel.Children.Add(scrollViewer);

            return mainPanel;
        }

        private Control CreerLigneLegende(string texte, IBrush couleur)
        {
            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 3, 0, 3)
            };

            var cercle = new Border
            {
                Width = 20,
                Height = 20,
                CornerRadius = new CornerRadius(10),
                Background = couleur,
                Margin = new Thickness(0, 0, 10, 0)
            };

            var label = new TextBlock
            {
                Text = texte,
                VerticalAlignment = VerticalAlignment.Center
            };

            stack.Children.Add(cercle);
            stack.Children.Add(label);

            return stack;
        }

        // Événements
        private void OnEtageSelectionne(object? sender, SelectionChangedEventArgs e)
        {
            if (_listeEtages.SelectedIndex >= 0 && _listeEtages.SelectedIndex < _projet.Etages.Count)
            {
                var etage = _projet.Etages[_listeEtages.SelectedIndex];
                _projet.EtageActif = etage;
                _vueGraphique.SetEtageActif(etage);
            }
        }

        private void OnBoucheSelectionneeChanged(object? sender, BoucheVMC? bouche)
        {
            if (bouche != null)
            {
                _panelEditionBouche.IsVisible = true;
                _txtNomBouche.Text = bouche.Nom;
                _cmbTypeBouche.SelectedItem = bouche.Type;
                _numDiametre.Value = (decimal)bouche.Diametre;
                _numDebit.Value = (decimal)bouche.Debit;
            }
            else
            {
                _panelEditionBouche.IsVisible = false;
            }
        }

        private void OnProjetModifie(object? sender, EventArgs e)
        {
            MettreAJourInterface();
        }

        private async void BtnAjouterEtage_Click(object? sender, RoutedEventArgs e)
        {
            // Créer une fenêtre de dialogue pour les dimensions
            var dialogWindow = new Window
            {
                Title = "Nouvel Étage",
                Width = 350,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var stackPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            // Nom
            var lblNom = new TextBlock { Text = "Nom de l'étage:", Margin = new Thickness(0, 0, 0, 5) };
            stackPanel.Children.Add(lblNom);
            var txtNom = new TextBox 
            { 
                Text = $"Étage {_projet.Etages.Count + 1}",
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackPanel.Children.Add(txtNom);

            // Largeur
            var lblLargeur = new TextBlock { Text = "Largeur (mètres):", Margin = new Thickness(0, 0, 0, 5) };
            stackPanel.Children.Add(lblLargeur);
            var numLargeur = new NumericUpDown
            {
                Value = 15M,
                Minimum = 3M,
                Maximum = 50M,
                Increment = 0.5M,
                Margin = new Thickness(0, 0, 0, 15)
            };
            stackPanel.Children.Add(numLargeur);

            // Hauteur
            var lblHauteur = new TextBlock { Text = "Hauteur (mètres):", Margin = new Thickness(0, 0, 0, 5) };
            stackPanel.Children.Add(lblHauteur);
            var numHauteur = new NumericUpDown
            {
                Value = 12M,
                Minimum = 3M,
                Maximum = 50M,
                Increment = 0.5M,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(numHauteur);

            // Boutons
            var panelBoutons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnOk = new Button
            {
                Content = "Créer",
                Background = Brushes.DarkGreen,
                Foreground = Brushes.White,
                Padding = new Thickness(20, 10, 20, 10),
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnOk.Click += (s, args) => dialogWindow.Close(true);

            var btnAnnuler = new Button
            {
                Content = "Annuler",
                Padding = new Thickness(20, 10, 20, 10)
            };
            btnAnnuler.Click += (s, args) => dialogWindow.Close(false);

            panelBoutons.Children.Add(btnOk);
            panelBoutons.Children.Add(btnAnnuler);
            stackPanel.Children.Add(panelBoutons);

            dialogWindow.Content = stackPanel;

            var result = await dialogWindow.ShowDialog<bool>(this);

            if (result && numLargeur.Value.HasValue && numHauteur.Value.HasValue)
            {
                var etage = new Etage(
                    txtNom.Text ?? $"Étage {_projet.Etages.Count + 1}",
                    (double)numLargeur.Value.Value,
                    (double)numHauteur.Value.Value
                );
                _projet.AjouterEtage(etage);
                MettreAJourInterface();
            }
        }

        private async void BtnSupprimerEtage_Click(object? sender, RoutedEventArgs e)
        {
            if (_listeEtages.SelectedIndex < 0)
            {
                var msgBox = new Window
                {
                    Title = "Attention",
                    Width = 300,
                    Height = 150,
                    Content = new TextBlock
                    {
                        Text = "Veuillez sélectionner un étage à supprimer.",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    }
                };
                await msgBox.ShowDialog(this);
                return;
            }

            if (_projet.Etages.Count <= 1)
            {
                var msgBox = new Window
                {
                    Title = "Impossible",
                    Width = 300,
                    Height = 150,
                    Content = new TextBlock
                    {
                        Text = "Impossible de supprimer le dernier étage.",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        TextWrapping = TextWrapping.Wrap
                    }
                };
                await msgBox.ShowDialog(this);
                return;
            }

            var etageASupprimer = _projet.Etages[_listeEtages.SelectedIndex];

            // Demander confirmation
            var confirmWindow = new Window
            {
                Title = "Confirmation",
                Width = 350,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var confirmStack = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20
            };

            var txtConfirm = new TextBlock
            {
                Text = $"Voulez-vous vraiment supprimer l'étage '{etageASupprimer.Nom}' ?\n\nToutes les bouches et murs seront perdus.",
                TextWrapping = TextWrapping.Wrap
            };
            confirmStack.Children.Add(txtConfirm);

            var panelBoutons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var btnOui = new Button
            {
                Content = "Oui, supprimer",
                Background = Brushes.DarkRed,
                Foreground = Brushes.White,
                Padding = new Thickness(15, 8, 15, 8),
                Margin = new Thickness(0, 0, 10, 0)
            };
            btnOui.Click += (s, args) => confirmWindow.Close(true);

            var btnNon = new Button
            {
                Content = "Non, annuler",
                Padding = new Thickness(15, 8, 15, 8)
            };
            btnNon.Click += (s, args) => confirmWindow.Close(false);

            panelBoutons.Children.Add(btnOui);
            panelBoutons.Children.Add(btnNon);
            confirmStack.Children.Add(panelBoutons);

            confirmWindow.Content = confirmStack;

            var confirm = await confirmWindow.ShowDialog<bool>(this);

            if (confirm)
            {
                _projet.SupprimerEtage(etageASupprimer.Id);
                MettreAJourInterface();
            }
        }

        private void BtnSauvegarderBouche_Click(object? sender, RoutedEventArgs e)
        {
            var bouche = _vueGraphique.GetBoucheSelectionnee();
            if (bouche != null)
            {
                bouche.Nom = _txtNomBouche.Text ?? "Sans nom";
                bouche.Type = (TypeBouche)(_cmbTypeBouche.SelectedItem ?? TypeBouche.Extraction);
                bouche.Diametre = _numDiametre.Value.HasValue ? (double)_numDiametre.Value.Value : 125;
                bouche.Debit = _numDebit.Value.HasValue ? (double)_numDebit.Value.Value : 45;
                _vueGraphique.InvalidateVisual();
                MettreAJourInterface();
            }
        }

        private void BtnSupprimerBouche_Click(object? sender, RoutedEventArgs e)
        {
            _vueGraphique.SupprimerBoucheSelectionnee();
            _panelEditionBouche.IsVisible = false;
            MettreAJourInterface();
        }

        private async void BtnExporterJson_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Exporter le projet",
                Filters = new System.Collections.Generic.List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Fichiers JSON", Extensions = { "json" } }
                },
                DefaultExtension = "json"
            };

            var result = await dialog.ShowAsync(this);
            if (result != null)
            {
                try
                {
                    string json = _projet.SauvegarderJson();
                    File.WriteAllText(result, json);

                    var msgBox = new Window
                    {
                        Title = "Succès",
                        Width = 300,
                        Height = 150,
                        Content = new TextBlock
                        {
                            Text = "Projet exporté avec succès!",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    };
                    await msgBox.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    var msgBox = new Window
                    {
                        Title = "Erreur",
                        Width = 400,
                        Height = 150,
                        Content = new TextBlock
                        {
                            Text = $"Erreur lors de l'export: {ex.Message}",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap
                        }
                    };
                    await msgBox.ShowDialog(this);
                }
            }
        }

        private async void BtnImporterJson_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Importer un projet",
                Filters = new System.Collections.Generic.List<FileDialogFilter>
                {
                    new FileDialogFilter { Name = "Fichiers JSON", Extensions = { "json" } }
                },
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                try
                {
                    string json = File.ReadAllText(result[0]);
                    var projet = ProjetVMC.ChargerJson(json);

                    if (projet != null)
                    {
                        _projet = projet;
                        
                        // ✅ CORRECTION : Définir l'étage actif s'il n'est pas déjà défini
                        if (_projet.EtageActif == null && _projet.Etages.Count > 0)
                        {
                            _projet.EtageActif = _projet.Etages[0];
                        }
                        
                        _vueGraphique.SetProjet(_projet);
                        MettreAJourInterface();

                        var msgBox = new Window
                        {
                            Title = "Succès",
                            Width = 300,
                            Height = 150,
                            Content = new TextBlock
                            {
                                Text = "Projet importé avec succès!",
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            }
                        };
                        await msgBox.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    var msgBox = new Window
                    {
                        Title = "Erreur",
                        Width = 400,
                        Height = 150,
                        Content = new TextBlock
                        {
                            Text = $"Erreur lors de l'import: {ex.Message}",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center,
                            TextWrapping = TextWrapping.Wrap
                        }
                    };
                    await msgBox.ShowDialog(this);
                }
            }
        }

        private void MettreAJourInterface()
        {
            // Mettre à jour la liste des étages
            _listeEtages.Items.Clear();
            foreach (var etage in _projet.Etages)
            {
                _listeEtages.Items.Add(etage.Nom); // ✅ Afficher le nom au lieu de l'objet
            }
            if (_projet.EtageActif != null)
            {
                _listeEtages.SelectedIndex = _projet.Etages.IndexOf(_projet.EtageActif);
            }

            MettreAJourBilan();
        }

        private void MettreAJourBilan()
        {
            var bilan = _projet.CalculerBilan();

            _texteBilanExtraction.Text = $"Débit total extraction: {bilan.DebitTotalExtraction:F1} m³/h ({bilan.BouchesExtraction.Count} bouches)";
            _texteBilanInsufflation.Text = $"Débit total insufflation: {bilan.DebitTotalInsufflation:F1} m³/h ({bilan.BouchesInsufflation.Count} bouches)";
            _texteBilanTotal.Text = $"Nombre total de bouches: {bilan.NombreTotalBouches}";

            if (bilan.EstEquilibre())
            {
                _texteBilanEquilibre.Text = "✓ Le système est équilibré";
                _texteBilanEquilibre.Foreground = Brushes.Green;
            }
            else
            {
                double diff = bilan.DebitTotalExtraction - bilan.DebitTotalInsufflation;
                _texteBilanEquilibre.Text = $"⚠ Déséquilibre: {Math.Abs(diff):F1} m³/h";
                _texteBilanEquilibre.Foreground = Brushes.Red;
            }

            var details = "";

            // ✅ ÉLÉMENTS VMC UNIQUES
            details += "ÉLÉMENTS VMC:\n";
            details += new string('=', 50) + "\n";
            details += $"  Échangeur: {(bilan.Echangeur != null ? "✓ Installé" : "✗ Manquant")}\n";
            details += $"  Plenum Extraction: {(bilan.PlenumExtraction != null ? "✓ Installé" : "✗ Manquant")}\n";
            details += $"  Plenum Insufflation: {(bilan.PlenumInsufflation != null ? "✓ Installé" : "✗ Manquant")}\n";
            details += $"  Système complet: {(bilan.SystemeComplet() ? "✓ OUI" : "✗ NON")}\n\n";

            // ✅ BILANS PAR ÉTAGE
            details += "BILANS PAR ÉTAGE:\n";
            details += new string('=', 50) + "\n";
            foreach (var bilanEtage in bilan.BilansEtages)
            {
                details += $"\n═══ {bilanEtage.NomEtage.ToUpper()} ═══\n";
                details += $"  Extraction: {bilanEtage.DebitTotalExtraction:F1} m³/h ({bilanEtage.BouchesExtraction.Count} bouches)\n";
                details += $"  Insufflation: {bilanEtage.DebitTotalInsufflation:F1} m³/h ({bilanEtage.BouchesInsufflation.Count} bouches)\n";
                details += $"  Équilibre: {(bilanEtage.EstEquilibre() ? "✓" : "✗")} ({Math.Abs(bilanEtage.DebitTotalExtraction - bilanEtage.DebitTotalInsufflation):F1} m³/h)\n";
                
                if (bilanEtage.BouchesExtraction.Count > 0)
                {
                    details += "  Bouches d'extraction:\n";
                    foreach (var b in bilanEtage.BouchesExtraction)
                        details += $"    • {b.Nom,-15} Ø{b.Diametre}mm  {b.Debit:F1} m³/h\n";
                }
                
                if (bilanEtage.BouchesInsufflation.Count > 0)
                {
                    details += "  Bouches d'insufflation:\n";
                    foreach (var b in bilanEtage.BouchesInsufflation)
                        details += $"    • {b.Nom,-15} Ø{b.Diametre}mm  {b.Debit:F1} m³/h\n";
                }
            }

            details += "\n";
            details += "BILAN GLOBAL:\n";
            details += new string('=', 50) + "\n";
            details += "BOUCHES D'EXTRACTION:\n";
            foreach (var bouche in bilan.BouchesExtraction)
            {
                details += $"  • {bouche.Nom,-20} Ø{bouche.Diametre}mm  {bouche.Debit,6:F1} m³/h\n";
            }
            details += $"\nTotal extraction: {bilan.DebitTotalExtraction:F1} m³/h\n\n";

            details += "BOUCHES D'INSUFFLATION:\n";
            foreach (var bouche in bilan.BouchesInsufflation)
            {
                details += $"  • {bouche.Nom,-20} Ø{bouche.Diametre}mm  {bouche.Debit,6:F1} m³/h\n";
            }
            details += $"\nTotal insufflation: {bilan.DebitTotalInsufflation:F1} m³/h\n\n";

            if (bilan.Portes.Count > 0 || bilan.Fenetres.Count > 0)
            {
                details += "AUTRES ÉLÉMENTS:\n";
                details += new string('=', 50) + "\n";
                foreach (var porte in bilan.Portes)
                {
                    details += $"  • {porte.Nom} (Porte)\n";
                }
                foreach (var fenetre in bilan.Fenetres)
                {
                    details += $"  • {fenetre.Nom} (Fenêtre)\n";
                }
            }

            _texteBilanDetaille.Text = details;
        }
    }
}