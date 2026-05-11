# 🎓 Guía del Instructor — Módulo 4: Tags, Releases y GitHub Packages

**Duración:** 25 minutos (ejercicios 4.1 a 4.4)
**Archivos clave:** `src/FinancialUtils/FinancialUtils.csproj`, workflow `publish.yml` (se crea en el ejercicio)

---

## Objetivo pedagógico

Que los participantes entiendan el ciclo completo de distribución de software: código → tag → release → package. En equipos profesionales, el código no se comparte copiando archivos ni mandando ZIPs por Slack. Se versiona con tags, se distribuye con releases y se consume como dependencia a través de un registry (NuGet, npm, Maven, etc.).

```
Commit → Tag (v1.0.0) → Release (binarios + notas) → Package (NuGet en GitHub Packages)
```

---

## Antes de empezar

### Material que debes tener listo

- Terminal con `git`, `gh` y `dotnet` configurados.
- Conocimiento del versionamiento semántico (SemVer).
- El `.csproj` de FinancialUtils abierto para mostrar las propiedades de NuGet.
- La pestaña **Packages** del repo visible (puede estar vacía al inicio).

### Nota importante sobre GitHub Packages en Enterprise

En GitHub Enterprise:
- **GitHub Packages puede no estar habilitado** a nivel de organización. Verifica con anticipación.
- El **GITHUB_TOKEN** tiene permisos limitados en algunos entornos enterprise. Si el workflow de publicación falla con 403, puede ser una restricción de la org.
- Si Packages no está disponible, el ejercicio 4.3 puede hacerse como **demo conceptual** sin ejecutar el workflow. Los conceptos de `dotnet pack` y `dotnet nuget push` son transferibles a NuGet.org u otros registries.

---

## Ejercicio 4.1 — Tags y versionamiento semántico (10 min) ⭐ PRIORIDAD ALTA

### Qué explicar

#### Tags ligeros vs anotados

| Aspecto | Tag ligero | Tag anotado |
|---------|-----------|-------------|
| Comando | `git tag v1.0.0` | `git tag -a v1.0.0 -m "mensaje"` |
| Contenido | Solo un puntero a un commit | Objeto completo: autor, fecha, mensaje |
| `git show` | Muestra el commit | Muestra el tag + el commit |
| Cuándo usar | Marcas temporales o internas | **Siempre** para releases |

**Regla general:** Si el tag va a convertirse en un release o lo van a ver otras personas, usa tag anotado. Si es para uso personal temporal, ligero está bien.

#### Versionamiento semántico (SemVer)

Formato: `MAJOR.MINOR.PATCH` — cada componente tiene un significado preciso:

| Componente | Cuándo incrementar | Ejemplo concreto con FinancialUtils |
|------------|-------------------|--------------------------------------|
| **MAJOR** | Cambio incompatible (breaking change) | `Calculator.Divide` ahora lanza excepción en vez de retornar 0 |
| **MINOR** | Funcionalidad nueva, compatible hacia atrás | Se agrega `Calculator.CompoundInterest` |
| **PATCH** | Bug fix sin cambio de API | Se corrige redondeo en `Formatter.TruncateDecimals` |

**Pregunta que debes hacer al grupo:** "Si agrego un nuevo método `Calculator.NetPresentValue()` sin modificar ninguno existente, ¿qué componente incremento?" → MINOR (funcionalidad nueva, compatible).

"¿Y si cambio `Calculator.Add(int, int)` para que acepte `decimal` en vez de `int`?" → MAJOR (cambio de API que rompe código existente).

#### Demo en vivo

```bash
# Crear tag anotado
git tag -a v1.0.0 -m "Release inicial: Calculator + Formatter con tests completos"

# Ver el tag
git show v1.0.0
# Señalar: muestra el autor del tag, la fecha, el mensaje Y el commit al que apunta

# Listar tags
git tag --list

# Subir al remote
git push origin v1.0.0

# Mostrar en GitHub: el tag aparece en Code > Tags
```

#### Eliminar y mover tags — cuándo y cuándo NO

```bash
# Eliminar tag (local + remoto)
git tag -d v0.1.0
git push origin --delete v0.1.0
```

**Regla de oro que debes enfatizar:** **Nunca muevas un tag que ya tiene un Release asociado.** Si necesitas corregir algo, crea `v1.0.1`. Mover tags rompe la confianza: alguien que descargó `v1.0.0` espera que siempre apunte al mismo código.

**Analogía útil:** Un tag es como un número de versión en una etiqueta de producto. No puedes cambiar el contenido de una caja ya vendida y pretender que sigue siendo la misma versión.

---

## Ejercicio 4.2 — Crear un GitHub Release (5 min)

### Qué explicar

Un Release es una **snapshot distribuible**: incluye código fuente comprimido automáticamente (.zip y .tar.gz), notas de cambios y opcionalmente binarios adjuntos.

#### Notas automáticas vs manuales

