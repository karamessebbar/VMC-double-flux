using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using VMCDoubleFlux.Models;

namespace VMCDoubleFlux.Controls
{
    /// <summary>
    /// Contrôle personnalisé pour afficher et éditer le plan de l'étage.
    /// Affiche le débit effectif calculé par le moteur sur chaque bouche,
    /// ainsi qu'un badge d'alerte si le système est déséquilibré.
    /// </summary>
    public class VueGraphique : Control
    {
        private const double OFFSET_X = 50;
        private const double OFFSET_Y = 50;

        private ProjetVMC? _projet;
        private Etage? _etageActif;

        private Mur?      _murSelectionne;
        private BoucheVMC? _boucheSelectionnee;
        private bool _estEnDeplacement;

        // ── Allocations reçues du moteur ─────────────────────────
        /// <summary>
        /// IdBouche → débit effectif calculé par le moteur.
        /// Null = pas encore calculé (affichage débit nominal).
        /// </summary>
        private Dictionary<string, double>? _debitsEffectifs;

        /// <summary>
        /// true si le système est déséquilibré (affichage badge alerte).
        /// </summary>
        private bool _systemeDesequilibre;

        // Mode d'édition
        public enum ModeEdition
        {
            Selection,
            AjouterBouche,
            TracerMur,
            AjouterPorte
        }

        private ModeEdition _modeActuel = ModeEdition.Selection;
        private TypeBouche  _typeBoucheAjouter = TypeBouche.Extraction;
        private Point?      _pointDebutMur;

        // 🎨 Palette
        private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromRgb(248, 249, 250));
        private static readonly IBrush GridMinorBrush  = new SolidColorBrush(Color.FromRgb(236, 236, 236));
        private static readonly IBrush GridMajorBrush  = new SolidColorBrush(Color.FromRgb(214, 214, 214));
        private static readonly IBrush TitleBrush      = new SolidColorBrush(Color.FromRgb(44, 62, 80));

        private static readonly Pen SelectedWallPen =
            new Pen(new SolidColorBrush(Color.FromRgb(255, 193, 7)), 5, lineCap: PenLineCap.Round);

        // Événements
        public event EventHandler<BoucheVMC?>? BoucheSelectionneeChanged;
        public event EventHandler? ProjetModifie;

        public VueGraphique()
        {
            ClipToBounds = true;
            Focusable    = true;
        }

        // ── API publique ─────────────────────────────────────────

        public void SetProjet(ProjetVMC projet)
        {
            _projet  = projet;
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
            if (typeBouche.HasValue) _typeBoucheAjouter = typeBouche.Value;
            _boucheSelectionnee = null;
            _murSelectionne = null;
            InvalidateVisual();
        }

        /// <summary>
        /// Met à jour les débits effectifs calculés par le moteur
        /// et déclenche un nouveau rendu.
        /// </summary>
        public void SetDebitsEffectifs(Dictionary<string, double>? debits, bool desequilibre)
        {
            _debitsEffectifs     = debits;
            _systemeDesequilibre = desequilibre;
            InvalidateVisual();
        }

        public BoucheVMC? GetBoucheSelectionnee() => _boucheSelectionnee;

        // ── Rendu ────────────────────────────────────────────────

        public override void Render(DrawingContext context)
        {
            base.Render(context);
            if (_projet == null || _etageActif == null) return;

            context.FillRectangle(BackgroundBrush, new Rect(0, 0, Bounds.Width, Bounds.Height));

            double echelle = _projet.Echelle;

            DessinerGrille(context, echelle, OFFSET_X, OFFSET_Y);
            DessinerEchelles(context, echelle, OFFSET_X, OFFSET_Y);
            DessinerMurs(context, echelle, OFFSET_X, OFFSET_Y);
            DessinerBouches(context, echelle, OFFSET_X, OFFSET_Y);
            DessinerTitre(context);

            // Badge de déséquilibre global (coin bas-gauche de la vue)
            if (_systemeDesequilibre)
                DessinerBadgeDesequilibre(context);
        }

