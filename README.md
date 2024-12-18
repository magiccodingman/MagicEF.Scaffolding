# Magic EF Scaffolding

Magic EF Scaffolding is a revolutionary project designed to make database-first Entity Framework (EF) the powerhouse it was always meant to be. Say goodbye to the perceived downsides of database-first and embrace automation and ease that will make you wonder why anyone would ever choose code-first! If you've been longing for a truly optimized workflow for database-first development, you're in for a treat.

## Prerequisites

### Required Tools
This project works alongside `dotnet ef dbcontext`. You’ll need to install the following tools:

#### Install `dotnet ef`
In your terminal or command prompt:
```bash
dotnet tool install --global dotnet-ef
```

#### You can use the DotNET tool install from Nuget
```bash
dotnet tool install --global MagicEf
```
Then you can use this in any environment easily and not have to target the exe and so on.

#### Recommended Setup
It’s highly recommended to use a separate C# class library for your database models and scaffolding. Combining this with your primary project is not advised. Create a new C# Class Library project if you haven’t already.

#### Install Required NuGet Packages
Navigate to the directory of your class library and run the following commands:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package  Microsoft.EntityFrameworkCore.Design
dotnet add package  Microsoft.EntityFrameworkCore.Proxies
```

Please note that the, "Microsoft.EntityFrameworkCore.Design" may need to be added with the version matching your framework. I personally have a NET 8.0 project and am using the 8.0.0 version and added all of these as a nuget package to my project.

## Project Setup

### Directory Structure
Create the following folder structure in your project:
- `Concrete`
- `DbHelpers`
- `DbModels`
- `Extensions`
- `Interfaces`
- `MetaDataClasses`

### Initial DbContext Setup
At the base directory of your project, create a new C# file for your custom `DbContext`. The filename is up to you. Use the following template for the class:

```csharp
public partial class MyDbContext : ReadOnlyDbContext
{
    public MyDbContext()
    {
    }

    public MyDbContext(DbContextOptions<ReadOnlyDbContext> options)
        : base(options) // Pass the correct type to the base class constructor
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(GetConnectionString()).UseLazyLoadingProxies(); // Adding Lazy loading proxies is optional. Use what you want or don't use whatever.

    public string GetConnectionString()
    {
        // Write your logic to return the connection string
    }
}
```

**Important**: Copy this template exactly, including the inheritance from `ReadOnlyDbContext`, which will be generated in later steps. I also suggest you create this after you run the `--scaffoldProtocol` first.

## Scaffolding with `dotnet ef`

To scaffold your database models, use the following PowerShell command:

```powershell
cd "<path-to-your-project>"

# Read the connection string from a file
$connectionString = Get-Content -Path "ExampleConnectionString.txt"

# Define paths
$modelsDirectory = "DbModels"   # Scaffolded models directory
$contextDirectory = "."         # DbContext remains in the base directory

# Execute scaffolding
```
```powershell
dotnet ef dbcontext scaffold $connectionString Microsoft.EntityFrameworkCore.SqlServer `
    --context ReadOnlyDbContext `
    --output-dir $modelsDirectory `
    --context-dir $contextDirectory `
    --namespace DataAccess `
    --force `
    --data-annotations
```

### Notes:
- Adjust paths as necessary for your setup.
- The `DbModels` directory will contain your scaffolded models.
- Do not edit scaffolded models or the `ReadOnlyDbContext` class, as they will be overwritten when re-scaffolded in the future.

## Magic EF Scaffolding

This is where the magic happens! Once `dotnet ef` has scaffolded your models and context, Magic EF Scaffolding automates further enhancements and organization for an efficient database-first workflow.

### Installation and Setup
If you want to do this manually, you can, but I suggest just using the dotnet tool MagicEF from Nuget.
- Clone the Magic EF Scaffolding repository.
- Build the project to generate the executable file (`MagicEf.exe`).
- Optionally, add the executable to your system path for easy access, or call it directly using its full path.

### Command Examples

Here are example commands and their purposes. Replace the paths with your project-specific paths.

#### Fix Ambiguous Index
```bash
MagicEF --ambiguousIndex --directoryPath "<path-to-DbModels-folder>"
```
This command resolves common issues with ambiguous context in scaffolded models.

#### Remove `OnConfiguring`
```bash
MagicEF --removeOnConfiguring --filePath "<path-to-ReadOnlyDbContext.cs>"
```
Removes the `OnConfiguring` method from the scaffolded `ReadOnlyDbContext`, ensuring it exists only in your custom `DbContext` class for better control.

#### Remove `separateVirtualProperties`
```bash
MagicEF --separateVirtualProperties --directoryPath "<path-to-DbModels-folder>"
```
Separates the virtual properties from the scaffold models into a separate file appended with, "SeparateVirtual" to the file name. The virtual properties are then added to a partial class. Thus functioning identically but making GIT control better when changes occur. Separating actual table/model changes from reference changes.

#### Generate Helper Files
```bash
MagicEF --dbHelpers "<path-to-DbHelpers-folder>" --customContextFilePath "<path-to-custom-DbContext.cs>"
```
Generates essential helper files in the `DbHelpers` folder to simplify database interactions.

#### Scaffold Protocol
```bash
MagicEF --scaffoldProtocol \
    --concretePath "<path-to-Concrete-folder>" \
    --modelPath "<path-to-DbModels-folder>" \
    --extensionPath "<path-to-Extensions-folder>" \
    --metaDataPath "<path-to-MetaDataClasses-folder>" \
    --interfacesPath "<path-to-Interfaces-folder>" \
    --projectFilePath "<path-to-project.csproj>"
