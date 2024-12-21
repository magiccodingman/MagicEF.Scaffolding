# Define the connection string for Azure SQL with AAD authentication
# Replace this with your actual connection string
$connectionString = "{Your_Connection_String}" # Use a safe string like AD auth

# Define user-specified variables
$projectFileName = "{csproj_full_file_name}" # Name of your project file (assumes .\ by default)
$namespace = "{Project_Namespace}"             # Project namespace
$dbContextFile = "{Your_DbContext_Name}"            # Name of your DbContext file

# Define default directories and paths (users are advised not to change these)
$modelsDirectory = ".\DbModels"              # Models directory (default: .\DbModels)
$dbHelpersDirectory = ".\DbHelpers"          # Directory for DB helpers (default: .\DbHelpers)
$extensionsDirectory = ".\Extensions"        # Directory for extensions (default: .\Extensions)
$metaDataDirectory = ".\MetaDataClasses"     # Directory for metadata classes (default: .\MetaDataClasses)
$interfacesDirectory = ".\Interfaces"        # Directory for interfaces (default: .\Interfaces)
$concreteDirectory = ".\Concrete"            # Directory for concrete classes (default: .\Concrete)

# Install and update MagicEf tool
dotnet tool install --global MagicEf
dotnet tool update --global MagicEf

MagicEf --initialSetup --projectFilePath ".\$projectFileName" --namespace "$namespace" --dbContext "$dbContextFile"

# Scaffold DbContext and models
dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer `
    --project ".\$projectFileName" `
    --context ReadOnlyDbContext `
    --output-dir $modelsDirectory `
    --context-dir "." `
    --namespace $namespace `
    --force `
    --data-annotations --verbose

# Use MagicEf for scaffolding and file organization
MagicEF --scaffoldProtocol `
    --concretePath $concreteDirectory `
    --modelPath $modelsDirectory `
    --extensionPath $extensionsDirectory `
    --metaDataPath $metaDataDirectory `
    --interfacesPath $interfacesDirectory `
    --projectFilePath ".\$projectFileName" `
    --verbose

MagicEF --ambiguousIndex --directoryPath $modelsDirectory
MagicEF --removeOnConfiguring --filePath ".\$dbContextFile.cs"
MagicEF --separateVirtualProperties --directoryPath $modelsDirectory
MagicEF --dbHelpers $dbHelpersDirectory --customContextFilePath ".\$dbContextFile.cs"

