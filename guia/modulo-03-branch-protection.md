# 🎓 Guía del Instructor — Módulo 3: Branch Protection, CODEOWNERS y Merge Strategies

**Duración:** 25 minutos (ejercicios 3.1 a 3.5)
**Archivos clave:** `.github/CODEOWNERS`, `.github/pull_request_template.md`, `.github/ISSUE_TEMPLATE/`

---

## Objetivo pedagógico

Que los participantes entiendan que un pipeline de CI sin políticas de protección es **decorativo**. Cualquiera puede ignorar un test rojo y hacer merge. Las branch protection rules son el mecanismo técnico que convierte las buenas intenciones del equipo en reglas ejecutables. Este módulo cubre todo el ecosistema de políticas: protección de ramas, CODEOWNERS, templates, commits firmados y estrategias de merge.

---

## Antes de empezar

### Material que debes tener listo

- Acceso a **Settings > Branches** (o **Settings > Rules > Rulesets**) de tu repo de demostración.
- El archivo `.github/CODEOWNERS` abierto.
- Los templates de PR e issues listos para mostrar.
- Una clave SSH configurada para demo de commits firmados (o capturas de pantalla si no quieres hacer la demo en vivo).

### Nota sobre GitHub Enterprise vs GitHub.com

En GitHub Enterprise, la interfaz de branch protection puede diferir ligeramente:
- **GitHub.com (repos modernos):** Usa "Rulesets" como reemplazo de las branch protection rules clásicas.
- **GitHub Enterprise Server:** Puede tener la interfaz clásica o Rulesets, dependiendo de la versión.
- **Ambos funcionan conceptualmente igual.** Menciona la diferencia pero no pierdas tiempo en ello — el concepto es lo que importa.

---

## Ejercicio 3.1 — Branch Protection en `main` (5 min)

### Qué explicar

Este es un ejercicio de **configuración en la UI**, no de código. Los participantes van a Settings y configuran reglas.

#### Cada regla y por qué existe

**1. Require a pull request before merging**
- Impide hacer `git push` directo a `main`.
- Obliga a crear una rama, hacer PR y obtener aprobación.
- **Sin esto:** cualquier persona con permisos de escritura puede hacer push directo, saltándose el CI por completo.

**2. Required approvals: 1**
- Exige al menos una aprobación de otro colaborador antes de hacer merge.
- **Número recomendado:** 1 para equipos pequeños (2-5 personas), 2 para equipos grandes o código crítico.
- **Anécdota útil:** En muchos equipos, el número de approvals requeridos es inversamente proporcional a la velocidad de entrega. Un equipo que requiere 3 approvals tiene colas de PRs más largas.

**3. Dismiss stale pull request approvals when new commits are pushed**
- Si alguien aprueba un PR y después el autor hace un nuevo push, la aprobación se invalida.
- **Escenario concreto que debes explicar:** 
  1. Ana crea un PR con un cambio seguro. 
  2. Carlos lo revisa y aprueba.
  3. Ana hace un push con un cambio completamente diferente (sin mala intención, quizás corrigió un merge conflict).
  4. **Sin esta regla:** el PR sigue aprobado y Ana puede hacer merge. Carlos aprobó código que ya no es el que está en el PR.
  5. **Con esta regla:** Carlos tiene que volver a revisar.

**4. Require review from Code Owners**
- Conecta con el ejercicio 3.2 (CODEOWNERS). Si un PR modifica archivos cubiertos por CODEOWNERS, el owner de esa área **debe** ser quien apruebe.
- **Sin esto:** cualquier colaborador puede aprobar, aunque no tenga contexto del área modificada.

**5. Require status checks to pass**
- Los status checks son los jobs del CI: `Format check`, `Build`, `Test`.
- **IMPORTANTE:** Los nombres de los status checks deben coincidir exactamente con el campo `name:` del job en el workflow, no con la clave del job. En nuestro ci.yml:
  - Job `format` tiene `name: Format check` → el status check se llama "Format check".
  - Job `build` tiene `name: Build` → status check "Build".
  - Job `test` tiene `name: Test` → status check "Test".