| Método | Cuándo usar | Calidad del resultado |
|--------|------------|----------------------|
| `--generate-notes` | Commits siguen Conventional Commits y PRs tienen buenos títulos | Buena — resume PRs mergeados desde el último release |
| `--notes "..."` | Quieres control total del changelog | Excelente — pero requiere trabajo manual |
| `--notes-file CHANGELOG.md` | Mantienes un CHANGELOG manualmente | Excelente si el CHANGELOG está actualizado |

**Recomendación:** Para equipos que usan Conventional Commits (`feat:`, `fix:`, `docs:`), `--generate-notes` funciona sorprendentemente bien. Para equipos sin convenciones, las notas automáticas son ruido y es mejor escribirlas manualmente.

#### Demo en vivo

```bash
# Crear release con notas manuales
gh release create v1.0.0 \
  --title "v1.0.0 — Release inicial" \
  --notes "### ✨ Funcionalidades
- Calculator: Add, Subtract, Multiply, Divide, CompoundInterest, LoanPayment
- Formatter: FormatCurrency, FormatPercentage, FormatNumber, TruncateDecimals
- Tests completos con xUnit
- Pipeline CI con 4 jobs encadenados"

# Verificar en GitHub
gh release view v1.0.0
```

Después, muestra la pestaña **Releases** en GitHub. Señala:
- El código fuente se adjunta automáticamente.
- Las notas se renderizan como Markdown.
- El tag asociado aparece vinculado.

#### Pre-releases

Menciona brevemente que puedes crear pre-releases para betas o release candidates:

```bash
gh release create v2.0.0-beta.1 --prerelease --generate-notes
```

Los pre-releases se marcan visualmente en GitHub y no aparecen como "Latest release". Útil para distribuir versiones de prueba sin confundir a los consumidores.

---

## Ejercicio 4.3 — Workflow de publicación a GitHub Packages (5 min)

### Qué explicar

Este ejercicio conecta todo el módulo 1 (Actions) con el módulo 4 (distribución). El workflow se dispara cuando se publica un Release — no al crear el tag.

#### Flujo completo

```
git tag -a v1.0.0 → git push origin v1.0.0 → gh release create v1.0.0
                                                        │
                                              workflow publish.yml se dispara
                                                        │
                                              dotnet pack → dotnet nuget push
                                                        │
                                              Paquete en GitHub Packages
```

#### Anatomía del workflow

Recorre el workflow `publish.yml` línea por línea:

1. **Trigger: `release: types: [published]`**
   - NO es `push: tags` — explica la diferencia.
   - `push: tags` se dispara cuando el tag llega al remote. `release: [published]` se dispara cuando alguien crea un Release (que puede ser mucho después del tag, o con `--draft` que no dispara hasta que se publique).
   - **Pregunta útil:** "¿Por qué no usamos `push: tags`?" → Porque podrías crear un tag para pruebas internas sin querer publicar un paquete. El Release es la acción explícita de "esto está listo para distribución".

2. **Permissions: `packages: write`**
   - El `GITHUB_TOKEN` por defecto no tiene permisos de escritura en Packages.
   - Sin esta línea, el `dotnet nuget push` falla con 403.
   - En GitHub Enterprise, los permisos del GITHUB_TOKEN pueden estar más restringidos. Si falla, verificar la configuración de la org.

3. **Extracción de versión: `${GITHUB_REF_NAME#v}`**
   - `GITHUB_REF_NAME` contiene el nombre del tag: `v1.0.0`.
   - `${GITHUB_REF_NAME#v}` es shell parameter expansion que quita el prefijo `v` → `1.0.0`.
   - Esto es necesario porque NuGet no acepta el prefijo `v` en versiones. El tag es `v1.0.0` pero el paquete es `1.0.0`.

4. **`dotnet pack` con versión dinámica**
   - `-p:PackageVersion=${{ steps.version.outputs.VERSION }}` sobrescribe la versión del `.csproj`.
   - Esto permite que el `.csproj` tenga `<Version>1.0.0</Version>` como default, pero el workflow use la versión del tag.

5. **`--skip-duplicate`**
   - Si por algún error re-publicas la misma versión, el comando no falla — simplemente no sube el paquete duplicado.
   - Sin esto, un re-run del workflow fallaría con "package already exists".

#### Configuración del `.csproj`

El ejercicio pide agregar propiedades de NuGet al `.csproj`. Explica cada una:

```xml
<PackageId>FinancialUtils</PackageId>           <!-- Nombre único del paquete -->
<Description>Utilidades financieras</Description> <!-- Se muestra en el registry -->
<Authors>TU_USUARIO</Authors>                     <!-- Autor del paquete -->
<RepositoryUrl>https://github.com/...</RepositoryUrl>  <!-- Link al repo -->
<PackageLicenseExpression>MIT</PackageLicenseExpression> <!-- Licencia -->
```

**Nota:** `PackageId` debe ser único dentro del scope del registry. En GitHub Packages, el scope es la organización. Dos repos en la misma org no pueden publicar paquetes con el mismo `PackageId`.

### Si GitHub Packages no está disponible

