# 🎓 Guía del Instructor — Módulo 2: Matrix Builds y Artefactos

**Duración:** 30 minutos (ejercicios 2.1 a 2.3)
**Archivo clave:** `.github/workflows/matrix.yml`

---

## Objetivo pedagógico

Que los participantes entiendan que un matrix strategy no es magia — es un producto cartesiano de variables que genera N jobs automáticamente. Deben poder calcular cuántos jobs genera un matrix **antes** de ejecutarlo, y entender cómo `exclude` y `fail-fast` modifican ese comportamiento.

---

## Antes de empezar

### Material que debes tener listo

- El archivo `.github/workflows/matrix.yml` abierto en pantalla.
- Una calculadora mental del producto cartesiano (2 × 2 = 4, menos 1 exclusión = 3).
- La pestaña **Actions** con un run del matrix ya completado para mostrar cómo se ven los 3 jobs.

### Conexión con el módulo anterior

Empieza diciendo: "En el módulo anterior vimos cómo encadenar jobs con `needs`. Pero ¿qué pasa si necesitas correr los mismos tests en Linux y Windows? ¿Y contra .NET 8 y .NET 9? ¿Copias el job 4 veces?" La respuesta es no — usas `strategy.matrix`.

---

## Ejercicio 2.1 — Analizar el matrix existente (10 min) ⭐ PRIORIDAD ALTA

### Qué explicar

Proyecta `matrix.yml` y recorre su estructura:

```yaml
strategy:
  fail-fast: false
  matrix:
    os: [ubuntu-latest, windows-latest]
    dotnet-version: ['8.0.x', '9.0.x']
    exclude:
      - os: windows-latest
        dotnet-version: '8.0.x'
```

#### 1. El producto cartesiano

Dibuja la tabla en una pizarra o proyector:

```
                 .NET 8.0.x    .NET 9.0.x
ubuntu-latest       ✅             ✅
windows-latest      ❌             ✅
```

- Sin exclusiones: 2 × 2 = 4 jobs.
- Con la exclusión de `windows-latest` + `.NET 8.0.x`: 4 - 1 = **3 jobs**.

**Pregunta al grupo antes de dar la respuesta:** "¿Cuántos jobs genera este matrix?" Deja que calculen. Si alguien dice 4, pregunta: "¿Y el bloque `exclude`?"

#### 2. Por qué excluir windows + .NET 8

Explica el razonamiento práctico:
- .NET 8 es LTS (soporte hasta noviembre 2026), así que queremos verificar compatibilidad.
- Pero no necesitamos **todas** las combinaciones — correr en Windows consume más minutos de Actions (Windows es ~2x más lento que Linux en runners hosted).
- Excluir una combinación que aporta poco valor ahorra costo y tiempo.

#### 3. `fail-fast: true` vs `fail-fast: false`

| Comportamiento | `fail-fast: true` (default) | `fail-fast: false` |
|---------------|---------------------------|-------------------|
| Un job falla | Cancela los demás inmediatamente | Los demás siguen corriendo |
| Cuándo usar | Desarrollo rápido — quieres feedback inmediato | Matrices de compatibilidad — quieres saber **todo** lo que falla |

**Analogía útil:** `fail-fast: true` es como un examen donde te sacan al primer error. `fail-fast: false` es como un examen completo donde al final ves todas las preguntas que fallaste.

En nuestro caso, `fail-fast: false` tiene sentido porque si los tests fallan en .NET 8 pero pasan en .NET 9, queremos saber **ambas** cosas, no que GitHub cancele el job de .NET 9 porque el de .NET 8 falló primero.

#### 4. Artefactos con `if: always()`

El step de `upload-artifact` tiene `if: always()`. Esto es crítico: si los tests fallan, queremos el `.trx` para diagnosticar qué falló. Sin `if: always()`, un test rojo haría que el step de upload no corra y perderíamos la evidencia.

**Pregunta provocadora:** "Si ya tenemos `fail-fast: false`, ¿por qué necesitamos también `if: always()` en el upload?" Respuesta: `fail-fast` controla si los **otros jobs** se cancelan. `if: always()` controla si un **step dentro del mismo job** corre aunque un step anterior falle. Son independientes.

### Respuestas esperadas

| Pregunta | Respuesta |
|----------|-----------|
| ¿Cuántos jobs genera? | 3 (2×2=4, menos 1 exclusión) |
| ¿Por qué el exclude? | Ahorrar minutos de CI. Windows + .NET 8 aporta poco valor vs. el costo |
| ¿Cuándo `fail-fast: false`? | Matrices de compatibilidad donde quieres el panorama completo |
| ¿Por qué `if: always()` en upload? | Para capturar artefactos aunque los tests fallen |

---

## Ejercicio 2.2 — Agregar dimensión al matrix (10 min)

### Qué explicar

Este ejercicio pide agregar `configuration: [Release, Debug]` al matrix. Es importante que los participantes **calculen antes de ejecutar**.

#### Cálculo paso a paso

Sin exclusiones nuevas:
- 2 OS × 2 versiones × 2 configuraciones = **8 jobs**
- Exclusión existente: `windows-latest` + `.NET 8.0.x` — esto elimina **2 jobs** (uno Debug, uno Release)
- Exclusión nueva sugerida: `Debug` + `windows-latest` — esto elimina **1 job más** (windows + .NET 9 + Debug)
- **Total: 5 jobs**

Pero la solución del ejercicio propone estas exclusiones:

