# ⚙️ Módulo 1: GitHub Actions — Jobs encadenados, outputs y artefactos

![Duración](https://img.shields.io/badge/Duración-60%20min-blue)
![Dificultad](https://img.shields.io/badge/Dificultad-Intermedio-orange)

## 🎯 Objetivos

- ✅ Entender dependencias entre jobs con `needs`
- ✅ Pasar valores entre jobs con `outputs`
- ✅ Pasar archivos entre jobs con `upload-artifact` / `download-artifact`
- ✅ Generar resúmenes visuales con `$GITHUB_STEP_SUMMARY`
- ✅ Diagnosticar errores comunes en workflows sin ejecutarlos

---

## Contexto

La diferencia entre un pipeline que funciona y uno que escala está en cómo estructuras los jobs: qué depende de qué, qué información pasa entre ellos y qué artefactos produces. Este módulo cubre esas tres cosas con el pipeline real de este repositorio, que corre `dotnet format`, `dotnet build` y `dotnet test` en jobs separados con dependencias explícitas.

---

## Ejercicio 1.1 — Leer el pipeline antes de ejecutarlo (15 min)

Abre `.github/workflows/ci.yml` y responde:

1. ¿Qué job se ejecuta primero? ¿Por qué no hay un job de restore independiente?
2. ¿Qué impediría que el job `test` corra si `build` falla?
3. ¿Qué valor está pasando el job `format` al job `build` mediante `outputs`? ¿Dónde lo consume `build`?
4. ¿En qué casos corre el job `summary`? Presta atención a `if: always()`.
5. ¿Qué diferencia hay entre el artefacto `coverage-report` y `test-results`? ¿Por qué uno usa `if: always()` y el otro no?

**Sin ejecutar nada todavía.** El objetivo es leer YAML como documentación.

### Conceptos clave

**`needs`** define dependencias explícitas. Sin él, los jobs corren en paralelo y no hay garantía de orden.

```yaml
test:
  needs: build   # test espera a que build termine con éxito
```

**`outputs`** permite exponer valores entre jobs. Los jobs corren en runners separados, así que no comparten variables de entorno ni sistema de archivos.

```yaml
jobs:
  format:
    outputs:
      dotnet-version: ${{ steps.setup-dotnet.outputs.dotnet-version }}
    steps:
      - id: setup-dotnet
        uses: actions/setup-dotnet@v4
        ...

  build:
    needs: format
    steps:
      - run: echo "Versión de .NET: ${{ needs.format.outputs.dotnet-version }}"
```

**`upload-artifact` / `download-artifact`** pasan archivos entre jobs. Cada job corre en un runner limpio sin acceso al sistema de archivos de los otros jobs.

---

## Ejercicio 1.2 — Correr el pipeline y revisar los artefactos (15 min)

1. Clona tu repositorio (si no lo hiciste en la preparación):

```bash
# Reemplaza TU_ORG y TU_USUARIO con tus valores
git clone https://github.com/TU_ORG/workshop-github-intermedio-TU_USUARIO.git
cd workshop-github-intermedio-TU_USUARIO
dotnet restore
dotnet build
dotnet test
```

2. Crea una rama y dispara el workflow:

```bash
git checkout -b feature/primer-ejercicio
echo "// ejercicio" >> src/FinancialUtils/Calculator.cs
# Esto romperá dotnet format --verify-no-changes porque agrega contenido fuera de estilo
```

En lugar de agregar un comentario directamente, haz un cambio válido:

```bash
git checkout -b feature/primer-ejercicio
# Modifica el XML del csproj: agrega un Description al PropertyGroup
git add .
git commit -m "feat: primer ejercicio del workshop"
git push origin feature/primer-ejercicio
```

3. Ve a la pestaña **Actions** en GitHub y observa:
   - El orden de ejecución de los jobs
   - Qué pasa si `format` falla: ¿llegan a correr `build` y `test`?
   - El contenido del **Step Summary** generado por el job `summary`

4. Descarga el artefacto `coverage-report`. Dentro encontrarás un XML de cobertura en formato Cobertura (el que genera `XPlat Code Coverage`). En un pipeline real se convierte a HTML con herramientas como ReportGenerator.

**Punto de discusión:** ¿Por qué el job `test` vuelve a hacer `dotnet restore` si `build` ya lo hizo? ¿Qué pasaría si el job `build` subiera los binarios como artefacto y `test` los descargara?

---

## Ejercicio 1.3 — Agregar conteo de pruebas al Step Summary (15 min)

Modifica `.github/workflows/ci.yml` para que el job `test` exponga como output la cantidad de pruebas ejecutadas, y que el job `summary` lo muestre en el Step Summary.

Puedes extraer el número del archivo `.trx` que genera `dotnet test`:

```bash
# El archivo .trx es XML. Con grep simple:
TOTAL=$(grep -oP 'total="\K[^"]+' coverage/**/*.trx | head -1)
echo "total-tests=$TOTAL" >> $GITHUB_OUTPUT
```

El job `summary` debería mostrar algo como:

```
| Pruebas ejecutadas | 35 |
```

---

## Ejercicio 1.4 — Diagnosticar el workflow roto (15 min)

Abre `exercises/01-actions-jobs/workflow-roto.yml`. Tiene **4 errores intencionales**. Encuéntralos sin ejecutar el archivo.

> 💡 **Tip:** Los errores son de configuración, no de lógica de negocio. Busca typos, nombres de referencia incorrectos y rutas que no existen.

Anota cada error y su corrección antes de revisar `exercises/01-actions-jobs/solucion.md`.

Si necesitas más de 10 minutos para encontrar los 4, revisa la solución y entiende por qué cada uno falla. Estos patrones se repiten en proyectos reales.

### Pistas (solo si te atoras)

<details>
<summary>🔍 Pista 1</summary>
Revisa el trigger <code>on.push</code> — ¿la clave está en singular o plural?
</details>

<details>
<summary>🔍 Pista 2</summary>
Verifica el nombre del runner — ¿está escrito exactamente como lo espera GitHub?
</details>

<details>
<summary>🔍 Pista 3</summary>
El job <code>test</code> declara una dependencia con <code>needs</code> — ¿el nombre del job referenciado coincide con el nombre real?
</details>

<details>
<summary>🔍 Pista 4</summary>
¿La ruta del artefacto de cobertura coincide con el directorio donde <code>dotnet test</code> genera los resultados?
</details>

---

## Ejercicio 1.5 — Crear un workflow reutilizable (15 min)

Un **reusable workflow** es un workflow que otros workflows pueden invocar con `uses`, igual que una acción. Esto elimina duplicación cuando varios repositorios (o varios workflows del mismo repo) comparten la misma lógica.

### Conceptos clave

```yaml
# El workflow reutilizable se define con workflow_call como trigger
on:
  workflow_call:
    inputs:
      dotnet-version:
        description: 'Versión de .NET a usar'
        required: true
        type: string
    secrets:
      token:
        required: false
```

```yaml
# El workflow que lo consume lo invoca con uses apuntando al archivo
jobs:
  ci:
    uses: ./.github/workflows/reusable-build.yml
    with:
      dotnet-version: '9.0.x'
```

### Ejercicio: extraer build + test a un workflow reutilizable

1. Crea `.github/workflows/reusable-build.yml`:

```yaml
name: Reusable Build & Test

on:
  workflow_call:
    inputs:
      dotnet-version:
        description: 'Versión de .NET'
        required: true
        type: string
      configuration:
        description: 'Configuración de build'
        required: false
        type: string
        default: 'Release'
    outputs:
      test-result:
        description: 'Resultado de los tests'
        value: ${{ jobs.build-and-test.outputs.test-result }}

jobs:
  build-and-test:
    name: Build & Test (.NET ${{ inputs.dotnet-version }})
    runs-on: ubuntu-latest
    outputs:
      test-result: ${{ steps.test.outcome }}
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration ${{ inputs.configuration }} --no-restore

      - name: Test
        id: test
        run: dotnet test --configuration ${{ inputs.configuration }} --no-restore
```

2. Crea `.github/workflows/ci-reusable.yml` que lo consuma:

```yaml
name: CI (Reusable)

on:
  workflow_dispatch:

jobs:
  dotnet9:
    uses: ./.github/workflows/reusable-build.yml
    with:
      dotnet-version: '9.0.x'

  dotnet8:
    uses: ./.github/workflows/reusable-build.yml
    with:
      dotnet-version: '8.0.x'
      configuration: 'Debug'

  summary:
    runs-on: ubuntu-latest
    needs: [dotnet9, dotnet8]
    if: always()
    steps:
      - name: Resumen
        run: |
          echo "## Resultados" >> $GITHUB_STEP_SUMMARY
          echo "| Versión | Resultado |" >> $GITHUB_STEP_SUMMARY
          echo "|---------|-----------|" >> $GITHUB_STEP_SUMMARY
          echo "| .NET 9 | ${{ needs.dotnet9.outputs.test-result }} |" >> $GITHUB_STEP_SUMMARY
          echo "| .NET 8 | ${{ needs.dotnet8.outputs.test-result }} |" >> $GITHUB_STEP_SUMMARY
```

3. Haz push y dispara el workflow manualmente desde la pestaña **Actions > CI (Reusable) > Run workflow**.

### Preguntas para reflexionar

1. ¿Qué ventaja tiene un reusable workflow vs. copiar los mismos steps en varios workflows?
2. ¿Cuál es la diferencia entre un reusable workflow y una composite action?
3. ¿Puede un reusable workflow vivir en otro repositorio? ¿Qué necesitarías configurar?

### Restricciones importantes

- Un workflow que usa `workflow_call` **no puede tener otros triggers** en el mismo archivo.
- Los reusable workflows solo pueden anidarse hasta **4 niveles** de profundidad.
- El caller y el reusable workflow deben estar en la **misma organización** (o el repo del reusable debe ser público).

---

## 🛠️ Troubleshooting del Módulo

| Problema | Solución |
|----------|----------|
| El workflow no se dispara | Verifica que la rama siga el patrón `feature/**` o `fix/**` |
| El job `build` no corre | Revisa que `format` haya pasado — `needs` bloquea si el job previo falla |
| El artefacto se sube vacío | Verifica la ruta en `path:` y que `dotnet test` genere los archivos esperados |
| El Step Summary no se ve | Busca la pestaña "Summary" en el run de Actions, no en los logs del job |
| `grep` no encuentra el archivo `.trx` | Verifica la ruta `coverage/**/*.trx` — puede variar según la versión de .NET |
| El reusable workflow no se encuentra | Verifica que la ruta en `uses:` sea relativa al repo: `./.github/workflows/archivo.yml` |
| El workflow reutilizable no se dispara | Confirma que el trigger sea `workflow_call` (no `workflow_dispatch`) |

---

## ✅ Verificación

Al terminar este módulo, deberías poder responder:

1. ¿Qué diferencia hay entre `outputs` y `upload-artifact`?
2. ¿Por qué el job `summary` usa `if: always()`?
3. ¿Cuántos de los 4 errores del workflow roto encontraste antes de ver la solución?
4. ¿Cuándo usarías un reusable workflow en lugar de copiar steps entre workflows?

> 📝 **Para el instructor:** El Ejercicio 1.4 (workflow roto) es el de mayor impacto práctico. El 1.5 (reusable workflow) es ideal si el grupo avanza rápido. Si el tiempo es limitado, prioriza 1.4 sobre 1.3 y 1.5.
