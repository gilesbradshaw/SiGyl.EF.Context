using Newtonsoft.Json.Linq;
using  SiGyl.EF.Context.Processors;
using SiGyl.Models.Infrastructure.ChangeDetection;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Reflection;
using System.Threading.Tasks;

namespace  SiGyl.EF.Context
{
    public interface IInjectableContext : IObjectContextAdapter, IDisposable
    {

        void Init();
		
        DbEntityEntry Entry(object entity);
        DbEntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
        void SetModified(object i);
        object GetOriginal(object i);
        Changes DetectChanges(Changes changes);
  
		Func<Assembly, EntityType, JProperty[]> JsonModelExtender(Dictionary<string, IInjectableContext> contexts);
        

    }
}