```yaml
exclude:
  - os: windows-latest
    dotnet-version: '8.0.x'    # Elimina 2 (Debug y Release en Windows/.NET 8)
  - configuration: Debug
    os: windows-latest         # Elimina 1 (Debug en Windows/.NET 9)
```

Total final: 8 - 2 - 1 = **5 jobs**.

> **Nota:** La solución en `solucion.md` dice 6 jobs. Esto es porque el cálculo depende de si las exclusiones se aplican en conjunto o individualmente. Si la primera exclusión ya eliminó ambas combinaciones de windows+.NET 8 (Debug y Release), y la segunda elimina Debug+windows (que solo queda .NET 9), son 8 - 2 - 1 = 5. **Recalcula con el grupo** — el valor pedagógico está en el proceso, no en el número exacto.

#### Cambio en el build step

Enfatiza que deben cambiar:

```yaml
# Antes (hardcodeado)
run: dotnet build --configuration Release

# Después (usa la variable del matrix)
run: dotnet build --configuration ${{ matrix.configuration }}
```

Y en el nombre del artefacto:

```yaml
name: results-dotnet${{ matrix.dotnet-version }}-${{ matrix.os }}-${{ matrix.configuration }}
```

Si no agregan la configuración al nombre, los artefactos se **sobrescriben** entre sí (Release sobrescribe Debug del mismo OS/versión).

### Error frecuente

Los participantes olvidan actualizar el nombre del artefacto. Si dos combinaciones producen un artefacto con el mismo nombre, `upload-artifact@v4` falla (en v3 sobrescribía silenciosamente — v4 requiere nombres únicos).

---

## Ejercicio 2.3 — Descargar artefactos con gh CLI (10 min)

### Qué explicar

Este ejercicio demuestra cómo interactuar con artefactos de CI desde la terminal. Es una habilidad práctica que muchos participantes no conocen.

#### Comandos clave

```bash
# Listar runs del workflow matrix
gh run list --workflow=matrix.yml --limit=5

# Obtener el ID del último run
RUN_ID=$(gh run list --workflow=matrix.yml --limit=1 --json databaseId --jq '.[0].databaseId')

# Descargar todos los artefactos
gh run download $RUN_ID

# Ver lo que se descargó
ls -la
find . -name "*.trx" -type f
```

#### Demo en vivo sugerida

1. Ejecuta `gh run list --workflow=matrix.yml` para mostrar los runs disponibles.
2. Descarga los artefactos de un run completado.
3. Muestra las carpetas que se crean — una por cada combinación del matrix.
4. Abre un archivo `.trx` y muestra que es XML con los resultados de los tests.

### Punto de discusión valioso

> "Si necesitaran consolidar los reportes de cobertura de las 3 combinaciones en un solo reporte HTML, ¿cómo lo harían?"

**Respuesta guiada:**
1. Agregarías un job final que dependa del matrix completo: `needs: [test-matrix]`.
2. Descargarías todos los artefactos con `download-artifact`.
3. Usarías ReportGenerator para combinar los XMLs de Cobertura en un solo HTML.
4. Subirías el HTML como artefacto final.

No es necesario implementarlo — solo que entiendan la arquitectura.

---

## Errores frecuentes de los participantes

| Error | Causa | Solución rápida |
|-------|-------|-----------------|
| "El matrix genera un número diferente al esperado" | No contaron las exclusiones correctamente | Dibujar la tabla OS × versiones y tachar las exclusiones |
| Artefactos con nombres duplicados | No incluyeron todas las dimensiones del matrix en el nombre | Agregar `${{ matrix.configuration }}` al nombre |
| `dotnet test` falla en .NET 8 | El proyecto usa APIs de .NET 9 que no existen en .NET 8 | **Esto es intencional.** Explica que es exactamente lo que el matrix detecta |
| `gh run download` no encuentra artefactos | El run no ha terminado, o los artefactos expiraron | Verificar con `gh run view $RUN_ID` que el run terminó |
| `workflow_dispatch` no aparece en la UI | El workflow no está en la rama `main` | Hacer push del `matrix.yml` a `main` primero |

---

## Concepto avanzado: .NET 8 vs .NET 9 en el matrix

Si algún participante pregunta "¿por qué tener .NET 8 en el matrix si el proyecto es .NET 9?":

- **Class libraries** pueden multi-targetear (.NET 8 + .NET 9). Si la librería va a publicarse como paquete NuGet, los consumidores pueden estar en .NET 8.
- En nuestro caso, el `.csproj` solo tiene `<TargetFramework>net9.0</TargetFramework>`, así que el build en .NET 8 **probablemente falle**. Esto está bien — demuestra que el matrix detecta incompatibilidades.
- Si quisiéramos multi-target real, cambiaríamos a `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>` (plural).

---

## Tiempos recomendados

| Ejercicio | Tiempo | Prioridad |
|-----------|--------|-----------|
| 2.1 — Analizar el matrix | 10 min | **Máxima** |
| 2.2 — Agregar dimensión | 10 min | Alta |
| 2.3 — Descargar artefactos | 10 min | Media (puede ser demo) |

Si necesitas recortar, el 2.3 puede hacerse como demostración en vivo en 3 minutos.

---

## Transición al Módulo 3 (Branch Protection)

Cierra diciendo: "Ya saben diseñar pipelines con jobs encadenados y matrix strategy. Pero ¿de qué sirve un pipeline perfecto si cualquiera puede hacer merge a `main` ignorando un test rojo? El pipeline es necesario pero no suficiente — necesitan **políticas** que obliguen a respetarlo."
