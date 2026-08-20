---
name: skill-writing
description: >
  Covers how to create and edit Agent Skills (SKILL.md files and supporting assets).
  Use this skill when asked to write a new skill, update an existing skill, improve skill instructions,
  or restructure skill content. Keywords: skill, SKILL.md, agent skill, skill writing, skill editing, create skill, update skill
---

# Skill: Skill Writing & Editing

## Overview

Agent Skills are directories containing a `SKILL.md` file with YAML frontmatter and Markdown instructions. They may also include `scripts/`, `references/`, and `assets/` subdirectories.

Skills in this repository live under `.github/skills/<skill-name>/`.

---

## Creating a New Skill

### 1. Pick a name

- Lowercase letters, numbers, and hyphens only
- No leading/trailing/consecutive hyphens (`--`)
- Must match the directory name exactly
- Example: `skill-writing`, `pdf-processing`, `data-analysis`

### 2. Create the directory

```
.github/skills/<skill-name>/
```

### 3. Write `SKILL.md`

Every skill requires this frontmatter:

```yaml
---
name: skill-name
description: >
  What this skill does and when to use it. Include action keywords (verbs) and
  domain nouns so the agent can recognize relevant prompts.
  Max 1024 characters.
---
```

Optional fields:

| Field | Use when |
|---|---|
| `compatibility` | Requires specific tools/env (e.g. `Requires Python 3.11+`) |
| `metadata` | Storing author, version, or other key-value info |
| `allowed-tools` | Pre-approving specific tools (experimental) |

### 4. Write the body

After the frontmatter, write Markdown instructions. Use these proven patterns:

**Step-by-step workflow** — numbered lists for sequential tasks  
**Gotchas section** — list non-obvious facts specific to this environment that the agent would get wrong otherwise  
**Output template** — provide a concrete template when output format matters  
**Validation loop** — instruct the agent to verify its own work before proceeding  
**Progressive disclosure** — keep `SKILL.md` under 500 lines; move deep reference material to `references/`

### 5. Add supporting files (optional)

```
scripts/    # Executable scripts the agent can run
references/ # Detailed reference docs loaded on demand
assets/     # Templates, data files, static resources
```

When referencing these in `SKILL.md`, tell the agent *when* to load them:

```markdown
If the API returns a non-200 status, read `references/error-codes.md`.
```

---

## Editing an Existing Skill

1. **Read the existing `SKILL.md`** fully before making any edits.
2. **Identify the gap**: Is the description too vague? Are instructions missing a step? Is there a gotcha that should be added? Did the content become outdated due to changes in the project?
3. **Prefer targeted edits** over rewrites — preserve working sections.
4. **Update the `description`** if the skill's trigger conditions have changed. The description is what the agent reads at startup to decide whether to activate the skill.
5. **Move bulk content to `references/`** if `SKILL.md` is growing beyond ~500-600 lines.

---

## Quality Checklist

Before finalizing any skill:

- [ ] `name` matches the directory name exactly
- [ ] `description` describes both *what* the skill does and *when* to use it, with action keywords
- [ ] Body covers steps the agent would otherwise get wrong (not generic advice)
- [ ] Output format defined with a concrete template if the skill produces structured output
- [ ] `SKILL.md` is under 500-600 lines; large reference material moved to `references/`
- [ ] File references use relative paths and specify *when* to load them
- [ ] No duplicate information between frontmatter description and body

---

## Gotchas

- **Don't explain general knowledge.** The agent already knows how HTTP works or what a PDF is, omit these general explanations. Focus on what is specific to this project or workflow.
- **Description is for activation, not documentation.** It must be keyword-rich and describe trigger conditions, not a paragraph of prose.
- **`name` must match directory exactly.** A mismatch will cause the skill to fail validation.
- **Avoid menus of equal options.** Pick a default approach and briefly mention alternatives as escape hatches.
- **Generic LLM-generated skills are low value.** Ground every instruction in real project context — conventions, actual APIs, known failure modes.
- **Don't go into excessive detail.** Focus on the high-level flow of features. Mentioning class names and project names is acceptable, but skills should not enumerate individual methods, properties, or endpoint signatures — that level of detail belongs in the code itself.

---

## Example Minimal Skill

```markdown
---
name: csv-import
description: >
  Import CSV files into the database. Use when the user provides a CSV file
  to load, asks to import data, or mentions bulk data upload.
---

# CSV Import

## Steps

1. Validate the CSV: `python scripts/validate_csv.py <file>`
2. If validation fails, fix the reported issues and re-validate.
3. Import: `python scripts/import_csv.py <file> --table <target_table>`
4. Confirm row count matches source: `python scripts/verify_import.py <file> <target_table>`

## Gotchas

- Column names in the CSV must match the database schema exactly (case-sensitive).
- Empty strings are imported as NULL — warn the user if this is unexpected.
- The import script does not deduplicate; run `scripts/check_duplicates.py` first if the source may have duplicates.
```
