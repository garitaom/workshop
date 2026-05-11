# 🎓 Guía del Instructor — Módulo 1: GitHub Actions (Jobs, Outputs, Artefactos)

**Duración:** 30 minutos (ejercicios 1.1 a 1.4) + 15 min opcionales (1.5)
**Archivos clave:** `.github/workflows/ci.yml`, `exercises/01-actions-jobs/workflow-roto.yml`

---

## Objetivo pedagógico

Que los participantes pasen de "copiar YAML de internet" a **leer y entender** un pipeline como documentación técnica. El módulo no empieza escribiendo código — empieza leyendo. Esto es intencional: en proyectos reales, el 80% del trabajo con CI es diagnosticar pipelines existentes, no crear nuevos.

---

## Antes de empezar

### Verificaciones previas

- Confirma que todos los participantes tienen su repositorio creado y clonado (paso de preparación del README).
- Ejecuta `dotnet build && dotnet test` en tu propio repo para confirmar que el entorno está sano.
- Ten abierto el workflow `ci.yml` en tu editor y en la pestaña Actions de GitHub para hacer demos en vivo.

### Material que debes tener listo

- El archivo `.github/workflows/ci.yml` abierto en pantalla.
- La pestaña **Actions** de tu repo de demostración con al menos un run exitoso.
- La solución en `exercises/01-actions-jobs/solucion.md` abierta (sin proyectar).

---

## Ejercicio 1.1 — Leer el pipeline (15 min)

### Qué explicar antes de que empiecen

Proyecta el `ci.yml` y haz un recorrido de alto nivel:

1. **Estructura general:** El pipeline tiene 4 jobs: `format` → `build` → `test` → `summary`. Dibuja el flujo en una pizarra o pide que lo visualicen.

2. **`needs` — por qué existe:** Sin `needs`, GitHub Actions ejecuta todos los jobs en paralelo. Explica que cada job corre en un runner limpio (una máquina virtual nueva). No comparten disco, ni variables de entorno, ni procesos. Esto sorprende a la gente que viene de Jenkins o Azure DevOps donde los stages comparten el workspace.

3. **`outputs` — cómo fluyen los datos:** El job `format` expone `dotnet-version` como output. El job `build` lo consume con `${{ needs.format.outputs.dotnet-version }}`. Enfatiza que esto solo sirve para valores pequeños (strings), no para archivos.

4. **`upload-artifact` / `download-artifact` — cómo fluyen los archivos:** El job `test` sube dos artefactos: `coverage-report` (el XML de cobertura) y `test-results` (el `.trx`). El job `summary` descarga `test-results`. Pregunta al grupo: "¿Por qué no pasa la cobertura con `outputs`?" (porque es un archivo, no un string).

5. **`if: always()` — cuándo se necesita:** El job `summary` tiene `if: always()`. Sin eso, si `test` falla, `summary` no correría y perderías el resumen. El artefacto `test-results` también tiene `if: always()` para capturar resultados incluso cuando los tests fallan.

### Respuestas esperadas

| Pregunta | Respuesta |
|----------|-----------|
| ¿Qué job se ejecuta primero? | `format`, porque los demás tienen `needs` que apuntan a él directa o indirectamente. |
| ¿Por qué no hay job de restore? | Porque `dotnet restore` es rápido y cada job lo necesita de todas formas (runners separados). Un job de restore independiente agregaría un nodo más al grafo sin beneficio. |
| ¿Qué impide que test corra si build falla? | `needs: build` — GitHub Actions no ejecuta un job si alguno de sus `needs` falló. |
| ¿Qué pasa con el output? | `format` expone la versión de .NET instalada. `build` lo usa en el nombre del step de setup (cosmético en este caso, pero demuestra el mecanismo). |
| ¿Por qué `test-results` usa `if: always()`? | Para capturar los resultados del `.trx` incluso cuando los tests fallan. Sin eso, un test rojo eliminaría la evidencia. |

### Error común de los participantes

Algunos confundirán `outputs` (valores) con `artifacts` (archivos). Aclara: `outputs` es como pasar un parámetro entre funciones; `artifacts` es como guardar un archivo en un bucket compartido.

---

## Ejercicio 1.2 — Correr el pipeline (15 min)

### Qué explicar

- El paso dice "crea una rama `feature/primer-ejercicio`". Es importante que la rama siga el patrón `feature/**` porque el trigger del CI solo corre en `main`, `feature/**` y `fix/**`. Si alguien crea una rama `ejercicio-1`, el workflow no se dispara. **Esto es un punto de aprendizaje intencional.**

- Explica brevemente el flujo: push → GitHub detecta el trigger → encola los jobs → cada job corre en un runner.

### Demo en vivo sugerida