Si la org enterprise tiene Packages deshabilitado:
1. Explica el workflow conceptualmente sin ejecutarlo.
2. Muestra los comandos de `dotnet pack` y `dotnet nuget push` localmente.
3. El concepto es 100% transferible a NuGet.org u otro registry privado (Azure Artifacts, MyGet, etc.).

---

## Ejercicio 4.4 — Consumir un paquete (5 min)

### Qué explicar

Este ejercicio cierra el ciclo: publicaste un paquete, ahora otro proyecto lo consume como dependencia.

#### Configurar el source de NuGet

GitHub Packages no es un source público — requiere autenticación. Hay dos formas:

**Opción 1: CLI (rápida, para una sola máquina)**
```bash
dotnet nuget add source \
  "https://nuget.pkg.github.com/TU_ORG/index.json" \
  --name "github-packages" \
  --username TU_USUARIO \
  --password $(gh auth token) \
  --store-password-in-clear-text
```

**Opción 2: `nuget.config` (portable, para el repo)**
```xml
<packageSources>
  <add key="github" value="https://nuget.pkg.github.com/TU_ORG/index.json" />
</packageSources>
```

**`--store-password-in-clear-text`** — sí, se ve inseguro. Es necesario en macOS/Linux porque el credential store de NuGet no funciona bien fuera de Windows. En CI, usas `GITHUB_TOKEN` que se inyecta como variable de entorno.

#### Demo sugerida

Si el tiempo lo permite, crea un proyecto de prueba rápido:

```bash
mkdir /tmp/test-consume && cd /tmp/test-consume
dotnet new console
dotnet add package FinancialUtils --version 1.0.0 --source github-packages
```

Si no hay tiempo, explica el concepto y muestra el comando.

#### GitHub Packages vs NuGet.org

| Aspecto | GitHub Packages | NuGet.org |
|---------|----------------|-----------|
| Visibilidad | Controlada por permisos del repo/org | Pública |
| Autenticación | Requiere token | No requiere para consumir |
| Costo | Incluido en GitHub (con límites) | Gratis |
| Cuándo usar | Paquetes internos de organización | Paquetes open source |
| Descubribilidad | Solo dentro de la org | Motor de búsqueda público |

**Regla práctica:** Si el paquete es para uso interno del equipo → GitHub Packages. Si es para la comunidad → NuGet.org.

---

## Errores frecuentes de los participantes

| Error | Causa | Solución rápida |
|-------|-------|-----------------|
| `git push origin v1.0.0` dice "already exists" | El tag ya existe en el remote | `git push origin --delete v1.0.0` y volver a subir |
| `gh release create` falla con "tag not found" | No subieron el tag primero | `git push origin v1.0.0` antes de crear el release |
| Workflow `publish.yml` no se dispara | El trigger no es `release: [published]` o el release se creó como draft | Verificar trigger y que el release esté publicado (no draft) |
| `dotnet nuget push` da 401/403 | Falta `permissions: packages: write` en el workflow, o la org restringe Packages | Agregar permisos o verificar con admin de la org |
| `dotnet pack` genera versión `1.0.0` siempre | No están pasando `-p:PackageVersion` con la versión del tag | Verificar el step de extracción de versión |
| No ven el paquete en GitHub | Puede tardar unos segundos en aparecer | Refrescar la pestaña Packages |
| Error al consumir: "Unable to load the service index" | Source de NuGet mal configurado o credenciales inválidas | Verificar URL y re-autenticar con `gh auth token` |

---

## Tiempos recomendados

| Ejercicio | Tiempo | Prioridad |
|-----------|--------|-----------|
| 4.1 — Tags y SemVer | 10 min | **Máxima** |
| 4.2 — GitHub Release | 5 min | Alta |
| 4.3 — Workflow de publicación | 5 min | Alta (demo si Packages no disponible) |
| 4.4 — Consumir paquete | 5 min | Media (puede ser conceptual) |

Si necesitas recortar: 4.4 puede ser una explicación de 2 minutos sin demo. 4.3 puede ser demo del instructor si el tiempo es muy limitado.

---

## Cierre del Workshop

Después de este módulo, recapitula todo el trayecto:

1. **Módulo 1:** Diseñaron pipelines con jobs encadenados, outputs y artefactos. Aprendieron a diagnosticar errores.
2. **Módulo 2:** Usaron matrix strategy para probar múltiples combinaciones eficientemente.
3. **Módulo 3:** Configuraron políticas que convierten el pipeline de decorativo a obligatorio.
4. **Módulo 4:** Cerraron el ciclo con tags, releases y distribución como paquetes NuGet.

**Mensaje final sugerido:** "Ahora tienen las herramientas para llevar un repositorio de 'funciona en mi máquina' a 'escala con el equipo'. No se trata de memorizar YAML — se trata de entender qué problema resuelve cada pieza y cuándo aplicarla."

Invita a los participantes a revisar la sección de **Recursos adicionales** del README principal para profundizar: composite actions, reusable workflows avanzados, environments con approval gates, Dependabot, CodeQL y secret scanning.
