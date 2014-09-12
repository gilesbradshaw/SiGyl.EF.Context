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
    public interface IInjectableContextAsync : IInjectableContext
    {
		Func<Task> Presave { get; set; }
		Task<int> SaveChangesAsync();
		IEnumerable<IPreProcessorAsync> PreProcessorAsyncs { get; }
		IEnumerable<IContextProcessorAsync> ProcessorAsyncs { get; }
		IEnumerable<IProcessorAsync> PostProcessorAsyncs { get; }
		IEnumerable<IProcessorAsync> PostPostProcessorAsyncs { get; }
		IInjectableContextAsync PreProcessAsync(IPreProcessorAsync processAsync);
		IInjectableContextAsync ProcessAsync(IContextProcessorAsync processAsync);
		IInjectableContextAsync PostProcessAsync(IProcessorAsync processAsync);
		IInjectableContextAsync PostPostProcessAsync(IProcessorAsync processAsync);



    }
}
