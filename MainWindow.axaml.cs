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
        // ── Modèles ─────────────────────────────────────────────
        private ProjetVMC    _projet;
        private VueGraphique _vueGraphique;
        private readonly MoteurVMC _moteur = new MoteurVMC();

        // Dernier résultat moteur (pour le bilan détaillé)
        private ResultatMoteur? _dernierResultat;

        // ── Contrôles UI ─────────────────────────────────────────
        private ListBox _listeEtages;
        private ListBox _listeBouches;
        private TextBlock _texteBilanExtraction;
        private TextBlock _texteBilanInsufflation;
        private TextBlock _texteBilanTotal;
        private TextBlock _texteBilanEquilibre;
        private TabControl _tabControl;
        private TextBox    _texteBilanDetaille;

        private TextBox       _txtNomBouche;
        private ComboBox      _cmbTypeBouche;
        private NumericUpDown _numDiametre;
        private NumericUpDown _numDebit;
        private CheckBox      _chkDebitFixe;
        private Button        _btnSauvegarderBouche;
        private Button        _btnSupprimerBouche;
        private Panel         _panelEditionBouche;

        // ── Contrôles Moteur VMC ─────────────────────────────────
        private NumericUpDown _numCapacite;
        private TextBlock     _txtDebitExtractionDemande;
        private TextBlock     _txtDebitInsufflationDemande;
        private ProgressBar   _progressExtraction;
        private ProgressBar   _progressInsufflation;
        private TextBlock     _txtTauxExtraction;
        private TextBlock     _txtTauxInsufflation;
        private TextBlock     _txtStatutExtraction;
        private TextBlock     _txtStatutInsufflation;
        private Border        _bordStatutExtraction;
        private Border        _bordStatutInsufflation;
        private TextBlock     _txtStatutGlobal;
        private Border        _bordStatutGlobal;
        private TextBlock     _txtDetailDebits;

        // ────────────────────────────────────────────────────────
        public MainWindow()
        {
            InitializeComponent();
            _projet = new ProjetVMC();
            CreerInterface();
            _vueGraphique.SetProjet(_projet);
            // Brancher l'événement capacité seulement maintenant,
            // après que _projet et tous les contrôles sont prêts.
            _numCapacite.ValueChanged += (_, _) => RecalculerMoteur();
            // RecalculerMoteur d'abord pour que _dernierResultat soit dispo
            // quand MettreAJourBilan s'exécute à l'intérieur de MettreAJourInterface.
            RecalculerMoteur();
            MettreAJourInterface();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        // ════════════════════════════════════════════════════════
        //  CONSTRUCTION DE L'INTERFACE
        // ════════════════════════════════════════════════════════

        private void CreerInterface()
        {
            Title  = "VMC Double Flux - Edition Professionnelle";
            Width  = 1600;
            Height = 900;
            Background = new SolidColorBrush(Color.FromRgb(240, 240, 245));

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(270, GridUnitType.Pixel));
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(1,   GridUnitType.Star));
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition(280, GridUnitType.Pixel));

            var panneauControle = CreerPanneauControle();
            Grid.SetColumn(panneauControle, 0);
            mainGrid.Children.Add(panneauControle);

            var panneauCentre = new Grid();
            panneauCentre.RowDefinitions.Add(new RowDefinition(50, GridUnitType.Pixel));
            panneauCentre.RowDefinitions.Add(new RowDefinition(1,  GridUnitType.Star));
            Grid.SetColumn(panneauCentre, 1);

            _tabControl = new TabControl { Margin = new Thickness(10, 10, 10, 5) };

            var tabVue = new TabItem { Header = "VUE GRAPHIQUE" };
            _vueGraphique = new VueGraphique { Margin = new Thickness(10) };
            _vueGraphique.BoucheSelectionneeChanged += OnBoucheSelectionneeChanged;
            _vueGraphique.ProjetModifie             += OnProjetModifie;
            tabVue.Content = _vueGraphique;
            _tabControl.Items.Add(tabVue);

            var tabBilan = new TabItem { Header = "BILAN AERAULIQUE" };
            tabBilan.Content = CreerPanneauBilan();
            _tabControl.Items.Add(tabBilan);

            Grid.SetRow(_tabControl, 1);
            panneauCentre.Children.Add(_tabControl);
            mainGrid.Children.Add(panneauCentre);

            var panneauMoteur = CreerPanneauMoteur();
            Grid.SetColumn(panneauMoteur, 2);
            mainGrid.Children.Add(panneauMoteur);

            Content = mainGrid;
        }

        // ════════════════════════════════════════════════════════
        //  PANNEAU GAUCHE
        // ════════════════════════════════════════════════════════

        private Control CreerPanneauControle()
        {
            var stackPanel = new StackPanel
            {
                Background = new SolidColorBrush(Color.FromRgb(44, 62, 80))
            };
            stackPanel.Children.Add(new TextBlock
            {
                Text = "PANNEAU DE CONTROLE", Foreground = Brushes.White,
                FontSize = 14, FontWeight = FontWeight.Bold,
                Padding = new Thickness(15, 15, 15, 10)
            });
            stackPanel.Children.Add(CreerSectionEtages());
            stackPanel.Children.Add(CreerSectionOutils());
            stackPanel.Children.Add(CreerSectionEchelle());
            _panelEditionBouche = CreerSectionEditionBouche();
            stackPanel.Children.Add(_panelEditionBouche);
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
            _listeEtages = new ListBox { Background = Brushes.White, Height = 100, Margin = new Thickness(10, 5, 10, 5) };
            _listeEtages.SelectionChanged += OnEtageSelectionne;
            section.Children.Add(_listeEtages);

            var panelBoutons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 0, 10, 5) };
            var btnAjouter = new Button { Content = "+ Ajouter", Background = Brushes.DarkGreen, Foreground = Brushes.White, Margin = new Thickness(0,0,5,0), Padding = new Thickness(10,8,10,8), FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Stretch };
            btnAjouter.Click += BtnAjouterEtage_Click;
            var btnSupprimer = new Button { Content = "− Supprimer", Background = Brushes.DarkRed, Foreground = Brushes.White, Margin = new Thickness(0,0,5,0), Padding = new Thickness(10,8,10,8), FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Stretch };
            btnSupprimer.Click += BtnSupprimerEtage_Click;
            var btnRenommer = new Button { Content = "✏ Renommer", Background = new SolidColorBrush(Color.FromRgb(30, 100, 180)), Foreground = Brushes.White, Padding = new Thickness(10,8,10,8), FontWeight = FontWeight.SemiBold, HorizontalAlignment = HorizontalAlignment.Stretch };
            btnRenommer.Click += BtnRenommerEtage_Click;
            panelBoutons.Children.Add(btnAjouter);
            panelBoutons.Children.Add(btnSupprimer);
            section.Children.Add(panelBoutons);
            section.Children.Add(btnRenommer);
            return section;
        }

        private Control CreerSectionOutils()
        {
            var section = CreerSection("OUTILS");
            var outils = new (string Emoji, string Label, Action Action)[]
            {
                ("🖱️",  "Sélection",     () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.Selection)),
                ("🧱",  "Mur",           () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.TracerMur)),
                ("💨",  "Extraction",    () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Extraction)),
                ("🌬️", "Insufflation",  () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Insufflation)),
                ("🚪",  "Porte",         () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Porte)),
                ("🪟",  "Fenêtre",       () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Fenetre)),
                ("♻️",  "Échangeur",     () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.Echangeur)),
                ("🔴",  "Plenum Extr.",  () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.PlenumExtraction)),
                ("🔵",  "Plenum Insuf.", () => _vueGraphique.SetModeEdition(VueGraphique.ModeEdition.AjouterBouche, TypeBouche.PlenumInsufflation)),
            };
            var grille = new WrapPanel { Margin = new Thickness(8, 4, 8, 8) };
            foreach (var (emoji, label, action) in outils)
            {
                var btn = CreerBoutonOutil(emoji, label);
                btn.Click += (s, e) => action();
                grille.Children.Add(btn);
            }
            section.Children.Add(grille);
            return section;
        }

        private Button CreerBoutonOutil(string emoji, string label)
        {
            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            stack.Children.Add(new TextBlock { Text = emoji, FontSize = 26, HorizontalAlignment = HorizontalAlignment.Center });
            stack.Children.Add(new TextBlock { Text = label, FontSize = 10, FontWeight = FontWeight.SemiBold, Foreground = Brushes.White, HorizontalAlignment = HorizontalAlignment.Center });
            return new Button
            {
                Content = stack,
                Background = new SolidColorBrush(Color.FromRgb(90, 90, 90)),
                Width = 118, Margin = new Thickness(2), Padding = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Left,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
        }

        private Control CreerSectionEchelle()
        {
            var section = CreerSection("ECHELLE");
            var stackH = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 5, 10, 5) };
            stackH.Children.Add(new TextBlock { Text = "Pixels/mètre:", Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,10,0) });
            var numEchelle = new NumericUpDown { Width = 80, Minimum = 10, Maximum = 200, Value = 50, Increment = 5 };
            numEchelle.ValueChanged += (s, e) =>
            {
                if (_projet != null && numEchelle.Value.HasValue)
                {
                    _projet.Echelle = (double)numEchelle.Value.Value;
                    _vueGraphique.InvalidateVisual();
                }
            };
            stackH.Children.Add(numEchelle);
            section.Children.Add(stackH);
            return section;
        }

        private Panel CreerSectionEditionBouche()
        {
            var section = CreerSection("ÉDITION BOUCHE");
            section.IsVisible = false;

            var grid = new Grid { Margin = new Thickness(10, 5, 10, 5) };
            for (int i = 0; i < 5; i++) grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            grid.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));

            void AddRow(int row, string label, Control ctrl)
            {
                var lbl = new TextBlock { Text = label, Foreground = Brushes.White, Margin = new Thickness(0,5,10,5), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(lbl, row); Grid.SetColumn(lbl, 0);
                Grid.SetRow(ctrl, row); Grid.SetColumn(ctrl, 1);
                grid.Children.Add(lbl);
                grid.Children.Add(ctrl);
            }

            _txtNomBouche = new TextBox { Margin = new Thickness(0,5,0,5) };
            _cmbTypeBouche = new ComboBox { Margin = new Thickness(0,5,0,5) };
            foreach (TypeBouche t in Enum.GetValues(typeof(TypeBouche)))
            {
                if (t == TypeBouche.Echangeur || t == TypeBouche.PlenumExtraction || t == TypeBouche.PlenumInsufflation) continue;
                _cmbTypeBouche.Items.Add(t);
            }
            _numDiametre = new NumericUpDown { Minimum = 80, Maximum = 250, Increment = 5, Margin = new Thickness(0,5,0,5) };
            _numDebit    = new NumericUpDown { Minimum = 5,  Maximum = 500, Increment = 5, Margin = new Thickness(0,5,0,5) };
            _chkDebitFixe = new CheckBox
            {
                Content = new TextBlock { Text = "Imposé (non redistribué)", Foreground = new SolidColorBrush(Color.FromRgb(241, 196, 15)), FontSize = 11 },
                Margin = new Thickness(0,5,0,5)
            };

            AddRow(0, "Nom:",        _txtNomBouche);
            AddRow(1, "Type:",       _cmbTypeBouche);
            AddRow(2, "Ø (mm):",     _numDiametre);
            AddRow(3, "Débit:",      _numDebit);
            AddRow(4, "Débit fixe:", _chkDebitFixe);

            section.Children.Add(grid);

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
            var btnBilan = CreerBouton("AFFICHER BILAN", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
            btnBilan.Click += (s, e) => { _tabControl.SelectedIndex = 1; MettreAJourBilan(); };
            section.Children.Add(btnBilan);
            var btnExporter = CreerBouton("EXPORTER JSON", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
            btnExporter.Click += BtnExporterJson_Click;
            section.Children.Add(btnExporter);
            var btnImporter = CreerBouton("IMPORTER JSON", new SolidColorBrush(Color.FromRgb(255, 193, 7)));
            btnImporter.Click += BtnImporterJson_Click;
            section.Children.Add(btnImporter);
            return section;
        }

        private StackPanel CreerSection(string titre)
        {
            var section = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            section.Children.Add(new TextBlock
            {
                Text = titre, Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
                FontSize = 11, FontWeight = FontWeight.Bold, Padding = new Thickness(15, 5, 15, 5)
            });
            return section;
        }

        private Button CreerBouton(string texte, IBrush background) => new Button
        {
            Content = texte, Background = background, Foreground = Brushes.White,
            Margin = new Thickness(10, 3, 10, 3), HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(10, 8, 10, 8), FontWeight = FontWeight.SemiBold
        };

        // ════════════════════════════════════════════════════════
        //  PANNEAU DROIT — Moteur VMC
        // ════════════════════════════════════════════════════════

        private Control CreerPanneauMoteur()
        {
            var stack = new StackPanel { Background = new SolidColorBrush(Color.FromRgb(30, 42, 54)) };

            stack.Children.Add(new TextBlock { Text = "MOTEUR VMC", Foreground = Brushes.White, FontSize = 14, FontWeight = FontWeight.Bold, Padding = new Thickness(15, 15, 15, 10) });

            // Capacité unique
            stack.Children.Add(CreerSousEnteteMoteur("CAPACITÉ VENTILATEUR"));
            var bordCapa = new Border { Background = new SolidColorBrush(Color.FromRgb(44, 62, 80)), CornerRadius = new CornerRadius(6), Margin = new Thickness(10,4,10,4), Padding = new Thickness(12,10) };
            var rowCapa = new Grid();
            rowCapa.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            rowCapa.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
            rowCapa.Children.Add(new TextBlock { Text = "Capacité (m³/h)", Foreground = new SolidColorBrush(Color.FromRgb(236, 240, 241)), VerticalAlignment = VerticalAlignment.Center });
            _numCapacite = new NumericUpDown { Width = 120, Minimum = 10, Maximum = 2000, Increment = 10, Value = 200, FormatString = "F0" };
            Grid.SetColumn(_numCapacite, 1);
            rowCapa.Children.Add(_numCapacite);
            bordCapa.Child = rowCapa;
            stack.Children.Add(bordCapa);
            stack.Children.Add(new TextBlock { Text = "↑ Identique extraction & insufflation", Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)), FontSize = 10, FontStyle = FontStyle.Italic, Margin = new Thickness(12,0,10,6) });

            // Extraction
            stack.Children.Add(CreerSousEnteteMoteur("VENTILATEUR EXTRACTION"));
            stack.Children.Add(CreerBlocMoteurCote(true,
                out _txtDebitExtractionDemande, out _progressExtraction,
                out _txtTauxExtraction, out _txtStatutExtraction, out _bordStatutExtraction));

            // Insufflation
            stack.Children.Add(CreerSousEnteteMoteur("VENTILATEUR INSUFFLATION"));
            stack.Children.Add(CreerBlocMoteurCote(false,
                out _txtDebitInsufflationDemande, out _progressInsufflation,
                out _txtTauxInsufflation, out _txtStatutInsufflation, out _bordStatutInsufflation));

            // Redistribution
            stack.Children.Add(CreerSousEnteteMoteur("REDISTRIBUTION DÉBITS"));
            var bordDetail = new Border { Background = new SolidColorBrush(Color.FromRgb(40, 55, 70)), CornerRadius = new CornerRadius(6), Margin = new Thickness(10,4,10,4), Padding = new Thickness(12,8) };
            _txtDetailDebits = new TextBlock { Text = "—", Foreground = new SolidColorBrush(Color.FromRgb(189, 195, 199)), FontSize = 11, TextWrapping = TextWrapping.Wrap, FontFamily = new FontFamily("Courier New") };
            bordDetail.Child = _txtDetailDebits;
            stack.Children.Add(bordDetail);

            // Statut global
            stack.Children.Add(CreerSousEnteteMoteur("STATUT GLOBAL"));
            _bordStatutGlobal = new Border { Background = new SolidColorBrush(Color.FromRgb(26, 50, 37)), CornerRadius = new CornerRadius(6), Margin = new Thickness(10,4,10,4), Padding = new Thickness(12,10) };
            _txtStatutGlobal = new TextBlock { Text = "✅ Système dans les capacités", Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)), FontWeight = FontWeight.Bold, FontSize = 12, TextWrapping = TextWrapping.Wrap };
            _bordStatutGlobal.Child = _txtStatutGlobal;
            stack.Children.Add(_bordStatutGlobal);

            var btnRecalc = CreerBouton("⟳  RECALCULER", new SolidColorBrush(Color.FromRgb(0, 120, 215)));
            btnRecalc.Margin = new Thickness(10, 10, 10, 4);
            btnRecalc.Click += (s, e) => RecalculerMoteur();
            stack.Children.Add(btnRecalc);

            // Légende
            stack.Children.Add(CreerSousEnteteMoteur("LÉGENDE"));
            var bordLegende = new Border { Background = new SolidColorBrush(Color.FromRgb(40, 55, 70)), CornerRadius = new CornerRadius(6), Margin = new Thickness(10,4,10,10), Padding = new Thickness(12,8) };
            var legendeStack = new StackPanel { Spacing = 5 };
            legendeStack.Children.Add(new TextBlock { Text = "✅ Normal — débit dans la capacité",          Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)), FontSize = 11, TextWrapping = TextWrapping.Wrap });
            legendeStack.Children.Add(new TextBlock { Text = "⛔ Insufflation bloquée ",        Foreground = new SolidColorBrush(Color.FromRgb(255, 149, 0)),  FontSize = 11, TextWrapping = TextWrapping.Wrap });
            legendeStack.Children.Add(new TextBlock { Text = "⛔ Extraction bloquée — surplus non évacué", Foreground = new SolidColorBrush(Color.FromRgb(255, 59, 48)),   FontSize = 11, TextWrapping = TextWrapping.Wrap });
            legendeStack.Children.Add(new TextBlock { Text = "🔒 Toutes fixes — équilibre impossible",      Foreground = new SolidColorBrush(Color.FromRgb(255, 220, 0)),   FontSize = 11, TextWrapping = TextWrapping.Wrap });
            bordLegende.Child = legendeStack;
            stack.Children.Add(bordLegende);

            // NOTE: _numCapacite.ValueChanged est branché dans le constructeur,
            // après que _projet et tous les contrôles soient initialisés.

            return new ScrollViewer
            {
                Content = stack,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            };
        }

        private Border CreerBlocMoteurCote(
            bool isExtraction,
            out TextBlock txtDemande, out ProgressBar progress,
            out TextBlock txtTaux, out TextBlock txtStatut, out Border bordStatut)
        {
            var barreColor = isExtraction ? Color.FromRgb(220, 53, 69) : Color.FromRgb(13, 110, 253);
            var contenu = new StackPanel { Spacing = 6 };

            txtDemande = new TextBlock { Text = "Effectif : — m³/h", Foreground = new SolidColorBrush(Color.FromRgb(189, 195, 199)), FontSize = 11 };
            contenu.Children.Add(txtDemande);

            progress = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Height = 10, Foreground = new SolidColorBrush(barreColor), Background = new SolidColorBrush(Color.FromRgb(73, 92, 110)), CornerRadius = new CornerRadius(4) };
            contenu.Children.Add(progress);

            txtTaux = new TextBlock { Text = "Charge : 0 %", Foreground = new SolidColorBrush(Color.FromRgb(189, 195, 199)), FontSize = 11 };
            contenu.Children.Add(txtTaux);

            bordStatut = new Border { CornerRadius = new CornerRadius(4), Padding = new Thickness(8, 5), Background = new SolidColorBrush(Color.FromRgb(26, 37, 31)) };
            txtStatut = new TextBlock { Text = "✅ Normal", Foreground = new SolidColorBrush(Color.FromRgb(46, 204, 113)), FontWeight = FontWeight.Bold, FontSize = 11, TextWrapping = TextWrapping.Wrap };
            bordStatut.Child = txtStatut;
            contenu.Children.Add(bordStatut);

            return new Border { Background = new SolidColorBrush(Color.FromRgb(44, 62, 80)), CornerRadius = new CornerRadius(6), Margin = new Thickness(10,4,10,4), Padding = new Thickness(12,10), Child = contenu };
        }

        private TextBlock CreerSousEnteteMoteur(string texte) => new TextBlock
        {
            Text = texte, Foreground = new SolidColorBrush(Color.FromRgb(149, 165, 166)),
            FontSize = 11, FontWeight = FontWeight.Bold, Padding = new Thickness(15, 10, 15, 4)
        };

        // ════════════════════════════════════════════════════════
        //  LOGIQUE MOTEUR VMC
        // ════════════════════════════════════════════════════════

        private void RecalculerMoteur()
        {
            // Garde-fou : si le contrôle n'est pas encore initialisé, on sort.
            if (_numCapacite == null) return;

            _moteur.Capacite = (double)(_numCapacite.Value ?? 200);

            var bilan    = _projet.CalculerBilan();
            var resultat = _moteur.Calculer(bilan);
            _dernierResultat = resultat;

            AfficherResultatMoteur(resultat);

            // Transmettre les débits effectifs à la vue graphique
            _vueGraphique?.SetDebitsEffectifs(
                resultat.TousDebitsParId,
                !resultat.ToutDansCapacite);

            // Rafraîchir le bilan maintenant que _dernierResultat est à jour
            MettreAJourBilan();
        }

        private void AfficherResultatMoteur(ResultatMoteur r)
        {
            // Extraction
            _txtDebitExtractionDemande.Text =
                $"Effectif : {r.Extraction.DebitEffectif:F1} m³/h" +
                (r.Extraction.Depassement > 0 ? $"  (débord. +{r.Extraction.Depassement:F1})" : "");
            _progressExtraction.Value = Math.Min(r.Extraction.TauxCharge, 100);
            _txtTauxExtraction.Text   = $"Charge : {r.Extraction.TauxCharge:F0} %";
            AppliquerStyleStatutMoteur(
                r.Extraction.Etat == EtatCoteMoteur.Normal,
                r.Extraction.Etat == EtatCoteMoteur.DesequilibreImpossible,
                r.Extraction.Etat == EtatCoteMoteur.ExtractionBloquee,
                _txtStatutExtraction, _bordStatutExtraction,
                r.Extraction.Etat switch
                {
                    EtatCoteMoteur.Normal                => "✅ Normal",
                    EtatCoteMoteur.ExtractionBloquee     => $"⛔ Bloquée +{r.Extraction.Depassement:F1} m³/h",
                    EtatCoteMoteur.DesequilibreImpossible=> "🔒 Tout fixe",
                    _                                    => "—"
                });

            // Insufflation
            _txtDebitInsufflationDemande.Text =
                $"Effectif : {r.Insufflation.DebitEffectif:F1} m³/h" +
                (r.Insufflation.DebitEffectif > r.Insufflation.CapaciteMax
                    ? $"  (bypass +{(r.Insufflation.DebitEffectif - r.Insufflation.CapaciteMax):F1})"
                    : "");
            _progressInsufflation.Value = Math.Min(r.Insufflation.TauxCharge, 100);
            _txtTauxInsufflation.Text   = $"Charge : {r.Insufflation.TauxCharge:F0} %";
            AppliquerStyleStatutMoteur(
                r.Insufflation.Etat == EtatCoteMoteur.Normal,
                r.Insufflation.Etat == EtatCoteMoteur.InsufflationLibre || r.Insufflation.Etat == EtatCoteMoteur.DesequilibreImpossible,
                false,
                _txtStatutInsufflation, _bordStatutInsufflation,
                r.Insufflation.Etat switch
                {
                    EtatCoteMoteur.Normal                => "✅ Normal",
                    EtatCoteMoteur.InsufflationLibre     => $"⛔ Bloquée +{(r.Insufflation.DebitEffectif - r.Insufflation.CapaciteMax):F1} m³/h",
                    EtatCoteMoteur.DesequilibreImpossible=> "🔒 Tout fixe",
                    _                                    => "—"
                });

            // Redistribution
            AfficherDetailRedistribution(r);

            // Statut global
            bool ok = r.ToutDansCapacite;
            _txtStatutGlobal.Text       = r.StatutGlobal;
            _txtStatutGlobal.Foreground = ok
                ? new SolidColorBrush(Color.FromRgb(46, 204, 113))
                : new SolidColorBrush(Color.FromRgb(231, 76, 60));
            _bordStatutGlobal.Background = ok
                ? new SolidColorBrush(Color.FromRgb(26, 50, 37))
                : new SolidColorBrush(Color.FromRgb(60, 20, 20));
        }

        private void AfficherDetailRedistribution(ResultatMoteur r)
        {
            var sb = new System.Text.StringBuilder();

            void AjouterCote(string titre, ResultatCoteMoteur cote)
            {
                sb.AppendLine(titre);
                if (cote.Allocations.Count == 0) { sb.AppendLine("  (aucune bouche)"); return; }
                foreach (var alloc in cote.Allocations.OrderByDescending(a => a.EstFixe))
                {
                    string tag = alloc.EstFixe ? "🔒" : "🔄";
                    string nom = alloc.Bouche.Nom.Length > 12 ? alloc.Bouche.Nom[..12] + "…" : alloc.Bouche.Nom.PadRight(13);
                    sb.AppendLine($"  {tag} {nom} {alloc.DebitEffectif,6:F1} m³/h");
                }
                sb.AppendLine($"  ─── Total : {cote.DebitEffectif:F1} m³/h");
            }

            AjouterCote("EXTRACTION :", r.Extraction);
            sb.AppendLine();
            AjouterCote("INSUFFLATION :", r.Insufflation);
            _txtDetailDebits.Text = sb.ToString().TrimEnd();
        }

        private static void AppliquerStyleStatutMoteur(
            bool estNormal, bool estAvertissement, bool estErreur,
            TextBlock txt, Border bord, string message)
        {
            txt.Text = message;
            if (estNormal)
            {
                txt.Foreground  = new SolidColorBrush(Color.FromRgb(46, 204, 113));
                bord.Background = new SolidColorBrush(Color.FromRgb(26, 37, 31));
            }
            else if (estAvertissement)
            {
                txt.Foreground  = new SolidColorBrush(Color.FromRgb(255, 149, 0));
                bord.Background = new SolidColorBrush(Color.FromRgb(40, 32, 10));
            }
            else if (estErreur)
            {
                txt.Foreground  = new SolidColorBrush(Color.FromRgb(255, 59, 48));
                bord.Background = new SolidColorBrush(Color.FromRgb(45, 15, 15));
            }
        }

        // ════════════════════════════════════════════════════════
        //  PANNEAU BILAN (onglet centre)
        // ════════════════════════════════════════════════════════

        private Control CreerPanneauBilan()
        {
            var mainPanel = new DockPanel();

            var scrollViewer = new ScrollViewer { Background = Brushes.White };

            var stackPanel = new StackPanel { Margin = new Thickness(20) };
            stackPanel.Children.Add(new TextBlock { Text = "BILAN AÉRAULIQUE", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.FromRgb(44, 62, 80)), Margin = new Thickness(0,0,0,20) });

            var resumeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(236, 240, 241)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(8),
                Margin = new Thickness(0,0,0,20), MaxWidth = 800, Padding = new Thickness(15)
            };
            var resumePanel = new StackPanel();
            _texteBilanExtraction   = new TextBlock { FontSize = 14, Margin = new Thickness(0,3,0,3) };
            _texteBilanInsufflation = new TextBlock { FontSize = 14, Margin = new Thickness(0,3,0,3) };
            _texteBilanTotal        = new TextBlock { FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0,10,0,3) };
            _texteBilanEquilibre    = new TextBlock { FontSize = 14, FontWeight = FontWeight.Bold, Margin = new Thickness(0,3,0,3) };
            resumePanel.Children.Add(_texteBilanExtraction);
            resumePanel.Children.Add(_texteBilanInsufflation);
            resumePanel.Children.Add(_texteBilanTotal);
            resumePanel.Children.Add(_texteBilanEquilibre);
            resumeBorder.Child = resumePanel;
            stackPanel.Children.Add(resumeBorder);

            stackPanel.Children.Add(new TextBlock { Text = "Détail des bouches", FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0,0,0,10) });

            var detailsBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15), MaxWidth = 800
            };
            _texteBilanDetaille = new TextBox
            {
                IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.Wrap,
                MinHeight = 300, FontFamily = new FontFamily("Courier New"),
                FontSize = 12, Background = Brushes.White, BorderThickness = new Thickness(0)
            };
            detailsBorder.Child = _texteBilanDetaille;
            stackPanel.Children.Add(detailsBorder);

            stackPanel.Children.Add(new TextBlock { Text = "Légende", FontSize = 16, FontWeight = FontWeight.Bold, Margin = new Thickness(0,20,0,10) });
            var legendeBorder = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(189, 195, 199)),
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(5),
                Padding = new Thickness(15), MaxWidth = 800
            };
            var legendePanel = new StackPanel();
            legendePanel.Children.Add(CreerLigneLegendeBilan("🔴 ROUGE: Extraction (air vicié)",           new SolidColorBrush(Color.FromRgb(220, 53, 69))));
            legendePanel.Children.Add(CreerLigneLegendeBilan("🔵 BLEU: Insufflation (air neuf réchauffé)", new SolidColorBrush(Color.FromRgb(13, 110, 253))));
            legendePanel.Children.Add(CreerLigneLegendeBilan("🟤 MARRON: Porte",                           new SolidColorBrush(Color.FromRgb(139, 69, 19))));
            legendePanel.Children.Add(CreerLigneLegendeBilan("🔵 BLEU ACIER: Fenêtre",                     new SolidColorBrush(Color.FromRgb(70, 130, 180))));
            legendeBorder.Child = legendePanel;
            stackPanel.Children.Add(legendeBorder);

            scrollViewer.Content = stackPanel;
            mainPanel.Children.Add(scrollViewer);
            return mainPanel;
        }

        private Control CreerLigneLegendeBilan(string texte, IBrush couleur)
        {
            var stack = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            stack.Children.Add(new Border { Width = 20, Height = 20, CornerRadius = new CornerRadius(10), Background = couleur, Margin = new Thickness(0,0,10,0) });
            stack.Children.Add(new TextBlock { Text = texte, VerticalAlignment = VerticalAlignment.Center });
            return stack;
        }

        // ════════════════════════════════════════════════════════
        //  ÉVÉNEMENTS
        // ════════════════════════════════════════════════════════

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
                _txtNomBouche.Text          = bouche.Nom;
                _cmbTypeBouche.SelectedItem = bouche.Type;
                _numDiametre.Value          = (decimal)bouche.Diametre;
                _numDebit.Value             = (decimal)bouche.Debit;
                _chkDebitFixe.IsChecked     = bouche.DebitFixe;
            }
            else
            {
                _panelEditionBouche.IsVisible = false;
            }
        }

        private void OnProjetModifie(object? sender, EventArgs e)
        {
            MettreAJourInterface();
            RecalculerMoteur();
        }

        private void BtnSauvegarderBouche_Click(object? sender, RoutedEventArgs e)
        {
            var bouche = _vueGraphique.GetBoucheSelectionnee();
            if (bouche != null)
            {
                TypeBouche ancienType = bouche.Type;
                bouche.Nom      = _txtNomBouche.Text ?? "Sans nom";
                bouche.Type     = (TypeBouche)(_cmbTypeBouche.SelectedItem ?? TypeBouche.Extraction);
                bouche.Diametre = _numDiametre.Value.HasValue ? (double)_numDiametre.Value.Value : 125;
                bouche.Debit    = _numDebit.Value.HasValue    ? (double)_numDebit.Value.Value    : 45;
                bouche.DebitFixe = _chkDebitFixe.IsChecked == true;

                // Si c'était un élément global et qu'on change son type vers une bouche normale
                bool estaitGlobal = ancienType == TypeBouche.Echangeur ||
                                    ancienType == TypeBouche.PlenumExtraction ||
                                    ancienType == TypeBouche.PlenumInsufflation;
                bool devientNormal = bouche.Type == TypeBouche.Extraction ||
                                     bouche.Type == TypeBouche.Insufflation ||
                                     bouche.Type == TypeBouche.Porte ||
                                     bouche.Type == TypeBouche.Fenetre;

                if (estaitGlobal && devientNormal && _projet.EtageActif != null)
                {
                    if (ancienType == TypeBouche.Echangeur)
                        { _projet.Echangeur = null; _projet.EchangeurEtageId = null; }
                    else if (ancienType == TypeBouche.PlenumExtraction)
                        { _projet.PlenumExtraction = null; _projet.PlenumExtractionEtageId = null; }
                    else if (ancienType == TypeBouche.PlenumInsufflation)
                        { _projet.PlenumInsufflation = null; _projet.PlenumInsufflationEtageId = null; }

                    _projet.EtageActif.AjouterBouche(bouche);
                }

                _vueGraphique.InvalidateVisual();
                MettreAJourInterface();
                RecalculerMoteur();
            }
        }

        private void BtnSupprimerBouche_Click(object? sender, RoutedEventArgs e)
        {
            _vueGraphique.SupprimerBoucheSelectionnee();
            _panelEditionBouche.IsVisible = false;
            MettreAJourInterface();
            RecalculerMoteur();
        }

        // ════════════════════════════════════════════════════════
        //  BOÎTES DE DIALOGUE ÉTAGES
        // ════════════════════════════════════════════════════════

        private async void BtnAjouterEtage_Click(object? sender, RoutedEventArgs e)
        {
            var dlg = new Window { Title = "Nouvel Étage", Width = 350, Height = 300, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var sp = new StackPanel { Margin = new Thickness(20) };
            sp.Children.Add(new TextBlock { Text = "Nom de l'étage:", Margin = new Thickness(0,0,0,5) });
            var txtNom = new TextBox { Text = $"Étage {_projet.Etages.Count}", Margin = new Thickness(0,0,0,15) };
            sp.Children.Add(txtNom);
            sp.Children.Add(new TextBlock { Text = "Largeur (mètres):", Margin = new Thickness(0,0,0,5) });
            var numL = new NumericUpDown { Value = 15M, Minimum = 3M, Maximum = 50M, Increment = 0.5M, Margin = new Thickness(0,0,0,15) };
            sp.Children.Add(numL);
            sp.Children.Add(new TextBlock { Text = "Hauteur (mètres):", Margin = new Thickness(0,0,0,5) });
            var numH = new NumericUpDown { Value = 12M, Minimum = 3M, Maximum = 50M, Increment = 0.5M, Margin = new Thickness(0,0,0,20) };
            sp.Children.Add(numH);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok  = new Button { Content = "Créer", Background = Brushes.DarkGreen, Foreground = Brushes.White, Padding = new Thickness(20,10,20,10), Margin = new Thickness(0,0,10,0) };
            ok.Click += (s, a) => dlg.Close(true);
            var ann = new Button { Content = "Annuler", Padding = new Thickness(20,10,20,10) };
            ann.Click += (s, a) => dlg.Close(false);
            btns.Children.Add(ok); btns.Children.Add(ann);
            sp.Children.Add(btns);
            dlg.Content = sp;

            var result = await dlg.ShowDialog<bool>(this);
            if (result && numL.Value.HasValue && numH.Value.HasValue)
            {
                string nomSaisi = (txtNom.Text ?? $"Étage {_projet.Etages.Count + 1}").Trim();
                double largeur  = (double)numL.Value.Value;
                double hauteur  = (double)numH.Value.Value;

                var etageExistant = _projet.Etages
                    .FirstOrDefault(et => string.Equals(et.Nom, nomSaisi, StringComparison.OrdinalIgnoreCase));

                if (etageExistant != null)
                {
                    etageExistant.Largeur = largeur;
                    etageExistant.Hauteur = hauteur;
                    _projet.EtageActif = etageExistant;
                }
                else
                {
                    _projet.AjouterEtage(new Etage(nomSaisi, largeur, hauteur));
                }

                MettreAJourInterface();
            }
        }

        private async void BtnRenommerEtage_Click(object? sender, RoutedEventArgs e)
        {
            if (_listeEtages.SelectedIndex < 0)
            {
                var m = new Window { Title = "Attention", Width = 300, Height = 150, Content = new TextBlock { Text = "Veuillez sélectionner un étage.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap } };
                await m.ShowDialog(this); return;
            }
            var etage = _projet.Etages[_listeEtages.SelectedIndex];
            var dlg = new Window { Title = "Renommer l'étage", Width = 320, Height = 180, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var sp = new StackPanel { Margin = new Thickness(20) };
            sp.Children.Add(new TextBlock { Text = "Nouveau nom:", Margin = new Thickness(0,0,0,8) });
            var txtNom = new TextBox { Text = etage.Nom, Margin = new Thickness(0,0,0,20) };
            sp.Children.Add(txtNom);
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var ok  = new Button { Content = "Renommer", Background = new SolidColorBrush(Color.FromRgb(30, 100, 180)), Foreground = Brushes.White, Padding = new Thickness(15,8,15,8), Margin = new Thickness(0,0,10,0) };
            ok.Click += (s, a) => dlg.Close(true);
            var ann = new Button { Content = "Annuler", Padding = new Thickness(15,8,15,8) };
            ann.Click += (s, a) => dlg.Close(false);
            btns.Children.Add(ok); btns.Children.Add(ann);
            sp.Children.Add(btns);
            dlg.Content = sp;
            if (await dlg.ShowDialog<bool>(this))
            {
                string nouveauNom = (txtNom.Text ?? etage.Nom).Trim();
                if (!string.IsNullOrEmpty(nouveauNom))
                {
                    etage.Nom = nouveauNom;
                    MettreAJourInterface();
                }
            }
        }

        private async void BtnSupprimerEtage_Click(object? sender, RoutedEventArgs e)
        {
            if (_listeEtages.SelectedIndex < 0)
            {
                var m = new Window { Title = "Attention", Width = 300, Height = 150, Content = new TextBlock { Text = "Veuillez sélectionner un étage.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap } };
                await m.ShowDialog(this); return;
            }
            if (_projet.Etages.Count <= 1)
            {
                var m = new Window { Title = "Impossible", Width = 300, Height = 150, Content = new TextBlock { Text = "Impossible de supprimer le dernier étage.", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap } };
                await m.ShowDialog(this); return;
            }
            var etage = _projet.Etages[_listeEtages.SelectedIndex];
            var dlg = new Window { Title = "Confirmation", Width = 350, Height = 180, WindowStartupLocation = WindowStartupLocation.CenterOwner };
            var csp = new StackPanel { Margin = new Thickness(20), Spacing = 20 };
            csp.Children.Add(new TextBlock { Text = $"Supprimer '{etage.Nom}' ?\nToutes les bouches et murs seront perdus.", TextWrapping = TextWrapping.Wrap });
            var btns = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var oui = new Button { Content = "Oui, supprimer", Background = Brushes.DarkRed, Foreground = Brushes.White, Padding = new Thickness(15,8,15,8), Margin = new Thickness(0,0,10,0) };
            oui.Click += (s, a) => dlg.Close(true);
            var non = new Button { Content = "Non, annuler", Padding = new Thickness(15,8,15,8) };
            non.Click += (s, a) => dlg.Close(false);
            btns.Children.Add(oui); btns.Children.Add(non);
            csp.Children.Add(btns);
            dlg.Content = csp;
            if (await dlg.ShowDialog<bool>(this))
            {
                _projet.SupprimerEtage(etage.Id);
                MettreAJourInterface();
            }
        }

        // ════════════════════════════════════════════════════════
        //  IMPORT / EXPORT JSON
        // ════════════════════════════════════════════════════════

        private async void BtnExporterJson_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog { Title = "Exporter le projet", Filters = new System.Collections.Generic.List<FileDialogFilter> { new FileDialogFilter { Name = "Fichiers JSON", Extensions = { "json" } } }, DefaultExtension = "json" };
            var result = await dialog.ShowAsync(this);
            if (result != null)
            {
                try
                {
                    File.WriteAllText(result, _projet.SauvegarderJson());
                    var m = new Window { Title = "Succès", Width = 300, Height = 150, Content = new TextBlock { Text = "Projet exporté avec succès!", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
                    await m.ShowDialog(this);
                }
                catch (Exception ex)
                {
                    var m = new Window { Title = "Erreur", Width = 400, Height = 150, Content = new TextBlock { Text = $"Erreur : {ex.Message}", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap } };
                    await m.ShowDialog(this);
                }
            }
        }

        private async void BtnImporterJson_Click(object? sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Title = "Importer un projet", Filters = new System.Collections.Generic.List<FileDialogFilter> { new FileDialogFilter { Name = "Fichiers JSON", Extensions = { "json" } } }, AllowMultiple = false };
            var result = await dialog.ShowAsync(this);
            if (result != null && result.Length > 0)
            {
                try
                {
                    var projet = ProjetVMC.ChargerJson(File.ReadAllText(result[0]));
                    if (projet != null)
                    {
                        _projet = projet;
                        if (_projet.EtageActif == null && _projet.Etages.Count > 0)
                            _projet.EtageActif = _projet.Etages[0];
                        _vueGraphique.SetProjet(_projet);
                        MettreAJourInterface();
                        RecalculerMoteur();
                        var m = new Window { Title = "Succès", Width = 300, Height = 150, Content = new TextBlock { Text = "Projet importé avec succès!", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
                        await m.ShowDialog(this);
                    }
                }
                catch (Exception ex)
                {
                    var m = new Window { Title = "Erreur", Width = 400, Height = 150, Content = new TextBlock { Text = $"Erreur : {ex.Message}", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap } };
                    await m.ShowDialog(this);
                }
            }
        }

        // ════════════════════════════════════════════════════════
        //  MISE À JOUR INTERFACE
        // ════════════════════════════════════════════════════════

        private void MettreAJourInterface()
        {
            _listeEtages.SelectionChanged -= OnEtageSelectionne;
            _listeEtages.Items.Clear();
            foreach (var etage in _projet.Etages)
                _listeEtages.Items.Add(etage.Nom);
            if (_projet.EtageActif != null)
                _listeEtages.SelectedIndex = _projet.Etages.IndexOf(_projet.EtageActif);
            _listeEtages.SelectionChanged += OnEtageSelectionne;
            // Toujours recalculer avant de rafraîchir le bilan
            // pour que _dernierResultat soit à jour.
            RecalculerMoteur();
        }

        private void MettreAJourBilan()
        {
            var bilan = _projet.CalculerBilan();

            // ── Débits effectifs issus du moteur (plafonnés à la capacité) ──
            // Si le moteur n'a pas encore tourné, on tombe sur les nominaux.
            double debitEffExt  = _dernierResultat?.Extraction.DebitEffectif   ?? bilan.DebitTotalExtraction;
            double debitEffInsuf = _dernierResultat?.Insufflation.DebitEffectif ?? bilan.DebitTotalInsufflation;
            int    nbExt   = bilan.BouchesExtraction.Count;
            int    nbInsuf = bilan.BouchesInsufflation.Count;

            // Afficher nominal ET effectif si différents
            string suffixExt  = Math.Abs(debitEffExt  - bilan.DebitTotalExtraction)   > 0.05
                ? $"  (demandé: {bilan.DebitTotalExtraction:F1})" : "";
            string suffixInsuf = Math.Abs(debitEffInsuf - bilan.DebitTotalInsufflation) > 0.05
                ? $"  (demandé: {bilan.DebitTotalInsufflation:F1})" : "";

            _texteBilanExtraction.Text   = $"Débit effectif extraction: {debitEffExt:F1} m³/h ({nbExt} bouches){suffixExt}";
            _texteBilanInsufflation.Text = $"Débit effectif insufflation: {debitEffInsuf:F1} m³/h ({nbInsuf} bouches){suffixInsuf}";
            _texteBilanTotal.Text        = $"Nombre total de bouches: {bilan.NombreTotalBouches}";

            // Équilibre basé sur les débits effectifs (pas nominaux)
            double diffEff = Math.Abs(debitEffExt - debitEffInsuf);
            bool equilibreEff = diffEff < 5.0;

            if (_dernierResultat != null &&
                (_dernierResultat.Extraction.Etat == EtatCoteMoteur.DesequilibreImpossible ||
                 _dernierResultat.Insufflation.Etat == EtatCoteMoteur.DesequilibreImpossible))
            {
                _texteBilanEquilibre.Text       = "🔒 Équilibre impossible — toutes les bouches sont fixes avec totaux inégaux";
                _texteBilanEquilibre.Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 0));
            }
            else if (equilibreEff)
            {
                _texteBilanEquilibre.Text       = "✓ Le système est équilibré";
                _texteBilanEquilibre.Foreground = Brushes.Green;
            }
            else
            {
                _texteBilanEquilibre.Text       = $"⚠ Déséquilibre effectif: {diffEff:F1} m³/h";
                _texteBilanEquilibre.Foreground = Brushes.Red;
            }

            // Récupérer les débits effectifs du dernier résultat moteur
            var debitsEff = _dernierResultat?.TousDebitsParId;

            var details = new System.Text.StringBuilder();
            details.AppendLine("ÉLÉMENTS VMC:");
            details.AppendLine(new string('=', 50));
            details.AppendLine($"  Échangeur: {(bilan.Echangeur != null ? "✓ Installé" : "✗ Manquant")}");
            details.AppendLine($"  Plenum Extraction: {(bilan.PlenumExtraction != null ? "✓ Installé" : "✗ Manquant")}");
            details.AppendLine($"  Plenum Insufflation: {(bilan.PlenumInsufflation != null ? "✓ Installé" : "✗ Manquant")}");
            details.AppendLine($"  Système complet: {(bilan.SystemeComplet() ? "✓ OUI" : "✗ NON")}");
            details.AppendLine();

            // ── Bilans par étage ──────────────────────────────────
            details.AppendLine("BILANS PAR ÉTAGE:");
            details.AppendLine(new string('=', 50));
            foreach (var bilanEtage in bilan.BilansEtages)
            {
                details.AppendLine($"\n═══ {bilanEtage.NomEtage.ToUpper()} ═══");
                details.AppendLine($"  Extraction   : {bilanEtage.DebitTotalExtraction:F1} m³/h ({bilanEtage.BouchesExtraction.Count} bouches)");
                details.AppendLine($"  Insufflation : {bilanEtage.DebitTotalInsufflation:F1} m³/h ({bilanEtage.BouchesInsufflation.Count} bouches)");
                details.AppendLine($"  Équilibre    : {(bilanEtage.EstEquilibre() ? "✓" : "✗")} ({Math.Abs(bilanEtage.DebitTotalExtraction - bilanEtage.DebitTotalInsufflation):F1} m³/h)");

                void AjouterListeBouches(string titre, System.Collections.Generic.List<BoucheVMC> bouches)
                {
                    if (bouches.Count == 0) return;
                    details.AppendLine($"  {titre}");
                    foreach (var b in bouches)
                    {
                        string fixeTag = b.DebitFixe ? "🔒" : "🔄";
                        // Débit effectif si disponible
                        double deff = debitsEff != null && debitsEff.TryGetValue(b.Id, out double v) ? v : b.Debit;
                        string diffStr = "";
                        if (debitsEff != null && !b.DebitFixe)
                        {
                            double ecart = deff - b.Debit;
                            diffStr = ecart == 0 ? "" : (ecart > 0 ? $" [+{ecart:F1}]" : $" [{ecart:F1}]");
                        }
                        details.AppendLine($"    {fixeTag} {b.Nom,-15} Ø{b.Diametre}mm  nominal:{b.Debit:F1}  effectif:{deff:F1} m³/h{diffStr}");
                    }
                }

                AjouterListeBouches("Bouches d'extraction:", bilanEtage.BouchesExtraction);
                AjouterListeBouches("Bouches d'insufflation:", bilanEtage.BouchesInsufflation);
            }

            // ── Bilan global ─────────────────────────────────────
            details.AppendLine();
            details.AppendLine("BILAN GLOBAL:");
            details.AppendLine(new string('=', 50));

            void AjouterSection(string titre, System.Collections.Generic.List<BoucheVMC> bouches, double totalEff, double totalNominal)
            {
                details.AppendLine(titre);
                foreach (var b in bouches)
                {
                    string fixeTag = b.DebitFixe ? " 🔒 FIXE" : " 🔄 libre";
                    double deff = debitsEff != null && debitsEff.TryGetValue(b.Id, out double v) ? v : b.Debit;
                    string effStr = (!b.DebitFixe && Math.Abs(deff - b.Debit) > 0.05)
                        ? $"  → effectif: {deff:F1}"
                        : "";
                    details.AppendLine($"  • {b.Nom,-20} Ø{b.Diametre}mm  {b.Debit,6:F1} m³/h{fixeTag}{effStr}");
                }
                string totalSuffix = Math.Abs(totalEff - totalNominal) > 0.05
                    ? $"  (demandé: {totalNominal:F1})" : "";
                details.AppendLine($"\nTotal effectif: {totalEff:F1} m³/h{totalSuffix}");
                details.AppendLine();
            }

            AjouterSection("BOUCHES D'EXTRACTION:", bilan.BouchesExtraction, debitEffExt, bilan.DebitTotalExtraction);
            AjouterSection("BOUCHES D'INSUFFLATION:", bilan.BouchesInsufflation, debitEffInsuf, bilan.DebitTotalInsufflation);

            if (bilan.Portes.Count > 0 || bilan.Fenetres.Count > 0)
            {
                details.AppendLine("AUTRES ÉLÉMENTS:");
                details.AppendLine(new string('=', 50));
                foreach (var p in bilan.Portes)   details.AppendLine($"  • {p.Nom} (Porte)");
                foreach (var f in bilan.Fenetres) details.AppendLine($"  • {f.Nom} (Fenêtre)");
            }

            // ── Résumé moteur si déséquilibre ─────────────────────
            if (_dernierResultat != null && !_dernierResultat.ToutDansCapacite)
            {
                details.AppendLine();
                details.AppendLine("⚠ ALERTE MOTEUR:");
                details.AppendLine(new string('=', 50));
                details.AppendLine(_dernierResultat.StatutGlobal);
            }

            _texteBilanDetaille.Text = details.ToString().TrimEnd();
        }
    }
}