# Soluciones — Módulo 3: Tags, Releases y GitHub Packages

## Ejercicio 4.1 — Tags y SemVer

```bash
# Crear tag anotado para la versión inicial
git tag -a v1.0.0 -m "Release inicial: Calculator + Formatter con tests completos"

# Verificar que se creó correctamente
git show v1.0.0
# Debe mostrar: tagger, fecha, mensaje y el commit al que apunta

# Subir al remote
git push origin v1.0.0
```

**Diferencia clave:**
- Tag ligero (`git tag v1.0.0`): solo un puntero, no tiene autor ni fecha propia
- Tag anotado (`git tag -a v1.0.0 -m "..."`): objeto completo con metadatos, recomendado para releases

---

## Ejercicio 4.2 — Crear un Release

```bash
# Opción 1: Con notas manuales
gh release create v1.0.0 \
  --title "v1.0.0 — Release inicial" \
  --notes "### ✨ Funcionalidades
- Calculator: Add, Subtract, Multiply, Divide, CompoundInterest, LoanPayment
- Formatter: FormatCurrency, FormatPercentage, FormatNumber, TruncateDecimals
- Tests completos con xUnit
- Pipeline CI con 4 jobs encadenados"

# Opción 2: Con notas generadas automáticamente
gh release create v1.0.0 --generate-notes

# Verificar
gh release view v1.0.0
```

Para adjuntar binarios:

```bash
dotnet publish src/FinancialUtils -c Release -o ./publish
cd publish && zip -r ../FinancialUtils-v1.0.0.zip . && cd ..
gh release upload v1.0.0 FinancialUtils-v1.0.0.zip
```

---

## Ejercicio 4.3 — Workflow de publicación a GitHub Packages

El workflow `.github/workflows/publish.yml` se dispara automáticamente cuando creas un Release. Puntos clave:

1. **Trigger:** `release: types: [published]` — se dispara al publicar, no al crear el tag
2. **Permisos:** `permissions: packages: write` — necesario para publicar paquetes
3. **Versión:** Se extrae del nombre del tag con `${GITHUB_REF_NAME#v}` (quita el prefijo `v`)
4. **Auth:** Usa `GITHUB_TOKEN` automáticamente, no necesitas configurar secrets

Para verificar que funciona:
1. Crea un tag: `git tag -a v1.0.0 -m "Release inicial" && git push origin v1.0.0`
2. Crea el release: `gh release create v1.0.0 --generate-notes`
3. Ve a la pestaña "Actions" del repo y verifica que el workflow "Publish NuGet Package" se ejecutó
4. Ve a la pestaña "Packages" del repo y verifica que aparece `FinancialUtils` versión `1.0.0`

---

## Ejercicio 4.4 — Consumir el paquete

```bash
# Configurar el source de GitHub Packages
dotnet nuget add source \
  "https://nuget.pkg.github.com/TU_USUARIO/index.json" \
  --name "github-packages" \
  --username TU_USUARIO \
  --password $(gh auth token) \
  --store-password-in-clear-text

# Crear un proyecto de prueba para verificar
mkdir /tmp/test-consume && cd /tmp/test-consume
dotnet new console
dotnet add package FinancialUtils --version 1.0.0 --source github-packages

# Verificar que se instaló
dotnet list package
```

**GitHub Packages vs. NuGet.org:**
- **GitHub Packages:** Paquetes internos, visibilidad controlada por permisos del repo, integración nativa con Actions
- **NuGet.org:** Paquetes públicos de uso general, mayor descubribilidad, ecosistema más amplio