- **Require branches to be up to date:** Si `main` avanzó desde que creaste tu rama, debes hacer merge/rebase antes de poder hacer merge del PR. Esto garantiza que los tests corren contra el estado real de `main`, no contra una versión desactualizada.

**6. Require signed commits**
- Cada commit debe tener una firma criptográfica (GPG o SSH).
- Se cubre en detalle en el ejercicio 3.4.

**7. Do not allow bypassing the above settings**
- Ni siquiera los administradores del repo pueden saltarse las reglas.
- En equipos pequeños esto puede ser frustrante (el único admin no puede hacer un hotfix directo). En producción es una buena práctica.

### Demo en vivo sugerida

1. Abre Settings > Branches en tu repo.
2. Configura cada regla explicando su propósito.
3. **Prueba inmediata:** Intenta hacer `git push origin main` directo. Debe fallar con "remote rejected".
4. Muestra que la única forma de llegar a `main` ahora es con un PR aprobado con checks verdes.

### Preguntas de discusión

- **"¿Qué sucede si intentas hacer push directo a `main` ahora?"** → Git rechaza el push con un error de "protected branch".
- **"¿Qué problema resuelve 'Require branches to be up to date'?"** → Evita el "merge del miedo": tu rama pasa los tests, pero `main` cambió y cuando se combina, los tests fallan. **Costo:** si `main` se mueve rápido, los participantes tendrán que actualizar su rama constantemente.

---

## Ejercicio 3.2 — CODEOWNERS (8 min)

### Qué explicar

CODEOWNERS es un concepto simple con sutilezas importantes.

#### Sintaxis básica

```
# Patrón           Owner
*                   @usuario-global        # Todo lo que no tenga regla más específica
/src/               @equipo/backend        # Código fuente
/tests/             @equipo/qa             # Tests
/.github/workflows/ @equipo/devops         # CI/CD
```

#### Las 3 cosas que más confunden

**1. "La última regla que coincida gana"**

```
*                   @ana          # Ana es owner de todo
/src/               @carlos       # PERO Carlos es owner de /src/
```

Si un PR modifica `src/Calculator.cs`, Carlos es el reviewer, no Ana. Esto es contra-intuitivo para gente que espera que las reglas se acumulen.

**2. El owner debe tener permisos de escritura**

Si pones `@nuevo-pasante` en CODEOWNERS pero esa persona tiene permisos de solo lectura, GitHub **ignora silenciosamente** esa entrada. No hay error, no hay advertencia — simplemente no se solicita review. Esto es una fuente de bugs en organizaciones grandes.

**3. CODEOWNERS no reemplaza branch protection**

Sin "Require review from Code Owners" activado en branch protection, el CODEOWNERS solo **sugiere** reviewers. Cualquier colaborador con permisos puede aprobar. Los dos mecanismos trabajan juntos:
- CODEOWNERS define **quién** debe revisar.
- Branch protection define **que alguien** debe revisar y que **ese alguien debe ser el owner**.

#### Ejercicio práctico

Los participantes editan `.github/CODEOWNERS` reemplazando `@TU_USUARIO` con su propio usuario. En un workshop con GitHub Enterprise:
- Si tienen equipos/teams configurados, pueden usar `@org/equipo-nombre`.
- Si trabajan individualmente, usan su propio `@usuario`.

Después de hacer push del CODEOWNERS, deben crear un PR que modifique un archivo en `src/` y verificar que GitHub solicita review automáticamente.

### Error frecuente

Si el CODEOWNERS no funciona (no solicita reviews), las causas más comunes son:
1. El usuario no tiene permisos de escritura en el repo.
2. "Require review from Code Owners" no está activado en branch protection.
3. El archivo está en la ubicación incorrecta (debe estar en `.github/CODEOWNERS`, `CODEOWNERS` en la raíz, o `docs/CODEOWNERS`).

