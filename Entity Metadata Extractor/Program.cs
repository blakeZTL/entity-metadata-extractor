using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace GetEntityMetadata;

partial class Program
{
    static void Main(string[] args)
    {
        Console.Clear();
        Console.Title = "BURST Entity Metadata Extractor";
        Console.WriteLine("\nWelcome to the BURST Entity Metadata Extractor!");
        Console.WriteLine(
            "\n\nThis tool will extract the metadata for all entities in a solution and save it to a JSON file."
        );
        Console.WriteLine(
            "\n\nPlease note that this tool will only extract the metadata for the entities in the solution.\nIt will not extract the data in the entities."
        );
        Console.WriteLine(
            "\n\nIf you want to extract the metadata for all columns, not just custom, please enter 'N' when prompted."
        );
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\n\nPress any key to continue...");
        Console.ReadKey();
        Console.Clear();
        Console.WriteLine(
            "\n\nPlease enter the URL for your Dataverse environment.\nYou will be prompted to select credentials in your default browser.\n"
        );
        Console.ResetColor();

        string? url = Console.ReadLine();
        while (ValidateUrl(url) == null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                "\nInvalid URL. Please enter a valid URL for your Dataverse environment.\n"
            );
            Console.ResetColor();
            url = Console.ReadLine();
        }

        string connectionString =
            $"AuthType=OAuth;Url={url};RedirectUri=http://localhost;LoginPrompt=Auto";
        ServiceClient? service = null;
        try
        {
            service = new ServiceClient(connectionString);
        }
        catch
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                "\n\nConnection failed. Please check the URL and try again.\n\nPress any key to exit..."
            );
            Console.ReadKey();
            Environment.Exit(0);
        }

        if (service != null && service.IsReady)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n\nConnection successful!");
            EntityCollection solutionComponents;
            Console.ForegroundColor = ConsoleColor.Green;
            do
            {
                Console.WriteLine(
                    "\n\nPlease enter the name of the solution you want to extract.\n"
                );
                Console.ResetColor();
                string? solutionName = Console.ReadLine();
                while (string.IsNullOrWhiteSpace(solutionName))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        "\nInvalid solution name. Please enter the name of the solution you want to extract."
                    );
                    Console.ForegroundColor = ConsoleColor.Green;
                    solutionName = Console.ReadLine();
                }

                solutionComponents = service.RetrieveMultiple(
                    new QueryExpression("solutioncomponent")
                    {
                        ColumnSet = new ColumnSet(true),
                        Criteria = new FilterExpression
                        {
                            Conditions =
                            {
                                new ConditionExpression("componenttype", ConditionOperator.Equal, 1)
                            }
                        },
                        LinkEntities =
                        {
                            new LinkEntity(
                                "solutioncomponent",
                                "solution",
                                "solutionid",
                                "solutionid",
                                JoinOperator.Inner
                            )
                            {
                                LinkCriteria = new FilterExpression
                                {
                                    Conditions =
                                    {
                                        new ConditionExpression(
                                            "friendlyname",
                                            ConditionOperator.Equal,
                                            solutionName
                                        )
                                    }
                                }
                            }
                        }
                    }
                );
                if (solutionComponents.Entities.Count == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        "\n\nNo solution with that name was found. Please try again."
                    );
                    Console.ForegroundColor = ConsoleColor.Green;
                }
            } while (solutionComponents.Entities.Count == 0);
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"\nFound {solutionComponents.Entities.Count} solution entities");
            Console.ForegroundColor = ConsoleColor.Green;

            string? customColumnsOnly;
            do
            {
                Console.WriteLine(
                    "\n\nDo you want to include only custom columns? (Y/N) (Default: Y)"
                );
                Console.ResetColor();
                customColumnsOnly = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(customColumnsOnly))
                {
                    customColumnsOnly = "Y";
                }
                else
                {
                    customColumnsOnly = customColumnsOnly.ToUpper();
                }
            } while (customColumnsOnly != "Y" && customColumnsOnly != "N");

            bool customColumnsOnlyBool = customColumnsOnly == "Y";

            List<object> entityDefinitions = new();
            foreach (var solutionComponent in solutionComponents.Entities)
            {
                solutionComponent.TryGetAttributeValue("objectid", out Guid objectId);

                var entityRequest = new RetrieveEntityRequest
                {
                    EntityFilters = EntityFilters.Attributes,
                    MetadataId = objectId,
                    RetrieveAsIfPublished = true
                };

                var entityResponse = (RetrieveEntityResponse)service.Execute(entityRequest);

                if (entityResponse == null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine(
                        $"Entity with ID {objectId} was not found in the solution. Skipping..."
                    );
                    Console.ForegroundColor = ConsoleColor.Green;
                    continue;
                }

                var entityMetadata = entityResponse.EntityMetadata;

                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine(
                    $"\n\n{entityMetadata.LogicalName} ({entityMetadata.DisplayName?.UserLocalizedLabel?.Label})"
                );
                Console.ForegroundColor = ConsoleColor.Gray;

                var attributeDetails = new List<object>();
                foreach (var attribute in entityMetadata.Attributes)
                {
                    bool isCustomAttribute;
                    if (attribute.IsCustomAttribute.HasValue)
                    {
                        isCustomAttribute = attribute.IsCustomAttribute.Value;
                    }
                    else
                    {
                        isCustomAttribute = false;
                    }
                    if (customColumnsOnlyBool && !isCustomAttribute)
                    {
                        continue;
                    }

                    Console.WriteLine($"{attribute.LogicalName} ({attribute.AttributeType})");
                    attributeDetails.Add(
                        new
                        {
                            attribute.LogicalName,
                            DisplayName = attribute.DisplayName?.UserLocalizedLabel?.Label,
                            AttributeType = attribute.AttributeType?.ToString(),
                            Description = attribute.Description?.UserLocalizedLabel?.Label,
                            attribute.IsPrimaryName
                        }
                    );
                }
                entityDefinitions.Add(
                    new
                    {
                        EntityName = entityResponse.EntityMetadata.LogicalName,
                        DisplayName = entityResponse
                            .EntityMetadata
                            .DisplayName
                            ?.UserLocalizedLabel
                            ?.Label,
                        EntityDefinition = attributeDetails
                    }
                );
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n\nAll entities extracted!");
            Console.Beep();
            Console.WriteLine("\n\nPress any key to save the file...");
            Console.ReadKey();

            // Serialize the data to JSON
            string json = JsonConvert.SerializeObject(entityDefinitions, Formatting.Indented);

            // Get the path to the user's Downloads folder
            string downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );

            // Create the full path for the new file
            string filePath = Path.Combine(downloadsPath, "entity_definitions.json");

            // Write the JSON data to the file
            File.WriteAllText(filePath, json);

            Console.WriteLine($"\nFile saved to {filePath}");
            Console.WriteLine("\n\nPress any key to exit...");
            Console.ReadKey();

            Environment.Exit(0);
        }
    }

    private static string? ValidateUrl(string? url)
    {
        var envRegex = DvUrlRegex();
        if (!string.IsNullOrWhiteSpace(url) && envRegex.IsMatch(url))
        {
            return url;
        }
        else
        {
            return null;
        }
    }

    [GeneratedRegex("^https:\\/\\/[a-zA-Z0-9-]+\\.crm\\d+\\.dynamics\\.com$")]
    private static partial Regex DvUrlRegex();
}
