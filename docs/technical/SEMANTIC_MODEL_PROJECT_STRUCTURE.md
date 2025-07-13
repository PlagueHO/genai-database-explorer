# Semantic Model Project Structure

A Generative AI Database Explorer project stores all the necessary files and configurations to work with a database schema and its semantic model. At the minimum, a project contains the following structure:

```text
project/
├── settings.json
```

However, depending on the storage strategy and features used, the project structure can expand to include additional directories and files. Below is a detailed description of the project structure and its components.

```text
project/
├── settings.json
└── my_app_model/
    ├── semanticmodel.json
    ├── tables/
    │   ├── table1.json
    │   ├── table2.json
    │   └── ...
    ├── views/
    │   ├── view1.json
    │   ├── view2.json
    │   └── ...
    └── stored_procedures/
        ├── procedure1.json
        ├── procedure2.json
        └── ...
```

## Settings File

The `settings.json` file contains configuration settings for the project, such as the database connection details, storage strategy, and other project-specific options. This file is essential for initializing and managing the project.
