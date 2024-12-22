# gaidebexp

The GenAI Database Explorer (GAIDBEXP) is a console tool that produces semantic models from a database schema and enriches it with a database dictionary. It is intended to produce a semantic model that can be used to query using natural language.

## init-project

The `init-project` command is part of the `gaidbexp` console application, which is designed to initialize a GenAI Database Explorer project. This command sets up the necessary project structure and configurations to start using the GenAI Database Explorer.

### Usage

```bash
gaidbexp init-project --project <project path>
```

### Options

- `--project`, `-p` (required): Specifies the path to the GenAI Database Explorer project directory.

### Description

The `init-project` command initializes a new GenAI Database Explorer project at the specified path. It ensures that the project directory is properly set up and ready for further development and usage.

### Example

```bash
gaidbexp init-project --project /path/to/project
```

This example initializes a new GenAI Database Explorer project in the directory `/path/to/project`.
