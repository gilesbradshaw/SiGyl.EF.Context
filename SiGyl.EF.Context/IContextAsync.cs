using Newtonsoft.Json.Linq;
using SiGyl.EF.Context.Processors;
using SiGyl.Models.Infrastructure.ChangeDetection;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Infrastructure;
using System.Reflection;
using System.Threading.Tasks;

namespace SiGyl.EF.Context
{
    public interface IContextAsync : IContext
    {
		Func<Task> Presave { get; set; }
		Task<int> SaveChangesAsync();
		IEnumerable<IPreProcessorAsync> PreProcessorAsyncs { get; }
		IEnumerable<IContextProcessorAsync> ProcessorAsyncs { get; }
		IEnumerable<IProcessorAsync> PostProcessorAsyncs { get; }
		IEnumerable<IProcessorAsync> PostPostProcessorAsyncs { get; }
		IContextAsync PreProcessAsync(IPreProcessorAsync processAsync);
		IContextAsync ProcessAsync(IContextProcessorAsync processAsync);
		IContextAsync PostProcessAsync(IProcessorAsync processAsync);
		IContextAsync PostPostProcessAsync(IProcessorAsync processAsync);



    }
}
