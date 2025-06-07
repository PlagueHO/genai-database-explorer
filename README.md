# Generative AI Database Explorer

[![Continuous Integration][continuous-integration-shield]][continuous-integration-url]
[![Continuous Delivery][continuous-delivery-shield]][continuous-delivery-url]
[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]
[![LinkedIn][linkedin-shield]][linkedin-url]

With **Generative AI Database Explorer**, you can explore your database schema and stored procedures using Generative AI. This tool helps you understand your database schema and stored procedures by generating SQL queries based on the schema and explaining the schema and stored procedures in the database to the user based on the stored schema.

Although there are many other tools available that perform similar functions, this tool produces a **semantic model** of the database schema, combined with a data dictionary and enriched using Generative AI.

The reason that this approach of enriching a semantic model rather than just querying the database directly is:

1. Many databases are not normalized and have grown organically over time. This can make it difficult to understand the schema and stored procedures by just looking at the table & column names.
1. Data dictionaries are often not maintained or are incomplete, but can still be useful to provide additional information about the schema.
1. Additional grounding information may need to be provided by a user to ensure that the Generative AI can provide accurate information.
1. Enables greater control and the database owner can review and adjust the semantic model to ensure it is correct.
1. The semantic model can be stored in version control and used as an asset that is deployed as part of another application.

## Components

### Console App (GenAIDBExplorer.Console)

A console application that provides commands to manage Generative AI Database Explorer projects, including functions for:

- **init-project**: Initializing a new project folder with a `settings.json` file to contain the configuration for the project.e!!!! XOX
- **extract-model**: Extract a representation of the database schema as a **semantic model** in the **project folder** based on the `settings.json` file.
- **data-dictionary**: Update an extracted semantic model with additional information provided from a set of data dictionary files.
- **enrich-model**: Enrich an existing **semantic model** of a database schema in the project folder based on the `settings.json` file using Generative AI to produce an **enriched** semantic model.
- **show-object**: Show the details of a table, column, or stored procedure in the **semantic model**.
- **query-model**: Answer questions based on the semantic model by using Generative AI. This includes recommending SQL.

### Web App (GenAIDBExplorer.Web)

This planned app is a simple web application that can take an **enriched model** and enable a user to explore the database schema and stored procedures using chat.

## Generative AI Database Explorer project

All commands require the -p/--project setting that specifies a folder on disk called a "project folder" that will contain a settings.json file that the user will configure before being able to execute any other commands.

## Semantic Model

This is a representation of the database schema that is enriched by Generative AI. The semantic model is stored in subfolder in the project folder with the name of the database in a file called `semanticmodel.json`. The **semantic model** must be produced by the console tool before it can be used to querying or with the web application.

## Disclaimer

This repository is provided "as is" without warranty of any kind, whether express or implied. Use at your own risk! The author will not be liable for any losses or damages associated with the use of this repository. 

It is intended to be used as a starting point for your own project and not as a final product.

## License

Copyright (c) 2024 Daniel Scott-Raynsford

Licensed under the [MIT](LICENSE) license.

## Contact

- [Daniel Scott-Raynsford](https://danielscottraynsford.com/) | [@github](https://github.com/PlagueHO) | danielscottraynsford.com
- Project Link: [https://github.com/PlagueHO/genai-database-explorer](https://github.com/PlagueHO/genai-database-explorer)

## Acknowledgments

TBC

## References

TBC

[continuous-integration-shield]: https://github.com/PlagueHO/genai-database-explorer/actions/workflows/continuous-integration.yml/badge.svg
[continuous-integration-url]: https://github.com/PlagueHO/genai-database-explorer/actions/workflows/continuous-integration.yml
[continuous-delivery-shield]: https://github.com/PlagueHO/genai-database-explorer/actions/workflows/continuous-delivery.yml/badge.svg
[continuous-delivery-url]: https://github.com/PlagueHO/genai-database-explorer/actions/workflows/continuous-delivery.yml
[contributors-shield]: https://img.shields.io/github/contributors/PlagueHO/genai-database-explorer.svg
[contributors-url]: https://github.com/PlagueHO/genai-database-explorer/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/PlagueHO/genai-database-explorer.svg
[forks-url]: https://github.com/PlagueHO/genai-database-explorer/network/members
[stars-shield]: https://img.shields.io/github/stars/PlagueHO/genai-database-explorer.svg
[stars-url]: https://github.com/PlagueHO/genai-database-explorer/stargazers
[issues-shield]: https://img.shields.io/github/issues/PlagueHO/genai-database-explorer.svg
[issues-url]: https://github.com/PlagueHO/genai-database-explorer/issues
[license-shield]: https://img.shields.io/github/license/PlagueHO/genai-database-explorer.svg
[license-url]: https://github.com/PlagueHO/genai-database-explorer/blob/master/LICENSE
[linkedin-shield]: https://img.shields.io/badge/-LinkedIn-black.svg?logo=linkedin&colorB=555
[linkedin-url]: https://www.linkedin.com/in/dscottraynsford

[openai.com]: https://img.shields.io/badge/OpenAI-5A5AFF?style=for-the-badge&logo=openai&logoColor=white
[openai-url]: https://openai.com/
[azure.com]: https://img.shields.io/badge/Microsoft_Azure-0078D4?style=for-the-badge&logo=microsoft-azure&logoColor=white
[azure-url]: https://azure.microsoft.com
[dotnet.microsoft.com]: https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[dotnet-url]: https://dotnet.microsoft.com
[python.org]: https://img.shields.io/badge/Python-3776AB?style=for-the-badge&logo=python&logoColor=white
[python-url]: https://www.python.org
[learn-sk]: https://img.shields.io/badge/Semantic%20Kernel-5E5E5E?style=for-the-badge&logo=microsoft
[sk-url]: https://learn.microsoft.com/en-us/semantic-kernel/

