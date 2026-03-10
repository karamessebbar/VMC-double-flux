using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System;
using System.Globalization;
using VMCDoubleFlux.Models;

namespace VMCDoubleFlux.Controls
{
    /// <summary>
    /// Contrôle personnalisé pour afficher et éditer le plan de l'étage
    /// </summary>
    public class VueGraphique : Control
    {
        private const double OFFSET_X = 50;
        private const double OFFSET_Y = 50;

        private ProjetVMC? _projet;
        private Etage? _etageActif;

        // ✅ Mur sélectionné (pour suppression)
        private Mur? _murSelectionne;

        private BoucheVMC? _boucheSelectionnee;
        private Point _offsetMouvement;
        private bool _estEnDeplacement;

        // Mode d'édition
        public enum ModeEdition
        {
            Selection,
            AjouterBouche,
            TracerMur,
            AjouterPorte
        }

        private ModeEdition _modeActuel = ModeEdition.Selection;
        private TypeBouche _typeBoucheAjouter = TypeBouche.Extraction;
        private Point? _pointDebutMur;

        // 🎨 Palette (rendu plus pro)
        private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(248, 249, 250));
        private static readonly IBrush GridMinorBrush = new SolidColorBrush(Color.FromRgb(236, 236, 236));
        private static readonly IBrush GridMajorBrush = new SolidColorBrush(Color.FromRgb(214, 214, 214));
        private static readonly IBrush TitleBrush = new SolidColorBrush(Color.FromRgb(44, 62, 80));

        // ✅ Pen de surbrillance mur sélectionné
        private static readonly Pen SelectedWallPen =
            new Pen(new SolidColorBrush(Color.FromRgb(255, 193, 7)), 5, lineCap: PenLineCap.Round);

        // Événements
        public event EventHandler<BoucheVMC?>? BoucheSelectionneeChanged; // ✅ null-safe
        public event EventHandler? ProjetModifie;

        public VueGraphique()
        {
            ClipToBounds = true;
            Focusable = true;
        }

        public void SetProjet(ProjetVMC projet)
        {
            _projet = projet;
            _etageActif = projet.EtageActif;
            _boucheSelectionnee = null;
            _murSelectionne = null;
            InvalidateVisual();
        }

        public void SetEtageActif(Etage? etage)
        {
            _etageActif = etage;
            _boucheSelectionnee = null;
            _murSelectionne = null;
            InvalidateVisual();
        }

        public void SetModeEdition(ModeEdition mode, TypeBouche? typeBouche = null)
        {
            _modeActuel = mode;
            if (typeBouche.HasValue)
                _typeBoucheAjouter = typeBouche.Value;

            _boucheSelectionnee = null;
            _murSelectionne = null;
            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            if (_projet == null || _etageActif == null)
                return;

            // 🎨 Fond moderne
            context.FillRectangle(BackgroundBrush, new Rect(0, 0, Bounds.Width, Bounds.Height));

            double echelle = _projet.Echelle;
            double offsetX = OFFSET_X;
            double offsetY = OFFSET_Y;

            // Dessiner la grille
            DessinerGrille(context, echelle, offsetX, offsetY);

            // Dessiner les échelles
            DessinerEchelles(context, echelle, offsetX, offsetY);

            // Dessiner les murs
            DessinerMurs(context, echelle, offsetX, offsetY);

            // ❌ SUPPRIMÉ : DessinerConnexions - on ne dessine plus les lignes entre bouches et VMC

            // Dessiner les bouches VMC (extraction/insufflation) et portes/fenêtres
            DessinerBouches(context, echelle, offsetX, offsetY);

            // Dessiner le titre
            DessinerTitre(context);
        }