1. Haz un cambio trivial (editar el `<Description>` del `.csproj`).
2. Push a `feature/demo-instructor`.
3. Abre la pestaña Actions y muestra en tiempo real:
   - El job `format` corriendo primero.
   - Los jobs encadenados esperando.
   - Clic en el job `summary` para ver el Step Summary renderizado.
   - Clic en "Artifacts" para descargar `coverage-report`.

### Punto de discusión importante

> "¿Por qué test vuelve a hacer dotnet restore si build ya lo hizo?"

**Respuesta:** Porque cada job corre en un runner **limpio**. El directorio `~/.nuget/packages` del job `build` no existe en el runner del job `test`. La alternativa sería subir los binarios como artefacto y que `test` los descargue, pero eso agrega complejidad y fragilidad (rutas relativas, permisos). Para proyectos pequeños, repetir `restore` es más simple y confiable.

---

## Ejercicio 1.3 — Step Summary con conteo de pruebas (15 min)

### Qué explicar

Este ejercicio es el más técnico del módulo. Los participantes necesitan:
1. Saber que `dotnet test --logger "trx"` genera un archivo `.trx` (XML).
2. Extraer un valor del XML con `grep`.
3. Escribir ese valor en `$GITHUB_OUTPUT` para exponerlo como output del job.
4. Consumir el output desde el job `summary`.

### Solución paso a paso (no la proyectes, úsala como referencia)

En el job `test`, después del step de pruebas:

```yaml
- name: Contar pruebas
  id: count
  run: |
    TOTAL=$(grep -oP 'total="\K[^"]+' coverage/**/*.trx | head -1)
    echo "total-tests=$TOTAL" >> $GITHUB_OUTPUT
```

El job `test` necesita exponer el output:

```yaml
test:
  outputs:
    total-tests: ${{ steps.count.outputs.total-tests }}
```

En el job `summary`, agregar la línea:

```yaml
echo "| Pruebas ejecutadas | ${{ needs.test.outputs.total-tests }} |" >> $GITHUB_STEP_SUMMARY
```

### Si el grupo se atora

- Recuerda que `grep -oP` usa PCRE (Perl-Compatible Regular Expressions). En macOS, el `grep` nativo no soporta `-P`. En GitHub Actions (ubuntu-latest) sí funciona.
- Si alguien pregunta por una alternativa, `grep -o 'total="[0-9]*"'` funciona sin `-P`.

### Cuándo saltar este ejercicio

Si el grupo va lento o tiene poca experiencia con bash/regex, este ejercicio puede convertirse en un cuello de botella. Muestra la solución como demo y pasa al 1.4 que tiene más impacto.

---

## Ejercicio 1.4 — Workflow roto (15 min) ⭐ PRIORIDAD ALTA

### Por qué este es el ejercicio más importante

En el mundo real, nadie escribe workflows desde cero. La habilidad más valiosa es **diagnosticar por qué un pipeline falla leyendo el YAML**, sin necesidad de hacer push y esperar 5 minutos a que GitHub te diga el error.

### Los 4 errores y cómo explicarlos

**Error 1: `branch:` en vez de `branches:`**

```yaml
# Incorrecto
on:
  push:
    branch:
      - main

# Correcto
on:
  push:
    branches:
      - main
```

**Impacto:** El workflow simplemente nunca se dispara. No hay error visible en la UI. GitHub ignora claves desconocidas silenciosamente. Este es el error más peligroso porque el feedback es "nada pasa" en vez de un mensaje de error.

**Cómo encontrarlo:** Comparar con la documentación oficial o con otro workflow que sí funcione. La extensión de GitHub Actions para VS Code puede detectar esto.

---

**Error 2: `ubuntu-latests` (typo en el runner)**

```yaml
# Incorrecto
runs-on: ubuntu-latests

# Correcto
runs-on: ubuntu-latest
```

**Impacto:** Error en tiempo de ejecución: "No hosted runner found with the specified labels". Es fácil de diagnosticar porque el mensaje es claro.

**Anécdota útil:** Este typo es tan común que hay memes al respecto. En equipos grandes, tener un template de workflow compartido evita este tipo de errores.

---

**Error 3: `needs: formatting` (referencia a job inexistente)**

```yaml
# Incorrecto — el job se llama "format", no "formatting"
needs: formatting

# Correcto
needs: format
```

**Impacto:** Error en tiempo de validación del workflow. GitHub dice que la dependencia no existe. No hay validación estática antes de que el workflow corra — solo lo ves cuando intentas ejecutarlo.

**Lección:** Los nombres de los jobs son las claves del bloque `jobs:`, no el valor de `name:`. El job se llama `format` (la clave), aunque su `name:` sea "Format check".

---

**Error 4: `path: reports/coverage/` (ruta incorrecta)**