---

## Ejercicio 3.3 — Templates de PR e Issues (5 min)

### Qué explicar

Este es un ejercicio rápido de observación. Los templates ya existen en el repo.

#### Por qué importan

Sin template, el 90% de los PRs llegan con descripción vacía o "fix stuff". Un template no garantiza buenas descripciones, pero reduce la fricción. Es como poner un formulario en vez de un campo de texto libre — la gente lo completa porque los campos ya están ahí.

#### Demo sugerida

1. Crea un PR desde cualquier rama. Muestra que la descripción se pre-carga con el contenido de `pull_request_template.md`.
2. Ve a Issues > New issue. Muestra que aparecen las dos plantillas (bug report y feature request).
3. Muestra el checklist que ya existe en el template de PR:
   ```
   - [ ] Los tests existentes pasan (`dotnet test`)
   - [ ] Agregué tests para el comportamiento nuevo o modificado
   - [ ] `dotnet format --verify-no-changes` pasa sin errores
   ```

#### La sección que se agrega en el ejercicio

El ejercicio pide agregar un checklist específico para cambios financieros:

```markdown
## Impacto en precisión financiera
- [ ] Los cálculos usan `decimal`, no `double`
- [ ] Los casos límite (tasa 0, periodos negativos) están cubiertos en tests
```

**Punto clave para el instructor:** Este tipo de checklist específico al dominio es **mucho más útil** que un checklist genérico. Un checklist que dice "los tests pasan" es obvio. Uno que dice "verifica que usas `decimal` y no `double` en cálculos financieros" captura conocimiento tribal del equipo.

---

## Ejercicio 3.4 — Commits firmados (3 min)

### Qué explicar

Este es más explicativo que práctico. Configurar firma SSH toma tiempo y puede fallar por múltiples razones (clave no existe, formato incorrecto, GitHub no la reconoce). **Recomendación: haz esto como explicación con una demo preparada, no como ejercicio hands-on.**

#### El problema que resuelve

Git permite configurar cualquier nombre y email como autor:

```bash
git config user.name "Elon Musk"
git config user.email "elon@spacex.com"
git commit -m "Lancé un cohete"
```

Ese commit aparece como si lo hubiera hecho Elon. No hay validación. La firma criptográfica es la prueba de que el commit **realmente fue hecho** por quien dice el campo `Author`.

#### SSH vs GPG

| Aspecto | SSH | GPG |
|---------|-----|-----|
| Configuración | Simple — la misma clave SSH que usas para auth | Compleja — requiere generar keypairs GPG |
| Soporte | Desde Git 2.34+ (2021) | Siempre |
| Recomendación actual | **Preferida** — más simple, misma seguridad | Legacy — solo si ya la tienes |

#### Demo preparada

Si haces demo, ten pre-configurada la firma SSH. Los pasos son:

```bash
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true
```

Muestra un commit firmado en GitHub con el badge **Verified** en verde.

#### Lo que NO protege

Enfatiza: firmar commits protege contra **suplantación de identidad**, no contra **código malicioso**. Un commit firmado por un atacante con acceso legítimo sigue siendo un commit firmado. La firma garantiza autenticidad, no integridad del contenido.

En equipos enterprise, la firma de commits suele ser un requisito de **auditoría** (SOC2, ISO 27001), no una medida técnica de seguridad.

---

## Ejercicio 3.5 — Merge strategies (4 min)

### Qué explicar

Este es puramente conceptual — no hay hands-on. Proyecta los tres diagramas y explica cada estrategia.

#### Las tres estrategias

**Merge commit** — preserva todo el historial:
```
o---o---o  feature
         \
o---o---o--M  main
```
- Cada commit de la rama se mantiene intacto.
- Se agrega un commit de merge (M) que une las dos líneas.
- Resultado: historial completo pero denso. `git log --oneline --graph` se vuelve un espagueti en repos activos.
- **Cuándo usar:** Ramas largas con múltiples autores donde cada commit tiene valor.

