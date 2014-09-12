using SiGyl.Models.Infrastructure.ChangeDetection;
using StructureMap;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Transactions;
using System.Linq;
using System.Threading.Tasks;


namespace SiGyl.EF.Context.Infrastructure
{






    public static class FluentContext
    {

		//notn permanent solution
		static ConcurrentDictionary<Transaction, Changes> Changes = new ConcurrentDictionary<Transaction, Changes>();

		

		static Func<Changes, Transaction, Func<Task>> GetPostProcessAsync = (changes, currentTransaction) => () =>
		{
			return changes.ProcessAsync(async (k, c) =>
			{
				if (Changes.TryRemove(k, out c))
					if (c.Context != null)
					{
						if (c.HasChanges && c.Context.PostProcessorAsyncs != null)
							foreach (var processorAsync in c.Context.PostProcessorAsyncs)
								await processorAsync.ProcessChangesAsync(c);
						if (c.HasChanges && c.Context.PostPostProcessorAsyncs != null && c.Context.PostPostProcessorAsyncs.Any())
						{
							await Task.Delay(1000);
							foreach (var processorAsync in c.Context.PostPostProcessorAsyncs)
								await processorAsync.ProcessChangesAsync(c);
						}
							
					}


			}, currentTransaction);
		};

		public static async Task<T> ProcessAsync<T>(this T context, Func<T, Task> process) where T : class, SiGyl.EF.Context.IContextAsync
		{
			var currentTransaction = Transaction.Current;
			//will store all the changes for the new transaction
			var changes = new Changes();
			//chain these changes to the parent changes
			if (currentTransaction != null)
			{
				Changes parent;
				Changes.TryGetValue(currentTransaction, out parent);
				changes.Parent = parent;

			}


			using (var ts = new TransactionScope(TransactionScopeOption.RequiresNew, new TransactionOptions { IsolationLevel = IsolationLevel.RepeatableRead, Timeout = TimeSpan.Zero }, TransactionScopeAsyncFlowOption.Enabled))
			{

				currentTransaction = Transaction.Current;
				Changes.TryAdd(currentTransaction, changes);
				if (changes.Parent != null)
					changes.Parent.Children.Add(currentTransaction, changes);

				//     try
				{
					context.Presave = async () =>
					{
						var newChanges = new Changes();

						while ((newChanges = context.DetectChanges(changes)).HasChanges && context.PreProcessorAsyncs != null)
							foreach (var processorAsync in context.PreProcessorAsyncs)
								await processorAsync.doProcessChanges(newChanges, context);
					};
					await process(context);

					if (context.Presave != null)
					{
						await context.Presave();
						var x = await context.SaveChangesAsync();
					}


					//run any configured post processors
					if (changes.HasChanges && context.ProcessorAsyncs != null)
						foreach (var processorAsync in context.ProcessorAsyncs)
							await processorAsync.ProcessChangesAsync(changes);

					ts.Complete();
				}
			}


			//if the top most transaction has completed run all the post processors





			if (changes.Parent == null)
			{

				await GetPostProcessAsync(changes, currentTransaction)();

			}

			return context;
		}


        //process a context running processors and post processors as required
      

        public class BlankLocker : IDisposable
        {

            public void Dispose()
            {

            }
        }
    }
}