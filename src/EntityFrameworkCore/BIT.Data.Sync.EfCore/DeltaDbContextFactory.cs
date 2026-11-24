using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Linq;
using System.Reflection;

namespace BIT.EfCore.Sync
{
    /// <summary>
    /// Builds the per-operation <see cref="DeltaDbContext"/> factories used by <see cref="EfDeltaStore"/> and <see cref="EfSequenceService"/>.
    /// </summary>
    internal static class DeltaDbContextFactory
    {
        /// <summary>
        /// A factory that creates new contexts of the same type and options as <paramref name="template"/>.
        /// The template itself is only read here, never used for queries or saves.
        /// </summary>
        public static Func<DeltaDbContext> FromTemplate(DeltaDbContext template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            var type = template.GetType();
            var options = template.GetService<IDbContextOptions>() as DbContextOptions;
            if (type == typeof(DeltaDbContext))
                return () => new DeltaDbContext(options);

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var withOptions = options == null ? null : type.GetConstructors(flags).FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(options);
            });
            if (withOptions != null)
                return () => (DeltaDbContext)withOptions.Invoke(new object[] { options });

            var parameterless = type.GetConstructor(flags, null, Type.EmptyTypes, null);
            if (parameterless != null)
                return () => (DeltaDbContext)parameterless.Invoke(null);

            throw new ArgumentException($"Cannot create new instances of {type.FullName}; pass a Func<DeltaDbContext> factory instead.", nameof(template));
        }

        public static Func<DeltaDbContext> FromOptions(DbContextOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            return () => new DeltaDbContext(options);
        }
    }
}
