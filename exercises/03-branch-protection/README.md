# 🛡️ Módulo 2: Políticas de repositorio, CODEOWNERS y estrategias de merge

![Duración](https://img.shields.io/badge/Duración-25%20min-blue)
![Dificultad](https://img.shields.io/badge/Dificultad-Intermedio-orange)

## 🎯 Objetivos

- ✅ Configurar branch protection rules para `main`
- ✅ Escribir un archivo CODEOWNERS con owners por área de código
- ✅ Usar templates de PR e issues para estandarizar la comunicación
- ✅ Configurar commits firmados con SSH
- ✅ Entender la diferencia entre squash, merge commit y rebase
- ✅ Conocer Rulesets como sucesor de las reglas clásicas

---

## Contexto

Las branch protection rules son el contrato técnico del equipo sobre qué condiciones debe cumplir el código para llegar a `main`. Sin ellas, el pipeline de CI es decorativo: cualquiera puede hacer merge ignorando checks fallidos o sin revisión. Pero las reglas de rama son solo una parte del sistema de políticas de un repositorio. Este módulo cubre todo lo que compone ese sistema: protección de ramas, CODEOWNERS, templates de PR e issues, commits firmados y estrategias de merge.

---

## Ejercicio 3.1 — Configurar branch protection en `main` (5 min)

Ve a **Settings > Branches > Add branch ruleset** (repos modernos) o **Branch protection rules** (repos con configuración clásica).

Configura lo siguiente para `main`:

**Pull request obligatorio**
- Require a pull request before merging: activado
- Required approvals: 1
- Dismiss stale pull request approvals when new commits are pushed: activado
- Require review from Code Owners: activado (esto se conecta con el ejercicio 3.2)

**Status checks**
- Require status checks to pass before merging: activado
- Require branches to be up to date before merging: activado
- Status checks requeridos: `Format check`, `Build`, `Test`

**Restricciones adicionales**
- Require signed commits: activado (se discute en el ejercicio 3.4)
- Do not allow bypassing the above settings: activado

**Preguntas para discutir antes de continuar:**
1. ¿Qué sucede si intentas hacer push directo a `main` ahora?
2. "Dismiss stale reviews" — describe un escenario concreto donde esto evita un problema real.
3. "Require branches to be up to date" — ¿qué problema resuelve y qué costo operativo agrega?

---

## Ejercicio 3.2 — CODEOWNERS (8 min)

CODEOWNERS es un archivo que define quién debe revisar cambios en partes específicas del repositorio. Cuando un PR modifica archivos que coinciden con un patrón, GitHub solicita automáticamente la revisión del owner correspondiente, y ese review se convierte en requerido si tienes "Require review from Code Owners" activado en la branch protection.

El archivo ya existe en este repositorio en `.github/CODEOWNERS`. Ábrelo y revísalo antes de hacer los ejercicios.

### Sintaxis

```
# Patrón                   Owner (usuario o equipo)
*                           @usuario-global
/src/                       @equipo/backend
/src/FinancialUtils/        @usuario-experto-en-finanzas
*.cs                        @equipo/dotnet-guild
/.github/workflows/         @equipo/devops
```

Las reglas se evalúan de arriba hacia abajo y **la última que coincida gana**. Esto significa que si tienes un owner global en `*` y uno más específico para `/src/`, el de `/src/` toma precedencia para esos archivos.

### Ejercicio: modificar CODEOWNERS para simular un equipo

Edita `.github/CODEOWNERS` para definir dos "equipos" ficticios usando tu propio usuario. El objetivo es entender cómo se estructura el archivo para un equipo real, no solo para un usuario individual.

```
# Owner global — último recurso
*                               @TU_USUARIO

# El equipo de backend revisa la librería
/src/FinancialUtils/            @TU_USUARIO

# El equipo de QA revisa los tests
/tests/                         @TU_USUARIO

# DevOps revisa cualquier cambio en pipelines o configuración de CI
/.github/workflows/             @TU_USUARIO
/.editorconfig                  @TU_USUARIO
/.github/CODEOWNERS             @TU_USUARIO
```

Haz commit y push de este cambio. Luego crea un PR que modifique `src/FinancialUtils/Calculator.cs` y observa cómo GitHub solicita automáticamente la revisión del owner de `/src/FinancialUtils/`.

**Lo que CODEOWNERS no hace:** no reemplaza la branch protection. Si tienes "Required approvals: 1" pero no tienes "Require review from Code Owners" activado, la aprobación de cualquier colaborador satisface el requisito, aunque no sea el owner del área modificada. Los dos mecanismos trabajan juntos.

**Limitación importante:** si un owner listado en CODEOWNERS no tiene acceso de escritura al repositorio, GitHub ignora esa entrada silenciosamente. En repos de organización, verifica que los usuarios o equipos tienen los permisos correctos.

---

## Ejercicio 3.3 — Templates de PR e issues (5 min)

Este repositorio ya incluye templates en `.github/`. Revísalos:

```
.github/
├── pull_request_template.md
└── ISSUE_TEMPLATE/
    ├── bug_report.md
    └── feature_request.md
```

### Por qué importan los templates

Sin template, la mayoría de los PRs llegan con una descripción vacía o con "fix stuff". Un template no garantiza buenas descripciones, pero reduce la fricción de escribir una: el colaborador ve los campos y los completa porque ya están ahí.

Lo mismo aplica para issues. Sin template, los bugs reportados rara vez tienen los pasos para reproducir o el entorno donde ocurren.

### Ejercicio

1. Crea un PR desde cualquier rama hacia `main`. Observa que la descripción se precarga con el contenido de `pull_request_template.md`.

2. Ve a la pestaña **Issues > New issue**. Observa que aparecen las dos plantillas como opciones.

3. Modifica `pull_request_template.md` para agregar una sección específica para cambios en calculadoras financieras:

```markdown
## Impacto en precisión financiera

<!-- Solo si el cambio afecta Calculator.cs o Formatter.cs -->
- [ ] Los cálculos usan `decimal`, no `double`
- [ ] Los casos límite (tasa 0, periodos negativos) están cubiertos en tests
```

Este tipo de checklist específica al dominio es más útil que un checklist genérico de "los tests pasan".

---

## Ejercicio 3.4 — Commits firmados (3 min)

"Require signed commits" en la branch protection exige que cada commit que llegue a `main` tenga una firma GPG o SSH válida. El objetivo es garantizar que el commit realmente fue hecho por quien dice el autor, no que alguien configuró `git config user.email` con el correo de otra persona.

### Configurar firma con SSH (más simple que GPG en 2024)

```bash
# Verificar si ya tienes una clave SSH
ls ~/.ssh/*.pub

# Si no tienes, generar una
ssh-keygen -t ed25519 -C "tu@email.com"

# Configurar Git para firmar con SSH
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true

# Agregar la clave a GitHub como "Signing key"
# Settings > SSH and GPG keys > New SSH key > Key type: Signing Key
cat ~/.ssh/id_ed25519.pub
```

Una vez configurado, cada `git commit` agrega la firma automáticamente. En GitHub, los commits firmados aparecen con el badge **Verified**.

### Lo que esto protege y lo que no

Firmar commits protege contra suplantación de identidad en el historial de Git. No protege contra código malicioso: un commit puede estar perfectamente firmado por el autor correcto y aun así contener un bug intencional. La firma garantiza autenticidad, no integridad del contenido.

En equipos enterprise, los commits firmados son frecuentemente un requisito de auditoría, no una medida de seguridad técnica.

---

## Ejercicio 3.5 — Comparar estrategias de merge (4 min)

GitHub ofrece tres opciones. La diferencia afecta la legibilidad del historial y cómo haces `git bisect` o `git revert` después.

### Merge commit

```
o---o---o  feature
         \
o---o---o--M  main
```

Preserva el historial completo de la rama. Útil cuando los commits tienen valor (múltiples autores, decisiones documentadas). El `git log --oneline --graph` en repos activos se vuelve difícil de leer.

### Squash and merge

```
o---o---o  feature (descartados)
         \
o---o---o--S  main
```

Colapsa todos los commits en uno. Historial de `main` limpio y lineal. Se pierde la granularidad. Útil para features pequeñas o cuando los commits de la rama son ruido ("WIP", "fix typo", "otro intento").

### Rebase and merge

```
A---B---C  feature (reescritos con nuevos SHAs)

o---o---o---A'---B'---C'  main
```

Reproduce los commits sobre `main` con nuevos hashes. Historial lineal sin merge commit, conservando los commits individuales. La contra: cambia los SHAs, lo que puede generar confusión si alguien tiene la rama local después del merge.

**Ejercicio:** En Settings > General > Merge button, habilita las tres opciones. Crea tres PRs mínimos con una estrategia diferente cada uno. Después compara:

```bash
git pull origin main
git log --oneline --graph
```

Observa cómo se ve el historial con cada estrategia y decide cuál usaría tu equipo como política por defecto.

---

## Rulesets vs Branch protection rules clásicas

GitHub introdujo Rulesets como sucesor de las branch protection rules clásicas. Las diferencias que importan en la práctica:

- Rulesets se pueden aplicar a múltiples ramas con un patrón (`feature/**`, `release/*`)
- Rulesets soportan bypass lists granulares por rol o por actor específico
- Rulesets son exportables e importables como JSON (útil para replicar políticas entre repos)
- Las reglas clásicas siguen funcionando pero no reciben nuevas funcionalidades

En repos nuevos, usa Rulesets. La migración desde reglas clásicas no es automática y no hay herramienta oficial para hacerlo en bulk.

---

## Resumen: qué vive dónde

| Política | Dónde se configura |
|----------|--------------------|
| Protección de ramas | Settings > Branches |
| Reviewers automáticos por área de código | `.github/CODEOWNERS` |
| Template de PR | `.github/pull_request_template.md` |
| Templates de issues | `.github/ISSUE_TEMPLATE/` |
| Commits firmados | Branch protection + configuración local de Git |
| Estrategia de merge permitida | Settings > General > Merge button |
| Auto-delete de ramas al mergear | Settings > General > Automatically delete head branches |

---

## 🛠️ Troubleshooting del Módulo

| Problema | Solución |
|----------|----------|
| No puedo hacer push directo a `main` | ¡Eso es lo esperado! La branch protection funciona. Crea un PR |
| CODEOWNERS no solicita reviews | Verifica que el usuario tenga acceso de escritura y que "Require review from Code Owners" esté activado |
| El commit no aparece como "Verified" | Verifica que la clave SSH esté agregada como **Signing Key** (no solo Authentication Key) en GitHub |
| No encuentro Rulesets en Settings | Solo está disponible en repos de organización o repos públicos con plan adecuado |
| El template de PR no se carga | Verifica que el archivo sea `.github/pull_request_template.md` (exactamente ese nombre y ruta) |
| `git commit.gpgsign true` da error | Verifica primero que `gpg.format` está configurado como `ssh` |

---

## ✅ Verificación

Al terminar este módulo, deberías poder responder:

1. ¿Qué diferencia hay entre "Required approvals" y "Require review from Code Owners"?
2. ¿Qué pasa si un owner en CODEOWNERS no tiene acceso de escritura al repo?
3. ¿Cuándo usarías squash merge vs. merge commit vs. rebase?
4. ¿Commits firmados protegen contra código malicioso? ¿Por qué sí o por qué no?

> 📝 **Para el instructor:** El Ejercicio 3.1 (branch protection) y 3.5 (merge strategies) son los de mayor impacto. Si el tiempo es corto, los commits firmados (3.4) pueden ser una demostración rápida.
