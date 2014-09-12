using StructureMap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SiGyl.Models.Infrastructure
{
    public static class FluentList
    {
        public static T FluentAdd<T>(this List<T> list, T item)
        {
            list.Add(item);
            return item;
        }
        public static T FluentRemove<T>(this List<T> list, T item)
        {
            list.Remove(item);
            return item;
        }

        public static T FluentRemove<T>(this List<T> list, Func< List<T>, T> item)
        {
            var v = item.Invoke(list);
            list.Remove(v);
            return v;
        }

        public static List<T> FluentListAdd<T>(this List<T> list, T item)
        {
            list.Add(item);
            return list;
        }


        public static IList<T> Replace<T>(this IList<T> list, T newItem, Func<T, bool> find)
        {

            if (list == null)
                list = new List<T>();
         //   lock (list)
          //  {
                var v = list.SingleOrDefault(find);
                
                    if (v != null)
                        list.Remove(v);
                    list.Add(newItem);
                
                return list;
           // }
            
        }

        
    }
}