        // ── Grille ───────────────────────────────────────────────

        private void DessinerGrille(DrawingContext context, double echelle, double offsetX, double offsetY)
        {
            if (_etageActif == null) return;

            const double pas = 2.0;
            for (double x = 0; x <= _etageActif.Largeur; x += pas)
            {
                bool maj = ((int)Math.Round(x)) % 10 == 0;
                var pen = new Pen(maj ? GridMajorBrush : GridMinorBrush, maj ? 1.2 : 0.7);
                double sx = offsetX + x * echelle;
                context.DrawLine(pen, new Point(sx, offsetY), new Point(sx, offsetY + _etageActif.Hauteur * echelle));
            }
            for (double y = 0; y <= _etageActif.Hauteur; y += pas)
            {
                bool maj = ((int)Math.Round(y)) % 10 == 0;
                var pen = new Pen(maj ? GridMajorBrush : GridMinorBrush, maj ? 1.2 : 0.7);
                double sy = offsetY + y * echelle;
                context.DrawLine(pen, new Point(offsetX, sy), new Point(offsetX + _etageActif.Largeur * echelle, sy));
            }
            context.DrawRectangle(null,
                new Pen(new SolidColorBrush(Color.FromRgb(190, 190, 190)), 1),
                new Rect(offsetX, offsetY, _etageActif.Largeur * echelle, _etageActif.Hauteur * echelle));
        }

        private void DessinerEchelles(DrawingContext context, double echelle, double offsetX, double offsetY)
        {
            if (_etageActif == null) return;
            var brush = new SolidColorBrush(Color.FromRgb(90, 90, 90));
            var tf = new Typeface("Arial");

            for (double x = 0; x <= _etageActif.Largeur; x += 2.0)
            {
                double sx = offsetX + x * echelle;
                var t = new FormattedText($"{x:0}m", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 10, brush);
                context.DrawText(t, new Point(sx - t.Width / 2, offsetY - 20));
            }
            for (double y = 0; y <= _etageActif.Hauteur; y += 2.0)
            {
                double sy = offsetY + y * echelle;
                var t = new FormattedText($"{y:0}m", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 10, brush);
                context.DrawText(t, new Point(offsetX - 35, sy - t.Height / 2));
            }
        }

        // ── Murs ─────────────────────────────────────────────────

        private void DessinerMurs(DrawingContext context, double echelle, double offsetX, double offsetY)
        {
            if (_etageActif == null) return;
            foreach (var mur in _etageActif.Murs)
            {
                var pen = mur.EstExterieur
                    ? new Pen(new SolidColorBrush(Color.FromRgb(30, 30, 30)), 3)
                    : new Pen(new SolidColorBrush(Color.FromRgb(90, 90, 90)), 2);

                var debut = new Point(offsetX + mur.Debut.X * echelle, offsetY + mur.Debut.Y * echelle);
                var fin   = new Point(offsetX + mur.Fin.X   * echelle, offsetY + mur.Fin.Y   * echelle);

                if (_murSelectionne == mur)
                    context.DrawLine(SelectedWallPen, debut, fin);
                context.DrawLine(pen, debut, fin);
            }
        }

        // ── Bouches ──────────────────────────────────────────────

