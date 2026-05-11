# Soluciones — Módulo 1 (cont.): Matrix builds y artefactos

## Ejercicio 2.1 — Análisis del matrix

El matrix en `.github/workflows/matrix.yml` genera:

- **OS:** `ubuntu-latest`, `windows-latest` → 2
- **Versiones .NET:** `8.0.x`, `9.0.x` → 2
- **Total sin exclusiones:** 2 × 2 = 4 jobs
- **Exclusiones:** `windows-latest` + `8.0.x` → se elimina 1
- **Total final:** 3 jobs

El `fail-fast: false` hace que **todos** los jobs corran aunque uno falle. Esto es útil en matrices de compatibilidad porque quieres saber **todas** las combinaciones que fallan, no solo la primera.

---

## Ejercicio 2.2 — Agregar una dimensión

Para agregar `configuration: [Debug, Release]`, el matrix se modifica así:

```yaml
strategy:
  fail-fast: false
  matrix:
    os: [ubuntu-latest, windows-latest]
    dotnet-version: ['8.0.x', '9.0.x']
    configuration: [Debug, Release]
    exclude:
      - os: windows-latest
        dotnet-version: '8.0.x'
```

**Cálculo:**
- Sin exclusiones: 2 × 2 × 2 = 8
- Exclusiones: `windows-latest` + `8.0.x` elimina **2** jobs (uno Debug y uno Release)
- **Total: 6 jobs**

El step de build debe usar la variable:

```yaml
- run: dotnet build --configuration ${{ matrix.configuration }}
```

Y el nombre del artefacto debe incluir la configuración:

```yaml
- name: Upload test results
  uses: actions/upload-artifact@v4
  with:
    name: test-results-${{ matrix.os }}-${{ matrix.dotnet-version }}-${{ matrix.configuration }}
    path: coverage/
```

---

## Ejercicio 2.3 — Descargar artefactos con `gh`

```bash
# Obtener el ID del último run del workflow matrix
RUN_ID=$(gh run list --workflow=matrix.yml --limit=1 --json databaseId --jq '.[0].databaseId')

# Descargar todos los artefactos del run
gh run download $RUN_ID

# Descargar solo un artefacto específico
gh run download $RUN_ID --name test-results-ubuntu-latest-8.0.x-Release

# Ver la estructura de lo descargado
find . -name "*.trx" -type f
```