        private void DessinerGrille(DrawingContext context, double echelle, double offsetX, double offsetY)
        {
            if (_etageActif == null) return;

            const double pasMetres = 2.0;

            for (double x = 0; x <= _etageActif.Largeur; x += pasMetres)
            {
                bool majeure = ((int)Math.Round(x)) % 10 == 0;
                var pen = new Pen(majeure ? GridMajorBrush : GridMinorBrush, majeure ? 1.2 : 0.7);

                double screenX = offsetX + x * echelle;
                context.DrawLine(pen,
                    new Point(screenX, offsetY),
                    new Point(screenX, offsetY + _etageActif.Hauteur * echelle));
            }

            for (double y = 0; y <= _etageActif.Hauteur; y += pasMetres)
            {
                bool majeure = ((int)Math.Round(y)) % 10 == 0;
                var pen = new Pen(majeure ? GridMajorBrush : GridMinorBrush, majeure ? 1.2 : 0.7);

                double screenY = offsetY + y * echelle;
                context.DrawLine(pen,
                    new Point(offsetX, screenY),
                    new Point(offsetX + _etageActif.Largeur * echelle, screenY));
            }

            var penCadre = new Pen(new SolidColorBrush(Color.FromRgb(190, 190, 190)), 1);
            context.DrawRectangle(
                null,
                penCadre,
                new Rect(offsetX, offsetY, _etageActif.Largeur * echelle, _etageActif.Hauteur * echelle)
            );
        }

        private void DessinerEchelles(DrawingContext context, double echelle, double offsetX, double offsetY)
        {
            if (_etageActif == null) return;

            var textBrush = new SolidColorBrush(Color.FromRgb(90, 90, 90));
            var typeface = new Typeface("Arial");
            var fontSize = 10;

            for (double x = 0; x <= _etageActif.Largeur; x += 2.0)
            {
                double screenX = offsetX + x * echelle;
                var text = new FormattedText(
                    $"{x:0}m",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    textBrush);

                context.DrawText(text, new Point(screenX - (text.Width / 2), offsetY - 20));
            }

            for (double y = 0; y <= _etageActif.Hauteur; y += 2.0)
            {
                double screenY = offsetY + y * echelle;
                var text = new FormattedText(
                    $"{y:0}m",
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    fontSize,
                    textBrush);

                context.DrawText(text, new Point(offsetX - 35, screenY - (text.Height / 2)));
            }
        }