        private void DessinerBouches(DrawingContext context, double echelle, double offsetX, double offsetY)
        {
            if (_etageActif == null) return;

            var tfBold   = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var tfNormal = new Typeface("Arial");

            foreach (var bouche in _etageActif.Bouches)
            {
                var pos = new Point(offsetX + bouche.Position.X * echelle, offsetY + bouche.Position.Y * echelle);
                switch (bouche.Type)
                {
                    case TypeBouche.Porte:
                        DessinerPorte(context, pos, bouche == _boucheSelectionnee, bouche.Rotation);
                        break;
                    case TypeBouche.Fenetre:
                        DessinerFenetre(context, pos, bouche == _boucheSelectionnee, bouche.Rotation);
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
                    default:
                        DessinerBoucheCirculaire(context, pos, bouche, tfBold, tfNormal);
                        break;
                }
            }

            // Éléments uniques de l'étage actif
            if (_projet != null)
            {
                if (_projet.Echangeur != null && _projet.EchangeurEtageId == _etageActif.Id)
                {
                    var p = new Point(offsetX + _projet.Echangeur.Position.X * echelle, offsetY + _projet.Echangeur.Position.Y * echelle);
                    DessinerEchangeur(context, p, _projet.Echangeur == _boucheSelectionnee);
                }
                if (_projet.PlenumExtraction != null && _projet.PlenumExtractionEtageId == _etageActif.Id)
                {
                    var p = new Point(offsetX + _projet.PlenumExtraction.Position.X * echelle, offsetY + _projet.PlenumExtraction.Position.Y * echelle);
                    DessinerPlenum(context, p, _projet.PlenumExtraction, true, _projet.PlenumExtraction == _boucheSelectionnee);
                }
                if (_projet.PlenumInsufflation != null && _projet.PlenumInsufflationEtageId == _etageActif.Id)
                {
                    var p = new Point(offsetX + _projet.PlenumInsufflation.Position.X * echelle, offsetY + _projet.PlenumInsufflation.Position.Y * echelle);
                    DessinerPlenum(context, p, _projet.PlenumInsufflation, false, _projet.PlenumInsufflation == _boucheSelectionnee);
                }
            }
        }

        // ── Bouche circulaire (extraction / insufflation) ─────────

        private void DessinerBoucheCirculaire(DrawingContext context, Point pos, BoucheVMC bouche,
            Typeface typeface, Typeface typefaceDetail)
        {
            IBrush brush = GetBrushByType(bouche.Type);

            // Surbrillance si sélectionnée
            if (bouche == _boucheSelectionnee)
            {
                context.DrawEllipse(null,
                    new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 6), pos, 28, 28);
                context.DrawEllipse(new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    null, pos, 34, 34);
            }

