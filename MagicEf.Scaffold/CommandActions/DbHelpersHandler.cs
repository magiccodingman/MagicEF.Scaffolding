using MagicEf.Scaffold.Helpers;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace MagicEf.Scaffold.CommandActions
{
    public class DbHelpersHandler : CommandHandlerBase
    {
        public override void Handle(string[] args)
        {
            // Parse required arguments
            string? dbHelpersPath = ArgumentHelper.GetArgumentValue(args, "--dbHelpers");
            string? customContextFilePath = ArgumentHelper.GetArgumentValue(args, "--customContextFilePath");

            if (string.IsNullOrEmpty(dbHelpersPath) || string.IsNullOrEmpty(customContextFilePath))
            {
                Console.WriteLine("Error: Both --dbHelpers and --customContextFilePath arguments are required.");
                return;
            }

            // Ensure the DbHelpers directory exists
            if (!Directory.Exists(dbHelpersPath))
            {
                Directory.CreateDirectory(dbHelpersPath);
            }

            // Read and parse the custom context file
            if (!File.Exists(customContextFilePath))
            {
                Console.WriteLine($"Error: The file {customContextFilePath} does not exist.");
                return;
            }

            var code = FileHelper.ReadFile(customContextFilePath);
            var root = RoslynHelper.ParseCode(code);

            // Extract the class name and namespace
            // Get the class name directly from the syntax tree
            var classDeclaration = root.DescendantNodes()
                                       .OfType<ClassDeclarationSyntax>()
                                       .FirstOrDefault();

            if (classDeclaration == null)
            {
                Console.WriteLine("Error: Could not find a class in the file.");
                return;
            }

            var className = classDeclaration.Identifier.Text;
            var namespaceName = RoslynHelper.GetNamespace(root);

            if (string.IsNullOrEmpty(className))
            {
                Console.WriteLine("Error: Could not extract class name.");
                return;
            }

            if (string.IsNullOrEmpty(namespaceName))
            {
                Console.WriteLine("Error: Could not extract namespace.");
                return;
            }

            // Generate the required files in the DbHelpers directory
            CreateDbCacheClass(dbHelpersPath, namespaceName, className);
            CreateDbHelperClass(dbHelpersPath, namespaceName, className);
            CreateEntityHelperClass(dbHelpersPath, namespaceName);
            CreateIReadOnlyRepositoryInterface(dbHelpersPath, namespaceName, className);
            CreateIRepositoryInterface(dbHelpersPath, namespaceName);
            CreateReadOnlyRepositoryBaseClass(dbHelpersPath, namespaceName, className);
            CreateRepositoryBaseClass(dbHelpersPath, namespaceName, className);
            CreateLazyLoadExtensionClass(dbHelpersPath, namespaceName, className);

            Console.WriteLine("DbHelpers files have been generated successfully.");
        }

        private void CreateDbCacheClass(string dbHelpersPath, string namespaceName, string className)
        {
            string fileName = "DbCache.cs";
            string filePath = Path.Combine(dbHelpersPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} already exists.");
                return;
            }

            string content = $@"namespace {namespaceName}
{{
    public static class DbCache
    {{
        public static {className}.DbEnvironment dbEnvironment {{ get; set; }}
        public static string DecryptedConnectionString {{ get; set; }}
    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"Created {fileName}");
        }

        private void CreateDbHelperClass(string dbHelpersPath, string namespaceName, string className)
        {
            string fileName = "DbHelper.cs";
            string filePath = Path.Combine(dbHelpersPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} already exists.");
                return;
            }
            /*
             public {className} GetContext(string ConnectionString)
        {{
            return new {className}(new DbContextOptionsBuilder<{className}>().UseSqlServer(ConnectionString).Options);
        }}
             */
            string content = $@"using Microsoft.EntityFrameworkCore;

namespace {namespaceName}
{{
    public class DbHelper
    {{

        public {className} Get{className}()
        {{
            return new {className}(new DbContextOptionsBuilder<{className}>().UseSqlServer(new {className}().GetConnectionString()).Options);
        }}
    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"Created {fileName}");
        }

        private void CreateEntityHelperClass(string dbHelpersPath, string namespaceName)
        {
            string fileName = "EntityHelper.cs";
            string filePath = Path.Combine(dbHelpersPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} already exists.");
                return;
            }

            string content = $@"using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace {namespaceName}
{{
    public static class EntityHelper
    {{
        public static PropertyInfo[] GetKeyProperties(Type EntityType) 
        {{
            var properties = EntityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties.Where(p => p.GetCustomAttribute<KeyAttribute>() != null).ToArray();
        }}

        public static PropertyInfo[] GetKeyProperties<T>() where T : class
        {{
            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            return properties.Where(p => p.GetCustomAttribute<KeyAttribute>() != null).ToArray();
        }}
    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"Created {fileName}");
        }

        private void CreateIReadOnlyRepositoryInterface(string dbHelpersPath, string namespaceName, string className)
        {
            string fileName = "IReadOnlyRepository.cs";
            string filePath = Path.Combine(dbHelpersPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} already exists.");
                return;
            }

            string content = $@"using System.Collections.Generic;

namespace {namespaceName}
{{
    public interface IReadOnlyRepository<TEntity>
    {{
        TEntity GetById(object id);
        IQueryable<TEntity> GetAll({className}? _ContextOverride = null);
        IQueryable<TEntity> GetAllWithContext(out {className} context);
        IQueryable<TEntity> GetAllNoTracking({className}? _ContextOverride = null);
        IQueryable<TEntity> GetAllNoTrackingWithContext(out {className} context);
    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"Created {fileName}");
        }

        private void CreateIRepositoryInterface(string dbHelpersPath, string namespaceName)
        {
            string fileName = "IRepository.cs";
            string filePath = Path.Combine(dbHelpersPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} already exists.");
                return;
            }

            string content = $@"using System.Collections.Generic;

namespace {namespaceName}
{{
    public interface IRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : class
    {{
        void Add(TEntity entity);
        void AddRange(IEnumerable<TEntity> entities);
        void Update(TEntity entity);
        void UpdateRange(IEnumerable<TEntity> entities);
        void Delete(TEntity entity);
        void DeleteRange(IEnumerable<TEntity> entities);
    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"Created {fileName}");
        }

        private void CreateReadOnlyRepositoryBaseClass(string dbHelpersPath, string namespaceName, string className)
        {
            string fileName = "ReadOnlyRepositoryBase.cs";
            string filePath = Path.Combine(dbHelpersPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} already exists.");
                return;
            }

            string content = $@"using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace {namespaceName}
{{
    public class ReadOnlyRepositoryBase<TEntity> where TEntity : class
    {{
        protected {className} _dbContext {{ get; set; }}
        protected DbSet<TEntity> _dbSet;

        public ReadOnlyRepositoryBase()
        {{
            _dbContext = new DbHelper().Get{className}();
            _dbSet = _dbContext.Set<TEntity>();
        }}
        public virtual IQueryable<TEntity> GetAll({className}? _ContextOverride = null)
        {{
            if (_ContextOverride != null)
                return _ContextOverride.Set<TEntity>();
            else
                return _dbSet;
        }}
        
        public virtual IQueryable<TEntity> GetAllWithContext(out {className} context)
        {{
            context = _dbContext;
            return GetAll(context);
        }}

        public virtual IQueryable<TEntity> GetAllNoTracking({className}? _ContextOverride = null)
        {{
            if (_ContextOverride != null)
                return _ContextOverride.Set<TEntity>().AsNoTracking();
            else
                return _dbSet.AsNoTracking();
        }}

        public virtual IQueryable<TEntity> GetAllNoTrackingWithContext(out {className} context)
        {{
            context = _dbContext;
            return GetAllNoTracking(context);
        }}

    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"Created {fileName}");
        }

        private void CreateLazyLoadExtensionClass(string dbHelpersPath, string namespaceName, string className)
        {
            string fileName = "LazyLoadExtension.cs";
            string filePath = Path.Combine(dbHelpersPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} already exists.");
                return;
            }

            string content = $@"using System.Linq.Expressions;

namespace {namespaceName}
{{
    public static class LazyLoadExtension
    {{
        public static TProperty LazyLoad<TEntity, TProperty>(
    this TEntity entity,
    Expression<Func<TEntity, TProperty?>> navigationExpression
) where TEntity : class
  where TProperty : class
        {{
            // Get the compiled accessor for the navigation property
            var navigationAccessor = navigationExpression.Compile();

            // Get the current value of the navigation property
            var navigationProperty = navigationAccessor(entity);

            if (navigationProperty == null)
            {{
                using (var dbContext = new DbHelper().Get{className}())
                {{
                    // Attach the entity to the context if necessary
                    var entry = dbContext.Entry(entity);

                    if (!entry.IsKeySet)
                    {{
                        dbContext.Attach(entity);
                    }}

                    // Load the navigation property explicitly
                    entry.Reference(navigationExpression).Load();

                    // Update the navigation property after loading
                    navigationProperty = navigationAccessor(entity);
                }}
            }}

            // Return the now-loaded navigation property
            return navigationProperty!;
        }}
    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"Created {fileName}");
        }

        private void CreateRepositoryBaseClass(string dbHelpersPath, string namespaceName, string className)
        {
            string fileName = "RepositoryBase.cs";
            string filePath = Path.Combine(dbHelpersPath, fileName);

            if (File.Exists(filePath))
            {
                Console.WriteLine($"File {fileName} already exists.");
                return;
            }

            string content = $@"using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace {namespaceName}
{{
    public abstract class RepositoryBase<TEntity> : ReadOnlyRepositoryBase<TEntity> where TEntity : class
    {{
        public virtual void Add(TEntity entity)
        {{
            try
            {{
                _dbContext.Set<TEntity>().Add(entity);
                _dbContext.SaveChanges();
            }}
            catch (DbUpdateException e)
            {{
                LogDbErrors(e);
                throw;
            }}
        }}

        public virtual void AddRange(IEnumerable<TEntity> entities)
        {{
            try
            {{
                _dbContext.Set<TEntity>().AddRange(entities);
                _dbContext.SaveChanges();
            }}
            catch (DbUpdateException e)
            {{
                LogDbErrors(e);
                throw;
            }}
        }}

        public virtual void Update(TEntity entity)
        {{
            try
            {{
                if (_dbContext.Entry(entity).State == EntityState.Detached)
                {{
                    _dbContext.Set<TEntity>().Attach(entity);
                }}
                _dbContext.Entry(entity).State = EntityState.Modified;
                _dbContext.SaveChanges();
            }}
            catch (DbUpdateException e)
            {{
                LogDbErrors(e);
                throw;
            }}
        }}

        public virtual void UpdateRange(IEnumerable<TEntity> entities)
        {{
            try
            {{
                foreach (TEntity entity in entities)
                {{
                    if (_dbContext.Entry(entity).State == EntityState.Detached)
                    {{
                        _dbContext.Set<TEntity>().Attach(entity);
                    }}
                    _dbContext.Entry(entity).State = EntityState.Modified;
                }}
                _dbContext.SaveChanges();
            }}
            catch (DbUpdateException e)
            {{
                LogDbErrors(e);
                throw;
            }}
        }}

        public virtual void Delete(TEntity entity)
        {{
            try
            {{
                _dbContext.Set<TEntity>().Remove(entity);
                _dbContext.SaveChanges();
            }}
            catch (DbUpdateException e)
            {{
                LogDbErrors(e);
                throw;
            }}
        }}

        public virtual void DeleteRange(IEnumerable<TEntity> entities)
        {{
            try
            {{
                _dbContext.Set<TEntity>().RemoveRange(entities);
                _dbContext.SaveChanges();
            }}
            catch (DbUpdateException e)
            {{
                LogDbErrors(e);
                throw;
            }}
        }}

        private void LogDbErrors(DbUpdateException e)
        {{
            Console.WriteLine(""An error occurred updating the database: "" + e.Message);
            if (e.InnerException != null)
            {{
                Console.WriteLine(""Inner Exception: "" + e.InnerException.Message);
            }}
        }}
    }}
}}";

            File.WriteAllText(filePath, content);
            Console.WriteLine($"Created {fileName}");
        }
    }
}
