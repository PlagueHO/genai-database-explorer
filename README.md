# Generative AI Database Explorer

This is a .NET 8 application that allows you to explore a SQL Database using Generative AI. It consists of several components.

## GenAIDBExplorer.Console App

A simple .NET 8 console application that provides commands to manage Generative AI Database Explorer projects, including functions for:

- Initializing a new project folder with a settings.json file.
- Building a representation of the database schema in the project folder based on the settings.json file
- Querying the database by using Generative AI to generate SQL queries based on the schema
- Explaining the schema and stored procedures in the database to the user based on the stored schema

### Generative AI Database Explorer project

All commands require the -p/--project setting that specifies a folder on disk called a "Project folder" that will contain a settings.json file that the user will configure before being able to execute any other commands.
