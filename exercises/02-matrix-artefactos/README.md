# 🔀 Módulo 1 (cont.): Matrix builds y estrategia de artefactos

![Duración](https://img.shields.io/badge/Duración-30%20min-blue)
![Dificultad](https://img.shields.io/badge/Dificultad-Intermedio-orange)

## 🎯 Objetivos

- ✅ Entender cómo funciona `matrix strategy` para probar múltiples combinaciones
- ✅ Usar `exclude` para eliminar combinaciones innecesarias
- ✅ Diferenciar `fail-fast: true` vs `fail-fast: false`
- ✅ Agregar dimensiones al matrix y calcular el total de jobs
- ✅ Descargar artefactos de múltiples combinaciones con GitHub CLI

---

## Contexto

El matrix strategy sirve para correr el mismo job contra múltiples combinaciones de variables sin duplicar código. En .NET es especialmente relevante porque una librería puede necesitar certificarse contra múltiples versiones del runtime. Este ejercicio usa el matrix de `matrix.yml`.

---

## Ejercicio 2.1 — Analizar el matrix existente (10 min)

Abre `.github/workflows/matrix.yml` y responde:

1. ¿Cuántos jobs en total genera este matrix? Haz el cálculo antes de correrlo.
2. ¿Por qué existe el bloque `exclude`? ¿Qué problema concreto resuelve?
3. ¿Qué diferencia hay entre `fail-fast: true` y `fail-fast: false`? ¿Cuándo usarías cada uno en un proyecto real?
4. Los artefactos se suben con `if: always()`. ¿Por qué es importante eso si `fail-fast: false` ya evita que el pipeline pare?

**Cálculo esperado:**
- 2 sistemas operativos × 2 versiones de .NET = 4 combinaciones
- Menos 1 excluida (windows + .NET 8) = **3 jobs**

---

## Ejercicio 2.2 — Agregar una dimensión al matrix (10 min)

Modifica `matrix.yml` para agregar una dimensión `configuration` con valores `Release` y `Debug`.

```yaml
matrix:
  os: [ubuntu-latest, windows-latest]
  dotnet-version: ['8.0.x', '9.0.x']
  configuration: [Release, Debug]
  exclude:
    - os: windows-latest
      dotnet-version: '8.0.x'
    - configuration: Debug
      os: windows-latest   # Debug solo en Linux para reducir costo
```

Actualiza los pasos para usar `${{ matrix.configuration }}` en lugar del valor hardcodeado `Release`.

Calcula cuántos jobs genera este matrix antes de hacer push.

---

## Ejercicio 2.3 — Descargar múltiples artefactos del matrix (10 min)

Después de que el matrix corra, cada combinación sube su propio artefacto:

```
results-dotnet8.0.x-ubuntu-latest
results-dotnet9.0.x-ubuntu-latest
results-dotnet8.0.x-windows-latest
...
```

Con GitHub CLI puedes descargarlos todos:

```bash
# Obtén el ID del último run del workflow de matrix
RUN_ID=$(gh run list --workflow=matrix.yml --limit=1 --json databaseId --jq '.[0].databaseId')

# Descarga todos los artefactos del run en carpetas separadas
gh run download $RUN_ID

# Lista lo que se descargó
ls -la
```

**Punto de discusión:** Si necesitaras consolidar los reportes de cobertura de todas las combinaciones en un solo reporte HTML, ¿cómo lo estructurarías? ReportGenerator soporta múltiples archivos de entrada. ¿Agregarías un job final con `needs` al matrix completo?

---

## Nota sobre .NET 8 y versiones LTS en el matrix

.NET 8 es la versión LTS actual con soporte hasta noviembre 2026. Tenerlo en el matrix junto con .NET 9 (STS) tiene sentido para garantizar compatibilidad con ambas versiones. La decisión de cuándo remover una versión del matrix es de producto, no de infraestructura.

La lista de versiones de .NET soportadas está en: https://dotnet.microsoft.com/platform/support/policy/dotnet-core

---

## 🛠️ Troubleshooting del Módulo

| Problema | Solución |
|----------|----------|
| El matrix genera más/menos jobs de lo esperado | Recalcula: (OS × versiones × config) - exclusiones |
| Los artefactos se sobrescriben | Verifica que el nombre incluya `${{ matrix.dotnet-version }}-${{ matrix.os }}` para que sea único |
| `gh run download` no encuentra artefactos | Verifica el `RUN_ID` con `gh run list --workflow=matrix.yml` |
| `dotnet test` falla en .NET 8 | Es esperado si el proyecto usa APIs de .NET 9+. Esto es parte del aprendizaje |
| El workflow dispatch no aparece | Verifica que `workflow_dispatch:` esté en el trigger del workflow |

---

## ✅ Verificación

Al terminar este módulo, deberías poder responder:

1. ¿Cuántos jobs genera un matrix de 2 OS × 3 versiones × 2 configuraciones con 2 exclusiones?
2. ¿Cuándo usarías `fail-fast: false` vs `fail-fast: true`?
3. ¿Por qué los artefactos del matrix se suben con `if: always()`?
4. ¿Cómo consolidarías reportes de cobertura de múltiples combinaciones en un solo reporte?

> 📝 **Para el instructor:** El Ejercicio 2.1 (análisis del matrix) es el más valioso conceptualmente. Si el tiempo es corto, el 2.3 (descargar artefactos) puede hacerse como demostración en vivo.
