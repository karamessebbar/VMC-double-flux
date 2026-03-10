#!/bin/bash

# Script de compilation et d'exécution pour VMC Double Flux
# Compatible Ubuntu

echo "=================================="
echo "VMC Double Flux - Compilation"
echo "=================================="

# Vérifier si dotnet est installé
if ! command -v dotnet &> /dev/null
then
    echo "❌ .NET SDK n'est pas installé."
    echo ""
    echo "Pour installer .NET sur Ubuntu, suivez ces étapes:"
    echo ""
    echo "1. Téléchargez et installez .NET 8.0 SDK:"
    echo "   wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh"
    echo "   chmod +x dotnet-install.sh"
    echo "   ./dotnet-install.sh --channel 8.0"
    echo ""
    echo "2. Ajoutez .NET au PATH:"
    echo "   export PATH=\"\$PATH:\$HOME/.dotnet\""
    echo "   echo 'export PATH=\"\$PATH:\$HOME/.dotnet\"' >> ~/.bashrc"
    echo ""
    echo "Ou installez via APT (Ubuntu 22.04+):"
    echo "   sudo apt-get update"
    echo "   sudo apt-get install -y dotnet-sdk-8.0"
    echo ""
    exit 1
fi

echo "✓ .NET SDK détecté: $(dotnet --version)"
echo ""

# Restaurer les packages NuGet
echo "📦 Restauration des packages NuGet..."
dotnet restore

if [ $? -ne 0 ]; then
    echo "❌ Échec de la restauration des packages"
    exit 1
fi

echo "✓ Packages restaurés avec succès"
echo ""

# Compilation
echo "🔨 Compilation du projet..."
dotnet build --configuration Release

if [ $? -ne 0 ]; then
    echo "❌ Échec de la compilation"
    exit 1
fi

echo "✓ Compilation réussie"
echo ""

# Exécution
echo "🚀 Lancement de l'application..."
echo ""
dotnet run --configuration Release
