using  SiGyl.EF.Context.Infrastructure;
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
using System.Threading.Tasks;
using System.Transactions;

namespace  SiGyl.EF.Context.Processors
{
    public interface IProcessorAsync
    {

		Task ProcessChangesAsync(Changes changes);

    }
	public interface IPreProcessorAsync
    {
        Task doProcessChanges(Changes changes, object context);
    }
	public interface IPreProcessorAsync<TContext> : IPreProcessorAsync
        where TContext : IInjectableContextAsync
    {

        Task ProcessChanges(Changes changes, TContext context);

    }



	public class ProcessorAsync<ContextType> : ProcessorBaseAsync, IProcessorAsync
        where ContextType : IInjectableContextAsync
    {
      


        Func<Type, object, string, Task<bool>> tick = async (changeType, item, methodName) =>
        {
            var m = changeType.MakeGenericType(typeof(ContextType), item.GetType());

            var processors = ObjectFactory.GetAllInstances(m);
            foreach(var processor in processors)
               await (Task) processor.GetType().GetMethods().Single(method => method.Name == methodName && method.GetParameters().Count() == 1 && method.GetParameters().First().ParameterType == item.GetType()).Invoke(processor, new object[] { item });


            return true;
        };

        public virtual async Task ProcessChangesAsync(Changes changes)
        {
            if (Transaction.Current != null)
                throw new Exception("Attempt to post process a context within a transaction");
			if (CreateMethod != null && changes.Added != null)
				foreach (var c in changes.Added.Select(async e => await (Task)tick.Invoke(ChangeType, e.Entity, CreateMethod)))
					await c;
			if (ModifyMethod != null && changes.Modified != null)
				foreach (var c in changes.Modified.Select(async e => await (Task)tick.Invoke(ChangeType, e.ObjectStateEntry.Entity, ModifyMethod)))
					await c;
			if (DeleteMethod != null && changes.Modified != null)
				foreach (var c in changes.Deleted.Select(async e => await (Task)tick.Invoke(ChangeType, e, DeleteMethod)))
					await c;
           
        }

    }

	public abstract class ProcessorBaseAsync
    {
        public string DeleteMethod { get; set; }
        public string ModifyMethod { get; set; }
        public string CreateMethod { get; set; }
        public string PostCreateMethod { get; set; }
        public Type ChangeType { get; set; }
    }