```yaml
# Incorrecto
path: reports/coverage/

# Correcto
path: coverage/
```

**Impacto:** `dotnet test --results-directory ./coverage` genera los archivos en `coverage/`, no en `reports/coverage/`. El comportamiento depende de `if-no-files-found`:
- `error` (default): el job falla con un mensaje claro.
- `warn`: el artefacto se sube vacío y el job pasa — **esto puede enmascarar el problema durante semanas**.
- `ignore`: igual que `warn` pero sin advertencia.

**Lección:** Siempre usa `if-no-files-found: error` para artefactos críticos. Usa `ignore` solo para artefactos opcionales como logs de diagnóstico.

### Dinámica sugerida

1. Da 8-10 minutos para trabajar individualmente.
2. Pide a voluntarios que compartan los errores que encontraron.
3. Recorre los 4 errores explicando el impacto de cada uno.
4. Pregunta: "¿Cuál de estos 4 errores es el más difícil de detectar en un proyecto real?" (Respuesta esperada: Error 1, porque falla silenciosamente).

---

## Ejercicio 1.5 — Workflow reutilizable (15 min) — OPCIONAL

### Cuándo incluirlo

- El grupo avanza rápido y terminó el 1.4 en menos de 10 minutos.
- Hay participantes avanzados que ya conocen los conceptos básicos de Actions.
- Si el tiempo es limitado, este es el primer ejercicio que se sacrifica.

### Qué explicar

1. **El problema que resuelve:** Muestra dos workflows que comparten los mismos 5 steps de build+test. Pregunta: "¿Qué pasa cuando necesitas cambiar la versión de .NET? Tienes que editarlo en ambos archivos." Un reusable workflow centraliza esa lógica.

2. **`workflow_call` vs. `workflow_dispatch`:** Confusión muy común. `workflow_dispatch` es para ejecutar manualmente. `workflow_call` es para ser llamado por otro workflow. No pueden coexistir en el mismo archivo.

3. **Diferencia con composite actions:** Un reusable workflow es un workflow completo (con jobs y runners). Una composite action es una secuencia de steps que se inyecta dentro de un job existente. Regla general: si necesitas un runner propio o múltiples jobs, usa reusable workflow. Si solo son 3-4 steps reutilizables, usa composite action.

4. **Restricciones clave:**
   - Máximo 4 niveles de anidación.
   - Mismo org/enterprise o repo público.
   - Los secrets no se heredan automáticamente — necesitas `secrets: inherit` o declararlos explícitamente.

### Demo sugerida

Si haces demo en vivo, ten los dos archivos ya creados (`reusable-build.yml` y `ci-reusable.yml`). Dispara `ci-reusable.yml` con `workflow_dispatch` desde la pestaña Actions y muestra cómo los dos jobs "hijos" (dotnet9 y dotnet8) ejecutan el mismo workflow con parámetros diferentes.

---

## Errores frecuentes de los participantes

| Error | Causa | Solución rápida |
|-------|-------|-----------------|
| "El workflow no se dispara" | Rama no sigue el patrón `feature/**` o `fix/**` | Renombrar la rama: `git checkout -b feature/mi-rama` |
| "Permission denied" al hacer push | No tienen permisos de escritura en el repo | Verificar que crearon su propio repo, no están en el template |
| "dotnet format falla" | Agregaron código con formato incorrecto (intencional en 1.2) | Ejecutar `dotnet format` sin `--verify-no-changes` para corregir |
| Confunden job ID con job name | Usan "Format check" en `needs` en vez de `format` | Explicar que `needs` usa la clave del mapa YAML, no el `name:` |
| El Step Summary no aparece | Buscan en los logs del job en vez de en la pestaña Summary | Mostrar dónde está la pestaña Summary en la UI |

---

## Tiempos recomendados

| Ejercicio | Tiempo | Prioridad |
|-----------|--------|-----------|
| 1.1 — Leer el pipeline | 15 min | Alta |
| 1.2 — Correr y revisar artefactos | 15 min | Alta |
| 1.3 — Step Summary con conteo | 15 min | Media (puede ser demo) |
| 1.4 — Workflow roto | 15 min | **Máxima** |
| 1.5 — Workflow reutilizable | 15 min | Baja (opcional) |

Si necesitas recortar, sacrifica en este orden: 1.5 → 1.3 → 1.2. Nunca sacrifiques 1.1 ni 1.4.

---

## Transición al Módulo 2 (Matrix)

Cierra diciendo: "Ahora saben cómo encadenar jobs, pasar datos entre ellos y diagnosticar errores. Pero ¿qué pasa cuando necesitan probar contra 3 versiones de .NET en 2 sistemas operativos? ¿Copian el job 6 veces? No — usan matrix strategy."
