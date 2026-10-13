#!/usr/bin/env bash
# Prépare le site statique tel qu'il est déployé sur GitHub Pages.
#
#   scripts/publier-site.sh [chemin de base] [dossier de sortie]
#   scripts/publier-site.sh /OeufFrais/ artifacts/site
#
# Le chemin de base est écrit dans <base href> AVANT la publication : le service worker vérifie
# l'empreinte de chaque fichier, et un index.html modifié après coup l'empêcherait de s'installer
# (plus de fonctionnement hors ligne). Le fichier source est restauré à la fin.
set -euo pipefail

BASE="${1:-/}"
SORTIE="${2:-artifacts/site}"
RACINE="$(cd "$(dirname "$0")/.." && pwd)"
INDEX="$RACINE/src/OeufFrais.Web/wwwroot/index.html"

cd "$RACINE"
cp "$INDEX" "$INDEX.original"
trap 'mv "$INDEX.original" "$INDEX"' EXIT
sed -i "s|<base href=\"/\" />|<base href=\"$BASE\" />|" "$INDEX"
grep -q "<base href=\"$BASE\" />" "$INDEX" || { echo "Balise <base> introuvable dans $INDEX" >&2; exit 1; }

rm -rf artifacts/publish "$SORTIE"
dotnet publish src/OeufFrais.Web -c Release -o artifacts/publish
mkdir -p "$(dirname "$SORTIE")"
cp -r artifacts/publish/wwwroot "$SORTIE"

# GitHub Pages compresse lui-même : les versions .br et .gz ne servent qu'à alourdir le site.
find "$SORTIE" \( -name '*.br' -o -name '*.gz' \) -delete

# GitHub Pages ne sait pas réécrire les adresses : pour /OeufFrais/code, il sert 404.html,
# qui démarre l'app comme index.html. (Absent du manifeste du service worker, c'est voulu.)
cp "$SORTIE/index.html" "$SORTIE/404.html"
touch "$SORTIE/.nojekyll"

echo "Site prêt dans $SORTIE (base $BASE), $(du -sh "$SORTIE" | cut -f1)."
