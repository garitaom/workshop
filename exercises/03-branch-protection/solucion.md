# Soluciones — Módulo 2: Branch protection, CODEOWNERS y merge strategies

## Ejercicio 3.1 — Configurar branch protection

Settings configuradas para `main`:

1. **Require a pull request before merging** ✅
   - Required approvals: 1
2. **Require status checks to pass before merging** ✅
   - Status checks: `Format check`, `Build`, `Test` (del workflow CI)
3. **Require conversation resolution before merging** ✅
4. **Do not allow bypassing the above settings** ✅ (opcional, según el equipo)

> **Nota:** "Include administrators" aplica las reglas incluso a los admins del repo. En equipos pequeños puede ser incómodo, pero en producción es una buena práctica.

---

## Ejercicio 3.2 — CODEOWNERS

Archivo `.github/CODEOWNERS` configurado:

```
# Owners por defecto
* @tu-usuario

# Código fuente — equipo de desarrollo
src/ @tu-usuario

# Tests — QA
tests/ @tu-usuario

# CI/CD — DevOps
.github/workflows/ @tu-usuario

# Configuración del editor
.editorconfig @tu-usuario
```

**Clave:** Los owners deben tener acceso de **escritura** al repo, o no podrán aprobar PRs.

---

## Ejercicio 3.3 — Templates de PR e Issues

Los templates ya están creados en el repo:

- `.github/pull_request_template.md` — se carga automáticamente al crear un PR
- `.github/ISSUE_TEMPLATE/bug_report.md` — plantilla para reportar bugs
- `.github/ISSUE_TEMPLATE/feature_request.md` — plantilla para solicitar features

Para verificar, crea un PR y confirma que la plantilla aparece automáticamente en el cuerpo del PR.

---

## Ejercicio 3.4 — Commits firmados con SSH

```bash
# 1. Generar clave SSH (si no tienes una)
ssh-keygen -t ed25519 -C "tu@email.com"

# 2. Configurar Git para firmar con SSH
git config --global gpg.format ssh
git config --global user.signingkey ~/.ssh/id_ed25519.pub
git config --global commit.gpgsign true

# 3. Agregar la clave en GitHub
# Settings > SSH and GPG keys > New SSH key
# Tipo: Signing Key (no Authentication Key)

# 4. Verificar
git commit --allow-empty -m "test: commit firmado"
git log --show-signature -1
```

En GitHub, el commit mostrará la etiqueta **Verified** en verde.

---

## Ejercicio 3.5 — Merge strategies

| Estrategia | Resultado en el historial | Cuándo usar |
|-----------|--------------------------|-------------|
| **Merge commit** | Preserva todos los commits + commit de merge | Ramas de larga vida, quieres historial completo |
| **Squash merge** | Un solo commit con todos los cambios | Feature branches con muchos commits WIP |
| **Rebase** | Commits individuales sin commit de merge | Historial lineal, commits ya limpios |

**Recomendación para equipos:** Squash merge para feature branches (historial limpio en `main`) + merge commit para releases o ramas de integración.
