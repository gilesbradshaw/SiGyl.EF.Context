using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace SiGyl.EF.Context.Infrastructure
{


    public abstract class ContextSubscriptionAsync<TContext> : IContextSubscriptionAsync
       where TContext : IContext
    {
		internal protected static ConcurrentDictionary<Type, ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentDictionary<Type, IContextSubscriptionAsync>>>> SubscriptionDictionary = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentDictionary<Type, IContextSubscriptionAsync>>>>();

        //only for use during testing!
    static public void Reset()
    {
        SubscriptionDictionary = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentDictionary<Type, IContextSubscriptionAsync>>>>();
    }


    }



	public abstract class ContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime> : IContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>
		where TContextAsync : IContextAsync
    {

		public IContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime> Initialise(TConfiguration configuration, TRuntime item)
        {
			var contextDictionary = ContextSubscriptionAsync<TContextAsync>.SubscriptionDictionary.GetOrAdd(typeof(TContextAsync), t => new ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentDictionary<Type, IContextSubscriptionAsync>>>());
			var idDictionary = contextDictionary.GetOrAdd(item.GetType(), t => new ConcurrentDictionary<object, ConcurrentDictionary<Type, IContextSubscriptionAsync>>());
			var subscriberDictionary = idDictionary.GetOrAdd(GetKey(item), t => new ConcurrentDictionary<Type, IContextSubscriptionAsync>());
			return subscriberDictionary.GetOrAdd(this.GetType(), t => GetInitialised(configuration, item)) as IContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>;

        }

		protected abstract IContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime> GetInitialised(TConfiguration configuration, TRuntime item);

        protected abstract object GetKey(TRuntime item);
        protected abstract object GetKey(TConfiguration item);





		Func<object, ContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>, Func<IContextSubscriptionAsync, Task>, Task> d =
            async (o,i, a) =>
            {
                await a.Invoke(
					ContextSubscriptionAsync<TContextAsync>.SubscriptionDictionary.GetOrAdd(typeof(TContextAsync), (t) => new ConcurrentDictionary<Type, ConcurrentDictionary<object, ConcurrentDictionary<Type, IContextSubscriptionAsync>>>())
					.GetOrAdd(typeof(TRuntime), (t) => new ConcurrentDictionary<object, ConcurrentDictionary<Type, IContextSubscriptionAsync>>())
					.GetOrAdd(o, (t) => new ConcurrentDictionary<Type, IContextSubscriptionAsync>())
                    .GetOrAdd(i.GetType(), i)
                    );
            };

        public async virtual Task CreateAsync(TConfiguration item)
        {
			await d.Invoke(GetKey(item), this, async (s) => await ((ContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>)s).ConfigurationCreateAsync(item));
        }

		public async virtual Task ModifyAsync(TConfiguration item)
        {
			await d.Invoke(GetKey(item), this, async (s) => await ((ContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>)s).ConfigurationModifyAsync(item));
        }

		public async virtual Task ModifyAsync(TRuntime item)
        {
			await d.Invoke(GetKey(item), this, async (s) => await ((ContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>)s).RuntimeModifyAsync(item));
        }

		public async virtual Task CreateAsync(TRuntime item)
        {
			await d.Invoke(GetKey(item), this, async (s) => await ((ContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>)s).RuntimeCreateAsync(item));
        }

		public async virtual Task DeleteAsync(TRuntime item)
        {
			await d.Invoke(GetKey(item), this, async (s) => await ((ContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>)s).RuntimeDeleteAsync(item));
        }



		protected abstract Task RuntimeModifyAsync(TRuntime item);


		protected abstract Task RuntimeCreateAsync(TRuntime item);


		protected abstract Task RuntimeDeleteAsync(TRuntime item);


		protected abstract Task ConfigurationCreateAsync(TConfiguration item);

		protected abstract Task ConfigurationModifyAsync(TConfiguration item);

        public static void Inject<T>(StructureMap.IInitializationExpression x)
			where T : ContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>
        {
			x.For<IContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime>>().Use<T>();
			x.For<IConfigurationSubscriptionAsync<TContextAsync, TConfiguration>>().Use<T>();
			x.For<IRuntimeSubscriptionAsync<TContextAsync, TRuntime>>().Use<T>();
        }
        
    }

	public interface IContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime> : IConfigurationSubscriptionAsync<TContextAsync, TConfiguration>, IRuntimeSubscriptionAsync<TContextAsync, TRuntime>
		where TContextAsync : IContext
    {
		IContextSubscriptionAsync<TContextAsync, TConfiguration, TRuntime> Initialise(TConfiguration configuration, TRuntime item);
    }


	public interface IRuntimeSubscriptionAsync<TContext, T> : IContextSubscriptionAsync
        where TContext : IContext
    {
		Task ModifyAsync(T item);
		Task CreateAsync(T item);
		Task DeleteAsync(T item);

        
    }

	public interface IConfigurationSubscriptionAsync<TContext, T> : IContextSubscriptionAsync
       where TContext : IContext
    {

		Task CreateAsync(T item);
		Task ModifyAsync(T item);
		


    }


    





    public interface IContextSubscriptionAsync { }
}