        private void DessinerMurs(DrawingContext context, double echelle, double offsetX, double offsetY)
        {
            if (_etageActif == null) return;

            foreach (var mur in _etageActif.Murs)
            {
                var pen = mur.EstExterieur
                    ? new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 3)
                    : new Pen(new SolidColorBrush(Color.FromRgb(90, 90, 90)), 2);

                Point debut = new Point(
                    offsetX + mur.Debut.X * echelle,
                    offsetY + mur.Debut.Y * echelle);
                Point fin = new Point(
                    offsetX + mur.Fin.X * echelle,
                    offsetY + mur.Fin.Y * echelle);

                // ✅ Si mur sélectionné => surbrillance
                if (_murSelectionne == mur)
                    context.DrawLine(SelectedWallPen, debut, fin);

                context.DrawLine(pen, debut, fin);
            }
        }

        private void DessinerBouches(DrawingContext context, double echelle, double offsetX, double offsetY)
        {
            if (_etageActif == null) return;

            var typeface = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var typefaceDetail = new Typeface("Arial");

            foreach (var bouche in _etageActif.Bouches)
            {
                Point pos = new Point(
                    offsetX + bouche.Position.X * echelle,
                    offsetY + bouche.Position.Y * echelle);

                // Dessiner selon le type
                switch (bouche.Type)
                {
                    case TypeBouche.Porte:
                        DessinerPorte(context, pos, bouche == _boucheSelectionnee);
                        break;
                    case TypeBouche.Fenetre:
                        DessinerFenetre(context, pos, bouche == _boucheSelectionnee);
                        break;
                    case TypeBouche.Echangeur:
                        DessinerEchangeur(context, pos, bouche == _boucheSelectionnee);
                        break;
                    case TypeBouche.PlenumExtraction:
                        DessinerPlenum(context, pos, bouche, true, bouche == _boucheSelectionnee);
                        break;
                    case TypeBouche.PlenumInsufflation:
                        DessinerPlenum(context, pos, bouche, false, bouche == _boucheSelectionnee);
                        break;
                    default: // Extraction ou Insufflation
                        DessinerBoucheCirculaire(context, pos, bouche, typeface, typefaceDetail);
                        break;
                }
            }

            // Dessiner les éléments uniques s'ils sont sur un autre étage
            if (_projet != null)
            {
                // Échangeur
                if (_projet.Echangeur != null && _projet.EchangeurEtageId == _etageActif.Id)
                {
                    Point posEch = new Point(
                        offsetX + _projet.Echangeur.Position.X * echelle,
                        offsetY + _projet.Echangeur.Position.Y * echelle);
                    DessinerEchangeur(context, posEch, _projet.Echangeur == _boucheSelectionnee);
                }

                // Plenum Extraction
                if (_projet.PlenumExtraction != null && _projet.PlenumExtractionEtageId == _etageActif.Id)
                {
                    Point posPlEx = new Point(
                        offsetX + _projet.PlenumExtraction.Position.X * echelle,
                        offsetY + _projet.PlenumExtraction.Position.Y * echelle);
                    DessinerPlenum(context, posPlEx, _projet.PlenumExtraction, true, _projet.PlenumExtraction == _boucheSelectionnee);
                }

                // Plenum Insufflation
                if (_projet.PlenumInsufflation != null && _projet.PlenumInsufflationEtageId == _etageActif.Id)
                {
                    Point posPlIn = new Point(
                        offsetX + _projet.PlenumInsufflation.Position.X * echelle,
                        offsetY + _projet.PlenumInsufflation.Position.Y * echelle);
                    DessinerPlenum(context, posPlIn, _projet.PlenumInsufflation, false, _projet.PlenumInsufflation == _boucheSelectionnee);
                }
            }
        }

        /// <summary>
        /// Dessine une porte (rectangle avec arc)
        /// </summary>
        private void DessinerPorte(DrawingContext context, Point pos, bool estSelectionnee)
        {
            const double largeur = 50;
            const double epaisseur = 8;

            // Couleur de la porte
            IBrush brushPorte = new SolidColorBrush(Color.FromRgb(139, 69, 19)); // Marron

            // Surbrillance si sélectionnée
            if (estSelectionnee)
            {
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 4),
                    new Rect(pos.X - largeur / 2 - 5, pos.Y - epaisseur / 2 - 5, largeur + 10, epaisseur + 10),
                    5
                );
            }

            // Rectangle de la porte
            Rect rectPorte = new Rect(pos.X - largeur / 2, pos.Y - epaisseur / 2, largeur, epaisseur);
            context.FillRectangle(brushPorte, rectPorte, 2);
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(80, 40, 10)), 1.5), rectPorte, 2);

            // Poignée
            context.DrawEllipse(
                new SolidColorBrush(Color.FromRgb(212, 175, 55)),
                null,
                new Point(pos.X + largeur / 4, pos.Y),
                3, 3
            );

            // Arc d'ouverture (en pointillés)
            var arcGeometry = new PathGeometry();
            var arcFigure = new PathFigure { StartPoint = new Point(pos.X - largeur / 2, pos.Y), IsClosed = false };
            arcFigure.Segments.Add(new ArcSegment
            {
                Point = new Point(pos.X - largeur / 2, pos.Y - largeur),
                Size = new Size(largeur, largeur),
                SweepDirection = SweepDirection.Clockwise
            });
            arcGeometry.Figures.Add(arcFigure);

            context.DrawGeometry(
                null,
                new Pen(new SolidColorBrush(Color.FromRgb(139, 69, 19)), 1, new DashStyle(new double[] { 3, 3 }, 0)),
                arcGeometry
            );

            // Étiquette "PORTE"
            var textPorte = new FormattedText(
                "PORTE",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial", FontStyle.Normal, FontWeight.Bold),
                9,
                new SolidColorBrush(Color.FromRgb(80, 40, 10))
            );
            context.DrawText(textPorte, new Point(pos.X - textPorte.Width / 2, pos.Y + epaisseur / 2 + 5));
        }

        /// <summary>
        /// Dessine une fenêtre (deux rectangles parallèles avec croisillons)
        /// </summary>
        private void DessinerFenetre(DrawingContext context, Point pos, bool estSelectionnee)
        {
            const double largeur = 45;
            const double hauteur = 10;

            // Couleur de la fenêtre
            IBrush brushCadre = new SolidColorBrush(Color.FromRgb(70, 130, 180)); // Bleu acier
            IBrush brushVitre = new SolidColorBrush(Color.FromArgb(100, 135, 206, 250)); // Bleu ciel transparent

            // Surbrillance si sélectionnée
            if (estSelectionnee)
            {
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 4),
                    new Rect(pos.X - largeur / 2 - 5, pos.Y - hauteur / 2 - 5, largeur + 10, hauteur + 10),
                    5
                );
            }

            // Rectangle de la fenêtre
            Rect rectFenetre = new Rect(pos.X - largeur / 2, pos.Y - hauteur / 2, largeur, hauteur);
            context.FillRectangle(brushVitre, rectFenetre, 2);
            context.DrawRectangle(null, new Pen(brushCadre, 2), rectFenetre, 2);

            // Croisillons (diviseur vertical au milieu)
            context.DrawLine(
                new Pen(brushCadre, 1.5),
                new Point(pos.X, pos.Y - hauteur / 2),
                new Point(pos.X, pos.Y + hauteur / 2)
            );

            // Étiquette "FENÊTRE"
            var textFenetre = new FormattedText(
                "FENÊTRE",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial", FontStyle.Normal, FontWeight.Bold),
                9,
                new SolidColorBrush(Color.FromRgb(40, 80, 120))
            );
            context.DrawText(textFenetre, new Point(pos.X - textFenetre.Width / 2, pos.Y + hauteur / 2 + 5));
        }

        /// <summary>
        /// Dessine l'échangeur (rectangle vert avec symbole)
        /// </summary>
        private void DessinerEchangeur(DrawingContext context, Point pos, bool estSelectionne)
        {
            const double largeur = 60;
            const double hauteur = 50;

            // Couleur échangeur
            IBrush brushEchangeur = new SolidColorBrush(Color.FromRgb(40, 167, 69)); // Vert

            // Surbrillance si sélectionné
            if (estSelectionne)
            {
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 4),
                    new Rect(pos.X - largeur / 2 - 5, pos.Y - hauteur / 2 - 5, largeur + 10, hauteur + 10),
                    5
                );
            }

            // Rectangle de l'échangeur
            Rect rectEchangeur = new Rect(pos.X - largeur / 2, pos.Y - hauteur / 2, largeur, hauteur);
            context.FillRectangle(brushEchangeur, rectEchangeur, 5);
            context.DrawRectangle(null, new Pen(Brushes.White, 2), rectEchangeur, 5);

            // Symbole échange (flèches)
            var typefaceBold = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var textEch = new FormattedText(
                "⇄",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typefaceBold,
                20,
                Brushes.White);
            context.DrawText(textEch, new Point(pos.X - textEch.Width / 2, pos.Y - textEch.Height / 2 - 5));

            // Étiquette "ÉCHANGEUR"
            var textLabel = new FormattedText(
                "ÉCHANGEUR",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial", FontStyle.Normal, FontWeight.Bold),
                8,
                Brushes.White);
            context.DrawText(textLabel, new Point(pos.X - textLabel.Width / 2, pos.Y + 10));
        }

        /// <summary>
        /// Dessine un plenum (carré avec label)
        /// </summary>
        private void DessinerPlenum(DrawingContext context, Point pos, BoucheVMC bouche, bool estExtraction, bool estSelectionne)
        {
            const double taille = 50;

            // Couleur selon le type
            IBrush brushPlenum = estExtraction 
                ? new SolidColorBrush(Color.FromRgb(180, 30, 40))  // Rouge foncé pour extraction
                : new SolidColorBrush(Color.FromRgb(30, 80, 200)); // Bleu foncé pour insufflation

            // Surbrillance si sélectionné
            if (estSelectionne)
            {
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 4),
                    new Rect(pos.X - taille / 2 - 5, pos.Y - taille / 2 - 5, taille + 10, taille + 10),
                    5
                );
            }

            // Rectangle du plenum
            Rect rectPlenum = new Rect(pos.X - taille / 2, pos.Y - taille / 2, taille, taille);
            context.FillRectangle(brushPlenum, rectPlenum, 5);
            context.DrawRectangle(null, new Pen(Brushes.White, 2), rectPlenum, 5);

            // Symbole
            var typefaceBold = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var textSymb = new FormattedText(
                estExtraction ? "PE" : "PI",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typefaceBold,
                16,
                Brushes.White);
            context.DrawText(textSymb, new Point(pos.X - textSymb.Width / 2, pos.Y - textSymb.Height / 2 - 5));

            // Étiquette
            var textLabel = new FormattedText(
                estExtraction ? "PLENUM\nEXTRACTION" : "PLENUM\nINSUFFLATION",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Arial", FontStyle.Normal, FontWeight.Bold),
                7,
                Brushes.White);
            context.DrawText(textLabel, new Point(pos.X - textLabel.Width / 2, pos.Y + 8));
        }

        /// <summary>
        /// Dessine une bouche VMC circulaire (extraction ou insufflation)
        /// </summary>
        private void DessinerBoucheCirculaire(DrawingContext context, Point pos, BoucheVMC bouche, 
            Typeface typeface, Typeface typefaceDetail)
        {
            IBrush brush = GetBrushByType(bouche.Type);

            if (bouche == _boucheSelectionnee)
            {
                context.DrawEllipse(
                    null,
                    new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 6),
                    pos,
                    28, 28
                );

                context.DrawEllipse(
                    new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    null,
                    pos,
                    34,
                    34
                );
            }

            Pen borderPen = (bouche == _boucheSelectionnee)
                ? new Pen(Brushes.White, 2)
                : new Pen(new SolidColorBrush(Color.FromRgb(250, 250, 250)), 2);

            double rayon = 25;
            context.DrawEllipse(brush, borderPen, pos, rayon, rayon);

            context.DrawEllipse(
                null,
                new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 1),
                pos,
                rayon, rayon);

            var textNom = new FormattedText(
                bouche.Nom.ToUpper(),
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                9,
                Brushes.White);

            context.DrawText(textNom, new Point(pos.X - textNom.Width / 2, pos.Y - 15));

            string details = $"Ø{bouche.Diametre}mm\n{bouche.Debit}m³/h";
            var textDetails = new FormattedText(
                details,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typefaceDetail,
                8,
                Brushes.White);

            context.DrawText(textDetails, new Point(pos.X - textDetails.Width / 2, pos.Y - 3));
        }

        private void DessinerTitre(DrawingContext context)
        {
            if (_etageActif == null) return;

            var typeface = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);

            var text = new FormattedText(
                $"VUE DE DESSUS - {_etageActif.Nom}",
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                16,
                TitleBrush);

            var paddingX = 14.0;
            var paddingY = 8.0;
            var rect = new Rect(
                (Bounds.Width - text.Width) / 2 - paddingX,
                6,
                text.Width + paddingX * 2,
                text.Height + paddingY * 2
            );

            context.FillRectangle(new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)), rect, 10);
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 1), rect, 10);

            context.DrawText(text, new Point((Bounds.Width - text.Width) / 2, 6 + paddingY));
        }

        private static IBrush GetBrushByType(TypeBouche type)
        {
            return type switch
            {
                TypeBouche.Extraction => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
                TypeBouche.Insufflation => new SolidColorBrush(Color.FromRgb(13, 110, 253)),
                TypeBouche.Porte => new SolidColorBrush(Color.FromRgb(139, 69, 19)),
                TypeBouche.Fenetre => new SolidColorBrush(Color.FromRgb(70, 130, 180)),
                TypeBouche.Echangeur => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
                TypeBouche.PlenumExtraction => new SolidColorBrush(Color.FromRgb(180, 30, 40)),
                TypeBouche.PlenumInsufflation => new SolidColorBrush(Color.FromRgb(30, 80, 200)),
                _ => new SolidColorBrush(Color.FromRgb(120, 120, 120))
            };
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);

            // ✅ IMPORTANT: sans ça, Delete/Backspace ne marche pas
            Focus();

            var point = e.GetPosition(this);

            // ✅ Clic droit => supprimer mur sélectionné
            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (_modeActuel == ModeEdition.Selection)
                {
                    if (SelectionnerMur(point))
                    {
                        SupprimerMurSelectionne();
                        e.Handled = true;
                        return;
                    }
                }
            }

            if (_modeActuel == ModeEdition.AjouterBouche)
            {
                AjouterBoucheAPosition(point);
                return;
            }

            if (_modeActuel == ModeEdition.Selection)
            {
                // 1) Essayer de sélectionner un mur
                if (SelectionnerMur(point))
                {
                    // si mur sélectionné, on stoppe (pas de sélection bouche)
                    e.Handled = true;
                    return;
                }

                // 2) Sinon sélectionner une bouche
                _murSelectionne = null;
                SelectionnerBouche(point);
                return;
            }

            if (_modeActuel == ModeEdition.TracerMur)
            {
                _murSelectionne = null;
                _boucheSelectionnee = null;

                if (_pointDebutMur == null)
                {
                    _pointDebutMur = point;
                }
                else
                {
                    AjouterMur(_pointDebutMur.Value, point);
                    _pointDebutMur = null;
                }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // ✅ Supprimer mur sélectionné avec Delete / Backspace
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if (_murSelectionne != null)
                {
                    SupprimerMurSelectionne();
                    e.Handled = true;
                    return;
                }
            }

            // (optionnel) Escape pour désélectionner
            if (e.Key == Key.Escape)
            {
                _murSelectionne = null;
                _boucheSelectionnee = null;
                InvalidateVisual();
                e.Handled = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);

            if (_estEnDeplacement && _boucheSelectionnee != null && _projet != null)
            {
                var point = e.GetPosition(this);
                double echelle = _projet.Echelle;

                _boucheSelectionnee.Position.X = (point.X - OFFSET_X) / echelle;
                _boucheSelectionnee.Position.Y = (point.Y - OFFSET_Y) / echelle;

                InvalidateVisual();
                ProjetModifie?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _estEnDeplacement = false;
        }

        private void AjouterBoucheAPosition(Point screenPoint)
        {
            if (_projet == null || _etageActif == null) return;

            double echelle = _projet.Echelle;
            var position = new Point2D(
                (screenPoint.X - OFFSET_X) / echelle,
                (screenPoint.Y - OFFSET_Y) / echelle
            );

            // Gérer les éléments uniques
            if (_typeBoucheAjouter == TypeBouche.Echangeur)
            {
                if (_projet.Echangeur != null)
                {
                    // Un échangeur existe déjà, on le déplace ou on affiche un message
                    return;
                }
                var echangeur = new BoucheVMC
                {
                    Nom = "Échangeur",
                    Type = TypeBouche.Echangeur,
                    Position = position,
                    Diametre = 0,
                    Debit = 0
                };
                _projet.DefinirEchangeur(echangeur, _etageActif.Id);
            }
            else if (_typeBoucheAjouter == TypeBouche.PlenumExtraction)
            {
                if (_projet.PlenumExtraction != null)
                {
                    return;
                }
                var plenum = new BoucheVMC
                {
                    Nom = "Plenum Extraction",
                    Type = TypeBouche.PlenumExtraction,
                    Position = position,
                    Diametre = 0,
                    Debit = 0
                };
                _projet.DefinirPlenumExtraction(plenum, _etageActif.Id);
            }
            else if (_typeBoucheAjouter == TypeBouche.PlenumInsufflation)
            {
                if (_projet.PlenumInsufflation != null)
                {
                    return;
                }
                var plenum = new BoucheVMC
                {
                    Nom = "Plenum Insufflation",
                    Type = TypeBouche.PlenumInsufflation,
                    Position = position,
                    Diametre = 0,
                    Debit = 0
                };
                _projet.DefinirPlenumInsufflation(plenum, _etageActif.Id);
            }
            else
            {
                // Bouche normale
                var bouche = new BoucheVMC
                {
                    Nom = _typeBoucheAjouter.ToString(),
                    Type = _typeBoucheAjouter,
                    Position = position,
                    Diametre = _typeBoucheAjouter == TypeBouche.Extraction ? 125 : 150,
                    Debit = _typeBoucheAjouter == TypeBouche.Extraction ? 45 : 30
                };
                _etageActif.AjouterBouche(bouche);
            }

            InvalidateVisual();
            ProjetModifie?.Invoke(this, EventArgs.Empty);
        }

        private void SelectionnerBouche(Point screenPoint)
        {
            if (_projet == null || _etageActif == null) return;

            double echelle = _projet.Echelle;
            _boucheSelectionnee = null;

            foreach (var bouche in _etageActif.Bouches)
            {
                Point posBouche = new Point(
                    OFFSET_X + bouche.Position.X * echelle,
                    OFFSET_Y + bouche.Position.Y * echelle);

                double distance = Math.Sqrt(
                    Math.Pow(screenPoint.X - posBouche.X, 2) +
                    Math.Pow(screenPoint.Y - posBouche.Y, 2));

                // Zone de sélection adaptée au type
                double rayonSelection = (bouche.Type == TypeBouche.Porte || bouche.Type == TypeBouche.Fenetre) ? 30 : 25;

                if (distance < rayonSelection)
                {
                    _boucheSelectionnee = bouche;
                    _estEnDeplacement = true;
                    break;
                }
            }

            BoucheSelectionneeChanged?.Invoke(this, _boucheSelectionnee);
            InvalidateVisual();
        }

        private void AjouterMur(Point debut, Point fin)
        {
            if (_projet == null || _etageActif == null) return;

            double echelle = _projet.Echelle;
            var mur = new Mur
            {
                Debut = new Point2D((debut.X - OFFSET_X) / echelle, (debut.Y - OFFSET_Y) / echelle),
                Fin = new Point2D((fin.X - OFFSET_X) / echelle, (fin.Y - OFFSET_Y) / echelle),
                EstExterieur = false
            };

            _etageActif.AjouterMur(mur);
            InvalidateVisual();
            ProjetModifie?.Invoke(this, EventArgs.Empty);
        }

        // ✅ Sélection mur (clic près d'un segment)
        private bool SelectionnerMur(Point screenPoint)
        {
            if (_projet == null || _etageActif == null)
                return false;

            double echelle = _projet.Echelle;

            foreach (var mur in _etageActif.Murs)
            {
                Point a = new Point(
                    OFFSET_X + mur.Debut.X * echelle,
                    OFFSET_Y + mur.Debut.Y * echelle);

                Point b = new Point(
                    OFFSET_X + mur.Fin.X * echelle,
                    OFFSET_Y + mur.Fin.Y * echelle);

                if (DistancePointSegment(screenPoint, a, b) < 6)
                {
                    _murSelectionne = mur;
                    _boucheSelectionnee = null;
                    BoucheSelectionneeChanged?.Invoke(this, null);
                    InvalidateVisual();
                    return true;
                }
            }

            _murSelectionne = null;
            return false;
        }

        private static double DistancePointSegment(Point p, Point a, Point b)
        {
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;

            if (dx == 0 && dy == 0)
                return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));

            double t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy);
            t = Math.Clamp(t, 0, 1);

            double projX = a.X + t * dx;
            double projY = a.Y + t * dy;

            return Math.Sqrt(Math.Pow(p.X - projX, 2) + Math.Pow(p.Y - projY, 2));
        }

        public void SupprimerMurSelectionne()
        {
            if (_murSelectionne != null && _etageActif != null)
            {
                _etageActif.Murs.Remove(_murSelectionne);
                _murSelectionne = null;
                InvalidateVisual();
                ProjetModifie?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SupprimerBoucheSelectionnee()
        {
            if (_boucheSelectionnee != null && _etageActif != null)
            {
                _etageActif.SupprimerBouche(_boucheSelectionnee.Id);
                _boucheSelectionnee = null;
                InvalidateVisual();
                ProjetModifie?.Invoke(this, EventArgs.Empty);
            }
        }

        public BoucheVMC? GetBoucheSelectionnee() => _boucheSelectionnee;
    }
}