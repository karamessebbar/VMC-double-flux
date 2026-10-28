using System;
using System.Collections.Generic;
using System.Linq;

namespace VMCDoubleFlux.Models
{
    // ─────────────────────────────────────────────────────────────
    //  États possibles après calcul d'un côté
    // ─────────────────────────────────────────────────────────────

    public enum EtatCoteMoteur
    {
        Normal,
        InsufflationLibre,
        ExtractionBloquee,
        DesequilibreImpossible
    }

    // ─────────────────────────────────────────────────────────────
    //  Résultat d'une bouche après calcul
    // ─────────────────────────────────────────────────────────────

    public class AllocBouche
    {
        public BoucheVMC Bouche     { get; set; } = null!;
        public double DebitEffectif { get; set; }
        public bool EstFixe         => Bouche.DebitFixe;
    }

    // ─────────────────────────────────────────────────────────────
    //  Résultat pour un côté
    // ─────────────────────────────────────────────────────────────

    public class ResultatCoteMoteur
    {
        public double DebitFixeTotal   { get; set; }
        public double DebitLibreAlloue { get; set; }
        public double CapaciteMax      { get; set; }

        public double DebitEffectif   => DebitFixeTotal + DebitLibreAlloue;
        public double Depassement     => Math.Max(0, DebitFixeTotal - CapaciteMax);
        public bool   EstDansCapacite => DebitEffectif <= CapaciteMax + 0.01;
        public double TauxCharge      => CapaciteMax > 0 ? DebitEffectif / CapaciteMax * 100.0 : 0;

        public EtatCoteMoteur       Etat        { get; set; }
        public List<AllocBouche>    Allocations { get; set; } = new();

        /// <summary>Accès rapide : IdBouche → débit effectif calculé.</summary>
        public Dictionary<string, double> DebitParId =>
            Allocations.ToDictionary(a => a.Bouche.Id, a => a.DebitEffectif);
    }

    // ─────────────────────────────────────────────────────────────
    //  Résultat complet
    // ─────────────────────────────────────────────────────────────

    public class ResultatMoteur
    {
        public ResultatCoteMoteur Extraction   { get; set; } = new();
        public ResultatCoteMoteur Insufflation { get; set; } = new();

        public bool ToutDansCapacite =>
            Extraction.EstDansCapacite && Insufflation.EstDansCapacite
            && Extraction.Etat   != EtatCoteMoteur.DesequilibreImpossible
            && Insufflation.Etat != EtatCoteMoteur.DesequilibreImpossible;

        /// <summary>IdBouche → débit effectif, tous côtés confondus.</summary>
        public Dictionary<string, double> TousDebitsParId
        {
            get
            {
                var d = new Dictionary<string, double>(Extraction.DebitParId);
                foreach (var kv in Insufflation.DebitParId)
                    d[kv.Key] = kv.Value;
                return d;
            }
        }

