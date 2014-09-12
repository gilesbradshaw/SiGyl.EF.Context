using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;

namespace SiGyl.Models.Infrastructure.ChangeDetection
{

    public class ModifiedObjectState
    {
        public ObjectStateEntry ObjectStateEntry { get; set; }
        public IEnumerable<string> ModifiedProperties { get; set; }
    }

    public class Changes
    {
        public IEnumerable<ObjectStateEntry> Added { get; set; }
        public IEnumerable<object> Deleted { get; set; }
        public IEnumerable<ModifiedObjectState> Modified { get; set; }
        public SiGyl.EF.Context.IContextAsync Context { get; set; }
        public Changes Parent { get; set; }
        Dictionary<Transaction, Changes> _children = new Dictionary<Transaction, Changes>();
        public Dictionary<Transaction, Changes> Children { get { return _children; } }
        public bool HasChanges
        {
            get {

                if (Added == null && Modified == null && Deleted == null)
                    return false;
                return Added.Any() || Modified.Any() || Deleted.Any(); }
        }


        public void Process(Action<Transaction, Changes> processAction, Transaction key)
        {
            processAction(key, this);
            foreach (var change in Children)
            {
                change.Value.Process(processAction, change.Key);
            }

        }


		public async Task ProcessAsync(Func<Transaction, Changes, Task> processActionAsync, Transaction key)
		{
			await processActionAsync(key, this);
			foreach (var change in Children)
			{
				await change.Value.ProcessAsync(processActionAsync, change.Key);
			}

		}

    }
}