	public abstract class ContextProcessorBaseAsync<ContextType> : ProcessorBaseAsync
        where ContextType : IInjectableContextAsync
    {
        protected Func<Type, Type, IEnumerable<object>, string, IInjectableContextAsync,Task< bool>> tick = async (changeType, itemType, items, methodName, ctx) =>
        {
            var m = changeType.MakeGenericType(typeof(ContextType), itemType);
            var processors = ObjectFactory.GetAllInstances(m);
            var cast = typeof(System.Linq.Enumerable).GetMethod("Cast", BindingFlags.Static | BindingFlags.Public).MakeGenericMethod(new Type[] { itemType });
            foreach (var processor in processors)
            {
                await (Task) processor.GetType().GetMethods().Single(method => method.Name == methodName && method.GetParameters().Count() == 2 && method.GetParameters().First().ParameterType == typeof(ContextType) && method.GetParameters().Last().ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>) && method.GetParameters().Last().ParameterType.GetGenericArguments()[0] == itemType).Invoke(processor, new object[] { ctx, cast.Invoke(items, new object[] { items }) });
            }
            return true;
			
        };
    }


	public class PreProcessorAsync<ContextType> : ContextProcessorBaseAsync<ContextType>, IPreProcessorAsync
        where ContextType : IInjectableContextAsync
    {
        public Type CreateChangeType { get; set; }
        public Type ModifyChangeType { get; set; }
        public Type DeleteChangeType { get; set; }




        public virtual async Task ProcessChanges(Changes changes, ContextType ctx)
        {
            if (CreateMethod != null && changes.Added != null)
            {
				if (CreateChangeType != null)
					foreach (var c in changes.Added.GroupBy(c => c.Entity.GetType()).Select(async e => await (Task)tick.Invoke(CreateChangeType, e.Key, e.Select(ee => ee.Entity), CreateMethod, ctx)))
						await c;
				else
					if (ChangeType != null)
						foreach(var c in changes.Added.GroupBy(c => c.Entity.GetType()).Select(async e => await (Task)tick.Invoke(ChangeType, e.Key, e.Select(ee => ee.Entity), CreateMethod, ctx)))
							await c;
            }

			if (ModifyMethod != null && changes.Modified != null)
				if (ModifyChangeType != null)
					foreach (var c in changes.Modified.GroupBy(c => c.ObjectStateEntry.Entity.GetType()).Select(async e => await (Task)tick.Invoke(ModifyChangeType, e.Key, e.Select(ee => ee.ObjectStateEntry.Entity), ModifyMethod, ctx)))
						await c;
				else
					if (ChangeType != null)
						foreach (var c in changes.Modified.GroupBy(c => c.ObjectStateEntry.Entity.GetType()).Select(async e => await (Task)tick.Invoke(ChangeType, e.Key, e.Select(ee => ee.ObjectStateEntry.Entity), ModifyMethod, ctx)))
							await c;

			if (DeleteMethod != null && changes.Deleted != null)
				if (DeleteChangeType != null)
					foreach (var c in changes.Deleted.GroupBy(c => c.GetType()).Select(async e => await (Task)tick.Invoke(DeleteChangeType, e.Key, e, DeleteMethod, ctx)))
						await c;
				else
					if (ChangeType != null)
						foreach (var c in changes.Deleted.GroupBy(c => c.GetType()).Select(async e => await (Task)tick.Invoke(ChangeType, e.Key, e, DeleteMethod, ctx)))
							await c;

        }


        public async Task doProcessChanges(Changes changes, object context)
        {
            await ProcessChanges(changes, (ContextType)context);
        }
    }


	public interface IContextProcessorAsync : IProcessorAsync
    {
    }



	public class ContextProcessorAsync<ContextType> : ContextProcessorBaseAsync<ContextType>, IContextProcessorAsync
        where ContextType : class, IInjectableContextAsync
    {
        public async Task ProcessChangesAsync(Changes changes)
        {
           await ObjectFactory.GetInstance<ContextType>().ProcessAsync(

                async (ctx) =>
                {
					if (CreateMethod != null && changes.Added != null)
						foreach (var c in changes.Added.GroupBy(c => c.Entity.GetType()).Select(async e => await (Task)tick.Invoke(ChangeType, e.Key, e.Select(ee => ee.Entity), CreateMethod, ctx)))
							await c;

					if (PostCreateMethod != null && changes.Added != null)
						foreach (var c in changes.Added.GroupBy(c => c.Entity.GetType()).Select(async e => await (Task)tick.Invoke(ChangeType, e.Key, e.Select(ee => ee.Entity), PostCreateMethod, ctx)))
							await c;

    
                    if (ModifyMethod!= null && changes.Modified != null)
                        foreach(var c in changes.Modified.GroupBy(c => c.ObjectStateEntry.Entity.GetType()).Select(async e => await (Task) tick.Invoke(ChangeType, e.Key, e.Select(ee => ee.ObjectStateEntry.Entity), ModifyMethod, ctx)))
							await c;

					if (DeleteMethod != null && changes.Deleted != null)
						foreach (var c in changes.Deleted.GroupBy(c => c.GetType()).Select(async e => await (Task)tick.Invoke(ChangeType, e.Key, e, DeleteMethod, ctx)))
							await c;

                }

                );
        }




    }



	public class PreContextProcessorAsync<ContextType> : PreProcessorAsync<ContextType>, IPreProcessorAsync<ContextType>
        where ContextType : IInjectableContextAsync
    {

    }
}



