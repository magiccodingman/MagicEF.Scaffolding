# Define the connection string for Azure SQL with AAD authentication
# Replace this with your actual connection string
$connectionString = "{Your_Connection_String}" # Use a safe string like AD auth

# Define user-specified variables
$projectFileName = "{csproj_file_name}.csproj" # Name of your project file (assumes .\ by default)
$namespace = "{Project_Namespace}"             # Project namespace
$dbContextFile = "{Your_DbContext_Name}"            # Name of your DbContext Unique Class Name

# Define default directories and paths (users are advised not to change these)
$modelsDirectory = ".\DbModels"              # Models directory (default: .\DbModels)
$dbHelpersDirectory = ".\DbHelpers"          # Directory for DB helpers (default: .\DbHelpers)
$extensionsDirectory = ".\Extensions"        # Directory for extensions (default: .\Extensions)
$metaDataDirectory = ".\MetaDataClasses"     # Directory for metadata classes (default: .\MetaDataClasses)
$interfacesDirectory = ".\Interfaces"        # Directory for interfaces (default: .\Interfaces)
$concreteDirectory = ".\Concrete"            # Directory for concrete classes (default: .\Concrete)
$separateOutputVirtualDirectory = "" # (optional) Can specify separated virtual file directory. eg, ".\SeparatedVdFiles"

# ---------------------------------------------------------------
# Share Library Variables (Optional)
# These variables define the paths for the designated Share library locations,
# meant to be used with 'initialShareSetupHandler' and 'shareScaffoldProtocolHandler'.
# By default, they are empty, but when correctly set, they enable the share protocol.
# ---------------------------------------------------------------

# Define Share namespace (default set to ":" to indicate disabled state)
$shareNamespace = ":"

# Determine the base directory for Share library (assumes Share project is in the same solution)
$shareBasePath = (Join-Path (Get-Item $modelsDirectory).Parent.Parent.FullName $shareNamespace)

# Define Share library paths (assuming standard protocol unless manually changed)
$shareProjectFilePath = Join-Path $shareBasePath "$shareNamespace.csproj"
$shareReadOnlyInterfacesPath = Join-Path $shareBasePath "ReadOnlyInterfaces"
$shareInterfaceExtensionsPath = Join-Path $shareBasePath "InterfaceExtensions"
$shareReadOnlyModelsPath = Join-Path $shareBasePath "ReadOnlyModels"
$shareMetadataClassesPath = Join-Path $shareBasePath "MetaDataClasses"
$shareViewDtoModelsPath = Join-Path $shareBasePath "ViewDtoModels"
$shareSharedExtensionsPath = Join-Path $shareBasePath "SharedExtensions"
$shareSharedMetadataPath = Join-Path $shareBasePath "SharedMetaData"

# ---------------------------------------------------------------
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
    --metadataPath $metaDataDirectory `
    --interfacesPath $interfacesDirectory `
    --projectFilePath ".\$projectFileName" `
    --verbose

MagicEF --ambiguousIndex --directoryPath $modelsDirectory
MagicEF --removeOnConfiguring --filePath ".\ReadOnlyDbContext.cs"
MagicEF --separateVirtualProperties --directoryPath $modelsDirectory --outputPath $separateOutputVirtualDirectory
MagicEF --dbHelpers $dbHelpersDirectory --customContextFilePath ".\$dbContextFile.cs"

# ================= SHARE LIBRARY SETUP =================
# The Share Scaffold setup runs only if the shareNamespace is not the default ":"
if ($shareNamespace -ne ":") {
    Write-Host "Share Scaffold is enabled. Verifying paths and initializing..."

    # Check if required Share paths exist
    if (!(Test-Path $shareBasePath)) {
        Write-Host "ERROR: Share base path not found: $shareBasePath"
        exit 1
    }
    if (!(Test-Path $shareProjectFilePath)) {
        Write-Host "ERROR: Share project file not found: $shareProjectFilePath"
        exit 1
    }

    # Run the initial setup handler (this will create default Share directories if missing)
    MagicEF --initialShareSetupHandler --shareProjectFilePath "$shareProjectFilePath" --dbProjectFilePath ".\$projectFileName"

    # Revalidate the Share directories after potential creation
    $shareDirectories = @(
        $shareReadOnlyInterfacesPath,
        $shareInterfaceExtensionsPath,
        $shareReadOnlyModelsPath,
        $shareMetadataClassesPath,
        $shareViewDtoModelsPath,
        $shareSharedExtensionsPath,
        $shareSharedMetadataPath
    )

    $missingDirs = $shareDirectories | Where-Object { !(Test-Path $_) }

    if ($missingDirs.Count -gt 0) {
        Write-Host "ERROR: The following required Share directories are missing after setup:"
        $missingDirs | ForEach-Object { Write-Host $_ }
        exit 1
    }

    # Run Share Scaffold Protocol Handler
    MagicEF --shareScaffoldProtocolHandler `
        --shareNamespace "$shareNamespace" `
        --shareReadOnlyInterfacesPath "$shareReadOnlyInterfacesPath" `
        --shareInterfaceExtensionsPath "$shareInterfaceExtensionsPath" `
        --shareReadOnlyModelsPath "$shareReadOnlyModelsPath" `
        --shareMetadataClassesPath "$shareMetadataClassesPath" `
        --shareViewDtoModelsPath "$shareViewDtoModelsPath" `
        --shareSharedExtensionsPath "$shareSharedExtensionsPath" `
        --shareSharedMetadataPath "$shareSharedMetadataPath" `
        --dbNamespace "$namespace" `
        --dbModelsPath "$modelsDirectory" `
        --dbExtensionsPath "$extensionsDirectory" `
        --dbMetadataPath "$metaDataDirectory"

    Write-Host "Share Scaffold completed successfully."
} else {
    Write-Host "Share Scaffold is disabled. Set a valid shareNamespace to enable."
}

Write-Output "Script execution completed."