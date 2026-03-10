using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace VMCDoubleFlux.Models
{
    /// <summary>
    /// Type de bouche VMC
    /// </summary>
    public enum TypeBouche
    {
        Extraction,         // Rouge - air vicié
        Insufflation,       // Bleu - air neuf
        Porte,              // Marron - porte
        Fenetre,            // Bleu clair - fenêtre
        Echangeur,          // Vert - 1 seul par maison
        PlenumExtraction,   // Rouge foncé - 1 seul par maison
        PlenumInsufflation  // Bleu foncé - 1 seul par maison
    }

    /// <summary>
    /// Point 2D pour les coordonnées
    /// </summary>
    public class Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public Point2D() : this(0, 0) { }
    }

    /// <summary>
    /// Bouche de VMC (extraction ou insufflation)
    /// </summary>
    public class BoucheVMC
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public TypeBouche Type { get; set; }
        public double Diametre { get; set; } // en mm
        public double Debit { get; set; } // en m³/h
        public Point2D Position { get; set; }
        public string Piece { get; set; } // Nom de la pièce

        public BoucheVMC()
        {
            Id = Guid.NewGuid().ToString();
            Nom = "Nouvelle bouche";
            Type = TypeBouche.Extraction;
            Diametre = 125;
            Debit = 45;
            Position = new Point2D();
            Piece = "";
        }

        public override string ToString()
        {
            return $"{Nom} - Ø{Diametre}mm - {Debit}m³/h";
        }
    }

    /// <summary>
    /// Mur intérieur (horizontal ou vertical)
    /// </summary>
    public class Mur
    {
        public string Id { get; set; }
        public Point2D Debut { get; set; }
        public Point2D Fin { get; set; }
        public bool EstExterieur { get; set; }
        public List<Ouverture> Ouvertures { get; set; }

        public Mur()
        {
            Id = Guid.NewGuid().ToString();
            Debut = new Point2D();
            Fin = new Point2D();
            EstExterieur = false;
            Ouvertures = new List<Ouverture>();
        }

        public bool EstHorizontal => Math.Abs(Debut.Y - Fin.Y) < 0.01;
        public bool EstVertical => Math.Abs(Debut.X - Fin.X) < 0.01;
        
        public double Longueur
        {
            get
            {
                double dx = Fin.X - Debut.X;
                double dy = Fin.Y - Debut.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }
        }
    }

    /// <summary>
    /// Ouverture (porte ou fenêtre) dans un mur
    /// </summary>
    public class Ouverture
    {
        public string Id { get; set; }
        public string Type { get; set; } // "Porte" ou "Fenetre"
        public double Position { get; set; } // Position sur le mur (0-1)
        public double Largeur { get; set; } // en mètres

        public Ouverture()
        {
            Id = Guid.NewGuid().ToString();
            Type = "Porte";
            Position = 0.5;
            Largeur = 0.9;
        }
    }

    /// <summary>
    /// Bilan aéraulique d'un étage
    /// </summary>
    public class BilanEtage
    {
        public string NomEtage { get; set; }
        public List<BoucheVMC> BouchesExtraction { get; set; }
        public List<BoucheVMC> BouchesInsufflation { get; set; }
        public List<BoucheVMC> Portes { get; set; }
        public List<BoucheVMC> Fenetres { get; set; }

        public double DebitTotalExtraction => BouchesExtraction.Sum(b => b.Debit);
        public double DebitTotalInsufflation => BouchesInsufflation.Sum(b => b.Debit);
        public int NombreTotalBouches => BouchesExtraction.Count + BouchesInsufflation.Count;

        public BilanEtage()
        {
            NomEtage = "";
            BouchesExtraction = new List<BoucheVMC>();
            BouchesInsufflation = new List<BoucheVMC>();
            Portes = new List<BoucheVMC>();
            Fenetres = new List<BoucheVMC>();
        }

        public bool EstEquilibre(double tolerance = 5.0)
        {
            return Math.Abs(DebitTotalExtraction - DebitTotalInsufflation) <= tolerance;
        }
    }

    /// <summary>
    /// Étage de la maison
    /// </summary>
    public class Etage
    {
        public string Id { get; set; }
        public string Nom { get; set; }
        public double Largeur { get; set; } // en mètres
        public double Hauteur { get; set; } // en mètres
        public double HauteurSousPlafond { get; set; } // en mètres
        public List<Mur> Murs { get; set; }
        public List<BoucheVMC> Bouches { get; set; }

        public Etage()
        {
            Id = Guid.NewGuid().ToString();
            Nom = "Nouvel étage";
            Largeur = 15.0;
            Hauteur = 12.0;
            HauteurSousPlafond = 2.5;
            Murs = new List<Mur>();
            Bouches = new List<BoucheVMC>();
            
            // Créer les murs extérieurs
            CreerMursExterieurs();
        }

        public Etage(string nom, double largeur, double hauteur) : this()
        {
            Nom = nom;
            Largeur = largeur;
            Hauteur = hauteur;
            
            // Recréer les murs avec les nouvelles dimensions
            Murs.Clear();
            CreerMursExterieurs();
        }

        private void CreerMursExterieurs()
        {
            // Mur haut
            Murs.Add(new Mur
            {
                Debut = new Point2D(0, 0),
                Fin = new Point2D(Largeur, 0),
                EstExterieur = true
            });

            // Mur droit
            Murs.Add(new Mur
            {
                Debut = new Point2D(Largeur, 0),
                Fin = new Point2D(Largeur, Hauteur),
                EstExterieur = true
            });

            // Mur bas
            Murs.Add(new Mur
            {
                Debut = new Point2D(Largeur, Hauteur),
                Fin = new Point2D(0, Hauteur),
                EstExterieur = true
            });

            // Mur gauche
            Murs.Add(new Mur
            {
                Debut = new Point2D(0, Hauteur),
                Fin = new Point2D(0, 0),
                EstExterieur = true
            });
        }

        public void AjouterBouche(BoucheVMC bouche)
        {
            Bouches.Add(bouche);
        }

        public void SupprimerBouche(string boucheId)
        {
            Bouches.RemoveAll(b => b.Id == boucheId);
        }

        public void AjouterMur(Mur mur)
        {
            Murs.Add(mur);
        }

        public void SupprimerMur(string murId)
        {
            Murs.RemoveAll(m => m.Id == murId && !m.EstExterieur);
        }

        /// <summary>
        /// Calcule le bilan aéraulique de cet étage
        /// </summary>
        public BilanEtage CalculerBilan()
        {
            var bilan = new BilanEtage { NomEtage = Nom };

            foreach (var bouche in Bouches)
            {
                switch (bouche.Type)
                {
                    case TypeBouche.Extraction:
                        bilan.BouchesExtraction.Add(bouche);
                        break;
                    case TypeBouche.Insufflation:
                        bilan.BouchesInsufflation.Add(bouche);
                        break;
                    case TypeBouche.Porte:
                        bilan.Portes.Add(bouche);
                        break;
                    case TypeBouche.Fenetre:
                        bilan.Fenetres.Add(bouche);
                        break;
                }
            }

            return bilan;
        }
    }

    /// <summary>
    /// Projet VMC complet
    /// </summary>
    public class ProjetVMC
    {
        public string Nom { get; set; }
        public double Echelle { get; set; } // pixels par mètre
        public List<Etage> Etages { get; set; }
        public string EtageActifId { get; set; }

        // ✅ Éléments VMC uniques pour toute la maison
        public BoucheVMC? Echangeur { get; set; }
        public BoucheVMC? PlenumExtraction { get; set; }
        public BoucheVMC? PlenumInsufflation { get; set; }

        // ID de l'étage où se trouvent les éléments uniques
        public string? EchangeurEtageId { get; set; }
        public string? PlenumExtractionEtageId { get; set; }
        public string? PlenumInsufflationEtageId { get; set; }

        public ProjetVMC()
        {
            Nom = "Nouveau projet VMC";
            Echelle = 50.0; // 50 pixels = 1 mètre
            Etages = new List<Etage>();
            
            // Créer un étage par défaut
            var etageDefaut = new Etage("Rez-de-chaussee", 15.0, 12.0);
            Etages.Add(etageDefaut);
            EtageActifId = etageDefaut.Id;
        }

        [JsonIgnore]
        public Etage? EtageActif
        {
            get => Etages.Find(e => e.Id == EtageActifId);
            set => EtageActifId = value?.Id ?? "";
        }

        public void AjouterEtage(Etage etage)
        {
            Etages.Add(etage);
        }

        public void SupprimerEtage(string etageId)
        {
            if (Etages.Count > 1)
            {
                Etages.RemoveAll(e => e.Id == etageId);
                if (EtageActifId == etageId && Etages.Count > 0)
                {
                    EtageActifId = Etages[0].Id;
                }
            }
        }

        /// <summary>
        /// Définit l'échangeur (1 seul autorisé)
        /// </summary>
        public bool DefinirEchangeur(BoucheVMC echangeur, string etageId)
        {
            Echangeur = echangeur;
            EchangeurEtageId = etageId;
            return true;
        }

        /// <summary>
        /// Définit le plenum d'extraction (1 seul autorisé)
        /// </summary>
        public bool DefinirPlenumExtraction(BoucheVMC plenum, string etageId)
        {
            PlenumExtraction = plenum;
            PlenumExtractionEtageId = etageId;
            return true;
        }

        /// <summary>
        /// Définit le plenum d'insufflation (1 seul autorisé)
        /// </summary>
        public bool DefinirPlenumInsufflation(BoucheVMC plenum, string etageId)
        {
            PlenumInsufflation = plenum;
            PlenumInsufflationEtageId = etageId;
            return true;
        }

        /// <summary>
        /// Calcule le bilan aéraulique total
        /// </summary>
        public BilanAeraulique CalculerBilan()
        {
            var bilan = new BilanAeraulique
            {
                Echangeur = Echangeur,
                PlenumExtraction = PlenumExtraction,
                PlenumInsufflation = PlenumInsufflation
            };

            // Calculer les bilans par étage
            foreach (var etage in Etages)
            {
                var bilanEtage = etage.CalculerBilan();
                bilan.BilansEtages.Add(bilanEtage);

                // Ajouter au bilan global
                foreach (var bouche in etage.Bouches)
                {
                    bilan.AjouterBouche(bouche);
                }
            }

            return bilan;
        }

        /// <summary>
        /// Sauvegarde le projet au format JSON
        /// </summary>
        public string SauvegarderJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// Charge un projet depuis JSON
        /// </summary>
        public static ProjetVMC? ChargerJson(string json)
        {
            try
            {
                return JsonConvert.DeserializeObject<ProjetVMC>(json);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Bilan aéraulique du projet
    /// </summary>
    public class BilanAeraulique
    {
        public List<BoucheVMC> BouchesExtraction { get; set; }
        public List<BoucheVMC> BouchesInsufflation { get; set; }
        public List<BoucheVMC> Portes { get; set; }
        public List<BoucheVMC> Fenetres { get; set; }

        // ✅ Éléments VMC uniques
        public BoucheVMC? Echangeur { get; set; }
        public BoucheVMC? PlenumExtraction { get; set; }
        public BoucheVMC? PlenumInsufflation { get; set; }

        // ✅ Bilans par étage
        public List<BilanEtage> BilansEtages { get; set; }

        public double DebitTotalExtraction { get; set; }
        public double DebitTotalInsufflation { get; set; }
        public int NombreTotalBouches { get; set; }

        public BilanAeraulique()
        {
            BouchesExtraction = new List<BoucheVMC>();
            BouchesInsufflation = new List<BoucheVMC>();
            Portes = new List<BoucheVMC>();
            Fenetres = new List<BoucheVMC>();
            BilansEtages = new List<BilanEtage>();
        }

        public void AjouterBouche(BoucheVMC bouche)
        {
            switch (bouche.Type)
            {
                case TypeBouche.Extraction:
                    BouchesExtraction.Add(bouche);
                    DebitTotalExtraction += bouche.Debit;
                    NombreTotalBouches++;
                    break;
                case TypeBouche.Insufflation:
                    BouchesInsufflation.Add(bouche);
                    DebitTotalInsufflation += bouche.Debit;
                    NombreTotalBouches++;
                    break;
                case TypeBouche.Porte:
                    Portes.Add(bouche);
                    break;
                case TypeBouche.Fenetre:
                    Fenetres.Add(bouche);
                    break;
                // Les éléments uniques ne sont pas comptés ici
                case TypeBouche.Echangeur:
                case TypeBouche.PlenumExtraction:
                case TypeBouche.PlenumInsufflation:
                    break;
            }
        }

        public bool EstEquilibre()
        {
            double diff = Math.Abs(DebitTotalExtraction - DebitTotalInsufflation);
            return diff < 5.0; // Tolérance de 5 m³/h
        }

        public bool SystemeComplet()
        {
            return Echangeur != null && PlenumExtraction != null && PlenumInsufflation != null;
        }

        public override string ToString()
        {
            return $"Extraction: {DebitTotalExtraction} m³/h | Insufflation: {DebitTotalInsufflation} m³/h";
        }
    }
}