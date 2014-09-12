using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SiGyl.EF.Context.Infrastructure.Injection
{
    public interface IContextInjector<TContext>
        where TContext: IContext
    {
        void Inject<T>(IInitializationExpression x);

    }


    public static class ContextInjector<TContext>
        where TContext : IContext
    {
        public static void Inject< TInjector>(TInjector injector, IInitializationExpression x)
        {

            var method = typeof(TInjector).GetMethod("Inject", BindingFlags.Public | BindingFlags.Instance);

              IEnumerable<Type> entityTypes = typeof(TContext).GetProperties().Where(pi => pi.PropertyType.IsGenericType && pi.PropertyType.Namespace == "System.Data.Entity" && pi.PropertyType.Name == "IDbSet`1")
                  .Select(p => p.PropertyType.GenericTypeArguments[0]);

              foreach (var t in entityTypes)
                  method.MakeGenericMethod(t).Invoke(injector, new object[]{x});

        }

        public static void Configure<TConfigurer>(TConfigurer configurer)
        {

            var method = typeof(TConfigurer).GetMethod("Configure", BindingFlags.Public | BindingFlags.Instance);

            IEnumerable<Type> entityTypes = typeof(TContext).GetProperties().Where(pi => pi.PropertyType.IsGenericType && pi.PropertyType.Namespace == "System.Data.Entity" && pi.PropertyType.Name == "IDbSet`1")
                .Select(p => p.PropertyType.GenericTypeArguments[0]);

            foreach (var t in entityTypes)
                method.MakeGenericMethod(t).Invoke(configurer, new object[] {});

        }
    }
}