            // Cercle principal
            double rayon = 25;
            context.DrawEllipse(brush,
                new Pen(new SolidColorBrush(Color.FromRgb(250, 250, 250)), 2), pos, rayon, rayon);
            context.DrawEllipse(null,
                new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 1), pos, rayon, rayon);

            // Indicateur débit fixe : petit cadenas en haut à droite
            if (bouche.DebitFixe)
            {
                var tfSmall = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
                var lockText = new FormattedText("🔒", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, tfSmall, 11,
                    new SolidColorBrush(Color.FromRgb(255, 220, 0)));
                context.DrawText(lockText, new Point(pos.X + 14, pos.Y - 32));
            }

            // Nom
            var textNom = new FormattedText(bouche.Nom.ToUpper(),
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, 9, Brushes.White);
            context.DrawText(textNom, new Point(pos.X - textNom.Width / 2, pos.Y - 15));

            // ── Débit affiché ────────────────────────────────────
            // Si on a des allocations moteur, on utilise le débit effectif ;
            // sinon on affiche le débit nominal de la bouche.
            double debitAffiche = bouche.Debit;
            bool   debitRecalcule = false;
            if (_debitsEffectifs != null && _debitsEffectifs.TryGetValue(bouche.Id, out double deff))
            {
                debitAffiche  = deff;
                debitRecalcule = !bouche.DebitFixe; // les libres sont recalculées
            }

            string detailStr = $"Ø{bouche.Diametre}mm\n{debitAffiche:F1}m³/h";
            IBrush detailColor = Brushes.White;

            // Si déséquilibre ET bouche libre → afficher en orange pour signaler le problème
            if (_systemeDesequilibre && !bouche.DebitFixe)
                detailColor = new SolidColorBrush(Color.FromRgb(255, 160, 0));

            var textDetails = new FormattedText(detailStr,
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typefaceDetail, 8, detailColor);
            context.DrawText(textDetails, new Point(pos.X - textDetails.Width / 2, pos.Y - 3));

            // ── Badge "capacité restante" si déséquilibre ────────
            // Affiché en dessous de la bouche si le système est déséquilibré
            // et que la bouche est libre (elle reçoit un débit partiel).
            if (_systemeDesequilibre && debitRecalcule)
            {
                // Calculer l'écart par rapport au débit nominal
                double ecart = debitAffiche - bouche.Debit;
                string badgeStr = ecart >= 0
                    ? $"+{ecart:F1}"
                    : $"{ecart:F1}";

                IBrush badgeBg = ecart < 0
                    ? new SolidColorBrush(Color.FromArgb(200, 200, 60, 20))   // rouge si sous-alimenté
                    : new SolidColorBrush(Color.FromArgb(200, 40, 160, 40));  // vert si sur-alimenté

                var tfBadge = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
                var badgeText = new FormattedText(badgeStr,
                    CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfBadge, 9, Brushes.White);

                var badgeRect = new Rect(
                    pos.X - badgeText.Width / 2 - 4,
                    pos.Y + rayon + 3,
                    badgeText.Width + 8,
                    badgeText.Height + 2);

                context.FillRectangle(badgeBg, badgeRect, 3);
                context.DrawText(badgeText, new Point(badgeRect.X + 4, badgeRect.Y + 1));
            }
        }

        // ── Badge global déséquilibre ────────────────────────────

        private void DessinerBadgeDesequilibre(DrawingContext context)
        {
            const string msg = "⚠ Déséquilibre ";
            var tf = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var text = new FormattedText(msg,
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 12,
                new SolidColorBrush(Color.FromRgb(255, 240, 180)));

            double px = OFFSET_X + 8;
            double py = Bounds.Height - 36;
            var bg = new Rect(px - 6, py - 4, text.Width + 12, text.Height + 8);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(210, 160, 60, 0)), bg, 6);
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(180, 255, 180, 0)), 1), bg, 6);
            context.DrawText(text, new Point(px, py));
        }

        // ── Porte ────────────────────────────────────────────────

        private void DessinerPorte(DrawingContext context, Point pos, bool estSelectionnee, double rotation = 0)
        {
            const double largeur = 50;
            const double epaisseur = 8;

            var transform = Matrix.CreateTranslation(-pos.X, -pos.Y)
                          * Matrix.CreateRotation(rotation * Math.PI / 180)
                          * Matrix.CreateTranslation(pos.X, pos.Y);

            using (context.PushTransform(transform))
            {
                IBrush brushPorte = new SolidColorBrush(Color.FromRgb(139, 69, 19));

                if (estSelectionnee)
                    context.DrawRectangle(
                        new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                        new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 4),
                        new Rect(pos.X - largeur / 2 - 5, pos.Y - epaisseur / 2 - 5, largeur + 10, epaisseur + 10), 5);

                Rect rectPorte = new Rect(pos.X - largeur / 2, pos.Y - epaisseur / 2, largeur, epaisseur);
                context.FillRectangle(brushPorte, rectPorte, 2);
                context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(80, 40, 10)), 1.5), rectPorte, 2);

                context.DrawEllipse(new SolidColorBrush(Color.FromRgb(212, 175, 55)), null,
                    new Point(pos.X + largeur / 4, pos.Y), 3, 3);

                var arcGeometry = new PathGeometry();
                var arcFigure   = new PathFigure { StartPoint = new Point(pos.X - largeur / 2, pos.Y), IsClosed = false };
                arcFigure.Segments.Add(new ArcSegment
                {
                    Point = new Point(pos.X - largeur / 2, pos.Y - largeur),
                    Size  = new Size(largeur, largeur),
                    SweepDirection = SweepDirection.Clockwise
                });
                arcGeometry.Figures.Add(arcFigure);
                context.DrawGeometry(null,
                    new Pen(new SolidColorBrush(Color.FromRgb(139, 69, 19)), 1, new DashStyle(new double[] { 3, 3 }, 0)),
                    arcGeometry);

                var textPorte = new FormattedText("PORTE", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface("Arial", FontStyle.Normal, FontWeight.Bold), 9,
                    new SolidColorBrush(Color.FromRgb(80, 40, 10)));
                context.DrawText(textPorte, new Point(pos.X - textPorte.Width / 2, pos.Y + epaisseur / 2 + 5));
            }
        }

        // ── Fenêtre ──────────────────────────────────────────────

        private void DessinerFenetre(DrawingContext context, Point pos, bool estSelectionnee, double rotation = 0)
        {
            const double largeur = 45;
            const double hauteur = 10;

            var transform = Matrix.CreateTranslation(-pos.X, -pos.Y)
                          * Matrix.CreateRotation(rotation * Math.PI / 180)
                          * Matrix.CreateTranslation(pos.X, pos.Y);

            using (context.PushTransform(transform))
            {
                IBrush brushCadre = new SolidColorBrush(Color.FromRgb(70, 130, 180));
                IBrush brushVitre = new SolidColorBrush(Color.FromArgb(100, 135, 206, 250));

                if (estSelectionnee)
                    context.DrawRectangle(
                        new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                        new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 4),
                        new Rect(pos.X - largeur / 2 - 5, pos.Y - hauteur / 2 - 5, largeur + 10, hauteur + 10), 5);

                Rect rectFenetre = new Rect(pos.X - largeur / 2, pos.Y - hauteur / 2, largeur, hauteur);
                context.FillRectangle(brushVitre, rectFenetre, 2);
                context.DrawRectangle(null, new Pen(brushCadre, 2), rectFenetre, 2);
                context.DrawLine(new Pen(brushCadre, 1.5),
                    new Point(pos.X, pos.Y - hauteur / 2), new Point(pos.X, pos.Y + hauteur / 2));

                var textFenetre = new FormattedText("FENÊTRE", CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight, new Typeface("Arial", FontStyle.Normal, FontWeight.Bold), 9,
                    new SolidColorBrush(Color.FromRgb(40, 80, 120)));
                context.DrawText(textFenetre, new Point(pos.X - textFenetre.Width / 2, pos.Y + hauteur / 2 + 5));
            }
        }

        // ── Échangeur ────────────────────────────────────────────

        private void DessinerEchangeur(DrawingContext context, Point pos, bool estSelectionne)
        {
            const double largeur = 60;
            const double hauteur = 50;

            IBrush brush = new SolidColorBrush(Color.FromRgb(40, 167, 69));

            if (estSelectionne)
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 4),
                    new Rect(pos.X - largeur / 2 - 5, pos.Y - hauteur / 2 - 5, largeur + 10, hauteur + 10), 5);

            Rect rect = new Rect(pos.X - largeur / 2, pos.Y - hauteur / 2, largeur, hauteur);
            context.FillRectangle(brush, rect, 5);
            context.DrawRectangle(null, new Pen(Brushes.White, 2), rect, 5);

            var tfBold = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var textSym = new FormattedText("⇄", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, tfBold, 20, Brushes.White);
            context.DrawText(textSym, new Point(pos.X - textSym.Width / 2, pos.Y - textSym.Height / 2 - 5));

            var textLabel = new FormattedText("ÉCHANGEUR", CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, new Typeface("Arial", FontStyle.Normal, FontWeight.Bold), 8, Brushes.White);
            context.DrawText(textLabel, new Point(pos.X - textLabel.Width / 2, pos.Y + 10));
        }

        // ── Plenum ───────────────────────────────────────────────

        private void DessinerPlenum(DrawingContext context, Point pos, BoucheVMC bouche,
            bool estExtraction, bool estSelectionne)
        {
            const double taille = 50;

            IBrush brush = estExtraction
                ? new SolidColorBrush(Color.FromRgb(180, 30, 40))
                : new SolidColorBrush(Color.FromRgb(30, 80, 200));

            if (estSelectionne)
                context.DrawRectangle(
                    new SolidColorBrush(Color.FromArgb(50, 255, 215, 0)),
                    new Pen(new SolidColorBrush(Color.FromArgb(160, 255, 215, 0)), 4),
                    new Rect(pos.X - taille / 2 - 5, pos.Y - taille / 2 - 5, taille + 10, taille + 10), 5);

            Rect rect = new Rect(pos.X - taille / 2, pos.Y - taille / 2, taille, taille);
            context.FillRectangle(brush, rect, 5);
            context.DrawRectangle(null, new Pen(Brushes.White, 2), rect, 5);

            var tfBold = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var textSymb = new FormattedText(estExtraction ? "PE" : "PI",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tfBold, 16, Brushes.White);
            context.DrawText(textSymb, new Point(pos.X - textSymb.Width / 2, pos.Y - textSymb.Height / 2 - 5));

            var textLabel = new FormattedText(
                estExtraction ? "PLENUM\nEXTRACTION" : "PLENUM\nINSUFFLATION",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface("Arial", FontStyle.Normal, FontWeight.Bold), 7, Brushes.White);
            context.DrawText(textLabel, new Point(pos.X - textLabel.Width / 2, pos.Y + 8));
        }

        // ── Titre ────────────────────────────────────────────────

        private void DessinerTitre(DrawingContext context)
        {
            if (_etageActif == null) return;
            var tf   = new Typeface("Arial", FontStyle.Normal, FontWeight.Bold);
            var text = new FormattedText($"VUE DE DESSUS - {_etageActif.Nom}",
                CultureInfo.CurrentCulture, FlowDirection.LeftToRight, tf, 16, TitleBrush);

            double px = 14, py = 8;
            var rect = new Rect((Bounds.Width - text.Width) / 2 - px, 6,
                text.Width + px * 2, text.Height + py * 2);
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(170, 255, 255, 255)), rect, 10);
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)), 1), rect, 10);
            context.DrawText(text, new Point((Bounds.Width - text.Width) / 2, 6 + py));
        }

        // ── Helpers ──────────────────────────────────────────────

        private static IBrush GetBrushByType(TypeBouche type) => type switch
        {
            TypeBouche.Extraction         => new SolidColorBrush(Color.FromRgb(220, 53, 69)),
            TypeBouche.Insufflation       => new SolidColorBrush(Color.FromRgb(13, 110, 253)),
            TypeBouche.Porte              => new SolidColorBrush(Color.FromRgb(139, 69, 19)),
            TypeBouche.Fenetre            => new SolidColorBrush(Color.FromRgb(70, 130, 180)),
            TypeBouche.Echangeur          => new SolidColorBrush(Color.FromRgb(40, 167, 69)),
            TypeBouche.PlenumExtraction   => new SolidColorBrush(Color.FromRgb(180, 30, 40)),
            TypeBouche.PlenumInsufflation => new SolidColorBrush(Color.FromRgb(30, 80, 200)),
            _                             => new SolidColorBrush(Color.FromRgb(120, 120, 120))
        };

        // ── Entrées souris ────────────────────────────────────────

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();

            var point = e.GetPosition(this);

            if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            {
                if (_modeActuel == ModeEdition.Selection)
                {
                    if (_projet != null && _etageActif != null)
                    {
                        double echelle = _projet.Echelle;
                        foreach (var bouche in _etageActif.Bouches)
                        {
                            if (bouche.Type != TypeBouche.Porte && bouche.Type != TypeBouche.Fenetre) continue;
                            var pb = new Point(OFFSET_X + bouche.Position.X * echelle, OFFSET_Y + bouche.Position.Y * echelle);
                            if (Math.Sqrt(Math.Pow(point.X - pb.X, 2) + Math.Pow(point.Y - pb.Y, 2)) < 35)
                            {
                                bouche.Rotation = (bouche.Rotation + 90) % 360;
                                InvalidateVisual();
                                e.Handled = true;
                                return;
                            }
                        }
                    }
                    if (SelectionnerMur(point)) { SupprimerMurSelectionne(); e.Handled = true; return; }
                }
            }

            if (_modeActuel == ModeEdition.AjouterBouche) { AjouterBoucheAPosition(point); return; }

            if (_modeActuel == ModeEdition.Selection)
            {
                if (SelectionnerMur(point)) { e.Handled = true; return; }
                _murSelectionne = null;
                SelectionnerBouche(point);
                return;
            }

            if (_modeActuel == ModeEdition.TracerMur)
            {
                _murSelectionne = null;
                _boucheSelectionnee = null;
                if (_pointDebutMur == null) _pointDebutMur = point;
                else { AjouterMur(_pointDebutMur.Value, point); _pointDebutMur = null; }
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                if (_murSelectionne != null) { SupprimerMurSelectionne(); e.Handled = true; return; }
            }
            if (e.Key == Key.Escape)
            {
                _murSelectionne = null;
                _boucheSelectionnee = null;
                BoucheSelectionneeChanged?.Invoke(this, null);
                InvalidateVisual();
                e.Handled = true;
            }
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_estEnDeplacement && _boucheSelectionnee != null && _projet != null)
            {
                var pt = e.GetPosition(this);
                _boucheSelectionnee.Position.X = (pt.X - OFFSET_X) / _projet.Echelle;
                _boucheSelectionnee.Position.Y = (pt.Y - OFFSET_Y) / _projet.Echelle;
                InvalidateVisual();
                ProjetModifie?.Invoke(this, EventArgs.Empty);
            }
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            _estEnDeplacement = false;
        }

        // ── Helpers internes ─────────────────────────────────────

        private void AjouterBoucheAPosition(Point screenPoint)
        {
            if (_projet == null || _etageActif == null) return;
            double echelle = _projet.Echelle;
            var position = new Point2D((screenPoint.X - OFFSET_X) / echelle, (screenPoint.Y - OFFSET_Y) / echelle);

            if (_typeBoucheAjouter == TypeBouche.Echangeur)
            {
                if (_projet.Echangeur != null) return;
                _projet.DefinirEchangeur(new BoucheVMC { Nom = "Échangeur", Type = TypeBouche.Echangeur, Position = position, Diametre = 0, Debit = 0 }, _etageActif.Id);
            }
            else if (_typeBoucheAjouter == TypeBouche.PlenumExtraction)
            {
                if (_projet.PlenumExtraction != null) return;
                _projet.DefinirPlenumExtraction(new BoucheVMC { Nom = "Plenum Extraction", Type = TypeBouche.PlenumExtraction, Position = position, Diametre = 0, Debit = 0 }, _etageActif.Id);
            }
            else if (_typeBoucheAjouter == TypeBouche.PlenumInsufflation)
            {
                if (_projet.PlenumInsufflation != null) return;
                _projet.DefinirPlenumInsufflation(new BoucheVMC { Nom = "Plenum Insufflation", Type = TypeBouche.PlenumInsufflation, Position = position, Diametre = 0, Debit = 0 }, _etageActif.Id);
            }
            else
            {
                _etageActif.AjouterBouche(new BoucheVMC
                {
                    Nom = _typeBoucheAjouter.ToString(),
                    Type = _typeBoucheAjouter,
                    Position = position,
                    Diametre = _typeBoucheAjouter == TypeBouche.Extraction ? 125 : 150,
                    Debit = _typeBoucheAjouter == TypeBouche.Extraction ? 45 : 30
                });
            }
            InvalidateVisual();
            ProjetModifie?.Invoke(this, EventArgs.Empty);
        }

        private void SelectionnerBouche(Point screenPoint)
        {
            if (_projet == null || _etageActif == null) return;
            double echelle = _projet.Echelle;
            _boucheSelectionnee = null;

            // Bouches normales de l'étage
            foreach (var bouche in _etageActif.Bouches)
            {
                var pb = new Point(OFFSET_X + bouche.Position.X * echelle, OFFSET_Y + bouche.Position.Y * echelle);
                double dist = Math.Sqrt(Math.Pow(screenPoint.X - pb.X, 2) + Math.Pow(screenPoint.Y - pb.Y, 2));
                double rayon = (bouche.Type == TypeBouche.Porte || bouche.Type == TypeBouche.Fenetre) ? 30 : 25;
                if (dist < rayon) { _boucheSelectionnee = bouche; _estEnDeplacement = true; break; }
            }

            // Échangeur et plenums (éléments globaux affichés sur l'étage actif)
            if (_boucheSelectionnee == null)
            {
                var globaux = new[]
                {
                    (_projet.Echangeur,          _projet.EchangeurEtageId),
                    (_projet.PlenumExtraction,    _projet.PlenumExtractionEtageId),
                    (_projet.PlenumInsufflation,  _projet.PlenumInsufflationEtageId),
                };
                foreach (var (bouche, etageId) in globaux)
                {
                    if (bouche == null || etageId != _etageActif.Id) continue;
                    var pb = new Point(OFFSET_X + bouche.Position.X * echelle, OFFSET_Y + bouche.Position.Y * echelle);
                    double dist = Math.Sqrt(Math.Pow(screenPoint.X - pb.X, 2) + Math.Pow(screenPoint.Y - pb.Y, 2));
                    if (dist < 35) { _boucheSelectionnee = bouche; _estEnDeplacement = true; break; }
                }
            }

            BoucheSelectionneeChanged?.Invoke(this, _boucheSelectionnee);
            InvalidateVisual();
        }

        private void AjouterMur(Point debut, Point fin)
        {
            if (_projet == null || _etageActif == null) return;
            double echelle = _projet.Echelle;
            _etageActif.AjouterMur(new Mur
            {
                Debut = new Point2D((debut.X - OFFSET_X) / echelle, (debut.Y - OFFSET_Y) / echelle),
                Fin   = new Point2D((fin.X   - OFFSET_X) / echelle, (fin.Y   - OFFSET_Y) / echelle),
                EstExterieur = false
            });
            InvalidateVisual();
            ProjetModifie?.Invoke(this, EventArgs.Empty);
        }

        private bool SelectionnerMur(Point screenPoint)
        {
            if (_projet == null || _etageActif == null) return false;
            double echelle = _projet.Echelle;
            foreach (var mur in _etageActif.Murs)
            {
                var a = new Point(OFFSET_X + mur.Debut.X * echelle, OFFSET_Y + mur.Debut.Y * echelle);
                var b = new Point(OFFSET_X + mur.Fin.X   * echelle, OFFSET_Y + mur.Fin.Y   * echelle);
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
            double dx = b.X - a.X, dy = b.Y - a.Y;
            if (dx == 0 && dy == 0)
                return Math.Sqrt(Math.Pow(p.X - a.X, 2) + Math.Pow(p.Y - a.Y, 2));
            double t = Math.Clamp(((p.X - a.X) * dx + (p.Y - a.Y) * dy) / (dx * dx + dy * dy), 0, 1);
            return Math.Sqrt(Math.Pow(p.X - (a.X + t * dx), 2) + Math.Pow(p.Y - (a.Y + t * dy), 2));
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
            if (_boucheSelectionnee == null || _etageActif == null || _projet == null) return;

            // Éléments globaux (échangeur, plenums)
            if (_boucheSelectionnee == _projet.Echangeur)
                { _projet.Echangeur = null; _projet.EchangeurEtageId = null; }
            else if (_boucheSelectionnee == _projet.PlenumExtraction)
                { _projet.PlenumExtraction = null; _projet.PlenumExtractionEtageId = null; }
            else if (_boucheSelectionnee == _projet.PlenumInsufflation)
                { _projet.PlenumInsufflation = null; _projet.PlenumInsufflationEtageId = null; }
            else
                _etageActif.SupprimerBouche(_boucheSelectionnee.Id);

            _boucheSelectionnee = null;
            InvalidateVisual();
            ProjetModifie?.Invoke(this, EventArgs.Empty);
        }
    }
}
