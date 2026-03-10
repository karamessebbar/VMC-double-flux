#!/bin/bash

# Script de correction des erreurs de compilation
echo "🔧 Correction des erreurs de compilation..."

cd ~/Desktop/master/s8/dotnet/VMCDoubleFlux

# Correction 1: Brush cast dans VueGraphique.cs (lignes 200-207)
sed -i '200,207s/Brush brushConnexion/IBrush brushConnexion/' VueGraphique.cs

# Correction 2: Brush cast dans VueGraphique.cs (lignes 245-252)  
sed -i '245,252s/Brush brush/IBrush brush/' VueGraphique.cs

# Correction 3: StackPanel.Padding (ligne 434 MainWindow.axaml.cs)
sed -i 's/Padding = new Thickness(20)/Margin = new Thickness(20)/' MainWindow.axaml.cs

echo "✅ Corrections appliquées!"
echo ""
echo "Compilation..."
dotnet build

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ ✅ ✅ COMPILATION RÉUSSIE! ✅ ✅ ✅"
    echo ""
    echo "Lancement de l'application..."
    dotnet run
else
    echo "❌ Échec de la compilation"
fi