        public string StatutGlobal
        {
            get
            {
                var msgs = new List<string>();

                if (Extraction.Etat   == EtatCoteMoteur.DesequilibreImpossible ||
                    Insufflation.Etat == EtatCoteMoteur.DesequilibreImpossible)
                    msgs.Add("🔒 Toutes les bouches sont fixes : équilibre impossible — ajoutez une bouche libre ou modifiez les débits fixes.");

                if (Extraction.Etat == EtatCoteMoteur.ExtractionBloquee)
                    msgs.Add($"⛔ Extraction bloquée  (+{Extraction.Depassement:F1} m³/h)");

                if (Insufflation.Etat == EtatCoteMoteur.InsufflationLibre)
                    msgs.Add($"⛔ Insufflation bloquée  (+{(Insufflation.DebitEffectif - Insufflation.CapaciteMax):F1} m³/h)");

                return msgs.Count == 0
                    ? "✅ Système dans les capacités"
                    : string.Join("\n", msgs);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────
    //  Moteur VMC double flux
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Moteur VMC double flux.
    ///
    /// Règles :
    ///   • Une seule Capacite identique pour les deux côtés.
    ///   • Bouches DebitFixe==true → débit imposé, jamais modifié.
    ///   • Bouches libres → reliquat (Capacite − fixes) distribué
    ///     aléatoirement via la méthode des points de coupe,
    ///     arrondi à 0.1 m³/h, écart d'arrondi corrigé sur la 1re libre.
    ///   • Si deux côtés 100 % fixes avec totaux inégaux → DesequilibreImpossible.
    ///   • Si fixes > Capacite → ExtractionBloquee ou InsufflationLibre.
    /// </summary>
    public class MoteurVMC
    {
        private readonly Random _rng = new Random();

        public double Capacite             { get; set; } = 200.0;
        public double CapaciteExtraction   => Capacite;
        public double CapaciteInsufflation => Capacite;

        // ── Calcul principal ─────────────────────────────────────

        public ResultatMoteur Calculer(
            IReadOnlyList<BoucheVMC> bouchesExtraction,
            IReadOnlyList<BoucheVMC> bouchesInsufflation)
        {
            var extraction   = CalculerCote(bouchesExtraction,   estExtraction: true);
            var insufflation = CalculerCote(bouchesInsufflation, estExtraction: false);

            // Déséquilibre impossible : les DEUX côtés sont 100 % fixes, totaux différents.
            bool extTouteFixed   = bouchesExtraction.Count   > 0 && bouchesExtraction.All(b => b.DebitFixe);
            bool insufTouteFixed = bouchesInsufflation.Count > 0 && bouchesInsufflation.All(b => b.DebitFixe);

            if (extTouteFixed && insufTouteFixed)
            {
                double tE = bouchesExtraction.Sum(b => b.Debit);
                double tI = bouchesInsufflation.Sum(b => b.Debit);
                if (Math.Abs(tE - tI) > 0.5)
                {
                    extraction.Etat   = EtatCoteMoteur.DesequilibreImpossible;
                    insufflation.Etat = EtatCoteMoteur.DesequilibreImpossible;
                }
            }

            return new ResultatMoteur { Extraction = extraction, Insufflation = insufflation };
        }

        public ResultatMoteur Calculer(BilanAeraulique bilan) =>
            Calculer(bilan.BouchesExtraction, bilan.BouchesInsufflation);

        // ── Calcul d'un côté ─────────────────────────────────────

        private ResultatCoteMoteur CalculerCote(
            IReadOnlyList<BoucheVMC> bouches,
            bool estExtraction)
        {
            var result = new ResultatCoteMoteur { CapaciteMax = Capacite };

            if (bouches.Count == 0)
            {
                result.Etat = EtatCoteMoteur.Normal;
                return result;
            }

            // 1. Fixes
            double totalFixe = bouches.Where(b => b.DebitFixe).Sum(b => b.Debit);
            result.DebitFixeTotal = totalFixe;

            // 2. Reliquat
            double reliquat = Math.Max(0, Capacite - totalFixe);

            // 3. Bouches libres → distribution aléatoire
            var libres = bouches.Where(b => !b.DebitFixe).ToList();
            double debitLibreAlloue = 0;

            if (libres.Count > 0 && reliquat > 0)
            {
                double[] parts;

                if (libres.Count == 1)
                {
                    parts = new[] { reliquat };
                }
                else
                {
                    // Méthode des points de coupe dans [0, reliquat]
                    var cuts = new double[libres.Count + 1];
                    cuts[0] = 0;
                    cuts[libres.Count] = reliquat;
                    for (int i = 1; i < libres.Count; i++)
                        cuts[i] = _rng.NextDouble() * reliquat;
                    Array.Sort(cuts);

                    parts = new double[libres.Count];
                    for (int i = 0; i < libres.Count; i++)
                        parts[i] = cuts[i + 1] - cuts[i];
                }

                // Arrondir à 0.1 m³/h
                double totalArrondi = 0;
                for (int i = 0; i < parts.Length; i++)
                {
                    parts[i] = Math.Round(parts[i], 1);
                    totalArrondi += parts[i];
                }

                // Corriger l'écart d'arrondi sur la 1re bouche libre
                double ecart = Math.Round(reliquat - totalArrondi, 1);
                parts[0] = Math.Max(0, parts[0] + ecart);

                for (int i = 0; i < libres.Count; i++)
                {
                    result.Allocations.Add(new AllocBouche
                    {
                        Bouche = libres[i],
                        DebitEffectif = parts[i]
                    });
                    debitLibreAlloue += parts[i];
                }
            }
            else if (libres.Count > 0)
            {
                // Reliquat nul : les bouches libres reçoivent 0
                foreach (var b in libres)
                    result.Allocations.Add(new AllocBouche { Bouche = b, DebitEffectif = 0 });
            }

            result.DebitLibreAlloue = debitLibreAlloue;

            // 4. Ajouter les fixes
            foreach (var b in bouches.Where(b => b.DebitFixe))
                result.Allocations.Add(new AllocBouche { Bouche = b, DebitEffectif = b.Debit });

            // 5. État
            result.Etat = totalFixe > Capacite
                ? (estExtraction ? EtatCoteMoteur.ExtractionBloquee : EtatCoteMoteur.InsufflationLibre)
                : EtatCoteMoteur.Normal;

            return result;
        }
    }
}