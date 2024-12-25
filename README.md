# Magic EF Scaffolding

Magic EF Scaffolding is a revolutionary project designed to make database-first Entity Framework (EF) the powerhouse it was always meant to be. Say goodbye to the perceived downsides of database-first and embrace automation and ease that will make you wonder why anyone would ever choose code-first! If you've been longing for a truly optimized workflow for database-first development, you're in for a treat.

Read the article on Magic EF to fully digest and understand the capabilities!
[Magic EF Article](https://magiccodingman.com/MagicEf)

## Wait there's more?
Yes there's more! Much more to the Magic EF protocol. Please reference the Wiki to see additional arguments and capabilities:
[Magic EF Wiki](https://github.com/magiccodingman/MagicEF.Scaffolding/wiki)

Additional features not discussed in the read me is:
1.) Pipeline Migration Runner Protocol
2.) View DTO Scaffolding Protocol

The Read Me here mostly goes over the primary Magic EF protocol.

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

## Video Tutorials
I made some video tutorials. Tried my best to go over what's important. In my opinion, the GIT/Wiki is easier and faster to digest, but I also prefer reading! Up to you, I just wanted to make it easier to learn how to use MagicEF with however you wish to digest :)

[How to Setup](https://www.youtube.com/watch?v=tst1XOHJbb8)

[Overview & How to Use](https://www.youtube.com/watch?v=oRbfcgw2Q1c)

## Automated Setup & Use
This automated initial setup will automatically set up your project based on the suggested protocol and specifications.

### Option 1: Automated setup W/ Script (recommended)
Use the ``Scaffold_Script_Exemple.ps1`` provided in the repository. Simply place the file in your Csharp project directory location next to the csproj file. Edit the file and replace the first 4 variables based on your specifications:
```ps1
# Replace this with your actual connection string
$connectionString = "{Your_Connection_String}" # Use a safe string like AD auth

# Define user-specified variables
$projectFileName = "{csproj_file_name}.csproj" # Name of your project file (assumes .\ by default)
$namespace = "{Project_Namespace}"             # Project namespace
$dbContextFile = "{Your_DbContext_Name}"            # Name of your DbContext file
```

Then open a powershell or command line (any OS works) and CD to the project directory. Finally run ``.\Scaffold_Script_Example.ps1`` or whatever is the equivalent in your OS. And then like magic it'll install everything for you! It's highly suggested you utilize this route to work with Magic EF. Anything else will require deeper understanding and is more likely for enterprise level setup where additional separate is required. 

## Re-Use The script (the script is magic!)
You can re-run this script safely over and over however many times you wish. Your changes will not be removed or altered. This will scaffold your database and apply new MagicEF protocol extensions whenever you make database changes. This script duals as the initial setup and a fantastic easy to use script for use whenever you want to run the scaffold. Magic EF is meant to be used alongside DotNet scaffolding and this bundles it together for you!

## Script Use In Pipelines
This script can be utilized within Azure pipeline or any pipeline process. As all the safety features are baked in on your behalf. You can now utilize database first like never before in pipelines across environments safely! How though?! Bwahahaha, let me tell you! The following commands resolve legendary database first pipeline environment issues:

``--ambiguousIndex``

``--removeOnConfiguring``

MagicEF runs after dotnet scaffold. Once a dotnet scaffold occurs, code breaking changes occurs within the scaffolded DbContext (aka the MagicEF ReadOnlyDbContext). Additional ambiguous index issues is commonplace on all scaffolded models. These commands remove code breaking changes that occur after any dotnet scaffold. Safely allowing you to proceed after a scaffold to match your context to the environment you're moving too! And baked into MagicEF's protocol is a process that avoids many more significant challenges that normally occur with pipeline environment changes.

### Option 2: Automated setup W/O script
You can run the following command to start the initial setup for your project. This isn't fully suggested nor is it fully considered, "Automated" without the script. As you'll still need to following the rest of the manual setup instructions. Though you can skip steps 1-3 if you run this first.
```bash
MagicEf --initialSetup --projectFilePath "{Full_Path_To_csproj_File}" --namespace "{Project_Namespace}" --dbContext "{Desired_DbContext_Class_Name}"
```


## Manual Setup
The following is the instructions to manually setup MagicEF
#### Step 1: Install Required NuGet Packages
Navigate to the directory of your class library and run the following commands:
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package  Microsoft.EntityFrameworkCore.Design
dotnet add package  Microsoft.EntityFrameworkCore.Proxies
```

#### Step 2: Directory Structure
Create the following folder structure in your project:
- `Concrete`
- `DbHelpers`
- `DbModels`
- `Extensions`
- `Interfaces`
- `MetaDataClasses`

#### Step 3: CS file Manual Initial setup
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
        => optionsBuilder.UseSqlServer(GetConnectionString());

    public string GetConnectionString()
    {
        // Write your logic to return the connection string
        return null; // return the actual connection string!
    }
}
```

**Important**: Copy this template exactly, including the inheritance from `ReadOnlyDbContext`, which will be generated in later steps. I also suggest you create this after you run the `--scaffoldProtocol` first.

Then pre-create the ReadOnlyDbContext with the following example. This'll get overwritten, but it's just to have the code not freak out on the initial run:
```csharp
public partial class ReadOnlyDbContext : DbContext
{
    public ReadOnlyDbContext()
    {
    }

    public ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options)
        : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseSqlServer("");
    }
```

#### Step 4: Scaffolding with `dotnet ef`

You can scaffold directly utilizing the command line if you wish, but I'm going to show you how to do this with PowerShell.

```powershell
cd "<path-to-your-project>"

# Read the connection string from a file
$connectionString = Get-Content -Path "ExampleConnectionString.txt"

# Define paths
$modelsDirectory = "DbModels"   # Scaffolded models directory
$contextDirectory = "."         # DbContext remains in the base directory

# Execute scaffolding
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

## Repository Base, Context Passing, and Advanced Querying

The repository base provided in this project introduces several powerful features to streamline database access and simplify complex queries. These features include context sharing, `WithContext` methods for effortless context management, and a robust `LazyLoad` helper for post-context data retrieval.

---

### Repository Base Overview

At its core, the repository base provides methods for retrieving entities from the database using Entity Framework Core. These methods are designed with flexibility in mind, allowing you to:
1. Override the default `DbContext` when needed.
2. Use "no tracking" for optimized read-only queries.
3. Extract the context (`WithContext`) for easy reuse in multi-repository operations.

#### Method Definitions

1. **`GetAll`**
   ```csharp
   public virtual IQueryable<TEntity> GetAll(DbContext _ContextOverride = null)
   {
       if (_ContextOverride != null)
           return _ContextOverride.Set<TEntity>();
       else
           return _dbSet;
   }
   ```
   Retrieves all entities of type `TEntity`. It uses the default context unless an override is provided.

2. **`GetAllNoTracking`**
   ```csharp
   public virtual IQueryable<TEntity> GetAllNoTracking(DbContext _ContextOverride = null)
   {
       if (_ContextOverride != null)
           return _ContextOverride.Set<TEntity>().AsNoTracking();
       else
           return _dbSet.AsNoTracking();
   }
   ```
   Similar to `GetAll`, but disables change tracking, making it ideal for read-only queries.

3. **`GetAllWithContext`**
   ```csharp
   public virtual IQueryable<TEntity> GetAllWithContext(out DbContext context)
   {
       context = _dbContext;
       return GetAll(context);
   }
   ```
   Returns the entities while also providing the active context via an `out` parameter. This enables easy sharing of the context for subsequent queries.

4. **`GetAllNoTrackingWithContext`**
   ```csharp
   public virtual IQueryable<TEntity> GetAllNoTrackingWithContext(out DbContext context)
   {
       context = _dbContext;
       return GetAllNoTracking(context);
   }
   ```
   Combines the advantages of "no tracking" queries with the ability to extract the active context.

---

### Context Passing with `WithContext`

Using `WithContext` methods allows developers to efficiently reuse the same context across multiple repository calls, simplifying complex operations.

#### Example: Context Passing in Joins

```csharp
var data = new EntityARepository().GetAllWithContext(out var sharedContext)
    .Where(a => a.IsActive)
    .Join(new EntityBRepository().GetAll(sharedContext),
          a => a.ForeignKeyId,
          b => b.Id,
          (a, b) => new { EntityA = a, EntityB = b })
    .ToList();
```

In this example:
- The `sharedContext` is extracted once using `GetAllWithContext`.
- It is then reused across multiple repositories to ensure a single database connection is used.

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

---

### Advanced Lazy Loading: `LazyLoad`

Lazy loading traditionally depends on an active Entity Framework context, which is not available after a query is executed. The `LazyLoad` helper enables post-context lazy loading, allowing you to load related data even after the context has been disposed.

#### LazyLoad Usage

```csharp
var entity = new EntityRepository().GetById(1);
var relatedData = entity.LazyLoad(x => x.RelatedEntity);
```

In this example:
- `LazyLoad` dynamically fetches the `RelatedEntity` of `entity` without requiring an active context.
- This feature is ideal for scenarios where additional data is needed after the primary query execution.

---

### Best Practices for Query Performance

1. **Pre-loading Collections**
   For collections, it's recommended to pre-load related data using `Include` to minimize performance overhead:
   ```csharp
   var entities = new EntityRepository().GetAll().Include(x => x.RelatedEntities).ToList();
   ```

2. **Avoid Overusing LazyLoad**
   Use `LazyLoad` sparingly for single-entity relationships. For collections, pre-loading is preferable to avoid multiple database calls.

---


## Conclusion

Magic EF Scaffolding revolutionizes database-first workflows by automating tedious tasks, enabling effortless integration of database changes into your C# code. Whether you’re running locally or in a pipeline, this tool makes database-first EF development simple, efficient, and scalable. Say goodbye to manual adjustments and embrace the future of database-first workflows!

### Extra Notes
I made this project quite some time ago, but wanted to rebuild it into a significantly more production worthy state. And this need was extreme for me when I needed proper environmental pipeline scaffolding. The OnModelCreating that's generated when scaffolding isn't technically required, but it is so helpful for performance! The ability to generate it in a pipeline process so that it meets any environment was critical for me. And I hope you see how crticial it can become for you. I cannot code without this setup anymore. This has become my new standard, protocal, and my desire for working with literally any database.

I will never use code first again personally. Who knows though, did I convince you too?!
