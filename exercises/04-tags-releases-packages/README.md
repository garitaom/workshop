# 🏷️ Módulo 3: Tags, Releases y GitHub Packages

![Duración](https://img.shields.io/badge/Duración-25%20min-blue)
![Dificultad](https://img.shields.io/badge/Dificultad-Intermedio-orange)

## 🎯 Objetivos

- ✅ Entender la diferencia entre tags ligeros y tags anotados
- ✅ Crear tags con versionamiento semántico (SemVer)
- ✅ Crear un GitHub Release con notas automáticas y artefactos adjuntos
- ✅ Configurar un workflow que publique un NuGet package a GitHub Packages
- ✅ Consumir un paquete publicado en GitHub Packages desde otro proyecto

---

## Contexto

En equipos profesionales, el código no se "comparte" copiando archivos. Se versiona con **tags**, se distribuye como **releases** y se consume como **packages**. Este módulo conecta el pipeline de CI que ya construimos con el ciclo completo de distribución de software.

```
Commit → Tag (v1.0.0) → Release (binarios + notas) → Package (NuGet en GitHub Packages)
```

---

## Prerequisitos

```bash
# Verificar instalaciones
git --version
gh --version
dotnet --version   # 9.0 o superior

# Autenticarse en GitHub CLI (si no lo has hecho)
gh auth login
```

---

## Ejercicio 4.1 — Tags con Git y versionamiento semántico (10 min)

### Tags ligeros vs. anotados

```bash
# Tag ligero — solo un puntero a un commit (útil para marcas temporales)
git tag v0.1.0

# Tag anotado — incluye autor, fecha y mensaje (recomendado para releases)
git tag -a v1.0.0 -m "Primera versión estable de FinancialUtils"

# Ver todos los tags
git tag --list

# Ver detalles de un tag anotado
git show v1.0.0

# Subir un tag específico al remote
git push origin v1.0.0

# Subir todos los tags al remote
git push origin --tags
```

### Versionamiento semántico (SemVer)

El estándar [SemVer](https://semver.org/lang/es/) define el formato `MAJOR.MINOR.PATCH`:

| Componente | Cuándo incrementar | Ejemplo |
|------------|-------------------|---------|
| **MAJOR** | Cambio incompatible (breaking change) | `Calculator.Divide` ahora lanza `DivideByZeroException` en vez de retornar `0` |
| **MINOR** | Funcionalidad nueva compatible hacia atrás | Se agrega `Calculator.CompoundInterest` |
| **PATCH** | Corrección de bug sin cambio de API | Se corrige redondeo en `Formatter.TruncateDecimals` |

**Práctica:** Crea un tag anotado para la versión actual del proyecto:

```bash
git tag -a v1.0.0 -m "Release inicial: Calculator + Formatter con tests completos"
git push origin v1.0.0
```

### Eliminar y mover tags

```bash
# Eliminar tag local
git tag -d v0.1.0

# Eliminar tag remoto
git push origin --delete v0.1.0

# Mover un tag a otro commit (recrear)
git tag -d v1.0.0
git tag -a v1.0.0 -m "Release inicial" HEAD~2
git push origin v1.0.0 --force
```

> ⚠️ **Nunca muevas tags que ya se publicaron como Release.** Si necesitas corregir algo, crea un `v1.0.1`.

---

## Ejercicio 4.2 — Crear un GitHub Release (5 min)

Un **Release** en GitHub es una snapshot distribuible del código: incluye el código fuente comprimido, notas de cambios y opcionalmente binarios adjuntos.

### Desde GitHub CLI

```bash
# Crear release desde el último tag
gh release create v1.0.0 \
  --title "v1.0.0 — Release inicial" \
  --notes "### ✨ Funcionalidades
- Calculator: Add, Subtract, Multiply, Divide, CompoundInterest, LoanPayment
- Formatter: FormatCurrency, FormatPercentage, FormatNumber, TruncateDecimals
- Tests completos con xUnit
- Pipeline CI con 4 jobs encadenados"

# Crear release con notas generadas automáticamente desde los commits
gh release create v1.1.0 --generate-notes

# Crear un pre-release (para versiones beta/RC)
gh release create v2.0.0-beta.1 \
  --title "v2.0.0 Beta 1" \
  --prerelease \
  --generate-notes

# Adjuntar binarios compilados al release
dotnet publish src/FinancialUtils -c Release -o ./publish
cd publish && zip -r ../FinancialUtils-v1.0.0.zip . && cd ..
gh release upload v1.0.0 FinancialUtils-v1.0.0.zip
```

### Notas automáticas vs. manuales

| Método | Cuándo usar |
|--------|------------|
| `--generate-notes` | Commits siguen Conventional Commits y los PRs tienen títulos descriptivos |
| `--notes "..."` | Quieres control total sobre el contenido del changelog |
| `--notes-file CHANGELOG.md` | Mantienes un CHANGELOG.md manualmente en el repo |

**Práctica:** Crea un release para `v1.0.0` con notas manuales y verifica que aparezca en la pestaña "Releases" del repo.

---

## Ejercicio 4.3 — Publicar a GitHub Packages con un workflow (5 min)

GitHub Packages permite publicar paquetes NuGet directamente desde tu repositorio. Otros proyectos pueden consumirlos como dependencias.

### Workflow de publicación

Crea `.github/workflows/publish.yml`:

```yaml
name: Publish NuGet Package

on:
  release:
    types: [published]

env:
  DOTNET_NOLOGO: true
  DOTNET_CLI_TELEMETRY_OPTOUT: true

jobs:
  publish:
    name: Pack & Publish
    runs-on: ubuntu-latest
    permissions:
      packages: write
      contents: read

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Extraer versión del tag
        id: version
        run: echo "VERSION=${GITHUB_REF_NAME#v}" >> $GITHUB_OUTPUT

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Test
        run: dotnet test --configuration Release --no-restore

      - name: Pack
        run: |
          dotnet pack src/FinancialUtils/FinancialUtils.csproj \
            --configuration Release \
            --no-build \
            -p:PackageVersion=${{ steps.version.outputs.VERSION }} \
            --output ./nupkg

      - name: Publicar a GitHub Packages
        run: |
          dotnet nuget push ./nupkg/*.nupkg \
            --api-key ${{ secrets.GITHUB_TOKEN }} \
            --source "https://nuget.pkg.github.com/${{ github.repository_owner }}/index.json" \
            --skip-duplicate
```

### Cómo funciona

1. El workflow se dispara cuando publicas un **Release** (no al crear el tag)
2. Extrae la versión del nombre del tag (`v1.0.0` → `1.0.0`)
3. Compila, ejecuta tests y empaqueta como `.nupkg`
4. Publica a GitHub Packages usando `GITHUB_TOKEN` (no necesitas secrets adicionales)

### Configurar el `.csproj` para NuGet

Agrega estas propiedades a `src/FinancialUtils/FinancialUtils.csproj`:

```xml
<PropertyGroup>
  <!-- ... propiedades existentes ... -->
  <PackageId>FinancialUtils</PackageId>
  <Description>Utilidades financieras: interés compuesto, amortización y formateo</Description>
  <Authors>TU_USUARIO</Authors>
  <RepositoryUrl>https://github.com/TU_USUARIO/workshop-github-intermedio</RepositoryUrl>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
</PropertyGroup>
```

---

## Ejercicio 4.4 — Consumir un paquete desde GitHub Packages (5 min)

Para consumir un paquete publicado en GitHub Packages desde otro proyecto, necesitas configurar el source de NuGet.

### Configurar el source

```bash
# Agregar GitHub Packages como source de NuGet (una sola vez)
dotnet nuget add source \
  "https://nuget.pkg.github.com/TU_USUARIO/index.json" \
  --name "github-packages" \
  --username TU_USUARIO \
  --password $(gh auth token) \
  --store-password-in-clear-text
```

O crea un `nuget.config` en la raíz del proyecto consumidor:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
    <add key="github" value="https://nuget.pkg.github.com/TU_USUARIO/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <github>
      <add key="Username" value="TU_USUARIO" />
      <add key="ClearTextPassword" value="%GITHUB_TOKEN%" />
    </github>
  </packageSourceCredentials>
</configuration>
```

### Instalar el paquete

```bash
# Desde otro proyecto .NET
dotnet add package FinancialUtils --version 1.0.0 --source github-packages
```

**Punto de discusión:** ¿Cuándo publicarías en GitHub Packages vs. NuGet.org? GitHub Packages es ideal para paquetes internos de organización. NuGet.org es para paquetes públicos de uso general.

---

## Referencia rápida

| Tarea | Comando |
|-------|---------|
| Crear tag anotado | `git tag -a v1.0.0 -m "mensaje"` |
| Listar tags | `git tag --list` |
| Subir tag | `git push origin v1.0.0` |
| Subir todos los tags | `git push origin --tags` |
| Eliminar tag remoto | `git push origin --delete v1.0.0` |
| Crear release | `gh release create v1.0.0` |
| Release con notas auto | `gh release create v1.0.0 --generate-notes` |
| Crear pre-release | `gh release create v2.0.0-beta.1 --prerelease` |
| Adjuntar archivo a release | `gh release upload v1.0.0 archivo.zip` |
| Listar releases | `gh release list` |
| Ver un release | `gh release view v1.0.0` |
| Empaquetar NuGet | `dotnet pack -c Release` |
| Publicar NuGet | `dotnet nuget push *.nupkg --source github` |

---

## 🛠️ Troubleshooting del Módulo

| Problema | Solución |
|----------|----------|
| `git push origin v1.0.0` dice "already exists" | El tag ya existe en el remote. Elimínalo con `git push origin --delete v1.0.0` y vuelve a subirlo |
| `gh release create` falla con "tag not found" | Sube el tag primero: `git push origin v1.0.0` |
| El workflow `publish.yml` no se dispara | Verifica que el trigger sea `release: types: [published]`, no `push` con tags |
| `dotnet pack` genera versión `1.0.0` aunque el tag sea `v2.0.0` | Verifica que el step de versión extraiga correctamente: `${GITHUB_REF_NAME#v}` |
| `dotnet nuget push` da 401 | Verifica que el job tenga `permissions: packages: write` y que use `GITHUB_TOKEN` |
| No veo el paquete en GitHub | Ve a la pestaña "Packages" del repo. Puede tardar unos segundos en aparecer |
| Error al consumir el paquete | Verifica que el `nuget.config` tenga el source correcto y las credenciales sean válidas |

---

## ✅ Verificación

Al terminar este módulo, deberías poder responder:

1. ¿Cuál es la diferencia entre un tag ligero y un tag anotado? ¿Cuándo usarías cada uno?
2. ¿Qué ventaja tiene `--generate-notes` sobre escribir notas manualmente?
3. ¿Por qué el workflow de publicación se dispara con `release: [published]` y no con `push: tags`?
4. ¿Cuándo publicarías en GitHub Packages vs. NuGet.org?
5. ¿Qué pasa si publicas la misma versión dos veces a GitHub Packages?

> 📝 **Para el instructor:** El Ejercicio 4.1 (tags) y 4.2 (releases) son los fundamentales. El 4.3 (workflow de publicación) es el de mayor impacto si hay tiempo para una demo en vivo. El 4.4 (consumir paquetes) puede dejarse como ejercicio para después del workshop.