**Squash and merge** — un solo commit limpio:
```
o---o---o  feature (commits descartados)
o---o---o--S  main  (un solo commit con todos los cambios)
```
- Todos los commits de la rama se colapsan en uno.
- El historial de `main` queda limpio y lineal.
- **Cuándo usar:** Feature branches donde los commits son ruido ("WIP", "fix typo", "por fin funciona").

**Rebase and merge** — commits individuales re-escritos:
```
o---o---o---A'---B'---C'  main  (commits reescritos con nuevos SHAs)
```
- Los commits se reescriben sobre la punta de `main`.
- No hay commit de merge — historial lineal.
- **Cuándo usar:** Cuando los commits ya están limpios y quieres historial lineal sin el ruido del commit de merge.

#### Recomendación práctica

Para la mayoría de equipos: **Squash merge para feature branches** + **merge commit para releases o ramas de integración**.

Esto da lo mejor de ambos mundos:
- Historial de `main` limpio (un commit por feature/PR).
- Si necesitas detalle, miras el PR en GitHub donde están los commits originales.

#### Configuración en GitHub

Settings > General > Pull Requests. Puedes:
- Habilitar/deshabilitar cada método.
- Elegir un default.
- Tip: deshabilitar "merge commit" y "rebase" para forzar squash merge es una práctica común en equipos que priorizan un historial limpio.

---

## Nota sobre Rulesets vs Branch Protection Rules

Si algún participante pregunta:

- **Branch Protection Rules** son la forma clásica (configuración por rama).
- **Rulesets** son el reemplazo moderno: permiten definir reglas que aplican a múltiples ramas/tags con un solo conjunto de configuración.
- En GitHub Enterprise, los Rulesets pueden aplicarse a nivel de **organización**, no solo de repositorio.
- Para este workshop, ambos funcionan. No pierdas tiempo en la diferencia — menciona que Rulesets es el camino futuro y sigue adelante.

---

## Errores frecuentes de los participantes

| Error | Causa | Solución rápida |
|-------|-------|-----------------|
| Branch protection no aparece en Settings | No tienen permisos de admin en el repo | Verificar que crearon su propio repo |
| "Push directo a main sigue funcionando" | No activaron "Do not allow bypassing" y son admins | Activar la opción o usar Rulesets |
| CODEOWNERS no solicita reviews | El usuario no tiene permisos write, o "Require review from Code Owners" no está activado | Verificar ambas condiciones |
| El status check no aparece en la lista | El workflow nunca corrió — GitHub solo muestra checks que existen | Hacer un push a una rama para que el CI corra al menos una vez |
| El template de PR no se carga | El archivo no está en `.github/pull_request_template.md` | Verificar ubicación y nombre exacto |
| "Mi commit no dice Verified" | La clave SSH no está registrada como Signing Key en GitHub | Ir a Settings > SSH keys y verificar tipo "Signing Key" |

---

## Tiempos recomendados

| Ejercicio | Tiempo | Prioridad |
|-----------|--------|-----------|
| 3.1 — Branch protection | 5 min | **Máxima** |
| 3.2 — CODEOWNERS | 8 min | Alta |
| 3.3 — Templates | 5 min | Media |
| 3.4 — Commits firmados | 3 min | Baja (explicación > hands-on) |
| 3.5 — Merge strategies | 4 min | Media (conceptual) |

Si necesitas recortar: 3.4 puede ser solo una mención de 1 minuto. 3.5 puede integrarse como parte de la discusión de 3.1.

---

## Transición al Módulo 4 (Tags, Releases, Packages)

Cierra diciendo: "Ya tienen un pipeline robusto con protecciones. El código que llega a `main` está formateado, compila, pasa tests y fue revisado. Ahora la pregunta es: ¿cómo distribuyen ese código? ¿Copian archivos? No — usan tags para marcar versiones, releases para distribuir y packages para consumir como dependencias."
