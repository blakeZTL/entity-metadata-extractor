# entity-metadata-extractor
A .NET console application to help create an Entity Definition file for all the contents of a given Dataverse solution.

# BURST Entity Metadata Extractor

The *Entity Metadata Extractor* is a .NET Console application used to generate a JSON file with Dataverse metadata.
Using the application just involves inputting a target environment, the solution's display name and that is it!
The goal is to provide a quick and easy way to leverage descriptions inside Dataverse to provide detailed database schema for stakeholders.

## Getting Started

Head over to the [latest releases page](https://github.com/blakeZTL/entity-metadata-extractor/releases) and download the *EntityMetadataExtractor.zip* file. 
Unzip file and use the *Entity Metadata Extractor* executable (.exe). No installation needed.

Output file is a JSON file with the following schema:

    [
        {
            "EntityName": string,
            "DisplayName": string,
            "EntityDefinition": [
                {
                    "LogicalName": string,
                    "DisplayName": string,
                    "AttributeType": string,
                    "Description": string,
                    "IsPrimaryName": boolean
                }
            ]
        }
    ]

### Prerequisites

None

### Installing

None

## Built With

* [Microsoft.PowerPlatform.Dataverse.Client](https://github.com/microsoft/PowerPlatform-DataverseServiceClient) - The Dataverse SDK used

## Authors

* **Blake Bradford** - *Initial work*

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details
