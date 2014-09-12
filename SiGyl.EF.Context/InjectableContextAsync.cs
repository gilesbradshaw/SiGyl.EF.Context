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
	public abstract class InjectableContextAsync : InjectableContext
	{
		public InjectableContextAsync(string nameOrConnectionString) : base(nameOrConnectionString) { }



		static Dictionary<Type, List<IContextProcessorAsync>> _processorAsyncs = new Dictionary<Type, List<IContextProcessorAsync>>();

		static Dictionary<Type, List<IProcessorAsync>> _postProcessorAsyncs = new Dictionary<Type, List<IProcessorAsync>>();
		static Dictionary<Type, List<IProcessorAsync>> _postPostProcessorAsyncs = new Dictionary<Type, List<IProcessorAsync>>();


		static Dictionary<Type, List<IPreProcessorAsync>> _preProcessorAsyncs = new Dictionary<Type, List<IPreProcessorAsync>>();

		public IEnumerable<IPreProcessorAsync> PreProcessorAsyncs { get { return _preProcessorAsyncs.ContainsKey(this.GetType()) ? _preProcessorAsyncs[this.GetType()] : null; } }
		public IEnumerable<IContextProcessorAsync> ProcessorAsyncs { get { return _processorAsyncs.ContainsKey(this.GetType()) ? _processorAsyncs[this.GetType()] : null; } }
		public IEnumerable<IProcessorAsync> PostProcessorAsyncs { get { return _postProcessorAsyncs.ContainsKey(this.GetType()) ? _postProcessorAsyncs[this.GetType()] : null; } }
		public IEnumerable<IProcessorAsync> PostPostProcessorAsyncs { get { return _postPostProcessorAsyncs.ContainsKey(this.GetType()) ? _postPostProcessorAsyncs[this.GetType()] : null; } }

		public IInjectableContextAsync PreProcessAsync(IPreProcessorAsync preProcessAsync)
		{
			if (!_preProcessorAsyncs.ContainsKey(this.GetType()))
				_preProcessorAsyncs.Add(this.GetType(), new List<IPreProcessorAsync>());
			_preProcessorAsyncs[this.GetType()].Add(preProcessAsync);
			return this as IInjectableContextAsync;
		}
		public IInjectableContextAsync ProcessAsync(IContextProcessorAsync processAsync)
		{
			if (!_processorAsyncs.ContainsKey(this.GetType()))
				_processorAsyncs.Add(this.GetType(), new List<IContextProcessorAsync>());
			_processorAsyncs[this.GetType()].Add(processAsync);
			return this as IInjectableContextAsync;
		}

		public IInjectableContextAsync PostProcessAsync(IProcessorAsync postProcessAsync)
		{
			if (!_postProcessorAsyncs.ContainsKey(this.GetType()))
				_postProcessorAsyncs.Add(this.GetType(), new List<IProcessorAsync>());
			_postProcessorAsyncs[this.GetType()].Add(postProcessAsync);
			return this as IInjectableContextAsync;
		}

		public IInjectableContextAsync PostPostProcessAsync(IProcessorAsync postPostProcessAsync)
		{
			if (!_postPostProcessorAsyncs.ContainsKey(this.GetType()))
				_postPostProcessorAsyncs.Add(this.GetType(), new List<IProcessorAsync>());
			_postPostProcessorAsyncs[this.GetType()].Add(postPostProcessAsync);
			return this as IInjectableContextAsync;
		}


	}

    
}



