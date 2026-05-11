# Solución: workflow-roto.yml

## Errores y correcciones

**Error 1 — `branch:` en lugar de `branches:`**

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

GitHub Actions no reconoce `branch` en singular. El workflow simplemente no se dispara y no hay ningún error visible en la interfaz, lo que lo hace especialmente difícil de detectar. La única pista es que el workflow nunca aparece en la pestaña Actions al hacer push.

---

**Error 2 — Typo en el runner: `ubuntu-latests`**

```yaml
# Incorrecto
runs-on: ubuntu-latests

# Correcto
runs-on: ubuntu-latest
```

GitHub devuelve un error de "No hosted runner found with the specified labels". Este tipo de typo es fácil de introducir al copiar y pegar entre workflows.

---

**Error 3 — `needs` referencia un job que no existe**

```yaml
# Incorrecto
needs: formatting

# Correcto
needs: format
```

El job se llama `format`, no `formatting`. GitHub Actions falla en tiempo de ejecución con un error de dependencia. No hay validación estática antes de que el workflow corra.

---

**Error 4 — Ruta incorrecta del artefacto de cobertura**

```yaml
# Incorrecto
path: reports/coverage/

# Correcto
path: coverage/
```

`dotnet test --results-directory ./coverage` genera la cobertura en `coverage/` en la raíz. La ruta `reports/coverage/` no existe y `upload-artifact` falla con `if-no-files-found: error` (el comportamiento por defecto).

---

## Por qué estos errores son comunes

El Error 1 es el más peligroso porque falla silenciosamente. Los Errores 2 y 3 fallan en ejecución con mensajes claros. El Error 4 depende de cómo esté configurado el `if-no-files-found`: con `warn` el artefacto se sube vacío y el job pasa, lo que puede enmascarar el problema durante semanas.

La extensión de GitHub Actions para VS Code valida sintaxis YAML básica, pero no resuelve el Error 3 (referencias entre jobs) ni el Error 4 (rutas de archivos).
