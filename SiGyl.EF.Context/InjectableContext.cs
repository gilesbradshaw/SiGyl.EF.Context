using  SiGyl.EF.Context.Infrastructure;
using  SiGyl.EF.Context.Processors;
using SiGyl.Models.Infrastructure.ChangeDetection;
using StructureMap;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Transactions;

namespace  SiGyl.EF.Context
{
    public abstract class InjectableContext : DbContext
    {
        public InjectableContext(string nameOrConnectionString) : base(nameOrConnectionString) {
			//var objectContext = (this as IObjectContextAdapter).ObjectContext;

			// Sets the command timeout for all the commands
			//objectContext.CommandTimeout = 300;

		}

        public void Init()
        {
            Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, " ALTER DATABASE [" + Database.Connection.Database + "] SET ALLOW_SNAPSHOT_ISOLATION ON ");
            Database.ExecuteSqlCommand(TransactionalBehavior.DoNotEnsureTransaction, " ALTER DATABASE [" + Database.Connection.Database + "] SET READ_COMMITTED_SNAPSHOT ON ");

        }



        protected void DetectChanges()
        {
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;
            if (objectContext != null)
            {
                objectContext.DetectChanges();
            }
        }
        public Changes DetectChanges(Changes changes)
        {
            var newChanges = new Changes();
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;

            //could be a proxy for testing &c
            if (objectContext != null)
            {
               
                objectContext.DetectChanges();
                var oldAdded = changes.Added!=null ? changes.Added.Select(e=>e.Entity).ToList() : new List<object>();
                changes.Added = objectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Added).Where(e => !e.IsRelationship).ToList();
                newChanges.Added = changes.Added.Where(a => !oldAdded.Contains(a.Entity)).ToList();
                var oldDeleted = changes.Deleted != null ? changes.Deleted.ToList() : new List<object>();
                
            changes.Deleted = objectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Deleted).Where(e=>!e.IsRelationship).ToList().Select(e =>
                    {

                        var deletedType = this.GetType().Assembly.DefinedTypes.Single(t => t.Name == e.EntitySet.ElementType.Name);
                        object ret = oldDeleted.SingleOrDefault(o=>{
                            if (e.Entity.GetType() != o.GetType()) return false;
                            foreach (var ek in e.EntityKey.EntityKeyValues)
                            {
                                var p = deletedType.GetProperty(ek.Key);
                                if(!p.GetValue(e.Entity).Equals(p.GetValue(o)))
                                    return false;
                            
                            }
                            return true;
                        });
                        if(ret!=null)
                            return ret;
                        ret = deletedType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                        foreach (var ek in e.EntityKey.EntityKeyValues)
                        {
                            var p = ret.GetType().GetProperty(ek.Key);
                            p.SetValue(ret, p.GetValue(e.Entity));
                        }

                        //lazy for minute just copy all int props
                        foreach (var pk in e.Entity.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p=>p.SetMethod!=null && p.PropertyType==typeof(Int32)))
                        {
                            
                            pk.SetValue(ret, pk.GetValue(e.Entity));
                        }
                        
                        return ret;
                    }
                    ).ToList();

                newChanges.Deleted = changes.Deleted.Where(a => !oldDeleted.Contains(a)).ToList();
                var oldModified = changes.Modified !=null ? changes.Modified.ToList() : new List<ModifiedObjectState>(); // != null ? changes.Modified.Select(e => e.Entity).ToList() : new List<object>();
                var oldEntities = changes.Modified !=null? changes.Modified.Select(o=>o.ObjectStateEntry.Entity).ToList() : new List<object>();
                changes.Modified = objectContext.ObjectStateManager.GetObjectStateEntries(EntityState.Modified).Where(e => !e.IsRelationship).Select(e => new ModifiedObjectState { ObjectStateEntry = e, ModifiedProperties = e.GetModifiedProperties() }).ToList();//  e.ToList();
                
                newChanges.Modified= changes.Modified.Where(a => !oldEntities.Contains(a.ObjectStateEntry.Entity)
                    || a.ModifiedProperties.Any(p=> !oldModified.Single(om=>om.ObjectStateEntry.Entity== a.ObjectStateEntry.Entity).ModifiedProperties.Contains(p))).ToList();
                
            }
        

            newChanges.Context = this as IInjectableContextAsync;
            changes.Context = this as IInjectableContextAsync;
            return newChanges;
        
        }

        public override int SaveChanges()
        {
            DetectChanges();
            return base.SaveChanges();
        }

        
    }



    
}