```
Generates metadata, extensions, interfaces, and concrete files for scaffolded models, enhancing your workflow without overwriting existing files.

### Workflow Automation
For efficiency, create a script combining EF scaffolding and Magic EF Scaffolding. Automate this process in local development or CI/CD pipelines to ensure your scaffolding aligns with database changes.

### Generated File Types
1. **Concrete Files**: Contains methods like `GetById` with automatically generated parameters.
2. **Extension Files**: Add custom properties or methods to models using the `NotMapped` attribute.
3. **Interface Files**: Define model contracts.
4. **Metadata Files**: Store auxiliary data about models.
5. **Helper Files**: Simplify database access and operations.

## Example Usage

Here’s how easy it becomes to use your database-first models:

### Retrieve a Context
```csharp
using (var _dbContext = new DbHelper().GetMyDbContext())
{
    // Your database logic here
}
```

### Repository Pattern
Automatically generated repository classes make CRUD operations straightforward:

#### Add Entities
```csharp
repository.Add(entity);
repository.AddRange(entities);
```

#### Update Entities
```csharp
repository.Update(entity);
repository.UpdateRange(entities);
```

#### Delete Entities
```csharp
repository.Delete(entity);
repository.DeleteRange(entities);
```

#### Query Entities
```csharp
var results = repository.GetAllNoTracking().Where(x => x.Id == 2).ToList();
```

### LINQ Integration
Leverage LINQ to build SQL queries seamlessly:
```csharp
var result = repository.GetAll().FirstOrDefault(x => x.Name == "Sample");
```

---

## Repository Base and Context Passing

### Repository Base Overview

The repository base in this project provides two foundational methods for querying entities: `GetAll` and `GetAllNoTracking`. These methods allow seamless access to your database entities while offering flexibility to override the default `DbContext` used for querying. This flexibility ensures optimal performance and control, especially in scenarios requiring shared database contexts.

#### Method Definitions

1. **`GetAll`**
    ```csharp
    public virtual IQueryable<TEntity> GetAll(SayouDbContext _ContextOverride = null)
    {
        if (_ContextOverride != null)
            return _ContextOverride.Set<TEntity>();
        else
            return _dbSet;
    }
    ```
   This method retrieves all entities of a specific type (`TEntity`). By default, it uses the repository's primary `DbContext`, but an override can be provided for custom scenarios.

2. **`GetAllNoTracking`**
    ```csharp
    public virtual IQueryable<TEntity> GetAllNoTracking(SayouDbContext _ContextOverride = null)
    {
        if (_ContextOverride != null)
            return _ContextOverride.Set<TEntity>().AsNoTracking();
        else
            return _dbSet.AsNoTracking();
    }
    ```
   Similar to `GetAll`, this method retrieves entities but disables change tracking to improve query performance. It is ideal for read-heavy operations where changes to entities are not needed.

### Context Passing Example

Passing a shared context allows you to group multiple repository calls under the same `DbContext`, which is particularly useful for operations involving joins or transactions.

#### Example Usage: Context Passing with Joins

```csharp
using (var sharedContext = new MyDbContext())
{
    // Query all eligible entities
    var query = new MyRepository().GetAllNoTracking(sharedContext)
        .Join(new AnotherRepository().GetAllNoTracking(sharedContext),
              x => x.ForeignKeyId,
              y => y.Id,
              (x, y) => new { EntityX = x, EntityY = y })
        .Where(joined => joined.EntityX.IsActive && joined.EntityY.CreatedDate > DateTime.UtcNow.AddMonths(-1))
        .Select(joined => new
        {
            EntityXName = joined.EntityX.Name,
            EntityYDescription = joined.EntityY.Description
        });

    foreach (var result in query)
    {
        Console.WriteLine($"EntityX: {result.EntityXName}, EntityY: {result.EntityYDescription}");
    }
}
```

In this example:
- The same `sharedContext` is passed to both repositories, ensuring a single connection to the database.
- The `Join` operation leverages LINQ to seamlessly combine data from multiple entities, optimized for SQL generation.

### Benefits of Context Passing
- **Performance**: Reduces the overhead of creating multiple `DbContext` instances.
- **Consistency**: Ensures all operations share the same transaction scope.
- **Flexibility**: Facilitates complex queries involving multiple repositories.

---

## Conclusion

Magic EF Scaffolding revolutionizes database-first workflows by automating tedious tasks, enabling effortless integration of database changes into your C# code. Whether you’re running locally or in a pipeline, this tool makes database-first EF development simple, efficient, and scalable. Say goodbye to manual adjustments and embrace the future of database-first workflows!

### Extra Notes
I made this project quite some time ago, but wanted to rebuild it into a significantly more production worthy state. And this need was extreme for me when I needed proper environmental pipeline scaffolding. The OnModelCreating that's generated when scaffolding isn't technically required, but it is so helpful for performance! The ability to generate it in a pipeline process so that it meets any environment was critical for me. And I hope you see how crticial it can become for you. I cannot code without this setup anymore. This has become my new standard, protocal, and my desire for working with literally any database.

I will never use code first again personally. Who knows though, did I convince you too?!
