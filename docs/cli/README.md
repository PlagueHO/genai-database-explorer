# GenAI Database Explorer (GAIDBEXP) CLI Tool

The GenAI Database Explorer (GAIDBEXP) is a console tool that produces semantic models from a database schema and enriches it with a database dictionary. It is intended to produce a semantic model that can be used to query using natural language.

## init-project

Initializes a new GenAI Database Explorer project.

### Usage

```bash
gaidbexp init-project --project <project path>
```

### Options

- `--project`, `-p` (required): Specifies the path to the GenAI Database Explorer project directory.

### Description

The `init-project` command initializes a new GenAI Database Explorer project at the specified path. If the directory does not exist, it will be created. If the directory exists and is not empty, the command will fail with an error. This ensures that the project directory is properly set up and ready for further development and usage.

### Example

```bash
gaidbexp init-project --project /path/to/project
```

This example initializes a new GenAI Database Explorer project in the directory `/path/to/project`.

---

## Command Reference

Below are the available commands for the GenAI Database Explorer (GAIDBEXP) CLI tool, including their parameters and usage:

### extract-model

Extracts a semantic model from the database schema.

**Usage:**

```bash
gaidbexp extract-model --project <path> [--skip-tables] [--skip-views] [--skip-stored-procedures]
```

**Options:**

- `--project`, `-p` (required): Path to the project directory.
- `--skip-tables`: Skip tables during extraction.
- `--skip-views`: Skip views during extraction.
- `--skip-stored-procedures`: Skip stored procedures during extraction.

### data-dictionary

Applies data dictionary files to the semantic model.

**Usage:**

```bash
gaidbexp data-dictionary table --project <path> --source-path <pattern> [--schema-name <name>] [--name <object>] [--show]
```

**Options:**

- `--project`, `-p` (required): Path to the project directory.
- `--source-path`, `-d` (required): Path pattern to the data dictionary files.
- `--schema-name`, `-s`: Schema name of the table to process.
- `--name`, `-n`: Name of the table to process.
- `--show`: Display the entity after processing.

### enrich-model

Enriches the semantic model using Generative AI.

**Usage:**

```bash
gaidbexp enrich-model --project <path> [--skip-tables] [--skip-views] [--skip-stored-procedures]

# Target a single object type
gaidbexp enrich-model table --project <path> --schema-name <name> --name <object> [--show]
gaidbexp enrich-model view --project <path> --schema-name <name> --name <object> [--show]
gaidbexp enrich-model storedprocedure --project <path> --schema-name <name> --name <object> [--show]
```

**Options:**

- `--project`, `-p` (required): Path to the project directory.
- `--skip-tables`: Skip tables during enrichment.
- `--skip-views`: Skip views during enrichment.
- `--skip-stored-procedures`: Skip stored procedures during enrichment.
- For subcommands: `--schema-name`, `-s` and `--name`, `-n` select a specific object.
- `--show`: Display the entity after enrichment.

### show-object

Displays details of a table, view, or stored procedure.

**Usage:**

```bash
gaidbexp show-object table --project <path> --schema-name <name> --name <object>
gaidbexp show-object view --project <path> --schema-name <name> --name <object>
gaidbexp show-object storedprocedure --project <path> --schema-name <name> --name <object>
```

**Options:**

- `--project`, `-p` (required): Path to the project directory.
- `--schema-name`, `-s` (required): Schema name of the object.
- `--name`, `-n` (required): Name of the object.

### query-model

Generates SQL or answers questions against the semantic model.

**Usage:**

```bash
gaidbexp query-model --project <path> --question <text>
```

**Options:**

- `--project`, `-p` (required): Path to the project directory.
- `--question`, `-q` (required): The question to ask the model.

### export-model

Exports the semantic model to a file.

**Usage:**

```bash
gaidbexp export-model --project <path> [--output-file-name <file>] [--file-type <type>] [--split-files]
```

**Options:**

- `--project`, `-p` (required): Path to the project directory.
- `--output-file-name`, `-o`: Name of the output file (defaults to `exported_model.md`).
- `--file-type`, `-f`: Type of output file (defaults to `markdown`).
- `--split-files`, `-s`: Split export into individual files per entity.

---

## generate-vectors

Generates embeddings for semantic model entities and upserts them into the configured vector index.

**Usage:**

```bash
gaidbexp generate-vectors --project <path> [--overwrite] [--dry-run] [--skip-tables] [--skip-views] [--skip-stored-procedures]
```

Subcommands for targeting a single object type:

```bash
gaidbexp generate-vectors table --project <path> --schema-name <name> --name <object> [--overwrite] [--dry-run]
gaidbexp generate-vectors view --project <path> --schema-name <name> --name <object> [--overwrite] [--dry-run]
gaidbexp generate-vectors storedprocedure --project <path> --schema-name <name> --name <object> [--overwrite] [--dry-run]
```

**Options:**

- `--project`, `-p` (required): Path to the project directory.
- `--overwrite`: Regenerate embeddings even when content hash is unchanged.
- `--dry-run`: Execute without writing local files or updating the external index.
- `--skip-tables`: Skip tables.
- `--skip-views`: Skip views.
- `--skip-stored-procedures`: Skip stored procedures.
- For subcommands: `--schema-name`, `-s` and `--name`, `-n` select a specific object.

### Examples (generate-vectors)

```bash
# Generate embeddings for all entities using current settings
gaidbexp generate-vectors --project d:/temp

# Force regeneration and index upsert
gaidbexp generate-vectors --project d:/temp --overwrite

# Preview actions without writing
gaidbexp generate-vectors --project d:/temp --dry-run

# Target a single table
gaidbexp generate-vectors table --project d:/temp --schema-name dbo --name tblItemSellingLimit
```

---

## reconcile-index

Reconciles the external vector index with locally persisted embeddings by re-upserting records.

**Usage:**

```bash
gaidbexp reconcile-index --project <path> [--dry-run]
```

**Options:**

- `--project`, `-p` (required): Path to the project directory.
- `--dry-run`: Execute without updating the external index.

### Examples (reconcile-index)

```bash
# Preview index reconciliation
gaidbexp reconcile-index --project d:/temp --dry-run

# Perform reconciliation (writes to index)
gaidbexp reconcile-index --project d:/temp
```
